using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;
using FileAccess = Godot.FileAccess;

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
    
    // Frame extract, `ffmpeg -i troll.jpg -f image2pipe - > troll.png`
    public static string GetVideoFrame(string inFile, double position)
    {
        var culture = CultureInfo.InvariantCulture;
        
        string command = "ffmpeg";
        string[] parameters = ["-ss", position.ToString(culture), "-i", inFile, "-frames:v", "1", "-c:v", "png", "-f", "image2pipe", "-"];
        
        var process = OS.ExecuteWithPipe(command, parameters, true);
        var io = process["stdio"].As<FileAccess>();
        var pid = process["pid"].As<int>();
        
        var stream = new MemoryStream();
        
        while (true)
        {
            var length = (long)1024*8;
            GD.Print("Reading");
            var buffer = io.GetBuffer(length);
            
            // if (buffer.All(x => x == 0)) break;
            stream.Write(buffer, 0, buffer.Length);
            if (buffer.Length == 0) break;
        }

        if (OS.IsProcessRunning(pid)) OS.Kill(pid); // Ensure no more process
        
        var frameData = stream.ToArray();
        if (frameData.Length == 0)
        {
            throw new Exception("Reading failed, read 0 bytes");
        }
        var base64 = Convert.ToBase64String(frameData);
        return base64;
    }
    
    public static VideoInfo GetVideoInfo(string file)
    {
        string command = "ffprobe";
        string[] parameters = ["-of", "json", "-select_streams", "v:0", "-show_entries", "stream=display_aspect_ratio,width,height,r_frame_rate:format=filename,size,duration", file];

        Array output = new Array();
        OS.Execute(command, parameters, output);
        
        return VideoInfo.FromOutput(output[0].AsString());
    }

    public class VideoInfo
    {
        public int width, height, frameCount, fileSize;
        public double duration, frameRate;
        public string filename;

        public VideoInfo(int width, int height, double duration, double frameRate, int frameCount, string filename,
            int fileSize)
        {
            this.width = width;
            this.height = height;
            this.duration = duration;
            this.frameRate = frameRate;
            this.frameCount = frameCount;
            this.filename = filename;
            this.fileSize = fileSize;
        }
        
        public static VideoInfo FromOutput(string output)
        {
            var dict = Json.ParseString(output).AsGodotDictionary<string, Variant>();
            var firstStream = dict["streams"].AsGodotArray<Dictionary<String, Variant>>()[0];
            var format = dict["format"].AsGodotDictionary<string, string>();

            var frameRateStrings = firstStream["r_frame_rate"].AsString().Split("/");
            var frameRate = (double)int.Parse(frameRateStrings[0]) / int.Parse(frameRateStrings[1]);
            var duration = double.Parse(format["duration"], CultureInfo.InvariantCulture);
            var size = int.Parse(format["size"]);

            return new VideoInfo(firstStream["width"].AsInt32(), firstStream["height"].AsInt32(), duration, frameRate, (int)(duration * frameRate), format["filename"], size);
        }
    }

    public class Container<T>
    {
        public T value;
    }
}