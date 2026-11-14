using Godot;
using Stillwake.Game.Features.Crew;

namespace Stillwake.Game.Dev;

/// <summary>
/// F8 in any dev run: every helmet turns the next way the code has (<see cref="BodyFit.Trial"/>, helmets-fit-the-suit), then each
/// its own again, named in a corner, to compare them in play.
/// </summary>
public partial class HelmetTurnKey : CanvasLayer
{
    private static readonly HelmetTurn?[] Ways = [null, HelmetTurn.Soft, HelmetTurn.Bolted, HelmetTurn.Ring, HelmetTurn.BoltedStill];
    private readonly Label _name = new() { Position = new Vector2(16, 48), Visible = false };
    private int _way;

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
        if (e is not InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.F8 }) return;
        _way = (_way + 1) % Ways.Length;
        BodyFit.Trial = Ways[_way];
        _name.Text = $"helmets (F8): {Ways[_way] switch
        {
            null => "each its own",
            HelmetTurn.Soft => "soft",
            HelmetTurn.Bolted => "bolted, face turning",
            HelmetTurn.Ring => "ball on a ring",
            _ => "bolted, face still",
        }}";
        _name.Visible = true;
        GetViewport().SetInputAsHandled();
    }

    public override void _ExitTree() => BodyFit.Trial = null;
}
