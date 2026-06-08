using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using VisionFlow.Models;
using VisionFlow.Tools;

namespace VisionFlow;

/// <summary>
/// 压力测试 / 连续运行测试
/// </summary>
public class StressTestRunner
{
    private readonly List<FlowNode> _nodes;
    private readonly List<Connection> _connections;
    private int _totalRuns;
    private int _successCount;
    private int _failCount;
    private int _errorCount;
    private readonly List<ErrorRecord> _errors = new();
    private CancellationTokenSource? _cts;

    public int TotalRuns => _totalRuns;
    public int SuccessCount => _successCount;
    public int FailCount => _failCount;
    public int ErrorCount => _errorCount;
    public IReadOnlyList<ErrorRecord> Errors => _errors.AsReadOnly();
    public bool IsRunning { get; private set; }

    public event Action<int, int, int>? ProgressChanged; // success, fail, total
    public event Action<ErrorRecord>? ErrorOccurred;

    public StressTestRunner(List<FlowNode> nodes, List<Connection> connections)
    {
        _nodes = nodes;
        _connections = connections;
    }

    public async Task<StressTestResult> RunAsync(int count, int delayMs = 0)
    {
        if (IsRunning) return new StressTestResult { Success = false, Message = "测试已在运行" };

        _totalRuns = 0;
        _successCount = 0;
        _failCount = 0;
        _errorCount = 0;
        _errors.Clear();
        _cts = new CancellationTokenSource();
        IsRunning = true;

        var sw = Stopwatch.StartNew();

        try
        {
            for (int i = 0; i < count; i++)
            {
                if (_cts.Token.IsCancellationRequested) break;

                // 重置所有节点状态
                foreach (var n in _nodes) n.ResetStatus();

                // 执行一次
                var result = await ExecuteOnceAsync(i + 1);

                _totalRuns++;

                if (result.Success)
                {
                    _successCount++;
                }
                else if (result.IsError)
                {
                    _errorCount++;
                }
                else
                {
                    _failCount++;
                }

                ProgressChanged?.Invoke(_successCount, _failCount + _errorCount, _totalRuns);

                // 延迟（如果设置了）
                if (delayMs > 0 && i < count - 1)
                    await Task.Delay(delayMs, _cts.Token);

                // 每100次报告一次
                if (_totalRuns % 100 == 0)
                    Debug.WriteLine($"[StressTest] 进度: {_totalRuns}/{count} 成功:{_successCount} 失败:{_failCount} 错误:{_errorCount}");
            }
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("[StressTest] 用户取消");
        }
        finally
        {
            IsRunning = false;
            _cts.Dispose();
            _cts = null;
        }

        sw.Stop();

        return new StressTestResult
        {
            Success = _failCount == 0 && _errorCount == 0,
            TotalRuns = _totalRuns,
            SuccessCount = _successCount,
            FailCount = _failCount,
            ErrorCount = _errorCount,
            TotalDurationMs = sw.ElapsedMilliseconds,
            AvgDurationMs = _totalRuns > 0 ? sw.ElapsedMilliseconds / (double)_totalRuns : 0,
            Errors = _errors.ToList()
        };
    }

    public void Cancel() => _cts?.Cancel();

    private async Task<ExecuteOnceResult> ExecuteOnceAsync(int runIndex)
    {
        var sw = Stopwatch.StartNew();
        var ctx = new ExecuteContext();

        // 拓扑排序
        var sorted = TopologicalSort();
        if (sorted == null)
            return new ExecuteOnceResult { Success = false, IsError = true, ErrorMessage = "拓扑排序失败（循环依赖）" };

        try
        {
            foreach (var node in sorted)
            {
                // 设置运行状态
                node.ExecutionStatus = NodeExecutionStatus.Running;
                node.LastErrorMessage = null;

                // 填充输入槽
                foreach (var slot in node.Tool.InputSlots)
                {
                    if (!slot.IsOptional || ctx.Inputs.ContainsKey(slot.Name))
                    {
                        slot.Value = ctx.Inputs.GetValueOrDefault(slot.Name);
                    }
                }

                // 执行
                Result execResult;
                try
                {
                    execResult = await node.Tool.Execute(ctx);
                }
                catch (Exception ex)
                {
                    node.ExecutionStatus = NodeExecutionStatus.Error;
                    node.LastErrorMessage = ex.Message;

                    var record = new ErrorRecord
                    {
                        RunIndex = runIndex,
                        NodeName = node.Tool.Name,
                        NodeId = node.Id,
                        ErrorType = "Exception",
                        ErrorMessage = ex.Message,
                        StackTrace = ex.StackTrace ?? ""
                    };
                    _errors.Add(record);
                    ErrorOccurred?.Invoke(record);

                    return new ExecuteOnceResult { Success = false, IsError = true, ErrorMessage = ex.Message };
                }

                // 处理输出
                if (execResult.Success)
                {
                    node.ExecutionStatus = NodeExecutionStatus.Success;

                    // 从上下文中取出该节点的所有输出，存入共享上下文
                    foreach (var slot in node.Tool.OutputSlots)
                    {
                        if (slot.Value != null)
                            ctx.Inputs[slot.Name] = slot.Value;
                    }
                }
                else
                {
                    node.ExecutionStatus = NodeExecutionStatus.Error;
                    node.LastErrorMessage = execResult.ErrorMessage;

                    var record = new ErrorRecord
                    {
                        RunIndex = runIndex,
                        NodeName = node.Tool.Name,
                        NodeId = node.Id,
                        ErrorType = "Fail",
                        ErrorMessage = execResult.ErrorMessage ?? "未知错误"
                    };
                    _errors.Add(record);
                    ErrorOccurred?.Invoke(record);

                    return new ExecuteOnceResult { Success = false, IsError = false, ErrorMessage = execResult.ErrorMessage };
                }
            }

            sw.Stop();
            return new ExecuteOnceResult { Success = true, DurationMs = sw.Elapsed.TotalMilliseconds };
        }
        catch (Exception ex)
        {
            sw.Stop();
            var record = new ErrorRecord
            {
                RunIndex = runIndex,
                NodeName = "Flow",
                ErrorType = "Fatal",
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace ?? ""
            };
            _errors.Add(record);
            ErrorOccurred?.Invoke(record);

            return new ExecuteOnceResult { Success = false, IsError = true, ErrorMessage = ex.Message };
        }
    }

    private List<FlowNode>? TopologicalSort()
    {
        var sorted = new List<FlowNode>();
        var visited = new Dictionary<string, int>(); // 0=未访问, 1=访问中, 2=已完成
        var temp = new List<FlowNode>();

        bool DFS(FlowNode node)
        {
            if (visited.ContainsKey(node.Id) && visited[node.Id] == 1) return false; // 循环
            if (visited.ContainsKey(node.Id) && visited[node.Id] == 2) return true;

            visited[node.Id] = 1;
            temp.Add(node);

            // 找下游节点
            var downstream = _connections.Where(c => c.SourceNode == node).Select(c => c.TargetNode).ToList();
            foreach (var dep in downstream)
            {
                if (!DFS(dep)) return false;
            }

            temp.Remove(node);
            visited[node.Id] = 2;
            sorted.Insert(0, node);
            return true;
        }

        foreach (var node in _nodes)
        {
            if (!visited.ContainsKey(node.Id))
            {
                if (!DFS(node)) return null; // 循环依赖
            }
        }

        return sorted;
    }
}

public class ErrorRecord
{
    public int RunIndex { get; set; }
    public string NodeName { get; set; } = "";
    public string NodeId { get; set; } = "";
    public string ErrorType { get; set; } = ""; // Exception / Fail / Fatal
    public string ErrorMessage { get; set; } = "";
    public string StackTrace { get; set; } = "";
    public DateTime Time { get; set; } = DateTime.Now;

    public override string ToString() => $"[{RunIndex}] {NodeName} [{ErrorType}]: {ErrorMessage}";
}

public class ExecuteOnceResult
{
    public bool Success { get; set; }
    public bool IsError { get; set; } // true=崩溃级别错误
    public string? ErrorMessage { get; set; }
    public double DurationMs { get; set; }
}

public class StressTestResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int TotalRuns { get; set; }
    public int SuccessCount { get; set; }
    public int FailCount { get; set; }
    public int ErrorCount { get; set; }
    public double TotalDurationMs { get; set; }
    public double AvgDurationMs { get; set; }
    public List<ErrorRecord> Errors { get; set; } = new();
}