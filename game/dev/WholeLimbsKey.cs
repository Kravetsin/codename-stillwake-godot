using Godot;
using Stillwake.Game.Features.Crew;

namespace Stillwake.Game.Dev;

/// <summary>
/// F10 in any run, the plain game too (whole-limbs, a try): every figure's arms and legs drawn from one picture bent at the joint, or
/// from two meeting at a seam as before, named in a corner, to compare them in play.
/// </summary>
public partial class WholeLimbsKey : CanvasLayer
{
    private readonly Label _name = new() { Position = new Vector2(16, 72), Visible = false };

    public override void _Ready()
    {
        Layer = 100;
        _name.AddThemeFontSizeOverride("font_size", 16);
        _name.AddThemeColorOverride("font_outline_color", Colors.Black);
        _name.AddThemeConstantOverride("outline_size", 5);
        AddChild(_name);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.F10 }) return;
        Figure.WholeLimbs = !Figure.WholeLimbs;
        _name.Text = $"limbs (F10): {(Figure.WholeLimbs ? "one picture" : "two pictures")}";
        _name.Visible = true;
        GetViewport().SetInputAsHandled();
    }
}
