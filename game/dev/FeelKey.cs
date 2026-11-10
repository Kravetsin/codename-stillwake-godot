using Godot;

namespace Stillwake.Game.Dev;

/// <summary>
/// F8 in a dev room on a planet: the next of <see cref="FloraLooks.Feels"/>, how grass answers a body (plant-stirs), its name
/// shown a moment in a corner, so the user compares them in play and picks one.
/// </summary>
public partial class FeelKey : CanvasLayer
{
    private readonly Label _name = new() { Position = new Vector2(16, 16), Visible = false };
    private double _shown;

    public override void _Ready()
    {
        Layer = 100;
        _name.AddThemeFontSizeOverride("font_size", 22);
        _name.AddThemeColorOverride("font_outline_color", Colors.Black);
        _name.AddThemeConstantOverride("outline_size", 6);
        AddChild(_name);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.F8 }) return;
        int next = (IndexOf(FloraLooks.Feeling) + 1) % FloraLooks.Feels.Count;
        FloraLooks.Feeling = FloraLooks.Feels[next];
        _name.Text = $"grass: {FloraLooks.Feeling.Name} ({next + 1}/{FloraLooks.Feels.Count})";
        _name.Visible = true;
        _shown = 2.5;
        GetViewport().SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        if (_shown <= 0) return;
        _shown -= delta;
        if (_shown <= 0) _name.Visible = false;
    }

    private static int IndexOf(FloraLooks.Feel feel)
    {
        for (int i = 0; i < FloraLooks.Feels.Count; i++)
            if (FloraLooks.Feels[i] == feel) return i;
        return 0;
    }
}
