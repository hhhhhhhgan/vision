using System;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using System.Collections.Generic;
using System.Linq;

namespace VisionFlow.Views.ToolDialogs {

/// <summary>
/// C# 代码自动补齐提供器
/// </summary>
public class CSharpCompletionProvider
{
    private readonly List<CompletionData> _completions = new();

    public CSharpCompletionProvider()
    {
        // C# 关键字
        AddKeywords();
        // HALCON 类型
        AddHalconTypes();
        // VisionFlow 内置变量
        AddBuiltinVars();
        // Math 函数
        AddMathFunctions();
    }

    private void Add(string text, string description, string insertText)
        => _completions.Add(new CompletionData(text, description, insertText));

    private void AddKeywords()
    {
        var keywords = new[] {
            ("var", "隐式类型"),
            ("double", "64位浮点数"),
            ("int", "32位整数"),
            ("string", "字符串"),
            ("bool", "布尔值"),
            ("true", "真"),
            ("false", "假"),
            ("null", "空引用"),
            ("return", "返回语句"),
            ("if", "条件语句"),
            ("else", "否则分支"),
            ("for", "for循环"),
            ("foreach", "foreach循环"),
            ("while", "while循环"),
            ("break", "跳出循环"),
            ("continue", "继续下一次循环"),
            ("this", "当前实例"),
            ("new", "新建对象"),
            ("typeof", "获取类型"),
            ("nameof", "获取名称"),
            ("try", "尝试块"),
            ("catch", "捕获异常"),
            ("finally", "最终块"),
            ("throw", "抛出异常"),
            ("using", "using语句"),
            ("namespace", "命名空间"),
            ("class", "类定义"),
            ("public", "公共成员"),
            ("private", "私有成员"),
            ("protected", "保护成员"),
            ("internal", "内部成员"),
            ("static", "静态成员"),
            ("readonly", "只读字段"),
            ("const", "常量"),
            ("in", "输入参数修饰符"),
            ("out", "输出参数修饰符"),
            ("ref", "引用参数"),
            ("async", "异步方法"),
            ("await", "等待异步操作"),
            ("Task", "异步任务"),
            ("List", "泛型列表"),
            ("Dictionary", "字典"),
            ("Action", "动作委托"),
            ("Func", "函数委托"),
            ("event", "事件声明"),
            ("override", "重写方法"),
            ("virtual", "虚方法"),
            ("abstract", "抽象成员"),
            ("interface", "接口定义"),
            ("enum", "枚举定义"),
            ("struct", "结构体"),
            ("switch", "switch语句"),
            ("case", "case标签"),
            ("default", "默认分支"),
            ("do", "do-while循环"),
            ("lock", "锁定语句"),
            ("checked", "检查溢出"),
            ("unchecked", "取消检查"),
            ("unsafe", "不安全代码"),
            ("params", "可变参数"),
        };

        foreach (var (kw, desc) in keywords)
            Add(kw, desc, kw);
    }

    private void AddHalconTypes()
    {
        // HImage 成员
        Add("Width", "图像宽度 (int)", "Width");
        Add("Height", "图像高度 (int)", "Height");
        Add("Channels()", "通道数", "Channels()");
        Add("GetImageSize()", "获取图像尺寸", "GetImageSize(out _, out _)");
        Add("DispImage()", "显示图像", "DispImage(HalconWindow)");
        Add("ReduceDomain()", "ROI裁剪", "ReduceDomain(Region)");
        Add("Threshold()", "阈值分割", "Threshold(_, _)");
        Add("DynThreshold()", "动态阈值", "DynThreshold(_, _, _)");
        Add("TransposeImage()", "转置图像", "TransposeImage(_)");
        Add("RotateImage()", "旋转图像", "RotateImage(_, _)");
        Add("FlipImage()", "翻转图像", "FlipImage(_)");
        Add("ZoomImage()", "缩放图像", "ZoomImage(_, _)");
        Add("CropDomain()", "裁剪ROI", "CropDomain()");
        Add("MirrorImage()", "镜像图像", "MirrorImage(_)");
        Add("Emphasize()", "增强对比度", "Emphasize(_, _, _, _, _)");
        Add("Illuminate()", "光照校正", "Illuminate(_, _, _, _, _)");
        Add("MeanImage()", "均值滤波", "MeanImage(_, _, _)");
        Add("GaussImage()", "高斯滤波", "GaussImage(_)");
        Add("MedianImage()", "中值滤波", "MedianImage(_)");

        // HRegion 成员
        Add("Area", "面积 (double)", "Area");
        Add("Centroid()", "中心点坐标", "Centroid(out _, out _)");
        Add("Orientation()", "方向角度", "Orientation()");
        Add("SmallestRectangle1()", "最小外接矩形", "SmallestRectangle1(out _, out _, out _, out _)");
        Add("SmallestCircle()", "最小外接圆", "SmallestCircle(out _, out _, out _)");
        Add("Connection()", "连通域分析", "Connection()");
        Add("SelectShape()", "按形状特征筛选", "SelectShape(_, _, _, _, _)");
        Add("Union1()", "合并区域", "Union1()");
        Add("Intersection()", "交集", "Intersection(_)");
        Add("Difference()", "差集", "Difference(_)");
        Add("Complement()", "补集", "Complement()");
        Add("GenRectangle1()", "生成矩形", "GenRectangle1(_, _, _, _)");
        Add("GenCircle()", "生成圆形", "GenCircle(_, _, _)");
        Add("GenEllipse()", "生成椭圆", "GenEllipse(_, _, _, _, _)");
        Add("DilationCircle()", "圆形膨胀", "DilationCircle(_, _)");
        Add("ErosionCircle()", "圆形腐蚀", "ErosionCircle(_, _)");
        Add("OpeningCircle()", "圆形开运算", "OpeningCircle(_, _)");
        Add("ClosingCircle()", "圆形闭运算", "ClosingCircle(_, _)");
        Add("FillUp()", "填充空洞", "FillUp()");
        Add("Boundary()", "边界", "Boundary()");

        // HXLD 成员
        Add("GenContourPolygonXld()", "生成XLD多边形", "GenContourPolygonXld(_, _)");
        Add("GenCircleContourXld()", "生成XLD圆弧", "GenCircleContourXld(_, _, _, _, _, _, _)");
        Add("FitLineContourXld()", "拟合直线", "FitLineContourXld(_, _, _, _, _, _, _, _)");
        Add("FitCircleContourXld()", "拟合圆", "FitCircleContourXld(_, _, _, _, _, _, _)");
        Add("FitEllipseContourXld()", "拟合椭圆", "FitEllipseContourXld(_, _, _, _, _, _, _, _)");
        Add("LengthContour()", "轮廓长度", "LengthContour()");
        Add("AreaCenterXld()", "XLD面积和中心", "AreaCenterXld(out _, out _, out _)");

        // HALCON 类型
        Add("HImage", "HALCON图像类型", "HImage");
        Add("HRegion", "HALCON区域类型", "HRegion");
        Add("HXLD", "HALCON轮廓类型", "HXLD");
        Add("HTuple", "HALCON元组类型", "HTuple");
        Add("HalconWindow", "HALCON窗口 (用于显示)", "HalconWindow");

        // 查找线/圆输出
        Add("Row1", "直线端点1 行坐标", "Row1");
        Add("Col1", "直线端点1 列坐标", "Col1");
        Add("Row2", "直线端点2 行坐标", "Row2");
        Add("Col2", "直线端点2 列坐标", "Col2");
        Add("Row", "圆心 行坐标", "Row");
        Add("Column", "圆心 列坐标", "Column");
        Add("Radius", "半径", "Radius");
        Add("Distance", "距离值", "Distance");
        Add("Result", "脚本输出结果", "Result");
    }

    private void AddBuiltinVars()
    {
        Add("Image", "输入图像 (HImage?)", "Image");
        Add("Region", "输入区域 (HRegion?)", "Region");
        Add("Value", "输入数值 (double?)", "Value");
        Add("Result", "输出结果 (必须赋值)", "Result = ");
        Add("HalconWindow", "HALCON显示窗口", "HalconWindow");
    }

    private void AddMathFunctions()
    {
        var mathFuncs = new[] {
            ("Math.Abs()", "绝对值", "Math.Abs()"),
            ("Math.Sqrt()", "平方根", "Math.Sqrt()"),
            ("Math.Pow()", "幂函数", "Math.Pow(, )"),
            ("Math.Min()", "最小值", "Math.Min(, )"),
            ("Math.Max()", "最大值", "Math.Max(, )"),
            ("Math.Round()", "四舍五入", "Math.Round()"),
            ("Math.Floor()", "向下取整", "Math.Floor()"),
            ("Math.Ceiling()", "向上取整", "Math.Ceiling()"),
            ("Math.Sin()", "正弦", "Math.Sin()"),
            ("Math.Cos()", "余弦", "Math.Cos()"),
            ("Math.Tan()", "正切", "Math.Tan()"),
            ("Math.Asin()", "反正弦", "Math.Asin()"),
            ("Math.Acos()", "反余弦", "Math.Acos()"),
            ("Math.Atan()", "反正切", "Math.Atan()"),
            ("Math.Atan2()", "二维反正切", "Math.Atan2(, )"),
            ("Math.Log()", "自然对数", "Math.Log()"),
            ("Math.Log10()", "常用对数", "Math.Log10()"),
            ("Math.Exp()", "指数函数", "Math.Exp()"),
            ("Math.PI", "圆周率 π", "Math.PI"),
            ("Math.E", "自然常数 e", "Math.E"),
            ("Math.Sign()", "符号函数", "Math.Sign()"),
            ("Math.Truncate()", "截断小数", "Math.Truncate()"),
            ("double.IsNaN()", "是否为NaN", "double.IsNaN()"),
            ("double.IsInfinity()", "是否无穷", "double.IsInfinity()"),
        };

        foreach (var (name, desc, insert) in mathFuncs)
            Add(name, desc, insert);
    }

    public void ShowCompletion(TextArea textArea, IEnumerable<CompletionData> data)
    {
        var completionWindow = new CompletionWindow(textArea)
        {
            Width = 320,
            Height = 350
        };

        foreach (var item in data)
            completionWindow.CompletionList.CompletionData.Add(item);

        completionWindow.CompletionList.SelectItem(_completions.FirstOrDefault());
        completionWindow.Show();
        completionWindow.Closed += (_, _) => completionWindow = null;
    }

    public IEnumerable<CompletionData> GetCompletions(string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
            return _completions;

        return _completions.Where(c =>
            c.Text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
            c.Text.Contains(prefix, StringComparison.OrdinalIgnoreCase));
    }
}

public class CompletionData : ICompletionData
{
    public string Text { get; }
    public object Description { get; }
    public string InsertText { get; }
    public object Content => Text;
    public double Priority => 0;

    public CompletionData(string text, string description, string insertText)
    {
        Text = text;
        Description = description;
        InsertText = insertText;
    }

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        var startOffset = completionSegment.Offset;
        var length = completionSegment.Length;
        var text = textArea.Document.GetText(startOffset, length);
        textArea.Document.Replace(startOffset, length, InsertText);
    }
}
}