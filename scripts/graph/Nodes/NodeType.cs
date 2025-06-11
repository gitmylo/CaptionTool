using Godot;

namespace CaptionTool.scripts.graph.Nodes;

[GlobalClass]
public partial class NodeType : Resource
{
    [Export] public int id;
    [Export] public string name = "";
    [Export] public Color color = Colors.White;
}