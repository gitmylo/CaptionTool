using System.Threading.Tasks;
using System.Web;
using Godot;
using Godot.Collections;

namespace CaptionTool.scripts.graph.Nodes.impl.scripts.stringfunc;

// Functions which transform a string
[GlobalClass]
public partial class StringFunc1 : ExecutionCore
{
    public override async Task<Array<Array>> Execute(Array<Array> inputs, NodeExecutionContext context, Array values)
    {
        var strings = inputs[0].FromUGdArray<string>();
        var outputs = Inner();
        foreach (var input in strings)
        {
            switch (values[0].AsString())
            {
                case "basedir":
                    outputs.Add(input.GetBaseDir());
                    break;
                case "basename":
                    outputs.Add(input.GetBaseName());
                    break;
                case "fileext":
                    outputs.Add(input.GetExtension());
                    break;
                case "urlencode":
                    outputs.Add(HttpUtility.UrlEncode(input));
                    break;
            }
        }

        return Results(outputs);
    }
}