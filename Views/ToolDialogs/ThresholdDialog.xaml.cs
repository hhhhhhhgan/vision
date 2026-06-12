using System.Windows;
using VisionFlow.Tools;

namespace VisionFlow.Views.ToolDialogs {

public partial class ThresholdDialog 
{
    private readonly ThresholdTool _tool;
    private HalconDotNet.HImage? _inputImage;

    public ThresholdDialog(ThresholdTool tool)
    {
        InitializeComponent();
        _tool = tool;
        Tool = tool;
        MinThreshTextBox.Text = tool.MinThreshold.ToString();
        MaxThreshTextBox.Text = tool.MaxThreshold.ToString();
        ModeCombo.SelectedIndex = tool.ThresholdMode switch { "min" => 1, "range" => 2, _ => 0 };
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        _tool.MinThreshold = ParseDouble(MinThreshTextBox.Text, 128);
        _tool.MaxThreshold = ParseDouble(MaxThreshTextBox.Text, 255);
        _tool.ThresholdMode = ModeCombo.SelectedIndex switch { 1 => "min", 2 => "range", _ => "max" };
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
    private double ParseDouble(string s, double def) => double.TryParse(s, out var v) ? v : def;
}

}