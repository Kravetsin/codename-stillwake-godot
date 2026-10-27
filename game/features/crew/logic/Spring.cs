using System;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A damped spring following a value, frame by frame: it lags behind a change and swings on past it before it settles
/// (a held item's arm on a run, <see cref="HeroMotion.Carry"/>). Stepped in slices of at most 8 ms, so a slow frame does
/// not throw it.
/// </summary>
public sealed class Spring(double hz, double damping)
{
    private const double SliceMs = 8;
    private bool _set;

    /// <summary>Where it is, and how fast it moves, a second.</summary>
    public double At { get; private set; }
    public double Speed { get; private set; }

    /// <summary>Puts it at <paramref name="at"/>, still: what it follows from next.</summary>
    public void Hold(double at) => (At, Speed, _set) = (at, 0, true);

    /// <summary>Moves it on <paramref name="elapsedMs"/> toward <paramref name="target"/>; from where it is told first, at once.</summary>
    public double Follow(double target, double elapsedMs)
    {
        if (!_set) Hold(target);
        double w = 2 * Math.PI * hz;
        for (double left = elapsedMs; left > 0; left -= SliceMs)
        {
            double dt = Math.Min(left, SliceMs) / 1000;
            Speed += (w * w * (target - At) - 2 * damping * w * Speed) * dt;
            At += Speed * dt;
        }
        return At;
    }
}
