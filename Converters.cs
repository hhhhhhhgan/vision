using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using VisionFlow.Tools;

namespace VisionFlow {

public class BoolToStarConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? "●" : "";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class CategoryNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ToolCategory cat)
            return ToolCategoryInfo.GetName(cat);
        return value?.ToString() ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class CategoryColorConverter : IValueConverter
{
    private static readonly Dictionary<ToolCategory, string> Colors = new()
    {
        [ToolCategory.Input] = "#1E40AF",       // 深蓝
        [ToolCategory.Processing] = "#6D28D9",   // 紫色
        [ToolCategory.Detection] = "#991B1B",   // 深红
        [ToolCategory.Measurement] = "#065F46",   // 深绿
        [ToolCategory.Analysis] = "#92400E",     // 棕色
        [ToolCategory.Transform] = "#0E7490",    // 青色
        [ToolCategory.Utility] = "#374151",       // 灰色
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ToolCategory cat && Colors.TryGetValue(cat, out var hex))
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            return color;
        }
        return Colors[ToolCategory.Utility];
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
}