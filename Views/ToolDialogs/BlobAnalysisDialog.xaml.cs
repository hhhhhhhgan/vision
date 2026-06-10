using System.Windows;
using VisionFlow.Tools;

namespace VisionFlow.Views.ToolDialogs {

public partial class BlobAnalysisDialog : ToolDialogBase
{
    private readonly BlobAnalysisTool _tool;

    public BlobAnalysisDialog(BlobAnalysisTool tool)
    {
        InitializeComponent();
        _tool = tool;
        Tool = tool;
        MinAreaTextBox.Text = tool.MinArea.ToString();
        MaxAreaTextBox.Text = tool.MaxArea.ToString();
        SortCombo.SelectedIndex = tool.SortBy switch { "row" => 1, "column" => 2, _ => 0 };
        TopNTextBox.Text = tool.TopN.ToString();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        _tool.MinArea = double.TryParse(MinAreaTextBox.Text, out var m) ? m : 0;
        _tool.MaxArea = double.TryParse(MaxAreaTextBox.Text, out var x) ? x : 999999;
        _tool.SortBy = SortCombo.SelectedIndex switch { 1 => "row", 2 => "column", _ => "area" };
        _tool.TopN = int.TryParse(TopNTextBox.Text, out var t) ? t : 0;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}

}