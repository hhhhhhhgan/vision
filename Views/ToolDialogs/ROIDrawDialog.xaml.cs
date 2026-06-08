using System.Windows;
using System.Windows.Input;
using HalconDotNet;
using VisionFlow.Tools;

namespace VisionFlow.Views.ToolDialogs;

public partial class ROIDrawDialog : ToolDialogBase
{
    private readonly ROIDrawTool _tool;
    private HImage? _inputImage;
    private HRegion? _currentRegion;
    private bool _isDrawing;
    private Point _startPoint;

    public ROIDrawDialog(ROIDrawTool tool)
    {
        InitializeComponent();
        _tool = tool;
        Tool = tool;
    }

    public void SetInputImage(HImage image)
    {
        _inputImage?.Dispose();
        _inputImage = image.CopyImage();
        HWindow.SetFullImage(_inputImage);
    }

    private void DrawButton_Click(object sender, RoutedEventArgs e)
    {
        if (_inputImage == null) { MessageBox.Show("请先设置输入图像", "提示"); return; }
        string roiType = (ROITypeComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "矩形";
        MessageBox.Show($"已进入{roiType}绘制模式，请在图像窗口中拖动鼠标绘制", "绘制模式");
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        _currentRegion?.Dispose();
        _currentRegion = null;
        if (_inputImage != null) HWindow.SetFullImage(_inputImage);
    }

    private void HWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDrawing = true;
        _startPoint = e.GetPosition(HWindow);
    }

    private void HWindow_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDrawing) return;
        var p = e.GetPosition(HWindow);
        CoordsTextBlock.Text = $"起点: ({_startPoint.X:F0}, {_startPoint.Y:F0})  当前: ({p.X:F0}, {p.Y:F0})";
    }

    private void HWindow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDrawing) return;
        _isDrawing = false;

        var endPoint = e.GetPosition(HWindow);
        double x = Math.Min(_startPoint.X, endPoint.X);
        double y = Math.Min(_startPoint.Y, endPoint.Y);
        double w = Math.Abs(endPoint.X - _startPoint.X);
        double h = Math.Abs(endPoint.Y - _startPoint.Y);

        _currentRegion?.Dispose();
        _currentRegion = new HRegion();
        _currentRegion.GenRectangle1(y, x, y + h, x + w);

        _tool.ROICoords = $"{{\"type\":\"rect\",\"x\":{x},\"y\":{y},\"w\":{w},\"h\":{h}}}";

        if (_inputImage != null)
        {
            HWindow.SetFullImage(_inputImage);
            _currentRegion.DispRegion(HWindow.HalconWindow);
        }

        CoordsTextBlock.Text = $"区域: x={x:F0}, y={y:F0}, w={w:F0}, h={h:F0}";
    }

    private void OkButton_Click(object sender, RoutedEventArgs e) { DialogResult = true; Close(); }
    private void CancelButton_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}