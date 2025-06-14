using CaptionTool.scripts.graph.Nodes.impl.scripts;
using Godot;

namespace CaptionTool.scripts.graph;

public partial class ProgressItem: Control
{
    [Export] private ProgressBar bar; // Indeterminate unless completed or not started
    [Export] private ColorIndicator indicator;
    [Export] private Label nameLabel;
    [Export] private Button removeButton;
    [Export] private Button printErrorButton;
    
    public ExecutionTracker tracker;

    public void Init(ExecutionTracker tracker, string name)
    {
        this.tracker = tracker;
        nameLabel.Text = name;
        removeButton.Pressed += () =>
        {
            if (tracker.status == ExecutionTracker.Status.Running)
                Cancel();
            
            QueueFree();
        };
        printErrorButton.Pressed += () =>
        {
            GD.Print("ERROR");
            if (tracker.error != null)
            {
                GD.Print(tracker.error.ToString());
                throw tracker.error;
            }
        };
    }

    public void Cancel()
    {
        tracker.Cancel();
    }

    public bool Completed()
    {
        return tracker.status == ExecutionTracker.Status.Success || tracker.status == ExecutionTracker.Status.Failure;
    }

    public override void _Process(double delta)
    {
        if (tracker != null)
        {
            SetUiStatus(tracker.status);
        }
    }

    public void SetUiStatus(ExecutionTracker.Status status)
    {
        switch (status)
        {
            case ExecutionTracker.Status.Idle:
                bar.Value = 0;
                bar.Indeterminate = false;
                indicator.MarkUnSaved(); // Yellow
                break;
            case ExecutionTracker.Status.Running:
                bar.Indeterminate = true;
                indicator.MarkUnSaved();
                break;
            case ExecutionTracker.Status.Success:
                bar.Value = 100;
                bar.Indeterminate = false;
                indicator.MarkSaved(); // Green
                break;
            case ExecutionTracker.Status.Failure:
                bar.Value = 100;
                bar.Indeterminate = false;
                indicator.MarkNotCreated();
                printErrorButton.Visible = true;
                break;
        }
    }
}