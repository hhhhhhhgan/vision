using System.Windows;
using VisionFlow.Tools;

namespace VisionFlow.Views.ToolDialogs;

public partial class ImageSaveDialog : ToolDialogBase
{
    private readonly ImageSaveTool _tool;

    public ImageSaveDialog(ImageSaveTool tool)
    {
        InitializeComponent();
        _tool = tool;
        Tool = tool;
        PathTextBox.Text = tool.FilePath;
        FormatCombo.SelectedIndex = tool.Format switch { "jpg" => 1, "bmp" => 2, "tiff" => 3, _ => 0 };
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "PNG|*.png|JPEG|*.jpg|BMP|*.bmp|TIFF|*.tiff",
            DefaultExt = ".png"
        };
        if (dlg.ShowDialog() == true) PathTextBox.Text = dlg.FileName;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        _tool.FilePath = PathTextBox.Text;
        _tool.Format = FormatCombo.SelectedIndex switch { 1 => "jpg", 2 => "bmp", 3 => "tiff", _ => "png" };
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}
