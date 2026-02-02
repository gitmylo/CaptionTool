using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

namespace CaptionTool.scripts.graph.Nodes.impl.scripts.util;

[GlobalClass]
public partial class ResizeImage : ExecutionCore
{
    public override async Task<Array<Array>> Execute(Array<Array> inputs, NodeExecutionContext context, Array values)
    {
        var outputImages = Inner();
        foreach (var (img, target) in inputs[0].GrowZip<string, double>(inputs[1]))
        {
            outputImages.Add(ResizeBase64Image(img, (int)target));
        }

        return Results(outputImages);
    }
    
    public static string ResizeBase64Image(string base64Source, int max)
    {
        // 1. Decode Base64 to Byte Array
        byte[] imageBytes = Convert.FromBase64String(base64Source);

        // 2. Load bytes into a Godot Image
        var image = new Image();
        Error err = image.LoadPngFromBuffer(imageBytes); // Use LoadJpgFromBuffer if source is JPEG
        
        if (err != Error.Ok)
        {
            throw new Exception("Failed to load image from buffer.");
        }

        // 3. Resize the image
        // Interpolation options: Bilinear, Cubic, or Lanczos (best quality)
        int width = image.GetWidth();
        int height = image.GetHeight();
        
        if (width > max || height > max)
        {
            // Calculate the scale factor to fit the bounds
            float scaleW = (float)max / width;
            float scaleH = (float)max / height;
            float scale = Math.Min(scaleW, scaleH);

            int newWidth = (int)(width * scale);
            int newHeight = (int)(height * scale);

            image.Resize(newWidth, newHeight, Image.Interpolation.Lanczos);
        }

        // 4. Save back to a buffer (PNG is usually safest for quality)
        byte[] resizedBytes = image.SavePngToBuffer();

        // 5. Convert back to Base64
        return Convert.ToBase64String(resizedBytes);
    }
}