using System.Windows;
using HalconDotNet;
using VisionFlow.Tools;

namespace VisionFlow.Views.ToolDialogs;

/// <summary>
/// 阈值分割工具对话框 — 参数修改后实时预览
/// </summary>
public class ThresholdDialog : ToolDialogBase
{
    private readonly ThresholdTool _tool;
    private HImage? _inputImage;

    // UI控件引用
    private System.Windows.Controls.TextBox _minTextBox = null!;
    private System.Windows.Controls.TextBox _maxTextBox = null!;
    private System.Windows.Controls.ComboBox _modeCombo = null!;
    private System.Windows.Controls.Button _applyBtn = null!;
    private HWindowControl _hWindow = null!;
    private System.Windows.Controls.TextBlock _resultInfo = null!;

    public ThresholdDialog(ThresholdTool tool)
    {
        _tool = tool;
        Title = $"阈值分割 — {tool.Name}";
        Tag = tool; // 用于基类保存名称
        Width = 600;
        Height = 480;
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
        var paramPanel = new System.Windows.Controls.StackPanel { Margin = new Thickness(0, 0, 0, 12) };

        // 工具名称（可编辑）
        var nameRow = new System.Windows.Controls.Grid { Margin = new Thickness(0, 0, 0, 8) };
        nameRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(70) });
        nameRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        nameRow.Children.Add(new System.Windows.Controls.TextBlock { Text = "名称:", Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center });
        _nameBox = new System.Windows.Controls.TextBox { Text = _tool.Name, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.White, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x37, 0x41, 0x51)), Padding = new Thickness(8, 6, 8, 6) };
        nameRow.Children.Add(_nameBox);
        paramPanel.Children.Add(nameRow);

        var minRow = new System.Windows.Controls.Grid();
        minRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(100) });
        minRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        minRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(100) });
        minRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });

        var minLbl = new System.Windows.Controls.TextBlock { Text = "最小灰度:", Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center };
        System.Windows.Controls.Grid.SetColumn(minLbl, 0);
        _minTextBox = new System.Windows.Controls.TextBox { Text = "0", Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.White, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x37, 0x41, 0x51)), Padding = new Thickness(8, 6, 8, 6) };
        System.Windows.Controls.Grid.SetColumn(_minTextBox, 1);

        var maxLbl = new System.Windows.Controls.TextBlock { Text = "最大灰度:", Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center };
        System.Windows.Controls.Grid.SetColumn(maxLbl, 2);
        _maxTextBox = new System.Windows.Controls.TextBox { Text = "255", Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.White, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x37, 0x41, 0x51)), Padding = new Thickness(8, 6, 8, 6) };
        System.Windows.Controls.Grid.SetColumn(_maxTextBox, 3);

        minRow.Children.Add(minLbl);
        minRow.Children.Add(_minTextBox);
        minRow.Children.Add(maxLbl);
        minRow.Children.Add(_maxTextBox);
        paramPanel.Children.Add(minRow);

        // 模式选择
        var modeRow = new System.Windows.Controls.Grid { Margin = new Thickness(0, 8, 0, 0) };
        modeRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(100) });
        modeRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        var modeLbl = new System.Windows.Controls.TextBlock { Text = "阈值模式:", Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center };
        System.Windows.Controls.Grid.SetColumn(modeLbl, 0);
        _modeCombo = new System.Windows.Controls.ComboBox { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.White };
        _modeCombo.Items.Add("全局(MAX)");
        _modeCombo.Items.Add("全局(MIN)");
        _modeCombo.Items.Add("范围(RANGE)");
        _modeCombo.SelectedIndex = 0;
        System.Windows.Controls.Grid.SetColumn(_modeCombo, 1);
        modeRow.Children.Add(modeLbl);
        modeRow.Children.Add(_modeCombo);
        paramPanel.Children.Add(modeRow);

        Grid.SetRow(paramPanel, 0);
        grid.Children.Add(paramPanel);

        // 预览区
        var previewBorder = new System.Windows.Controls.Border { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x11, 0x18, 0x27)), CornerRadius = new System.Windows.Media.CornerRadius(8), Margin = new Thickness(0, 0, 0, 12) };
        var previewGrid = new System.Windows.Controls.Grid();
        previewGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        previewGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        previewGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

        _resultInfo = new System.Windows.Controls.TextBlock { Text = "📷 (无输入图像)", Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x6B, 0x72, 0x80)), FontSize = 10, Margin = new Thickness(8, 4, 8, 2) };
        _hWindow = new HWindowControl { Focusable = false, MinHeight = 200 };
        System.Windows.Controls.Grid.SetRow(_resultInfo, 0);
        System.Windows.Controls.Grid.SetRow(_hWindow, 1);
        previewGrid.Children.Add(_resultInfo);
        previewGrid.Children.Add(_hWindow);

        // 刷新按钮
        var refreshBtn = new System.Windows.Controls.Button { Content = "🔄 刷新预览", HorizontalAlignment = System.Windows.HorizontalAlignment.Right, Margin = new Thickness(0, 0, 8, 4), Padding = new Thickness(12, 4, 12, 4), Cursor = System.Windows.Input.Cursors.Hand };
        refreshBtn.Click += (_, _) => Apply();
        System.Windows.Controls.Grid.SetRow(refreshBtn, 2);
        previewGrid.Children.Add(refreshBtn);

        previewBorder.Child = previewGrid;
        Grid.SetRow(previewBorder, 1);
        grid.Children.Add(previewBorder);

        // 按钮区
        var btnPanel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
        _applyBtn = new System.Windows.Controls.Button { Content = "✅ 应用", Width = 80, Margin = new Thickness(0, 0, 8, 0), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x10, 0xB9, 0x81)), Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0), Padding = new Thickness(10, 8, 10, 8), Cursor = System.Windows.Input.Cursors.Hand };
        _applyBtn.Click += (_, _) => Apply();

        var okBtn = new System.Windows.Controls.Button { Content = "确定", Width = 80, Margin = new Thickness(0, 0, 8, 0), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3B, 0x82, 0xF6)), Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0), Padding = new Thickness(10, 8, 10, 8), Cursor = System.Windows.Input.Cursors.Hand };
        okBtn.Click += (_, _) => Confirm();

        var cancelBtn = new System.Windows.Controls.Button { Content = "取消", Width = 80, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)), Foreground = System.Windows.Media.Brushes.Gray, BorderThickness = new Thickness(0), Padding = new Thickness(10, 8, 10, 8), Cursor = System.Windows.Input.Cursors.Hand };
        cancelBtn.Click += (_, _) => Cancel();

        btnPanel.Children.Add(_applyBtn);
        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(cancelBtn);
        Grid.SetRow(btnPanel, 2);
        grid.Children.Add(btnPanel);

        Content = grid;
    }

    protected override void LoadFromTool()
    {
        _minTextBox.Text = _tool.MinGray.ToString();
        _maxTextBox.Text = _tool.MaxGray.ToString();
        _modeCombo.SelectedIndex = _tool.Mode switch { "min" => 1, "range" => 2, _ => 0 };
    }

    protected override void SaveToTool()
    {
        // 保存工具名称
        if (_nameBox != null)
            _tool.Name = _nameBox.Text;

        _tool.MinGray = double.TryParse(_minTextBox.Text, out var min) ? min : 0;
        _tool.MaxGray = double.TryParse(_maxTextBox.Text, out var max) ? max : 255;
        _tool.Mode = _modeCombo.SelectedIndex switch { 1 => "min", 2 => "range", _ => "max" };
    }

    protected override void ExecuteTool()
    {
        // 获取输入图像（从工具的输入槽）
        var inputSlot = _tool.InputSlots.FirstOrDefault(s => s.Name == "Image");
        _inputImage = inputSlot?.Value as HImage;

        if (_inputImage == null || !_inputImage.IsInitialized())
        {
            _resultInfo.Text = "⚠ 无输入图像（请先运行上游节点）";
            return;
        }

        // 执行阈值分割
        try
        {
            var min = double.TryParse(_minTextBox.Text, out var m) ? m : 0;
            var max = double.TryParse(_maxTextBox.Text, out var mx) ? mx : 255;

            using var region = _inputImage.Threshold(min, max);
            var area = region.AreaCenter(out HTuple row, out HTuple col);

            // 显示结果
            _hWindow.HalconWindow.SetPart(0, 0, _inputImage.Height - 1, _inputImage.Width - 1);
            _hWindow.HalconWindow.DispImage(_inputImage);
            _hWindow.HalconWindow.SetColor("cyan");
            _hWindow.HalconWindow.SetDraw("margin");
            _hWindow.HalconWindow.DispRegion(region);

            // 叠加中心点
            _hWindow.HalconWindow.SetColor("red");
            _hWindow.HalconWindow.DispCross(row, col, 20, 0);

            _resultInfo.Text = $"✅ 阈值: {min:F0}-{max:F0} | 区域数: {region.CountObj()} | 面积: {area:F0}";
        }
        catch (HalconException hex)
        {
            _resultInfo.Text = $"❌ 错误: {hex.Message}";
        }
    }
}