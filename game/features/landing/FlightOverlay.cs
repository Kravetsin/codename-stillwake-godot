using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Landing;

namespace Stillwake.Game.Features.Landing;

/// <summary>
/// What covers a drop pod's rider's screen while it flies, kept by Main across a surface switch, over the HUD and the dev panel:
/// from the bottom, the flight itself (<see cref="FlightScreen"/>), the dip to black that cuts to it from the world, and the white
/// (<see cref="Whiteout"/>), so the frame frozen on a planet's opening is white and the planet fades in after it.
/// </summary>
public partial class FlightOverlay : CanvasLayer
{
    private readonly Func<(int Surface, Flight? Own)?> _read;
    private readonly Func<double> _progress;
    private readonly Func<float> _zoom;
    private readonly Whiteout _white = new();
    private readonly FlightScreen _screen = new() { Visible = false };
    private readonly ColorRect _dip = new() { Color = Colors.Black, MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
    private readonly ColorRect _rect = new() { Color = Colors.White, MouseFilter = Control.MouseFilterEnum.Ignore };

    /// <param name="read">The surface this player's hero is on and its own flight; null outside a session.</param>
    /// <param name="progress">The share of the next tick drawn, so the pictures move between ticks.</param>
    /// <param name="zoom">The world's camera zoom: the flight is drawn at the world's scale.</param>
    public FlightOverlay(Func<(int Surface, Flight? Own)?> read, Func<double> progress, Func<float> zoom)
    {
        (_read, _progress, _zoom) = (read, progress, zoom);
        Layer = ScreenLayers.Flight;
        foreach (var rect in new ColorRect[] { _dip, _rect }) rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_screen);
        AddChild(_dip);
        AddChild(_rect);
    }

    public override void _Process(double delta)
    {
        var read = _read();
        float white = read is var (surface, own) ? _white.Frame(surface, own?.TicksLeft, delta) : 0;
        _rect.Visible = white > 0;
        _rect.Modulate = new Color(1, 1, 1, white);
        var flight = read?.Own;
        double s = flight is { } f ? FlightTimeline.Seconds(f, _progress()) : 0;
        // The screen shows from the dip's darkest moment to the flight's end, under the white.
        _screen.Visible = flight != null && s >= FlightTimeline.DipPeak;
        if (_screen.Visible) _screen.Show(s, FlightTimeline.Length(flight!), flight!.Up, _zoom());
        float dip = flight != null ? (float)FlightTimeline.Dip(s) : 0;
        _dip.Visible = dip > 0;
        _dip.Modulate = new Color(1, 1, 1, dip);
    }
}
