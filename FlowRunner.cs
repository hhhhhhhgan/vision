using System;
using System.IO;
using System.Text.Json;
using HalconDotNet;
using VisionFlow.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace VisionFlow {

/// <summary>
/// 流程运行器 — 外部调用入口
/// 加载工程文件 → 设置输入参数 → 执行流程 → 获取输出结果
/// </summary>
public class FlowRunner : IDisposable
{
    private readonly MainViewModel _viewModel;
    private readonly string _projectPath;
    private bool _disposed;

    public string ProjectName { get; }
    public IReadOnlyList<FlowParameter> Inputs => _viewModel.Parameters.Inputs;
    public IReadOnlyList<FlowParameter> Outputs => _viewModel.Parameters.Outputs;

    /// <summary>执行后的输出值</summary>
    public Dictionary<string, object?> OutputValues { get; private set; } = new();

    /// <summary>执行是否成功</summary>
    public bool LastRunSuccess { get; private set; }

    /// <summary>最后一条错误信息</summary>
    public string? LastErrorMessage { get; private set; }

    /// <summary>最后执行耗时（毫秒）</summary>
    public double LastRunDurationMs { get; private set; }

    private FlowRunner(MainViewModel viewModel, string projectPath)
    {
        _viewModel = viewModel;
        _projectPath = projectPath;
        ProjectName = viewModel.ProjectName;
    }

    /// <summary>
    /// 从工程文件创建运行器
    /// </summary>
    public static FlowRunner? FromFile(string vflowPath)
    {
        if (!File.Exists(vflowPath))
            return null;

        var vm = new MainViewModel();
        vm.LoadFromFile(vflowPath);
        return new FlowRunner(vm, vflowPath);
    }

    /// <summary>
    /// 从工程文件创建运行器（异步）
    /// </summary>
    public static async Task<FlowRunner?> FromFileAsync(string vflowPath)
    {
        return await Task.Run(() => FromFile(vflowPath));
    }

    /// <summary>
    /// 设置输入参数值
    /// </summary>
    /// <param name="name">参数名称</param>
    /// <param name="value">参数值（string/double/HImage/HRegion）</param>
    public void SetInput(string name, object? value)
    {
        var param = _viewModel.Parameters.Inputs.FirstOrDefault(p => p.Name == name);
        if (param == null) return;

        // 找到绑定节点，设置其对应槽的值
        if (!string.IsNullOrEmpty(param.LinkedNodeId) && !string.IsNullOrEmpty(param.LinkedSlotName))
        {
            var node = _viewModel.Nodes.FirstOrDefault(n => n.Node.Id == param.LinkedNodeId)?.Node;
            if (node != null)
            {
                var slot = node.Tool.InputSlots.FirstOrDefault(s => s.Name == param.LinkedSlotName);
                if (slot != null)
                    slot.Value = ConvertValue(value, param.DataType);
            }
        }
    }

    /// <summary>
    /// 批量设置输入参数
    /// </summary>
    public void SetInputs(Dictionary<string, object?> inputs)
    {
        foreach (var kvp in inputs)
            SetInput(kvp.Key, kvp.Value);
    }

    /// <summary>
    /// 执行流程
    /// </summary>
    public async Task<bool> RunAsync()
    {
        return await RunAsync(CancellationToken.None);
    }

    /// <summary>
    /// 执行流程（可取消）
    /// </summary>
    public async Task<bool> RunAsync(CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        LastRunSuccess = false;
        LastErrorMessage = null;

        try
        {
            // 拓扑排序
            var sorted = TopologicalSort();
            if (sorted == null)
            {
                LastErrorMessage = "流程存在循环依赖";
                LastRunSuccess = false;
                return false;
            }

            // 清空所有槽值
            foreach (var node in _viewModel.Nodes)
            {
                foreach (var s in node.Node.Tool.InputSlots) s.Value = null;
                foreach (var s in node.Node.Tool.OutputSlots) s.Value = null;
            }

            // 根据连线传递输入槽的值
            foreach (var connVm in _viewModel.Connections)
            {
                var conn = connVm.Connection;
                var srcSlot = conn.SourceNode.Tool.OutputSlots.FirstOrDefault(s => s.Name == conn.SourceSlot);
                var tgtSlot = conn.TargetNode.Tool.InputSlots.FirstOrDefault(s => s.Name == conn.TargetSlot);
                if (srcSlot != null && tgtSlot != null)
                    tgtSlot.Value = srcSlot.Value;
            }

            // 执行
            foreach (var nodeVm in sorted)
            {
                if (cancellationToken.IsCancellationRequested) return false;

                var node = nodeVm.Node;

                // 填充执行上下文
                var ctx = new ExecuteContext();
                foreach (var slot in node.Tool.InputSlots)
                    ctx.Inputs[slot.Name] = slot.Value;

                var result = await node.Tool.Execute(ctx);

                if (!result.Success)
                {
                    LastErrorMessage = $"[{node.Tool.Name}] {result.ErrorMessage}";
                    LastRunSuccess = false;
                    return false;
                }

                // 将输出写入槽
                foreach (var kvp in result.OutputData)
                {
                    var slot = node.Tool.OutputSlots.FirstOrDefault(s => s.Name == kvp.Key);
                    if (slot != null) slot.Value = kvp.Value;
                }
            }

            // 收集输出参数
            OutputValues.Clear();
            foreach (var param in _viewModel.Parameters.Outputs)
            {
                if (!string.IsNullOrEmpty(param.LinkedNodeId) && !string.IsNullOrEmpty(param.LinkedSlotName))
                {
                    var node = _viewModel.Nodes.FirstOrDefault(n => n.Node.Id == param.LinkedNodeId)?.Node;
                    if (node != null)
                    {
                        var slot = node.Tool.OutputSlots.FirstOrDefault(s => s.Name == param.LinkedSlotName);
                        if (slot != null)
                            OutputValues[param.Name] = slot.Value;
                    }
                }
            }

            LastRunSuccess = true;
            return true;
        }
        catch (Exception ex)
        {
            LastErrorMessage = ex.Message;
            LastRunSuccess = false;
            return false;
        }
        finally
        {
            sw.Stop();
            LastRunDurationMs = sw.Elapsed.TotalMilliseconds;
        }
    }

    /// <summary>
    /// 获取输出参数值
    /// </summary>
    public T? GetOutput<T>(string name)
    {
        if (OutputValues.TryGetValue(name, out var val))
        {
            if (val is T typed) return typed;
            try { return (T)Convert.ChangeType(val, typeof(T)); } catch { }
        }
        return default;
    }

    /// <summary>
    /// 获取所有输出（字典）
    /// </summary>
    public Dictionary<string, object?> GetAllOutputs() => new(OutputValues);

    private List<ViewModels.FlowNodeViewModel>? TopologicalSort()
    {
        var result = new List<ViewModels.FlowNodeViewModel>();
        var visited = new HashSet<string>();
        var inStack = new HashSet<string>();

        bool DFS(FlowNode node)
        {
            if (inStack.Contains(node.Id)) return false;
            if (visited.Contains(node.Id)) return true;
            visited.Add(node.Id);
            inStack.Add(node.Id);

            foreach (var conn in _viewModel.Connections.Where(c => c.Connection.SourceNode == node))
            {
                if (!DFS(conn.Connection.TargetNode)) return false;
            }

            inStack.Remove(node.Id);
            var vm = _viewModel.Nodes.FirstOrDefault(n => n.Node == node);
            if (vm != null) result.Add(vm);
            return true;
        }

        foreach (var node in _viewModel.Nodes.Select(n => n.Node))
        {
            if (!visited.Contains(node.Id))
            {
                if (!DFS(node)) return null;
            }
        }

        result.Reverse();
        return result;
    }

    private static object? ConvertValue(object? value, string dataType)
    {
        if (value == null) return null;

        return dataType switch
        {
            "image" when value is string path => new HImage(path),
            "number" => Convert.ToDouble(value),
            "string" => value.ToString(),
            _ => value
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // 释放 HALCON 资源（如果需要）
    }
}
}