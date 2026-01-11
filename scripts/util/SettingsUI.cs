using System;
using System.Linq;
using Godot;

namespace CaptionTool.scripts.util;

public partial class SettingsUI : Control
{
    [Export] private SpinBox fpsBox;
    [Export] public OptionButton saveTxtBox, bucketMode, fpsMode;
    [Export] private LineEdit inPath, procPath, outPath, buckets;

    public event Action SettingChanged;

    public override void _Ready()
    {
        fpsBox.ValueChanged += _ => InvokeUpdated();
        saveTxtBox.ItemSelected += _ => InvokeUpdated();
        bucketMode.ItemSelected += _ => InvokeUpdated();
        fpsMode.ItemSelected += _ => InvokeUpdated();

        inPath.TextChanged += _ => InvokeUpdated();
        procPath.TextChanged += _ => InvokeUpdated();
        outPath.TextChanged += _ => InvokeUpdated();
        buckets.TextChanged += _ => InvokeUpdated();
    }

    public void InvokeUpdated()
    {
        if (disableAutoSave) return;
        
        SettingChanged?.Invoke();
    }

    private bool disableAutoSave = false;

    public void SettingsFromConfig(Config c)
    {
        disableAutoSave = true;
        
        fpsBox.Value = c.fps;
        saveTxtBox.Selected = c.saveTxt;
        bucketMode.Selected = (int)c.bucketMode;
        fpsMode.Selected = (int)c.fpsMode;

        inPath.Text = c.inDir;
        procPath.Text = c.procDir;
        outPath.Text = c.outDir;
        buckets.Text = string.Join(", ", c.buckets);

        disableAutoSave = false;
    }

    public void ConfigFromSettings(Config c)
    {
        c.fps = (int) fpsBox.Value;
        c.saveTxt = saveTxtBox.Selected;
        c.bucketMode = (FrameBucketMode) bucketMode.Selected;
        c.fpsMode = (FpsMode) fpsMode.Selected;

        c.inDir = inPath.Text;
        c.procDir = procPath.Text;
        c.outDir = outPath.Text;
        try
        {
            c.buckets = buckets.Text.Split(",").Select(x => x.Trim()).Select(int.Parse).ToList();
        }catch (Exception e) {}
    }
}