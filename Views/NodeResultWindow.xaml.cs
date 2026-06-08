using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using HalconDotNet;
using VisionFlow.Models;

namespace VisionFlow.Views;

/// <summary>
/// 节点输出显示窗口 — 单步运行结果显示
/// </summary>
public class NodeResultWindow : Window
{
    private HWindowControl? _hWindow;
    private FlowNodeViewModel? _currentNode;
    private FlowNode? _lastNode;
    private readonly DispatcherTimer _refreshTimer;
    private TextBlock? _statusLabel;
    private TextBlock? _errorText;
    private TextBlock? _imageTitle;
    private StackPanel? _outputValuesPanel;

    public NodeResultWindow()
    {
        Title = "节点结果";
        Width = 480;
        Height = 520;
        WindowStartupLocation = WindowStartupLocation.Right;
        Background = new SolidColorBrush(Color.FromRgb(0x0B, 0x11, 0x20));

        BuildUI();

        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _refreshTimer.Tick += (_, _) => RefreshIfNeeded();
    }

    private void BuildUI()
    {
        var grid = new Grid { Margin = new Thickness(16) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(200) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // 标题栏
        var header = new Border { Background = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27)), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 0, 0, 8) };
        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var titleText = new TextBlock { Text = "🔍 节点执行结果", Foreground = Brushes.White, FontSize = 13, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
        _statusLabel = new TextBlock { FontSize = 14, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
        headerGrid.Children.Add(titleText);
        headerGrid.Children.Add(_statusLabel);
        header.Child = headerGrid;
        Grid.SetRow(header, 0);
        grid.Children.Add(header);

        // 错误信息
        _errorText = new TextBlock { Foreground = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)), FontSize = 11, TextWrapping = TextWrapping.Wrap, Visibility = Visibility.Collapsed, Margin = new Thickness(0, 0, 0, 8) };
        Grid.SetRow(_errorText, 1);
        grid.Children.Add(_errorText);

        // HALCON 窗口
        var hWinBorder = new Border { Background = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27)), CornerRadius = new CornerRadius(8), Margin = new Thickness(0, 0, 0, 8) };
        var hWinGrid = new Grid();
        hWinGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        hWinGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        _imageTitle = new TextBlock { Text = "📷 (无输出)", Foreground = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80)), FontSize = 10, Margin = new Thickness(8, 4, 8, 2) };
        _hWindow = new HWindowControl { Focusable = false };
        Grid.SetRow(_imageTitle, 0);
        Grid.SetRow(_hWindow, 1);
        hWinGrid.Children.Add(_imageTitle);
        hWinGrid.Children.Add(_hWindow);
        hWinBorder.Child = hWinGrid;
        Grid.SetRow(hWinBorder, 2);
        grid.Children.Add(hWinBorder);

        // 输出标签
        var outLabel = new TextBlock { Text = "📊 输出值:", Foreground = new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF)), FontSize = 11, Margin = new Thickness(0, 0, 0, 4) };
        Grid.SetRow(outLabel, 3);
        grid.Children.Add(outLabel);

        // 输出列表
        _outputValuesPanel = new StackPanel();
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = _outputValuesPanel };
        Grid.SetRow(scroll, 4);
        grid.Children.Add(scroll);

        Content = grid;
    }

    public void AttachToNode(FlowNodeViewModel nodeVm)
    {
        _currentNode = nodeVm;
        _currentNode.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FlowNodeViewModel.Node))
                Dispatcher.Invoke(RefreshDisplay);
        };

        Title = $"结果: {nodeVm.Node.Tool.Name}";
        RefreshDisplay();
    }

    private void RefreshIfNeeded()
    {
        if (_currentNode == null) return;
        _refreshTimer.Stop();
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        if (_currentNode == null) return;

        var node = _currentNode.Node;
        var status = node.ExecutionStatus;

        // 状态
        _statusLabel!.Text = status switch
        {
            NodeExecutionStatus.Running => "⏳ 运行中...",
            NodeExecutionStatus.Success => "✅ 成功",
            NodeExecutionStatus.Error => "❌ 失败",
            _ => "⭕ 空闲"
        };

        _statusLabel.Foreground = status switch
        {
            NodeExecutionStatus.Running => Brushes.Orange,
            NodeExecutionStatus.Success => new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)),
            NodeExecutionStatus.Error => new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)),
            _ => Brushes.Gray
        };

        // 错误信息
        if (status == NodeExecutionStatus.Error && !string.IsNullOrEmpty(node.LastErrorMessage))
        {
            _errorText!.Text = node.LastErrorMessage;
            _errorText.Visibility = Visibility.Visible;
        }
        else
        {
            if (_errorText != null) _errorText.Visibility = Visibility.Collapsed;
        }

        if (_lastNode == node) return;
        _lastNode = node;

        // 显示输出值
        _outputValuesPanel?.Children.Clear();
        foreach (var slot in node.Tool.OutputSlots)
        {
            if (slot.Value == null) continue;

            var valueStr = FormatValue(slot.Value);
            var item = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
            item.Children.Add(new TextBlock { Text = $"{slot.Name}:", Foreground = Brushes.Gray, Width = 90, FontSize = 11 });
            item.Children.Add(new TextBlock { Text = valueStr, Foreground = Brushes.White, FontSize = 11, FontWeight = FontWeights.Medium });
            _outputValuesPanel?.Children.Add(item);

            DisplayHalconObject(slot.Value, slot.Name);
        }
    }

    private string FormatValue(object? value)
    {
        if (value == null) return "null";
        if (value is HTuple tuple)
        {
            if (tuple.Length == 0) return "empty";
            if (tuple.Length == 1) return $"{tuple[0]}";
            if (tuple.Length <= 5) return string.Join(", ", tuple.ToArray().Select(v => v.ToString()));
            return $"[{tuple.Length} values]";
        }
        if (value is double d) return $"{d:F4}";
        if (value is int i) return i.ToString();
        if (value is string s) return s.Length > 50 ? s[..50] + "..." : s;
        if (value is bool b) return b.ToString();
        return $"[{value.GetType().Name}]";
    }

    private void DisplayHalconObject(object? value, string slotName)
    {
        if (_hWindow == null || value == null) return;
        try
        {
            _hWindow.HalconWindow.SetColor("green");
            _hWindow.HalconWindow.SetDraw("margin");

            if (value is HImage img && img.IsInitialized())
            {
                img.GetImageSize(out HTuple w, out HTuple h);
                _hWindow.HalconWindow.SetPart(0, 0, h - 1, w - 1);
                _hWindow.HalconWindow.DispImage(img);
                if (_imageTitle != null) _imageTitle.Text = $"📷 {slotName}: {w}×{h}";
            }
            else if (value is HRegion region && region.IsInitialized())
            {
                _hWindow.HalconWindow.SetColor("cyan");
                _hWindow.HalconWindow.DispRegion(region);
                if (_imageTitle != null) _imageTitle.Text = $"🔲 {slotName}: {region.CountObj()} regions";
            }
            else if (value is HXLD xld && xld.IsInitialized())
            {
                _hWindow.HalconWindow.SetColor("yellow");
                _hWindow.HalconWindow.DispXld(xld);
                if (_imageTitle != null) _imageTitle.Text = $"📐 {slotName}: XLD";
            }
        }
        catch { }
    }

    public void ClearDisplay()
    {
        if (_hWindow == null) return;
        try { _hWindow.HalconWindow.ClearWindow(); } catch { }
        if (_imageTitle != null) _imageTitle.Text = "📷 (无输出)";
    }
}