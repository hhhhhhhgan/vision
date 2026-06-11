using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using VisionFlow.Models;
using VisionFlow.Tools;
using VisionFlow.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace VisionFlow.Views {

public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;
    private FlowNodeViewModel? _draggingNode;
    private Border? _draggingBorder;
    private Point _dragStartPoint;

    private bool _isConnecting;
    private FlowNodeViewModel? _connectingFromNode;
    private string? _connectingFromSlot;
    private string? _connectingFromType;
    private Line? _tempLine;

    private readonly Dictionary<string, Border> _nodeBorders = new();

    // 分类折叠状态
    private readonly Dictionary<ToolCategory, bool> _categoryCollapsed = new();

    // 颜色
    private static readonly SolidColorBrush NodeBg = new(Color.FromRgb(0x1F, 0x29, 0x37));
    private static readonly SolidColorBrush NodeBorder = new(Color.FromRgb(0x3B, 0x82, 0xF6));
    private static readonly SolidColorBrush NodeSelectedBorder = new(Color.FromRgb(0x06, 0xB6, 0xD4));
    private static readonly SolidColorBrush NodeTitleBg = new(Color.FromRgb(0x11, 0x18, 0x27));
    private static readonly SolidColorBrush PortInput = new(Color.FromRgb(0x10, 0xB9, 0x81));
    private static readonly SolidColorBrush PortOutput = new(Color.FromRgb(0x3B, 0x82, 0xF6));
    private static readonly SolidColorBrush PortConnected = new(Color.FromRgb(0xF5, 0x9E, 0x0B));
    private static readonly SolidColorBrush TextPrimary = new(Color.FromRgb(0xF9, 0xFA, 0xFB));
    private static readonly SolidColorBrush TextMuted = new(Color.FromRgb(0x9C, 0xA3, 0xAF));
    private static readonly SolidColorBrush TextDim = new(Color.FromRgb(0x6B, 0x72, 0x80));

    private NodeResultWindow? _resultWindow;

    public MainWindow()
    {
        InitializeComponent();
        ViewModel.Nodes.CollectionChanged += (_, _) => RedrawAll();
        ViewModel.Connections.CollectionChanged += (_, _) => RedrawAll();
        ViewModel.StepEndNodeChanged += OnStepEndNodeChanged;

        foreach (ToolCategory cat in Enum.GetValues<ToolCategory>())
            _categoryCollapsed[cat] = false;
    }

    private void OnStepEndNodeChanged()
    {
        if (_resultWindow != null && ViewModel.StepEndNode != null)
            _resultWindow.AttachToNode(ViewModel.StepEndNode);
    }

    private void ShowNodeResult(FlowNodeViewModel nodeVm)
    {
        if (_resultWindow == null || !_resultWindow.IsLoaded)
        {
            _resultWindow = new NodeResultWindow();
            _resultWindow.Closed += (_, _) => _resultWindow = null;
            _resultWindow.Show();
        }

        _resultWindow.AttachToNode(nodeVm);
        ViewModel.StepEndNode = nodeVm;
    }

    // ========== 分类折叠 ==========

    private void CategoryHeader_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is ToolCategory cat)
        {
            _categoryCollapsed[cat] = !_categoryCollapsed[cat];

            // 找到对应的 ItemsControl 并切换可见性
            var parent = border.Parent;
            while (parent != null)
            {
                if (parent is StackPanel sp)
                {
                    foreach (var child in sp.Children)
                    {
                        if (child is ItemsControl ic && ic.ItemsSource != null)
                        {
                            var src = ic.ItemsSource;
                            var group = src as IEnumerable<IGrouping<ToolCategory, ToolDescriptor>>;
                            // 简单处理：重新加载
                        }
                    }
                }
                parent = VisualTreeHelper.GetParent(parent);
            }
        }
    }

    private static IEnumerable<DependencyObject> GetVisualsAtPoint(Visual root, Point point)
    {
        var hits = new List<DependencyObject>();
        var result = VisualTreeHelper.HitTest(root, point);
        if (result?.VisualHit != null)
        {
            hits.Add(result.VisualHit);
            if (result.VisualHit is FrameworkElement fe && fe.IsTapEnabled)
            {
                // nothing more needed
            }
            // Walk up the visual tree
            var parent = VisualTreeHelper.GetParent(result.VisualHit);
            while (parent != null)
            {
                hits.Add(parent);
                parent = VisualTreeHelper.GetParent(parent);
            }
        }
        return hits;
    }

    // ========== 工具箱拖拽 ==========

    private void ToolboxItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is ToolDescriptor desc)
        {
            var data = new DataObject("ToolType", desc.ToolType);
            DragDrop.DoDragDrop(fe, data, DragDropEffects.Copy);
        }
    }

    private void FlowCanvas_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent("ToolType") ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void FlowCanvas_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("ToolType")) return;
        var toolType = e.Data.GetData("ToolType") as Type;
        if (toolType == null) return;

        var pos = e.GetPosition(FlowCanvas);
        ViewModel.AddNode(toolType, pos.X - FlowNode.NODE_WIDTH / 2, pos.Y - 35);
    }

    private void FlowCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.Source == FlowCanvas)
        {
            foreach (var n in ViewModel.Nodes) n.IsSelected = false;
            ViewModel.SelectedNode = null;
            CancelConnecting();
        }
    }

    private void FlowCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e) => CancelConnecting();

    // ========== 节点创建（现代风格）============

    private Border CreateNodeBorder(FlowNodeViewModel nodeVm)
    {
        var node = nodeVm.Node;

        // 根据执行状态选择边框颜色
        var borderColor = node.ExecutionStatus switch
        {
            NodeExecutionStatus.Error => new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)),
            NodeExecutionStatus.Success => new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)),
            NodeExecutionStatus.Running => new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6)),
            _ => NodeBorder
        };

        var border = new Border
        {
            Width = FlowNode.NODE_WIDTH,
            Background = NodeBg,
            BorderBrush = borderColor,
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(10),
            Tag = nodeVm,
            Cursor = Cursors.Hand,
            Effect = new DropShadowEffect
            {
                Color = node.ExecutionStatus == NodeExecutionStatus.Error ? Color.FromRgb(0xEF, 0x44, 0x44) : Colors.Black,
                BlurRadius = node.ExecutionStatus == NodeExecutionStatus.Error ? 20 : 16,
                ShadowDepth = 3,
                Opacity = 0.5
            }
        };

        // 节点右键菜单
        var ctxMenu = new ContextMenu();
        var runItem = new MenuItem { Header = "▶ 单步运行到此处", Tag = nodeVm };
        runItem.Click += (_, _) => _ = ViewModel.RunToNode(nodeVm);
        var stepItem = new MenuItem { Header = "⏭ 运行此节点", Tag = nodeVm };
        stepItem.Click += (_, _) => _ = ViewModel.RunSelectedCommand.Execute(nodeVm);
        var editItem = new MenuItem { Header = "✏ 编辑", Tag = nodeVm };
        editItem.Click += (_, _) => EditNode(nodeVm);
        var viewItem = new MenuItem { Header = "🔍 查看结果", Tag = nodeVm };
        viewItem.Click += (_, _) => ShowNodeResult(nodeVm);
        var deleteItem = new MenuItem { Header = "🗑 删除节点", Tag = nodeVm };
        deleteItem.Click += (_, _) => ViewModel.DeleteSelectedCommand.Execute(nodeVm);
        ctxMenu.Items.Add(runItem);
        ctxMenu.Items.Add(stepItem);
        ctxMenu.Items.Add(new Separator());
        ctxMenu.Items.Add(editItem);
        ctxMenu.Items.Add(viewItem);
        ctxMenu.Items.Add(new Separator());
        ctxMenu.Items.Add(deleteItem);
        border.ContextMenu = ctxMenu;

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // 标题区
        var titleBgColor = node.ExecutionStatus switch
        {
            NodeExecutionStatus.Error => new SolidColorBrush(Color.FromRgb(0x7F, 0x1D, 0x1D)),
            NodeExecutionStatus.Success => new SolidColorBrush(Color.FromRgb(0x06, 0x4A, 0x3B)),
            _ => NodeTitleBg
        };

        var titleBorder = new Border
        {
            Background = titleBgColor,
            CornerRadius = new CornerRadius(9, 9, 0, 0),
            Padding = new Thickness(10, 7, 10, 7)
        };

        // 状态指示点
        var statusDot = new Ellipse
        {
            Width = 8,
            Height = 8,
            Margin = new Thickness(0, 0, 6, 0),
            Fill = node.ExecutionStatus switch
            {
                NodeExecutionStatus.Error => new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)),
                NodeExecutionStatus.Success => new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)),
                NodeExecutionStatus.Running => new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24)),
                _ => new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6))
            }
        };

        var title = new TextBlock
        {
            Text = node.Tool.Name,
            Foreground = TextPrimary,
            FontWeight = FontWeights.SemiBold,
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center
        };

        var titlePanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        titlePanel.Children.Add(statusDot);
        titlePanel.Children.Add(title);
        titleBorder.Child = titlePanel;
        Grid.SetRow(titleBorder, 0);
        grid.Children.Add(titleBorder);

        // 端口区
        var portGrid = new Grid { Margin = new Thickness(0, 6, 0, 6) };
        portGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        portGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var inputPanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left };
        for (int i = 0; i < node.Tool.InputSlots.Count; i++)
        {
            var slot = node.Tool.InputSlots[i];
            inputPanel.Children.Add(CreatePortElement(nodeVm, slot.Name, "input", i));
        }
        Grid.SetColumn(inputPanel, 0);
        portGrid.Children.Add(inputPanel);

        var outputPanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
        for (int i = 0; i < node.Tool.OutputSlots.Count; i++)
        {
            var slot = node.Tool.OutputSlots[i];
            outputPanel.Children.Add(CreatePortElement(nodeVm, slot.Name, "output", i));
        }
        Grid.SetColumn(outputPanel, 1);
        portGrid.Children.Add(outputPanel);

        Grid.SetRow(portGrid, 1);
        grid.Children.Add(portGrid);

        border.Child = grid;

        Canvas.SetLeft(border, node.X);
        Canvas.SetTop(border, node.Y);

        border.MouseLeftButtonDown += Node_MouseLeftButtonDown;
        border.MouseLeftButtonUp += Node_MouseLeftButtonUp;
        border.MouseMove += Node_MouseMove;
        border.MouseLeftButtonDown += (s, e) =>
        {
            if (e.ClickCount == 2) { EditNode(nodeVm); e.Handled = true; }
        };

        border.MouseEnter += (s, e) =>
        {
            if (!nodeVm.IsSelected)
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(0x60, 0xA5, 0xFA));
        };
        border.MouseLeave += (s, e) =>
        {
            if (!nodeVm.IsSelected)
                border.BorderBrush = NodeBorder;
        };

        return border;
    }

    private Border CreatePortElement(FlowNodeViewModel nodeVm, string slotName, string portType, int index)
    {
        var isConnected = portType == "input"
            ? nodeVm.Node.Tool.InputSlots[index].IsConnected
            : nodeVm.Node.Tool.OutputSlots[index].IsConnected;

        var port = new Border
        {
            Width = 10,
            Height = 10,
            CornerRadius = new CornerRadius(5),
            Background = isConnected ? PortConnected : (portType == "input" ? PortInput : PortOutput),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27)),
            BorderThickness = new Thickness(2),
            Margin = new Thickness(0, 2, 0, 2),
            Cursor = Cursors.Cross,
            Tag = new PortInfo { NodeVm = nodeVm, SlotName = slotName, PortType = portType, Index = index }
        };

        port.MouseLeftButtonDown += Port_MouseLeftButtonDown;
        port.MouseLeftButtonUp += Port_MouseLeftButtonUp;

        var container = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5, 0, 5, 0) };
        var label = new TextBlock
        {
            Text = slotName,
            Foreground = TextMuted,
            FontSize = 9,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = portType == "input" ? new Thickness(3, 0, 0, 0) : new Thickness(0, 0, 3, 0)
        };

        if (portType == "input")
        { container.Children.Add(port); container.Children.Add(label); }
        else
        { container.Children.Add(label); container.Children.Add(port); }

        return port;
    }

    // ========== 端口连线 ==========

    private void Port_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border port || port.Tag is not PortInfo info) return;
        e.Handled = true;

        _isConnecting = true;
        _connectingFromNode = info.NodeVm;
        _connectingFromSlot = info.SlotName;
        _connectingFromType = info.PortType;

        port.Background = PortConnected;

        var portPos = GetPortPosition(info.NodeVm.Node, info.SlotName, info.PortType, info.Index);
        _tempLine = new Line
        {
            X1 = portPos.X, Y1 = portPos.Y,
            X2 = portPos.X, Y2 = portPos.Y,
            Stroke = new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6)),
            StrokeThickness = 2,
            IsHitTestVisible = false
        };
        FlowCanvas.Children.Add(_tempLine);

        FlowCanvas.MouseMove += Connecting_MouseMove;
        FlowCanvas.MouseLeftButtonUp += Connecting_MouseLeftButtonUp;
    }

    private void Connecting_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isConnecting || _tempLine == null) return;
        var pos = e.GetPosition(FlowCanvas);
        _tempLine.X2 = pos.X;
        _tempLine.Y2 = pos.Y;
    }

    private void Connecting_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        FlowCanvas.MouseMove -= Connecting_MouseMove;
        FlowCanvas.MouseLeftButtonUp -= Connecting_MouseLeftButtonUp;

        Border? targetPort = null;
        PortInfo? targetInfo = null;

        var elements = GetVisualsAtPoint(FlowCanvas, e.GetPosition(FlowCanvas));
        foreach (var el in elements)
        {
            if (el is Border b && b.Tag is PortInfo pi && pi.NodeVm != _connectingFromNode && pi.PortType != _connectingFromType)
            {
                targetPort = b;
                targetInfo = pi;
                break;
            }
        }

        if (targetPort != null && targetInfo != null && _connectingFromNode != null && _connectingFromSlot != null && _connectingFromType != null)
        {
            if (_connectingFromType == "output")
                ViewModel.Connect((_connectingFromNode, _connectingFromSlot, targetInfo.NodeVm, targetInfo.SlotName));
            else
                ViewModel.Connect((targetInfo.NodeVm, targetInfo.SlotName, _connectingFromNode, _connectingFromSlot));
        }

        CancelConnecting();
    }

    private void Port_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) { }

    private void CancelConnecting()
    {
        _isConnecting = false;
        _connectingFromNode = null;
        _connectingFromSlot = null;
        _connectingFromType = null;

        if (_tempLine != null)
        {
            FlowCanvas.Children.Remove(_tempLine);
            _tempLine = null;
        }
    }

    // ========== 节点拖拽 ==========

    private void Node_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border || border.Tag is not FlowNodeViewModel nodeVm) return;

        foreach (var n in ViewModel.Nodes) n.IsSelected = false;
        nodeVm.IsSelected = true;
        ViewModel.SelectedNode = nodeVm;
        border.BorderBrush = NodeSelectedBorder;

        _draggingNode = nodeVm;
        _draggingBorder = border;
        _dragStartPoint = e.GetPosition(FlowCanvas);
        border.CaptureMouse();
        e.Handled = true;
    }

    private void Node_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border)
        {
            border.ReleaseMouseCapture();
            _draggingNode = null;
            _draggingBorder = null;
        }
    }

    private void Node_MouseMove(object sender, MouseEventArgs e)
    {
        if (_draggingNode == null || _draggingBorder == null || e.LeftButton != MouseButtonState.Pressed) return;

        var pos = e.GetPosition(FlowCanvas);
        _draggingNode.Node.X = pos.X - FlowNode.NODE_WIDTH / 2;
        _draggingNode.Node.Y = pos.Y - 35;
        Canvas.SetLeft(_draggingBorder, _draggingNode.Node.X);
        Canvas.SetTop(_draggingBorder, _draggingNode.Node.Y);

        UpdateConnectionsForNode(_draggingNode);
        ViewModel.MarkChanged();
    }

    // ========== 连线绘制 ==========

    private void DrawConnections()
    {
        var toRemove = FlowCanvas.Children.OfType<Path>().ToList();
        foreach (var p in toRemove) FlowCanvas.Children.Remove(p);

        foreach (var connVm in ViewModel.Connections)
        {
            var conn = connVm.Connection;
            var srcSlotIdx = conn.SourceNode.Tool.OutputSlots.ToList().FindIndex(s => s.Name == conn.SourceSlot);
            var tgtSlotIdx = conn.TargetNode.Tool.InputSlots.ToList().FindIndex(s => s.Name == conn.TargetSlot);

            var srcPos = GetPortPosition(conn.SourceNode, conn.SourceSlot, "output", srcSlotIdx >= 0 ? srcSlotIdx : 0);
            var tgtPos = GetPortPosition(conn.TargetNode, conn.TargetSlot, "input", tgtSlotIdx >= 0 ? tgtSlotIdx : 0);

            var midX = (srcPos.X + tgtPos.X) / 2;

            var path = new Path
            {
                Stroke = new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6)),
                StrokeThickness = 2,
                Cursor = Cursors.Hand,
                Tag = connVm
            };

            var geometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = srcPos };
            figure.Segments.Add(new BezierSegment(
                new Point(midX, srcPos.Y),
                new Point(midX, tgtPos.Y),
                tgtPos, true));
            geometry.Figures.Add(figure);
            path.Data = geometry;

            path.MouseLeftButtonDown += (s, e) =>
            {
                if (s is Path p && p.Tag is ConnectionViewModel c)
                {
                    ViewModel.Disconnect(c);
                    e.Handled = true;
                }
            };

            FlowCanvas.Children.Add(path);
        }
    }

    private void UpdateConnectionsForNode(FlowNodeViewModel nodeVm)
    {
        foreach (var connVm in ViewModel.Connections)
        {
            var conn = connVm.Connection;
            if (conn.SourceNode != nodeVm.Node && conn.TargetNode != nodeVm.Node) continue;

            var srcSlotIdx = conn.SourceNode.Tool.OutputSlots.ToList().FindIndex(s => s.Name == conn.SourceSlot);
            var tgtSlotIdx = conn.TargetNode.Tool.InputSlots.ToList().FindIndex(s => s.Name == conn.TargetSlot);

            var srcPos = GetPortPosition(conn.SourceNode, conn.SourceSlot, "output", srcSlotIdx >= 0 ? srcSlotIdx : 0);
            var tgtPos = GetPortPosition(conn.TargetNode, conn.TargetSlot, "input", tgtSlotIdx >= 0 ? tgtSlotIdx : 0);

            foreach (var path in FlowCanvas.Children.OfType<Path>())
            {
                if (path.Tag != connVm) continue;
                var geometry = new PathGeometry();
                var figure = new PathFigure { StartPoint = srcPos };
                var midX = (srcPos.X + tgtPos.X) / 2;
                figure.Segments.Add(new BezierSegment(
                    new Point(midX, srcPos.Y),
                    new Point(midX, tgtPos.Y),
                    tgtPos, true));
                geometry.Figures.Add(figure);
                path.Data = geometry;
            }
        }
    }

    private Point GetPortPosition(FlowNode node, string slotName, string portType, int index)
    {
        double portX = portType == "output" ? node.X + FlowNode.NODE_WIDTH : node.X;
        double portY = node.Y + 28 + index * 20 + 10;
        return new Point(portX, portY);
    }

    // ========== 重绘 ==========

    private void RedrawAll()
    {
        var toRemove = FlowCanvas.Children.OfType<UIElement>().Where(e => e is Border || e is Path).ToList();
        foreach (var e in toRemove) FlowCanvas.Children.Remove(e);
        _nodeBorders.Clear();

        foreach (var nodeVm in ViewModel.Nodes)
        {
            var border = CreateNodeBorder(nodeVm);
            FlowCanvas.Children.Add(border);
            _nodeBorders[nodeVm.Node.Id] = border;
        }

        DrawConnections();
    }

    // ========== 压力测试 ==========

    private void StressTest_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Nodes.Count == 0)
        {
            MessageBox.Show("请先创建流程节点", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var nodes = ViewModel.Nodes.Select(n => n.Node).ToList();
        var connections = ViewModel.Connections.Select(c => c.Connection).ToList();

        var dialog = new Views.ToolDialogs.StressTestDialog(nodes, connections)
        {
            Owner = this
        };
        dialog.Show();
    }

    // ========== 工具编辑 ==========

    public void EditNode(FlowNodeViewModel? nodeVm)
    {
        if (nodeVm == null) return;
        var toolType = nodeVm.Node.Tool.GetType();

        Views.ToolDialogs.ToolDialogBase? dialog = null;

        if (toolType == typeof(ImageLoadTool)) dialog = new Views.ToolDialogs.ImageLoadDialog(nodeVm.Node.Tool as Tools.ImageLoadTool);
        else if (toolType == typeof(ImageRotateTool)) dialog = new Views.ToolDialogs.ImageRotateDialog(nodeVm.Node.Tool as Tools.ImageRotateTool);
        else if (toolType == typeof(ROIDrawTool)) dialog = new Views.ToolDialogs.ROIDrawDialog(nodeVm.Node.Tool as Tools.ROIDrawTool);
        else if (toolType == typeof(FindLineTool)) dialog = new Views.ToolDialogs.FindLineDialog(nodeVm.Node.Tool as Tools.FindLineTool);
        else if (toolType == typeof(FindCircleTool)) dialog = new Views.ToolDialogs.FindCircleDialog(nodeVm.Node.Tool as Tools.FindCircleTool);
        else if (toolType == typeof(LocatorTool)) dialog = new Views.ToolDialogs.LocatorDialog(nodeVm.Node.Tool as Tools.LocatorTool);
        else if (toolType == typeof(CoordinateTransformTool)) dialog = new Views.ToolDialogs.CoordinateTransformDialog(nodeVm.Node.Tool as Tools.CoordinateTransformTool);
        else if (toolType == typeof(ThresholdTool)) dialog = new Views.ToolDialogs.ThresholdDialog(nodeVm.Node.Tool as Tools.ThresholdTool);
        else if (toolType == typeof(MorphologyTool)) dialog = new Views.ToolDialogs.MorphologyDialog(nodeVm.Node.Tool as Tools.MorphologyTool);
        else if (toolType == typeof(BlobAnalysisTool)) dialog = new Views.ToolDialogs.BlobAnalysisDialog(nodeVm.Node.Tool as Tools.BlobAnalysisTool);
        else if (toolType == typeof(CaliperMeasureTool)) dialog = new Views.ToolDialogs.CaliperMeasureDialog(nodeVm.Node.Tool as Tools.CaliperMeasureTool);
        else if (toolType == typeof(PointToPointDistanceTool)) dialog = new Views.ToolDialogs.PointToPointDistanceDialog(nodeVm.Node.Tool as Tools.PointToPointDistanceTool);
        else if (toolType == typeof(PointToLineDistanceTool)) dialog = new Views.ToolDialogs.PointToLineDistanceDialog(nodeVm.Node.Tool as Tools.PointToLineDistanceTool);
        else if (toolType == typeof(LineToLineDistanceTool)) dialog = new Views.ToolDialogs.LineToLineDistanceDialog(nodeVm.Node.Tool as Tools.LineToLineDistanceTool);
        else if (toolType == typeof(GetGrayValueTool)) dialog = new Views.ToolDialogs.GetGrayValueDialog(nodeVm.Node.Tool as Tools.GetGrayValueTool);
        else if (toolType == typeof(CSharpScriptTool)) dialog = new Views.ToolDialogs.CSharpScriptDialog(nodeVm.Node.Tool as Tools.CSharpScriptTool);
        else dialog = new Views.ToolDialogs.ImageProcessingDialog(nodeVm.Node.Tool as Tools.ToolBase);

        if (dialog != null)
        {
            // 参数修改后立即刷新结果窗口
            dialog.OnApplied = () =>
            {
                if (_resultWindow != null && _resultWindow.IsLoaded)
                    _resultWindow.RefreshDisplay();
                RedrawAll();
            };
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
                ViewModel.MarkChanged();
        }
    }

    public class PortInfo
    {
        public FlowNodeViewModel NodeVm { get; set; } = null!;
        public string SlotName { get; set; } = "";
        public string PortType { get; set; } = "";
        public int Index { get; set; }
    }
}