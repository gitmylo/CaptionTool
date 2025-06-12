using Godot;

namespace CaptionTool.scripts.graph.Nodes;

[GlobalClass]
public partial class NodeDef : Resource
{
    [Export] public string name;
    [Export] public string description;
    [Export] public string identifier;
    [Export] public PackedScene node;
}