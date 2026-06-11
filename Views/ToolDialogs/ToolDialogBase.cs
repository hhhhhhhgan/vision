using System;
using System.Windows;

namespace VisionFlow.Views.ToolDialogs {

/// <summary>
/// 工具对话框基类
/// 提供 Apply（应用）和 OK（确认）两种执行模式
/// </summary>
public abstract class ToolDialogBase : Window
{
    /// <summary>工具执行完成后的回调（用于刷新结果窗口）</summary>
    public Action? OnApplied { get; set; }

    /// <summary>工具名称（可编辑）</summary>
    public string ToolName
    {
        get => _toolNameBox?.Text ?? "";
        set { if (_toolNameBox != null) _toolNameBox.Text = value; }
    }

    private System.Windows.Controls.TextBox? _toolNameBox;

    protected ToolDialogBase()
    {
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;
    }

    /// <summary>
    /// 子类可覆盖以提供自定义工具名称输入框
    /// </summary>
    protected virtual System.Windows.Controls.UIElement? CreateNamePanel(string currentName)
    {
        var panel = new System.Windows.Controls.Grid { Margin = new Thickness(0, 0, 0, 8) };
        panel.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(70) });
        panel.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });

        var lbl = new System.Windows.Controls.TextBlock { Text = "名称:", Foreground = System.Windows.Media.Brushes.Gray, VerticalAlignment = System.Windows.VerticalAlignment.Center };
        System.Windows.Controls.Grid.SetColumn(lbl, 0);

        _toolNameBox = new System.Windows.Controls.TextBox
        {
            Text = currentName,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x37, 0x41, 0x51)),
            Padding = new Thickness(8, 6, 8, 6)
        };
        System.Windows.Controls.Grid.SetColumn(_toolNameBox, 1);

        panel.Children.Add(lbl);
        panel.Children.Add(_toolNameBox);
        return panel;
    }

    /// <summary>工具实例（由子类在构造函数中赋值）</summary>
    protected ToolBase? Tool { get; set; }

    /// <summary>
    /// 加载当前工具参数到对话框
    /// </summary>
    protected virtual void LoadFromTool() { }

    /// <summary>
    /// 将对话框的值保存到工具
    /// </summary>
    protected virtual void SaveToTool() { }

    /// <summary>
    /// 执行该工具（使用当前工具的参数）
    /// </summary>
    protected virtual void ExecuteTool() { }

    /// <summary>
    /// 应用（不关闭对话框）
    /// </summary>
    protected void Apply()
    {
        try
        {
            // 保存工具名称
            if (_toolNameBox != null && Tag is ToolBase tool)
                tool.Name = _toolNameBox.Text;

            SaveToTool();
            ExecuteTool();
            OnApplied?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"执行失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 确认并关闭
    /// </summary>
    protected void Confirm()
    {
        try
        {
            if (_toolNameBox != null && Tag is ToolBase tool)
                tool.Name = _toolNameBox.Text;

            SaveToTool();
            ExecuteTool();
            OnApplied?.Invoke();
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"执行失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 仅关闭（不保存不执行）
    /// </summary>
    protected void Cancel()
    {
        DialogResult = false;
        Close();
    }
}
}