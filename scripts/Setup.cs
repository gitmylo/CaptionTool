using Godot;
using System;
using CaptionTool.scripts.util;

public partial class Setup : Node
{
    [Signal] public delegate void MissingPathEventHandler();
    
    [Export(PropertyHint.GlobalDir)] private string source = "", dest = "";
    [Export] private SpinBox fpsSpinBox;
    [Export] private CheckBox saveTxt;

    [Export] private PackedScene MainScene;

    public override void _Ready()
    {
        if (source != "" && dest != "")
        {
            Start(); // Auto-init if already set
        }
    }

    public void SetSource(string source)
    {
        this.source = source;
    }

    public void SetDest(string dest)
    {
        this.dest = dest;
    }
    
    public void Start()
    {
        if (source == "" || dest == "")
        {
            EmitSignalMissingPath();
            return;
        }
        
        var scene = MainScene.Instantiate<Main>();
        foreach (var child in GetChildren())
        {
            child.QueueFree();
        }
        
        scene.InitPaths(source, dest, new Config {fps = (int)fpsSpinBox.Value, saveTxt = saveTxt.ButtonPressed});
        
        AddChild(scene);
    }
}
