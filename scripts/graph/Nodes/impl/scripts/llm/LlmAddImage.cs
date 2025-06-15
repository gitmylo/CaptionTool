using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

namespace CaptionTool.scripts.graph.Nodes.impl.scripts.llm;

[GlobalClass]
public partial class LlmAddImage : ExecutionCore
{
    public override async Task<Array<Array>> Execute(Array<Array> inputs, NodeExecutionContext context, Array values)
    {
        var items = inputs[0].GrowZip<LlmMessage, string>(inputs[1]);
        var output = Inner();

        foreach (var item in items)
        {
            var clone = item.Item1.Clone();
            clone.images = clone.images.Append(item.Item2).ToArray();
            output.Add(clone);
        }
        
        return Results(output);
    }
}