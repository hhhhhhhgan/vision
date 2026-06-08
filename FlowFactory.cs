using System.Collections.Generic;
using VisionFlow.Models;
using VisionFlow.Tools;

namespace VisionFlow;

/// <summary>
/// 流程工厂 — 创建各种测试流程验证工具互通性
/// </summary>
public static class FlowFactory
{
    // ========== 流程1：图像加载 → 灰度转换 → 阈值分割 → 形态学 → Blob分析 ==========
    public static (List<FlowNode> nodes, List<Connection> connections) CreateBlobDetectionFlow()
    {
        var nodes = new List<FlowNode>();
        var connections = new List<Connection>();

        // 1. 图像加载
        var load = new FlowNode
        {
            Id = "n1_load",
            X = 50, Y = 80,
            Tool = new ImageLoadTool { Name = "图像加载", FilePath = "test.jpg" }
        };
        nodes.Add(load);

        // 2. 阈值分割
        var thresh = new FlowNode
        {
            Id = "n2_thresh",
            X = 250, Y = 80,
            Tool = new ThresholdTool { Name = "阈值分割", MinGray = 100, MaxGray = 255 }
        };
        nodes.Add(thresh);

        // 3. 形态学(开)
        var morph = new FlowNode
        {
            Id = "n3_morph",
            X = 450, Y = 80,
            Tool = new MorphologyTool { Name = "形态学开", Operation = "opening", ElementSize = 3, Iterations = 1 }
        };
        nodes.Add(morph);

        // 4. Blob分析
        var blob = new FlowNode
        {
            Id = "n4_blob",
            X = 650, Y = 80,
            Tool = new BlobAnalysisTool { Name = "Blob分析", MinArea = 100, MaxArea = 9999999, SortBy = "area" }
        };
        nodes.Add(blob);

        connections.Add(new Connection { Id = "c1", SourceNode = load, SourceSlot = "Image", TargetNode = thresh, TargetSlot = "Image" });
        connections.Add(new Connection { Id = "c2", SourceNode = thresh, SourceSlot = "Region", TargetNode = morph, TargetSlot = "Region" });
        connections.Add(new Connection { Id = "c3", SourceNode = morph, SourceSlot = "Region", TargetNode = blob, TargetSlot = "Region" });

        return (nodes, connections);
    }

    // ========== 流程2：图像 → 定位器 → 查找线 → 坐标变换 → 点线距离 ==========
    public static (List<FlowNode> nodes, List<Connection> connections) CreateLocatorFlow()
    {
        var nodes = new List<FlowNode>();
        var connections = new List<Connection>();

        var load = new FlowNode { Id = "n1", X = 50, Y = 50, Tool = new ImageLoadTool { Name = "图像加载", FilePath = "test.jpg" } };
        nodes.Add(load);

        var locator = new FlowNode { Id = "n2", X = 250, Y = 50, Tool = new LocatorTool { Name = "定位器", Mode = "blob_center", OffsetX = 10, OffsetY = 10 } };
        nodes.Add(locator);

        var findLine = new FlowNode { Id = "n3", X = 450, Y = 50, Tool = new FindLineTool { Name = "查找线", EdgeThreshold = 20, MinLineLength = 30 } };
        nodes.Add(findLine);

        var coord = new FlowNode { Id = "n4", X = 450, Y = 180, Tool = new CoordinateTransformTool { Name = "坐标变换" } };
        nodes.Add(coord);

        var p2l = new FlowNode { Id = "n5", X = 650, Y = 120, Tool = new PointToLineDistanceTool { Name = "点线距离" } };
        nodes.Add(p2l);

        connections.Add(new Connection { Id = "c1", SourceNode = load, SourceSlot = "Image", TargetNode = locator, TargetSlot = "Image" });
        connections.Add(new Connection { Id = "c2", SourceNode = locator, SourceSlot = "OffsetedRow", TargetNode = coord, TargetSlot = "InputRow" });
        connections.Add(new Connection { Id = "c3", SourceNode = locator, SourceSlot = "OffsetedCol", TargetNode = coord, TargetSlot = "InputCol" });
        connections.Add(new Connection { Id = "c4", SourceNode = findLine, SourceSlot = "Row1", TargetNode = p2l, TargetSlot = "LineRow1" });
        connections.Add(new Connection { Id = "c5", SourceNode = findLine, SourceSlot = "Col1", TargetNode = p2l, TargetSlot = "LineCol1" });
        connections.Add(new Connection { Id = "c6", SourceNode = findLine, SourceSlot = "Row2", TargetNode = p2l, TargetSlot = "LineRow2" });
        connections.Add(new Connection { Id = "c7", SourceNode = findLine, SourceSlot = "Col2", TargetNode = p2l, TargetSlot = "LineCol2" });
        connections.Add(new Connection { Id = "c8", SourceNode = coord, SourceSlot = "OutputRow", TargetNode = p2l, TargetSlot = "PointRow" });
        connections.Add(new Connection { Id = "c9", SourceNode = coord, SourceSlot = "OutputCol", TargetNode = p2l, TargetSlot = "PointCol" });

        return (nodes, connections);
    }

    // ========== 流程3：图像旋转 → 灰度提取 → 卡尺测量 → 保存 ==========
    public static (List<FlowNode> nodes, List<Connection> connections) CreateMeasureFlow()
    {
        var nodes = new List<FlowNode>();
        var connections = new List<Connection>();

        var load = new FlowNode { Id = "n1", X = 50, Y = 80, Tool = new ImageLoadTool { Name = "图像加载", FilePath = "test.jpg" } };
        nodes.Add(load);

        var rotate = new FlowNode { Id = "n2", X = 250, Y = 80, Tool = new ImageRotateTool { Name = "图像旋转", Angle = 45 } };
        nodes.Add(rotate);

        var gray = new FlowNode { Id = "n3", X = 450, Y = 80, Tool = new GetGrayValueTool { Name = "灰度提取" } };
        nodes.Add(gray);

        var caliper = new FlowNode { Id = "n4", X = 450, Y = 200, Tool = new CaliperMeasureTool { Name = "卡尺测量", NumMeasures = 20, EdgeThreshold = 20 } };
        nodes.Add(caliper);

        var save = new FlowNode { Id = "n5", X = 650, Y = 140, Tool = new ImageSaveTool { Name = "保存图像", FilePath = "output.png" } };
        nodes.Add(save);

        connections.Add(new Connection { Id = "c1", SourceNode = load, SourceSlot = "Image", TargetNode = rotate, TargetSlot = "Image" });
        connections.Add(new Connection { Id = "c2", SourceNode = rotate, SourceSlot = "Image", TargetNode = gray, TargetSlot = "Image" });
        connections.Add(new Connection { Id = "c3", SourceNode = rotate, SourceSlot = "Image", TargetNode = caliper, TargetSlot = "Image" });
        connections.Add(new Connection { Id = "c4", SourceNode = rotate, SourceSlot = "Image", TargetNode = save, TargetSlot = "Image" });

        return (nodes, connections);
    }

    // ========== 流程4：完整检测流程（图像加载 → 阈值 → 形态学 → Blob → 条件判断 → 保存） ==========
    public static (List<FlowNode> nodes, List<Connection> connections) CreateCompleteInspectionFlow()
    {
        var nodes = new List<FlowNode>();
        var connections = new List<Connection>();

        var load = new FlowNode { Id = "n1", X = 50, Y = 50, Tool = new ImageLoadTool { Name = "图像加载" } };
        nodes.Add(load);

        var thresh = new FlowNode { Id = "n2", X = 250, Y = 50, Tool = new ThresholdTool { Name = "阈值分割", MinGray = 128, MaxGray = 255 } };
        nodes.Add(thresh);

        var morph = new FlowNode { Id = "n3", X = 250, Y = 180, Tool = new MorphologyTool { Name = "形态学(开)", Operation = "opening", ElementSize = 5, Iterations = 2 } };
        nodes.Add(morph);

        var blob = new FlowNode { Id = "n4", X = 450, Y = 50, Tool = new BlobAnalysisTool { Name = "Blob分析", MinArea = 500, SortBy = "area" } };
        nodes.Add(blob);

        var locator = new FlowNode { Id = "n5", X = 450, Y = 180, Tool = new LocatorTool { Name = "定位器", OffsetX = 5, OffsetY = 5 } };
        nodes.Add(locator);

        var findCircle = new FlowNode { Id = "n6", X = 650, Y = 50, Tool = new FindCircleTool { Name = "查找圆", EdgeThreshold = 25, MinRadius = 20, MaxRadius = 100 } };
        nodes.Add(findCircle);

        var p2p = new FlowNode { Id = "n7", X = 650, Y = 180, Tool = new PointToPointDistanceTool { Name = "圆圆距离" } };
        nodes.Add(p2p);

        var save = new FlowNode { Id = "n8", X = 850, Y = 115, Tool = new ImageSaveTool { Name = "保存结果", FilePath = "result.png" } };
        nodes.Add(save);

        // 连线
        connections.Add(new Connection { Id = "c1", SourceNode = load, SourceSlot = "Image", TargetNode = thresh, TargetSlot = "Image" });
        connections.Add(new Connection { Id = "c2", SourceNode = thresh, SourceSlot = "Region", TargetNode = morph, TargetSlot = "Region" });
        connections.Add(new Connection { Id = "c3", SourceNode = load, SourceSlot = "Image", TargetNode = locator, TargetSlot = "Image" });
        connections.Add(new Connection { Id = "c4", SourceNode = morph, SourceSlot = "Region", TargetNode = blob, TargetSlot = "Region" });
        connections.Add(new Connection { Id = "c5", SourceNode = locator, SourceSlot = "FoundRow", TargetNode = findCircle, TargetSlot = "OffsetRow" });
        connections.Add(new Connection { Id = "c6", SourceNode = locator, SourceSlot = "FoundCol", TargetNode = findCircle, TargetSlot = "OffsetCol" });
        connections.Add(new Connection { Id = "c7", SourceNode = findCircle, SourceSlot = "Row", TargetNode = p2p, TargetSlot = "Point1Row" });
        connections.Add(new Connection { Id = "c8", SourceNode = findCircle, SourceSlot = "Col", TargetNode = p2p, TargetSlot = "Point1Col" });
        connections.Add(new Connection { Id = "c9", SourceNode = blob, SourceSlot = "Rows", TargetNode = p2p, TargetSlot = "Point2Row" });
        connections.Add(new Connection { Id = "c10", SourceNode = blob, SourceSlot = "Cols", TargetNode = p2p, TargetSlot = "Point2Col" });
        connections.Add(new Connection { Id = "c11", SourceNode = load, SourceSlot = "Image", TargetNode = save, TargetSlot = "Image" });

        return (nodes, connections);
    }

    // ========== 流程5：ROI绘制 → 阈值分割 → 查找线 → 点线距离 ==========
    public static (List<FlowNode> nodes, List<Connection> connections) CreateROIFlow()
    {
        var nodes = new List<FlowNode>();
        var connections = new List<Connection>();

        var load = new FlowNode { Id = "n1", X = 50, Y = 50, Tool = new ImageLoadTool { Name = "图像加载" } };
        nodes.Add(load);

        var roi = new FlowNode { Id = "n2", X = 250, Y = 50, Tool = new ROIDrawTool { Name = "ROI绘制", Type = "rectangle", Row1 = 100, Col1 = 100, Row2 = 300, Col2 = 400 } };
        nodes.Add(roi);

        var thresh = new FlowNode { Id = "n3", X = 450, Y = 50, Tool = new ThresholdTool { Name = "阈值分割", MinGray = 80, MaxGray = 255 } };
        nodes.Add(thresh);

        var findLine = new FlowNode { Id = "n4", X = 450, Y = 180, Tool = new FindLineTool { Name = "查找线", EdgeThreshold = 15, MinLineLength = 50 } };
        nodes.Add(findLine);

        var p2l = new FlowNode { Id = "n5", X = 650, Y = 115, Tool = new PointToLineDistanceTool { Name = "点线距离" } };
        nodes.Add(p2l);

        connections.Add(new Connection { Id = "c1", SourceNode = load, SourceSlot = "Image", TargetNode = roi, TargetSlot = "Image" });
        connections.Add(new Connection { Id = "c2", SourceNode = roi, SourceSlot = "Region", TargetNode = thresh, TargetSlot = "Region" });
        connections.Add(new Connection { Id = "c3", SourceNode = load, SourceSlot = "Image", TargetNode = findLine, TargetSlot = "Image" });
        connections.Add(new Connection { Id = "c4", SourceNode = roi, SourceSlot = "Row1", TargetNode = p2l, TargetSlot = "PointRow" });
        connections.Add(new Connection { Id = "c5", SourceNode = roi, SourceSlot = "Col1", TargetNode = p2l, TargetSlot = "PointCol" });
        connections.Add(new Connection { Id = "c6", SourceNode = findLine, SourceSlot = "Row1", TargetNode = p2l, TargetSlot = "LineRow1" });
        connections.Add(new Connection { Id = "c7", SourceNode = findLine, SourceSlot = "Col1", TargetNode = p2l, TargetSlot = "LineCol1" });
        connections.Add(new Connection { Id = "c8", SourceNode = findLine, SourceSlot = "Row2", TargetNode = p2l, TargetSlot = "LineRow2" });
        connections.Add(new Connection { Id = "c9", SourceNode = findLine, SourceSlot = "Col2", TargetNode = p2l, TargetSlot = "LineCol2" });

        return (nodes, connections);
    }

    // ========== 流程6：图像处理流水线 ==========
    public static (List<FlowNode> nodes, List<Connection> connections) CreateImageProcessingPipeline()
    {
        var nodes = new List<FlowNode>();
        var connections = new List<Connection>();

        var load = new FlowNode { Id = "n1", X = 50, Y = 50, Tool = new ImageLoadTool { Name = "图像加载" } };
        nodes.Add(load);

        var rotate = new FlowNode { Id = "n2", X = 250, Y = 50, Tool = new ImageRotateTool { Name = "旋转90°", Angle = 90 } };
        nodes.Add(rotate);

        var thresh = new FlowNode { Id = "n3", X = 450, Y = 50, Tool = new ThresholdTool { Name = "阈值分割", MinGray = 150, MaxGray = 255 } };
        nodes.Add(thresh);

        var morph = new FlowNode { Id = "n4", X = 450, Y = 180, Tool = new MorphologyTool { Name = "形态学(闭)", Operation = "closing", ElementSize = 5 } };
        nodes.Add(morph);

        var save = new FlowNode { Id = "n5", X = 650, Y = 115, Tool = new ImageSaveTool { Name = "保存处理结果" } };
        nodes.Add(save);

        connections.Add(new Connection { Id = "c1", SourceNode = load, SourceSlot = "Image", TargetNode = rotate, TargetSlot = "Image" });
        connections.Add(new Connection { Id = "c2", SourceNode = rotate, SourceSlot = "Image", TargetNode = thresh, TargetSlot = "Image" });
        connections.Add(new Connection { Id = "c3", SourceNode = thresh, SourceSlot = "Region", TargetNode = morph, TargetSlot = "Region" });
        connections.Add(new Connection { Id = "c4", SourceNode = rotate, SourceSlot = "Image", TargetNode = save, TargetSlot = "Image" });

        return (nodes, connections);
    }
}

/// <summary>
/// 流程执行验证器
/// </summary>
public class FlowValidator
{
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();

    public bool Validate(List<FlowNode> nodes, List<Connection> connections)
    {
        Errors.Clear();
        Warnings.Clear();

        // 1. 检查节点数量
        if (nodes.Count == 0)
        {
            Errors.Add("流程为空");
            return false;
        }

        // 2. 检查节点ID唯一性
        var ids = new HashSet<string>();
        foreach (var n in nodes)
        {
            if (ids.Contains(n.Id)) Errors.Add($"节点ID重复: {n.Id}");
            ids.Add(n.Id);
        }

        // 3. 检查连接端点有效性
        foreach (var c in connections)
        {
            if (!nodes.Contains(c.SourceNode)) Errors.Add($"连接源节点不存在: {c.SourceNode?.Id}");
            if (!nodes.Contains(c.TargetNode)) Errors.Add($"连接目标节点不存在: {c.TargetNode?.Id}");

            if (c.SourceNode != null)
            {
                var srcSlot = c.SourceNode.Tool.OutputSlots.FirstOrDefault(s => s.Name == c.SourceSlot);
                if (srcSlot == null) Errors.Add($"源节点[{c.SourceNode.Tool.Name}]没有输出槽: {c.SourceSlot}");
            }

            if (c.TargetNode != null)
            {
                var tgtSlot = c.TargetNode.Tool.InputSlots.FirstOrDefault(s => s.Name == c.TargetSlot);
                if (tgtSlot == null) Errors.Add($"目标节点[{c.TargetNode.Tool.Name}]没有输入槽: {c.TargetSlot}");
            }
        }

        // 4. 检查拓扑排序（循环依赖）
        if (HasCycle(nodes, connections))
        {
            Errors.Add("流程存在循环依赖");
            return false;
        }

        // 5. 检查数据类型兼容性（软检查）
        foreach (var c in connections)
        {
            if (c.SourceNode == null || c.TargetNode == null) continue;

            var srcSlot = c.SourceNode.Tool.OutputSlots.FirstOrDefault(s => s.Name == c.SourceSlot);
            var tgtSlot = c.TargetNode.Tool.InputSlots.FirstOrDefault(s => s.Name == c.TargetSlot);

            if (srcSlot != null && tgtSlot != null)
            {
                var srcType = srcSlot.ValueType.Name;
                var tgtType = tgtSlot.ValueType.Name;

                // 允许 HImage→HImage, HRegion→HRegion, HTuple→double/int 等隐式转换
                // 这里做软检查，不阻止执行
                if (srcType != tgtType && srcType != "Object" && tgtType != "Object")
                {
                    Warnings.Add($"类型不匹配（可能需要转换）: {c.SourceNode.Tool.Name}.{c.SourceSlot}({srcType}) → {c.TargetNode.Tool.Name}.{c.TargetSlot}({tgtType})");
                }
            }
        }

        return Errors.Count == 0;
    }

    private bool HasCycle(List<FlowNode> nodes, List<Connection> connections)
    {
        var visited = new HashSet<string>();
        var inStack = new HashSet<string>();

        bool DFS(FlowNode node)
        {
            if (inStack.Contains(node.Id)) return true;
            if (visited.Contains(node.Id)) return false;

            visited.Add(node.Id);
            inStack.Add(node.Id);

            foreach (var conn in connections.Where(c => c.SourceNode == node))
            {
                if (DFS(conn.TargetNode)) return true;
            }

            inStack.Remove(node.Id);
            return false;
        }

        foreach (var n in nodes)
        {
            visited.Clear();
            inStack.Clear();
            if (DFS(n)) return true;
        }

        return false;
    }

    public string GetReport()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"流程验证报告:");
        sb.AppendLine($"  错误: {Errors.Count}");
        sb.AppendLine($"  警告: {Warnings.Count}");

        if (Errors.Count > 0)
        {
            sb.AppendLine("\n错误:");
            foreach (var e in Errors) sb.AppendLine($"  ❌ {e}");
        }

        if (Warnings.Count > 0)
        {
            sb.AppendLine("\n警告:");
            foreach (var w in Warnings) sb.AppendLine($"  ⚠ {w}");
        }

        return sb.ToString();
    }
}