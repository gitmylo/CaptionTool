using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CaptionTool.scripts;
using CaptionTool.scripts.util;
using Newtonsoft.Json;

public partial class NewUI : Node
{
    public static readonly string[] ImageExtensions = ["png", "jpg", "jpeg", "bmp", "webp"];
    
    #region EXPORTS_AND_PROPERTIES
    
    [Export] private TabContainer MainTabs;
    [Export] private SettingsUI SettingsTab;

    [Export] private FileDialog WorkspaceDialog;
    
    [Export] private Tree inFiles;
    private TreeItem inFilesRoot;
    [Export] private VideoStreamPlayer player;

    [Export] private Button playPause, saveButton, newButton, deleteButton;

    [Export] private Slider progressBar;//, minSlider, maxSlider;
    [Export] private MinMaxSlider rangeSlider;

    [Export] private TextEdit captionBox;
    [Export] private AspectRatioContainer aspectBox;
    [Export] private TextureRect imageBox;
    [Export] private ItemList EntryList;

    [Export] private Label activeVidLabel;
    public string activeVid;
    private SaveableCaption[] captions;
    private int selectedCaption = -1; // Idx of selected caption, -1 if nothing is selected
    private double activeDuration;
    private float activeAspect;

    [Export] private ColorIndicator indicator;
    [Export] private Label rangeIndicatorText;
    
    public Config config;
    public string selectedWorkspace;
    public string configPath => selectedWorkspace.PathJoin("workspace.json");
    public string sourceDir => selectedWorkspace.PathJoin(config.inDir);
    public string procDir => selectedWorkspace.PathJoin(config.procDir);
    public string destDir => selectedWorkspace.PathJoin(config.outDir);

    [Export] private SpinBox threadCount;

    [Export] private Button exportButton;
    [Export] private ProgressBar mainProgressBar;
    [Export] private BoxContainer progressBarParent;
    [Export] private PackedScene progressBarScene;
    [Export] private LineEdit exportFlags;

    public string CaptionFileFor(string original) =>
        selectedWorkspace.PathJoin(config.procDir).PathJoin(original.TrimPrefix(sourceDir)).GetBaseName() + ".json";
    public string captionsTemplateFile => CaptionFileFor(activeVid);// selectedWorkspace.PathJoin(config.procDir).PathJoin(activeVid.TrimPrefix(sourceDir)).GetBaseName() + ".json";
    
    #endregion

    #region SETUP

    public void EnsureDir(string dir)
    {
        DirAccess.MakeDirAbsolute(dir);
    }
    
    public override void _Ready()
    {
        Engine.MaxFps = 60;
        
        InitNew();
        SettingsTab.SettingChanged += () =>
        {
            UpdateConfigSave();
        };
        
        WorkspaceDialog.Show(); // Show at the start

        inFiles.SetColumnExpand(0, true);
        inFiles.SetColumnExpand(1, false);
        inFiles.SetColumnCustomMinimumWidth(1, 50);
        inFilesRoot = inFiles.CreateItem();
        inFiles.ItemActivated += () =>
        {
            var item = inFiles.GetSelected();
            var path = item.GetTooltipText(0); // Contains the full path
            GD.Print($"Selected file {path}.");
            // SelectVideo(path);
            SelectMedia(path);
        };
        
        bool? wasPaused = null;
        SceneTreeTimer lastTimer = null;
        progressBar.ValueChanged += value =>
        {
            // var (start, end, _) = GetRange();
            // player.SetStreamPosition(Mathf.Lerp(start, end, value / 100));
            player.SetStreamPosition(player.GetStreamLength() * value);

            if (wasPaused.HasValue)
            {
                wasPaused = player.Paused;
            }
            player.Paused = false; // Note: does not show new current frame

            var timer = GetTree().CreateTimer(0.2);
            lastTimer = timer;
            timer.Timeout += () =>
            {
                if (timer == lastTimer)
                {
                    player.Paused = true;
                    player.SetStreamPosition(player.GetStreamLength() * value);
                }
            };
        };
		
        progressBar.GuiInput += @event =>
        {
            if (@event is InputEventMouseButton mouseEvent)
            {
                if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.IsReleased())
                {
                    if (wasPaused.HasValue)
                    {
                        var oldPos = player.GetStreamPosition();
                        GetTree().CreateTimer(0.5).Timeout += () =>
                        {
                            player.SetStreamPosition(oldPos);
                            player.Paused = wasPaused.Value;
                            wasPaused = null;
                        };
                    }
                }
            }
        };

        // minSlider.ValueChanged += _ => MarkIndicatorChangeAccurate();
        // maxSlider.ValueChanged += _ => MarkIndicatorChangeAccurate();
        rangeSlider.ValueChanged += _ => MarkIndicatorChangeAccurate();
        rangeSlider.AlwaysTriggerValueChanged += _ => UpdateRangeIndicatorText();
        captionBox.TextChanged += MarkIndicatorChangeAccurate;

        EntryList.ItemSelected += index =>
        {
            SelectCaption((int)index);
        };
    }

    public override void _Process(double delta)
    {
        var (start, end, _) = GetRange();
        var pos = player.StreamPosition;
        var length = player.GetStreamLength();
		
        if (player.IsPlaying())
        {
            if (pos > end || pos < start) // Ensures it stays in bounds
            {
                player.SetStreamPosition(start);
            }

            // if (!player.IsPaused())
            {
                // var length = end - start;
                // var relPos = pos - start;
                // var prog = relPos / length;

                var prog = pos / length;
                progressBar.SetValueNoSignal(prog);
            }
        }

        exportButton.Disabled = busyRef.busy;

        UpdateMainProgressBar();
    }

    public void MarkIndicatorChangeAccurate()
    {
        if (selectedCaption == -1)
        {
            indicator.MarkNotCreated();
            return;
        }
        indicator.MarkUnSaved();
    }

    void WorkspaceMenuOptionSelected(int id)
    {
        switch (id)
        {
            case 0: // Open
                WorkspaceDialog.Show();
                break;
            case 1: // refresh
                RefreshPaths();
                break;
            case 2: // Exit
                GetTree().Quit();
                break;
        }
    }

    void InitNew()
    {
        MainTabs.Visible = false;

        activeVid = "";
        SelectCaption(-1); // Clears caption input and sliders as well
        captions = null;
        activeVidLabel.Text = "No video selected";
        EntryList.SetItemCount(0);
    }

    void SelectWorkspace(string dir)
    {
        InitNew();
        MainTabs.Visible = true;
        selectedWorkspace = dir;

        config = new Config();
        if (FileAccess.FileExists(configPath))
        {
            UpdateConfigLoad();
            RefreshPaths();
        }
        else
        {
            SettingsTab.Visible = true; // Switch to settings tab when a new workspace was created
            UpdateConfigSave();
        }
    }
    
    #endregion

    #region CONFIGS

    void SaveConfig()
    {
        if (selectedWorkspace == "") return;
        string saveData = JsonConvert.SerializeObject(config);
        using (FileAccess f = FileAccess.Open(configPath, FileAccess.ModeFlags.Write))
        {
            f.StoreString(saveData);
        }
    }

    void UpdateConfigSave()
    {
        SettingsTab.ConfigFromSettings(config);
        SaveConfig();
    }

    void LoadConfig()
    {
        if (selectedWorkspace == "") return;
        string saveData;
        using (FileAccess f = FileAccess.Open(configPath, FileAccess.ModeFlags.Read))
        {
            saveData = f.GetAsText();
        }

        // var dict = (Dictionary) Json.ParseString(saveData);
        // config.FromDict(dict);
        config = JsonConvert.DeserializeObject<Config>(saveData);
    }

    void UpdateConfigLoad()
    {
        LoadConfig();
        SettingsTab.SettingsFromConfig(config);
    }
    
    #endregion

    #region CAPTIONING

    void SelectMedia(string file)
    {
        activeVid = file;
        activeVidLabel.Text = file;

        captions = CaptionsForVideo(); // Try to get the already written captions.
        InitCaptionsList(captions.Length);
        
        if (ImageExtensions.Contains(file.GetExtension().ToLower()))
        {
            aspectBox.Visible = false;
            imageBox.Visible = true;
            playPause.Disabled = true;
            player.Paused = true;
            SelectImage(file);
        }
        else
        {
            aspectBox.Visible = true;
            imageBox.Visible = false;
            playPause.Disabled = false;
            SelectVideo(file);
        }
        
        SelectCaption(0); // Try to select the first caption
    }

    void SelectImage(string file)
    {
        imageBox.Texture = ImageTexture.CreateFromImage(Image.LoadFromFile(file));
    }
    
    void SelectVideo(string file)
    {
        player.GetStream().SetFile(file);
        activeDuration = FfmpegUtil.GetDuration(file);
        activeAspect = FfmpegUtil.GetAspectRatio(file);
        aspectBox.Ratio = Mathf.Abs(activeAspect);
        GD.Print($"Active duration: {activeDuration}, aspect ratio: {activeAspect}");
        
        if (player.IsPlaying())
        {
            player.Stop();
        }
        player.SetStreamPosition(0);
        player.Play();
        player.Paused = false;
        GetTree().CreateTimer(.2).Timeout += () =>
        {
            player.Paused = true;
            player.SetStreamPosition(0);
        };
    }
    
    // Util functions
    public void AddFilePathsRecursive(string basePath, TreeItem item)
    {
        foreach (var dir in DirAccess.GetDirectoriesAt(basePath))
        {
            var child = item.CreateChild();
            child.SetText(0, dir);
            AddFilePathsRecursive(basePath.PathJoin(dir), child);

            if (child.GetChildCount() == 0)
            {
                item.RemoveChild(child); // No empty directories need to be shown
            }
            else
            {
                child.Collapsed = true;
            }
        }
		
        foreach (var file in DirAccess.GetFilesAt(basePath))
        {
            if (blacklist.Contains(file.GetExtension())) continue;
            var child = item.CreateChild();
            child.SetText(0, file);
            child.SetText(1, CaptionsForVideo(basePath.PathJoin(file)).Length.ToString()); // TODO: Caption count, update automatically when saving
            child.SetTooltipText(0, basePath.PathJoin(file));
        }
    }

    private string[] blacklist = ["txt"];
    public List<string> GetAllFilePathsRecursive(string basePath, List<string> addTo = null)
    {
        if (addTo == null) addTo = new List<string>();
        foreach (var dir in DirAccess.GetDirectoriesAt(basePath))
        {
            GetAllFilePathsRecursive(basePath.PathJoin(dir), addTo);
        }

        foreach (var file in DirAccess.GetFilesAt(basePath))
        {
            if (blacklist.Contains(file.GetExtension())) continue;
            addTo.Add(basePath.PathJoin(file));
        }

        return addTo;
    }
    
    public void ResetSliders()
    {
        // minSlider.Value = minSlider.MinValue;
        // maxSlider.Value = maxSlider.MaxValue;
        rangeSlider.SetValuesNoEvent(0, 1);
        progressBar.Value = 0;
        captionBox.Text = "";
    }
    
    public void TogglePlay()
    {
        // FfmpegUtil.PlayFull(activeVid, player.GetGlobalRect() with {Position = player.GetGlobalPosition() + DisplayServer.WindowGetPosition()});
        var (start, end, seek) = GetRange();
        GD.Print($"{start}-{end} at {seek}");
        // FfmpegUtil.PlayPartial(activeVid,
        // player.GetGlobalRect() with { Position = player.GetGlobalPosition() + DisplayServer.WindowGetPosition() }, start, end, seek);
        // player.Paused = !player.Paused;
        if (player.IsPlaying())
        {
            player.Paused = !player.Paused;
        }
        else
        {
            player.Play();
        }
    }
    
    public (double, double, double) GetRange()
    {
        var vidLength = activeDuration;
        // var first = minSlider.Value * vidLength;
        // var second = maxSlider.Value * vidLength;
        var first = rangeSlider.a * vidLength;
        var second = rangeSlider.b * vidLength;

        var start = Math.Min(first, second);
        var end = Math.Max(first, second);
        return (start, end, progressBar.Value * (end-start));
    }
    
    public void RefreshPaths()
    {
        var childrenIn = inFilesRoot.GetChildren();
        foreach (var child in childrenIn)
        {
            inFilesRoot.RemoveChild(child);
        }
        AddFilePathsRecursive(sourceDir, inFilesRoot);
    }

    public SaveableCaption[] CaptionsForVideo(string targetPath = null)
    {
        if (targetPath == null) targetPath = captionsTemplateFile;
        else targetPath = CaptionFileFor(targetPath);
        if (!FileAccess.FileExists(targetPath))
        {
            return []; // Not found or failed
        }

        string value;
        using (var f = FileAccess.Open(targetPath, FileAccess.ModeFlags.Read))
        {
            value = f.GetAsText();
        }

        return JsonConvert.DeserializeObject<SaveableCaption[]>(value) ?? new SaveableCaption[]{};
    }

    public void SaveCaptionsForCurrent()
    {
        SaveCaptionFor(activeVid, captions);
        // string targetPath = captionsTemplateFile;
        // string value = JsonConvert.SerializeObject(captions);
        // DirAccess.MakeDirRecursiveAbsolute(targetPath.GetBaseDir());
        // using (var f = FileAccess.Open(targetPath, FileAccess.ModeFlags.Write))
        // {
        //     f.StoreString(value);
        // }
    }

    public TreeItem FindTreeItemByPathRecursive(string path, TreeItem item = null)
    {
        if (item == null)
        {
            item = inFilesRoot;
        }

        if (item.GetTooltipText(0) == path)
        {
            return item;
        }

        return item.GetChildren().Select(x => FindTreeItemByPathRecursive(path, x)).FirstOrDefault(x => x != null);
    }

    public void SaveCaptionFor(string target, SaveableCaption[] captions)
    {
        string targetPath = CaptionFileFor(target);
        string value = JsonConvert.SerializeObject(captions);
        DirAccess.MakeDirRecursiveAbsolute(targetPath.GetBaseDir());
        using (var f = FileAccess.Open(targetPath, FileAccess.ModeFlags.Write))
        {
            f.StoreString(value);
        }
        
        var item = FindTreeItemByPathRecursive(target);
        if (item != null)
        {
            item.SetText(1, captions.Length.ToString());
        }
    }

    public void InitCaptionsList(int count)
    {
        EntryList.SetItemCount(0);
        for (int i = 0; i < count; i++)
        {
            EntryList.AddItem((EntryList.ItemCount + 1).ToString());
        }
    }

    public void SelectCaption(int id)
    {
        if (captions == null || id > captions.Length - 1)
        {
            id = -1; // Deselect and clear
            EntryList.DeselectAll();
            // return;
        }
        
        selectedCaption = id;

        if (id < 0)
        {
            captionBox.Text = "";
            // minSlider.Value = minSlider.MinValue;
            // maxSlider.Value = maxSlider.MaxValue;
            rangeSlider.SetValuesNoEvent(0, 1);
            return;
        }

        var caption = captions[id];

        captionBox.Text = caption.caption;
        // minSlider.Value = caption.start / activeDuration;
        // maxSlider.Value = caption.end / activeDuration;
        // GD.Print(caption.start, " ", caption.end);
        rangeSlider.SetValuesNoEvent(caption.start / activeDuration, caption.end / activeDuration);
        GD.Print(caption.start, " ", activeDuration);

        EntryList.Select(id); // Does not trigger signal, safe
        
        indicator.MarkSaved();
    }
    
    // Button responses
    public void SaveButtonPressed()
    {
        if (selectedCaption == -1)
        {
            NewButtonPressed(); // Save with nothing selected should create a new entry
            return;
        }

        var selected = captions[selectedCaption];
        var (start, end, _) = GetRange();
        selected.caption = captionBox.Text;
        selected.start = start;
        selected.end = end;
        SaveCaptionsForCurrent();
        indicator.MarkSaved();
    }

    public void NewButtonPressed()
    {
        var (start, end, _) = GetRange();
        captions = captions.Append(new SaveableCaption
        {
            caption = captionBox.Text,
            start = start,
            end = end
        }).ToArray();
        SaveCaptionsForCurrent();
        indicator.MarkSaved();

        EntryList.AddItem((EntryList.ItemCount + 1).ToString());
        SelectCaption(captions.Length - 1);
    }

    public void DeleteButtonPressed()
    {
        if (selectedCaption == -1 || selectedCaption > captions.Length - 1)
        {
            return;
        }

        captions = captions.Where((_, i) => i != selectedCaption).ToArray();
        EntryList.RemoveItem(selectedCaption);
        SelectCaption(Math.Max(0, selectedCaption - 1));
        SaveCaptionsForCurrent();
        indicator.MarkSaved();
        FixEntryList();
    }

    public void FixEntryList()
    {
        for (int i = 0; i < EntryList.ItemCount; i++)
        {
            EntryList.SetItemText(i, (i + 1).ToString());
        }
    }

    public void UpdateRangeIndicatorText()
    {
        var (start, end, _) = GetRange();
        rangeIndicatorText.Text = $"[{MakePrintable(start)}s -> {MakePrintable(end)}s] ({MakePrintable(end - start)}s)";
    }

    public string MakePrintable(double number)
    {
        return number.ToString("0.00");
    }
    
    #endregion
    
    #region EXPORTING

    public List<ExportableEntry> GetEntriesRecursive(string currentPath = "", List<ExportableEntry> current = null)
    {
        if (currentPath == "") currentPath = sourceDir;
        if (current == null) current = new List<ExportableEntry>();
        var dir = DirAccess.Open(currentPath);
        foreach (var subDir in dir.GetDirectories())
        {
            GetEntriesRecursive(currentPath.PathJoin(subDir), current);
        }
        foreach (var file in dir.GetFiles())
        {
            string path = currentPath.PathJoin(file);
            var captions = CaptionsForVideo(path);
            if (captions.Length == 0 && SettingsTab.saveTxtBox.Selected >= 2) // On 2 and 3, don't save empty captions at all
            {
                current.Add(new ExportableEntry(path, 0, config, new SaveableCaption {bypassduration = true}));
            }
            else for (int i = 0; i < captions.Length; i++)
            {
                var caption = captions[i];
                current.Add(new ExportableEntry(path, i, config, caption));
            }
        }
        
        return current;
    }

    private ConcurrentBag<StatusContainer> exportStatuses = new();
    private BusyRef busyRef = new BusyRef { busy = false }; // Reference which indicates if exporting is busy
    
    public void ExportButtonPressed()
    {
        if (busyRef.busy) return;
        busyRef.busy = true; // Mark as busy
        
        var entries = GetEntriesRecursive().Select(entry => new StatusContainer()
        {
            name = entry.sourceFile,
            token = new CancellationToken(),
            entry = entry
        });
        exportStatuses = new ConcurrentBag<StatusContainer>(entries);
        foreach (var entry in exportStatuses)
        {
            var item = progressBarScene.Instantiate<ProgressBarItem>();
            item.Init(entry);
            progressBarParent.AddChild(item);
        }
        
        new Thread(_ =>
        {
            Parallel.ForEach(exportStatuses, new ParallelOptions() { MaxDegreeOfParallelism = (int)threadCount.Value }, entry =>
            {
                entry.entry.Export(destDir, exportFlags.Text, entry).Wait();
            });
            busyRef.busy = false;
        }).Start();
    }

    public void UpdateMainProgressBar()
    {
        double total = 0;
        double completed = 0;
        foreach (var child in progressBarParent.GetChildren())
        {
            if (child is ProgressBarItem bar)
            {
                total += 1;
                completed += bar.progressBar.Value;
            }
        }

        mainProgressBar.Value = completed / total;
    }

    #endregion
}

class BusyRef
{
    public bool busy;
}
