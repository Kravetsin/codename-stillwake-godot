using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Landing;
using S = Stillwake.Game.Sound;

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
    /// <summary>The rider's hearing of its flight, on the Suit bus: the hull's rumble rising with the speed, a thump at the white.</summary>
    private readonly AudioStreamPlayer _rumble = Cues.Player(S.PodRumble, S.Suit), _flash = Cues.Player(S.PodFlash, S.Suit),
        _engine = Cues.Player(S.PodThrust, S.Suit);
    private bool _flashed;

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
        AddChild(_rumble);
        AddChild(_flash);
        AddChild(_engine);
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
        Hear(flight, s);
    }

    /// <summary>
    /// The engine from inside from its firing, the hull's rumble from the cut, both louder as it speeds, to the flight's end; the
    /// thump once, on the flight's last tick, the screen white and about to freeze on a planet's opening. A flight ended any other
    /// way stops them.
    /// </summary>
    private void Hear(Flight? flight, double s)
    {
        bool flies = flight != null && s >= FlightTimeline.DipPeak;
        if (flies && !_rumble.Playing) _rumble.Play();
        else if (!flies && _rumble.Playing) _rumble.Stop();
        if (flies) _rumble.VolumeDb = S.PodRumble.VolumeDb + Mathf.LinearToDb(0.3f + 0.7f * (float)FlightTimeline.Fraction(s, FlightTimeline.Length(flight!)));
        // The engine from inside, from the moment it fires (the drop's kick, or the lift-off) to the white, louder with the speed.
        bool burns = flight != null && s >= (flight.Up ? 0 : FlightTimeline.DropFrom);
        if (burns && !_engine.Playing) _engine.Play();
        else if (!burns && _engine.Playing) _engine.Stop();
        if (burns) _engine.VolumeDb = S.PodThrust.VolumeDb - 4 + Mathf.LinearToDb(0.5f + 0.5f * (float)FlightTimeline.Fraction(s, FlightTimeline.Length(flight!)));
        if (flight is { TicksLeft: <= 1 } && !_flashed) _flash.Play();
        _flashed = flight is { TicksLeft: <= 1 } || (_flashed && flight != null);
    }
}
