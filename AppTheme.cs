namespace VisionFlow {

/// <summary>
/// 全局配色方案
/// </summary>
public static class AppTheme
{
    // 主色调
    public static readonly string PrimaryBlue = "#0D6EFD";      // 主蓝色
    public static readonly string PrimaryBlueDark = "#0A58CA";   // 深蓝（悬停）
    public static readonly string PrimaryBlueLight = "#3D8BFD";  // 浅蓝

    // 背景色
    public static readonly string BackgroundDark = "#0F1923";    // 最深背景
    public static readonly string BackgroundMain = "#1A2634";   // 主背景
    public static readonly string BackgroundCard = "#243447";   // 卡片/面板
    public static readonly string BackgroundElevated = "#2D4058"; // 悬浮/选中

    // 边框
    public static readonly string BorderDefault = "#3D5A73";     // 默认边框
    public static readonly string BorderHover = "#5A8FD4";       // 悬停边框

    // 文字
    public static readonly string TextPrimary = "#E8F4FF";       // 主文字
    public static readonly string TextSecondary = "#8BA4C7";      // 次要文字
    public static readonly string TextMuted = "#5A7390";         // 弱化文字

    // 状态色
    public static readonly string Success = "#10B981";            // 绿色
    public static readonly string Warning = "#F59E0B";            // 橙色
    public static readonly string Error = "#EF4444";             // 红色
    public static readonly string Info = "#06B6D4";              // 青色

    // 端口颜色
    public static readonly string PortInput = "#10B981";          // 输入口-绿
    public static readonly string PortOutput = "#3B82F6";        // 输出口-蓝
    public static readonly string PortConnected = "#F59E0B";      // 已连接-橙

    // 渐变色
    public static readonly string GradientBlue = "linear-gradient(135deg, #0D6EFD 0%, #0A58CA 100%)";
}
}