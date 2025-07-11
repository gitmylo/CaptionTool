﻿using Godot;
using Godot.Collections;

namespace CaptionTool.scripts.graph.Nodes;

[GlobalClass]
public partial class NodeConsts : Resource
{
    [Export] public Array<NodeType> types = new();
    [Export] public Array<NodeDef> nodes = new();
    [Export] public NodeDef inputNodeDef;
    [Export] public NodeDef outputNodeDef;
    [Export] public int AnyType;
}