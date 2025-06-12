using System.Threading;
using System.Threading.Tasks;
using CaptionTool.scripts.util;
using Godot;
using Godot.Collections;

namespace CaptionTool.scripts.graph.Nodes.impl.scripts.captions;

[GlobalClass]
public partial class JoinCaption : ExecutionCore
{
    public override async Task<Array<Array>> Execute(Array<Array> inputs, NodeExecutionContext context, Array values)
    {
        var startEnd = inputs[1].FromUGdArray<double>().GrowZip(inputs[2].FromUGdArray<double>());
        var startEndBypass = startEnd.GrowZip(inputs[3].FromUGdArray<bool>());
        var startEndBypassCaption = startEndBypass.GrowZip(inputs[0].FromUGdArray<string>());
        var results = Inner();
        foreach (var (((start, end), bypassDuration), caption) in startEndBypassCaption)
        {
            results.Add(new SaveableCaption
            {
                start = start,
                end = end,
                bypassduration = bypassDuration,
                caption = caption
            });
        }

        return Results(results);
    }
}