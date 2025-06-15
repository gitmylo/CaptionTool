using System.Threading.Tasks;
using Godot;
using Godot.Collections;

namespace CaptionTool.scripts.graph.Nodes.impl.scripts.llm;

[GlobalClass]
public partial class LlmCreateMessage : ExecutionCore
{
    public override async Task<Array<Array>> Execute(Array<Array> inputs, NodeExecutionContext context, Array values)
    {
        var output = Inner();
        
        var role = values[0].AsString();
        foreach (var content in inputs[0].FromUGdArray<string>())
        {
            output.Add(new LlmMessage {message = content, role = role});
        }

        return Results(output);
    }
}