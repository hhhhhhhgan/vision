using System.Windows;
using HalconDotNet;
using VisionFlow.Tools;

namespace VisionFlow.Views.ToolDialogs {

public partial class ImageRotateDialog : ToolDialogBase
{
    private readonly ImageRotateTool _tool;
    private HImage? _inputImage;
    private HImage? _outputImage;

    public ImageRotateDialog(ImageRotateTool tool)
    {
        InitializeComponent();
        _tool = tool;
        Tool = tool;
        AngleTextBox.Text = tool.Angle.ToString();
    }

    public void SetInputImage(HImage image)
    {
        _inputImage?.Dispose();
        _inputImage = image.CopyImage();
        UpdateDisplay();
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_inputImage == null) { MessageBox.Show("请先设置输入图像", "提示"); return; }
        if (!double.TryParse(AngleTextBox.Text, out double angle)) { MessageBox.Show("请输入有效的角度值", "错误"); return; }
        _outputImage?.Dispose();
        _outputImage = _inputImage.RotateImage(angle, "constant");
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (_outputImage != null) HWindow.SetFullImage(_outputImage);
        else if (_inputImage != null) HWindow.SetFullImage(_inputImage);
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(AngleTextBox.Text, out double angle)) _tool.Angle = angle;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
}