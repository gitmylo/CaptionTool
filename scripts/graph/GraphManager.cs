using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CaptionTool.scripts.graph.Nodes;
using CaptionTool.scripts.graph.Nodes.impl.scripts;
using CaptionTool.scripts.ui;
using CaptionTool.scripts.util;
using Godot;
using Godot.Collections;

namespace CaptionTool.scripts.graph;

public partial class GraphManager: GraphEdit
{
    [Export] public NodeConsts consts;
    
    [Export] private CustomNode InputNode;
    [Export] private CustomNode OutputNode;

    [Export] private NewUI ui;
    
    [ExportCategory("Popup")]
    [Export] private Popup popup;
    [Export] private LineEdit popupSearchBox;
    [Export] private VBoxContainer availableNodes; // Contains NodeButton entries
    [Export] private PackedScene nodeButtonScene;
    [Export] private AcceptDialog errorDialog;

    [Export] private SaveWorkflowPopup saveDialog;

    [ExportCategory("UI components")]
    [Export] private Button testFileButton;
    [Export] private Button processAllButton;
    [Export] private ScrollContainer scroll;
    
    [Export] private PopupMenu popupMenu;
    [Export] private Label currentWorkflowLabel;

    [Export] private Button refreshButton;
    [Export] private VBoxContainer savedGraphList;
    [Export] private PackedScene workflowListItemScene;

    [ExportCategory("Progress")]
    [Export] private ProgressBar globalProgressBar;
    [Export] private VBoxContainer progressItemsList;
    [Export] private Button clearCompletedButton;
    [Export] private Button cancelRunningButton;
    [Export] private SpinBox maxThreadsBox;
    [Export] private PackedScene progressItemScene;

    private Vector2 lastPopup;
    private string loadedWorkflow;

    public override void _Ready()
    {
        InitConnection();
        InitNodeButtons();

        InputNode.nodeDef = consts.inputNodeDef;
        OutputNode.nodeDef = consts.outputNodeDef;

        foreach (var type in consts.types)
        {
            AddValidConnectionType(type.id, consts.AnyType);
            AddValidConnectionType(consts.AnyType, type.id);
        }
        
        ConnectionRequest += LinkConnectionRequest;
        DisconnectionRequest += LinkDisconnectionRequest;
        DeleteNodesRequest += LinkDeleteNodesRequest;
        PopupRequest += p =>
        {
            lastPopup = p;
            scroll.ScrollHorizontal = 0;
            popup.Popup();
            popup.SetPosition((Vector2I)(GetGlobalMousePosition() + (Vector2.Left * (popup.Size.X/2)) + (Vector2.Up * 15)));
            popupSearchBox.Text = "";
            UpdateSearch("");
            popupSearchBox.GrabFocus();
        };
        popupSearchBox.TextChanged += UpdateSearch;

        testFileButton.Pressed += () =>
        {
            ExecuteGraphForInput(ui.activeVid);
        };
        processAllButton.Pressed += () =>
        {
            RunTrackers(ui.GetAllFilePathsRecursive(ui.sourceDir).ToArray());
        };

        saveDialog.manager = this;

        popupMenu.IdPressed += id =>
        {
            switch (id)
            {
                case 0: // Save
                    if (loadedWorkflow != null) SaveGraph(loadedWorkflow);
                    else saveDialog.Show();
                    break;
                case 1: // Save as
                    saveDialog.Show();
                    break;
                case 2: // Refresh saved
                    RefreshSavedList();
                    break;
            }
        };

        refreshButton.Pressed += RefreshSavedList;
        
        RefreshSavedList();

        clearCompletedButton.Pressed += () =>
        {
            ClearCompletedProgressNodes();
        };

        cancelRunningButton.Pressed += () =>
        {
            CancelAllProgressNodes();
        };
    }

    public override void _Process(double delta)
    {
        var list = progressItemsList.GetChildren().Where(x => x is ProgressItem).Cast<ProgressItem>().ToArray();
        if (list.Length == 0)
        {
            globalProgressBar.Value = 0;
            return;
        }
        var completed = list.Count(x => x.Completed());
        globalProgressBar.Value = (float)completed / list.Length;
    }

    private void UpdateSearch(string query)
    {
        var allNodes = availableNodes.GetChildren().Select(x => x as NodeButton).OfType<NodeButton>().ToList();
        foreach (var node in allNodes)
        {
            node.Visible = node.name.ToLower().Contains(query.ToLower()) || node.description.ToLower().Contains(query.ToLower());
        }
    }

    // Signal receivers
    private void LinkConnectionRequest(StringName fromNode, long fromPort,  StringName toNode, long toPort)
    {
        foreach (var connection in Connections.Where(x => x["to_node"].AsStringName() == toNode && x["to_port"].AsInt64() == toPort))
        {
            DisconnectNode(connection["from_node"].AsStringName(), connection["from_port"].AsInt32(), connection["to_node"].AsStringName(),  connection["to_port"].AsInt32());
        }
        ConnectNode(fromNode, (int)fromPort, toNode, (int)toPort);
    }

    private void LinkDisconnectionRequest(StringName fromNode, long fromPort, StringName toNode, long toPort)
    {
        DisconnectNode(fromNode, (int)fromPort, toNode, (int)toPort);
    }
    
    private void LinkDeleteNodesRequest(Array<StringName> nodes)
    {
        foreach (var node in GetNodes().FindAll(x => nodes.Contains(x.Name) && !x.required))
        {
            node.QueueFree();
        }
    }
    
    // Other

    void InitNodeButtons()
    {
        foreach (var node in consts.nodes)
        {
            CreateNodeButton(node.name, node.description, node.node, node);
        }
    }
    
    void CreateNodeButton(string name, string description, PackedScene node, NodeDef def)
    {
        var button = nodeButtonScene.Instantiate<NodeButton>();
        button.name = name;
        button.description = description;
        button.node = node;
        button.graph = this;
        button.def = def;
        button.InitReady();
        availableNodes.AddChild(button);
    }

    public CustomNode AddNode(NodeDef def)
    {
        var nodeInst = def.node.Instantiate<CustomNode>();
        nodeInst.PositionOffset = lastPopup + ScrollOffset;
        nodeInst.graph = this;
        nodeInst.nodeDef = def;
        AddChild(nodeInst);
        
        popup.Visible = false;
        return nodeInst;
    }

    void InitConnection()
    {
        ConnectNode(InputNode.Name, 1, OutputNode.Name, 0);
    }

    List<CustomNode> GetNodes()
    {
        return GetChildren().Where(x => x is CustomNode).Cast<CustomNode>().ToList();
    }

    public void ExecuteGraphForInput(string target)
    {
        RunTrackers([target]);
    }

    public void RunTrackers(string[] targets)
    {
        var trackers = targets.Select(PrepareTracker).ToArray();
        if (trackers.Any(x => x == null))
        {
            // Something went wrong, likely an ignored input, return.
            return;
        }
        new Thread(() =>
        {
            Parallel.ForEach(trackers, new ParallelOptions { MaxDegreeOfParallelism = (int)maxThreadsBox.Value }, x =>
            {
                try
                {
                    x.Execute();
                    var result = x.tree.output.context.captionsOut;
                    ui.SaveCaptionFor(x.tree.context.fileName, result);
                }
                catch (Exception e)
                {
                    x.errorMessage = e.Message;
                    x.error = e;
                }
            });
        }).Start();
    }

    public ExecutionTracker PrepareTracker(string target)
    {
        var inCaptions = ui.CaptionsForVideo(target);
        var graph = BuildGraph(target, inCaptions); // Graph is completely disconnected from godot nodes, only uses resources and is safe to use on a new thread.
        if (graph == null) return null;
        
        var tracker = ExecutionTracker.FromTree(graph);

        var trackerNode = progressItemScene.Instantiate<ProgressItem>();
        trackerNode.Init(tracker, target);
        progressItemsList.AddChild(trackerNode);
        
        return tracker;
    }

    public ExecutionTree BuildGraph(string fileName, SaveableCaption[] captions)
    {
        var context = new NodeExecutionContext {fileName = fileName, captionsIn = captions};
        var tree = new ExecutionTree {context = context};
        var nodesByName = GetNodes().ToDictionary(x => x.Name);
        System.Collections.Generic.Dictionary<(StringName, int), (StringName, int)> valueSources = new ();
        foreach (var connection in Connections)
        {
            var fromNode = connection["from_node"].AsStringName();
            var fromPort = connection["from_port"].AsInt32();
            var toNode = connection["to_node"].AsStringName();
            var toPort = connection["to_port"].AsInt32();
            
            valueSources[(toNode, toPort)] = (fromNode, fromPort);
        }

        var outputNode = FindConnections(OutputNode.Name, nodesByName, new (), valueSources);
        if (outputNode == null) return null;
        
        tree.output = outputNode;
        
        return tree;
    }

    public ExecutionNode FindConnections(StringName originNode, System.Collections.Generic.Dictionary<StringName, CustomNode> nodesByName, System.Collections.Generic.Dictionary<StringName, ExecutionNode> nodeHistory, System.Collections.Generic.Dictionary<(StringName, int), (StringName, int)> valueSources)
    {
        var node = nodesByName[originNode];
        var inputCount = node.GetInputPortCount();
        var thisNode = new ExecutionNode {node = node.core, uiValues = node.GetControlValues()};
        for (int i = 0; i < inputCount; i++)
        {
            // If the connection doesn't exist, show an error indicating that a connection is missing
            if (!valueSources.ContainsKey((node.Name, i)))
            {
                errorDialog.DialogText = $"A node ({originNode}) is missing an input, please connect this node.";
                errorDialog.PopupCentered();
                return null;
            }
            
            var source = valueSources[(node.Name, i)];
            ExecutionNode sourceNode;
            if (nodeHistory.ContainsKey(source.Item1))
            {
                sourceNode = nodeHistory[source.Item1];
            }
            else
            {
                sourceNode = FindConnections(source.Item1, nodesByName, nodeHistory, valueSources); // Recursively trace back the node graph
                if (sourceNode == null) return null; // Error is infectious
            }
            thisNode.inputConnections.Add((sourceNode, source.Item2, i));
        }

        return thisNode;
    }

    private string WorkflowSavePath(string fileName, bool withUser = true)
    {
        DirAccess.Open("user://").MakeDirRecursive("workflows/" + fileName.GetBaseDir());
        return (withUser ? "user://workflows/" : "workflows/") + fileName;
    }

    public void SaveGraph(string fileName)
    {
        var path = WorkflowSavePath(fileName);
        var nodes = GetNodes();
        Godot.Collections.Dictionary<string, Variant> saveable = new ();
        Array<Godot.Collections.Dictionary<string, Variant>> nodeValues = new ();
        foreach (var node in nodes)
        {
            Godot.Collections.Dictionary<string, Variant> value = new();
            value["id"] = node.nodeDef.identifier;
            value["name"] = node.Name;
            value["position"] = node.PositionOffset;
            value["size"] = node.Size;
            value["fieldValues"] = node.GetValues();
            nodeValues.Add(value);
        }
        
        saveable["nodes"] = nodeValues;
        saveable["connections"] = Connections;

        var serialized = Json.Stringify(saveable);
        using (var file = FileAccess.Open(path, FileAccess.ModeFlags.Write))
        {
            file.StoreString(serialized);
        }
        RefreshSavedList();
        SelectNewWorkflow(fileName);
    }

    public void LoadGraph(string fileName)
    {
        var path = WorkflowSavePath(fileName);

        string serialized;
        using (var file = FileAccess.Open(path, FileAccess.ModeFlags.Read))
        {
            serialized = file.GetAsText();
        }

        var loadable = Json.ParseString(serialized).AsGodotDictionary();
        var nodeValues = loadable["nodes"].AsGodotArray<Godot.Collections.Dictionary<string, Variant>>();
        
        ClearConnections();
        // Clear nodes from graph (excluding input/output)
        var oldNodes = GetNodes();
        foreach (var oldNode in oldNodes)
        {
            oldNode.Name = oldNode.Name + "_DELETING";
            oldNode.QueueFree();
        }

        foreach (var nodeValue in nodeValues)
        {
            string id = nodeValue["id"].AsString();
            StringName name = nodeValue["name"].AsStringName();
            var posTest = nodeValue["position"].AsString().TrimPrefix("(").TrimSuffix(")").Split(", ").Select(x => double.Parse(x, CultureInfo.InvariantCulture)).ToArray();
            var position = new Vector2((float)posTest[0], (float)posTest[1]);

            var fieldValues = nodeValue["fieldValues"].AsGodotDictionary<StringName, Variant>();

            var def = LookupNode(id);
            var node = AddNode(def);
            if (node.nodeDef.identifier == "output") OutputNode = node;
            if (node.nodeDef.identifier == "input") InputNode = node;

            node.Name = name;
            node.PositionOffset = position;
            if (nodeValue.ContainsKey("size"))
            {
                var sizeTest = nodeValue["size"].AsString().TrimPrefix("(").TrimSuffix(")").Split(", ").Select(x => double.Parse(x, CultureInfo.InvariantCulture)).ToArray();
                var size = new Vector2((float)sizeTest[0], (float)sizeTest[1]);
                node.Size = size;
            }

            node.SetValues(fieldValues);
        }
        
        Connections = loadable["connections"].AsGodotArray<Dictionary>(); // Link the nodes
        
        SelectNewWorkflow(fileName);
    }

    public void RenameGraph(string oldName, string newName)
    {
        var oldLoc = WorkflowSavePath(oldName, true);
        var newLoc = WorkflowSavePath(newName, true);

        bool wasLoaded = oldName == loadedWorkflow;
        
        string loaded;
        using (var f = FileAccess.Open(oldLoc, FileAccess.ModeFlags.Read))
        {
            loaded = f.GetAsText();
        }
        DeleteGraph(oldName);
        using (var f = FileAccess.Open(newLoc, FileAccess.ModeFlags.Write))
        {
            f.StoreString(loaded);
        }
        RefreshSavedList();
        if (wasLoaded) SelectNewWorkflow(newName);
    }

    public void DeleteGraph(string name)
    {
        var loc  = WorkflowSavePath(name, false);
        using (var access = DirAccess.Open("user://"))
        {
            access.Remove(loc);
        }
        RefreshSavedList();
        if (name == loadedWorkflow) SelectNewWorkflow(null);
    }

    public void RefreshSavedList()
    {
        var children = savedGraphList.GetChildren();
        foreach (var child in children)
        {
            child.QueueFree();
        }
        
        string[] files;
        using (var dir = DirAccess.Open("user://workflows"))
        {
            files = dir.GetFiles();
        }
        foreach (var file in files)
        {
            var item = workflowListItemScene.Instantiate<WorkflowListItem>();
            item.manager = this;
            item.relatedWorkflow = file;
            savedGraphList.AddChild(item);
        }
    }

    public void SelectNewWorkflow(string name)
    {
        loadedWorkflow = name;
        if (name == null)
        {
            currentWorkflowLabel.Text = "No workflow selected";
        }
        else
        {
            currentWorkflowLabel.Text = name;
        }
    }

    public NodeDef LookupNode(string identifier)
    {
        switch (identifier)
        {
            case "input":
                return consts.inputNodeDef;
            case "output":
                return consts.outputNodeDef;
        }
        
        return consts.nodes.FirstOrDefault(x => x.identifier == identifier);
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventKey eventKey && eventKey.Pressed)
        {
            var mods = eventKey.GetModifiersMask();
            if (eventKey.Keycode == Key.S && (mods & KeyModifierMask.MaskCtrl) != 0)
            {
                if ((mods & KeyModifierMask.MaskShift) != 0)
                {
                    saveDialog.Show();
                }
                else
                {
                    if (loadedWorkflow != null) SaveGraph(loadedWorkflow);
                    else saveDialog.Show();
                }
            }

            if (eventKey.Keycode == Key.R && (mods & KeyModifierMask.MaskCtrl) != 0)
            {
                RefreshSavedList();
            }
        }
    }

    public void ClearCompletedProgressNodes()
    {
        foreach (var child in progressItemsList.GetChildren().Where(x => x is ProgressItem).Cast<ProgressItem>())
        {
            if (child.Completed())
            {
                child.QueueFree();
            }
        }
    }

    public void CancelAllProgressNodes()
    {
        foreach (var child in progressItemsList.GetChildren().Where(x => x is ProgressItem).Cast<ProgressItem>())
        {
            child.Cancel();
        }
    }
}