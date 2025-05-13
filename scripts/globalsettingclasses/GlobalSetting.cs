using Godot;
using System;
using System.Collections.Generic;

public partial class GlobalSetting : Node
{
    public static Dictionary<string, GlobalSetting> dictionary = new ();
    
    [Export] public string Name;
    [Export] private Control valueSource; // Using this is invalid for this in C#

    public string DefaultValue; // Set in ready
    public string LoadPath => $"user://globalsettings/{Name}";

    public override void _Ready()
    {
        dictionary[Name] = this; // Global lookup
        DefaultValue = GetValue();
        Load();

        switch (valueSource)
        {
            case LineEdit edit:
                edit.TextChanged += _ => Save();
                break;
            case TextEdit edit:
                edit.TextChanged += Save;
                break;
        }
    }

    public string GetValue()
    {
        switch (valueSource)
        {
            case LineEdit edit:
                return edit.Text;
            case TextEdit edit:
                return edit.Text;
        }

        return "";
    }

    public void SetValue(string value)
    {
        switch (valueSource)
        {
            case LineEdit edit:
                edit.Text = value;
                break;
            case TextEdit edit:
                edit.Text = value;
                break;
        }
    }
    
    public void Save()
    {
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(LoadPath.GetBaseDir()));
        using (var writer = FileAccess.Open(LoadPath, FileAccess.ModeFlags.Write))
        {
            writer.StoreString(GetValue());
        }
    }

    public void Load()
    {
        if (!FileAccess.FileExists(LoadPath)) return;
        
        using (var reader = FileAccess.Open(LoadPath, FileAccess.ModeFlags.Read))
        {
            var value = reader.GetAsText();
            SetValue(value);
        }
    }
}
