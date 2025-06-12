using CaptionTool.scripts.graph;
using Godot;

namespace CaptionTool.scripts.ui;

public partial class SaveWorkflowPopup : ConfirmationDialog
{
    [Export] private LineEdit fileNameBox;
    
    public GraphManager manager;
    public override void _Ready()
    {
        Confirmed += () =>
        {
            manager.SaveGraph(fileNameBox.Text);
        };
    }
}