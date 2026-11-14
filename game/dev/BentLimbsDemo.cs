using Godot;
using Stillwake.Game.Features.Crew;

namespace Stillwake.Game.Dev;

/// <summary>
/// F7 in <c>--bent-limbs</c> (exp/bent-limbs): every figure's arms and legs drawn rigid at the joint or bent as one piece
/// (<see cref="Figure.BentLimbs"/>), the way shown in a corner.
/// </summary>
public partial class BentLimbsDemo : CanvasLayer
{
    private readonly Label _name = new() { Position = new Vector2(16, 16) };

    public override void _Ready()
    {
        Layer = 100;
        _name.AddThemeFontSizeOverride("font_size", 22);
        _name.AddThemeColorOverride("font_outline_color", Colors.Black);
        _name.AddThemeConstantOverride("outline_size", 6);
        AddChild(_name);
        Say();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.F7 }) return;
        Figure.BentLimbs = !Figure.BentLimbs;
        Say();
        GetViewport().SetInputAsHandled();
    }

    private void Say() => _name.Text = $"limbs: {(Figure.BentLimbs ? "bent" : "rigid")} (F7)";
}
