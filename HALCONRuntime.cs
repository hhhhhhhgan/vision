using System;
using System.Collections.Concurrent;
using HalconDotNet;
using System.Collections.Generic;

namespace VisionFlow {

/// <summary>
/// HALCON 运行时管理器 — 集中初始化、预热、缓存
/// </summary>
public static class HALCONRuntime
{
    private static bool _initialized;
    private static readonly object _lock = new();
    private static readonly ConcurrentDictionary<string, WeakReference<HImage>> _imageCache = new();
    private static readonly ConcurrentDictionary<string, WeakReference<HRegion>> _regionCache = new();

    /// <summary>最后初始化耗时（ms）</summary>
    public static double InitDurationMs { get; private set; }

    /// <summary>是否已初始化</summary>
    public static bool IsInitialized => _initialized;

    /// <summary>
    /// 初始化 HALCON 环境（启动时调用一次）
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;

        var sw = System.Diagnostics.Stopwatch.StartNew();

        lock (_lock)
        {
            if (_initialized) return;

            // 1. 设置环境变量
            // 优先使用用户安装目录 (HalconDotNet.WPF.dll 路径)
            string halconRoot = @"C:\Users\xy0077\AppData\Local\Programs\MVTec\HALCON-26.05-Progress";
            if (!System.IO.Directory.Exists(halconRoot))
                halconRoot = @"C:\Users\xy0077\AppData\Local\Programs\MVTec\HALCON-24.11-Progress-Steady";
            if (!System.IO.Directory.Exists(halconRoot))
                halconRoot = @"C:\Program Files\MVTec\HALCON-26.05-Progress";
            if (!System.IO.Directory.Exists(halconRoot))
                halconRoot = @"C:\Program Files\MVTec\HALCON-24.11-Progress-Steady";
            {
                SetEnv("HALCONROOT", halconRoot);
                SetEnv("HALCONEXAMPLES", System.IO.Path.Combine(halconRoot, "examples"));
                SetEnv("HALCONIMAGES", System.IO.Path.Combine(halconRoot, "images"));

                string binPath = System.IO.Path.Combine(halconRoot, "bin", "dotnet35");
                if (!System.IO.Directory.Exists(binPath))
                    binPath = System.IO.Path.Combine(halconRoot, "bin", "x64-win64");
                if (!System.IO.Directory.Exists(binPath))
                    binPath = System.IO.Path.Combine(halconRoot, "bin", "dotnet5");
                if (System.IO.Directory.Exists(binPath))
                {
                    string currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
                    if (!currentPath.Contains(binPath))
                        SetEnv("PATH", currentPath + ";" + binPath);
                }
            }

            // 2. 触发 HALCON 许可证检查（提前执行避免延迟）
            try
            {
                // 创建一个临时图像操作来触发 HALCON 初始化
                using var tempImage = new HImage();
                tempImage.GenConstImage(1, 1, "byte", 0);

                // 预热一些常用操作
                using var region = new HRegion();
                region.GenRectangle1(0, 0, 1, 1);
                region.AreaCenter(out _, out _);
            }
            catch { /* 忽略初始化错误 */ }

            _initialized = true;
        }

        sw.Stop();
        InitDurationMs = sw.Elapsed.TotalMilliseconds;
    }

    private static void SetEnv(string key, string value)
    {
        try { Environment.SetEnvironmentVariable(key, value, EnvironmentVariableTarget.Process); } catch { }
    }

    /// <summary>
    /// 从缓存获取图像（如果可用且未销毁）
    /// </summary>
    public static HImage? GetCachedImage(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (_imageCache.TryGetValue(path, out var wr) && wr.TryGetTarget(out var img))
            return img.CopyImage();
        return null;
    }

    /// <summary>
    /// 缓存图像（弱引用，不阻止GC）
    /// </summary>
    public static void CacheImage(string path, HImage image)
    {
        if (string.IsNullOrEmpty(path) || image == null) return;
        _imageCache[path] = new WeakReference<HImage>(image.CopyImage());
    }

    /// <summary>
    /// 清空所有缓存
    /// </summary>
    public static void ClearCache()
    {
        _imageCache.Clear();
        _regionCache.Clear();
    }

    /// <summary>
    /// 获取当前缓存状态
    /// </summary>
    public static (int imageCount, int regionCount) GetCacheStats()
    {
        int img = 0, reg = 0;
        foreach (var _ in _imageCache)
            if (_imageCache.TryGetValue(_.Key, out var w) && w.TryGetTarget(out _)) img++;
        foreach (var _ in _regionCache)
            if (_regionCache.TryGetValue(_.Key, out var w) && w.TryGetTarget(out _)) reg++;
        return (img, reg);
    }
}
}