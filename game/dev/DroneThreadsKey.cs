using Godot;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Dev;

/// <summary>
/// F10 in any run, the plain game too: the drone's faint threads to its windows' corners on or off (<see cref="CornerFrame.Threads"/>,
/// drone-windows), said a moment in a corner, to compare them in play.
/// </summary>
public partial class DroneThreadsKey : CanvasLayer
{
    private readonly Label _name = new() { Position = new Vector2(16, 80), Visible = false };
    private double _shown;

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
        CornerFrame.Threads = !CornerFrame.Threads;
        _name.Text = $"drone threads (F10): {(CornerFrame.Threads ? "on" : "off")}";
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

    public override void _ExitTree() => CornerFrame.Threads = false;
}
