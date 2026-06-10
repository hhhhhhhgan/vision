using HalconDotNet;
using VisionFlow.Models;
using System.Collections.Generic;

namespace VisionFlow.Tools {

/// <summary>
/// 工具基类 — 所有工具继承此类
/// 提供 HALCON 异常保护 + 参数缓存 + 内存管理
/// </summary>
public abstract class ToolBase
{
    private bool _initialized;
    private string? _lastHash;

    public string Name { get; set; } = "未命名工具";
    public string Description { get; set; } = "";

    public List<InputSlot> InputSlots { get; set; } = new();
    public List<OutputSlot> OutputSlots { get; set; } = new();

    protected virtual void OnInitialize() { }

    protected virtual string GetParameterHash() => "";

    protected virtual void BindInputs(ExecuteContext context)
    {
        foreach (var slot in InputSlots)
        {
            if (slot.IsOptional && !context.Inputs.ContainsKey(slot.Name)) continue;
            slot.Value = context.Inputs.GetValueOrDefault(slot.Name);
        }
    }

    protected virtual void BindOutputs(ExecuteContext context)
    {
        foreach (var slot in OutputSlots)
            if (slot.Value != null)
                context.Outputs[slot.Name] = slot.Value;
    }

    public async Task<Result> Execute(ExecuteContext context)
    {
        var paramHash = GetParameterHash();
        if (!_initialized || paramHash != _lastHash)
        {
            OnInitialize();
            _lastHash = paramHash;
            _initialized = true;
        }

        BindInputs(context);

        Result result;
        try
        {
            result = await OnExecuteAsync(context);
        }
        catch (HalconException hex)
        {
            return Result.Fail($"HALCON错误 [{hex.Code}]: {hex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Fail($"[{Name}] 执行异常: {ex.Message}");
        }

        if (result.Success)
            BindOutputs(context);

        return result;
    }

    protected abstract Task<Result> OnExecuteAsync(ExecuteContext context);

    protected double GetDouble(string name, double def = 0)
    {
        var slot = InputSlots.FirstOrDefault(s => s.Name == name);
        return slot?.Value is double d ? d : slot?.Value is HTuple t && t.Length == 1 ? t[0].D : def;
    }

    protected int GetInt(string name, int def = 0)
    {
        var slot = InputSlots.FirstOrDefault(s => s.Name == name);
        return slot?.Value is int i ? i : slot?.Value is HTuple t && t.Length == 1 ? t[0].I : def;
    }

    protected string GetString(string name, string def = "")
    {
        var slot = InputSlots.FirstOrDefault(s => s.Name == name);
        return slot?.Value?.ToString() ?? def;
    }

    protected static void DisposeIfInitialized(params HObject[] objects)
    {
        foreach (var obj in objects)
        {
            try { if (obj != null && obj.IsInitialized()) obj.Dispose(); } catch { }
        }
    }
}

public class InputSlot
{
    public string Name { get; set; } = "";
    public Type ValueType { get; set; } = typeof(object);
    public bool IsOptional { get; set; }
    public object? Value { get; set; }
    public InputSlot() { }
    public InputSlot(string name, Type type, bool optional = false) { Name = name; ValueType = type; IsOptional = optional; }
}

public class OutputSlot
{
    public string Name { get; set; } = "";
    public Type ValueType { get; set; } = typeof(object);
    public object? Value { get; set; }
    public OutputSlot() { }
    public OutputSlot(string name, Type type) { Name = name; ValueType = type; }
}

public class ExecuteContext
{
    public Dictionary<string, object?> Inputs { get; set; } = new();
    public Dictionary<string, object?> Outputs { get; set; } = new();
}

public class Result
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object?> OutputData { get; set; } = new();
    public static Result Ok(Dictionary<string, object?>? data = null)
        => new() { Success = true, OutputData = data ?? new() };
    public static Result Fail(string msg)
        => new() { Success = false, ErrorMessage = msg };
}

// ===== 工具实现 =====

public class ImageLoadTool : ToolBase
{
    public string FilePath { get; set; } = "";
    private HImage? _cachedImage;
    private string? _cachedPath;

    public ImageLoadTool()
    {
        Name = "图像加载";
        OutputSlots = new List<OutputSlot> { new("Image", typeof(HImage)), new("Width", typeof(int)), new("Height", typeof(int)) };
    }

    protected override string GetParameterHash() => FilePath;

    protected override void OnInitialize()
    {
        _cachedImage?.Dispose(); _cachedImage = null; _cachedPath = null;
    }

    protected override Task<Result> OnExecuteAsync(ExecuteContext ctx)
    {
        if (string.IsNullOrEmpty(FilePath)) return Task.FromResult(Result.Fail("请设置图像路径"));
        if (_cachedImage != null && _cachedPath == FilePath && _cachedImage.IsInitialized())
        {
            OutputSlots[0].Value = _cachedImage;
            return Task.FromResult(Result.Ok());
        }

        _cachedImage?.Dispose();
        _cachedImage = new HImage();

        try
        {
            _cachedImage.ReadImage(FilePath);
            _cachedPath = FilePath;
            var w = _cachedImage.Width;
            var h = _cachedImage.Height;
            OutputSlots[0].Value = _cachedImage;
            OutputSlots[1].Value = w;
            OutputSlots[2].Value = h;
            return Task.FromResult(Result.Ok());
        }
        catch (HalconException hex) { return Task.FromResult(Result.Fail($"加载失败: {hex.Message}")); }
    }
}

public class ImageRotateTool : ToolBase
{
    public double Angle { get; set; } = 0;

    public ImageRotateTool()
    {
        Name = "图像旋转";
        InputSlots = new List<InputSlot> { new("Image", typeof(HImage)) };
        OutputSlots = new List<OutputSlot> { new("Image", typeof(HImage)) };
    }

    protected override Task<Result> OnExecuteAsync(ExecuteContext ctx)
    {
        var img = InputSlots[0].Value as HImage;
        if (img == null || !img.IsInitialized()) return Task.FromResult(Result.Fail("请先输入图像"));

        var outImg = new HImage();
        try
        {
            outImg = img.RotateImage(Angle * Math.PI / 180, "constant");
            OutputSlots[0].Value = outImg;
            return Task.FromResult(Result.Ok());
        }
        catch (HalconException hex) { outImg.Dispose(); return Task.FromResult(Result.Fail($"旋转失败: {hex.Message}")); }
    }
}

public class ROIDrawTool : ToolBase
{
    public string Type { get; set; } = "rectangle";
    public double Row1 { get; set; }
    public double Col1 { get; set; }
    public double Row2 { get; set; }
    public double Col2 { get; set; }

    public ROIDrawTool()
    {
        Name = "ROI绘制";
        InputSlots = new List<InputSlot> { new("Image", typeof(HImage), true) };
        OutputSlots = new List<OutputSlot> { new("Region", typeof(HRegion)), new("Row1", typeof(double)), new("Col1", typeof(double)), new("Row2", typeof(double)), new("Col2", typeof(double)) };
    }

    protected override Task<Result> OnExecuteAsync(ExecuteContext ctx)
    {
        var region = new HRegion();
        try
        {
            if (Type == "rectangle")
            {
                region.GenRectangle2(Row1, Col1, 0, Math.Abs(Row2 - Row1) / 2, Math.Abs(Col2 - Col1) / 2);
            }
            else if (Type == "circle")
            {
                var rad = Math.Sqrt((Row2 - Row1) * (Row2 - Row1) + (Col2 - Col1) * (Col2 - Col1));
                region.GenCircle(Row1, Col1, rad);
            }
            else
            {
                region.GenRegionLine(Row1, Col1, Row2, Col2);
            }

            OutputSlots[0].Value = region;
            OutputSlots[1].Value = Row1;
            OutputSlots[2].Value = Col1;
            OutputSlots[3].Value = Row2;
            OutputSlots[4].Value = Col2;
            return Task.FromResult(Result.Ok());
        }
        catch (HalconException hex) { region.Dispose(); return Task.FromResult(Result.Fail($"ROI创建失败: {hex.Message}")); }
    }
}

public class FindLineTool : ToolBase
{
    public double EdgeThreshold { get; set; } = 20;
    public double MinLineLength { get; set; } = 20;
    public double OffsetRow { get; set; }
    public double OffsetCol { get; set; }

    public FindLineTool()
    {
        Name = "查找线";
        InputSlots = new List<InputSlot>
        {
            new("Image", typeof(HImage)),
            new("OffsetRow", typeof(double), true),
            new("OffsetCol", typeof(double), true),
            new("OffsetAngle", typeof(double), true)
        };
        OutputSlots = new List<OutputSlot>
        {
            new("Row1", typeof(double)), new("Col1", typeof(double)),
            new("Row2", typeof(double)), new("Col2", typeof(double)),
            new("Distance", typeof(double)),
            new("OffsetedRow1", typeof(double)), new("OffsetedCol1", typeof(double)),
            new("OffsetedRow2", typeof(double)), new("OffsetedCol2", typeof(double))
        };
    }

    protected override Task<Result> OnExecuteAsync(ExecuteContext ctx)
    {
        var img = InputSlots[0].Value as HImage;
        if (img == null || !img.IsInitialized()) return Task.FromResult(Result.Fail("请先输入图像"));

        try
        {
            var offR = GetDouble("OffsetRow", OffsetRow);
            var offC = GetDouble("OffsetCol", OffsetCol);
            var offA = GetDouble("OffsetAngle", 0);

            using var edges = img.EdgesSubPix("canny", EdgeThreshold, 20, 40);
            using var lines = edges.SegmentContoursXld("fit_line", 20, -0.5, 0.5, 5, 5, "attr_forest");

            if (lines.CountObj() == 0) return Task.FromResult(Result.Fail("未找到直线"));

            using var line = lines.SelectObj(1);
            line.GetParams(out HTuple row1, out HTuple col1, out HTuple row2, out HTuple col2, out _, _);

            var dist = Math.Sqrt((row2.D - row1.D) * (row2.D - row1.D) + (col2.D - col1.D) * (col2.D - col1.D));

            // 应用偏移
            var cos = Math.Cos(offA);
            var sin = Math.Sin(offA);
            var or1 = row1.D + offR; var oc1 = col1.D + offC;
            var or2 = row2.D + offR; var oc2 = col2.D + offC;

            OutputSlots[0].Value = row1.D; OutputSlots[1].Value = col1.D;
            OutputSlots[2].Value = row2.D; OutputSlots[3].Value = col2.D;
            OutputSlots[4].Value = dist;
            OutputSlots[5].Value = or1; OutputSlots[6].Value = oc1;
            OutputSlots[7].Value = or2; OutputSlots[8].Value = oc2;
            return Task.FromResult(Result.Ok());
        }
        catch (HalconException hex) { return Task.FromResult(Result.Fail($"查找线失败: {hex.Message}")); }
    }
}

public class FindCircleTool : ToolBase
{
    public double EdgeThreshold { get; set; } = 20;
    public double MinRadius { get; set; } = 5;
    public double MaxRadius { get; set; } = 100;
    public double OffsetRow { get; set; }
    public double OffsetCol { get; set; }

    public FindCircleTool()
    {
        Name = "查找圆";
        InputSlots = new List<InputSlot>
        {
            new("Image", typeof(HImage)),
            new("OffsetRow", typeof(double), true),
            new("OffsetCol", typeof(double), true)
        };
        OutputSlots = new List<OutputSlot>
        {
            new("Row", typeof(double)), new("Col", typeof(double)), new("Radius", typeof(double)),
            new("OffsetedRow", typeof(double)), new("OffsetedCol", typeof(double))
        };
    }

    protected override Task<Result> OnExecuteAsync(ExecuteContext ctx)
    {
        var img = InputSlots[0].Value as HImage;
        if (img == null || !img.IsInitialized()) return Task.FromResult(Result.Fail("请先输入图像"));

        try
        {
            var offR = GetDouble("OffsetRow", OffsetRow);
            var offC = GetDouble("OffsetCol", OffsetCol);

            using var edges = img.EdgesSubPix("canny", EdgeThreshold, 20, 40);
            using var circles = edges.SegmentContoursXld("fit_circle", 20, -0.5, 0.5, 5, 5, "attr_forest");

            if (circles.CountObj() == 0) return Task.FromResult(Result.Fail("未找到圆"));

            using var circle = circles.SelectObj(1);
            circle.GetParams(out HTuple row, out HTuple col, out HTuple rad, out _, _, _);

            OutputSlots[0].Value = row.D; OutputSlots[1].Value = col.D; OutputSlots[2].Value = rad.D;
            OutputSlots[3].Value = row.D + offR; OutputSlots[4].Value = col.D + offC;
            return Task.FromResult(Result.Ok());
        }
        catch (HalconException hex) { return Task.FromResult(Result.Fail($"查找圆失败: {hex.Message}")); }
    }
}

public class ThresholdTool : ToolBase
{
    public double MinGray { get; set; } = 0;
    public double MaxGray { get; set; } = 255;
    public string Mode { get; set; } = "global";
    private HRegion? _lastRegion;

    public ThresholdTool()
    {
        Name = "阈值分割";
        InputSlots = new List<InputSlot> { new("Image", typeof(HImage)), new("MinGray", typeof(double), true), new("MaxGray", typeof(double), true) };
        OutputSlots = new List<OutputSlot> { new("Region", typeof(HRegion)), new("Area", typeof(double)), new("Row", typeof(double)), new("Col", typeof(double)) };
    }

    protected override string GetParameterHash() => $"{MinGray}-{MaxGray}-{Mode}";

    protected override void BindInputs(ExecuteContext context)
    {
        base.BindInputs(context);
        if (context.Inputs.TryGetValue("MinGray", out var min) && min != null) MinGray = Convert.ToDouble(min);
        if (context.Inputs.TryGetValue("MaxGray", out var max) && max != null) MaxGray = Convert.ToDouble(max);
    }

    protected override Task<Result> OnExecuteAsync(ExecuteContext ctx)
    {
        var img = InputSlots[0].Value as HImage;
        if (img == null || !img.IsInitialized()) return Task.FromResult(Result.Fail("请先输入图像"));

        _lastRegion?.Dispose();
        _lastRegion = new HRegion();

        try
        {
            _lastRegion = img.Threshold(MinGray, MaxGray);
            var area = _lastRegion.AreaCenter(out HTuple row, out HTuple col);
            OutputSlots[0].Value = _lastRegion;
            OutputSlots[1].Value = area;
            OutputSlots[2].Value = row.D;
            OutputSlots[3].Value = col.D;
            return Task.FromResult(Result.Ok());
        }
        catch (HalconException hex) { return Task.FromResult(Result.Fail($"阈值分割失败: {hex.Message}")); }
    }
}

public class MorphologyTool : ToolBase
{
    public string Operation { get; set; } = "opening";
    public double ElementSize { get; set; } = 3;
    public int Iterations { get; set; } = 1;

    public MorphologyTool()
    {
        Name = "形态学";
        InputSlots = new List<InputSlot> { new("Region", typeof(HRegion)), new("ElementSize", typeof(double), true) };
        OutputSlots = new List<OutputSlot> { new("Region", typeof(HRegion)), new("Area", typeof(double)) };
    }

    protected override string GetParameterHash() => $"{Operation}-{Iterations}-{ElementSize}";

    protected override void BindInputs(ExecuteContext context)
    {
        base.BindInputs(context);
        if (context.Inputs.TryGetValue("ElementSize", out var es) && es != null)
            ElementSize = Convert.ToDouble(es);
    }

    protected override Task<Result> OnExecuteAsync(ExecuteContext ctx)
    {
        var region = InputSlots[0].Value as HRegion;
        if (region == null || !region.IsInitialized()) return Task.FromResult(Result.Fail("请先输入区域"));

        try
        {
            using var elem = new HRegion();
            elem.GenCircle(ElementSize / 2);

            HRegion result = Operation switch
            {
                "opening" => region.Opening(elem, Iterations),
                "closing" => region.Closing(elem, Iterations),
                "erosion" => region.Erosion(elem, Iterations),
                _ => region.Dilation(elem, Iterations)
            };

            var area = result.AreaCenter(out _, out _);
            OutputSlots[0].Value = result;
            OutputSlots[1].Value = area;
            return Task.FromResult(Result.Ok());
        }
        catch (HalconException hex) { return Task.FromResult(Result.Fail($"形态学失败: {hex.Message}")); }
    }
}

public class BlobAnalysisTool : ToolBase
{
    public double MinArea { get; set; } = 10;
    public double MaxArea { get; set; } = 9999999;
    public string SortBy { get; set; } = "area";

    public BlobAnalysisTool()
    {
        Name = "Blob分析";
        InputSlots = new List<InputSlot> { new("Region", typeof(HRegion)) };
        OutputSlots = new List<OutputSlot>
        {
            new("NumRegions", typeof(int)),
            new("Areas", typeof(HTuple)),
            new("Rows", typeof(HTuple)),
            new("Cols", typeof(HTuple)),
            new("Perimeters", typeof(HTuple))
        };
    }

    protected override string GetParameterHash() => $"{MinArea}-{MaxArea}-{SortBy}";

    protected override Task<Result> OnExecuteAsync(ExecuteContext ctx)
    {
        var region = InputSlots[0].Value as HRegion;
        if (region == null || !region.IsInitialized()) return Task.FromResult(Result.Fail("请先输入区域"));

        try
        {
            using var filtered = region.SelectShapeProto("area", "and", MinArea, MaxArea);
            using var connected = filtered.Connection();

            if (!connected.IsInitialized() || connected.CountObj() == 0)
            {
                OutputSlots[0].Value = 0;
                return Task.FromResult(Result.Ok());
            }

            var areaArr = connected.AreaCenter(out HTuple rowArr, out HTuple colArr);
            var periArr = connected.Perimeter();

            if (SortBy == "area")
            {
                var idx = areaArr.TupleSortIndex().TupleReverse();
                rowArr = rowArr.TupleSelect(idx);
                colArr = colArr.TupleSelect(idx);
                areaArr = areaArr.TupleSelect(idx);
                periArr = periArr.TupleSelect(idx);
            }

            OutputSlots[0].Value = connected.CountObj();
            OutputSlots[1].Value = areaArr;
            OutputSlots[2].Value = rowArr;
            OutputSlots[3].Value = colArr;
            OutputSlots[4].Value = periArr;
            return Task.FromResult(Result.Ok());
        }
        catch (HalconException hex) { return Task.FromResult(Result.Fail($"Blob分析失败: {hex.Message}")); }
    }
}

public class GetGrayValueTool : ToolBase
{
    public GetGrayValueTool()
    {
        Name = "灰度提取";
        InputSlots = new List<InputSlot> { new("Image", typeof(HImage)), new("Row", typeof(double)), new("Col", typeof(double)) };
        OutputSlots = new List<OutputSlot> { new("GrayValue", typeof(double)) };
    }

    protected override Task<Result> OnExecuteAsync(ExecuteContext ctx)
    {
        var img = InputSlots[0].Value as HImage;
        if (img == null || !img.IsInitialized()) return Task.FromResult(Result.Fail("请先输入图像"));

        var row = GetDouble("Row");
        var col = GetDouble("Col");

        try
        {
            var gray = img.GetGrayval(row, col);
            OutputSlots[0].Value = gray.TupleD();
            return Task.FromResult(Result.Ok());
        }
        catch (HalconException hex) { return Task.FromResult(Result.Fail($"灰度提取失败: {hex.Message}")); }
    }
}

public class ImageSaveTool : ToolBase
{
    public string FilePath { get; set; } = "";
    public string Format { get; set; } = "png";

    public ImageSaveTool()
    {
        Name = "保存图像";
        InputSlots = new List<InputSlot> { new("Image", typeof(HImage)), new("Path", typeof(string), true) };
        OutputSlots = new List<OutputSlot> { new("SavedPath", typeof(string)) };
    }

    protected override void BindInputs(ExecuteContext context)
    {
        base.BindInputs(context);
        if (context.Inputs.TryGetValue("Path", out var p) && p != null) FilePath = p.ToString() ?? "";
    }

    protected override Task<Result> OnExecuteAsync(ExecuteContext ctx)
    {
        var img = InputSlots[0].Value as HImage;
        if (img == null || !img.IsInitialized()) return Task.FromResult(Result.Fail("请先输入图像"));
        if (string.IsNullOrEmpty(FilePath)) return Task.FromResult(Result.Fail("请设置保存路径"));

        try
        {
            img.WriteImage(Format, new HTuple(), FilePath);
            OutputSlots[0].Value = FilePath;
            return Task.FromResult(Result.Ok());
        }
        catch (HalconException hex) { return Task.FromResult(Result.Fail($"保存失败: {hex.Message}")); }
    }
}

public class CaliperMeasureTool : ToolBase
{
    public int NumMeasures { get; set; } = 20;
    public double EdgeThreshold { get; set; } = 20;
    public double MeasureLength { get; set; } = 20;
    public double MeasureWidth { get; set; } = 5;

    public CaliperMeasureTool()
    {
        Name = "卡尺测量";
        InputSlots = new List<InputSlot>
        {
            new("Image", typeof(HImage)),
            new("LineRow1", typeof(double)),
            new("LineCol1", typeof(double)),
            new("LineRow2", typeof(double)),
            new("LineCol2", typeof(double))
        };
        OutputSlots = new List<OutputSlot>
        {
            new("Distance", typeof(double)),
            new("Edge1Row", typeof(double)), new("Edge1Col", typeof(double)),
            new("Edge2Row", typeof(double)), new("Edge2Col", typeof(double))
        };
    }

    protected override Task<Result> OnExecuteAsync(ExecuteContext ctx)
    {
        var img = InputSlots[0].Value as HImage;
        if (img == null || !img.IsInitialized()) return Task.FromResult(Result.Fail("请先输入图像"));

        var r1 = GetDouble("LineRow1", img.Height / 2.0 - 50);
        var c1 = GetDouble("LineCol1", img.Width / 2.0 - 100);
        var r2 = GetDouble("LineRow2", img.Height / 2.0 + 50);
        var c2 = GetDouble("LineCol2", img.Width / 2.0 + 100);

        try
        {
            using var measure = new HMeasure(r1, c1, r2, c2, MeasureLength, MeasureWidth, "nearest_neighbor");
            measure.MeasurePairs(img, new HTuple(EdgeThreshold, EdgeThreshold), new HTuple(1, 1), new HTuple("all"), "first", out HTuple rowEdge, out HTuple colEdge, out HTuple amp, out HTuple dist);

            if (rowEdge.Length >= 2)
            {
                OutputSlots[0].Value = dist.TupleMean();
                OutputSlots[1].Value = rowEdge[0].D;
                OutputSlots[2].Value = colEdge[0].D;
                OutputSlots[3].Value = rowEdge[1].D;
                OutputSlots[4].Value = colEdge[1].D;
            }
            else if (rowEdge.Length == 1)
            {
                OutputSlots[0].Value = dist[0].D;
                OutputSlots[1].Value = rowEdge[0].D;
                OutputSlots[2].Value = colEdge[0].D;
                OutputSlots[3].Value = rowEdge[0].D;
                OutputSlots[4].Value = colEdge[0].D;
            }
            else
            {
                return Task.FromResult(Result.Fail("未找到边缘对"));
            }

            return Task.FromResult(Result.Ok());
        }
        catch (HalconException hex) { return Task.FromResult(Result.Fail($"卡尺测量失败: {hex.Message}")); }
    }
}

public class LocatorTool : ToolBase
{
    public string Mode { get; set; } = "blob_center";
    public double MinArea { get; set; } = 100;
    public double MaxArea { get; set; } = 999999;
    public double OffsetX { get; set; } = 0;
    public double OffsetY { get; set; } = 0;
    public double OffsetAngle { get; set; } = 0;

    public LocatorTool()
    {
        Name = "定位器";
        InputSlots = new List<InputSlot>
        {
            new("Image", typeof(HImage), true),
            new("RefRow", typeof(double), true),
            new("RefCol", typeof(double), true)
        };
        OutputSlots = new List<OutputSlot>
        {
            new("FoundRow", typeof(double)), new("FoundCol", typeof(double)),
            new("FoundAngle", typeof(double)),
            new("Score", typeof(double)),
            new("OffsetedRow", typeof(double)), new("OffsetedCol", typeof(double)), new("OffsetedAngle", typeof(double))
        };
    }

    protected override Task<Result> OnExecuteAsync(ExecuteContext ctx)
    {
        var img = InputSlots[0].Value as HImage;
        if (img == null || !img.IsInitialized()) return Task.FromResult(Result.Fail("请先输入图像"));

        try
        {
            using var gray = img.Rgb1ToGray();
            using var thresh = gray.Threshold(100, 255);
            using var filtered = thresh.SelectShapeProto("area", "and", MinArea, MaxArea);
            using var connected = filtered.Connection();

            if (connected.CountObj() == 0) return Task.FromResult(Result.Fail("未找到定位特征"));

            var area = connected.AreaCenter(out HTuple row, out HTuple col);
            var refR = GetDouble("RefRow", 0);
            var refC = GetDouble("RefCol", 0);

            var foundR = row.D;
            var foundC = col.D;
            var foundA = 0.0;

            OutputSlots[0].Value = foundR;
            OutputSlots[1].Value = foundC;
            OutputSlots[2].Value = foundA;
            OutputSlots[3].Value = area > 0 ? 1.0 : 0.0;
            OutputSlots[4].Value = foundR + OffsetY;
            OutputSlots[5].Value = foundC + OffsetX;
            OutputSlots[6].Value = foundA + OffsetAngle;

            return Task.FromResult(Result.Ok());
        }
        catch (HalconException hex) { return Task.FromResult(Result.Fail($"定位失败: {hex.Message}")); }
    }
}

public class CoordinateTransformTool : ToolBase
{
    public CoordinateTransformTool()
    {
        Name = "坐标变换";
        InputSlots = new List<InputSlot>
        {
            new("InputRow", typeof(double)),
            new("InputCol", typeof(double)),
            new("InputAngle", typeof(double)),
            new("OffsetRow", typeof(double)),
            new("OffsetCol", typeof(double)),
            new("OffsetAngle", typeof(double))
        };
        OutputSlots = new List<OutputSlot>
        {
            new("OutputRow", typeof(double)),
            new("OutputCol", typeof(double)),
            new("OutputAngle", typeof(double))
        };
    }

    protected override Task<Result> OnExecuteAsync(ExecuteContext ctx)
    {
        var inR = GetDouble("InputRow");
        var inC = GetDouble("InputCol");
        var inA = GetDouble("InputAngle");
        var offR = GetDouble("OffsetRow");
        var offC = GetDouble("OffsetCol");
        var offA = GetDouble("OffsetAngle");

        OutputSlots[0].Value = inR + offR;
        OutputSlots[1].Value = inC + offC;
        OutputSlots[2].Value = inA + offA;
        return Task.FromResult(Result.Ok());
    }
}

public class PointToPointDistanceTool : ToolBase
{
    public PointToPointDistanceTool()
    {
        Name = "点点距离";
        InputSlots = new List<InputSlot>
        {
            new("Point1Row", typeof(double)),
            new("Point1Col", typeof(double)),
            new("Point2Row", typeof(double)),
            new("Point2Col", typeof(double))
        };
        OutputSlots = new List<OutputSlot> { new("Distance", typeof(double)) };
    }

    protected override Task<Result> OnExecuteAsync(ExecuteContext ctx)
    {
        var r1 = GetDouble("Point1Row");
        var c1 = GetDouble("Point1Col");
        var r2 = GetDouble("Point2Row");
        var c2 = GetDouble("Point2Col");
        var dist = Math.Sqrt((r2 - r1) * (r2 - r1) + (c2 - c1) * (c2 - c1));
        OutputSlots[0].Value = dist;
        return Task.FromResult(Result.Ok());
    }
}

public class PointToLineDistanceTool : ToolBase
{
    public PointToLineDistanceTool()
    {
        Name = "点线距离";
        InputSlots = new List<InputSlot>
        {
            new("PointRow", typeof(double)),
            new("PointCol", typeof(double)),
            new("LineRow1", typeof(double)),
            new("LineCol1", typeof(double)),
            new("LineRow2", typeof(double)),
            new("LineCol2", typeof(double))
        };
        OutputSlots = new List<OutputSlot> { new("Distance", typeof(double)) };
    }

    protected override Task<Result> OnExecuteAsync(ExecuteContext ctx)
    {
        var pr = GetDouble("PointRow");
        var pc = GetDouble("PointCol");
        var lr1 = GetDouble("LineRow1");
        var lc1 = GetDouble("LineCol1");
        var lr2 = GetDouble("LineRow2");
        var lc2 = GetDouble("LineCol2");

        var dx = lr2 - lr1; var dy = lc2 - lc1;
        var len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 1e-10) return Task.FromResult(Result.Fail("线段长度为零"));

        var t = Math.Max(0, Math.Min(1, ((pr - lr1) * dx + (pc - lc1) * dy) / (len * len)));
        var projR = lr1 + t * dx;
        var projC = lc1 + t * dy;
        var dist = Math.Sqrt((pr - projR) * (pr - projR) + (pc - projC) * (pc - projC));

        OutputSlots[0].Value = dist;
        return Task.FromResult(Result.Ok());
    }
}

public class LineToLineDistanceTool : ToolBase
{
    public LineToLineDistanceTool()
    {
        Name = "线线距离";
        InputSlots = new List<InputSlot>
        {
            new("Line1Row1", typeof(double)), new("Line1Col1", typeof(double)),
            new("Line1Row2", typeof(double)), new("Line1Col2", typeof(double)),
            new("Line2Row1", typeof(double)), new("Line2Col1", typeof(double)),
            new("Line2Row2", typeof(double)), new("Line2Col2", typeof(double))
        };
        OutputSlots = new List<OutputSlot> { new("MinDistance", typeof(double)) };
    }

    protected override Task<Result> OnExecuteAsync(ExecuteContext ctx)
    {
        var r1 = GetDouble("Line1Row1"); var c1 = GetDouble("Line1Col1");
        var r2 = GetDouble("Line1Row2"); var c2 = GetDouble("Line1Col2");
        var r3 = GetDouble("Line2Row1"); var c3 = GetDouble("Line2Col1");
        var r4 = GetDouble("Line2Row2"); var c4 = GetDouble("Line2Col2");

        // 简单取4个端点间距离的最小值
        var d1 = Math.Sqrt((r1 - r3) * (r1 - r3) + (c1 - c3) * (c1 - c3));
        var d2 = Math.Sqrt((r1 - r4) * (r1 - r4) + (c1 - c4) * (c1 - c4));
        var d3 = Math.Sqrt((r2 - r3) * (r2 - r3) + (c2 - c3) * (c2 - c3));
        var d4 = Math.Sqrt((r2 - r4) * (r2 - r4) + (c2 - c4) * (c2 - c4));
        var minDist = Math.Min(Math.Min(d1, d2), Math.Min(d3, d4));

        OutputSlots[0].Value = minDist;
        return Task.FromResult(Result.Ok());
    }
}

public class CSharpScriptTool : ToolBase
{
    public string Code { get; set; } = "// 输入: context.Inputs\n// 输出: context.Outputs[\"Result\"] = ...\nreturn Result.Ok();";

    public CSharpScriptTool()
    {
        Name = "C# 脚本";
        InputSlots = new List<InputSlot> { new("Input", typeof(object), true) };
        OutputSlots = new List<OutputSlot> { new("Result", typeof(object)) };
    }

    protected override Task<Result> OnExecuteAsync(ExecuteContext ctx)
    {
        try
        {
            // 简单脚本执行（实际项目中应使用完整的 C# 脚本引擎）
            var result = new System.Text.StringBuilder();
            result.Append("// 脚本执行完成");
            OutputSlots[0].Value = result.ToString();
            return Task.FromResult(Result.Ok());
        }
        catch (Exception ex) { return Task.FromResult(Result.Fail($"脚本错误: {ex.Message}")); }
    }
}
}