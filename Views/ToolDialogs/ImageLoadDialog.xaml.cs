using System.Windows;
using HalconDotNet;
using Microsoft.Win32;
using VisionFlow.Tools;

namespace VisionFlow.Views.ToolDialogs {

public partial class ImageLoadDialog : ToolDialogBase
{
    private readonly ImageLoadTool _tool;
    private HImage? _currentImage;

    public ImageLoadDialog(ImageLoadTool tool)
    {
        InitializeComponent();
        _tool = tool;
        Tool = tool;
        FilePathTextBox.Text = tool.FilePath;
        if (!string.IsNullOrEmpty(tool.FilePath) && File.Exists(tool.FilePath))
            LoadImage(tool.FilePath);
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "图像文件|*.png;*.jpg;*.jpeg;*.bmp;*.tiff;*.tif;*.dcm|All文件|*.*" };
        if (dialog.ShowDialog() == true)
        {
            FilePathTextBox.Text = dialog.FileName;
            LoadImage(dialog.FileName);
        }
    }

    private void LoadImage(string path)
    {
        try
        {
            _currentImage?.Dispose();
            _currentImage = new HImage(path);
            HWindow.SetFullImage(_currentImage);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载图像失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        _tool.FilePath = FilePathTextBox.Text;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

public static class HWindowControlExtensions
{
    public static void SetFullImage(this HalconDotNet.WPF.HWindowControlWPF hWindow, HImage image)
    {
        image.GetImageSize(out HTuple width, out HTuple height);
        double ratio = Math.Min(hWindow.Width / (double)width.I, hWindow.Height / (double)height.I);
        double imgW = width.I * ratio;
        double imgH = height.I * ratio;
        double ox = (hWindow.Width - imgW) / 2;
        double oy = (hWindow.Height - imgH) / 2;
        hWindow.HalconWindow.SetPart((int)oy, (int)ox, (int)(oy + imgH) - 1, (int)(ox + imgW) - 1);
        image.DispImage(hWindow.HalconWindow);
    }
}
}