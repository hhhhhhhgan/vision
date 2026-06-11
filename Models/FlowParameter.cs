using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VisionFlow.Models {

/// <summary>
/// 流程参数定义（输入或输出）
/// </summary>
public class FlowParameter : INotifyPropertyChanged
{
    private string _id = Guid.NewGuid().ToString();
    public string Id { get => _id; set { _id = value; OnPropertyChanged(); } }

    private string _name = string.Empty;
    public string Name { get => _name; set { _name = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); } }

    private string _description = string.Empty;
    public string Description { get => _description; set { _description = value; OnPropertyChanged(); } }

    private string _dataType = "string";
    public string DataType { get => _dataType; set { _dataType = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); } }

    private string? _linkedNodeId;
    public string? LinkedNodeId { get => _linkedNodeId; set { _linkedNodeId = value; OnPropertyChanged(); } }

    private string? _linkedSlotName;
    public string? LinkedSlotName { get => _linkedSlotName; set { _linkedSlotName = value; OnPropertyChanged(); } }

    private string? _defaultValue;
    public string? DefaultValue { get => _defaultValue; set { _defaultValue = value; OnPropertyChanged(); } }

    /// <summary>是否有效（名称非空）</summary>
    public bool IsValid => !string.IsNullOrWhiteSpace(Name);

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// 工程参数配置
/// </summary>
public class ProjectParameters : INotifyPropertyChanged
{
    private List<FlowParameter> _inputs = new();
    public List<FlowParameter> Inputs { get => _inputs; set { _inputs = value; OnPropertyChanged(); } }

    private List<FlowParameter> _outputs = new();
    public List<FlowParameter> Outputs { get => _outputs; set { _outputs = value; OnPropertyChanged(); } }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

}