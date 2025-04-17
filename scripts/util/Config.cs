using Newtonsoft.Json;

namespace CaptionTool.scripts.util;

[JsonObject]
public class Config
{
    [JsonProperty] public int fps = 16;
    [JsonProperty] public bool saveTxt = true;
    [JsonProperty] public string inDir = "raw";
    [JsonProperty] public string procDir = "processing";
    [JsonProperty] public string outDir = "captioned";
}