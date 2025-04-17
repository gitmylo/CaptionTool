using Godot;

namespace CaptionTool.scripts;

public partial class ColorIndicator : ColorRect
{
    private Color saved = Colors.Green; // Saved
    private Color unsaved = Colors.Yellow; // Unsaved changes
    private Color notCreated = Colors.Red; // Not yet created
    
    public void MarkSaved()
    {
        Color = saved;
        TooltipText = "saved";
    }

    public void MarkUnSaved()
    {
        Color = unsaved;
        TooltipText = "not saved";
    }

    public void MarkNotCreated()
    {
        Color = notCreated;
        TooltipText = "not yet created";
    }

    public bool Saved => Color == saved;
    public bool UnSaved => Color == unsaved;
    public bool NotCreated => Color == notCreated;
}