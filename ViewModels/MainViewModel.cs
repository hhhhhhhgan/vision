using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using VisionFlow.Models;

namespace VisionFlow.ViewModels {

public class MainViewModel : INotifyPropertyChanged
{
    public ObservableCollection<FlowNodeViewModel> Nodes { get; } = new();
    public ObservableCollection<ConnectionViewModel> Connections { get; } = new();

    private FlowNodeViewModel? _selectedNode;
    public FlowNodeViewModel? SelectedNode { get => _selectedNode; set { _selectedNode = value; OnPropertyChanged(); } }

    private string _projectPath = string.Empty;
    public string ProjectPath { get => _projectPath; set { _projectPath = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProjectName)); } }

    public string ProjectName => string.IsNullOrEmpty(_projectPath) ? "未命名工程" : Path.GetFileNameWithoutExtension(_projectPath);

    private bool _hasUnsavedChanges;
    public bool HasUnsavedChanges { get => _hasUnsavedChanges; set { _hasUnsavedChanges = value; OnPropertyChanged(); } }

    public ICommand RunCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand SaveAsCommand { get; }
    public ICommand OpenCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand DeleteSelectedCommand { get; }
    public ICommand ConnectCommand { get; }
    public ICommand AddInputParamCommand { get; }
    public ICommand AddOutputParamCommand { get; }
    public ICommand RemoveParamCommand { get; }
    public ICommand OpenParametersCommand { get; }
    public ICommand RunSelectedStepCommand { get; }
    public ICommand RunSelectedCommand { get; }
    public ICommand RunToSelectedCommand { get; }

    // 单步运行相关
    public FlowNodeViewModel? StepStartNode { get; set; }
    public FlowNodeViewModel? StepEndNode { get; set; }

    // 流程参数配置
    public ProjectParameters Parameters { get; set; } = new();

    // 工具注册表（类型映射，用于反序列化）
    public static readonly Dictionary<string, Type> ToolTypeMap = Tools.ToolRegistry.TypeMap;

    // 工具描述符列表（按分类分组）
    public IEnumerable<IGrouping<ToolCategory, Tools.ToolDescriptor>> ToolCategories => Tools.ToolRegistry.ByCategory;

    // 扁平列表（兼容）
    public IReadOnlyList<Tools.ToolDescriptor> ToolDescriptors => Tools.ToolRegistry.All;

    public MainViewModel()
    {
        RunCommand = new RelayCommand(async _ => await RunFlow());
        SaveCommand = new RelayCommand(_ => SaveProject());
        SaveAsCommand = new RelayCommand(_ => SaveProjectAs());
        OpenCommand = new RelayCommand(_ => OpenProject());
        NewCommand = new RelayCommand(_ => NewProject());
        DeleteSelectedCommand = new RelayCommand<FlowNodeViewModel>(DeleteNode, n => n != null);
        ConnectCommand = new RelayCommand<(FlowNodeViewModel? src, string srcSlot, FlowNodeViewModel? tgt, string tgtSlot)?>(Connect);

        AddInputParamCommand = new RelayCommand(_ => AddInputParam());
        AddOutputParamCommand = new RelayCommand(_ => AddOutputParam());
        RemoveParamCommand = new RelayCommand<string>(RemoveParam);
        OpenParametersCommand = new RelayCommand(_ => OpenParametersWindow());
        RunSelectedStepCommand = new RelayCommand<FlowNodeViewModel>(async n => await RunSingleStep(n), n => n != null);
        RunSelectedCommand = new RelayCommand<FlowNodeViewModel>(async n => await RunSingleNode(n), n => n != null);
        RunToSelectedCommand = new RelayCommand<FlowNodeViewModel>(async n => await RunToNode(n), n => n != null);

        Nodes.CollectionChanged += (_, _) => HasUnsavedChanges = true;
        Connections.CollectionChanged += (_, _) => HasUnsavedChanges = true;
    }

    // ========== 节点操作 ==========

    public void AddNode(Type toolType, double x, double y)
    {
        var tool = Activator.CreateInstance(toolType) as ToolBase;
        if (tool == null) return;

        var nodeVm = new FlowNodeViewModel
        {
            Node = new FlowNode { Tool = tool, X = x, Y = y }
        };

        Nodes.Add(nodeVm);
    }

    public void DeleteNode(FlowNodeViewModel? nodeVm)
    {
        if (nodeVm == null) return;

        // 断开所有相关连线
        var toRemove = Connections.Where(c => c.Connection.SourceNode == nodeVm.Node || c.Connection.TargetNode == nodeVm.Node).ToList();
        foreach (var conn in toRemove)
        {
            // 清除槽连接状态
            if (conn.Connection.SourceNode.Tool.OutputSlots.FirstOrDefault(s => s.Name == conn.Connection.SourceSlot) != null)
                conn.Connection.SourceNode.Tool.OutputSlots.First(s => s.Name == conn.Connection.SourceSlot).IsConnected = false;
            if (conn.Connection.TargetNode.Tool.InputSlots.FirstOrDefault(s => s.Name == conn.Connection.TargetSlot) != null)
                conn.Connection.TargetNode.Tool.InputSlots.First(s => s.Name == conn.Connection.TargetSlot).IsConnected = false;
            Connections.Remove(conn);
        }

        Nodes.Remove(nodeVm);
        if (SelectedNode == nodeVm) SelectedNode = null;
        HasUnsavedChanges = true;
    }

    // ========== 连接操作 ==========

    public void Connect((FlowNodeViewModel? src, string srcSlot, FlowNodeViewModel? tgt, string tgtSlot)? args)
    {
        if (args == null) return;
        var (src, srcSlot, tgt, tgtSlot) = args.Value;
        if (src == null || tgt == null || string.IsNullOrEmpty(srcSlot) || string.IsNullOrEmpty(tgtSlot)) return;

        // 检查目标槽是否已连接
        var existing = Connections.FirstOrDefault(c => c.Connection.TargetNode == tgt.Node && c.Connection.TargetSlot == tgtSlot);
        if (existing != null)
        {
            // 断开旧的
            var oldSrc = existing.Connection.SourceNode;
            var oldSlot = oldSrc.Tool.OutputSlots.FirstOrDefault(s => s.Name == existing.Connection.SourceSlot);
            if (oldSlot != null) oldSlot.IsConnected = false;
            var oldTgtSlot = oldSrc.Tool.InputSlots.FirstOrDefault(s => s.Name == existing.Connection.TargetSlot);
            if (oldTgtSlot != null) oldTgtSlot.IsConnected = false;
            Connections.Remove(existing);
        }

        var conn = new ConnectionViewModel
        {
            Connection = new Connection
            {
                SourceNode = src.Node,
                TargetNode = tgt.Node,
                SourceSlot = srcSlot,
                TargetSlot = tgtSlot
            }
        };

        Connections.Add(conn);

        // 更新槽连接状态
        src.Node.Tool.OutputSlots.First(s => s.Name == srcSlot).IsConnected = true;
        tgt.Node.Tool.InputSlots.First(s => s.Name == tgtSlot).IsConnected = true;

        HasUnsavedChanges = true;
    }

    public void Disconnect(ConnectionViewModel? connVm)
    {
        if (connVm == null) return;
        var conn = connVm.Connection;

        var srcSlot = conn.SourceNode.Tool.OutputSlots.FirstOrDefault(s => s.Name == conn.SourceSlot);
        if (srcSlot != null) srcSlot.IsConnected = false;
        var tgtSlot = conn.TargetNode.Tool.InputSlots.FirstOrDefault(s => s.Name == conn.TargetSlot);
        if (tgtSlot != null) tgtSlot.IsConnected = false;

        Connections.Remove(connVm);
        HasUnsavedChanges = true;
    }

    // ========== 流程执行 ==========

    public async Task RunFlow()
    {
        if (Nodes.Count == 0)
        {
            MessageBox.Show("流程为空，请先添加节点", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // 重置所有节点状态
        foreach (var n in Nodes)
            n.Node.ResetStatus();

        var runner = new OptimizedFlowRunner(Nodes, Connections)
        {
            IncrementalEnabled = true,
            ParallelEnabled = true
        };

        var result = await runner.RunAsync();

        if (!result.Success)
        {
            MessageBox.Show($"执行失败:\n{result.ErrorMessage}", "执行错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // 显示执行结果
        var nodeTimes = runner.LastNodeDurations
            .Where(kv => kv.Value > 0)
            .OrderByDescending(kv => kv.Value)
            .Select(kv =>
            {
                var node = Nodes.FirstOrDefault(n => n.Node.Id == kv.Key)?.Node;
                var status = node?.ExecutionStatus == NodeExecutionStatus.Error ? "❌" : "✓";
                return $"  {status} {node?.Tool.Name}: {kv.Value:F1}ms";
            })
            .ToList();

        var errorNodes = Nodes.Where(n => n.Node.ExecutionStatus == NodeExecutionStatus.Error).ToList();
        var summary = errorNodes.Any()
            ? $"❌ 有 {errorNodes.Count} 个节点执行失败\n总耗时: {result.DurationMs:F1}ms"
            : $"✓ 流程执行完成\n总耗时: {result.DurationMs:F1}ms";

        if (nodeTimes.Any())
            summary += $"\n\n节点耗时:\n{string.Join("\n", nodeTimes.Take(10))}";

        MessageBox.Show(summary, "完成", MessageBoxButton.OK, errorNodes.Any() ? MessageBoxImage.Warning : MessageBoxImage.Information);
    }

    private List<FlowNodeViewModel>? TopologicalSort()
    {
        var result = new List<FlowNodeViewModel>();
        var visited = new HashSet<string>();
        var inStack = new HashSet<string>();

        bool DFS(FlowNode node)
        {
            if (inStack.Contains(node.Id)) return false; // 循环
            if (visited.Contains(node.Id)) return true;
            visited.Add(node.Id);
            inStack.Add(node.Id);

            foreach (var conn in Connections.Where(c => c.Connection.SourceNode == node))
            {
                if (!DFS(conn.Connection.TargetNode)) return false;
            }

            inStack.Remove(node.Id);
            var vm = Nodes.FirstOrDefault(n => n.Node == node);
            if (vm != null) result.Add(vm);
            return true;
        }

        foreach (var node in Nodes.Select(n => n.Node))
        {
            if (!visited.Contains(node.Id))
            {
                if (!DFS(node)) return null;
            }
        }

        result.Reverse();
        return result;
    }

    // ========== 单步运行 ==========

    /// <summary>
    /// 单步执行：从头执行到指定节点（含）
    /// </summary>
    public async Task RunToNode(FlowNodeViewModel? targetNode)
    {
        if (targetNode == null) return;

        // 找到从起点到目标节点的路径
        var path = GetExecutionPathTo(targetNode.Node);
        if (path == null || path.Count == 0)
        {
            MessageBox.Show("无法到达该节点（可能存在循环依赖）", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        foreach (var node in path)
        {
            if (node == targetNode.Node) break;
            await ExecuteSingleNodeVm(Nodes.First(n => n.Node == node), highlight: false);
        }
        await ExecuteSingleNodeVm(Nodes.First(n => n.Node == targetNode.Node), highlight: true);

        // 更新结果窗口
        StepEndNode = targetNode;
        StepEndNodeChanged?.Invoke();
    }

    /// <summary>
    /// 单步执行：只运行一个节点（需要手动满足输入）
    /// </summary>
    public async Task RunSingleNode(FlowNodeViewModel? nodeVm)
    {
        if (nodeVm == null) return;
        await ExecuteSingleNodeVm(nodeVm, highlight: true);
        StepEndNode = nodeVm;
        StepEndNodeChanged?.Invoke();
    }

    /// <summary>
    /// 单步执行下一个节点
    /// </summary>
    public async Task RunSingleStep(FlowNodeViewModel? currentNode)
    {
        if (currentNode == null) return;

        var sorted = TopologicalSort();
        if (sorted == null) return;

        var idx = sorted.FindIndex(n => n.Node == currentNode.Node);
        if (idx < 0 || idx >= sorted.Count - 1) return;

        var nextNode = sorted[idx + 1];
        await ExecuteSingleNodeVm(nextNode, highlight: true);

        StepStartNode = currentNode;
        StepEndNode = nextNode;
        StepEndNodeChanged?.Invoke();
    }

    private async Task ExecuteSingleNodeVm(FlowNodeViewModel nodeVm, bool highlight)
    {
        var node = nodeVm.Node;
        node.ResetStatus();
        node.ExecutionStatus = NodeExecutionStatus.Running;

        // 如果有前置节点，先把输出传给输入
        var ctx = new ExecuteContext();
        var upstream = Connections.Where(c => c.Connection.TargetNode == node).ToList();
        foreach (var conn in upstream)
        {
            var srcNode = conn.Connection.SourceNode;
            var srcSlot = conn.Connection.SourceSlot;
            var srcNodeVm = Nodes.FirstOrDefault(n => n.Node == srcNode);
            if (srcNodeVm != null)
            {
                var srcOutSlot = srcNodeVm.Node.Tool.OutputSlots.FirstOrDefault(s => s.Name == srcSlot);
                if (srcOutSlot?.Value != null)
                    ctx.Inputs[conn.Connection.TargetSlot] = srcOutSlot.Value;
            }
        }

        // 填充已有输入槽
        foreach (var slot in node.Tool.InputSlots)
        {
            if (slot.Value != null)
                ctx.Inputs[slot.Name] = slot.Value;
        }

        try
        {
            var result = await node.Tool.Execute(ctx);

            if (result.Success)
            {
                node.ExecutionStatus = NodeExecutionStatus.Success;
                foreach (var slot in node.Tool.OutputSlots)
                    slot.Value = ctx.Outputs.GetValueOrDefault(slot.Name) ?? result.OutputData.GetValueOrDefault(slot.Name);
            }
            else
            {
                node.ExecutionStatus = NodeExecutionStatus.Error;
                node.LastErrorMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            node.ExecutionStatus = NodeExecutionStatus.Error;
            node.LastErrorMessage = ex.Message;
        }

        if (highlight)
        {
            StepEndNode = nodeVm;
            StepEndNodeChanged?.Invoke();
        }
    }

    /// <summary>
    /// 获取从头到目标节点的执行路径（拓扑排序截取）
    /// </summary>
    private List<FlowNode>? GetExecutionPathTo(FlowNode targetNode)
    {
        var sorted = TopologicalSort();
        if (sorted == null) return null;

        var path = new List<FlowNode>();
        foreach (var nodeVm in sorted)
        {
            path.Add(nodeVm.Node);
            if (nodeVm.Node == targetNode) break;
        }

        return path;
    }

    // 单步结果变更通知（供窗口订阅）
    public event Action? StepEndNodeChanged;

    // ========== 工程保存/加载 ==========

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public void NewProject()
    {
        if (HasUnsavedChanges && MessageBox.Show("当前工程有未保存的更改，是否继续新建？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        Nodes.Clear();
        Connections.Clear();
        Parameters = new ProjectParameters();
        ProjectPath = string.Empty;
        HasUnsavedChanges = false;
    }

    // ========== 流程参数管理 ==========

    public void AddInputParam()
    {
        var param = new FlowParameter
        {
            Name = $"Input_{Parameters.Inputs.Count + 1}",
            DataType = "string",
            Description = "新增输入参数"
        };
        Parameters.Inputs.Add(param);
        HasUnsavedChanges = true;
    }

    public void AddOutputParam()
    {
        var param = new FlowParameter
        {
            Name = $"Output_{Parameters.Outputs.Count + 1}",
            DataType = "string",
            Description = "新增输出参数"
        };
        Parameters.Outputs.Add(param);
        HasUnsavedChanges = true;
    }

    public void RemoveParam(string? paramId)
    {
        if (string.IsNullOrEmpty(paramId)) return;

        var input = Parameters.Inputs.FirstOrDefault(p => p.Id == paramId);
        if (input != null) { Parameters.Inputs.Remove(input); HasUnsavedChanges = true; return; }

        var output = Parameters.Outputs.FirstOrDefault(p => p.Id == paramId);
        if (output != null) { Parameters.Outputs.Remove(output); HasUnsavedChanges = true; }
    }

    private Views.ToolDialogs.ParametersDialog? _parametersWindow;

    public void OpenParametersWindow()
    {
        if (_parametersWindow == null || !_parametersWindow.IsLoaded)
        {
            _parametersWindow = new Views.ToolDialogs.ParametersDialog(this);
            _parametersWindow.Owner = System.Windows.Application.Current.MainWindow;
        }
        _parametersWindow.Show();
        _parametersWindow.Activate();
    }

    public void CloseParametersWindow()
    {
        _parametersWindow?.Close();
        _parametersWindow = null;
    }

    public void SaveProject()
    {
        if (string.IsNullOrEmpty(ProjectPath))
        {
            SaveProjectAs();
            return;
        }
        SaveToFile(ProjectPath);
    }

    public void SaveProjectAs()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "VisionFlow工程|*.vflow",
            DefaultExt = ".vflow",
            FileName = ProjectName
        };

        if (dialog.ShowDialog() == true)
        {
            ProjectPath = dialog.FileName;
            SaveToFile(dialog.FileName);
        }
    }

    private void SaveToFile(string path)
    {
        var project = new Project
        {
            Name = ProjectName,
            ModifiedAt = DateTime.Now,
            Nodes = Nodes.Select(n => new NodeData
            {
                Id = n.Node.Id,
                ToolType = n.Node.Tool.GetType().Name,
                X = n.Node.X,
                Y = n.Node.Y,
                Parameters = SerializeToolProperties(n.Node.Tool)
            }).ToList(),
            Connections = Connections.Select(c => new ConnectionData
            {
                Id = c.Connection.Id,
                SourceNodeId = c.Connection.SourceNode.Id,
                SourceSlot = c.Connection.SourceSlot,
                TargetNodeId = c.Connection.TargetNode.Id,
                TargetSlot = c.Connection.TargetSlot
            }).ToList(),
            Parameters = Parameters
        };

        var json = JsonSerializer.Serialize(project, JsonOptions);
        File.WriteAllText(path, json);
        HasUnsavedChanges = false;
    }

    public void OpenProject()
    {
        if (HasUnsavedChanges && MessageBox.Show("当前工程有未保存的更改，是否继续打开？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "VisionFlow工程|*.vflow|All文件|*.*",
            DefaultExt = ".vflow"
        };

        if (dialog.ShowDialog() == true)
            LoadFromFile(dialog.FileName);
    }

    public void LoadFromFile(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            var project = JsonSerializer.Deserialize<Project>(json, JsonOptions);
            if (project == null) return;

            Nodes.Clear();
            Connections.Clear();

            var nodeMap = new Dictionary<string, FlowNode>();

            foreach (var nodeData in project.Nodes)
            {
                var typeName = nodeData.ToolType.Split(',').Last(); // 去掉程序集名
                if (!ToolTypeMap.TryGetValue(typeName, out var toolType))
                    if (!ToolTypeMap.TryGetValue(nodeData.ToolType, out toolType)) // 尝试直接匹配
                        continue;

                var tool = Activator.CreateInstance(toolType) as ToolBase;
                if (tool == null) continue;

                DeserializeToolProperties(tool, nodeData.Parameters);

                var nodeVm = new FlowNodeViewModel
                {
                    Node = new FlowNode { Id = nodeData.Id, Tool = tool, X = nodeData.X, Y = nodeData.Y }
                };

                Nodes.Add(nodeVm);
                nodeMap[nodeData.Id] = nodeVm.Node;
            }

            foreach (var connData in project.Connections)
            {
                if (!nodeMap.TryGetValue(connData.SourceNodeId, out var srcNode)) continue;
                if (!nodeMap.TryGetValue(connData.TargetNodeId, out var tgtNode)) continue;

                // 更新槽连接状态
                var srcSlot = srcNode.Tool.OutputSlots.FirstOrDefault(s => s.Name == connData.SourceSlot);
                var tgtSlot = tgtNode.Tool.InputSlots.FirstOrDefault(s => s.Name == connData.TargetSlot);
                if (srcSlot != null) srcSlot.IsConnected = true;
                if (tgtSlot != null) tgtSlot.IsConnected = true;

                Connections.Add(new ConnectionViewModel
                {
                    Connection = new Connection
                    {
                        Id = connData.Id,
                        SourceNode = srcNode,
                        TargetNode = tgtNode,
                        SourceSlot = connData.SourceSlot,
                        TargetSlot = connData.TargetSlot
                    }
                });
            }

            Parameters = project.Parameters ?? new ProjectParameters();

            ProjectPath = path;
            HasUnsavedChanges = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载工程失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ========== 属性序列化工具 ==========

    private Dictionary<string, object?> SerializeToolProperties(ToolBase tool)
    {
        var props = new Dictionary<string, object?>();
        var type = tool.GetType();

        // 公共可写属性（排除基类属性）
        foreach (var prop in type.GetProperties().Where(p => p.CanRead && p.Name != nameof(ToolBase.Name) && p.Name != nameof(ToolBase.Description)))
        {
            var val = prop.GetValue(tool);
            if (val == null || val is string s && string.IsNullOrEmpty(s)) continue;
            props[prop.Name] = val;
        }

        return props;
    }

    private void DeserializeToolProperties(ToolBase tool, Dictionary<string, object?> properties)
    {
        var type = tool.GetType();

        foreach (var kvp in properties)
        {
            var prop = type.GetProperty(kvp.Key);
            if (prop == null || !prop.CanWrite) continue;

            try
            {
                if (kvp.Value == null) continue;
                object? converted;

                if (prop.PropertyType == typeof(double))
                    converted = Convert.ToDouble(kvp.Value);
                else if (prop.PropertyType == typeof(int))
                    converted = Convert.ToInt32(kvp.Value);
                else if (prop.PropertyType == typeof(bool))
                    converted = Convert.ToBoolean(kvp.Value);
                else if (prop.PropertyType == typeof(string))
                    converted = kvp.Value.ToString();
                else
                    converted = kvp.Value;

                prop.SetValue(tool, converted);
            }
            catch { /* 忽略转换失败 */ }
        }
    }

    public void MarkChanged() => HasUnsavedChanges = true;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class FlowNodeViewModel : INotifyPropertyChanged
{
    public FlowNode Node { get; set; } = null!;

    private bool _isSelected;
    public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }

    public double X { get => Node.X; set { Node.X = value; OnPropertyChanged(); } }
    public double Y { get => Node.Y; set { Node.Y = value; OnPropertyChanged(); } }

    public double Height => Node.Height;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class ConnectionViewModel : INotifyPropertyChanged
{
    public Connection Connection { get; set; } = null!;

    // 连线起止点（由 View 计算）
    private double _x1, _y1, _x2, _y2;
    public double X1 { get => _x1; set { _x1 = value; OnPropertyChanged(); } }
    public double Y1 { get => _y1; set { _y1 = value; OnPropertyChanged(); } }
    public double X2 { get => _x2; set { _x2 = value; OnPropertyChanged(); } }
    public double Y2 { get => _y2; set { _y2 = value; OnPropertyChanged(); } }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;
    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null) { _execute = execute; _canExecute = canExecute; }
    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _execute(parameter);
    public event EventHandler? CanExecuteChanged { add => CommandManager.RequerySuggested += value; remove => CommandManager.RequerySuggested -= value; }
}

public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;
    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null) { _execute = execute; _canExecute = canExecute; }
    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;
    public void Execute(object? parameter) => _execute((T?)parameter);
    public event EventHandler? CanExecuteChanged { add => CommandManager.RequerySuggested += value; remove => CommandManager.RequerySuggested -= value; }
}
}