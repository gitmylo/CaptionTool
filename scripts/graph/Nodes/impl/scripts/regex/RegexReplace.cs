using System.Threading.Tasks;
using Godot;
using Godot.Collections;

namespace CaptionTool.scripts.graph.Nodes.impl.scripts;

// Inputs: 3 strings, boolean for match all
// Outputs: replaced[]
[GlobalClass]
public partial class RegexReplace : ExecutionCore
{
    public override async Task<Array<Array>> Execute(Array<Array> inputs, NodeExecutionContext context, Array values)
    {
        var regexes = inputs[0];
        var targetsAndReplacements = inputs[1].FromUGdArray<string>().GrowZip(inputs[2].FromUGdArray<string>());
        var matchAll = values[0].AsBool();
        var results = new Array();
        foreach (var regex in regexes)
        {
            RegEx compiled = RegEx.CreateFromString(regex.AsString());
            GD.Print(targetsAndReplacements);
            foreach (var (target, replacement) in targetsAndReplacements)
            {
                results.Add(compiled.Sub(target, replacement, matchAll));
            }
        }

        return Results(results);
    }
}