using System;
using System.Windows;
using System.Windows.Controls;
using HalconDotNet;
using VisionFlow.Tools;

namespace VisionFlow.Views.ToolDialogs {

public partial class ImageProcessingDialog : ToolDialogBase
{
    private readonly ToolBase _tool;
    private HImage? _inputImage;

    public ImageProcessingDialog(ToolBase tool)
    {
        InitializeComponent();
        _tool = tool;
        Tool = tool;
        BuildUI();
    }

    private void BuildUI()
    {
        ToolNameBlock.Text = _tool.Name;
        ToolDescBlock.Text = _tool.Description;
        ParamPanel.Children.Clear();

        // 根据工具类型动态生成参数UI
        switch (_tool)
        {
            case ZoomImageTool t:
                AddParam("Scale X", t.ScaleX.ToString(), "水平缩放比例", v => t.ScaleX = ParseDouble(v, 1.0));
                AddParam("Scale Y", t.ScaleY.ToString(), "垂直缩放比例", v => t.ScaleY = ParseDouble(v, 1.0));
                AddCombo("插值方式", new[] { "nearest", "bilinear", "bicubic" }, t.Interpolation, v => t.Interpolation = v);
                break;

            case CropImageTool t:
                AddParam("Row1", t.Row1.ToString(), "裁剪左上行坐标", v => t.Row1 = ParseDouble(v, 0));
                AddParam("Col1", t.Col1.ToString(), "裁剪左上列坐标", v => t.Col1 = ParseDouble(v, 0));
                AddParam("Row2", t.Row2.ToString(), "裁剪右下行坐标", v => t.Row2 = ParseDouble(v, 100));
                AddParam("Col2", t.Col2.ToString(), "裁剪右下列坐标", v => t.Col2 = ParseDouble(v, 100));
                break;

            case FlipImageTool t:
                AddCombo("翻转模式", new[] { "horizontal", "vertical", "both" }, t.FlipMode, v => t.FlipMode = v);
                break;

            case BrightnessContrastTool t:
                AddParam("Brightness", t.Brightness.ToString(), "亮度 (-255~255)", v => t.Brightness = ParseDouble(v, 0));
                AddParam("Contrast", t.Contrast.ToString(), "对比度 (0.5~2.0)", v => t.Contrast = ParseDouble(v, 1.0));
                break;

            case SmoothImageTool t:
                AddCombo("滤波器类型", new[] { "gaussian", "mean", "median" }, t.FilterType, v => t.FilterType = v);
                AddParam("Sigma", t.Sigma.ToString(), "平滑系数", v => t.Sigma = ParseDouble(v, 1.0));
                break;

            case SharpenImageTool t:
                AddParam("Sigma", t.Sigma.ToString(), "平滑系数", v => t.Sigma = ParseDouble(v, 0.5));
                AddParam("Factor", t.Factor.ToString(), "锐化强度", v => t.Factor = ParseDouble(v, 1.0));
                break;

            case BlendImagesTool t:
                AddParam("Alpha", t.Alpha.ToString(), "混合比例 (0=Image1全, 1=Image2全)", v => t.Alpha = ParseDouble(v, 0.5));
                break;

            case ImageSaveTool t:
                AddPathParam("保存路径", t.FilePath, "保存路径", v => t.FilePath = v);
                AddCombo("格式", new[] { "png", "jpg", "bmp", "tiff" }, t.Format, v => t.Format = v);
                break;

            case ImageLoadTool imgTool:
                AddPathParam("图像路径", imgTool.FilePath, "选择图像文件", v => imgTool.FilePath = v);
                break;

            case ImageRotateTool rotTool:
                AddParam("旋转角度", rotTool.Angle.ToString(), "角度（度）", v => rotTool.Angle = ParseDouble(v, 0));
                break;

            default:
                ParamPanel.Children.Add(new TextBlock
                {
                    Text = "此工具无需配置参数",
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x8B, 0xA4, 0xC7)),
                    FontSize = 12
                });
                break;
        }
    }

    private void AddParam(string label, string defaultValue, string tooltip, Action<string> onChanged)
    {
        var stack = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
        stack.Children.Add(new TextBlock { Text = label, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x8B, 0xA4, 0xC7)), FontSize = 10 });
        var tb = new TextBox { Text = defaultValue, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1A, 0x26, 0x34)), Foreground = System.Windows.Media.Brushes.White, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3D, 0x5A, 0x73)), Padding = new Thickness(6, 4, 6, 4), ToolTip = tooltip };
        tb.TextChanged += (_, _) => { try { onChanged(tb.Text); } catch { } };
        stack.Children.Add(tb);
        ParamPanel.Children.Add(stack);
    }

    private void AddCombo(string label, string[] options, string selected, Action<string> onChanged)
    {
        var stack = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
        stack.Children.Add(new TextBlock { Text = label, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x8B, 0xA4, 0xC7)), FontSize = 10 });
        var cb = new ComboBox { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1A, 0x26, 0x34)), Foreground = System.Windows.Media.Brushes.White, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3D, 0x5A, 0x73)), Padding = new Thickness(6, 4, 6, 4) };
        foreach (var opt in options) cb.Items.Add(opt);
        cb.SelectedItem = selected;
        cb.SelectionChanged += (_, e) => { if (cb.SelectedItem is string s) onChanged(s); };
        stack.Children.Add(cb);
        ParamPanel.Children.Add(stack);
    }

    private void AddPathParam(string label, string defaultValue, string tooltip, Action<string> onChanged)
    {
        var stack = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
        stack.Children.Add(new TextBlock { Text = label, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x8B, 0xA4, 0xC7)), FontSize = 10 });
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var tb = new TextBox { Text = defaultValue, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1A, 0x26, 0x34)), Foreground = System.Windows.Media.Brushes.White, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3D, 0x5A, 0x73)), Padding = new Thickness(6, 4, 6, 4), ToolTip = tooltip };
        tb.TextChanged += (_, _) => { try { onChanged(tb.Text); } catch { } };
        Grid.SetColumn(tb, 0);
        var btn = new Button { Content = "浏览...", Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3D, 0x5A, 0x73)), Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0), Padding = new Thickness(8, 4, 8, 4), Cursor = System.Windows.Input.Cursors.Hand };
        btn.Click += (_, _) =>
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "图像文件|*.png;*.jpg;*.jpeg;*.bmp;*.tiff;*.tif|All文件|*.*" };
            if (dlg.ShowDialog() == true) { tb.Text = dlg.FileName; onChanged(dlg.FileName); }
        };
        Grid.SetColumn(btn, 1);
        grid.Children.Add(tb);
        grid.Children.Add(btn);
        stack.Children.Add(grid);
        ParamPanel.Children.Add(stack);
    }

    private static double ParseDouble(string s, double def) => double.TryParse(s, out var v) ? v : def;

    public void SetInputImage(HImage image)
    {
        _inputImage?.Dispose();
        _inputImage = image.CopyImage();
        HWindow.SetFullImage(_inputImage);
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
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