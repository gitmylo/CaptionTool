using System.Threading.Tasks;
using Godot;
using Godot.Collections;

namespace CaptionTool.scripts.graph.Nodes.impl.scripts.math;

[GlobalClass]
public partial class Compare : ExecutionCore
{
    public override async Task<Array<Array>> Execute(Array<Array> inputs, NodeExecutionContext context, Array values)
    {
        var tasks = inputs[0].FromUGdArray<double>().GrowZip(inputs[1].FromUGdArray<double>());
        var results = Inner();
        var op = values[0].ToString();
        foreach (var (l, r) in tasks)
        {
            switch (op)
            {
                case "==":
                    results.Add(l == r);
                    break;
                case "!=":
                    results.Add(l != r);
                    break;
                case ">":
                    results.Add(l > r);
                    break;
                case ">=":
                    results.Add(l >= r);
                    break;
                case "<":
                    results.Add(l < r);
                    break;
                case "<=":
                    results.Add(l <= r);
                    break;
            }
        }
        return Results(results);
    }
}