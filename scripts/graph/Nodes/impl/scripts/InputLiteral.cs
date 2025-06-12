using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

namespace CaptionTool.scripts.graph.Nodes.impl.scripts;

[GlobalClass]
public partial class InputLiteral : ExecutionCore
{
    override public async Task<Array<Array>> Execute(Array<Array> inputs, NodeExecutionContext context, Array array)
    {
        return Results(Inner(array[0])).ToGdArray();
    }
}