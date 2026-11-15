using Godot;

namespace Stillwake.Game.Dev;

/// <summary>F9 in a dev room on a planet: a gust of wind now, not after a calm (plant-stirs), said a moment in a corner.</summary>
public partial class GustKey : CanvasLayer
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
        if (e is not InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.F9 }) return;
        Features.Flora.Gusts.Asked = true;
        _name.Text = "wind: a gust";
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
}
