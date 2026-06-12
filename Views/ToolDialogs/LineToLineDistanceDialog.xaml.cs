using System.Windows;
using VisionFlow.Tools;

namespace VisionFlow.Views.ToolDialogs {

public partial class LineToLineDistanceDialog 
{
    private readonly LineToLineDistanceTool _tool;

    public LineToLineDistanceDialog(LineToLineDistanceTool tool)
    {
        InitializeComponent();
        _tool = tool;
        Tool = tool;
        DistanceTextBlock.Text = "-";
    }

    private double SegSegDist(double r1, double c1, double r2, double c2, double r3, double c3, double r4, double c4)
    {
        double d1 = PtSegDist(r3, c3, r1, c1, r2, c2);
        double d2 = PtSegDist(r4, c4, r1, c1, r2, c2);
        double d3 = PtSegDist(r1, c1, r3, c3, r4, c4);
        double d4 = PtSegDist(r2, c2, r3, c3, r4, c4);
        return Math.Min(Math.Min(d1, d2), Math.Min(d3, d4));
    }

    private double PtSegDist(double pr, double pc, double lr1, double lc1, double lr2, double lc2)
    {
        double dx = lr2 - lr1, dy = lc2 - lc1;
        double len2 = dx * dx + dy * dy;
        if (len2 < 1e-6) return Math.Sqrt((pr - lr1) * (pr - lr1) + (pc - lc1) * (pc - lc1));
        double t = Math.Max(0, Math.Min(1, ((pr - lr1) * dx + (pc - lc1) * dy) / len2));
        double cr = lr1 + t * dx, cc = lc1 + t * dy;
        return Math.Sqrt((pr - cr) * (pr - cr) + (pc - cc) * (pc - cc));
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}

}