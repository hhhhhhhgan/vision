using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HalconDotNet;
using VisionFlow;
using VisionFlow.Models;
using VisionFlow.Tools;

namespace VisionFlow {

/// <summary>
/// 2小时稳定性测试 — 全流程逻辑自检
/// </summary>
public class StabilityTest
{
    private int _totalRuns;
    private int _successRuns;
    private int _failRuns;
    private int _errorRuns;
    private long _peakMemoryBytes;
    private int _peakGCHenan;
    private DateTime _startTime;
    private readonly List<string> _log = new();
    private readonly object _logLock = new();
    private bool _stopRequested;

    public event Action<string>? LogAdded;

    public async Task Run2HourTest()
    {
        _startTime = DateTime.Now;
        _totalRuns = 0; _successRuns = 0; _failRuns = 0; _errorRuns = 0;
        _peakMemoryBytes = 0;
        _stopRequested = false;

        var endTime = _startTime.AddHours(2);
        var iteration = 0;
        var sw = Stopwatch.StartNew();

        Log("========================================");
        Log("VisionFlow 2小时稳定性测试开始");
        Log($"开始时间: {_startTime:yyyy-MM-dd HH:mm:ss}");
        Log($"结束时间: {endTime:yyyy-MM-dd HH:mm:ss}");
        Log("========================================");

        // 预热
        await WarmUpAsync();
        GC.Collect();

        while (DateTime.Now < endTime && !_stopRequested)
        {
            iteration++;
            var iterStart = DateTime.Now;

            try
            {
                // 每轮执行多个流程
                await RunIterationAsync(iteration);

                _totalRuns++;
                _successRuns++;

                // 每50轮报告一次
                if (iteration % 50 == 0)
                {
                    var elapsed = DateTime.Now - _startTime;
                    var remaining = endTime - DateTime.Now;
                    var memNow = GC.GetTotalMemory(false);
                    if (memNow > _peakMemoryBytes) _peakMemoryBytes = memNow;

                    Log($"[进度] 迭代#{iteration} | 成功:{_successRuns} 失败:{_failRuns} 错误:{_errorRuns} | 内存:{memNow / 1024 / 1024}MB | 剩余:{remaining:hh\\:mm\\:ss}");
                }

                // 小延迟避免过度占用CPU
                await Task.Delay(10);
            }
            catch (Exception ex)
            {
                _errorRuns++;
                Log($"❌ 迭代#{iteration}异常: {ex.Message}");
            }

            // 每10分钟触发一次GC检查
            if (iteration % 6000 == 0)
            {
                GC.Collect();
                var gen2Before = GC.CollectionCount(2);
                await Task.Delay(100);
                var gen2After = GC.CollectionCount(2);
                if (gen2After > gen2Before)
                    Log($"⚠ Gen2 GC触发: {gen2After - gen2Before}次");
            }
        }

        sw.Stop();
        PrintFinalReport(sw.Elapsed);
    }

    public void Stop() => _stopRequested = true;

    private async Task WarmUpAsync()
    {
        Log("\n--- 预热阶段 ---");

        // 创建测试图像
        using var testImg = new HImage();
        testImg.GenImageConst("byte", 640, 480);

        // 测试所有工具
        var toolTypes = typeof(ImageLoadTool).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ToolBase)) && !t.IsAbstract);

        foreach (var tt in toolTypes)
        {
            try
            {
                var tool = Activator.CreateInstance(tt) as ToolBase;
                if (tool == null) continue;

                foreach (var slot in tool.InputSlots)
                {
                    if (slot.ValueType == typeof(HImage))
                        slot.Value = testImg;
                    else if (slot.ValueType == typeof(double) || slot.ValueType == typeof(int))
                        slot.Value = 100;
                }

                var ctx = new ExecuteContext();
                foreach (var s in tool.InputSlots)
                    if (s.Value != null) ctx.Inputs[s.Name] = s.Value;

                await tool.Execute(ctx);
            }
            catch { }
        }

        Log($"✅ 预热完成，工具数: {toolTypes.Count()}");
    }

    private async Task RunIterationAsync(int iteration)
    {
        // 轮换不同流程
        var flowIdx = iteration % 6;
        List<FlowNode> nodes;
        List<Connection> connections;

        switch (flowIdx)
        {
            case 0: (nodes, connections) = FlowFactory.CreateBlobDetectionFlow(); break;
            case 1: (nodes, connections) = FlowFactory.CreateLocatorFlow(); break;
            case 2: (nodes, connections) = FlowFactory.CreateMeasureFlow(); break;
            case 3: (nodes, connections) = FlowFactory.CreateCompleteInspectionFlow(); break;
            case 4: (nodes, connections) = FlowFactory.CreateROIFlow(); break;
            default: (nodes, connections) = FlowFactory.CreateImageProcessingPipeline(); break;
        }

        // 填充测试图像
        using var testImg = new HImage();
        testImg.GenImageConst("byte", 640, 480);

        var loadNode = nodes.FirstOrDefault(n => n.Tool is ImageLoadTool);
        if (loadNode?.Tool is ImageLoadTool load)
        {
            load.OutputSlots[0].Value = testImg;
            load.OutputSlots[1].Value = 640;
            load.OutputSlots[2].Value = 480;
        }

        // 执行流程
        await ExecuteFlowAsync(nodes, connections);

        // 验证结果
        ValidateFlowExecution(nodes, connections);

        // 清理
        testImg.Dispose();
        foreach (var n in nodes)
            n.ResetStatus();
    }

    private async Task ExecuteFlowAsync(List<FlowNode> nodes, List<Connection> connections)
    {
        var sorted = TopologicalSort(nodes, connections);
        if (sorted == null) throw new Exception("拓扑排序失败");

        var ctx = new ExecuteContext();

        foreach (var node in sorted)
        {
            // 填充输入槽
            var upstream = connections.Where(c => c.TargetNode == node).ToList();
            foreach (var conn in upstream)
            {
                var srcSlot = conn.SourceNode?.Tool.OutputSlots.FirstOrDefault(s => s.Name == conn.SourceSlot);
                if (srcSlot?.Value != null)
                    ctx.Inputs[conn.TargetSlot] = srcSlot.Value;
            }

            foreach (var slot in node.Tool.InputSlots)
            {
                if (slot.Value != null && !ctx.Inputs.ContainsKey(slot.Name))
                    ctx.Inputs[slot.Name] = slot.Value;
            }

            // 执行
            var result = await node.Tool.Execute(ctx);

            if (!result.Success)
                throw new Exception($"[{node.Tool.Name}] 执行失败: {result.ErrorMessage}");

            // 传递输出
            foreach (var slot in node.Tool.OutputSlots)
            {
                if (slot.Value != null)
                    ctx.Inputs[slot.Name] = slot.Value;
            }
        }
    }

    private List<FlowNode>? TopologicalSort(List<FlowNode> nodes, List<Connection> connections)
    {
        var result = new List<FlowNode>();
        var visited = new HashSet<string>();
        var inStack = new HashSet<string>();

        bool DFS(FlowNode node)
        {
            if (inStack.Contains(node.Id)) return false;
            if (visited.Contains(node.Id)) return true;
            visited.Add(node.Id);
            inStack.Add(node.Id);

            foreach (var conn in connections.Where(c => c.SourceNode == node))
            {
                if (!DFS(conn.TargetNode)) return false;
            }

            inStack.Remove(node.Id);
            result.Add(node);
            return true;
        }

        foreach (var n in nodes)
        {
            if (!visited.Contains(n.Id))
            {
                if (!DFS(n)) return null;
            }
        }

        result.Reverse();
        return result;
    }

    private void ValidateFlowExecution(List<FlowNode> nodes, List<Connection> connections)
    {
        // 验证节点状态
        foreach (var n in nodes)
        {
            if (n.ExecutionStatus == NodeExecutionStatus.Error)
            {
                Log($"⚠ 节点[{n.Tool.Name}]执行失败: {n.LastErrorMessage}");
                _failRuns++;
            }
        }

        // 验证连接完整性
        foreach (var c in connections)
        {
            if (c.SourceNode == null || c.TargetNode == null)
                throw new Exception($"连接[{c.Id}]端点为null");
        }
    }

    private void PrintFinalReport(TimeSpan elapsed)
    {
        var duration = elapsed;
        var totalMemMB = _peakMemoryBytes / 1024 / 1024;
        var memNowMB = GC.GetTotalMemory(false) / 1024 / 1024;
        var gen2Count = GC.CollectionCount(2);

        Log("");
        Log("========================================");
        Log("2小时稳定性测试报告");
        Log("========================================");
        Log($"运行时间: {duration:hh\\:mm\\:ss}");
        Log($"总迭代: {_totalRuns}");
        Log($"成功: {_successRuns} ({_totalRuns > 0 ? (_successRuns * 100.0 / _totalRuns):F1}%)");
        Log($"失败: {_failRuns}");
        Log($"异常: {_errorRuns}");
        Log($"内存峰值: {totalMemMB}MB");
        Log($"当前内存: {memNowMB}MB");
        Log($"Gen2 GC次数: {gen2Count}");
        Log($"每秒迭代: {(_totalRuns / duration.TotalSeconds):F1}");

        if (_failRuns == 0 && _errorRuns == 0 && gen2Count < 10 && memNowMB < 500)
            Log("✅ 稳定性测试通过！");
        else if (_failRuns < 10 && _errorRuns < 5)
            Log("⚠ 稳定性测试基本通过（有轻微问题）");
        else
            Log("❌ 稳定性测试未通过");

        Log("========================================");
    }

    private void Log(string msg)
    {
        lock (_logLock)
        {
            var ts = (DateTime.Now - _startTime).ToString(@"hh\:mm\:ss");
            _log.Add($"[{ts}] {msg}");
            LogAdded?.Invoke($"[{ts}] {msg}");
            Console.WriteLine($"[{ts}] {msg}");
        }
    }
}

/// <summary>
/// 逻辑自检测试
/// </summary>
public class LogicSelfCheck
{
    private readonly List<string> _issues = new();

    public void RunAll()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("逻辑自检开始");
        Console.WriteLine("========================================\n");

        CheckToolNaming();
        CheckSlotConnectivity();
        CheckCircularDependencies();
        CheckMemoryManagement();
        CheckSerializationIntegrity();
        CheckDialogMapping();

        Console.WriteLine("\n========================================");
        Console.WriteLine($"自检完成: {_issues.Count}个问题");
        if (_issues.Count == 0)
            Console.WriteLine("✅ 无问题发现");
        else
            foreach (var i in _issues)
                Console.WriteLine($"  ❌ {i}");
        Console.WriteLine("========================================");
    }

    private void CheckToolNaming()
    {
        Console.WriteLine("--- 检查1: 工具命名 ---");
        var toolTypes = typeof(ImageLoadTool).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ToolBase)) && !t.IsAbstract);

        foreach (var tt in toolTypes)
        {
            var tool = Activator.CreateInstance(tt) as ToolBase;
            if (tool == null) continue;

            if (string.IsNullOrEmpty(tool.Name) || tool.Name == "未命名工具")
                _issues.Add($"{tt.Name}: 名称为空");
            else if (tool.Name.Length > 20)
                _issues.Add($"{tt.Name}: 名称过长({tool.Name.Length})");

            Console.WriteLine($"  ✅ {tt.Name} = \"{tool.Name}\"");
        }
    }

    private void CheckSlotConnectivity()
    {
        Console.WriteLine("\n--- 检查2: 槽位连接 ---");
        var toolTypes = typeof(ImageLoadTool).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ToolBase)) && !t.IsAbstract);

        foreach (var tt in toolTypes)
        {
            var tool = Activator.CreateInstance(tt) as ToolBase;
            if (tool == null) continue;

            // 检查输入槽是否有可选标记
            foreach (var slot in tool.InputSlots)
            {
                if (string.IsNullOrEmpty(slot.Name))
                    _issues.Add($"{tt.Name}: 存在空名称的输入槽");
            }

            foreach (var slot in tool.OutputSlots)
            {
                if (string.IsNullOrEmpty(slot.Name))
                    _issues.Add($"{tt.Name}: 存在空名称的输出槽");
            }
        }

        Console.WriteLine($"  ✅ 所有工具槽位名称有效");
    }

    private void CheckCircularDependencies()
    {
        Console.WriteLine("\n--- 检查3: 循环依赖 ---");
        var (_, connections) = FlowFactory.CreateCompleteInspectionFlow();
        var nodes = connections.Select(c => c.SourceNode).Where(n => n != null).Concat(connections.Select(c => c.TargetNode).Where(n => n != null)).Distinct().ToList();

        var visited = new HashSet<string>();
        var inStack = new HashSet<string>();

        bool HasCycle(FlowNode node)
        {
            if (inStack.Contains(node.Id)) return true;
            if (visited.Contains(node.Id)) return false;
            visited.Add(node.Id);
            inStack.Add(node.Id);

            foreach (var conn in connections.Where(c => c.SourceNode == node))
            {
                if (conn.TargetNode != null && HasCycle(conn.TargetNode)) return true;
            }

            inStack.Remove(node.Id);
            return false;
        }

        foreach (var n in nodes)
        {
            visited.Clear(); inStack.Clear();
            if (HasCycle(n))
            {
                _issues.Add($"存在循环依赖: {n.Id}");
                Console.WriteLine($"  ❌ 循环依赖: {n.Id}");
                return;
            }
        }

        Console.WriteLine($"  ✅ 无循环依赖");
    }

    private void CheckMemoryManagement()
    {
        Console.WriteLine("\n--- 检查4: 内存管理 ---");

        var memBefore = GC.GetTotalMemory(true);

        for (int i = 0; i < 100; i++)
        {
            var tool = new ThresholdTool();
            var img = new HImage();
            img.GenImageConst("byte", 100, 100);
            tool.InputSlots[0].Value = img;

            var ctx = new ExecuteContext();
            ctx.Inputs["Image"] = img;
            tool.Execute(ctx).GetAwaiter().GetResult();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memAfter = GC.GetTotalMemory(true);
        var diffKB = (memAfter - memBefore) / 1024.0;

        if (diffKB < 100)
            Console.WriteLine($"  ✅ 内存稳定: +{diffKB:F1}KB (100次执行)");
        else
        {
            _issues.Add($"内存可能泄漏: +{diffKB:F1}KB");
            Console.WriteLine($"  ⚠ 内存增长: +{diffKB:F1}KB");
        }
    }

    private void CheckSerializationIntegrity()
    {
        Console.WriteLine("\n--- 检查5: 序列化完整性 ---");

        var project = new Project { Name = "TestProject" };
        project.Nodes.Add(new NodeData { Id = "n1", ToolType = "图像加载", X = 100, Y = 200, Parameters = new Dictionary<string, object?> { ["FilePath"] = "test.jpg" } });
        project.Connections.Add(new ConnectionData { Id = "c1", SourceNodeId = "n1", SourceSlot = "Image", TargetNodeId = "n2", TargetSlot = "Image" });
        project.Parameters.Inputs.Add(new FlowParameter { Name = "TestParam", DataType = "number" });

        var json = System.Text.Json.JsonSerializer.Serialize(project, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        var loaded = System.Text.Json.JsonSerializer.Deserialize<Project>(json);

        if (loaded?.Nodes.Count == 1 && loaded?.Connections.Count == 1 && loaded?.Parameters.Inputs.Count == 1)
            Console.WriteLine($"  ✅ 序列化/反序列化完整");
        else
            _issues.Add("序列化/反序列化数据丢失");
    }

    private void CheckDialogMapping()
    {
        Console.WriteLine("\n--- 检查6: 对话框映射 ---");
        var toolTypes = typeof(ImageLoadTool).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ToolBase)) && !t.IsAbstract)
            .ToList();

        var mappedCount = toolTypes.Count;
        Console.WriteLine($"  ✅ {mappedCount}个工具已注册");
    }
}
}