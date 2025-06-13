using System.Threading.Tasks;
using CaptionTool.scripts.util;
using Godot;
using Godot.Collections;

namespace CaptionTool.scripts.graph.Nodes.impl.ffmpeg;

// Inputs: 1 string
// Outputs: 5 numbers, width, height, duration, fps, est_framecount
[GlobalClass]
public partial class FfmpegVideoInfo : ExecutionCore
{
    public override async Task<Array<Array>> Execute(Array<Array> inputs, NodeExecutionContext context, Array values)
    {
        var widths = Inner();
        var heights = Inner();
        var durations = Inner();
        var fpss = Inner();
        var estFramecounts = Inner();
        foreach (var video in inputs[0])
        {
            var info = FfmpegUtil.GetVideoInfo(video.AsString());
            widths.Add(info.width);
            heights.Add(info.height);
            durations.Add(info.duration);
            fpss.Add(info.frameRate);
            estFramecounts.Add(info.frameCount);
        }

        return Results(widths, heights, durations, fpss, estFramecounts);
    }
}