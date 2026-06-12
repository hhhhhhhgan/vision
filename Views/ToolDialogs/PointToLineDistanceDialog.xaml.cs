using System.Windows;
using VisionFlow.Tools;

namespace VisionFlow.Views.ToolDialogs {

public partial class PointToLineDistanceDialog 
{
    private readonly PointToLineDistanceTool _tool;

    public PointToLineDistanceDialog(PointToLineDistanceTool tool)
    {
        InitializeComponent();
        _tool = tool;
        Tool = tool;
        DistanceTextBlock.Text = "-";
    }

    private void Compute()
    {
        try
        {
            double pr = double.Parse(PRTextBox.Text);
            double pc = double.Parse(PCTextBox.Text);
            double lr1 = double.Parse(LR1TextBox.Text);
            double lc1 = double.Parse(LC1TextBox.Text);
            double lr2 = double.Parse(LR2TextBox.Text);
            double lc2 = double.Parse(LC2TextBox.Text);

            double dx = lr2 - lr1, dy = lc2 - lc1;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1e-6) { DistanceTextBlock.Text = "-"; return; }

            double t = Math.Max(0, Math.Min(1, ((pr - lr1) * dx + (pc - lc1) * dy) / (len * len)));
            double cr = lr1 + t * dx, cc = lc1 + t * dy;
            double dist = Math.Sqrt((pr - cr) * (pr - cr) + (pc - cc) * (pc - cc));
            DistanceTextBlock.Text = dist.ToString("F3");
        }
        catch { DistanceTextBlock.Text = "-"; }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}

}