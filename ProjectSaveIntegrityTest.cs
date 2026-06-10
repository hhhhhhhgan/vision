using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using VisionFlow;
using VisionFlow.Models;
using VisionFlow.Tools;

namespace VisionFlow {

/// <summary>
/// 工程保存完整性验证
/// </summary>
public class ProjectSaveIntegrityTest
{
    public void RunAll()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("工程保存完整性测试");
        Console.WriteLine("========================================\n");

        TestProjectModel();
        TestSaveLoadCycle();
        TestToolParametersPreservation();
        TestConnectionIntegrity();
        TestParameterBindingIntegrity();
        TestNodePositionPreservation();

        Console.WriteLine("\n========================================");
        Console.WriteLine("测试完成");
        Console.WriteLine("========================================");
    }

    private void TestProjectModel()
    {
        Console.WriteLine("--- 测试1: Project模型结构 ---");
        try
        {
            var project = new Project
            {
                Name = "TestProject",
                Version = "1.0",
                Description = "测试工程",
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now
            };

            // 添加节点
            var node1 = new FlowNode
            {
                Id = "node_1",
                X = 100,
                Y = 200,
                Tool = new ImageLoadTool { Name = "图像加载", FilePath = "test.jpg" }
            };

            var node2 = new FlowNode
            {
                Id = "node_2",
                X = 350,
                Y = 200,
                Tool = new ThresholdTool { Name = "阈值分割", MinGray = 100, MaxGray = 255 }
            };

            project.Nodes.Add(new NodeData { Id = node1.Id, ToolType = "图像加载", X = node1.X, Y = node1.Y, Parameters = new Dictionary<string, object> { ["FilePath"] = "test.jpg" } });
            project.Nodes.Add(new NodeData { Id = node2.Id, ToolType = "阈值分割", X = node2.X, Y = node2.Y, Parameters = new Dictionary<string, object> { ["MinGray"] = 100.0, ["MaxGray"] = 255.0 } });

            // 添加连线
            project.Connections.Add(new ConnectionData { Id = "conn_1", SourceNodeId = "node_1", SourceSlot = "Image", TargetNodeId = "node_2", TargetSlot = "Image" });

            // 添加参数
            project.Parameters.Inputs.Add(new FlowParameter { Name = "InputImage", DataType = "image", Description = "输入图像" });
            project.Parameters.Outputs.Add(new FlowParameter { Name = "ResultArea", DataType = "number", Description = "结果面积" });

            // 序列化
            var json = JsonSerializer.Serialize(project, new JsonSerializerOptions { WriteIndented = true });

            Console.WriteLine($"✅ Project模型创建成功");
            Console.WriteLine($"  节点数: {project.Nodes.Count}");
            Console.WriteLine($"  连线数: {project.Connections.Count}");
            Console.WriteLine($"  输入参数: {project.Parameters.Inputs.Count}");
            Console.WriteLine($"  输出参数: {project.Parameters.Outputs.Count}");
            Console.WriteLine($"  JSON长度: {json.Length} 字符");

            // 反序列化
            var loaded = JsonSerializer.Deserialize<Project>(json);
            if (loaded != null && loaded.Nodes.Count == 2 && loaded.Connections.Count == 1)
            {
                Console.WriteLine($"✅ 序列化/反序列化循环成功");
            }
            else
            {
                Console.WriteLine($"❌ 反序列化数据丢失");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 异常: {ex.Message}");
        }
    }

    private void TestSaveLoadCycle()
    {
        Console.WriteLine("\n--- 测试2: 保存加载循环 ---");
        try
        {
            var nodes = FlowFactory.CreateCompleteInspectionFlow().nodes;
            var connections = FlowFactory.CreateCompleteInspectionFlow().connections;

            Console.WriteLine($"  创建流程: {nodes.Count}节点, {connections.Count}连线");

            // 模拟保存
            var project = new Project { Name = "InspectionFlow", Version = "1.0" };

            foreach (var node in nodes)
            {
                var tool = node.Tool;
                var toolType = ToolRegistry.TypeMap.FirstOrDefault(kv => kv.Value == tool.GetType()).Key ?? tool.GetType().Name;

                var nodeData = new NodeData
                {
                    Id = node.Id,
                    ToolType = toolType,
                    X = node.X,
                    Y = node.Y,
                    Parameters = new Dictionary<string, object>()
                };

                // 保存工具参数
                var toolProps = tool.GetType().GetProperties();
                foreach (var prop in toolProps)
                {
                    if (prop.Name == "InputSlots" || prop.Name == "OutputSlots" || prop.Name == "Name" || prop.Name == "Description") continue;
                    try
                    {
                        var val = prop.GetValue(tool);
                        if (val != null)
                            nodeData.Parameters[prop.Name] = val;
                    }
                    catch { }
                }

                project.Nodes.Add(nodeData);
            }

            foreach (var conn in connections)
            {
                project.Connections.Add(new ConnectionData
                {
                    Id = conn.Id,
                    SourceNodeId = conn.SourceNode?.Id ?? "",
                    SourceSlot = conn.SourceSlot,
                    TargetNodeId = conn.TargetNode?.Id ?? "",
                    TargetSlot = conn.TargetSlot
                });
            }

            // 保存到文件
            var tempPath = Path.Combine(Path.GetTempPath(), "visionflow_test.vflow");
            var json = JsonSerializer.Serialize(project, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(tempPath, json);

            Console.WriteLine($"  保存到: {tempPath}");
            Console.WriteLine($"  JSON大小: {new FileInfo(tempPath).Length} bytes");

            // 加载
            var loadedJson = File.ReadAllText(tempPath);
            var loadedProject = JsonSerializer.Deserialize<Project>(loadedJson);

            if (loadedProject != null)
            {
                Console.WriteLine($"✅ 加载成功: {loadedProject.Nodes.Count}节点, {loadedProject.Connections.Count}连线");

                // 验证节点数据
                var nodeCount = loadedProject.Nodes.Count;
                var connCount = loadedProject.Connections.Count;

                if (nodeCount == nodes.Count && connCount == connections.Count)
                    Console.WriteLine($"✅ 节点/连线数量一致");
                else
                    Console.WriteLine($"❌ 节点/连线数量不一致: 期望{nodes.Count}/{connections.Count} 实际{nodeCount}/{connCount}");

                // 验证参数
                foreach (var nodeData in loadedProject.Nodes)
                {
                    if (nodeData.Parameters.Count == 0)
                        Console.WriteLine($"⚠ 节点[{nodeData.ToolType}]参数为空");
                }
            }

            // 清理
            File.Delete(tempPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 异常: {ex.Message}");
        }
    }

    private void TestToolParametersPreservation()
    {
        Console.WriteLine("\n--- 测试3: 工具参数保存完整性 ---");

        var toolsToTest = new[]
        {
            typeof(ThresholdTool),
            typeof(MorphologyTool),
            typeof(FindLineTool),
            typeof(FindCircleTool),
            typeof(CaliperMeasureTool),
            typeof(LocatorTool),
            typeof(ImageRotateTool)
        };

        foreach (var toolType in toolsToTest)
        {
            try
            {
                var tool = Activator.CreateInstance(toolType) as ToolBase;
                if (tool == null) continue;

                var params_ = new Dictionary<string, object>();
                var toolProps = toolType.GetProperties();

                foreach (var prop in toolProps)
                {
                    if (prop.Name == "InputSlots" || prop.Name == "OutputSlots" || prop.Name == "Name" || prop.Name == "Description") continue;
                    try
                    {
                        var val = prop.GetValue(tool);
                        if (val != null)
                            params_[prop.Name] = val;
                    }
                    catch { }
                }

                // 序列化
                var json = JsonSerializer.Serialize(params_);
                var loaded = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                // 比较
                var matchCount = 0;
                foreach (var kvp in params_)
                {
                    if (loaded?.ContainsKey(kvp.Key) == true)
                        matchCount++;
                }

                if (matchCount == params_.Count)
                    Console.WriteLine($"  ✅ {tool.Name}: {params_.Count}参数全部保存");
                else
                    Console.WriteLine($"  ⚠ {tool.Name}: 期望{params_.Count}参数, 实际{matchCount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ {toolType.Name}: {ex.Message}");
            }
        }
    }

    private void TestConnectionIntegrity()
    {
        Console.WriteLine("\n--- 测试4: 连线完整性 ---");

        var (_, connections) = FlowFactory.CreateCompleteInspectionFlow();

        foreach (var conn in connections)
        {
            var issues = new List<string>();

            if (string.IsNullOrEmpty(conn.Id)) issues.Add("ID为空");
            if (string.IsNullOrEmpty(conn.SourceSlot)) issues.Add("SourceSlot为空");
            if (string.IsNullOrEmpty(conn.TargetSlot)) issues.Add("TargetSlot为空");
            if (conn.SourceNode == null) issues.Add("SourceNode为null");
            if (conn.TargetNode == null) issues.Add("TargetNode为null");

            if (issues.Count == 0)
                Console.WriteLine($"  ✅ [{conn.Id}] {conn.SourceNode?.Tool.Name}.{conn.SourceSlot} → {conn.TargetNode?.Tool.Name}.{conn.TargetSlot}");
            else
                Console.WriteLine($"  ❌ [{conn.Id}] 问题: {string.Join(", ", issues)}");
        }
    }

    private void TestParameterBindingIntegrity()
    {
        Console.WriteLine("\n--- 测试5: 流程参数绑定完整性 ---");

        var project = new Project { Name = "TestWithBindings" };

        project.Parameters.Inputs.Add(new FlowParameter { Name = "Threshold", DataType = "number", Description = "阈值", DefaultValue = "128" });
        project.Parameters.Inputs.Add(new FlowParameter { Name = "ImagePath", DataType = "string", Description = "图像路径" });
        project.Parameters.Outputs.Add(new FlowParameter { Name = "DetectedArea", DataType = "number", Description = "检测面积" });

        project.Parameters.Inputs[0].BindingNodeId = "node_2";
        project.Parameters.Inputs[0].BindingSlot = "MinGray";

        project.Parameters.Outputs[0].BindingNodeId = "node_4";
        project.Parameters.Outputs[0].BindingSlot = "Area";

        var json = JsonSerializer.Serialize(project, new JsonSerializerOptions { WriteIndented = true });
        var loaded = JsonSerializer.Deserialize<Project>(json);

        if (loaded != null)
        {
            var inputBinding = loaded.Parameters.Inputs[0];
            var outputBinding = loaded.Parameters.Outputs[0];

            if (inputBinding.BindingNodeId == "node_2" && inputBinding.BindingSlot == "MinGray")
                Console.WriteLine($"  ✅ 输入参数绑定正确: {inputBinding.Name} → {inputBinding.BindingNodeId}.{inputBinding.BindingSlot}");
            else
                Console.WriteLine($"  ❌ 输入参数绑定丢失");

            if (outputBinding.BindingNodeId == "node_4" && outputBinding.BindingSlot == "Area")
                Console.WriteLine($"  ✅ 输出参数绑定正确: {outputBinding.Name} → {outputBinding.BindingNodeId}.{outputBinding.BindingSlot}");
            else
                Console.WriteLine($"  ❌ 输出参数绑定丢失");
        }
    }

    private void TestNodePositionPreservation()
    {
        Console.WriteLine("\n--- 测试6: 节点位置保存 ---");

        var nodes = FlowFactory.CreateCompleteInspectionFlow().nodes;

        var project = new Project { Name = "PositionTest" };
        foreach (var node in nodes)
        {
            project.Nodes.Add(new NodeData
            {
                Id = node.Id,
                ToolType = node.Tool.Name,
                X = node.X,
                Y = node.Y,
                Parameters = new Dictionary<string, object>()
            });
        }

        var json = JsonSerializer.Serialize(project);
        var loaded = JsonSerializer.Deserialize<Project>(json);

        if (loaded != null)
        {
            var allMatch = true;
            foreach (var orig in nodes)
            {
                var loadedNode = loaded.Nodes.FirstOrDefault(n => n.Id == orig.Id);
                if (loadedNode == null)
                {
                    Console.WriteLine($"  ❌ 节点 {orig.Id} 丢失");
                    allMatch = false;
                    continue;
                }

                if (Math.Abs(loadedNode.X - orig.X) < 0.01 && Math.Abs(loadedNode.Y - orig.Y) < 0.01)
                    Console.WriteLine($"  ✅ {orig.Id}: ({orig.X:F0}, {orig.Y:F0})");
                else
                {
                    Console.WriteLine($"  ❌ {orig.Id}: 期望({orig.X:F0},{orig.Y:F0}) 实际({loadedNode.X:F0},{loadedNode.Y:F0})");
                    allMatch = false;
                }
            }

            if (allMatch) Console.WriteLine($"  ✅ 所有节点位置正确保存");
        }
    }
}

// 完整 Project 模型（确保所有字段可序列化）
public class Project
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "未命名工程";
    public string Version { get; set; } = "1.0";
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime ModifiedAt { get; set; } = DateTime.Now;
    public List<NodeData> Nodes { get; set; } = new();
    public List<ConnectionData> Connections { get; set; } = new();
    public ProjectParameters Parameters { get; set; } = new();
}

public class NodeData
{
    public string Id { get; set; } = "";
    public string ToolType { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class ConnectionData
{
    public string Id { get; set; } = "";
    public string SourceNodeId { get; set; } = "";
    public string SourceSlot { get; set; } = "";
    public string TargetNodeId { get; set; } = "";
    public string TargetSlot { get; set; } = "";
}

public class ProjectParameters
{
    public List<FlowParameter> Inputs { get; set; } = new();
    public List<FlowParameter> Outputs { get; set; } = new();
}

public class FlowParameter
{
    public string Name { get; set; } = "";
    public string DataType { get; set; } = "string";
    public string? Description { get; set; }
    public string? DefaultValue { get; set; }
    public string? BindingNodeId { get; set; }
    public string? BindingSlot { get; set; }
}
}