using System.Threading.Tasks;
using Godot;
using Godot.Collections;

namespace CaptionTool.scripts.graph.Nodes;

[GlobalClass]
public partial class ExecutionCore: Resource
{
    public virtual async Task<Array<Array>> Execute(Array<Array> inputs, NodeExecutionContext context, Array values)
    {
        return new Array<Array>();
    }

    protected Array<Array> Results(params Array[] results)
    {
        return new Array<Array>(results);
    }

    protected Array Inner(params Variant[] values)
    {
        return new Array(values);
    }
}