using System;
using System.Windows;
using VisionFlow;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // HALCON 性能优化配置
        try { HALCONPerfConfig.Apply(); HALCONPerfConfig.WarmUp(); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[HALCON] 配置失败: {ex.Message}"); }

        base.OnStartup(e);
    }
}