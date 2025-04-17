using System;
using System.Diagnostics;
using System.Globalization;
using Godot;
using Array = Godot.Collections.Array;

namespace CaptionTool.scripts.util;

// Assums ffmpeg is installed system-wide
public static class FfmpegUtil
{
    public static void CutVideo(string inFile, string outfile, double startSeconds, double endSeconds, Config config)
    {
        var culture = CultureInfo.InvariantCulture;
        
        // ffmpeg -ss 60.0 -to 120.0 -i input.mp4 -c copy output.mp4
        string command = "ffmpeg";
        string[] parameters = ["-ss", startSeconds.ToString(culture), "-to", endSeconds.ToString(culture), "-i", inFile, "-filter:v", $"fps={config.fps}", outfile];

        // Array output = new Array();
        
        OS.CreateProcess(command, parameters, true);
        // var outputString = (output[0].Obj as string);

        // GD.Print($"Output:\n{outputString}");
    }

    public static string PlayFull(string inFile, Rect2 rect, double seek = 0)
    {
        var size = rect.Size;
        var pos = rect.Position;
        
        string randomName = Guid.NewGuid().ToString();
        
        // ffplay -vf trim=0.0:1.0 -af atrim=0.0:1.0 -loop 0 -i inFile
        var culture = CultureInfo.InvariantCulture;
        
        string command = "ffplay";
        string[] parameters = ["-window_title", randomName
            , "-x", size.X.ToString(culture), "-y", size.Y.ToString(culture), "-left", pos.X.ToString(culture), "-top", pos.Y.ToString(culture)
            , "-alwaysontop"
            , "-an", "-ss", seek.ToString(culture), "-loop", "0", "-i", inFile];

        OS.CreateProcess(command, parameters);
        // OS.Execute(command, parameters);

        return randomName;
    }

    public static string PlayPartial(string inFile, Rect2 rect, double startSeconds, double endSeconds, double seek = 0)
    {
        var size = rect.Size;
        var pos = rect.Position;
        
        string randomName = Guid.NewGuid().ToString();
        
        // ffplay -vf trim=0.0:1.0 -af atrim=0.0:1.0 -loop 0 -i inFile
        var culture = CultureInfo.InvariantCulture;

        string command = "ffplay";
        string[] parameters = ["-window_title", randomName
            , "-x", size.X.ToString(culture), "-y", size.Y.ToString(culture), "-left", pos.X.ToString(culture), "-top", pos.Y.ToString(culture)
            , "-alwaysontop"
            , "-an", "-ss", seek.ToString(culture), "-vf", $"trim={startSeconds.ToString(culture)}:{endSeconds.ToString(culture)}", "-loop", "0", "-i", inFile];

        OS.CreateProcess(command, parameters);
        // OS.Execute(command, parameters);
        
        return randomName;
    }

    public static double GetDuration(string inFile)
    {
        // ffprobe -v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 inFile
        string command = "ffprobe";
        string[] parameters = ["-v", "error", "-show_entries", "format=duration", "-of", "default=noprint_wrappers=1:nokey=1", inFile];
        
        Array output = new Array();
        
        OS.Execute(command, parameters, output);
        var duration = (output[0].Obj as string);

        if (double.TryParse(duration, NumberFormatInfo.InvariantInfo, out var num))
        {
            return num;
        }

        return -1;
    }

    public static float GetAspectRatio(string inFile)
    {
        // ffprobe -v error -select_streams v:0 -show_entries stream=display_aspect_ratio -of csv inFile

        string command = "ffprobe";
        string[] parameters = ["-v", "error", "-select_streams", "v:0", "-show_entries", "stream=display_aspect_ratio", "-of", "csv", inFile];

        Array output = new Array();

        OS.Execute(command, parameters, output);
        var aspectCsv = (output[0].Obj as string);
        var aspectVals = aspectCsv.Split(",");
        if (aspectVals.Length == 2)
        {
            var aspectVal = aspectVals[1];
            var aspectHV = aspectVal.Split(":");
            if (aspectHV.Length == 2)
            {
                var hor = aspectHV[0];
                var vert = aspectHV[1];

                if (int.TryParse(hor, NumberFormatInfo.InvariantInfo, out var horVal)
                    && int.TryParse(vert, NumberFormatInfo.InvariantInfo, out var vertVal))
                {
                    return (float)horVal / vertVal;
                }
            }
        }
        
        return -1;
    }

    // Get the out path for the .mp4/webm and the .txt. Finds a valid index before returning to avoid duplicates.
    public static (string, string) GetOutPaths(string inFile, string outDir)
    {
        // Examples for path/to/file.mp4
        string fileExt = inFile.GetExtension(); // mp4
        string fileName = inFile.GetFile().GetBaseName(); // file

        int index = 0;
        bool exists;
        string outFileName;
        do
        {
            outFileName = $"{fileName}_{index}";
            exists = FileAccess.FileExists(ConstructFileName(outDir, outFileName, fileExt));
            index++;
        } while (exists);

        return (ConstructFileName(outDir, outFileName, fileExt), ConstructFileName(outDir, outFileName, "txt"));
    }

    public static string ConstructFileName(string dir, string filename, string extension) =>
        dir.PathJoin($"{filename}.{extension}");

    public static string CutVideoAndCreateCaption(string inFile, string outDir, string caption, double startSeconds, double endSeconds, Config config)
    {
        var (videoOut, captionOut) = GetOutPaths(inFile, outDir);
        if (caption.Length != 0 || config.saveTxt)
        {
            using (var file = FileAccess.Open(captionOut, FileAccess.ModeFlags.Write))
            {
                if (file == null)
                {
                    GD.PushError($"Could not open {captionOut} for writing.");
                    return null;
                }

                file.StoreString(caption);
            }
        }

        CutVideo(inFile, videoOut, startSeconds, endSeconds, config);
        return videoOut;
    }
}