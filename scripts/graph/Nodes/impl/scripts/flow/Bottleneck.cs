using System.Threading.Tasks;
using Godot;
using Godot.Collections;

namespace CaptionTool.scripts.graph.Nodes.impl.scripts.flow;

// Used to set parameter count to 0.
// Inputs: Any[], Any[1] (replacement)
// Outputs: Any[1]
[GlobalClass]
public partial class Bottleneck : ExecutionCore
{
    public async override Task<Array<Array>> Execute(Array<Array> inputs, NodeExecutionContext context, Array values)
    {
        if (inputs[0].Count == 0) return Results(Inner(inputs[1][0])); // Return replacement
        return Results(Inner(inputs[0][0]));
    }
}