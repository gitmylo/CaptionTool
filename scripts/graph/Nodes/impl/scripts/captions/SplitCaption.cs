using System.Threading.Tasks;
using CaptionTool.scripts.util;
using Godot;
using Godot.Collections;

namespace CaptionTool.scripts.graph.Nodes.impl.scripts.captions;

[GlobalClass]
public partial class SplitCaption : ExecutionCore
{
    public override async Task<Array<Array>> Execute(Array<Array> inputs, NodeExecutionContext context, Array values)
    {
        var captions = inputs[0];
        var texts = Inner();
        var starts = Inner();
        var ends = Inner();
        var bypassDurations = Inner();
        foreach (var captionV in captions)
        {
            var caption = captionV.As<SaveableCaption>();
            texts.Add(caption.caption);
            starts.Add(caption.start);
            ends.Add(caption.end);
            bypassDurations.Add(caption.bypassduration);
        }
        return Results(texts, starts, ends, bypassDurations);
    }
}