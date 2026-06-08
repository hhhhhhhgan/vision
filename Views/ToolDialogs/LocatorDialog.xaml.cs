using System.Windows;
using System.Windows.Controls;
using HalconDotNet;
using VisionFlow.Tools;

namespace VisionFlow.Views.ToolDialogs;

public partial class LocatorDialog : ToolDialogBase
{
    private readonly LocatorTool _tool;
    private HImage? _inputImage;

    public LocatorDialog(LocatorTool tool)
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
        OffsetXTextBox.Text = _tool.OffsetX.ToString();
        OffsetYTextBox.Text = _tool.OffsetY.ToString();
        OffsetAngleTextBox.Text = _tool.OffsetAngle.ToString();
        ThresholdTextBox.Text = _tool.BlobThreshold.ToString();
        MinAreaTextBox.Text = _tool.MinArea.ToString();
        FollowRefCheckBox.IsChecked = _tool.FollowRefPoint;
        FollowOffsetCheckBox.IsChecked = _tool.FollowOffset;

        LocateModeCombo.SelectedIndex = _tool.LocateMode switch
        {
            "edge_pair" => 1,
            "template" => 2,
            _ => 0
        };
    }

    private void SaveToTool()
    {
        _tool.ROIRow1 = ParseDouble(ROIRow1TextBox.Text, 50);
        _tool.ROICol1 = ParseDouble(ROICol1TextBox.Text, 50);
        _tool.ROIRow2 = ParseDouble(ROIRow2TextBox.Text, 350);
        _tool.ROICol2 = ParseDouble(ROICol2TextBox.Text, 350);
        _tool.OffsetX = ParseDouble(OffsetXTextBox.Text, 0);
        _tool.OffsetY = ParseDouble(OffsetYTextBox.Text, 0);
        _tool.OffsetAngle = ParseDouble(OffsetAngleTextBox.Text, 0);
        _tool.BlobThreshold = ParseDouble(ThresholdTextBox.Text, 128);
        _tool.MinArea = ParseDouble(MinAreaTextBox.Text, 100);
        _tool.FollowRefPoint = FollowRefCheckBox.IsChecked ?? false;
        _tool.FollowOffset = FollowOffsetCheckBox.IsChecked ?? false;
        _tool.LocateMode = LocateModeCombo.SelectedIndex switch { 1 => "edge_pair", 2 => "template", _ => "blob_center" };
    }

    private void LocateModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // 模式切换时更新参数显示
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_inputImage == null) { InfoTextBlock.Text = "请先设置输入图像"; return; }
        try
        {
            SaveToTool();
            ExecuteLocator();
        }
        catch (Exception ex)
        {
            InfoTextBlock.Text = $"错误: {ex.Message}";
        }
    }

    private void ExecuteLocator()
    {
        if (_inputImage == null) return;

        double r1 = ParseDouble(ROIRow1TextBox.Text, 50);
        double c1 = ParseDouble(ROICol1TextBox.Text, 50);
        double r2 = ParseDouble(ROIRow2TextBox.Text, 350);
        double c2 = ParseDouble(ROICol2TextBox.Text, 350);
        double thresh = ParseDouble(ThresholdTextBox.Text, 128);
        double minArea = ParseDouble(MinAreaTextBox.Text, 100);

        using HRegion roi = new HRegion();
        roi.GenRectangle1(r1, c1, r2, c2);

        using HImage reduced = _inputImage.ReduceDomain(roi);
        using HRegion threshold = reduced.BinaryThreshold("max", "light", out _);
        using HRegion selected = threshold.SelectShape("area", "and", minArea, double.MaxValue);
        using HRegion connected = selected.Connection();

        HWindow.SetFullImage(_inputImage);
        roi.DispRegion(HWindow.HalconWindow);

        if (connected.CountRegions() == 0)
        {
            InfoTextBlock.Text = "未找到特征区域";
            return;
        }

        HTuple areas, rows, cols;
        connected.AreaCenter(out areas, out rows, out cols);

        int maxIdx = 0;
        for (int i = 1; i < areas.Length; i++)
            if (areas[i].D > areas[maxIdx].D) maxIdx = i;

        double fRow = rows[maxIdx].D;
        double fCol = cols[maxIdx].D;

        double offsetX = ParseDouble(OffsetXTextBox.Text, 0);
        double offsetY = ParseDouble(OffsetYTextBox.Text, 0);

        double oRow = fRow + offsetY;
        double oCol = fCol + offsetX;

        // 绘制定位结果
        HRegion marker = new HRegion();
        marker.GenCross2(fRow, fCol, 20, 0);
        marker.DispRegion(HWindow.HalconWindow);

        HRegion offsetMarker = new HRegion();
        offsetMarker.GenCross2(oRow, oCol, 25, 0);
        offsetMarker.DispRegion(HWindow.HalconWindow);

        InfoTextBlock.Text = $"找到: ({fRow:F1}, {fCol:F1}) → 偏移后: ({oRow:F1}, {oCol:F1})  Area={areas[maxIdx].D:F0}";
    }

    private void UpdateDisplay()
    {
        if (_inputImage == null) return;
        HWindow.SetFullImage(_inputImage);

        double r1 = ParseDouble(ROIRow1TextBox.Text, 50);
        double c1 = ParseDouble(ROICol1TextBox.Text, 50);
        double r2 = ParseDouble(ROIRow2TextBox.Text, 350);
        double c2 = ParseDouble(ROICol2TextBox.Text, 350);

        HRegion roi = new HRegion();
        roi.GenRectangle1(r1, c1, r2, c2);
        roi.DispRegion(HWindow.HalconWindow);
        roi.Dispose();
    }

    public void SetInputImage(HImage image)
    {
        _inputImage?.Dispose();
        _inputImage = image.CopyImage();
        UpdateDisplay();
    }

    private double ParseDouble(string s, double def) => double.TryParse(s, out var v) ? v : def;

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