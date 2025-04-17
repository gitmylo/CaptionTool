using Godot;
using System;

[Tool]
[GlobalClass]
public partial class MinMaxSlider : Control
{
    private int dragState = 0; // 0 = none, 1 = a, 2 = b
    
    [Export] private Color background = Colors.DarkGray, activeBackground = Colors.LightGray, notches = Colors.White, hoverActiveBackground = Colors.LightGray, hoverNotches = Colors.White;

    [Export] private float inset;
    
    [Export] public double a = 0;
    [Export] public double b = 1;
    
    private (double, double) last = (0, 1);

    public event Action<(double, double)> ValueChanged;

    public override void _Process(double delta)
    {
        QueueRedraw();

        if (last.Item1 != a || last.Item2 != b)
        {
            last = (a, b);
            ValueChanged?.Invoke(last);
        }

        if (dragState != 0)
        {
            double dragValue = Mathf.Clamp(GetLocalMousePosition().X / Size.X, 0, 1);
            if (dragState == 1) // a
            {
                a = dragValue;
            }

            if (dragState == 2) // b
            {
                b = dragValue;
            }
        }
    }

    public void SetValuesNoEvent(double a, double b)
    {
        last = (a, b);
        this.a = a;
        this.b = b;
    }

    public ref double min
    {
        get
        {
            if (a < b) return ref a;
            return ref b;
        }
    }
    public ref double max
    {
        get
        {
            if (a > b) return ref a;
            return ref b;
        }
    }

    public override Vector2 _GetMinimumSize()
    {
        return new Vector2(50, 20);
    }

    public override void _Draw()
    {
        bool hovering = HasFocus() || GetGlobalRect().HasPoint(GetGlobalMousePosition());
        float startX = (float)(Position.X + (min * Size.X));
        float endX = (float)(Position.X + (max * Size.X));
        Vector2 offsetY = Vector2.Down * Size.Y; // Jump down from offset up
        Vector2 start = new Vector2(startX, 0);
        Vector2 end = new Vector2(endX, 0);
        Vector2 inset = new Vector2(0, this.inset);
        
        DrawRect(new Rect2(Vector2.Zero, GetSize()), background);
        DrawRect(new Rect2(start + inset, end-start + offsetY - (inset*2)), hovering ? hoverActiveBackground : activeBackground);

        DrawLine(start, start + offsetY, hovering ? hoverNotches : notches, 5);
        DrawLine(end, end + offsetY, hovering ? hoverNotches : notches, 5);
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (Engine.IsEditorHint()) return;

        if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.IsPressed())
        {
            double val = GetLocalMousePosition().X / Size.X;
            if (Math.Abs(val - a) < Math.Abs(val - b))
            {
                dragState = 1;
            }
            else
            {
                dragState = 2;
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.IsReleased())
        {
            dragState = 0;
        }
    }
}
