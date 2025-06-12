using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CaptionTool.scripts.graph.Nodes;
using CaptionTool.scripts.graph.Nodes.impl.scripts;
using CaptionTool.scripts.util;
using Godot;
using Godot.Collections;

namespace CaptionTool.scripts.graph;

public partial class GraphManager: GraphEdit
{
    [Export] public NodeConsts consts;
    
    [Export] private GraphNode InputNode;
    [Export] private GraphNode OutputNode;
    
    [ExportCategory("Popup")]
    [Export] private Popup popup;
    [Export] private LineEdit popupSearchBox;
    [Export] private VBoxContainer availableNodes; // Contains NodeButton entries
    [Export] private PackedScene nodeButtonScene;

    [Export] private Button testFileButton;
    [Export] private Button processAllButton;

    private Vector2 lastPopup;

    public override void _Ready()
    {
        InitConnection();
        InitNodeButtons();

        foreach (var type in consts.types)
        {
            AddValidConnectionType(type.id, consts.AnyType);
            AddValidConnectionType(consts.AnyType, type.id);
        }
        
        ConnectionRequest += LinkConnectionRequest;
        DisconnectionRequest += LinkDisconnectionRequest;
        DeleteNodesRequest += LinkDeleteNodesRequest;
        PopupRequest += p =>
        {
            lastPopup = p;
            popup.Popup();
            popup.SetPosition((Vector2I)(GetGlobalMousePosition() + (Vector2.Left * (popup.Size.X/2)) + (Vector2.Up * 15)));
            popupSearchBox.Text = "";
            UpdateSearch("");
            popupSearchBox.GrabFocus();
        };
        popupSearchBox.TextChanged += UpdateSearch;

        testFileButton.Pressed += () =>
        {
            var result = ExecuteGraphForInput();
            result.Wait();
            var output = result.Result;
        };
    }

    private void UpdateSearch(string query)
    {
        var allNodes = availableNodes.GetChildren().Select(x => x as NodeButton).OfType<NodeButton>().ToList();
        foreach (var node in allNodes)
        {
            node.Visible = node.name.ToLower().Contains(query.ToLower()) || node.description.ToLower().Contains(query.ToLower());
        }
    }

    // Signal receivers
    private void LinkConnectionRequest(StringName fromNode, long fromPort,  StringName toNode, long toPort)
    {
        foreach (var connection in Connections.Where(x => x["to_node"].AsStringName() == toNode && x["to_port"].AsInt64() == toPort))
        {
            DisconnectNode(connection["from_node"].AsStringName(), connection["from_port"].AsInt32(), connection["to_node"].AsStringName(),  connection["to_port"].AsInt32());
        }
        ConnectNode(fromNode, (int)fromPort, toNode, (int)toPort);
    }

    private void LinkDisconnectionRequest(StringName fromNode, long fromPort, StringName toNode, long toPort)
    {
        DisconnectNode(fromNode, (int)fromPort, toNode, (int)toPort);
    }
    
    private void LinkDeleteNodesRequest(Array<StringName> nodes)
    {
        foreach (var node in GetNodes().FindAll(x => nodes.Contains(x.Name) && !x.required))
        {
            node.QueueFree();
        }
    }
    
    // Other

    void InitNodeButtons()
    {
        foreach (var node in consts.nodes)
        {
            CreateNodeButton(node.name, node.description, node.node);
        }
    }
    
    void CreateNodeButton(string name, string description, PackedScene node)
    {
        var button = nodeButtonScene.Instantiate<NodeButton>();
        button.name = name;
        button.description = description;
        button.node = node;
        button.graph = this;
        button.InitReady();
        availableNodes.AddChild(button);
    }

    public void AddNode(PackedScene node)
    {
        var nodeInst = node.Instantiate<CustomNode>();
        nodeInst.PositionOffset = lastPopup;
        nodeInst.graph = this;
        AddChild(nodeInst);
        
        popup.Visible = false;
    }

    void InitConnection()
    {
        ConnectNode(InputNode.Name, 1, OutputNode.Name, 0);
    }

    List<CustomNode> GetNodes()
    {
        return GetChildren().Where(x => x is CustomNode).Cast<CustomNode>().ToList();
    }

    public async Task<SaveableCaption[]> ExecuteGraphForInput()
    {
        var graph = BuildGraph("Example.mp4",
            new SaveableCaption[] { new SaveableCaption { bypassduration = true, caption = "This is a caption" } });
        await graph.Execute();
        GD.Print(graph.context.captionsOut[0]);
        return graph.context.captionsOut;
    }

    public ExecutionTree BuildGraph(string fileName, SaveableCaption[] captions)
    {
        var context = new NodeExecutionContext {fileName = fileName, captionsIn = captions};
        var tree = new ExecutionTree {context = context};
        var nodesByName = GetNodes().ToDictionary(x => x.Name);
        System.Collections.Generic.Dictionary<(StringName, int), (StringName, int)> valueSources = new ();
        foreach (var connection in Connections)
        {
            var fromNode = connection["from_node"].AsStringName();
            var fromPort = connection["from_port"].AsInt32();
            var toNode = connection["to_node"].AsStringName();
            var toPort = connection["to_port"].AsInt32();
            
            valueSources[(toNode, toPort)] = (fromNode, fromPort);
        }

        var outputNode = FindConnections(OutputNode.Name, nodesByName, new (), valueSources);
        tree.output = outputNode;
        
        return tree;
    }

    public ExecutionNode FindConnections(StringName originNode, System.Collections.Generic.Dictionary<StringName, CustomNode> nodesByName, System.Collections.Generic.Dictionary<StringName, ExecutionNode> nodeHistory, System.Collections.Generic.Dictionary<(StringName, int), (StringName, int)> valueSources)
    {
        var node = nodesByName[originNode];
        var inputCount = node.GetInputPortCount();
        var thisNode = new ExecutionNode {node = node.core, uiValues = node.GetControlValues()};
        for (int i = 0; i < inputCount; i++)
        {
            var source = valueSources[(node.Name, i)];
            ExecutionNode sourceNode;
            if (nodeHistory.ContainsKey(source.Item1))
            {
                sourceNode = nodeHistory[source.Item1];
            }
            else
            {
                sourceNode = FindConnections(source.Item1, nodesByName, nodeHistory, valueSources); // Recursively trace back the node graph
            }
            thisNode.inputConnections.Add((sourceNode, source.Item2, i));
        }

        return thisNode;
    }
}