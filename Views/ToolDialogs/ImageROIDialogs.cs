using System.Windows;
using HalconDotNet;
using VisionFlow.Tools;
using System.Linq;

namespace VisionFlow.Views.ToolDialogs {

/// <summary>
/// 图像加载对话框
/// </summary>
public partial class ImageLoadDialog : ToolDialogBase
{
    private readonly ImageLoadTool _tool;
    private System.Windows.Controls.TextBox _pathBox = null!;
    private HWindowControl _hWindow = null!;
    private System.Windows.Controls.TextBlock _resultInfo = null!;

    public ImageLoadDialog(ImageLoadTool tool)
    {
        _tool = tool;
        Title = $"图像加载 — {tool.Name}";
        Width = 550;
        Height = 400;
        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x0B, 0x11, 0x20));
        BuildUI();
        LoadFromTool();
    }

    private void BuildUI()
    {
        var grid = new System.Windows.Controls.Grid { Margin = new Thickness(16) };
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

        // 路径选择
        var pathPanel = new System.Windows.Controls.Grid();
        pathPanel.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(70) });
        pathPanel.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        pathPanel.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = System.Windows.GridLength.Auto });

        var pathLbl = new System.Windows.Controls.TextBlock { Text = "图像路径:", Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center };
        System.Windows.Controls.Grid.SetColumn(pathLbl, 0);
        _pathBox = new System.Windows.Controls.TextBox { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.White, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x37, 0x41, 0x51)), Padding = new Thickness(8, 6, 8, 6) };
        System.Windows.Controls.Grid.SetColumn(_pathBox, 1);

        var browseBtn = new System.Windows.Controls.Button { Content = "📂 浏览", Padding = new Thickness(10, 6, 10, 6), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
        browseBtn.Click += (_, _) =>
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "图像文件|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff;*.gif|所有文件|*.*" };
            if (dlg.ShowDialog() == true) _pathBox.Text = dlg.FileName;
        };
        System.Windows.Controls.Grid.SetColumn(browseBtn, 2);
        pathPanel.Children.Add(pathLbl);
        pathPanel.Children.Add(_pathBox);
        pathPanel.Children.Add(browseBtn);
        Grid.SetRow(pathPanel, 0);
        grid.Children.Add(pathPanel);

        // 预览区
        var previewBorder = new System.Windows.Controls.Border { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x11, 0x18, 0x27)), CornerRadius = new System.Windows.Media.CornerRadius(8), Margin = new Thickness(0, 12, 0, 0) };
        var previewGrid = new System.Windows.Controls.Grid();
        previewGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        previewGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        _resultInfo = new System.Windows.Controls.TextBlock { Text = "📷 (无图像)", Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x6B, 0x72, 0x80)), FontSize = 10, Margin = new Thickness(8, 4, 8, 2) };
        _hWindow = new HWindowControl { Focusable = false };
        System.Windows.Controls.Grid.SetRow(_resultInfo, 0);
        System.Windows.Controls.Grid.SetRow(_hWindow, 1);
        previewGrid.Children.Add(_resultInfo);
        previewGrid.Children.Add(_hWindow);
        previewBorder.Child = previewGrid;
        Grid.SetRow(previewBorder, 1);
        grid.Children.Add(previewBorder);

        // 输出信息
        var infoPanel = new System.Windows.Controls.StackPanel { Margin = new Thickness(0, 8, 0, 0) };
        var widthPanel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
        widthPanel.Children.Add(new System.Windows.Controls.TextBlock { Text = "宽度: ", Foreground = System.Windows.Media.Brushes.Gray, FontSize = 11 });
        var widthVal = new System.Windows.Controls.TextBlock { Text = "-", Foreground = System.Windows.Media.Brushes.White, FontSize = 11, Tag = "width" };
        widthPanel.Children.Add(widthVal);
        var heightPanel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(16, 0, 0, 0) };
        heightPanel.Children.Add(new System.Windows.Controls.TextBlock { Text = "高度: ", Foreground = System.Windows.Media.Brushes.Gray, FontSize = 11 });
        var heightVal = new System.Windows.Controls.TextBlock { Text = "-", Foreground = System.Windows.Media.Brushes.White, FontSize = 11, Tag = "height" };
        heightPanel.Children.Add(heightVal);
        infoPanel.Children.Add(widthPanel);
        infoPanel.Children.Add(heightPanel);
        Grid.SetRow(infoPanel, 2);
        grid.Children.Add(infoPanel);

        // 按钮
        var btnPanel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
        var applyBtn = new System.Windows.Controls.Button { Content = "✅ 应用", Width = 80, Margin = new Thickness(0, 0, 8, 0), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x10, 0xB9, 0x81)), Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0), Padding = new Thickness(10, 8, 10, 8), Cursor = System.Windows.Input.Cursors.Hand };
        applyBtn.Click += (_, _) => Apply();
        var okBtn = new System.Windows.Controls.Button { Content = "确定", Width = 80, Margin = new Thickness(0, 0, 8, 0), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3B, 0x82, 0xF6)), Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0), Padding = new Thickness(10, 8, 10, 8), Cursor = System.Windows.Input.Cursors.Hand };
        okBtn.Click += (_, _) => Confirm();
        var cancelBtn = new System.Windows.Controls.Button { Content = "取消", Width = 80, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.Gray, BorderThickness = new Thickness(0), Padding = new Thickness(10, 8, 10, 8), Cursor = System.Windows.Input.Cursors.Hand };
        cancelBtn.Click += (_, _) => Cancel();
        btnPanel.Children.Add(applyBtn);
        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(cancelBtn);
        Grid.SetRow(btnPanel, 3);
        grid.Children.Add(btnPanel);

        Content = grid;
    }

    protected override void LoadFromTool() => _pathBox.Text = _tool.FilePath;

    protected override void SaveToTool() => _tool.FilePath = _pathBox.Text;

    protected override void ExecuteTool()
    {
        var path = _pathBox.Text;
        if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
        {
            _resultInfo.Text = "⚠ 文件不存在";
            return;
        }

        try
        {
            using var img = new HImage();
            img.ReadImage(path);
            var w = img.Width;
            var h = img.Height;

            _hWindow.HalconWindow.SetPart(0, 0, h - 1, w - 1);
            _hWindow.HalconWindow.DispImage(img);

            _resultInfo.Text = $"✅ {System.IO.Path.GetFileName(path)} | {w}×{h}";

            // 更新输出信息
            _tool.OutputSlots[0].Value = img;
            _tool.OutputSlots[1].Value = w;
            _tool.OutputSlots[2].Value = h;
        }
        catch (HalconException hex)
        {
            _resultInfo.Text = $"❌ 加载失败: {hex.Message}";
        }
    }
}

/// <summary>
/// 图像旋转对话框
/// </summary>
public partial class ImageRotateDialog : ToolDialogBase
{
    private readonly ImageRotateTool _tool;
    private System.Windows.Controls.TextBox _angleBox = null!;
    private HWindowControl _hWindow = null!;
    private System.Windows.Controls.TextBlock _resultInfo = null!;

    public ImageRotateDialog(ImageRotateTool tool)
    {
        _tool = tool;
        Title = $"图像旋转 — {tool.Name}";
        Width = 550;
        Height = 400;
        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x0B, 0x11, 0x20));
        BuildUI();
        LoadFromTool();
    }

    private void BuildUI()
    {
        var grid = new System.Windows.Controls.Grid { Margin = new Thickness(16) };
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

        var angleRow = new System.Windows.Controls.Grid();
        angleRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(80) });
        angleRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        angleRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = System.Windows.GridLength.Auto });

        var angleLbl = new System.Windows.Controls.TextBlock { Text = "旋转角度:", Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center };
        System.Windows.Controls.Grid.SetColumn(angleLbl, 0);
        _angleBox = new System.Windows.Controls.TextBox { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.White, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x37, 0x41, 0x51)), Padding = new Thickness(8, 6, 8, 6) };
        System.Windows.Controls.Grid.SetColumn(_angleBox, 1);
        var degLbl = new System.Windows.Controls.TextBlock { Text = "度 (°)", Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
        System.Windows.Controls.Grid.SetColumn(degLbl, 2);
        angleRow.Children.Add(angleLbl);
        angleRow.Children.Add(_angleBox);
        angleRow.Children.Add(degLbl);
        Grid.SetRow(angleRow, 0);
        grid.Children.Add(angleRow);

        var previewBorder = new System.Windows.Controls.Border { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x11, 0x18, 0x27)), CornerRadius = new System.Windows.Media.CornerRadius(8) };
        var previewGrid = new System.Windows.Controls.Grid();
        previewGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        previewGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        _resultInfo = new System.Windows.Controls.TextBlock { Text = "📷 (无输入)", Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x6B, 0x72, 0x80)), FontSize = 10, Margin = new Thickness(8, 4, 8, 2) };
        _hWindow = new HWindowControl { Focusable = false };
        System.Windows.Controls.Grid.SetRow(_resultInfo, 0);
        System.Windows.Controls.Grid.SetRow(_hWindow, 1);
        previewGrid.Children.Add(_resultInfo);
        previewGrid.Children.Add(_hWindow);
        previewBorder.Child = previewGrid;
        Grid.SetRow(previewBorder, 1);
        grid.Children.Add(previewBorder);

        var btnPanel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
        var applyBtn = new System.Windows.Controls.Button { Content = "✅ 应用", Width = 80, Margin = new Thickness(0, 0, 8, 0), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x10, 0xB9, 0x81)), Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0), Padding = new Thickness(10, 8, 10, 8), Cursor = System.Windows.Input.Cursors.Hand };
        applyBtn.Click += (_, _) => Apply();
        var okBtn = new System.Windows.Controls.Button { Content = "确定", Width = 80, Margin = new Thickness(0, 0, 8, 0), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3B, 0x82, 0xF6)), Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0), Padding = new Thickness(10, 8, 10, 8), Cursor = System.Windows.Input.Cursors.Hand };
        okBtn.Click += (_, _) => Confirm();
        var cancelBtn = new System.Windows.Controls.Button { Content = "取消", Width = 80, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.Gray, BorderThickness = new Thickness(0), Padding = new Thickness(10, 8, 10, 8), Cursor = System.Windows.Input.Cursors.Hand };
        cancelBtn.Click += (_, _) => Cancel();
        btnPanel.Children.Add(applyBtn);
        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(cancelBtn);
        Grid.SetRow(btnPanel, 2);
        grid.Children.Add(btnPanel);

        Content = grid;
    }

    protected override void LoadFromTool() => _angleBox.Text = _tool.Angle.ToString();
    protected override void SaveToTool() => _tool.Angle = double.TryParse(_angleBox.Text, out var a) ? a : 0;

    protected override void ExecuteTool()
    {
        var imgSlot = _tool.InputSlots.FirstOrDefault(s => s.Name == "Image");
        var img = imgSlot?.Value as HImage;
        if (img == null || !img.IsInitialized()) { _resultInfo.Text = "⚠ 无输入图像"; return; }

        try
        {
            var angle = double.TryParse(_angleBox.Text, out var a) ? a : 0;
            using var rotated = img.RotateImage(angle * Math.PI / 180, "constant");

            _hWindow.HalconWindow.SetPart(0, 0, rotated.Height - 1, rotated.Width - 1);
            _hWindow.HalconWindow.DispImage(rotated);

            _resultInfo.Text = $"✅ 旋转 {angle:F1}° → {rotated.Width}×{rotated.Height}";
            _tool.OutputSlots[0].Value = rotated;
        }
        catch (HalconException hex) { _resultInfo.Text = $"❌ {hex.Message}"; }
    }
}

/// <summary>
/// ROI绘制对话框
/// </summary>
public partial class ROIDrawDialog : ToolDialogBase
{
    private readonly ROIDrawTool _tool;
    private System.Windows.Controls.ComboBox _typeCombo = null!;
    private System.Windows.Controls.TextBox _p1Box = null!;
    private System.Windows.Controls.TextBox _p2Box = null!;
    private HWindowControl _hWindow = null!;
    private System.Windows.Controls.TextBlock _resultInfo = null!;

    public ROIDrawDialog(ROIDrawTool tool)
    {
        _tool = tool;
        Title = $"ROI绘制 — {tool.Name}";
        Width = 550;
        Height = 420;
        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x0B, 0x11, 0x20));
        BuildUI();
        LoadFromTool();
    }

    private void BuildUI()
    {
        var grid = new System.Windows.Controls.Grid { Margin = new Thickness(16) };
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

        var typeRow = new System.Windows.Controls.Grid();
        typeRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(80) });
        typeRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        var typeLbl = new System.Windows.Controls.TextBlock { Text = "ROI类型:", Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center };
        System.Windows.Controls.Grid.SetColumn(typeLbl, 0);
        _typeCombo = new System.Windows.Controls.ComboBox { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.White };
        _typeCombo.Items.Add("矩形");
        _typeCombo.Items.Add("圆形");
        _typeCombo.Items.Add("线段");
        _typeCombo.SelectedIndex = 0;
        System.Windows.Controls.Grid.SetColumn(_typeCombo, 1);
        typeRow.Children.Add(typeLbl);
        typeRow.Children.Add(_typeCombo);
        Grid.SetRow(typeRow, 0);
        grid.Children.Add(typeRow);

        var p1Row = new System.Windows.Controls.Grid { Margin = new Thickness(0, 8, 0, 0) };
        p1Row.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(80) });
        p1Row.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        var p1Lbl = new System.Windows.Controls.TextBlock { Text = "起点:", Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center };
        System.Windows.Controls.Grid.SetColumn(p1Lbl, 0);
        _p1Box = new System.Windows.Controls.TextBox { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.White, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x37, 0x41, 0x51)), Padding = new Thickness(8, 6, 8, 6), ToolTip = "格式: Row,Col" };
        System.Windows.Controls.Grid.SetColumn(_p1Box, 1);
        p1Row.Children.Add(p1Lbl);
        p1Row.Children.Add(_p1Box);
        Grid.SetRow(p1Row, 1);
        grid.Children.Add(p1Row);

        var p2Row = new System.Windows.Controls.Grid { Margin = new Thickness(0, 8, 0, 0) };
        p2Row.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(80) });
        p2Row.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        var p2Lbl = new System.Windows.Controls.TextBlock { Text = "终点:", Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center };
        System.Windows.Controls.Grid.SetColumn(p2Lbl, 0);
        _p2Box = new System.Windows.Controls.TextBox { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.White, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x37, 0x41, 0x51)), Padding = new Thickness(8, 6, 8, 6), ToolTip = "格式: Row,Col" };
        System.Windows.Controls.Grid.SetColumn(_p2Box, 1);
        p2Row.Children.Add(p2Lbl);
        p2Row.Children.Add(_p2Box);
        Grid.SetRow(p2Row, 2);
        grid.Children.Add(p2Row);

        var previewBorder = new System.Windows.Controls.Border { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x11, 0x18, 0x27)), CornerRadius = new System.Windows.Media.CornerRadius(8), Margin = new Thickness(0, 8, 0, 0) };
        var previewGrid = new System.Windows.Controls.Grid();
        previewGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        previewGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        _resultInfo = new System.Windows.Controls.TextBlock { Text = "📷 (无输入)", Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x6B, 0x72, 0x80)), FontSize = 10, Margin = new Thickness(8, 4, 8, 2) };
        _hWindow = new HWindowControl { Focusable = false };
        System.Windows.Controls.Grid.SetRow(_resultInfo, 0);
        System.Windows.Controls.Grid.SetRow(_hWindow, 1);
        previewGrid.Children.Add(_resultInfo);
        previewGrid.Children.Add(_hWindow);
        previewBorder.Child = previewGrid;
        Grid.SetRow(previewBorder, 2);
        grid.Children.Add(previewBorder);

        var btnPanel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
        var applyBtn = new System.Windows.Controls.Button { Content = "✅ 应用", Width = 80, Margin = new Thickness(0, 0, 8, 0), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x10, 0xB9, 0x81)), Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0), Padding = new Thickness(10, 8, 10, 8), Cursor = System.Windows.Input.Cursors.Hand };
        applyBtn.Click += (_, _) => Apply();
        var okBtn = new System.Windows.Controls.Button { Content = "确定", Width = 80, Margin = new Thickness(0, 0, 8, 0), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3B, 0x82, 0xF6)), Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0), Padding = new Thickness(10, 8, 10, 8), Cursor = System.Windows.Input.Cursors.Hand };
        okBtn.Click += (_, _) => Confirm();
        var cancelBtn = new System.Windows.Controls.Button { Content = "取消", Width = 80, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.Gray, BorderThickness = new Thickness(0), Padding = new Thickness(10, 8, 10, 8), Cursor = System.Windows.Input.Cursors.Hand };
        cancelBtn.Click += (_, _) => Cancel();
        btnPanel.Children.Add(applyBtn);
        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(cancelBtn);
        Grid.SetRow(btnPanel, 3);
        grid.Children.Add(btnPanel);

        Content = grid;
    }

    protected override void LoadFromTool()
    {
        _typeCombo.SelectedIndex = _tool.Type switch { "circle" => 1, "line" => 2, _ => 0 };
        _p1Box.Text = $"{_tool.Row1:F1},{_tool.Col1:F1}";
        _p2Box.Text = $"{_tool.Row2:F1},{_tool.Col2:F1}";
    }

    protected override void SaveToTool()
    {
        _tool.Type = _typeCombo.SelectedIndex switch { 1 => "circle", 2 => "line", _ => "rectangle" };
        ParsePoint(_p1Box.Text, out var r1, out var c1);
        ParsePoint(_p2Box.Text, out var r2, out var c2);
        _tool.Row1 = r1; _tool.Col1 = c1; _tool.Row2 = r2; _tool.Col2 = c2;
    }

    private void ParsePoint(string s, out double r, out double c)
    {
        var parts = s.Split(',');
        r = parts.Length > 0 && double.TryParse(parts[0].Trim(), out var rv) ? rv : 0;
        c = parts.Length > 1 && double.TryParse(parts[1].Trim(), out var cv) ? cv : 0;
    }

    protected override void ExecuteTool()
    {
        var imgSlot = _tool.InputSlots.FirstOrDefault(s => s.Name == "Image");
        var img = imgSlot?.Value as HImage;

        ParsePoint(_p1Box.Text, out var r1, out var c1);
        ParsePoint(_p2Box.Text, out var r2, out var c2);

        if (img != null && img.IsInitialized())
        {
            _hWindow.HalconWindow.SetPart(0, 0, img.Height - 1, img.Width - 1);
            _hWindow.HalconWindow.DispImage(img);
        }

        var type = _typeCombo.SelectedIndex switch { 1 => "circle", 2 => "line", _ => "rectangle" };
        _hWindow.HalconWindow.SetColor("yellow");
        _hWindow.HalconWindow.SetDraw("margin");

        try
        {
            if (type == "rectangle")
            {
                using var roi = new HRegion(new HTuple(r1, r1, r2, r2, r2, r1, r1), new HTuple(c1, c2, c2, c1, c1, c2, c2));
                roi = roi.GenContourRegionXld("border");
                _hWindow.HalconWindow.DispXld(roi);
                _resultInfo.Text = $"✅ 矩形 ROI: ({r1:F1},{c1:F1}) → ({r2:F1},{c2:F1})";
            }
            else if (type == "circle")
            {
                var rad = Math.Sqrt((r2 - r1) * (r2 - r1) + (c2 - c1) * (c2 - c1));
                _hWindow.HalconWindow.DispCircle(r1, c1, rad);
                _resultInfo.Text = $"✅ 圆形 ROI: 中心({r1:F1},{c1:F1}) 半径={rad:F1}";
            }
            else
            {
                _hWindow.HalconWindow.DispLine(r1, c1, r2, c2);
                var len = Math.Sqrt((r2 - r1) * (r2 - r1) + (c2 - c1) * (c2 - c1));
                _resultInfo.Text = $"✅ 线段 ROI: 长度={len:F1}";
            }
        }
        catch (HalconException hex) { _resultInfo.Text = $"❌ {hex.Message}"; }
    }
}
}