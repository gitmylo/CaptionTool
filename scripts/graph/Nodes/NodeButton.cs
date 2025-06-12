using Godot;

namespace CaptionTool.scripts.graph.Nodes;

public partial class NodeButton : PanelContainer
{
    [Export] public string name;
    [Export] public string description;

    [Export] public NodeDef def;

    [Export] private Label nameLabel;
    [Export] private Label descriptionLabel;
    [Export] private Button selectButton;
    
    [Export] public PackedScene node;
    [Export] public GraphManager graph;

    public override void _Ready()
    {
        selectButton.Pressed += () =>
        {
            graph.AddNode(def);
        };
    }

    public void InitReady()
    {
        nameLabel.Text = name;
        descriptionLabel.Text = description;
    }
}