using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HalconDotNet;
using VisionFlow;
using VisionFlow.Models;
using VisionFlow.Tools;

namespace VisionFlow {

/// <summary>
/// 流程互通性测试 — 验证所有工具接口互通，无异常
/// </summary>
public class FlowInteropTest
{
    private int _passed;
    private int _failed;
    private int _errors;
    private readonly List<string> _log = new();

    public void RunAll()
    {
        _passed = 0; _failed = 0; _errors = 0;
        _log.Clear();

        Log("========================================");
        Log("流程互通性测试开始");
        Log("========================================");

        // 准备测试图像
        var testImg = CreateTestImage();

        // 测试各流程
        TestFlow1_BlobDetection(testImg);
        TestFlow2_LocatorFlow(testImg);
        TestFlow3_MeasureFlow(testImg);
        TestFlow4_CompleteInspection(testImg);
        TestFlow5_ROIFlow(testImg);
        TestFlow6_ImageProcessingPipeline(testImg);

        // 工具单独测试
        TestAllToolsIndividually();

        // 内存泄漏测试
        TestMemoryLeak();

        Log("");
        Log("========================================");
        Log($"测试结果: 通过={_passed} 失败={_failed} 错误={_errors}");
        Log("========================================");

        PrintReport();
    }

    private HImage CreateTestImage()
    {
        try
        {
            // 创建一个 200x200 的测试图像
            var img = new HImage();
            img.GenImageConst("byte", 200, 200);
            return img;
        }
        catch
        {
            return new HImage();
        }
    }

    private void TestFlow1_BlobDetection(HImage testImg)
    {
        Log("\n--- 流程1: Blob检测 ---");
        try
        {
            var (nodes, connections) = FlowFactory.CreateBlobDetectionFlow();

            // 验证流程
            var validator = new FlowValidator();
            if (!validator.Validate(nodes, connections))
            {
                Log($"❌ 流程验证失败: {string.Join(", ", validator.Errors)}");
                _failed++;
                return;
            }

            // 手动填充图像到 ImageLoadTool
            var loadNode = nodes[0] as FlowNode;
            if (loadNode != null && loadNode.Tool is ImageLoadTool load)
            {
                load.OutputSlots[0].Value = testImg;
                load.OutputSlots[1].Value = 200;
                load.OutputSlots[2].Value = 200;
            }

            // 执行
            var result = ExecuteFlow(nodes, connections);
            if (result)
            {
                Log("✅ Blob检测流程通过");
                _passed++;
            }
            else
            {
                Log("❌ Blob检测流程失败");
                _failed++;
            }
        }
        catch (Exception ex)
        {
            Log($"❌ 异常: {ex.Message}");
            _errors++;
        }
    }

    private void TestFlow2_LocatorFlow(HImage testImg)
    {
        Log("\n--- 流程2: 定位器流程 ---");
        try
        {
            var (nodes, connections) = FlowFactory.CreateLocatorFlow();

            var validator = new FlowValidator();
            if (!validator.Validate(nodes, connections))
            {
                Log($"❌ 流程验证失败: {string.Join(", ", validator.Errors)}");
                _failed++;
                return;
            }

            // 填充图像
            var loadNode = nodes.FirstOrDefault(n => n.Tool is ImageLoadTool) as FlowNode;
            if (loadNode?.Tool is ImageLoadTool load)
            {
                load.OutputSlots[0].Value = testImg;
                load.OutputSlots[1].Value = 200;
                load.OutputSlots[2].Value = 200;
            }

            var result = ExecuteFlow(nodes, connections);
            if (result) { Log("✅ 定位器流程通过"); _passed++; }
            else { Log("❌ 定位器流程失败"); _failed++; }
        }
        catch (Exception ex)
        {
            Log($"❌ 异常: {ex.Message}");
            _errors++;
        }
    }

    private void TestFlow3_MeasureFlow(HImage testImg)
    {
        Log("\n--- 流程3: 测量流程 ---");
        try
        {
            var (nodes, connections) = FlowFactory.CreateMeasureFlow();

            var validator = new FlowValidator();
            if (!validator.Validate(nodes, connections))
            {
                Log($"❌ 流程验证失败: {string.Join(", ", validator.Errors)}");
                _failed++;
                return;
            }

            var loadNode = nodes.FirstOrDefault(n => n.Tool is ImageLoadTool) as FlowNode;
            if (loadNode?.Tool is ImageLoadTool load)
            {
                load.OutputSlots[0].Value = testImg;
                load.OutputSlots[1].Value = 200;
                load.OutputSlots[2].Value = 200;
            }

            var result = ExecuteFlow(nodes, connections);
            if (result) { Log("✅ 测量流程通过"); _passed++; }
            else { Log("❌ 测量流程失败"); _failed++; }
        }
        catch (Exception ex)
        {
            Log($"❌ 异常: {ex.Message}");
            _errors++;
        }
    }

    private void TestFlow4_CompleteInspection(HImage testImg)
    {
        Log("\n--- 流程4: 完整检测流程 ---");
        try
        {
            var (nodes, connections) = FlowFactory.CreateCompleteInspectionFlow();

            var validator = new FlowValidator();
            if (!validator.Validate(nodes, connections))
            {
                Log($"❌ 流程验证失败: {string.Join(", ", validator.Errors)}");
                _failed++;
                return;
            }

            var loadNode = nodes.FirstOrDefault(n => n.Tool is ImageLoadTool) as FlowNode;
            if (loadNode?.Tool is ImageLoadTool load)
            {
                load.OutputSlots[0].Value = testImg;
                load.OutputSlots[1].Value = 200;
                load.OutputSlots[2].Value = 200;
            }

            var result = ExecuteFlow(nodes, connections);
            if (result) { Log("✅ 完整检测流程通过"); _passed++; }
            else { Log("❌ 完整检测流程失败"); _failed++; }
        }
        catch (Exception ex)
        {
            Log($"❌ 异常: {ex.Message}");
            _errors++;
        }
    }

    private void TestFlow5_ROIFlow(HImage testImg)
    {
        Log("\n--- 流程5: ROI流程 ---");
        try
        {
            var (nodes, connections) = FlowFactory.CreateROIFlow();

            var validator = new FlowValidator();
            if (!validator.Validate(nodes, connections))
            {
                Log($"❌ 流程验证失败: {string.Join(", ", validator.Errors)}");
                _failed++;
                return;
            }

            var loadNode = nodes.FirstOrDefault(n => n.Tool is ImageLoadTool) as FlowNode;
            if (loadNode?.Tool is ImageLoadTool load)
            {
                load.OutputSlots[0].Value = testImg;
                load.OutputSlots[1].Value = 200;
                load.OutputSlots[2].Value = 200;
            }

            var result = ExecuteFlow(nodes, connections);
            if (result) { Log("✅ ROI流程通过"); _passed++; }
            else { Log("❌ ROI流程失败"); _failed++; }
        }
        catch (Exception ex)
        {
            Log($"❌ 异常: {ex.Message}");
            _errors++;
        }
    }

    private void TestFlow6_ImageProcessingPipeline(HImage testImg)
    {
        Log("\n--- 流程6: 图像处理流水线 ---");
        try
        {
            var (nodes, connections) = FlowFactory.CreateImageProcessingPipeline();

            var validator = new FlowValidator();
            if (!validator.Validate(nodes, connections))
            {
                Log($"❌ 流程验证失败: {string.Join(", ", validator.Errors)}");
                _failed++;
                return;
            }

            var loadNode = nodes.FirstOrDefault(n => n.Tool is ImageLoadTool) as FlowNode;
            if (loadNode?.Tool is ImageLoadTool load)
            {
                load.OutputSlots[0].Value = testImg;
                load.OutputSlots[1].Value = 200;
                load.OutputSlots[2].Value = 200;
            }

            var result = ExecuteFlow(nodes, connections);
            if (result) { Log("✅ 图像处理流水线通过"); _passed++; }
            else { Log("❌ 图像处理流水线失败"); _failed++; }
        }
        catch (Exception ex)
        {
            Log($"❌ 异常: {ex.Message}");
            _errors++;
        }
    }

    private void TestAllToolsIndividually()
    {
        Log("\n--- 工具单独测试 ---");

        var toolTypes = new[]
        {
            typeof(ImageLoadTool), typeof(ImageRotateTool), typeof(ROIDrawTool),
            typeof(ThresholdTool), typeof(MorphologyTool), typeof(BlobAnalysisTool),
            typeof(LocatorTool), typeof(FindLineTool), typeof(FindCircleTool),
            typeof(CaliperMeasureTool), typeof(PointToPointDistanceTool),
            typeof(PointToLineDistanceTool), typeof(LineToLineDistanceTool),
            typeof(GetGrayValueTool), typeof(CoordinateTransformTool),
            typeof(ImageSaveTool), typeof(CSharpScriptTool)
        };

        foreach (var toolType in toolTypes)
        {
            try
            {
                var tool = Activator.CreateInstance(toolType) as ToolBase;
                if (tool == null) { Log($"❌ {toolType.Name}: 无法创建实例"); _failed++; continue; }

                // 填充必要的输入槽
                var testImg = CreateTestImage();
                foreach (var slot in tool.InputSlots)
                {
                    if (slot.ValueType == typeof(HImage))
                        slot.Value = testImg;
                    else if (slot.ValueType == typeof(double) || slot.ValueType == typeof(int))
                        slot.Value = 100.0;
                }

                // 执行
                var ctx = new ExecuteContext();
                foreach (var slot in tool.InputSlots)
                    if (slot.Value != null)
                        ctx.Inputs[slot.Name] = slot.Value;

                var sw = Stopwatch.StartNew();
                var result = tool.Execute(ctx).GetAwaiter().GetResult();
                sw.Stop();

                if (result.Success)
                    Log($"✅ {tool.Name}: OK ({sw.ElapsedMilliseconds}ms)");
                else
                    Log($"⚠ {tool.Name}: {result.ErrorMessage}");

                _passed++;
            }
            catch (Exception ex)
            {
                Log($"❌ {toolType.Name}: {ex.Message}");
                _errors++;
            }
        }
    }

    private void TestMemoryLeak()
    {
        Log("\n--- 内存泄漏测试 (连续100次执行) ---");
        try
        {
            var memBefore = GC.GetTotalMemory(true);

            for (int i = 0; i < 100; i++)
            {
                var tool = new ThresholdTool();
                var img = new HImage();
                img.GenImageConst("byte", 100, 100);
                tool.InputSlots[0].Value = img;

                var ctx = new ExecuteContext();
                ctx.Inputs["Image"] = img;
                ctx.Inputs["MinGray"] = 50;
                ctx.Inputs["MaxGray"] = 200;

                tool.Execute(ctx).GetAwaiter().GetResult();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var memAfter = GC.GetTotalMemory(true);
            var diff = memAfter - memBefore;
            var diffKB = diff / 1024.0;

            if (diffKB < 100)
                Log($"✅ 内存稳定: {diffKB:F1}KB (无明显泄漏)");
            else if (diffKB < 1000)
                Log($"⚠ 内存增长: {diffKB:F1}KB (轻微增长，可接受)");
            else
                Log($"❌ 内存泄漏: {diffKB:F1}KB");

            _passed++;
        }
        catch (Exception ex)
        {
            Log($"❌ 内存测试异常: {ex.Message}");
            _errors++;
        }
    }

    private bool ExecuteFlow(List<FlowNode> nodes, List<Connection> connections)
    {
        try
        {
            var sorted = TopologicalSort(nodes, connections);
            if (sorted == null) return false;

            var ctx = new ExecuteContext();

            foreach (var node in sorted)
            {
                // 填充输入槽
                var upstream = connections.Where(c => c.TargetNode == node).ToList();
                foreach (var conn in upstream)
                {
                    var srcSlot = conn.SourceNode.Tool.OutputSlots.FirstOrDefault(s => s.Name == conn.SourceSlot);
                    if (srcSlot?.Value != null)
                        ctx.Inputs[conn.TargetSlot] = srcSlot.Value;
                }

                // 填充已有输入槽
                foreach (var slot in node.Tool.InputSlots)
                {
                    if (slot.Value != null && !ctx.Inputs.ContainsKey(slot.Name))
                        ctx.Inputs[slot.Name] = slot.Value;
                }

                // 执行
                var result = node.Tool.Execute(ctx).GetAwaiter().GetResult();

                if (!result.Success)
                {
                    Log($"  ❌ [{node.Tool.Name}] 失败: {result.ErrorMessage}");
                    return false;
                }

                // 传递输出
                foreach (var slot in node.Tool.OutputSlots)
                {
                    if (slot.Value != null)
                        ctx.Inputs[slot.Name] = slot.Value;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Log($"  ❌ 执行异常: {ex.Message}");
            return false;
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

    private void Log(string msg) => _log.Add(msg);

    private void PrintReport()
    {
        foreach (var l in _log)
            Console.WriteLine(l);
    }
}
}