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
        
        ConnectionRequest += LinkConnectionRequest;
        DisconnectionRequest += LinkDisconnectionRequest;
        DeleteNodesRequest += LinkDeleteNodesRequest;
        PopupRequest += p =>
        {
            lastPopup = p;
            popup.Popup();
            popup.SetPosition((Vector2I)(GetGlobalMousePosition() + (Vector2.Left * (popup.Size.X/2)) + (Vector2.Up * 15)));
            popupSearchBox.Text = "";
            popupSearchBox.GrabFocus();
        };
        popupSearchBox.TextChanged += newText =>
        {
            var allNodes = availableNodes.GetChildren().Select(x => x as NodeButton).OfType<NodeButton>().ToList();
            foreach (var node in allNodes)
            {
                GD.Print(node.name);
                node.Visible = node.name.ToLower().Contains(newText.ToLower()) || node.description.ToLower().Contains(newText.ToLower());
            }
        };
    }

    // Signal receivers
    private void LinkConnectionRequest(StringName fromNode, long fromPort,  StringName toNode, long toPort)
    {
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