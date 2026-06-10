using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace VisionFlow.Models {

/// <summary>
/// 工程文件（可序列化 JSON）
/// </summary>
public class Project
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "未命名工程";
    public string Version { get; set; } = "1.0";
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime ModifiedAt { get; set; } = DateTime.Now;

    /// <summary>节点数据（工具类型名 + 属性 + 位置）</summary>
    public List<NodeData> Nodes { get; set; } = new();

    /// <summary>连线数据</summary>
    public List<ConnectionData> Connections { get; set; } = new();

    /// <summary>流程参数配置</summary>
    public ProjectParameters Parameters { get; set; } = new();
}

public class NodeData
{
    public string Id { get; set; } = string.Empty;
    public string ToolType { get; set; } = string.Empty; // 如 "图像加载"
    public double X { get; set; }
    public double Y { get; set; }

    /// <summary>工具实例的公共属性（键值对），用于恢复工具状态</summary>
    public Dictionary<string, object?> Parameters { get; set; } = new();
}

public class ConnectionData
{
    public string Id { get; set; } = string.Empty;
    public string SourceNodeId { get; set; } = string.Empty;
    public string SourceSlot { get; set; } = string.Empty;
    public string TargetNodeId { get; set; } = string.Empty;
    public string TargetSlot { get; set; } = string.Empty;
}
}