using System.Windows;
using HalconDotNet;
using VisionFlow.Tools;

namespace VisionFlow.Views.ToolDialogs;

/// <summary>
/// 形态学工具对话框
/// </summary>
public partial class MorphologyDialog : ToolDialogBase
{
    private readonly MorphologyTool _tool;
    private HImage? _inputImage;
    private HRegion? _inputRegion;

    private System.Windows.Controls.ComboBox _opCombo = null!;
    private System.Windows.Controls.TextBox _radiusBox = null!;
    private System.Windows.Controls.TextBox _iterBox = null!;
    private HWindowControl _hWindow = null!;
    private System.Windows.Controls.TextBlock _resultInfo = null!;

    public MorphologyDialog(MorphologyTool tool)
    {
        _tool = tool;
        Title = $"形态学 — {tool.Name}";
        Width = 600;
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

        // 参数区
        var paramPanel = new System.Windows.Controls.StackPanel();

        var opRow = new System.Windows.Controls.Grid();
        opRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(100) });
        opRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        var opLbl = new System.Windows.Controls.TextBlock { Text = "操作:", Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center };
        System.Windows.Controls.Grid.SetColumn(opLbl, 0);
        _opCombo = new System.Windows.Controls.ComboBox { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.White };
        _opCombo.Items.Add("腐蚀(Dilation)");
        _opCombo.Items.Add("膨胀(Erosion)");
        _opCombo.Items.Add("开(Opening)");
        _opCombo.Items.Add("闭(Closing)");
        _opCombo.SelectedIndex = 2;
        System.Windows.Controls.Grid.SetColumn(_opCombo, 1);
        opRow.Children.Add(opLbl);
        opRow.Children.Add(_opCombo);
        paramPanel.Children.Add(opRow);

        var radiusRow = new System.Windows.Controls.Grid { Margin = new Thickness(0, 8, 0, 0) };
        radiusRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(100) });
        radiusRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        var radiusLbl = new System.Windows.Controls.TextBlock { Text = "结构元素:", Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center };
        System.Windows.Controls.Grid.SetColumn(radiusLbl, 0);
        _radiusBox = new System.Windows.Controls.TextBox { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.White, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x37, 0x41, 0x51)), Padding = new Thickness(8, 6, 8, 6) };
        System.Windows.Controls.Grid.SetColumn(_radiusBox, 1);
        radiusRow.Children.Add(radiusLbl);
        radiusRow.Children.Add(_radiusBox);
        paramPanel.Children.Add(radiusRow);

        var iterRow = new System.Windows.Controls.Grid { Margin = new Thickness(0, 8, 0, 0) };
        iterRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(100) });
        iterRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        var iterLbl = new System.Windows.Controls.TextBlock { Text = "迭代次数:", Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center };
        System.Windows.Controls.Grid.SetColumn(iterLbl, 0);
        _iterBox = new System.Windows.Controls.TextBox { Text = "1", Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.White, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x37, 0x41, 0x51)), Padding = new Thickness(8, 6, 8, 6) };
        System.Windows.Controls.Grid.SetColumn(_iterBox, 1);
        iterRow.Children.Add(iterLbl);
        iterRow.Children.Add(_iterBox);
        paramPanel.Children.Add(iterRow);

        Grid.SetRow(paramPanel, 0);
        grid.Children.Add(paramPanel);

        // 预览区
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
        _opCombo.SelectedIndex = _tool.Operation switch { "dilation" => 0, "erosion" => 1, "opening" => 2, "closing" => 3, _ => 2 };
        _radiusBox.Text = _tool.ElementSize.ToString();
        _iterBox.Text = _tool.Iterations.ToString();
    }

    protected override void SaveToTool()
    {
        _tool.Operation = _opCombo.SelectedIndex switch { 0 => "dilation", 1 => "erosion", 2 => "opening", 3 => "closing", _ => "opening" };
        _tool.ElementSize = double.TryParse(_radiusBox.Text, out var r) ? r : 3;
        _tool.Iterations = int.TryParse(_iterBox.Text, out var i) ? i : 1;
    }

    protected override void ExecuteTool()
    {
        var imgSlot = _tool.InputSlots.FirstOrDefault(s => s.Name == "Image");
        var regSlot = _tool.InputSlots.FirstOrDefault(s => s.Name == "Region");
        _inputImage = imgSlot?.Value as HImage;
        _inputRegion = regSlot?.Value as HRegion;

        if (_inputRegion == null || !_inputRegion.IsInitialized())
        {
            _resultInfo.Text = "⚠ 无输入区域（请先运行上游节点）";
            return;
        }

        try
        {
            var op = _opCombo.SelectedIndex switch { 0 => "dilation", 1 => "erosion", 2 => "opening", 3 => "closing", _ => "opening" };
            var radius = double.TryParse(_radiusBox.Text, out var r) ? r : 3;
            var iter = int.TryParse(_iterBox.Text, out var i) ? i : 1;

            using var elem = new HRegion(radius, radius);
            elem.GenCircle(radius / 2);

            HRegion result = op switch
            {
                "opening" => _inputRegion.Opening(elem, iter),
                "closing" => _inputRegion.Closing(elem, iter),
                "erosion" => _inputRegion.Erosion(elem, iter),
                _ => _inputRegion.Dilation(elem, iter)
            };

            var area = result.AreaCenter(out HTuple row, out HTuple col);

            if (_inputImage != null && _inputImage.IsInitialized())
            {
                _hWindow.HalconWindow.SetPart(0, 0, _inputImage.Height - 1, _inputImage.Width - 1);
                _hWindow.HalconWindow.DispImage(_inputImage);
            }
            _hWindow.HalconWindow.SetColor("lime");
            _hWindow.HalconWindow.SetDraw("margin");
            _hWindow.HalconWindow.DispRegion(result);
            _hWindow.HalconWindow.SetColor("red");
            _hWindow.HalconWindow.DispCross(row, col, 20, 0);

            _resultInfo.Text = $"✅ {op} | 区域数: {result.CountObj()} | 面积: {area:F0}";
        }
        catch (HalconException hex)
        {
            _resultInfo.Text = $"❌ 错误: {hex.Message}";
        }
    }
}

/// <summary>
/// Blob分析工具对话框
/// </summary>
public partial class BlobAnalysisDialog : ToolDialogBase
{
    private readonly BlobAnalysisTool _tool;
    private HImage? _inputImage;
    private HRegion? _inputRegion;

    private System.Windows.Controls.TextBox _minAreaBox = null!;
    private System.Windows.Controls.TextBox _maxAreaBox = null!;
    private System.Windows.Controls.ComboBox _sortCombo = null!;
    private HWindowControl _hWindow = null!;
    private System.Windows.Controls.TextBlock _resultInfo = null!;
    private System.Windows.Controls.ListBox _blobList = null!;

    public BlobAnalysisDialog(BlobAnalysisTool tool)
    {
        _tool = tool;
        Title = $"Blob分析 — {tool.Name}";
        Width = 650;
        Height = 500;
        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x0B, 0x11, 0x20));
        BuildUI();
        LoadFromTool();
    }

    private void BuildUI()
    {
        var grid = new System.Windows.Controls.Grid { Margin = new Thickness(16) };
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(200) });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

        // 参数区
        var paramPanel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };

        var minRow = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 16, 0) };
        minRow.Children.Add(new System.Windows.Controls.TextBlock { Text = "最小面积:", Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center, Width = 70 });
        _minAreaBox = new System.Windows.Controls.TextBox { Width = 80, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.White, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x37, 0x41, 0x51)), Padding = new Thickness(6, 4, 6, 4) };
        minRow.Children.Add(_minAreaBox);
        paramPanel.Children.Add(minRow);

        var maxRow = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 16, 0) };
        maxRow.Children.Add(new System.Windows.Controls.TextBlock { Text = "最大面积:", Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center, Width = 70 });
        _maxAreaBox = new System.Windows.Controls.TextBox { Width = 80, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.White, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x37, 0x41, 0x51)), Padding = new Thickness(6, 4, 6, 4) };
        maxRow.Children.Add(_maxAreaBox);
        paramPanel.Children.Add(maxRow);

        var sortRow = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
        sortRow.Children.Add(new System.Windows.Controls.TextBlock { Text = "排序:", Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center, Width = 40 });
        _sortCombo = new System.Windows.Controls.ComboBox { Width = 80, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.White };
        _sortCombo.Items.Add("面积");
        _sortCombo.Items.Add("列");
        _sortCombo.Items.Add("行");
        _sortCombo.SelectedIndex = 0;
        sortRow.Children.Add(_sortCombo);
        paramPanel.Children.Add(sortRow);

        Grid.SetRow(paramPanel, 0);
        grid.Children.Add(paramPanel);

        // 预览区
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

        // Blob列表
        _blobList = new System.Windows.Controls.ListBox { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.White, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x37, 0x41, 0x51)), FontSize = 11 };
        Grid.SetRow(_blobList, 2);
        grid.Children.Add(_blobList);

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

    protected override void LoadFromTool()
    {
        _minAreaBox.Text = _tool.MinArea.ToString();
        _maxAreaBox.Text = _tool.MaxArea.ToString();
        _sortCombo.SelectedIndex = _tool.SortBy switch { "column" => 1, "row" => 2, _ => 0 };
    }

    protected override void SaveToTool()
    {
        _tool.MinArea = double.TryParse(_minAreaBox.Text, out var m) ? m : 10;
        _tool.MaxArea = double.TryParse(_maxAreaBox.Text, out var mx) ? mx : 9999999;
        _tool.SortBy = _sortCombo.SelectedIndex switch { 1 => "column", 2 => "row", _ => "area" };
    }

    protected override void ExecuteTool()
    {
        var imgSlot = _tool.InputSlots.FirstOrDefault(s => s.Name == "Image");
        var regSlot = _tool.InputSlots.FirstOrDefault(s => s.Name == "Region");
        _inputImage = imgSlot?.Value as HImage;
        _inputRegion = regSlot?.Value as HRegion;

        if (_inputRegion == null || !_inputRegion.IsInitialized())
        {
            _resultInfo.Text = "⚠ 无输入区域";
            return;
        }

        try
        {
            var minArea = double.TryParse(_minAreaBox.Text, out var m) ? m : 10;
            var maxArea = double.TryParse(_maxAreaBox.Text, out var mx) ? mx : 9999999;
            var sortBy = _sortCombo.SelectedIndex switch { 1 => "column", 2 => "row", _ => "area" };

            using var filtered = _inputRegion.SelectShapeProto("area", "and", minArea, maxArea);
            using var connected = filtered.Connection();

            if (!connected.IsInitialized() || connected.CountObj() == 0)
            {
                _resultInfo.Text = "⚠ 无符合条件的区域";
                _blobList.Items.Clear();
                return;
            }

            var areaArr = connected.AreaCenter(out HTuple rowArr, out HTuple colArr);
            var periArr = connected.Perimeter();

            // 排序
            if (sortBy == "area")
            {
                var idx = areaArr.TupleSortIndex().TupleReverse();
                rowArr = rowArr.TupleSelect(idx);
                colArr = colArr.TupleSelect(idx);
                areaArr = areaArr.TupleSelect(idx);
                periArr = periArr.TupleSelect(idx);
            }

            // 显示
            if (_inputImage != null && _inputImage.IsInitialized())
            {
                _hWindow.HalconWindow.SetPart(0, 0, _inputImage.Height - 1, _inputImage.Width - 1);
                _hWindow.HalconWindow.DispImage(_inputImage);
            }
            _hWindow.HalconWindow.SetColor("cyan");
            _hWindow.HalconWindow.SetDraw("fill");
            _hWindow.HalconWindow.DispRegion(connected);

            // 标号
            _hWindow.HalconWindow.SetColor("yellow");
            for (int i = 0; i < Math.Min(rowArr.Length, 20); i++)
            {
                _hWindow.HalconWindow.DispCross(rowArr[i].D, colArr[i].D, 16, 0);
                _hWindow.HalconWindow.DispText($"#{i + 1}", "image", rowArr[i].D - 10, colArr[i].D - 10, "green", new HTuple(), new HTuple());
            }

            _resultInfo.Text = $"✅ 区域数: {connected.CountObj()} | 最大: {areaArr.TupleMax():F0} | 最小: {areaArr.TupleMin():F0}";

            // Blob列表
            _blobList.Items.Clear();
            for (int i = 0; i < Math.Min(rowArr.Length, 50); i++)
                _blobList.Items.Add($"#{i + 1}: Area={areaArr[i].D:F0}, Row={rowArr[i].D:F1}, Col={colArr[i].D:F1}");
        }
        catch (HalconException hex)
        {
            _resultInfo.Text = $"❌ 错误: {hex.Message}";
        }
    }
}