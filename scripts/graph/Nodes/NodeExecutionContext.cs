using CaptionTool.scripts.util;

namespace CaptionTool.scripts.graph.Nodes;

// Class used for execution context. Used for reading base inputs, writing outputs.
public class NodeExecutionContext
{
    public string fileName;
    public SaveableCaption[] captionsIn;
    public SaveableCaption[] captionsOut;
}