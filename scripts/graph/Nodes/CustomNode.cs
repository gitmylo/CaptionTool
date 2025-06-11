using Godot;

namespace CaptionTool.scripts.graph.Nodes;

[GlobalClass]
public partial class CustomNode : GraphNode
{
    [Export] public bool required; // Required nodes are not deletable
}