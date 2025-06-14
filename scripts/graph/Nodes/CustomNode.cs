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
    [Export] public NodeDef nodeDef;
    
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

    public void SetValues(Dictionary<StringName, Variant> values)
    {
        foreach (var control in controls)
        {
            if (values.ContainsKey(control.Name))
            {
                SetValueToControl(control,  values[control.Name], true);
            }
        }
    }

    public Dictionary<StringName, Variant> GetValues()
    {
        var dict = new Dictionary<StringName, Variant>();

        foreach (var control in controls)
        {
            if (control == null) continue;
            dict[control.Name] = GetValueFromControl(control, true);
        }
        
        return dict;
    }

    public Array GetControlValues()
    {
        Array outArray = new Array();
        foreach (var control in controls)
        {
            outArray.Add(GetValueFromControl(control));
        }

        return outArray;
    }

    // Used to get the values from the controls for the execution cores to use
    public Variant GetValueFromControl(Control control, bool forSaving = false)
    {
        switch (control)
        {
            case TextEdit textEdit:
                return textEdit.Text;
            case LineEdit lineEdit:
                return lineEdit.Text;
            case OptionButton optionButton:
                return forSaving ? optionButton.Selected : optionButton.GetItemText(optionButton.GetSelectedId());
            case CheckBox button:
                return button.IsPressed();
            case Slider slider:
                return slider.Value;
            case SpinBox spinBox:
                return spinBox.Value;
        }
        throw new NotImplementedException();
    }

    public void SetValueToControl(Control control, Variant value, bool forLoading = false)
    {
        switch (control)
        {
            case TextEdit textEdit:
                textEdit.Text = value.AsString();
                break;
            case LineEdit lineEdit:
                lineEdit.Text = value.AsString();
                break;
            case OptionButton optionButton:
                if (forLoading) optionButton.Selected = value.AsInt32();
                else optionButton.Select(value.AsInt32());
                break;
            case CheckBox button:
                button.SetPressed(value.AsBool());
                break;
            case Slider slider:
                slider.Value = value.AsDouble();
                break;
            case SpinBox spinBox:
                spinBox.Value = value.AsDouble();
                break;
        }
    }
}