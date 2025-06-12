using System.Collections.Generic;
using System.Linq;
using CaptionTool.scripts.graph.Nodes;
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
}