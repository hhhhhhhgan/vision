using System.Windows;
using VisionFlow.Tools;

namespace VisionFlow.Views.ToolDialogs {

public partial class CoordinateTransformDialog 
{
    public CoordinateTransformDialog(CoordinateTransformTool tool)
    {
        InitializeComponent();
        Tool = tool;
        LoadFromTool();
    }

    protected override void LoadFromTool()
    {
        if (Tool is not CoordinateTransformTool t) return;
        TransformModeCombo.SelectedIndex = t.TransformMode switch
        {
            "relative" => 1,
            "rotate" => 2,
            _ => 0
        };
        PivotRowTextBox.Text = t.PivotRow.ToString();
        PivotColTextBox.Text = t.PivotCol.ToString();
        ScaleTextBox.Text = t.Scale.ToString();
    }

    protected override void SaveToTool()
    {
        if (Tool is not CoordinateTransformTool t) return;
        t.TransformMode = TransformModeCombo.SelectedIndex switch { 1 => "relative", 2 => "rotate", _ => "absolute" };
        t.PivotRow = double.TryParse(PivotRowTextBox.Text, out var r) ? r : 0;
        t.PivotCol = double.TryParse(PivotColTextBox.Text, out var c) ? c : 0;
        t.Scale = double.TryParse(ScaleTextBox.Text, out var s) ? s : 1.0;
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
}