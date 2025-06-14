using System;
using System.IO;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;
using FileAccess = Godot.FileAccess;

namespace CaptionTool.scripts.graph.Nodes.impl.scripts.file;

[GlobalClass]
public partial class ReadFile : ExecutionCore
{
    public override async Task<Array<Array>> Execute(Array<Array> inputs, NodeExecutionContext context, Array values)
    {
        var outputs = Inner();
        foreach (var input in inputs[0].FromUGdArray<string>())
        {
            using (var f = FileAccess.Open(input, FileAccess.ModeFlags.Read))
            {
                outputs.Add(f.GetAsText());
            }
            // outputs.Add(File.ReadAllText(input));
        }
        
        return Results(outputs);
    }
}