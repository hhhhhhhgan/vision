using System.Windows.Media;

namespace VisionFlow.Models {

/// <summary>
/// 节点执行状态
/// </summary>
public enum NodeExecutionStatus
{
    Idle,       // 空闲/未执行
    Running,    // 执行中
    Success,    // 执行成功
    Error       // 执行失败
}

/// <summary>
/// 带执行状态的流程节点
/// </summary>
public class FlowNode : INotifyPropertyChanged
{
    private double _x;
    private double _y;
    private bool _isSelected;
    private NodeExecutionStatus _executionStatus = NodeExecutionStatus.Idle;
    private string? _lastErrorMessage;

    public string Id { get; set; } = Guid.NewGuid().ToString();
    public ToolBase Tool { get; set; } = null!;

    public double X { get => _x; set { _x = value; OnPropertyChanged(); } }
    public double Y { get => _y; set { _y = value; OnPropertyChanged(); } }
    public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }

    public NodeExecutionStatus ExecutionStatus
    {
        get => _executionStatus;
        set { _executionStatus = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusColor)); }
    }

    public string? LastErrorMessage
    {
        get => _lastErrorMessage;
        set { _lastErrorMessage = value; OnPropertyChanged(); }
    }

    /// <summary>状态对应的颜色</summary>
    public SolidColorBrush StatusColor => ExecutionStatus switch
    {
        NodeExecutionStatus.Running => new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6)),   // 蓝色=运行中
        NodeExecutionStatus.Success => new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)), // 绿色=成功
        NodeExecutionStatus.Error => new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)),   // 红色=失败
        _ => new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6))                           // 默认蓝色
    };

    public const double NODE_WIDTH = 160;
    public double Height => 70 + Math.Max(Tool.InputSlots.Count, Tool.OutputSlots.Count) * 20;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void ResetStatus()
    {
        ExecutionStatus = NodeExecutionStatus.Idle;
        LastErrorMessage = null;
    }
}

/// <summary>
/// 连接
/// </summary>
public class Connection : INotifyPropertyChanged
{
    private string _id = Guid.NewGuid().ToString();
    public string Id { get => _id; set { _id = value; OnPropertyChanged(); } }

    public FlowNode SourceNode { get; set; } = null!;
    public FlowNode TargetNode { get; set; } = null!;
    public string SourceSlot { get; set; } = string.Empty;
    public string TargetSlot { get; set; } = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
}