using System.Threading.Tasks;
using Godot;
using Godot.Collections;

namespace CaptionTool.scripts.graph.Nodes.impl.scripts.list;

[GlobalClass]
public partial class ListIndex : ExecutionCore
{
    public override async Task<Array<Array>> Execute(Array<Array> inputs, NodeExecutionContext context, Array values)
    {
        var list = inputs[0];
        var index = (int)inputs[1][0].AsDouble();
        return Results(Inner(list[index]));
    }
}