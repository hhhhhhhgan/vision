using System.Collections.Generic;
using System.Linq;
namespace VisionFlow.Tools {

public enum ToolCategory
{
    Input,       // 图像输入
    Processing,  // 图像处理
    Detection,   // 缺陷检测
    Measurement, // 测量
    Analysis,    // 分析
    Transform,   // 坐标变换
    Utility      // 工具
}

public class ToolDescriptor
{
    public Type ToolType { get; }
    public string Name { get; }
    public string Icon { get; }
    public string Description { get; }
    public ToolCategory Category { get; }

    public ToolDescriptor(Type toolType, string name, string icon, string description, ToolCategory category)
    {
        ToolType = toolType;
        Name = name;
        Icon = icon;
        Description = description;
        Category = category;
    }
}

public static class ToolRegistry
{
    public static readonly List<ToolDescriptor> All = new()
    {
        // 图像输入
        new(typeof(ImageLoadTool), "图像加载", "🖼", "从文件加载图像", ToolCategory.Input),

        // 图像处理
        new(typeof(ImageRotateTool), "图像旋转", "🔄", "旋转图像指定角度", ToolCategory.Processing),
        new(typeof(ROIDrawTool), "ROI绘制", "📐", "在图像上绘制ROI区域", ToolCategory.Processing),

        // 缺陷检测
        new(typeof(ThresholdTool), "阈值分割", "🔲", "对图像进行阈值分割提取区域", ToolCategory.Detection),
        new(typeof(MorphologyTool), "形态学", "🔠", "腐蚀/膨胀/开/闭运算", ToolCategory.Detection),
        new(typeof(LocatorTool), "定位器", "🎯", "查找特征位置并应用偏移补正", ToolCategory.Detection),
        new(typeof(FindLineTool), "查找线", "📏", "检测图像中的直线边缘", ToolCategory.Detection),
        new(typeof(FindCircleTool), "查找圆", "⭕", "检测图像中的圆形边缘", ToolCategory.Detection),

        // 测量
        new(typeof(CaliperMeasureTool), "卡尺测量", "📏", "卡尺工具测量两点间距离", ToolCategory.Measurement),
        new(typeof(PointToPointDistanceTool), "点点距离", "📍", "计算两点之间的欧氏距离", ToolCategory.Measurement),
        new(typeof(PointToLineDistanceTool), "点线距离", "📍", "计算点到线段的垂直距离", ToolCategory.Measurement),
        new(typeof(LineToLineDistanceTool), "线线距离", "📏", "计算两条线段间的最小距离", ToolCategory.Measurement),
        new(typeof(GetGrayValueTool), "灰度提取", "🖐", "提取指定坐标的灰度值", ToolCategory.Measurement),

        // 分析
        new(typeof(BlobAnalysisTool), "Blob分析", "📊", "连通域分析获取区域特征", ToolCategory.Analysis),

        // 坐标变换
        new(typeof(CoordinateTransformTool), "坐标变换", "🔄", "应用定位偏移到坐标（跟随定位器）", ToolCategory.Transform),

        // 工具
        new(typeof(CSharpScriptTool), "C# 脚本", "📜", "执行自定义 C# 脚本代码", ToolCategory.Utility),
        new(typeof(ImageSaveTool), "保存图像", "💾", "将图像保存到文件", ToolCategory.Utility),
    };

    public static IEnumerable<IGrouping<ToolCategory, ToolDescriptor>> ByCategory
        => All.GroupBy(t => t.Category);

    public static readonly Dictionary<string, Type> TypeMap = All.ToDictionary(t => t.Name, t => t.ToolType);
}

public static class ToolCategoryInfo
{
    private static readonly Dictionary<ToolCategory, (string Name, string Emoji, string Color)> Info = new()
    {
        [ToolCategory.Input] = ("📥 图像输入", "🖼", "#1E40AF"),
        [ToolCategory.Processing] = ("🎨 图像处理", "🎨", "#6D28D9"),
        [ToolCategory.Detection] = ("🔍 缺陷检测", "🔍", "#991B1B"),
        [ToolCategory.Measurement] = ("📏 测量", "📏", "#065F46"),
        [ToolCategory.Analysis] = ("📊 分析", "📊", "#92400E"),
        [ToolCategory.Transform] = ("🔄 坐标变换", "🔄", "#0E7490"),
        [ToolCategory.Utility] = ("🔧 工具", "🔧", "#374151"),
    };

    public static string GetName(ToolCategory cat) => Info.GetValueOrDefault(cat).Name ?? cat.ToString();
    public static string GetEmoji(ToolCategory cat) => Info.GetValueOrDefault(cat).Emoji ?? "📦";
    public static string GetColor(ToolCategory cat) => Info.GetValueOrDefault(cat).Color ?? "#374151";
}
}