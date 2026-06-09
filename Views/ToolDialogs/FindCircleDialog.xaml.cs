using System.Windows;
using HalconDotNet;
using VisionFlow.Tools;

namespace VisionFlow.Views.ToolDialogs;

public partial class FindCircleDialog : ToolDialogBase
{
    private HImage? _inputImage;
    private HXLD? _resultCircle;

    public FindCircleDialog(FindCircleTool tool)
    {
        InitializeComponent();
        Tool = tool;
        LoadFromTool();
    }

    protected override void LoadFromTool()
    {
        if (Tool is not FindCircleTool t) return;
        ROIRowTextBox.Text = t.ROIRow.ToString();
        ROIColTextBox.Text = t.ROICol.ToString();
        InnerRadiusTextBox.Text = t.ROIInnerRadius.ToString();
        OuterRadiusTextBox.Text = t.ROIOuterRadius.ToString();
        SigmaTextBox.Text = t.Sigma.ToString();
        EdgeThresholdTextBox.Text = t.EdgeThreshold.ToString();
        SelectPointsTextBox.Text = t.SelectPoints.ToString();
        CircularityTextBox.Text = t.Circularity.ToString();

        TransitionComboBox.SelectedIndex = t.Transition switch
        {
            "positive" => 1,
            "negative" => 2,
            _ => 0
        };
    }

    protected override void SaveToTool()
    {
        if (Tool is not FindCircleTool t) return;
        t.ROIRow = ParseDouble(ROIRowTextBox.Text, 200);
        t.ROICol = ParseDouble(ROIColTextBox.Text, 200);
        t.ROIInnerRadius = ParseDouble(InnerRadiusTextBox.Text, 50);
        t.ROIOuterRadius = ParseDouble(OuterRadiusTextBox.Text, 100);
        t.Sigma = ParseDouble(SigmaTextBox.Text, 1.0);
        t.EdgeThreshold = ParseDouble(EdgeThresholdTextBox.Text, 25);
        t.SelectPoints = ParseInt(SelectPointsTextBox.Text, 30);
        t.Circularity = ParseDouble(CircularityTextBox.Text, 0.3);
        t.Transition = TransitionComboBox.SelectedIndex switch { 1 => "positive", 2 => "negative", _ => "all" };
    }

    private double ParseDouble(string s, double def) => double.TryParse(s, out var v) ? v : def;
    private int ParseInt(string s, int def) => int.TryParse(s, out var v) ? v : def;

    public void SetInputImage(HImage image)
    {
        _inputImage?.Dispose();
        _inputImage = image.CopyImage();
        UpdateDisplay();
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_inputImage == null) { MessageBox.Show("请先设置输入图像", "提示"); return; }
        try
        {
            SaveToTool();
            ExecuteFindCircle();
            UpdateDisplay();
        }
        catch (Exception ex)
        {
            InfoTextBlock.Text = $"错误: {ex.Message}";
        }
    }

    private void ExecuteFindCircle()
    {
        if (_inputImage == null) return;

        double row = ParseDouble(ROIRowTextBox.Text, 200);
        double col = ParseDouble(ROIColTextBox.Text, 200);
        double innerR = ParseDouble(InnerRadiusTextBox.Text, 50);
        double outerR = ParseDouble(OuterRadiusTextBox.Text, 100);
        double sigma = ParseDouble(SigmaTextBox.Text, 1.0);
        double threshold = ParseDouble(EdgeThresholdTextBox.Text, 25);
        string transition = TransitionComboBox.SelectedIndex switch { 1 => "positive", 2 => "negative", _ => "all" };
        int selectPts = ParseInt(SelectPointsTextBox.Text, 30);
        int numIter = 5;
        double circThresh = ParseDouble(CircularityTextBox.Text, 0.3);

        HRegion roi = new HRegion();
        roi.GenCircleRing(row, col, innerR, outerR);

        using HImage reduced = _inputImage.ReduceDomain(roi);

        using HXLDContArray contours = reduced.EdgesImage(sigma, threshold, transition, "canny", numIter)
            .SymmetizeFeatures("arc_info", 2 * selectPts, 2, 0.02, out _, out _, out _, out _, out _, out _, out _);

        HTuple bestCont = new HTuple();
        double maxLength = 0;
        for (int i = 1; i <= contours.Num; i++)
        {
            using HXLDCont ci = (HXLDCont)contours[i - 1];
            try
            {
                HTuple length = ci.LengthContour();
                if (length.D > maxLength)
                {
                    maxLength = length.D;
                    bestCont = ci;
                }
            }
            catch { }
        }

        if (bestCont.Length == 0)
        {
            InfoTextBlock.Text = "错误: 未找到有效边缘轮廓";
            roi.Dispose();
            return;
        }

        using HXLDCont bc = (HXLDCont)bestCont;
        bc.FitCircleContourXld("tukey", numIter, 0, 0.02, 0, 0, out HTuple r, out HTuple c, out HTuple radius, out _, out _, out _);

        _resultCircle?.Dispose();
        _resultCircle = new HXLD();
        _resultCircle.GenCircleContourXld(r, c, radius, 0, 4 * Math.PI, "positive", 1.0);

        roi.Dispose();

        InfoTextBlock.Text = $"圆心: ({r.D:F1}, {c.D:F1})  半径: {radius.D:F1}";
    }

    private void UpdateDisplay()
    {
        if (_inputImage == null) return;
        HWindow.SetFullImage(_inputImage);

        if (_resultCircle != null)
        {
            // 显示 ROI 圆环
            double row = ParseDouble(ROIRowTextBox.Text, 200);
            double col = ParseDouble(ROIColTextBox.Text, 200);
            double innerR = ParseDouble(InnerRadiusTextBox.Text, 50);
            double outerR = ParseDouble(OuterRadiusTextBox.Text, 100);

            HRegion roi = new HRegion();
            roi.GenCircleRing(row, col, innerR, outerR);
            roi.DispRegion(HWindow.HalconWindow);
            roi.Dispose();

            // 显示圆
            _resultCircle.DispXld(HWindow.HalconWindow);
        }
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
