using System.Collections.Concurrent;
using System.Diagnostics;
using HalconDotNet;
using VisionFlow.Models;

namespace VisionFlow;

/// <summary>
/// 优化过的流程执行引擎
/// 支持：并行执行、增量执行（输入未变则跳过）、执行监控
/// </summary>
public class OptimizedFlowRunner
{
    private readonly List<FlowNode> _nodes;
    private readonly List<Connection> _connections;
    private readonly ConcurrentDictionary<string, NodeExecutionState> _nodeStates = new();
    private bool _disposed;

    /// <summary>启用增量执行（输入未变则跳过）</summary>
    public bool IncrementalEnabled { get; set; } = true;

    /// <summary>允许并行执行（无依赖的节点同时跑）</summary>
    public bool ParallelEnabled { get; set; } = true;

    /// <summary>最大并行度</summary>
    public int MaxParallelism { get; set; } = Environment.ProcessorCount;

    /// <summary>最后执行的节点耗时记录（节点ID → 耗时ms）</summary>
    public IReadOnlyDictionary<string, double> LastNodeDurations => _nodeDurations;

    private readonly ConcurrentDictionary<string, double> _nodeDurations = new();

    public OptimizedFlowRunner(IEnumerable<FlowNodeViewModel> nodes, IEnumerable<ConnectionViewModel> connections)
    {
        _nodes = nodes.Select(n => n.Node).ToList();
        _connections = connections.Select(c => c.Connection).ToList();

        foreach (var node in _nodes)
            _nodeStates[node.Id] = new NodeExecutionState();
    }

    /// <summary>
    /// 执行流程（并行优化版）
    /// </summary>
    public async Task<ExecutionResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var result = new ExecutionResult();
        var sw = Stopwatch.StartNew();

        // 1. 拓扑分层
        var layers = TopologicalLayers();
        if (layers == null)
            return result.Fail("流程存在循环依赖");

        // 2. 清空所有槽值
        ClearAllSlots();

        // 3. 根据连线填充输入槽
        PropagateInputs();

        // 4. 按层执行（层内可并行）
        foreach (var layer in layers)
        {
            if (cancellationToken.IsCancellationRequested)
                return result.Fail("用户取消");

            if (ParallelEnabled && layer.Count > 1)
            {
                // 层内并行
                var tasks = layer.Select(node => ExecuteNodeAsync(node, cancellationToken)).ToList();
                await Task.WhenAll(tasks);
            }
            else
            {
                // 顺序执行
                foreach (var node in layer)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return result.Fail("用户取消");

                    var execResult = await ExecuteNodeAsync(node, cancellationToken);
                    if (!execResult.Success)
                        return result.Fail($"[{node.Tool.Name}] {execResult.ErrorMessage}");
                }
            }
        }

        sw.Stop();
        result.DurationMs = sw.Elapsed.TotalMilliseconds;
        result.Success = true;
        return result;
    }

    /// <summary>
    /// 执行单个节点（带增量判断）
    /// </summary>
    private async Task<ExecutionResult> ExecuteNodeAsync(FlowNode node, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var state = _nodeStates[node.Id];

        // 设置运行中状态
        node.ExecutionStatus = NodeExecutionStatus.Running;
        node.LastErrorMessage = null;

        // --- 增量检查：输入未变则跳过 ---
        if (IncrementalEnabled && state.HasExecuted)
        {
            var inputsChanged = false;
            foreach (var slot in node.Tool.InputSlots)
            {
                if (!Equals(slot.Value, state.LastInputValues.GetValueOrDefault(slot.Name)))
                {
                    inputsChanged = true;
                    break;
                }
            }

            if (!inputsChanged)
            {
                sw.Stop();
                _nodeDurations[node.Id] = 0; // 跳过标记为0
                node.ExecutionStatus = NodeExecutionStatus.Success;
                return ExecutionResult.Ok();
            }
        }

        // --- 执行 ---
        var ctx = new ExecuteContext();
        foreach (var slot in node.Tool.InputSlots)
            ctx.Inputs[slot.Name] = slot.Value;

        Result execResult;
        try
        {
            execResult = await node.Tool.Execute(ctx);
        }
        catch (Exception ex)
        {
            // 工具内部崩溃，设置错误状态
            node.ExecutionStatus = NodeExecutionStatus.Error;
            node.LastErrorMessage = ex.Message;
            sw.Stop();
            _nodeDurations[node.Id] = sw.Elapsed.TotalMilliseconds;
            return ExecutionResult.Fail($"[{node.Tool.Name}] 异常: {ex.Message}");
        }

        sw.Stop();
        var elapsed = sw.Elapsed.TotalMilliseconds;
        _nodeDurations[node.Id] = elapsed;

        if (execResult.Success)
        {
            // 缓存输出
            foreach (var slot in node.Tool.OutputSlots)
                slot.Value = ctx.Outputs.GetValueOrDefault(slot.Name) ?? execResult.OutputData.GetValueOrDefault(slot.Name);

            // 更新增量状态
            state.HasExecuted = true;
            state.LastInputValues = node.Tool.InputSlots.ToDictionary(s => s.Name, s => s.Value);

            node.ExecutionStatus = NodeExecutionStatus.Success;
        }
        else
        {
            node.ExecutionStatus = NodeExecutionStatus.Error;
            node.LastErrorMessage = execResult.ErrorMessage;
        }

        if (!execResult.Success)
            return ExecutionResult.Fail(execResult.ErrorMessage ?? "未知错误");

        return ExecutionResult.Ok();
    }

    /// <summary>
    /// 拓扑分层（同一层内节点可并行）
    /// </summary>
    private List<List<FlowNode>>? TopologicalLayers()
    {
        var inDegree = _nodes.ToDictionary(n => n.Id, _ => 0);
        foreach (var conn in _connections)
        {
            if (inDegree.ContainsKey(conn.TargetNode.Id))
                inDegree[conn.TargetNode.Id]++;
        }

        var layers = new List<List<FlowNode>>();
        var remaining = new HashSet<FlowNode>(_nodes);

        while (remaining.Count > 0)
        {
            // 找入度为0的节点（无上游依赖）
            var layer = remaining.Where(n => inDegree[n.Id] == 0).ToList();
            if (layer.Count == 0) return null; // 循环依赖

            layers.Add(layer);
            foreach (var node in layer)
            {
                remaining.Remove(node);
                // 减少下游节点的入度
                foreach (var conn in _connections.Where(c => c.SourceNode == node))
                {
                    if (inDegree.ContainsKey(conn.TargetNode.Id))
                        inDegree[conn.TargetNode.Id]--;
                }
            }
        }

        return layers;
    }

    private void ClearAllSlots()
    {
        foreach (var node in _nodes)
        {
            foreach (var s in node.Tool.InputSlots) s.Value = null;
            foreach (var s in node.Tool.OutputSlots) s.Value = null;
        }
    }

    private void PropagateInputs()
    {
        foreach (var conn in _connections)
        {
            var srcSlot = conn.SourceNode.Tool.OutputSlots.FirstOrDefault(s => s.Name == conn.SourceSlot);
            var tgtSlot = conn.TargetNode.Tool.InputSlots.FirstOrDefault(s => s.Name == conn.TargetSlot);
            if (srcSlot != null && tgtSlot != null)
                tgtSlot.Value = srcSlot.Value;
        }
    }

    /// <summary>
    /// 重置增量状态（下次执行会跳过缓存）
    /// </summary>
    public void ResetIncrementalState()
    {
        foreach (var state in _nodeStates.Values)
            state.HasExecuted = false;
    }

    /// <summary>
    /// 获取节点执行统计
    /// </summary>
    public NodeStats GetNodeStats(string nodeId)
    {
        if (_nodeStates.TryGetValue(nodeId, out var state))
            return new NodeStats { HasExecuted = state.HasExecuted, LastDurationMs = _nodeDurations.GetValueOrDefault(nodeId, 0) };
        return new NodeStats();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}

public class NodeExecutionState
{
    public bool HasExecuted { get; set; }
    public Dictionary<string, object?> LastInputValues { get; set; } = new();
}

public class ExecutionResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public double DurationMs { get; set; }

    public static ExecutionResult Ok() => new() { Success = true };
    public static ExecutionResult Fail(string msg) => new() { Success = false, ErrorMessage = msg };
}

public class NodeStats
{
    public bool HasExecuted { get; set; }
    public double LastDurationMs { get; set; }
}