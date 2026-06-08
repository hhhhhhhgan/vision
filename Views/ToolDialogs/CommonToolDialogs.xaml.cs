using System.Windows;
using VisionFlow.Tools;

namespace VisionFlow.Views.ToolDialogs;

public partial class ThresholdDialog : ToolDialogBase
{
    private readonly ThresholdTool _tool;
    private HalconDotNet.HImage? _inputImage;

    public ThresholdDialog(ThresholdTool tool)
    {
        InitializeComponent();
        _tool = tool;
        Tool = tool;
        MinThreshTextBox.Text = tool.MinThreshold.ToString();
        MaxThreshTextBox.Text = tool.MaxThreshold.ToString();
        ModeCombo.SelectedIndex = tool.ThresholdMode switch { "min" => 1, "range" => 2, _ => 0 };
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        _tool.MinThreshold = ParseDouble(MinThreshTextBox.Text, 128);
        _tool.MaxThreshold = ParseDouble(MaxThreshTextBox.Text, 255);
        _tool.ThresholdMode = ModeCombo.SelectedIndex switch { 1 => "min", 2 => "range", _ => "max" };
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
    private double ParseDouble(string s, double def) => double.TryParse(s, out var v) ? v : def;
}

public partial class MorphologyDialog : ToolDialogBase
{
    private readonly MorphologyTool _tool;

    public MorphologyDialog(MorphologyTool tool)
    {
        InitializeComponent();
        _tool = tool;
        Tool = tool;
        OpCombo.SelectedIndex = tool.Operation switch { "erosion" => 1, "opening" => 2, "closing" => 3, _ => 0 };
        RadiusTextBox.Text = tool.StructElementRadius.ToString();
        IterTextBox.Text = tool.Iterations.ToString();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        _tool.Operation = OpCombo.SelectedIndex switch { 1 => "erosion", 2 => "opening", 3 => "closing", _ => "dilation" };
        _tool.StructElementRadius = double.TryParse(RadiusTextBox.Text, out var r) ? r : 3;
        _tool.Iterations = int.TryParse(IterTextBox.Text, out var i) ? i : 1;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}

public partial class BlobAnalysisDialog : ToolDialogBase
{
    private readonly BlobAnalysisTool _tool;

    public BlobAnalysisDialog(BlobAnalysisTool tool)
    {
        InitializeComponent();
        _tool = tool;
        Tool = tool;
        MinAreaTextBox.Text = tool.MinArea.ToString();
        MaxAreaTextBox.Text = tool.MaxArea.ToString();
        SortCombo.SelectedIndex = tool.SortBy switch { "row" => 1, "column" => 2, _ => 0 };
        TopNTextBox.Text = tool.TopN.ToString();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        _tool.MinArea = double.TryParse(MinAreaTextBox.Text, out var m) ? m : 0;
        _tool.MaxArea = double.TryParse(MaxAreaTextBox.Text, out var x) ? x : 999999;
        _tool.SortBy = SortCombo.SelectedIndex switch { 1 => "row", 2 => "column", _ => "area" };
        _tool.TopN = int.TryParse(TopNTextBox.Text, out var t) ? t : 0;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}

public partial class CaliperMeasureDialog : ToolDialogBase
{
    private readonly CaliperMeasureTool _tool;

    public CaliperMeasureDialog(CaliperMeasureTool tool)
    {
        InitializeComponent();
        _tool = tool;
        Tool = tool;
        LengthText.Text = tool.MeasureLength.ToString();
        WidthText.Text = tool.MeasureWidth.ToString();
        SigmaText.Text = tool.Sigma.ToString();
        ThreshText.Text = tool.Threshold.ToString();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        _tool.MeasureLength = double.TryParse(LengthText.Text, out var l) ? l : 20;
        _tool.MeasureWidth = double.TryParse(WidthText.Text, out var w) ? w : 3;
        _tool.Sigma = double.TryParse(SigmaText.Text, out var s) ? s : 1.0;
        _tool.Threshold = double.TryParse(ThreshText.Text, out var t) ? t : 25;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}

public partial class ImageSaveDialog : ToolDialogBase
{
    private readonly ImageSaveTool _tool;

    public ImageSaveDialog(ImageSaveTool tool)
    {
        InitializeComponent();
        _tool = tool;
        Tool = tool;
        PathTextBox.Text = tool.FilePath;
        FormatCombo.SelectedIndex = tool.Format switch { "jpg" => 1, "bmp" => 2, "tiff" => 3, _ => 0 };
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "PNG|*.png|JPEG|*.jpg|BMP|*.bmp|TIFF|*.tiff",
            DefaultExt = ".png"
        };
        if (dlg.ShowDialog() == true) PathTextBox.Text = dlg.FileName;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        _tool.FilePath = PathTextBox.Text;
        _tool.Format = FormatCombo.SelectedIndex switch { 1 => "jpg", 2 => "bmp", 3 => "tiff", _ => "png" };
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}

public partial class GetGrayValueDialog : ToolDialogBase
{
    private readonly GetGrayValueTool _tool;

    public GetGrayValueDialog(GetGrayValueTool tool)
    {
        InitializeComponent();
        _tool = tool;
        Tool = tool;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}