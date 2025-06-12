using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

namespace CaptionTool.scripts.graph.Nodes.impl.scripts;

// Inputs: 2 strings, boolean for match all
// Outputs: full match[], regexresult[]
[GlobalClass]
public partial class RegexMatch : ExecutionCore
{
    public override async Task<Array<Array>> Execute(Array<Array> inputs, NodeExecutionContext context, Array values)
    {
        var regexes = inputs[0];
        var targets = inputs[1];
        var matchAll = values[0].AsBool();
        var results = new Array();
        var fullTexts = new Array();
        foreach (var regex in regexes)
        {
            RegEx compiled = RegEx.CreateFromString(regex.AsString());
            foreach (var target in targets)
            {
                if (matchAll)
                {
                    var match = compiled.SearchAll(target.AsString());
                    results.AddRange(match);
                    fullTexts.AddRange(match.Select(x => x.Subject));
                }
                else
                {
                    var match = compiled.Search(target.AsString());
                    results.Add(match);
                    fullTexts.Add(match.Subject);
                }
            }
        }

        return Results(fullTexts, results);
    }
}