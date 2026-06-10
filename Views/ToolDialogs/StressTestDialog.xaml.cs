using System.Windows;
using System.Windows.Controls;
using VisionFlow.Models;
using VisionFlow.Tools;

namespace VisionFlow.Views.ToolDialogs {

public partial class StressTestDialog : Window
{
    private readonly List<FlowNode> _nodes;
    private readonly List<Connection> _connections;
    private StressTestRunner? _runner;
    private CancellationTokenSource? _cancelCts;

    public StressTestDialog(List<FlowNode> nodes, List<Connection> connections)
    {
        InitializeComponent();
        _nodes = nodes;
        _connections = connections;
    }

    private async void StartBtn_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(RunCountBox.Text, out int count) || count <= 0)
        {
            MessageBox.Show("请输入有效的运行次数（正整数）");
            return;
        }

        if (!int.TryParse(DelayBox.Text, out int delay))
        {
            delay = 0;
        }

        // 重置UI
        ErrorList.Items.Clear();
        ErrorList.Visibility = Visibility.Collapsed;
        ProgressBar.Value = 0;
        ProgressBar.Maximum = count;
        ProgressText.Text = $"0/{count}";
        SuccessCount.Text = "✓ 0";
        FailCount.Text = "✗ 0";
        ErrorCount.Text = "⚠ 0";
        AvgTime.Text = "0ms";

        StartBtn.IsEnabled = false;
        CancelBtn.IsEnabled = true;
        RunCountBox.IsEnabled = false;
        DelayBox.IsEnabled = false;

        _runner = new StressTestRunner(_nodes, _connections);
        _runner.ProgressChanged += OnProgress;
        _runner.ErrorOccurred += OnError;

        var result = await _runner.RunAsync(count, delay);

        StartBtn.IsEnabled = true;
        CancelBtn.IsEnabled = false;
        RunCountBox.IsEnabled = true;
        DelayBox.IsEnabled = true;

        // 显示最终结果
        var icon = result.Success ? MessageBoxImage.Information : MessageBoxImage.Warning;
        var title = result.Success ? "测试通过" : "测试发现问题";

        string msg;
        if (result.Errors.Count > 0)
        {
            msg = $"运行次数: {result.TotalRuns}\n" +
                  $"成功: {result.SuccessCount}  失败: {result.FailCount}  异常: {result.ErrorCount}\n" +
                  $"总耗时: {result.TotalDurationMs:F0}ms  平均: {result.AvgDurationMs:F2}ms\n\n" +
                  $"前5条错误:\n" +
                  string.Join("\n", result.Errors.Take(5).Select(r => $"[{r.RunIndex}] {r.NodeName}: {r.ErrorMessage}"));
        }
        else
        {
            msg = $"全部 {result.TotalRuns} 次执行成功！\n总耗时: {result.TotalDurationMs:F0}ms  平均: {result.AvgDurationMs:F2}ms";
        }

        MessageBox.Show(msg, title, MessageBoxButton.OK, icon);
    }

    private void OnProgress(int success, int failError, int total)
    {
        Dispatcher.Invoke(() =>
        {
            ProgressBar.Value = total;
            ProgressText.Text = $"{total}/{ProgressBar.Maximum}";
            SuccessCount.Text = $"✓ {success}";
            FailCount.Text = $"✗ {failError}";
            ErrorCount.Text = $"⚠ {failError}";

            if (_runner != null && total > 0)
                AvgTime.Text = $"{(_runner.TotalRuns > 0 ? (_runner.TotalRuns > 0 ? "?" : "0") : "0")}ms";
        });
    }

    private void OnError(ErrorRecord record)
    {
        Dispatcher.Invoke(() =>
        {
            ErrorList.Visibility = Visibility.Visible;
            ErrorList.Items.Add(record);
        });
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e)
    {
        _runner?.Cancel();
        CancelBtn.IsEnabled = false;
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e)
    {
        _runner?.Cancel();
        Close();
    }
}
}