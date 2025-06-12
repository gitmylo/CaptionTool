using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

namespace CaptionTool.scripts.graph.Nodes.impl.scripts;

[GlobalClass]
public partial class InputNode : ExecutionCore
{
    public async override Task<Array<Array>> Execute(Array<Array> inputs, NodeExecutionContext context, Array values)
    {
        return Results(Inner(context.fileName), context.captionsIn.ToUGdArray());
    }
}