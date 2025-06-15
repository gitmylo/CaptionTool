using System;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

namespace CaptionTool.scripts.graph.Nodes.impl.scripts.llm;

public partial class LlmMessage : Resource
{
    public string role;
    public string message;
    public string image;

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

        if (image != null)
        {
            var imageDict = new Dictionary();
            imageDict["type"] = "image_url";
            var image = new Dictionary();
            image["url"] = "data:image/jpeg;base64," + this.image;
            imageDict["image_url"] = image;
            content.Add(imageDict);
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
            image = image
        };
    }
}