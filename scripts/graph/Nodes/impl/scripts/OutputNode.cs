using System.Threading.Tasks;
using CaptionTool.scripts.util;
using Godot.Collections;

namespace CaptionTool.scripts.graph.Nodes.impl.scripts;

public partial class OutputNode : CustomNode
{
    public override Task<Array<Array>> Execute(Array<Array> inputs, NodeExecutionContext context)
    {
        context.captionsOut = inputs[0].FromUGdArray<SaveableCaption>();
        return base.Execute(inputs, context);
    }
}