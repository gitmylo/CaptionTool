using System.Threading.Tasks;
using Godot;
using Godot.Collections;

namespace CaptionTool.scripts.graph.Nodes.impl.scripts.stringfunc;

// Functions which use 2 strings
[GlobalClass]
public partial class StringFunc2 : ExecutionCore
{
    public override async Task<Array<Array>> Execute(Array<Array> inputs, NodeExecutionContext context, Array values)
    {
        var strings = inputs[0].GrowZip<string, string>(inputs[1]);
        var outputs = Inner();
        foreach (var input in strings)
        {
            switch (values[0].AsString())
            {
                case "concat":
                    outputs.Add(input.Item1 + input.Item2);
                    break;
                case "trimprefix":
                    outputs.Add(input.Item1.TrimPrefix(input.Item2));
                    break;
                case "trimsuffix":
                    outputs.Add(input.Item1.TrimSuffix(input.Item2));
                    break;
            }
        }

        return Results(outputs);
    }
}