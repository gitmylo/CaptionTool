using CaptionTool.scripts.graph;
using Godot;

namespace CaptionTool.scripts.ui;

public partial class RenameWorkflowPopup : ConfirmationDialog
{
    [Export] private LineEdit fileNameBox;
    [Export] private Label confirmLabel;
    
    public string oldName;
    public GraphManager manager;
    public override void _Ready()
    {
        confirmLabel.Text = $"Please type a filename for this workflow. (was {oldName}";
        Confirmed += () =>
        {
            manager.RenameGraph(oldName, fileNameBox.Text);
        };
    }
}