using System.Windows;
using HalconDotNet;
using VisionFlow.Tools;

namespace VisionFlow.Views.ToolDialogs;

/// <summary>
/// 查找直线对话框
/// </summary>
public class FindLineDialog : ToolDialogBase
{
    private readonly FindLineTool _tool;
    private System.Windows.Controls.TextBox _threshBox = null!;
    private System.Windows.Controls.TextBox _lenBox = null!;
    private System.Windows.Controls.TextBox _offsetRowBox = null!;
    private System.Windows.Controls.TextBox _offsetColBox = null!;
    private HWindowControl _hWindow = null!;
    private System.Windows.Controls.TextBlock _resultInfo = null!;

    public FindLineDialog(FindLineTool tool)
    {
        _tool = tool;
        Title = $"查找直线 — {tool.Name}";
        Width = 550;
        Height = 450;
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

        // 参数行
        var paramPanel = new System.Windows.Controls.StackPanel();

        var threshRow = MakeRow("边缘阈值:", _threshBox = new() { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.White, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x37, 0x41, 0x51)), Padding = new Thickness(8, 6, 8, 6) }, "20-255", null);
        paramPanel.Children.Add(threshRow);

        var lenRow = MakeRow("最小长度:", _lenBox = new() { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.White, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x37, 0x41, 0x51)), Padding = new Thickness(8, 6, 8, 6) }, "像素", null);
        paramPanel.Children.Add(lenRow);

        var offRow = new System.Windows.Controls.Grid();
        offRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(100) });
        offRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        offRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(100) });
        offRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        offRow.Children.Add(new System.Windows.Controls.TextBlock { Text = "偏移Row:", Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center });
        _offsetRowBox = new System.Windows.Controls.TextBox { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.White, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x37, 0x41, 0x51)), Padding = new Thickness(8, 6, 8, 6) };
        offRow.Children.Add(_offsetRowBox);
        offRow.Children.Add(new System.Windows.Controls.TextBlock { Text = " 偏移Col:", Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) });
        _offsetColBox = new System.Windows.Controls.TextBox { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.White, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x37, 0x41, 0x51)), Padding = new Thickness(8, 6, 8, 6) };
        offRow.Children.Add(_offsetColBox);
        paramPanel.Children.Add(offRow);

        Grid.SetRow(paramPanel, 0);
        grid.Children.Add(paramPanel);

        // 预览
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

        // 按钮
        var btnPanel = MakeButtons(Apply, Confirm, Cancel);
        Grid.SetRow(btnPanel, 2);
        grid.Children.Add(btnPanel);

        Content = grid;
    }

    private System.Windows.Controls.StackPanel MakeRow(string label, System.Windows.Controls.TextBox box, string unit, System.Windows.Controls.TextBlock? valueTarget)
    {
        var row = new System.Windows.Controls.Grid { Margin = new Thickness(0, 4, 0, 0) };
        row.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(80) });
        row.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        if (unit != null) row.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = System.Windows.GridLength.Auto });
        row.Children.Add(new System.Windows.Controls.TextBlock { Text = label, Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center });
        row.Children.Add(box);
        if (unit != null) row.Children.Add(new System.Windows.Controls.TextBlock { Text = unit, Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center, Margin = new Thickness(6, 0, 0, 0) });
        return row;
    }

    private System.Windows.Controls.StackPanel MakeButtons(Action apply, Action confirm, Action cancel)
    {
        var panel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
        var applyBtn = MakeBtn("✅ 应用", () => apply(), new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x10, 0xB9, 0x81)));
        var okBtn = MakeBtn("确定", () => confirm(), new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3B, 0x82, 0xF6)));
        var cancelBtn = MakeBtn("取消", () => cancel(), new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)));
        panel.Children.Add(applyBtn);
        panel.Children.Add(okBtn);
        panel.Children.Add(cancelBtn);
        return panel;
    }

    private System.Windows.Controls.Button MakeBtn(string content, Action click, System.Windows.Media.SolidColorBrush bg)
        => new System.Windows.Controls.Button { Content = content, Width = 80, Margin = new Thickness(0, 0, 8, 0), Background = bg, Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0), Padding = new Thickness(10, 8, 10, 8), Cursor = System.Windows.Input.Cursors.Hand, Click = (_, _) => click() };

    protected override void LoadFromTool()
    {
        _threshBox.Text = _tool.EdgeThreshold.ToString();
        _lenBox.Text = _tool.MinLineLength.ToString();
        _offsetRowBox.Text = _tool.OffsetRow.ToString();
        _offsetColBox.Text = _tool.OffsetCol.ToString();
    }

    protected override void SaveToTool()
    {
        _tool.EdgeThreshold = double.TryParse(_threshBox.Text, out var t) ? t : 20;
        _tool.MinLineLength = double.TryParse(_lenBox.Text, out var l) ? l : 20;
        _tool.OffsetRow = double.TryParse(_offsetRowBox.Text, out var r) ? r : 0;
        _tool.OffsetCol = double.TryParse(_offsetColBox.Text, out var c) ? c : 0;
    }

    protected override void ExecuteTool()
    {
        var imgSlot = _tool.InputSlots.FirstOrDefault(s => s.Name == "Image");
        var img = imgSlot?.Value as HImage;
        if (img == null || !img.IsInitialized()) { _resultInfo.Text = "⚠ 无输入图像"; return; }

        try
        {
            var thresh = double.TryParse(_threshBox.Text, out var t) ? t : 20;
            var len = double.TryParse(_lenBox.Text, out var l) ? l : 20;

            using var edges = img.EdgesSubPix("canny", thresh, 20, 40);
            using var lines = edges.SegmentContoursXld("fit_line", 20, -0.5, 0.5, 5, 5, "attr_forest");
            var numLines = lines.CountObj();

            _hWindow.HalconWindow.SetPart(0, 0, img.Height - 1, img.Width - 1);
            _hWindow.HalconWindow.DispImage(img);
            _hWindow.HalconWindow.SetColor("lime");
            _hWindow.HalconWindow.DispXld(lines);

            _resultInfo.Text = $"✅ 找到 {numLines} 条直线 (阈值={thresh:F0}, 最小长度={len:F0})";
        }
        catch (HalconException hex) { _resultInfo.Text = $"❌ {hex.Message}"; }
    }
}

/// <summary>
/// 查找圆对话框
/// </summary>
public class FindCircleDialog : ToolDialogBase
{
    private readonly FindCircleTool _tool;
    private System.Windows.Controls.TextBox _threshBox = null!;
    private System.Windows.Controls.TextBox _minRBox = null!;
    private System.Windows.Controls.TextBox _maxRBox = null!;
    private HWindowControl _hWindow = null!;
    private System.Windows.Controls.TextBlock _resultInfo = null!;

    public FindCircleDialog(FindCircleTool tool)
    {
        _tool = tool;
        Title = $"查找圆 — {tool.Name}";
        Width = 550;
        Height = 450;
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

        var paramPanel = new System.Windows.Controls.StackPanel();

        var threshRow = new System.Windows.Controls.Grid { Margin = new Thickness(0, 0, 0, 8) };
        threshRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(80) });
        threshRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        threshRow.Children.Add(new System.Windows.Controls.TextBlock { Text = "边缘阈值:", Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center });
        _threshBox = new System.Windows.Controls.TextBox { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.White, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x37, 0x41, 0x51)), Padding = new Thickness(8, 6, 8, 6) };
        threshRow.Children.Add(_threshBox);
        paramPanel.Children.Add(threshRow);

        var radiusRow = new System.Windows.Controls.Grid();
        radiusRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(80) });
        radiusRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        radiusRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(80) });
        radiusRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        radiusRow.Children.Add(new System.Windows.Controls.TextBlock { Text = "最小半径:", Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center });
        _minRBox = new System.Windows.Controls.TextBox { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.White, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x37, 0x41, 0x51)), Padding = new Thickness(8, 6, 8, 6) };
        radiusRow.Children.Add(_minRBox);
        radiusRow.Children.Add(new System.Windows.Controls.TextBlock { Text = "最大半径:", Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) });
        _maxRBox = new System.Windows.Controls.TextBox { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.White, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x37, 0x41, 0x51)), Padding = new Thickness(8, 6, 8, 6) };
        radiusRow.Children.Add(_maxRBox);
        paramPanel.Children.Add(radiusRow);

        Grid.SetRow(paramPanel, 0);
        grid.Children.Add(paramPanel);

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

    protected override void LoadFromTool()
    {
        _threshBox.Text = _tool.EdgeThreshold.ToString();
        _minRBox.Text = _tool.MinRadius.ToString();
        _maxRBox.Text = _tool.MaxRadius.ToString();
    }

    protected override void SaveToTool()
    {
        _tool.EdgeThreshold = double.TryParse(_threshBox.Text, out var t) ? t : 20;
        _tool.MinRadius = double.TryParse(_minRBox.Text, out var mr) ? mr : 5;
        _tool.MaxRadius = double.TryParse(_maxRBox.Text, out var mx) ? mx : 100;
    }

    protected override void ExecuteTool()
    {
        var imgSlot = _tool.InputSlots.FirstOrDefault(s => s.Name == "Image");
        var img = imgSlot?.Value as HImage;
        if (img == null || !img.IsInitialized()) { _resultInfo.Text = "⚠ 无输入图像"; return; }

        try
        {
            var thresh = double.TryParse(_threshBox.Text, out var t) ? t : 20;
            var minR = double.TryParse(_minRBox.Text, out var mr) ? mr : 5;
            var maxR = double.TryParse(_maxRBox.Text, out var mx) ? mx : 100;

            using var edges = img.EdgesSubPix("canny", thresh, 20, 40);
            using var circles = edges.SegmentContoursXld("fit_circle", 20, -0.5, 0.5, 5, 5, "attr_forest");
            var numCircles = circles.CountObj();

            _hWindow.HalconWindow.SetPart(0, 0, img.Height - 1, img.Width - 1);
            _hWindow.HalconWindow.DispImage(img);
            _hWindow.HalconWindow.SetColor("cyan");
            _hWindow.HalconWindow.DispXld(circles);

            _resultInfo.Text = $"✅ 找到 {numCircles} 个圆 (半径范围: {minR:F0}-{maxR:F0})";
        }
        catch (HalconException hex) { _resultInfo.Text = $"❌ {hex.Message}"; }
    }
}

/// <summary>
/// 卡尺测量对话框
/// </summary>
public class CaliperMeasureDialog : ToolDialogBase
{
    private readonly CaliperMeasureTool _tool;
    private System.Windows.Controls.TextBox _countBox = null!;
    private System.Windows.Controls.TextBox _threshBox = null!;
    private HWindowControl _hWindow = null!;
    private System.Windows.Controls.TextBlock _resultInfo = null!;

    public CaliperMeasureDialog(CaliperMeasureTool tool)
    {
        _tool = tool;
        Title = $"卡尺测量 — {tool.Name}";
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
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

        var paramPanel = new System.Windows.Controls.StackPanel();

        var countRow = new System.Windows.Controls.Grid { Margin = new Thickness(0, 0, 0, 8) };
        countRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(80) });
        countRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        countRow.Children.Add(new System.Windows.Controls.TextBlock { Text = "卡尺数量:", Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center });
        _countBox = new System.Windows.Controls.TextBox { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.White, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x37, 0x41, 0x51)), Padding = new Thickness(8, 6, 8, 6) };
        countRow.Children.Add(_countBox);
        paramPanel.Children.Add(countRow);

        var threshRow = new System.Windows.Controls.Grid { Margin = new Thickness(0, 0, 0, 8) };
        threshRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(80) });
        threshRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        threshRow.Children.Add(new System.Windows.Controls.TextBlock { Text = "边缘阈值:", Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center });
        _threshBox = new System.Windows.Controls.TextBox { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.White, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x37, 0x41, 0x51)), Padding = new Thickness(8, 6, 8, 6) };
        threshRow.Children.Add(_threshBox);
        paramPanel.Children.Add(threshRow);

        Grid.SetRow(paramPanel, 0);
        grid.Children.Add(paramPanel);

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

    protected override void LoadFromTool()
    {
        _countBox.Text = _tool.NumMeasures.ToString();
        _threshBox.Text = _tool.EdgeThreshold.ToString();
    }

    protected override void SaveToTool()
    {
        _tool.NumMeasures = int.TryParse(_countBox.Text, out var n) ? n : 20;
        _tool.EdgeThreshold = double.TryParse(_threshBox.Text, out var t) ? t : 20;
    }

    protected override void ExecuteTool()
    {
        var imgSlot = _tool.InputSlots.FirstOrDefault(s => s.Name == "Image");
        var img = imgSlot?.Value as HImage;
        if (img == null || !img.IsInitialized()) { _resultInfo.Text = "⚠ 无输入图像"; return; }

        try
        {
            var count = int.TryParse(_countBox.Text, out var n) ? n : 20;
            var thresh = double.TryParse(_threshBox.Text, out var t) ? t : 20;

            _hWindow.HalconWindow.SetPart(0, 0, img.Height - 1, img.Width - 1);
            _hWindow.HalconWindow.DispImage(img);
            _hWindow.HalconWindow.SetColor("yellow");

            // 简单演示：画测量线示意
            var midR = img.Height / 2.0;
            var midC = img.Width / 2.0;
            _hWindow.HalconWindow.DispLine(midR - 50, midC - 100, midR + 50, midC + 100);

            _resultInfo.Text = $"✅ 卡尺测量 (数量={count}, 阈值={thresh:F0})";
        }
        catch (HalconException hex) { _resultInfo.Text = $"❌ {hex.Message}"; }
    }
}