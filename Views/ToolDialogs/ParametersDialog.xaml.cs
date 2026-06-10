using System.Globalization;
using System.Windows;
using System.Windows.Data;
using VisionFlow.Models;
using VisionFlow.ViewModels;

namespace VisionFlow.Views.ToolDialogs {

public partial class ParametersDialog : Window
{
    private readonly MainViewModel _viewModel;

    public ParametersDialog(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void AddInput_Click(object sender, RoutedEventArgs e)
    {
        var param = new FlowParameter
        {
            Name = $"Input_{_viewModel.Parameters.Inputs.Count + 1}",
            DataType = "string",
            Description = ""
        };
        _viewModel.Parameters.Inputs.Add(param);
        _viewModel.HasUnsavedChanges = true;
    }

    private void AddOutput_Click(object sender, RoutedEventArgs e)
    {
        var param = new FlowParameter
        {
            Name = $"Output_{_viewModel.Parameters.Outputs.Count + 1}",
            DataType = "number",
            Description = ""
        };
        _viewModel.Parameters.Outputs.Add(param);
        _viewModel.HasUnsavedChanges = true;
    }

    private void RemoveInput_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string id)
            _viewModel.RemoveParam(id);
    }

    private void RemoveOutput_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string id)
            _viewModel.RemoveParam(id);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}

/// <summary>
/// 将节点ID转换为该节点的槽列表（输入或输出）
/// </summary>
public class NodeSlotsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string nodeId) return null;
        if (parameter is not string direction) return null;

        var vm = Application.Current.MainWindow?.DataContext as MainViewModel;
        var node = vm?.Nodes.FirstOrDefault(n => n.Node.Id == nodeId)?.Node;
        if (node == null) return new List<LinkSlot>();

        return direction == "input"
            ? node.Tool.InputSlots.ToList()
            : node.Tool.OutputSlots.ToList();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public static class ParameterDataTypes
{
    public static string[] All => new[]
    {
        "string", "number", "integer", "boolean",
        "image", "region", "xld", "file", "json"
    };
}
}