using Godot;
using System;

public partial class RelatedPopup : PopupMenu
{
    [Export] private Node showPlace;
    [Export] private Node hidePlace;
    [Export] private TabContainer tabs;
    [Export] private long validTab;

    public override void _Ready()
    {
        var desiredParent = tabs.CurrentTab == validTab ? showPlace : hidePlace;
        if (GetParent() != desiredParent) Reparent(desiredParent);
        
        tabs.TabChanged += tab =>
        {
            var desiredParent = tabs.CurrentTab == validTab ? showPlace : hidePlace;
            if (GetParent() != desiredParent) Reparent(desiredParent);
        };
    }
}
