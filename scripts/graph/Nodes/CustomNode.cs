using System.Threading.Tasks;
using Godot;
using Godot.Collections;

namespace CaptionTool.scripts.graph.Nodes;

[GlobalClass]
public partial class CustomNode : GraphNode
{
    [Export] public bool required; // Required nodes are not deletable

    public virtual async Task<Array<Array>> Execute(Array<Array> inputs, NodeExecutionContext context)
    {
        return new Array<Array>();
    }
}