using System.Windows;
using VisionFlow.Models;
using VisionFlow.Tools;
using System.Collections.Generic;

namespace VisionFlow {

/// <summary>
/// 创建演示流程（用于压力测试）
/// 流程: 图像加载 → 灰度转换 → 阈值分割 → 形态学(开) → Blob分析
/// </summary>
public static class DemoFlowFactory
{
    public static (List<FlowNode> nodes, List<Connection> connections) CreateSimpleFlow()
    {
        var nodes = new List<FlowNode>();
        var connections = new List<Connection>();

        // 1. 图像加载
        var loadNode = new FlowNode
        {
            Id = "node_load",
            X = 50, Y = 50,
            Tool = new ImageLoadTool
            {
                Name = "图像加载",
                FilePath = "test.jpg" // 测试时会失败，但能验证框架稳定性
            }
        };
        nodes.Add(loadNode);

        // 2. 灰度转换
        var grayNode = new FlowNode
        {
            Id = "node_gray",
            X = 250, Y = 50,
            Tool = new GrayToColorTool()
        };
        nodes.Add(grayNode);

        // 3. 阈值分割
        var threshNode = new FlowNode
        {
            Id = "node_thresh",
            X = 450, Y = 50,
            Tool = new ThresholdTool
            {
                Name = "阈值分割",
                MinGray = 100,
                MaxGray = 255,
                Mode = "global"
            }
        };
        nodes.Add(threshNode);

        // 4. 形态学
        var morphNode = new FlowNode
        {
            Id = "node_morph",
            X = 650, Y = 50,
            Tool = new MorphologyTool
            {
                Name = "形态学",
                Operation = "opening",
                Iterations = 1,
                ElementSize = 3
            }
        };
        nodes.Add(morphNode);

        // 5. Blob分析
        var blobNode = new FlowNode
        {
            Id = "node_blob",
            X = 850, Y = 50,
            Tool = new BlobAnalysisTool
            {
                Name = "Blob分析",
                MinArea = 100,
                MaxArea = 9999999,
                SortBy = "area"
            }
        };
        nodes.Add(blobNode);

        // 连线
        connections.Add(new Connection
        {
            Id = "conn_1",
            SourceNode = loadNode, SourceSlot = "Image",
            TargetNode = grayNode, TargetSlot = "GrayImage"
        });

        connections.Add(new Connection
        {
            Id = "conn_2",
            SourceNode = grayNode, SourceSlot = "ColorImage",
            TargetNode = threshNode, TargetSlot = "Image"
        });

        connections.Add(new Connection
        {
            Id = "conn_3",
            SourceNode = threshNode, SourceSlot = "Region",
            TargetNode = morphNode, TargetSlot = "Region"
        });

        connections.Add(new Connection
        {
            Id = "conn_4",
            SourceNode = morphNode, SourceSlot = "Region",
            TargetNode = blobNode, TargetSlot = "Region"
        });

        return (nodes, connections);
    }

    /// <summary>
    /// 创建最小测试流程（不需要图像文件）
    /// </summary>
    public static (List<FlowNode> nodes, List<Connection> connections) CreateMinimalFlow()
    {
        var nodes = new List<FlowNode>();
        var connections = new List<Connection>();

        // 仅Blob分析（模拟输入）
        var blobNode = new FlowNode
        {
            Id = "node_blob",
            X = 50, Y = 50,
            Tool = new BlobAnalysisTool
            {
                Name = "Blob分析",
                MinArea = 10,
                MaxArea = 9999999
            }
        };
        nodes.Add(blobNode);

        return (nodes, connections);
    }
}
}