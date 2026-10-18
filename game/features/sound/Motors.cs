using System;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Sound;

/// <summary>
/// A motor heard as it runs (0059), apart from Godot: how fast it spins, from still (0) to full (1), linearly over its
/// <see cref="S.Motor"/>'s spin-up and run-down, and how loaded it is, 0 to 1, eased toward its load or kicked to full by a
/// blow; from them, its loop's or grains' pitch scale (<see cref="Speed"/>) and volume (<see cref="GainDb"/>). The drill's
/// motor and the Former's.
/// </summary>
public struct MotorSound
{
    private LoopEnvelope _spin;

    /// <summary>How fast it spins, 0 to 1.</summary>
    public readonly double Spin => _spin.Share;

    /// <summary>Whether it is heard: from the frame it starts till it has run down.</summary>
    public readonly bool Playing => _spin.Playing;

    /// <summary>How loaded it is, 0 to 1.</summary>
    public double Load { get; private set; }

    /// <summary>The motor moved on <paramref name="elapsedS"/> seconds, running or not, its load eased toward <paramref name="loaded"/>.</summary>
    public void Step(bool runs, bool loaded, double elapsedS, S.Motor tune)
    {
        _spin.Step(runs, elapsedS, tune.SpinUpS, tune.SpinDownS);
        double tau = loaded ? tune.LoadInS : tune.LoadOutS;
        double toward = loaded ? 1 : 0;
        Load += (toward - Load) * (tau <= 0 ? 1 : 1 - Math.Exp(-elapsedS / tau));
    }

    /// <summary>A blow: fully loaded at once, easing off from there (a press stroke).</summary>
    public void Kick() => Load = 1;

    /// <summary>Its pitch scale: <see cref="S.Motor.Idle"/> standing, 1 at speed, sagging under load.</summary>
    public readonly double Speed(S.Motor tune) => (tune.Idle + (1 - tune.Idle) * Spin) * (1 - tune.Sag * Load);

    /// <summary>Its volume on top of its cue's, in decibels: rising with its speed till full, quieter under load.</summary>
    public readonly float GainDb(S.Motor tune) =>
        (float)(tune.LoadedDb * Load + 20 * Math.Log10(Math.Max(Math.Min(1, tune.Swell * Spin), Hearing.Faint)));
}

/// <summary>
/// Something seen on and off from frame to frame, held on a little after it was last seen (0059): the drill's bit grazing a
/// piece's edge keeps striking, rather than breaking into bursts.
/// </summary>
public struct TouchHold
{
    private double _left;

    /// <summary>Whether it counts as on, <paramref name="elapsedS"/> later: on while <paramref name="on"/> and for <paramref name="holdS"/> after.</summary>
    public bool Step(bool on, double elapsedS, double holdS)
    {
        if (on)
        {
            _left = holdS;
            return true;
        }
        _left = Math.Max(0, _left - elapsedS);
        return _left > 0;
    }
}
