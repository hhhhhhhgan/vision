using System.Windows;
using HalconDotNet;
using VisionFlow.Tools;
using System.Linq;

namespace VisionFlow.Views.ToolDialogs {

public partial class PointToPointDistanceDialog : ToolDialogBase
{
    private readonly PointToPointDistanceTool _tool;
    private System.Windows.Controls.TextBox _p1rBox = null!, _p1cBox = null!, _p2rBox = null!, _p2cBox = null!;
    private System.Windows.Controls.TextBlock _resultInfo = null!;

    public PointToPointDistanceDialog(PointToPointDistanceTool tool)
    {
        _tool = tool;
        Title = $"点点距离 — {tool.Name}";
        Width = 400;
        Height = 280;
        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x0B, 0x11, 0x20));
        BuildUI();
        LoadFromTool();
    }

    private void BuildUI()
    {
        var grid = new System.Windows.Controls.Grid { Margin = new Thickness(16) };
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

        // 点1
        var p1Row = new System.Windows.Controls.Grid();
        p1Row.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(80) });
        p1Row.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        p1Row.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        p1Row.Children.Add(new System.Windows.Controls.TextBlock { Text = "点1:", Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center });
        _p1rBox = MakeBox();
        _p1cBox = MakeBox();
        p1Row.Children.Add(_p1rBox);
        p1Row.Children.Add(_p1cBox);
        Grid.SetRow(p1Row, 0);
        grid.Children.Add(p1Row);

        // 点2
        var p2Row = new System.Windows.Controls.Grid { Margin = new Thickness(0, 8, 0, 0) };
        p2Row.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(80) });
        p2Row.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        p2Row.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        p2Row.Children.Add(new System.Windows.Controls.TextBlock { Text = "点2:", Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center });
        _p2rBox = MakeBox();
        _p2cBox = MakeBox();
        p2Row.Children.Add(_p2rBox);
        p2Row.Children.Add(_p2cBox);
        Grid.SetRow(p2Row, 1);
        grid.Children.Add(p2Row);

        _resultInfo = new System.Windows.Controls.TextBlock { Text = "📐 (输入坐标后应用)", Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x6B, 0x72, 0x80)), FontSize = 11, Margin = new Thickness(0, 12, 0, 0) };
        Grid.SetRow(_resultInfo, 2);
        grid.Children.Add(_resultInfo);

        var btnPanel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
        var applyBtn = MakeBtn("✅ 应用", Apply, new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x10, 0xB9, 0x81)));
        var okBtn = MakeBtn("确定", Confirm, new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3B, 0x82, 0xF6)));
        var cancelBtn = MakeBtn("取消", Cancel, new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)));
        btnPanel.Children.Add(applyBtn);
        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(cancelBtn);
        Grid.SetRow(btnPanel, 3);
        grid.Children.Add(btnPanel);

        Content = grid;
    }

    private System.Windows.Controls.TextBox MakeBox() => new System.Windows.Controls.TextBox { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.White, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x37, 0x41, 0x51)), Padding = new Thickness(8, 6, 8, 6), Margin = new Thickness(0, 0, 4, 0) };

    private System.Windows.Controls.Button MakeBtn(string content, Action click, System.Windows.Media.SolidColorBrush bg)
        => new System.Windows.Controls.Button { Content = content, Width = 80, Margin = new Thickness(0, 0, 8, 0), Background = bg, Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0), Padding = new Thickness(10, 8, 10, 8), Cursor = System.Windows.Input.Cursors.Hand, Click = (_, _) => click() };

    protected override void LoadFromTool() { }

    protected override void SaveToTool() { }

    protected override void ExecuteTool()
    {
        var r1 = double.TryParse(_p1rBox.Text, out var v1) ? v1 : 0;
        var c1 = double.TryParse(_p1cBox.Text, out var v2) ? v2 : 0;
        var r2 = double.TryParse(_p2rBox.Text, out var v3) ? v3 : 0;
        var c2 = double.TryParse(_p2cBox.Text, out var v4) ? v4 : 0;
        var dist = Math.Sqrt((r2 - r1) * (r2 - r1) + (c2 - c1) * (c2 - c1));
        _resultInfo.Text = $"📐 点({r1:F1},{c1:F1}) → 点({r2:F1},{c2:F1})\n距离 = {dist:F4}";
        _tool.OutputSlots[0].Value = dist;
    }
}

public partial class PointToLineDistanceDialog : ToolDialogBase
{
    private readonly PointToLineDistanceTool _tool;
    private System.Windows.Controls.TextBox _prBox = null!, _pcBox = null!;
    private System.Windows.Controls.TextBox _l1rBox = null!, _l1cBox = null!, _l2rBox = null!, _l2cBox = null!;
    private System.Windows.Controls.TextBlock _resultInfo = null!;

    public PointToLineDistanceDialog(PointToLineDistanceTool tool)
    {
        _tool = tool;
        Title = $"点线距离 — {tool.Name}";
        Width = 450;
        Height = 320;
        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x0B, 0x11, 0x20));
        BuildUI();
    }

    private void BuildUI()
    {
        var grid = new System.Windows.Controls.Grid { Margin = new Thickness(16) };
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

        var pointRow = new System.Windows.Controls.Grid();
        pointRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(80) });
        pointRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        pointRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        pointRow.Children.Add(new System.Windows.Controls.TextBlock { Text = "点坐标:", Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center });
        _prBox = MakeBox();
        _pcBox = MakeBox();
        pointRow.Children.Add(_prBox);
        pointRow.Children.Add(_pcBox);
        Grid.SetRow(pointRow, 0);
        grid.Children.Add(pointRow);

        var l1Row = new System.Windows.Controls.Grid { Margin = new Thickness(0, 8, 0, 0) };
        l1Row.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(80) });
        l1Row.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        l1Row.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        l1Row.Children.Add(new System.Windows.Controls.TextBlock { Text = "线点1:", Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center });
        _l1rBox = MakeBox();
        _l1cBox = MakeBox();
        l1Row.Children.Add(_l1rBox);
        l1Row.Children.Add(_l1cBox);
        Grid.SetRow(l1Row, 1);
        grid.Children.Add(l1Row);

        var l2Row = new System.Windows.Controls.Grid { Margin = new Thickness(0, 8, 0, 0) };
        l2Row.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(80) });
        l2Row.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        l2Row.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        l2Row.Children.Add(new System.Windows.Controls.TextBlock { Text = "线点2:", Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center });
        _l2rBox = MakeBox();
        _l2cBox = MakeBox();
        l2Row.Children.Add(_l2rBox);
        l2Row.Children.Add(_l2cBox);
        Grid.SetRow(l2Row, 2);
        grid.Children.Add(l2Row);

        _resultInfo = new System.Windows.Controls.TextBlock { Text = "📐 输入后应用", Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x6B, 0x72, 0x80)), FontSize = 11, Margin = new Thickness(0, 12, 0, 0) };
        Grid.SetRow(_resultInfo, 3);
        grid.Children.Add(_resultInfo);

        var btnPanel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
        var applyBtn = MakeBtn("✅ 应用", Apply, new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x10, 0xB9, 0x81)));
        var okBtn = MakeBtn("确定", Confirm, new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3B, 0x82, 0xF6)));
        var cancelBtn = MakeBtn("取消", Cancel, new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)));
        btnPanel.Children.Add(applyBtn);
        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(cancelBtn);

        Content = grid;
    }

    private System.Windows.Controls.TextBox MakeBox() => new System.Windows.Controls.TextBox { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.White, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x37, 0x41, 0x51)), Padding = new Thickness(8, 6, 8, 6), Margin = new Thickness(0, 0, 4, 0) };
    private System.Windows.Controls.Button MakeBtn(string content, Action click, System.Windows.Media.SolidColorBrush bg)
        => new System.Windows.Controls.Button { Content = content, Width = 80, Margin = new Thickness(0, 0, 8, 0), Background = bg, Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0), Padding = new Thickness(10, 8, 10, 8), Cursor = System.Windows.Input.Cursors.Hand, Click = (_, _) => click() };

    protected override void LoadFromTool() { }
    protected override void SaveToTool() { }

    protected override void ExecuteTool()
    {
        var pr = double.TryParse(_prBox.Text, out var v1) ? v1 : 0;
        var pc = double.TryParse(_pcBox.Text, out var v2) ? v2 : 0;
        var lr1 = double.TryParse(_l1rBox.Text, out var v3) ? v3 : 0;
        var lc1 = double.TryParse(_l1cBox.Text, out var v4) ? v4 : 0;
        var lr2 = double.TryParse(_l2rBox.Text, out var v5) ? v5 : 0;
        var lc2 = double.TryParse(_l2cBox.Text, out var v6) ? v6 : 0;

        var dx = lr2 - lr1; var dy = lc2 - lc1;
        var len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 1e-10) { _resultInfo.Text = "❌ 线段长度为零"; return; }

        var t = Math.Max(0, Math.Min(1, ((pr - lr1) * dx + (pc - lc1) * dy) / (len * len)));
        var projR = lr1 + t * dx;
        var projC = lc1 + t * dy;
        var dist = Math.Sqrt((pr - projR) * (pr - projR) + (pc - projC) * (pc - projC));

        _resultInfo.Text = $"📐 点({pr:F1},{pc:F1}) 到线段距离: {dist:F4}";
        _tool.OutputSlots[0].Value = dist;
    }
}

public partial class LineToLineDistanceDialog : ToolDialogBase
{
    private readonly LineToLineDistanceTool _tool;
    private System.Windows.Controls.TextBlock _resultInfo = null!;

    public LineToLineDistanceDialog(LineToLineDistanceTool tool)
    {
        _tool = tool;
        Title = $"线线距离 — {tool.Name}";
        Width = 400;
        Height = 220;
        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x0B, 0x11, 0x20));
        BuildUI();
    }

    private void BuildUI()
    {
        var grid = new System.Windows.Controls.Grid { Margin = new Thickness(16) };
        _resultInfo = new System.Windows.Controls.TextBlock { Text = "📏 线线距离工具\n通过连线传递4个端点坐标自动计算", Foreground = System.Windows.Media.Brushes.Gray, FontSize = 12, TextWrapping = System.Windows.TextWrapping.Wrap, VerticalAlignment = System.Windows.VerticalAlignment.Center, HorizontalAlignment = System.Windows.HorizontalAlignment.Center };
        Grid.SetRow(_resultInfo, 0);
        grid.Children.Add(_resultInfo);

        var btnPanel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Center };
        var okBtn = new System.Windows.Controls.Button { Content = "确定", Width = 100, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3B, 0x82, 0xF6)), Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0), Padding = new Thickness(10, 8, 10, 8), Cursor = System.Windows.Input.Cursors.Hand };
        okBtn.Click += (_, _) => Confirm();
        var cancelBtn = new System.Windows.Controls.Button { Content = "取消", Width = 100, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.Gray, BorderThickness = new Thickness(0), Padding = new Thickness(10, 8, 10, 8), Cursor = System.Windows.Input.Cursors.Hand };
        cancelBtn.Click += (_, _) => Cancel();
        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(cancelBtn);
        Grid.SetRow(btnPanel, 1);
        grid.Children.Add(btnPanel);

        Content = grid;
    }

    protected override void LoadFromTool() { }
    protected override void SaveToTool() { }
    protected override void ExecuteTool() { }
}

public partial class GetGrayValueDialog : ToolDialogBase
{
    private readonly GetGrayValueTool _tool;
    private System.Windows.Controls.TextBox _rowBox = null!, _colBox = null!;
    private HWindowControl _hWindow = null!;
    private System.Windows.Controls.TextBlock _resultInfo = null!;

    public GetGrayValueDialog(GetGrayValueTool tool)
    {
        _tool = tool;
        Title = $"灰度提取 — {tool.Name}";
        Width = 500;
        Height = 350;
        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x0B, 0x11, 0x20));
        BuildUI();
    }

    private void BuildUI()
    {
        var grid = new System.Windows.Controls.Grid { Margin = new Thickness(16) };
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

        var coordRow = new System.Windows.Controls.Grid();
        coordRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(60) });
        coordRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        coordRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(60) });
        coordRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        coordRow.Children.Add(new System.Windows.Controls.TextBlock { Text = "Row:", Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center });
        _rowBox = new System.Windows.Controls.TextBox { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.White, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x37, 0x41, 0x51)), Padding = new Thickness(8, 6, 8, 6) };
        coordRow.Children.Add(_rowBox);
        coordRow.Children.Add(new System.Windows.Controls.TextBlock { Text = "  Col:", Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) });
        _colBox = new System.Windows.Controls.TextBox { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.White, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x37, 0x41, 0x51)), Padding = new Thickness(8, 6, 8, 6) };
        coordRow.Children.Add(_colBox);
        Grid.SetRow(coordRow, 0);
        grid.Children.Add(coordRow);

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

    protected override void LoadFromTool() { }
    protected override void SaveToTool() { }

    protected override void ExecuteTool()
    {
        var imgSlot = _tool.InputSlots.FirstOrDefault(s => s.Name == "Image");
        var img = imgSlot?.Value as HImage;
        if (img == null || !img.IsInitialized()) { _resultInfo.Text = "⚠ 无输入图像"; return; }

        var row = double.TryParse(_rowBox.Text, out var r) ? r : img.Height / 2.0;
        var col = double.TryParse(_colBox.Text, out var c) ? c : img.Width / 2.0;

        try
        {
            var gray = img.GetGrayval(row, col);

            _hWindow.HalconWindow.SetPart(0, 0, img.Height - 1, img.Width - 1);
            _hWindow.HalconWindow.DispImage(img);
            _hWindow.HalconWindow.SetColor("red");
            _hWindow.HalconWindow.DispCross(row, col, 20, 0);

            _resultInfo.Text = $"🖐 坐标({row:F1},{col:F1}) → 灰度值 = {gray.TupleD():F2}";
            _tool.OutputSlots[0].Value = gray.TupleD();
        }
        catch (HalconException hex) { _resultInfo.Text = $"❌ {hex.Message}"; }
    }
}

public partial class CSharpScriptDialog : ToolDialogBase
{
    private readonly CSharpScriptTool _tool;
    private System.Windows.Controls.TextBox _codeBox = null!;

    public CSharpScriptDialog(CSharpScriptTool tool)
    {
        _tool = tool;
        Title = $"C# 脚本 — {tool.Name}";
        Width = 600;
        Height = 400;
        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x0B, 0x11, 0x20));
        BuildUI();
        LoadFromTool();
    }

    private void BuildUI()
    {
        var grid = new System.Windows.Controls.Grid { Margin = new Thickness(16) };
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

        _codeBox = new System.Windows.Controls.TextBox
        {
            AcceptsReturn = true,
            AcceptsTab = true,
            FontFamily = new System.Windows.Media.FontFamily("Consolas"),
            FontSize = 12,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x37, 0x41, 0x51)),
            Padding = new Thickness(8),
            VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto
        };
        Grid.SetRow(_codeBox, 0);
        grid.Children.Add(_codeBox);

        var btnPanel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right, Margin = new Thickness(0, 8, 0, 0) };
        var okBtn = new System.Windows.Controls.Button { Content = "确定", Width = 80, Margin = new Thickness(0, 0, 8, 0), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3B, 0x82, 0xF6)), Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0), Padding = new Thickness(10, 8, 10, 8), Cursor = System.Windows.Input.Cursors.Hand };
        okBtn.Click += (_, _) => Confirm();
        var cancelBtn = new System.Windows.Controls.Button { Content = "取消", Width = 80, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.Gray, BorderThickness = new Thickness(0), Padding = new Thickness(10, 8, 10, 8), Cursor = System.Windows.Input.Cursors.Hand };
        cancelBtn.Click += (_, _) => Cancel();
        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(cancelBtn);
        Grid.SetRow(btnPanel, 1);
        grid.Children.Add(btnPanel);

        Content = grid;
    }

    protected override void LoadFromTool() => _codeBox.Text = _tool.Code;
    protected override void SaveToTool() => _tool.Code = _codeBox.Text;
    protected override void ExecuteTool() { }
}
}