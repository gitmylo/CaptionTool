using System.Collections.Generic;
using System.Threading.Tasks;
using CaptionTool.scripts.util;
using Godot;
using Godot.Collections;

namespace CaptionTool.scripts.graph.Nodes.impl.scripts;

[GlobalClass]
public partial class OutputNode : ExecutionCore
{
    public override Task<Array<Array>> Execute(Array<Array> inputs, NodeExecutionContext context, Array values)
    {
        context.captionsOut = inputs[0].FromUGdArray<SaveableCaption>();
        return base.Execute(inputs, context, values);
    }
}