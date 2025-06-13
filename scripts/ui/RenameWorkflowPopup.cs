using CaptionTool.scripts.graph;
using Godot;

namespace CaptionTool.scripts.ui;

public partial class RenameWorkflowPopup : ConfirmationDialog
{
    [Export] private LineEdit fileNameBox;
    [Export] private Label confirmLabel;
    
    public string oldName;
    public GraphManager manager;

    public void ManualReady()
    {
        confirmLabel.Text = $"Please type a filename for this workflow. (was {oldName})";
        fileNameBox.Text = oldName;
        Confirmed += () =>
        {
            manager.RenameGraph(oldName, fileNameBox.Text);
        };
    }
}