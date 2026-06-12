using System.Windows;
using VisionFlow.Tools;

namespace VisionFlow.Views.ToolDialogs {

public partial class PointToPointDistanceDialog 
{
    public PointToPointDistanceDialog(PointToPointDistanceTool tool)
    {
        InitializeComponent();
        Tool = tool;
        LoadFromTool();
        Compute();
    }

    protected void LoadFromTool()
    {
        // 仅显示，当前由连线驱动，无持久参数
        DistanceTextBlock.Text = "-";
    }

    private void Compute()
    {
        try
        {
            double r1 = double.Parse(Row1TextBox.Text);
            double c1 = double.Parse(Col1TextBox.Text);
            double r2 = double.Parse(Row2TextBox.Text);
            double c2 = double.Parse(Col2TextBox.Text);
            double dist = Math.Sqrt((r2 - r1) * (r2 - r1) + (c2 - c1) * (c2 - c1));
            DistanceTextBlock.Text = dist.ToString("F3");
        }
        catch { DistanceTextBlock.Text = "-"; }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}

}