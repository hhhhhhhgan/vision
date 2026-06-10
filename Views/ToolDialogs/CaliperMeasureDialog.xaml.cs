using System.Windows;
using VisionFlow.Tools;

namespace VisionFlow.Views.ToolDialogs {

public partial class CaliperMeasureDialog : ToolDialogBase
{
    private readonly CaliperMeasureTool _tool;

    public CaliperMeasureDialog(CaliperMeasureTool tool)
    {
        InitializeComponent();
        _tool = tool;
        Tool = tool;
        LengthText.Text = tool.MeasureLength.ToString();
        WidthText.Text = tool.MeasureWidth.ToString();
        SigmaText.Text = tool.Sigma.ToString();
        ThreshText.Text = tool.Threshold.ToString();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        _tool.MeasureLength = double.TryParse(LengthText.Text, out var l) ? l : 20;
        _tool.MeasureWidth = double.TryParse(WidthText.Text, out var w) ? w : 3;
        _tool.Sigma = double.TryParse(SigmaText.Text, out var s) ? s : 1.0;
        _tool.Threshold = double.TryParse(ThreshText.Text, out var t) ? t : 25;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}

}