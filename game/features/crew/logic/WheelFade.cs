using System;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A wheel's coming and going (the thread icons' pick «Одна нить»): it comes in <see cref="ThreadIcon.WheelIn"/>; let go, the side it
/// was pointed at flares where it was for <see cref="ThreadIcon.FlareS"/> as the rest goes in <see cref="ThreadIcon.WheelOut"/>.
/// Client only.
/// </summary>
public sealed class WheelFade
{
    private double _for = double.PositiveInfinity;
    private int? _toward;

    /// <summary>Whether it is open.</summary>
    public bool Open { get; private set; }

    /// <summary>The side let go on, flaring; none while open or let go in the middle.</summary>
    public int? Picked { get; private set; }

    /// <summary>How much of the wheel shows, 0..1, but the side picked.</summary>
    public float Shown => Open ? Clamp(_for / ThreadIcon.WheelIn) : 1 - Clamp(_for / ThreadIcon.WheelOut);

    /// <summary>How much of the picked side's flare is left, 1 at the let-go to 0.</summary>
    public float Flare => !Open && Picked != null ? 1 - Clamp(_for / ThreadIcon.FlareS) : 0;

    /// <summary>Whether anything of it is drawn.</summary>
    public bool Drawn => Shown > 0 || Flare > 0;

    /// <summary>Steps it <paramref name="seconds"/> on, <paramref name="open"/> or not, pointed at <paramref name="toward"/> (a side open to pick).</summary>
    public void Step(double seconds, bool open, int? toward)
    {
        if (open != Open)
        {
            (_for, Picked) = (0, open ? null : _toward);
            Open = open;
        }
        if (open) _toward = toward;
        _for += seconds;
    }

    private static float Clamp(double v) => (float)Math.Clamp(v, 0, 1);
}
