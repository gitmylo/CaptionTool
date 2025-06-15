using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

namespace CaptionTool.scripts.graph.Nodes.impl.scripts.list;

[GlobalClass]
public partial class ListAppend : ExecutionCore
{
    public override async Task<Array<Array>> Execute(Array<Array> inputs, NodeExecutionContext context, Array values)
    {
        var list = inputs[0].FromUGdArray<Variant>().ToList();
        list.AddRange(inputs[1]);
        return Results(list.ToUGdArray());
    }
}