using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

namespace CaptionTool.scripts.graph.Nodes.impl.scripts;

// Execution runs backwards, from the final node to the start, whenever the required inputs are valid
public class ExecutionTree
{
    public NodeExecutionContext context;
    public ExecutionNode output;

    public async Task Execute()
    {
        output.context = context;
        await output.ExecuteRecursive();
    }
}

public class ExecutionNode
{
    public NodeExecutionContext context;
    public List<(ExecutionNode, int, int)> inputConnections = new (); // Connection (sourcenode, sourceidx, targetidx). This is targetnode
    public ExecutionCore node;

    public Array<Array> outputs;
    public Array uiValues;
    public bool hasExecuted;

    public async Task ExecuteRecursive()
    {
        if (hasExecuted) return;
        foreach (var (dependentNode, _, _) in inputConnections)
        {
            dependentNode.context = context;
            await dependentNode.ExecuteRecursive(); // Execute all dependent nodes so the outputs are available
        }

        int highestOutput = inputConnections.Count == 0 ? 0 : (inputConnections.Max(x => x.Item3)+1);
        var inputs = new Array<Array>();
        for (int i = 0; i < highestOutput; i++) inputs.Add(new Array());
        
        foreach (var connection in inputConnections)
        {
            var outputs = connection.Item1.outputs[connection.Item2];
            inputs[connection.Item3] = outputs;
        }

        outputs = await node.Execute(inputs, context, uiValues);
        hasExecuted = true;
    }
}
