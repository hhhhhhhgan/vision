using HalconDotNet;

namespace VisionFlow {

/// <summary>
/// HALCON 性能优化配置
/// 在程序启动时应用这些设置
/// </summary>
public static class HALCONPerfConfig
{
    /// <summary>
    /// 应用所有性能优化设置
    /// </summary>
    public static void Apply()
    {
        // ====== 1. 并行化设置 ======
        // 启用/禁用HALCON内部并行化（多核利用）
        HSystem.SetSystem("parallelize_operators", "true");  // 启用操作符级并行
        HSystem.SetSystem("parallelize_functions", "true"); // 启用函数级并行

        // ====== 2. 内存管理 ======
        // 设置HALCON使用的临时内存上限（MB）- 留一半给系统
        long memMB = GetAvailableMemoryMB() / 2;
        HSystem.SetSystem("temp_mem_max", memMB.ToString());

        // 禁用"内存不足"警告（避免频繁弹窗）
        HSystem.SetSystem("abort_err_bg", "false");

        // ====== 3. 图像缓存 ======
        // 预分配图像缓存大小（MB）- 根据图像尺寸设置
        HSystem.SetSystem("image_cache_total_mem", "512");

        // ====== 4. 显示优化 ======
        // 关闭HALCON窗口的抗锯齿（加速渲染）
        // 注意：此设置仅影响HALCON窗口，不影响WPF
        HSystem.SetSystem("flush_graphic", "true");

        // ====== 5. 线程数设置 ======
        // 允许HALCON使用最多N个线程（0=自动=CPU核心数）
        int maxThreads = Math.Max(1, Environment.ProcessorCount - 1);
        HSystem.SetSystem("parallelize_operators_max_num_threads", maxThreads.ToString());

        // ====== 6. 数值精度 ======
        // 使用更快的浮点运算（牺牲一点精度）
        HSystem.SetSystem("tuple_type", "mixed"); // mixed比strict快

        // ====== 7. 调试输出 ======
        // 关闭调试输出加速
        HSystem.SetSystem("db_time", "false"); // 关闭调试计时

        System.Diagnostics.Debug.WriteLine($"[HALCON Perf] 并行线程: {maxThreads}, 内存上限: {memMB}MB");
    }

    /// <summary>
    /// 获取可用内存（MB）
    /// </summary>
    private static long GetAvailableMemoryMB()
    {
        try
        {
            using var proc = System.Diagnostics.Process.GetCurrentProcess();
            var info = proc.PriorityClass;
            proc.Dispose();
            // 使用GC获取可用内存
            return GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024 * 1024);
        }
        catch
        {
            return 4096; // 默认4GB
        }
    }

    /// <summary>
    /// 图像处理预热（首次执行优化）
    /// </summary>
    public static void WarmUp()
    {
        System.Diagnostics.Debug.WriteLine("[HALCON WarmUp] 开始预热...");

        var sw = System.Diagnostics.Stopwatch.StartNew();

        // 创建测试图像
        using var testImg = new HImage();
        testImg.GenImageConst("byte", 640, 480);

        // 测试基本操作
        using var gray = testImg.Rgb1ToGray();
        using var thresh = gray.Threshold(128, 255);
        using var open = thresh.Opening(new HRegion(3, 3).GenCircle(1.5), 1);

        // 测试区域分析
        var area = open.AreaCenter(out _, out _);

        sw.Stop();
        System.Diagnostics.Debug.WriteLine($"[HALCON WarmUp] 完成，耗时: {sw.ElapsedMilliseconds}ms");
    }
}
}