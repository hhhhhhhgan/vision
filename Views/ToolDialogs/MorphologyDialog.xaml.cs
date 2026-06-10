using System.Windows;
using VisionFlow.Tools;

namespace VisionFlow.Views.ToolDialogs;

public partial class MorphologyDialog : ToolDialogBase
{
    private readonly MorphologyTool _tool;

    public MorphologyDialog(MorphologyTool tool)
    {
        InitializeComponent();
        _tool = tool;
        Tool = tool;
        OpCombo.SelectedIndex = tool.Operation switch { "erosion" => 1, "opening" => 2, "closing" => 3, _ => 0 };
        RadiusTextBox.Text = tool.StructElementRadius.ToString();
        IterTextBox.Text = tool.Iterations.ToString();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        _tool.Operation = OpCombo.SelectedIndex switch { 1 => "erosion", 2 => "opening", 3 => "closing", _ => "dilation" };
        _tool.StructElementRadius = double.TryParse(RadiusTextBox.Text, out var r) ? r : 3;
        _tool.Iterations = int.TryParse(IterTextBox.Text, out var i) ? i : 1;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}
