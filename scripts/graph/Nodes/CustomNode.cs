using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

namespace CaptionTool.scripts.graph.Nodes;

[GlobalClass]
public partial class CustomNode : GraphNode
{
    [Export] public bool required; // Required nodes are not deletable
    [Export] public GraphManager graph;
    [Export] public ExecutionCore core;
    
    [Export] public Array<Control> controls = new ();

    public override void _Ready()
    {
        // Set slot colors if not set yet
        var typeDict = graph.consts.types.ToDictionary(x => x.id);
        int slotCount = GetChildren().Count(x => x is Control);
        for (int i = 0; i < slotCount; i++)
        {
            if (IsSlotEnabledLeft(i))
            {
                SetSlotColorLeft(i, typeDict[GetSlotTypeLeft(i)].color);
            }
            if (IsSlotEnabledRight(i))
            {
                SetSlotColorRight(i, typeDict[GetSlotTypeRight(i)].color);
            }
        }
    }

    public Array GetControlValues()
    {
        Array outArray = new Array();
        foreach (var control in controls)
        {
            outArray.Add(GetValueFromControl(control).Value);
        }

        return outArray;
    }

    // Used to get the values from the controls for the execution cores to use
    public Variant? GetValueFromControl(Control control)
    {
        switch (control)
        {
            case TextEdit textEdit:
                return textEdit.Text;
            case LineEdit lineEdit:
                return lineEdit.Text;
            case OptionButton optionButton:
                return optionButton.GetItemText(optionButton.GetSelectedId());
            case Button button:
                return button.IsPressed();
            case Slider slider:
                return slider.Value;
            case SpinBox spinBox:
                return spinBox.Value;
        }
        return null;
    }
}