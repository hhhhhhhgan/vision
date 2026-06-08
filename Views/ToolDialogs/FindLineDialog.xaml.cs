using System.Windows;
using HalconDotNet;
using VisionFlow.Tools;

namespace VisionFlow.Views.ToolDialogs;

public partial class FindLineDialog : ToolDialogBase
{
    private readonly FindLineTool _tool;
    private HImage? _inputImage;
    private HXLD? _resultLine;
    private HTuple? _r1, _c1, _r2, _c2;

    public FindLineDialog(FindLineTool tool)
    {
        InitializeComponent();
        _tool = tool;
        Tool = tool;
        LoadFromTool();
    }

    private void LoadFromTool()
    {
        ROIRow1TextBox.Text = _tool.ROIRow1.ToString();
        ROICol1TextBox.Text = _tool.ROICol1.ToString();
        ROIRow2TextBox.Text = _tool.ROIRow2.ToString();
        ROICol2TextBox.Text = _tool.ROICol2.ToString();
        SigmaTextBox.Text = _tool.Sigma.ToString();
        EdgeThresholdTextBox.Text = _tool.EdgeThreshold.ToString();
        SelectPointsTextBox.Text = _tool.SelectPoints.ToString();
        NumIterationsTextBox.Text = _tool.NumIterations.ToString();

        TransitionComboBox.SelectedIndex = _tool.Transition switch
        {
            "positive" => 1,
            "negative" => 2,
            _ => 0
        };
    }

    private void SaveToTool()
    {
        _tool.ROIRow1 = ParseDouble(ROIRow1TextBox.Text, 100);
        _tool.ROICol1 = ParseDouble(ROICol1TextBox.Text, 100);
        _tool.ROIRow2 = ParseDouble(ROIRow2TextBox.Text, 300);
        _tool.ROICol2 = ParseDouble(ROICol2TextBox.Text, 400);
        _tool.Sigma = ParseDouble(SigmaTextBox.Text, 1.0);
        _tool.EdgeThreshold = ParseDouble(EdgeThresholdTextBox.Text, 25);
        _tool.SelectPoints = ParseInt(SelectPointsTextBox.Text, 15);
        _tool.NumIterations = ParseInt(NumIterationsTextBox.Text, 5);
        _tool.Transition = TransitionComboBox.SelectedIndex switch { 1 => "positive", 2 => "negative", _ => "all" };
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
            ExecuteFindLine();
            UpdateDisplay();
        }
        catch (Exception ex)
        {
            InfoTextBlock.Text = $"错误: {ex.Message}";
        }
    }

    private void ExecuteFindLine()
    {
        if (_inputImage == null) return;

        double r1 = ParseDouble(ROIRow1TextBox.Text, 100);
        double c1 = ParseDouble(ROICol1TextBox.Text, 100);
        double r2 = ParseDouble(ROIRow2TextBox.Text, 300);
        double c2 = ParseDouble(ROICol2TextBox.Text, 400);
        double sigma = ParseDouble(SigmaTextBox.Text, 1.0);
        double threshold = ParseDouble(EdgeThresholdTextBox.Text, 25);
        string transition = TransitionComboBox.SelectedIndex switch { 1 => "positive", 2 => "negative", _ => "all" };
        int selectPts = ParseInt(SelectPointsTextBox.Text, 15);
        int numIter = ParseInt(NumIterationsTextBox.Text, 5);

        HRegion roi = new HRegion();
        roi.GenRectangle1(r1, c1, r2, c2);

        using HImage reduced = _inputImage.ReduceDomain(roi);

        using HXLDContArray contours = reduced.EdgesImage(sigma, threshold, transition, "canny", numIter)
            .SymmetizeFeatures("arc_info", 2 * selectPts, 2, 0.05, out HTuple row1, out HTuple col1, out HTuple row2, out HTuple col2, out HTuple length, out HTuple phi, out HTuple pointOrder);

        HTuple indices = length.TupleSortIndex();
        int bestIdx = 0;
        for (int i = indices.Length - 1; i >= 0; i--)
        {
            if (length.TupleSelect(indices[i]).D > 5) { bestIdx = indices[i].I; break; }
        }

        HXLDCont best = new HXLDCont(contours[bestIdx]);
        best.FitLineContourXld("tukey", numIter, 0, 0.05, 0, out HTuple nr1, out HTuple nc1, out HTuple nr2, out HTuple nc2, out _, out _, out _, out _);

        _resultLine?.Dispose();
        _resultLine = new HXLD();
        _resultLine.GenContourPolygonXld(new HTuple(new double[] { nr1.D, nr2.D }), new HTuple(new double[] { nc1.D, nc2.D }));

        _r1 = nr1; _c1 = nc1; _r2 = nr2; _c2 = nc2;

        roi.Dispose();

        InfoTextBlock.Text = $"端点1: ({nr1.D:F1}, {nc1.D:F1})  端点2: ({nr2.D:F1}, {nc2.D:F1})";
    }

    private void UpdateDisplay()
    {
        if (_inputImage == null) return;
        HWindow.SetFullImage(_inputImage);

        if (_resultLine != null)
        {
            // 显示 ROI
            double r1 = ParseDouble(ROIRow1TextBox.Text, 100);
            double c1 = ParseDouble(ROICol1TextBox.Text, 100);
            double r2 = ParseDouble(ROIRow2TextBox.Text, 300);
            double c2 = ParseDouble(ROICol2TextBox.Text, 400);
            HRegion roi = new HRegion();
            roi.GenRectangle1(r1, c1, r2, c2);
            roi.DispRegion(HWindow.HalconWindow);
            roi.Dispose();

            // 显示线
            _resultLine.DispXld(HWindow.HalconWindow);
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