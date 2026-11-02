using Godot;

namespace Stillwake.Game.Dev;

/// <summary>A script's hands on the research tablet, through Godot's own input, so a recording plays what a player's hands do.</summary>
public sealed class TabletHands(Node owner)
{
    /// <summary>Where the pointer was last put.</summary>
    public Vector2 Last { get; private set; }

    public void Point(Vector2 at)
    {
        owner.GetViewport().WarpMouse(at);
        Move(at, null);
    }

    public void Move(Vector2 at, MouseButton? held)
    {
        Input.ParseInputEvent(new InputEventMouseMotion
        {
            Position = at,
            GlobalPosition = at,
            Relative = at - Last,
            ButtonMask = held == null ? 0 : MouseButtonMask.Left,
        });
        Last = at;
    }

    public void Mouse(Vector2 at, MouseButton button, bool pressed)
    {
        Last = at;
        owner.GetViewport().WarpMouse(at);
        Input.ParseInputEvent(new InputEventMouseButton { Position = at, GlobalPosition = at, ButtonIndex = button, Pressed = pressed, ButtonMask = pressed ? MouseButtonMask.Left : 0 });
    }

    public void Wheel(Vector2 at, MouseButton button, int notches)
    {
        owner.GetViewport().WarpMouse(at);
        for (int i = 0; i < notches; i++)
        {
            Input.ParseInputEvent(new InputEventMouseButton { Position = at, GlobalPosition = at, ButtonIndex = button, Pressed = true });
            Input.ParseInputEvent(new InputEventMouseButton { Position = at, GlobalPosition = at, ButtonIndex = button, Pressed = false });
        }
    }

    public static void Press(Key key)
    {
        Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = true });
        Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = false });
    }
}
