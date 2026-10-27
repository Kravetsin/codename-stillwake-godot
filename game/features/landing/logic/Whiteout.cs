using System;

namespace Stillwake.Game.Features.Landing;

/// <summary>
/// How white a drop pod's rider's screen is (the descent design, slice 1): rising over its flight's last <see cref="FadeTicks"/>,
/// full on its last tick, when the frame freezes on a planet's opening; falling over <see cref="FadeOutS"/> once its hero is on
/// the surface it flew to. A flight that ends any other way, docked home or gone with a load, leaves it clear at once.
/// </summary>
public sealed class Whiteout
{
    /// <summary>Ticks of a flight's end over which the white rises: a second.</summary>
    public const int FadeTicks = 20;

    /// <summary>Seconds over which it falls once the hero has arrived.</summary>
    public const double FadeOutS = 0.5;

    private int? _surface;
    private double? _sinceLanded;

    /// <summary>How white the screen is, 0 to 1, as of the last <see cref="Frame"/>.</summary>
    public float Opacity { get; private set; }

    /// <summary>
    /// The white of a frame <paramref name="deltaS"/> seconds after the last, its hero on <paramref name="surface"/>, its own flight
    /// <paramref name="ticksLeft"/> from landing (null for none): it falls from full only when the surface changed while white.
    /// </summary>
    public float Frame(int surface, int? ticksLeft, double deltaS)
    {
        if (_surface is int was && was != surface && Opacity > 0) _sinceLanded = 0;
        else if (_sinceLanded is double since) _sinceLanded = since + deltaS < FadeOutS ? since + deltaS : null;
        _surface = surface;
        float rising = ticksLeft is int t ? Math.Clamp((FadeTicks - t + 1) / (float)FadeTicks, 0, 1) : 0;
        float falling = _sinceLanded is double s ? (float)(1 - s / FadeOutS) : 0;
        return Opacity = Math.Max(rising, falling);
    }
}
