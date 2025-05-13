using Godot;
using System;
using System.Buffers.Text;
using System.Text;
using CaptionTool.scripts;
using Godot.Collections;
using Array = Godot.Collections.Array;

public partial class LLMCaptionButton : Button
{
    [Export] private VideoStreamPlayer Player;
    [Export] private TextEdit CaptionBox;
    [Export] private ColorIndicator Indicator;

    private bool busy, requesting; // Prevent concurrent runs

    private HttpClient client = new ();

    public override void _Pressed()
    {
        // Start captioning
        if (busy) return;
        busy = true;
        Disabled = true;
        SetContent("Captioning with LLM, please wait...");

        Stage1();
    }

    public void Stage1()
    {
        try
        {
            string endpoint = GlobalSetting.dictionary["api_endpoint"].GetValue();
            var (baseUrl, port, path) = ApiEndpoint(endpoint);

            client.ConnectToHost(baseUrl, port ?? -1);
            requesting = true;
        }
        catch (Exception e)
        {
            CleanUp();
            SetContent("Error: \n" + e.Message);
            return;
        }
    }

    public void Stage2()
    {
        try
        {
            string endpoint = GlobalSetting.dictionary["api_endpoint"].GetValue();
            var (baseUrl, port, path) = ApiEndpoint(endpoint);
            
            string key = GlobalSetting.dictionary["api_key"].GetValue();
            string model = GlobalSetting.dictionary["api_model"].GetValue();
            string systemPrompt = GlobalSetting.dictionary["system_prompt"].GetValue();
            string image = "data:image/jpeg;base64," + CaptureVideoFrame();
            
            var body = new Dictionary();
            body["model"] = model;
            var messages = new Array();
            {
                var sysMsg = new Dictionary();
                {
                    sysMsg["role"] = "system";
                    sysMsg["content"] = systemPrompt;
                    messages.Add(sysMsg);
                }
                
                var request = new Dictionary();
                {
                    request["role"] = "user";
                    var requestContent = new Array();
                    {
                        var requestImage = new Dictionary();
                        {
                            requestImage["type"] = "image_url";
                            var requestImageUrl = new Dictionary();
                            requestImageUrl["url"] = image;
                            requestImage["image_url"] = requestImageUrl;
                            
                            requestContent.Add(requestImage);
                        }
                        request["content"] = requestContent;
                    }
                    messages.Add(request);
                }
                body["messages"] = messages;
            }
            client.Request(HttpClient.Method.Post, path + "/chat/completions", ["Content-Type: application/json", $"Authorization: Bearer {key}"], Json.Stringify(body));
        }
        catch (Exception e)
        {
            CleanUp();
            SetContent("Error: \n" + e.Message);
            return;
        }
    }

    void SetContent(string content)
    {
        CaptionBox.Text = content;
        Indicator.MarkUnSaved();
    }

    void CleanUp()
    {
        busy = false;
        requesting = false;
        Disabled = false;
        client.Close();
    }

    private RegEx regex = RegEx.CreateFromString("(https?:\\/\\/[\\w\\.]+)(?::)?(\\d+)?(\\/?.*?)\\/?$"); // 3 groups, base url, port (optional), path (no trailing slash)
    public (string, int?, string) ApiEndpoint(string full)
    {
        var matches = regex.Search(full).Strings;
        return (matches[1], int.Parse(matches[2]), matches[3]);
    }

    public override void _PhysicsProcess(double delta) // Fixed rate
    {
        if (requesting)
        {
            client.Poll();
            var status = client.GetStatus();
            switch (status)
            {
                case HttpClient.Status.Connected:
                    Stage2(); // Continue to stage 2
                    break; // Connected
                case HttpClient.Status.Body:
                    if (client.HasResponse())
                    {
                        var chunk = client.ReadResponseBodyChunk();
                        var response = Encoding.UTF8.GetString(chunk);
                        var json = Json.ParseString(response).AsGodotDictionary();
                        if (json.ContainsKey("error"))
                        {
                            SetContent("Error: " + json["error"]);
                        }
                        else
                        {
                            try
                            {
                                string responseText = json["choices"].AsGodotArray()[0].AsGodotDictionary()["message"].AsGodotDictionary()["content"].AsString();
                                SetContent(responseText);
                            }
                            catch (Exception e)
                            {
                                SetContent("Error, could not parse response:\n" + json);
                            }
                        }
                        CleanUp();
                    }
                    break; // Result
                case HttpClient.Status.CantConnect:
                case HttpClient.Status.CantResolve:
                case HttpClient.Status.Disconnected:
                case HttpClient.Status.ConnectionError:
                case HttpClient.Status.TlsHandshakeError:
                    CleanUp();
                    SetContent("Error: " + status);
                    break; // Error
            }
        }
    }

    public string CaptureVideoFrame()
    {
        // Capture a frame from the video
        var bytes = Player.GetViewport().GetTexture().GetImage().GetRegion((Rect2I)Player.GetGlobalRect()).SaveJpgToBuffer();
        var encoded = new byte[Base64.GetMaxEncodedToUtf8Length(bytes.Length)];
        Base64.EncodeToUtf8(bytes, encoded, out int consumed, out int bytesWritten);
        return Encoding.UTF8.GetString(encoded[..bytesWritten]);
    }
}
