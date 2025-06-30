using System;
using Godot;

namespace CaptionTool.scripts.util;

public partial class SettingsUI : Control
{
    [Export] private SpinBox fpsBox;
    [Export] public OptionButton saveTxtBox;
    [Export] private LineEdit inPath, procPath, outPath;

    public event Action SettingChanged;

    public override void _Ready()
    {
        fpsBox.ValueChanged += _ => InvokeUpdated();
        saveTxtBox.ItemSelected += _ => InvokeUpdated();

        inPath.TextChanged += _ => InvokeUpdated();
        procPath.TextChanged += _ => InvokeUpdated();
        outPath.TextChanged += _ => InvokeUpdated();
    }

    public void InvokeUpdated()
    {
        SettingChanged?.Invoke();
    }

    public void SettingsFromConfig(Config c)
    {
        fpsBox.Value = c.fps;
        saveTxtBox.Selected = c.saveTxt;
        
        inPath.Text = c.inDir;
        procPath.Text = c.procDir;
        outPath.Text = c.outDir;
    }

    public void ConfigFromSettings(Config c)
    {
        c.fps = (int) fpsBox.Value;
        c.saveTxt = saveTxtBox.Selected;

        c.inDir = inPath.Text;
        c.procDir = procPath.Text;
        c.outDir = outPath.Text;
    }
}