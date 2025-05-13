using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using Newtonsoft.Json;

namespace CaptionTool.scripts.util;

[JsonObject]
public class SaveableCaption
{
    [JsonProperty] public string caption;
    [JsonProperty] public double start;
    [JsonProperty] public double end;
}

public class StatusContainer // Status container with progress. For editing from other thread.
{
    public string name = "";
    public double progress = 0;
    public CancellationToken token = CancellationToken.None;
    public int exitCode = -2;
    public int pid = 0;
    public string log = "";

    public ExportableEntry entry;
}

public class ExportableEntry(string sourceFile, int index, Config config, SaveableCaption caption)
{
    public string sourceFile = sourceFile;
    public int index = index;
    public Config config = config;
    public SaveableCaption caption = caption;
    

    public Task<int> Export(string destDir, string exportFlags, StatusContainer status)
    {
        string outFilePath = destDir.PathJoin(sourceFile.GetFile());
        string mediaExt = outFilePath.GetExtension();
        string outPath = outFilePath.GetBaseName() + "_" + index;
        string outVideoPath = $"{outPath}.{mediaExt}";
        string outCaptionPath = $"{outPath}.txt";

        DirAccess.MakeDirRecursiveAbsolute(destDir);
        
        WriteCaption(outCaptionPath);
        return RunFfmpegCommandNonBlocking(outVideoPath, exportFlags, status);
    }

    public void WriteCaption(string captionFile)
    {
        if (caption.caption == "" && !config.saveTxt) return;
        
        using (var f = FileAccess.Open(captionFile, FileAccess.ModeFlags.Write))
        {
            f.StoreString(caption.caption);
        }
    }

    private static RegEx durationMatch = RegEx.CreateFromString("\\bDuration: (\\d+):(\\d+):(\\d+\\.\\d+)"); // 4 groups, full match, h:m:s
    private static RegEx timeMatch = RegEx.CreateFromString("\\btime=(\\d+):(\\d+):(\\d+\\.\\d+)");

    public double ParseFfmpegTimeToSeconds(string h, string m, string s)
    {
        var culture = CultureInfo.InvariantCulture;
        if (int.TryParse(h, culture, out var hours))
        {
            if (int.TryParse(m, culture, out var minutes))
            {
                if (double.TryParse(s, culture, out var seconds))
                {
                    return seconds + ((minutes + (hours * 60)) * 60);
                }
            }
        }
        
        return -1;
    }

    // Estimates the progress by reading the ffmpeg log
    public double GetProgressFromLog(string log)
    {
        var duration = durationMatch.Search(log);
        if (duration != null)
        {
            var durationStr = duration.Strings;
            var lengthS = ParseFfmpegTimeToSeconds(durationStr[1], durationStr[2], durationStr[3]);
            var progress = timeMatch.SearchAll(log);
            double timeS = 0;
            if (progress.Count != 0)
            {
                var last = progress.Last().Strings;
                timeS = ParseFfmpegTimeToSeconds(last[1], last[2], last[3]);
            }
            
            return timeS / lengthS;
        }
        
        return 0;
    }

    public async Task<int> RunFfmpegCommandNonBlocking(string outFile, string exportFlags, StatusContainer status)
    {
        var culture = CultureInfo.InvariantCulture;
        
        // ffmpeg -ss 60.0 -to 120.0 -i input.mp4 -c copy output.mp4
        string command = "ffmpeg";
        string[] exportFlagsSplit = exportFlags == "" ? [] : exportFlags.Split(" "); // Empty list instead of list with 1 empty string.
        string[] parameters = [..exportFlagsSplit, "-ss", caption.start.ToString(culture), "-to", caption.end.ToString(culture), "-i", sourceFile, "-y", "-filter:v", $"fps={config.fps}", outFile];
        
        // Array output = new Array();

        Dictionary info = OS.ExecuteWithPipe("ffmpeg", parameters, false); // Not blocking
        status.pid = (int)info["pid"];
        var stdErr = (FileAccess)info["stderr"];
        string readErr = "";
        //  Duration: 00:00:20.33, start: -0.007000, bitrate: 9257 kb/s
        //frame=  220 fps= 18 q=32.0 size=     768kB time=00:00:10.64 bitrate= 591.3kbits/s speed=0.879x

        // Cycle:
        await Task.Run(() =>
        {
            while (true)
            {
                if (OS.IsProcessRunning(status.pid))
                {
                    readErr += stdErr.GetAsText();
                    status.log = readErr;
                    status.progress = GetProgressFromLog(readErr);
                }
                else
                {
                    readErr += stdErr.GetAsText();
                    status.log = readErr;
                    status.exitCode = OS.GetProcessExitCode(status.pid);
                    status.progress = 1; // Done
                    break;
                }
                
                Thread.Sleep(50);
            }
        }, status.token);

        readErr += stdErr.GetAsText();
        status.log = readErr;

        status.progress = 1;
        
        return status.exitCode;
    }
}