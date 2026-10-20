using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --ground=drill</c>'s hands (0077): the drill taken out of the hotbar and held on the cell <see cref="GroundDemo"/>
/// names, the pointer moved there and the left button held as a player's would, so the drill is seen digging as played.
/// </summary>
public partial class GroundHands(GroundDemo demo) : Node
{
    private double _since;
    private bool _drawn, _holding;

    public override void _Process(double delta)
    {
        if (demo.Target is not var (x, y))
        {
            _since = 0;
            Hold(false);
            return;
        }
        _since += delta;
        var at = GetViewport().GetCanvasTransform() * (new Vector2((float)x, (float)y) * ShipCamera.Cell);
        Input.ParseInputEvent(new InputEventMouseMotion { Position = at, GlobalPosition = at, ButtonMask = _holding ? MouseButtonMask.Left : 0 });
        if (!_drawn && _since > 0.3)
        {
            _drawn = true;
            Input.ParseInputEvent(new InputEventKey { Keycode = Key.Key2, PhysicalKeycode = Key.Key2, Pressed = true });
            Input.ParseInputEvent(new InputEventKey { Keycode = Key.Key2, PhysicalKeycode = Key.Key2, Pressed = false });
        }
        if (_since > 0.6) Hold(true);
    }

    private void Hold(bool held)
    {
        if (_holding == held) return;
        _holding = held;
        var at = GetViewport().GetMousePosition();
        Input.ParseInputEvent(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Pressed = held,
            ButtonMask = held ? MouseButtonMask.Left : 0,
            Position = at,
            GlobalPosition = at,
        });
    }
}
