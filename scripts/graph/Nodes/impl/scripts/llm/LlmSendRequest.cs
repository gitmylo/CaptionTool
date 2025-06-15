using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;
using HttpClient = Godot.HttpClient;

namespace CaptionTool.scripts.graph.Nodes.impl.scripts.llm;

[GlobalClass]
public partial class LlmSendRequest : ExecutionCore
{
    public override async Task<Array<Array>> Execute(Array<Array> inputs, NodeExecutionContext context, Array values)
    {
        var chat = inputs[0].FromUGdArray<LlmMessage>().Select(x => x.ToGDDict()).ToGdArray();

        var requestBody = new Dictionary();
        string key = GlobalSetting.dictionary["api_key"].GetValue();
        string model = GlobalSetting.dictionary["api_model"].GetValue();
        string endpoint = GlobalSetting.dictionary["api_endpoint"].GetValue();
        string fullendpoint = endpoint.TrimSuffix("/") + "/chat/completions";
        
        requestBody["model"] = model;
        requestBody["messages"] = chat;
        var bodyJson = Json.Stringify(requestBody);

        string output;
        using (var client = new System.Net.Http.HttpClient())
        {
            client.Timeout = TimeSpan.FromMinutes(15); // 15 minutes for 1 request max
            var request = new HttpRequestMessage(HttpMethod.Post, fullendpoint)
            {
                Headers = { Authorization = AuthenticationHeaderValue.Parse($"Bearer {key}")},
                Content = new StringContent(bodyJson, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"))
            };
            
            var post = await client.SendAsync(request);
            post.EnsureSuccessStatusCode();
            var result = await post.Content.ReadAsStringAsync();
            var resultDict = Json.ParseString(result).AsGodotDictionary();
            if (resultDict.ContainsKey("error"))
            {
                throw new Exception($"LLM Api error: {resultDict["error"]}");
            }
            else
            {
                try
                {
                    string responseText = resultDict["choices"].AsGodotArray()[0].AsGodotDictionary()["message"].AsGodotDictionary()["content"].AsString();
                    output = responseText;
                }
                catch (Exception e)
                {
                    throw new Exception("Failed to parse llm response, api doesn't follow output format or failed to parse for anothe reason.");
                }
            }
        }

        return Results(Inner(output));
    }
}