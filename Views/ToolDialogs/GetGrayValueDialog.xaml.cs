using System.Windows;
using VisionFlow.Tools;

namespace VisionFlow.Views.ToolDialogs {

public partial class GetGrayValueDialog 
{
    private readonly GetGrayValueTool _tool;

    public GetGrayValueDialog(GetGrayValueTool tool)
    {
        InitializeComponent();
        _tool = tool;
        Tool = tool;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}

}