using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Landing;

/// <summary>
/// The white over a drop pod's rider's screen (<see cref="Whiteout"/>): over the HUD and the dev panel, kept by Main across a
/// surface switch, so the frame frozen on a planet's opening is white and the planet fades in after it.
/// </summary>
public partial class FlightOverlay : CanvasLayer
{
    private readonly Func<(int Surface, int? TicksLeft)?> _read;
    private readonly Whiteout _white = new();
    private readonly ColorRect _rect = new() { Color = Colors.White, MouseFilter = Control.MouseFilterEnum.Ignore };

    /// <param name="read">The surface this player's hero is on and its own flight's ticks left; null outside a session.</param>
    public FlightOverlay(Func<(int Surface, int? TicksLeft)?> read)
    {
        _read = read;
        Layer = ScreenLayers.Flight;
        _rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_rect);
    }

    public override void _Process(double delta)
    {
        float white = _read() is var (surface, ticksLeft) ? _white.Frame(surface, ticksLeft, delta) : 0;
        _rect.Visible = white > 0;
        _rect.Modulate = new Color(1, 1, 1, white);
    }
}
