using Godot;
using System;
using System.Linq;
using CaptionTool.scripts.util;

public partial class ProgressBarItem : Node
{
    [Export] private Label textLabel;
    [Export] public ProgressBar progressBar;
    [Export] private Button deleteButton;
    [Export] private Window statusWindow;
    [Export] private TextEdit logText;

    private StatusContainer status;
    private string lastLog = "";

    public override void _Ready()
    {
        deleteButton.Pressed += QueueFree;
    }

    public void Init(StatusContainer status)
    {
        textLabel.Text = status.name;
        progressBar.Value = status.progress;
        statusWindow.Title = $"Log for {status.name}";
        statusWindow.CloseRequested += () =>
        {
            statusWindow.Hide();
        };

        this.status = status;
    }

    public void UpdateProgress()
    {
        double progress = status.progress;
        if (progress >= 1)
        {
            deleteButton.Visible = true;
        }

        progressBar.Value = progress;
        if (statusWindow.Visible && status.log != lastLog)
        {
            var oldScroll = (logText.ScrollVertical, logText.ScrollHorizontal);
            logText.Text = status.log;
            (logText.ScrollVertical, logText.ScrollHorizontal) = oldScroll;
            
            lastLog = status.log;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        UpdateProgress();
    }
}
