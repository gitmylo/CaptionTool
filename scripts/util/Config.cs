using System;
using Godot;
using Newtonsoft.Json;

namespace CaptionTool.scripts.util;

[JsonObject]
public class Config
{
    [JsonProperty] public int fps = 16;
    [JsonConverter(typeof(SaveTxtBoolToNewConverter))]
    [JsonProperty] public int saveTxt = 0;
    [JsonProperty] public string inDir = "raw";
    [JsonProperty] public string procDir = "processing";
    [JsonProperty] public string outDir = "captioned";
}

// A converter that allows loading old configs where saveTxt was a bool, and new configs where savetxt is an int
public class SaveTxtBoolToNewConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        writer.WriteValue((int)value);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.ValueType == typeof(bool))
        {
            return (reader.Value.ToString() == "true") ? 0 : 1;
        }
        else
        {
            return (int)reader.Value;
        }
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(bool)
                || objectType == typeof(int);
    }
}