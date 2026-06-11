using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using VisionFlow.Tools;
using System.Collections.Generic;
using System.Linq;

namespace VisionFlow.Views.ToolDialogs {

public partial class CSharpScriptDialog : ToolDialogBase
{
    private readonly CSharpScriptTool _tool;
    private readonly CSharpCompletionProvider _completionProvider = new();
    private CompletionWindow? _completionWindow;

    public CSharpScriptDialog(CSharpScriptTool tool)
    {
        InitializeComponent();
        _tool = tool;
        Tool = tool;

        // 配置 AvalonEdit
        CodeEditor.FontFamily = new FontFamily("Consolas, Courier New");
        CodeEditor.FontSize = 13;
        CodeEditor.ShowLineNumbers = true;
        CodeEditor.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
        CodeEditor.Foreground = new SolidColorBrush(Color.FromRgb(0xD4, 0xD4, 0xD4));
        CodeEditor.LineNumbersForeground = new SolidColorBrush(Color.FromRgb(0x85, 0x85, 0x85));

        // C# 语法高亮
        CodeEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");

        // 加载代码
        string defaultCode = @"// 可用变量: Image(HImage?), Region(HRegion?), Value(double?)
// 可用HALCON: HImage.Width/Height/DispImage(), HRegion.Area/Connection()
// 可用Math: Math.Sqrt/Abs/Sin/Cos/Tan/Atan/Log/Exp/PI/E
// 输出: Result = xxx

Result = Value ?? 0;
";

        CodeEditor.Text = string.IsNullOrEmpty(_tool.ScriptCode) ? defaultCode : _tool.ScriptCode;

        // 注册 Ctrl+Space 补齐
        CodeEditor.TextArea.KeyDown += TextArea_KeyDown;
        CodeEditor.TextArea.MouseRightButtonDown += TextArea_MouseRightButtonDown;

        // 每次文本变化更新状态
        CodeEditor.TextChanged += (_, _) => UpdateStatus();
    }

    private void TextArea_KeyDown(object sender, KeyEventArgs e)
    {
        // Ctrl+Space → 触发补齐
        if (e.Key == Key.Space && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            ShowCompletion();
            e.Handled = true;
            return;
        }

        // . → 触发成员补齐
        if (e.Key == Key.OemPeriod)
        {
            var offset = CodeEditor.CaretOffset;
            var line = CodeEditor.Document.GetLineByOffset(offset);
            var textBefore = CodeEditor.Document.GetText(line.Offset, offset - line.Offset);
            var word = textBefore.Split(' ', '\t', '\n', '(', ')').LastOrDefault() ?? "";

            var completions = _completionProvider.GetCompletions(word).ToList();
            if (completions.Any())
            {
                ShowCompletion(completions);
            }
            return;
        }

        // 普通字符 → 显示实时补齐
        // （仅显示前缀匹配项）
    }

    private void ShowCompletion(IEnumerable<CompletionData>? specificData = null)
    {
        var textArea = CodeEditor.TextArea;
        var offset = textArea.CaretOffset;

        // 找到当前单词起始位置
        var line = CodeEditor.Document.GetLineByOffset(offset);
        var textBefore = CodeEditor.Document.GetText(line.Offset, offset - line.Offset);

        // 提取当前单词
        int wordStart = textBefore.Length;
        for (int i = textBefore.Length - 1; i >= 0; i--)
        {
            char c = textBefore[i];
            if (char.IsLetterOrDigit(c) || c == '_')
                wordStart = i;
            else
                break;
        }

        var currentWord = textBefore.Substring(wordStart);
        var segment = new TextSegment { StartOffset = line.Offset + wordStart, Length = offset - line.Offset - wordStart };

        var data = specificData ?? _completionProvider.GetCompletions(currentWord).ToList();
        if (!data.Any()) return;

        _completionWindow?.Close();

        _completionWindow = new CompletionWindow(textArea)
        {
            Width = 340,
            Height = 360
        };

        foreach (var item in data)
            _completionWindow.CompletionList.CompletionData.Add(item);

        _completionWindow.CompletionList.SelectItem(data.First());
        _completionWindow.Show();
        _completionWindow.Closed += (_, _) => _completionWindow = null;
    }

    private void TextArea_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        // 右键显示补齐菜单
        ShowCompletion();
    }

    private void UpdateStatus()
    {
        var text = CodeEditor.Text;
        var lines = text.Split('\n');

        // 简单检查是否有 Result 赋值
        bool hasResult = lines.Any(l => l.TrimStart().StartsWith("Result", StringComparison.OrdinalIgnoreCase));

        if (hasResult)
            StatusTextBlock.Text = $"✓ 代码已就绪  •  {lines.Length} 行";
        else
            StatusTextBlock.Text = $"⚠ 警告: 未发现 Result 赋值  •  {lines.Length} 行";
    }

    private void FormatButton_Click(object sender, RoutedEventArgs e)
    {
        var lines = CodeEditor.Text.Split('\n');
        var formatted = new List<string>();
        int emptyCount = 0;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();
            bool isEmpty = string.IsNullOrWhiteSpace(line);

            if (isEmpty)
            {
                emptyCount++;
                if (emptyCount <= 1) // 最多一个空行
                    formatted.Add("");
            }
            else
            {
                emptyCount = 0;
                // 基础缩进格式化（保留原有缩进风格）
                formatted.Add(line);
            }
        }

        while (formatted.Count > 0 && string.IsNullOrWhiteSpace(formatted.Last()))
            formatted.RemoveAt(formatted.Count - 1);
        formatted.Add("");

        CodeEditor.Text = string.Join("\n", formatted);
        StatusTextBlock.Text = "已格式化";
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        _tool.ScriptCode = CodeEditor.Text;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

// 简单文本片段用于补齐
public class TextSegment : ISegment
{
    public int Offset { get; set; }
    public int Length { get; set; }
    public int EndOffset => Offset + Length;
}
}