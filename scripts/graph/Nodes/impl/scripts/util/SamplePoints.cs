using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

namespace CaptionTool.scripts.graph.Nodes.impl.scripts.util;

[GlobalClass]
public partial class SamplePoints : ExecutionCore
{
    public override async Task<Array<Array>> Execute(Array<Array> inputs, NodeExecutionContext context, Array values)
    {
        var minMaxes = inputs[0].GrowZip<double, double>(inputs[1]);
        var minMaxVals = minMaxes.ToUGdArray().GrowZip<(double, double), double>(inputs[2]);
        var method = values[0].AsString();
        var outputSamples = Inner();
        foreach (var (minmax, count) in minMaxVals)
        {
            int countI = (int)count;
            var (min, max) = minmax;
            switch (method)
            {
                case "Uniform":
                    outputSamples.AddRange(SampleUniform(min, max, countI, false));
                    break;
                case "Uniform space between":
                    outputSamples.AddRange(SampleUniform(min, max, countI, true));
                    break;
                case "Random":
                    var random = new Random();
                    for (var i = 0; i < countI; i++)
                    {
                        outputSamples.Add(random.NextDouble()*(max-min) + min);
                    }
                    break;
            }
        }

        return Results(outputSamples);
    }

    public double[] SampleUniform(double min, double max, int count, bool spaceBetween)
    {
        var effectiveCount = spaceBetween ? count + 2 : count;
        var output = new double[effectiveCount];
        var step = count / (max-min);
        for (int i = 0; i < effectiveCount; i++)
        {
            output[i] = min + step * i + (spaceBetween ? step : 0);
        }
        return output;
    }
}