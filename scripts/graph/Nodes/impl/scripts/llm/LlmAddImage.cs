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
        var output = Inner();

        foreach (var message in inputs[0].FromUGdArray<LlmMessage>())
        {
            var clone = message.Clone();
            var newImages = clone.images.ToList();
            newImages.AddRange(inputs[1].FromUGdArray<string>());
            clone.images = newImages.ToArray();
            output.Add(clone);
        }
        
        return Results(output);
    }
}