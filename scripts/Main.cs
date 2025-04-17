using Godot;
using System;
using CaptionTool.scripts.util;

public partial class Main : Node
{
	[Export] private Tree inFiles;
	private TreeItem inFilesRoot;
	[Export] private Tree outFiles;
	private TreeItem outFilesRoot;
	[Export] private VideoStreamPlayer player;

	[Export] private Button playPause;
	[Export] private Button saveButton;

	[Export] private Slider progressBar;
	[Export] private Slider minSlider;
	[Export] private Slider maxSlider;

	[Export] private TextEdit captionBox;
	[Export] private AspectRatioContainer aspectBox;
	
	private string sourceDir, destDir;

	private string activeVid;
	private double activeDuration;
	private float activeAspect;

	private Config config;
	
	public void InitPaths(string sourceDir, string destDir, Config config)
	{
		this.sourceDir = sourceDir;
		this.destDir = destDir;
		this.config = config;
		
		InitComponents();
		AddFilePathsRecursive(sourceDir, inFilesRoot);
		AddFilePathsRecursive(destDir, outFilesRoot);
	}

	public void InitComponents()
	{
		// Files
		inFilesRoot = inFiles.CreateItem();
		inFilesRoot.SetText(0, "Root");
		inFiles.ButtonClicked += (item, column, id, index) =>
		{
			var path = item.GetTooltipText(0); // Contains the full path
			GD.Print($"Selected file {path}.");
			SelectFile(path);
		};
		inFiles.ItemActivated += () =>
		{
			var item = inFiles.GetSelected();
			var path = item.GetTooltipText(0); // Contains the full path
			GD.Print($"Selected file {path}.");
			SelectFile(path);
		};
		outFilesRoot = outFiles.CreateItem();
		outFilesRoot.SetText(0, "Root");

		bool? wasPaused = null;
		SceneTreeTimer lastTimer = null;
		progressBar.ValueChanged += value =>
		{
			// var (start, end, _) = GetRange();
			// player.SetStreamPosition(Mathf.Lerp(start, end, value / 100));
			player.SetStreamPosition(player.GetStreamLength() * (value / 100));

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
					player.SetStreamPosition(player.GetStreamLength() * (value / 100));
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
	}

	public void ResetSliders()
	{
		minSlider.Value = minSlider.MinValue;
		maxSlider.Value = maxSlider.MaxValue;
		progressBar.Value = 0;
		captionBox.Text = "";
	}

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
			var child = item.CreateChild();
			child.SetText(0, file);
			child.SetTooltipText(0, basePath.PathJoin(file));
		}
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

	public void SelectFile(string file)
	{
		activeVid = file;
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

	public void ApplyCaption()
	{
		string caption = captionBox.Text;
		var (startSec, endSec, _) = GetRange();
		var outVid = FfmpegUtil.CutVideoAndCreateCaption(activeVid, destDir, caption, startSec, endSec, config);
		if (outVid != null)
		{
			RefreshPaths(true);
		}
	}

	public void RefreshPaths(bool outOnly)
	{
		var childrenOut = outFilesRoot.GetChildren();
		foreach (var child in childrenOut)
		{
			outFilesRoot.RemoveChild(child);
		}
		AddFilePathsRecursive(destDir, outFilesRoot);

		if (outOnly) return;
		var childrenIn = inFilesRoot.GetChildren();
		foreach (var child in childrenIn)
		{
			inFilesRoot.RemoveChild(child);
		}
		AddFilePathsRecursive(sourceDir, inFilesRoot);
	}

	public (double, double, double) GetRange()
	{
		var vidLength = activeDuration;
		var first = minSlider.Value * vidLength / 100;
		var second = maxSlider.Value * vidLength / 100;

		var start = Math.Min(first, second);
		var end = Math.Max(first, second);
		return (start, end, (progressBar.Value * (end-start) / 100));
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
				progressBar.SetValueNoSignal(prog * 100);
			}
		}
	}
}
