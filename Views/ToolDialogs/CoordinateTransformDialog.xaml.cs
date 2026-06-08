using System.Windows;
using VisionFlow.Tools;

namespace VisionFlow.Views.ToolDialogs;

public partial class CoordinateTransformDialog : ToolDialogBase
{
    private readonly CoordinateTransformTool _tool;

    public CoordinateTransformDialog(CoordinateTransformTool tool)
    {
        InitializeComponent();
        _tool = tool;
        Tool = tool;
        LoadFromTool();
    }

    private void LoadFromTool()
    {
        TransformModeCombo.SelectedIndex = _tool.TransformMode switch
        {
            "relative" => 1,
            "rotate" => 2,
            _ => 0
        };
        PivotRowTextBox.Text = _tool.PivotRow.ToString();
        PivotColTextBox.Text = _tool.PivotCol.ToString();
        ScaleTextBox.Text = _tool.Scale.ToString();
    }

    private void SaveToTool()
    {
        _tool.TransformMode = TransformModeCombo.SelectedIndex switch { 1 => "relative", 2 => "rotate", _ => "absolute" };
        _tool.PivotRow = double.TryParse(PivotRowTextBox.Text, out var r) ? r : 0;
        _tool.PivotCol = double.TryParse(PivotColTextBox.Text, out var c) ? c : 0;
        _tool.Scale = double.TryParse(ScaleTextBox.Text, out var s) ? s : 1.0;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        SaveToTool();
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}