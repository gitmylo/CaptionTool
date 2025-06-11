using System.Collections.Generic;
using System.Threading.Tasks;
using Godot.Collections;

namespace CaptionTool.scripts.graph.Nodes.impl.scripts;

public partial class InputNode : CustomNode
{
    public async override Task<Array<Array>> Execute(Array<Array> inputs, NodeExecutionContext context)
    {
        return new List<Array>{new List<string>{context.fileName}.ToUGdArray(), context.captionsIn.ToUGdArray()}.ToGdArray();
    }
}