using System;
using System.Linq;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

namespace CaptionTool.scripts.graph.Nodes.impl.scripts.llm;

public partial class LlmMessage : Resource
{
    public string role;
    public string message;
    public string[] images = new string[0];

    public Dictionary ToGDDict()
    {
        var dict = new Dictionary();
        dict["role"] = role;
        var content = new Array();
        if (message != null)
        {
            var messageDict = new Dictionary();
            messageDict["type"] = "text";
            messageDict["text"]  = message;
            content.Add(messageDict);
        }

        if (images != null)
        {
            foreach (var img in images)
            {
                var imageDict = new Dictionary();
                imageDict["type"] = "image_url";
                var image = new Dictionary();
                image["url"] = "data:image/jpeg;base64," + img;
                imageDict["image_url"] = image;
                content.Add(imageDict);
            }
        }
        
        dict["content"] = content;
        return dict;
    }

    public LlmMessage Clone()
    {
        return new LlmMessage
        {
            role = role,
            message = message,
            images = images.ToArray() // clones
        };
    }
}