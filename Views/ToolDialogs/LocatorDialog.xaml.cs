using System;
using System.Windows;
using System.Windows.Controls;
using HalconDotNet;
using VisionFlow.Tools;

namespace VisionFlow.Views.ToolDialogs {

public partial class LocatorDialog 
{
    private HImage? _inputImage;

    public LocatorDialog(LocatorTool tool)
    {
        InitializeComponent();
        Tool = tool;
        LoadFromTool();
    }

    protected override void LoadFromTool()
    {
        if (Tool is not LocatorTool t) return;
        ROIRow1TextBox.Text = t.ROIRow1.ToString();
        ROICol1TextBox.Text = t.ROICol1.ToString();
        ROIRow2TextBox.Text = t.ROIRow2.ToString();
        ROICol2TextBox.Text = t.ROICol2.ToString();
        OffsetXTextBox.Text = t.OffsetX.ToString();
        OffsetYTextBox.Text = t.OffsetY.ToString();
        OffsetAngleTextBox.Text = t.OffsetAngle.ToString();
        ThresholdTextBox.Text = t.BlobThreshold.ToString();
        MinAreaTextBox.Text = t.MinArea.ToString();
        FollowRefCheckBox.IsChecked = t.FollowRefPoint;
        FollowOffsetCheckBox.IsChecked = t.FollowOffset;

        LocateModeCombo.SelectedIndex = t.LocateMode switch
        {
            "edge_pair" => 1,
            "template" => 2,
            _ => 0
        };
    }

    protected override void SaveToTool()
    {
        if (Tool is not LocatorTool t) return;
        t.ROIRow1 = ParseDouble(ROIRow1TextBox.Text, 50);
        t.ROICol1 = ParseDouble(ROICol1TextBox.Text, 50);
        t.ROIRow2 = ParseDouble(ROIRow2TextBox.Text, 350);
        t.ROICol2 = ParseDouble(ROICol2TextBox.Text, 350);
        t.OffsetX = ParseDouble(OffsetXTextBox.Text, 0);
        t.OffsetY = ParseDouble(OffsetYTextBox.Text, 0);
        t.OffsetAngle = ParseDouble(OffsetAngleTextBox.Text, 0);
        t.BlobThreshold = ParseDouble(ThresholdTextBox.Text, 128);
        t.MinArea = ParseDouble(MinAreaTextBox.Text, 100);
        t.FollowRefPoint = FollowRefCheckBox.IsChecked ?? false;
        t.FollowOffset = FollowOffsetCheckBox.IsChecked ?? false;
        t.LocateMode = LocateModeCombo.SelectedIndex switch { 1 => "edge_pair", 2 => "template", _ => "blob_center" };
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
}