using System.Threading.Tasks;
using CaptionTool.scripts.util;
using Godot;
using Godot.Collections;

namespace CaptionTool.scripts.graph.Nodes.impl.ffmpeg;

[GlobalClass]
public partial class FfmpegFrameExtract : ExecutionCore
{
    public override async Task<Array<Array>> Execute(Array<Array> inputs, NodeExecutionContext context, Array values)
    {
        var inner = Inner();
        foreach (var input in inputs[0].GrowZip<string, double>(inputs[1]))
        {
            inner.Add(FfmpegUtil.GetVideoFrame(input.Item1, input.Item2));
        }
        return Results(inner);
    }
}