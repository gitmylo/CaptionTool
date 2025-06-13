using CaptionTool.scripts.graph;
using Godot;

namespace CaptionTool.scripts.ui;

public partial class WorkflowListItem : Control
{
    [Export] public Button LoadButton;
    [Export] public Button RenameButton;
    [Export] public Button DeleteButton;
    [Export] public Label NameLabel;

    [Export] public ConfirmationDialog deleteDialog;
    [Export] public RenameWorkflowPopup renameDialog;
    
    public string relatedWorkflow;

    public GraphManager manager;

    public override void _Ready()
    {
        NameLabel.Text = relatedWorkflow;

        deleteDialog.DialogText = $"Are you sure you want to delete {relatedWorkflow}?";

        LoadButton.Pressed += () =>
        {
            manager.LoadGraph(relatedWorkflow);
        };

        renameDialog.oldName = relatedWorkflow;
        renameDialog.manager = manager;
        renameDialog.ManualReady();
        RenameButton.Pressed += () =>
        {
            renameDialog.Show();
        };

        DeleteButton.Pressed += () =>
        {
            deleteDialog.Show();
        };

        deleteDialog.Confirmed += () =>
        {
            manager.DeleteGraph(relatedWorkflow);
        };
    }
}