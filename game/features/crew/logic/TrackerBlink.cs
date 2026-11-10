using System;
using System.Collections.Generic;
using Stillwake.Sim.Crew;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The tracker's light by its <see cref="Signal"/> (tracker-catchers): a level over time, 1 the steady light it has when calm (as the meter fills, so it leaks nothing),
/// 0 dark, above 1 brighter toward white; a pattern of steps, each held or ramped, repeating. The patterns are the warning page's pick «Маяк» (bc-beacon).
///
/// </summary>
public static class TrackerBlink
{
    /// <summary>One step of a pattern: <paramref name="Ms"/> long, its level going from <paramref name="From"/> to <paramref name="To"/>.</summary>
    public readonly record struct Step(double Ms, double From, double To);

    /// <summary>From the alarm on, while a catcher comes and hunts its hero: urgent.</summary>
    public static readonly Step[] Hunted = [new(610, 1, 1), new(90, 1.7, 1.7)];

    /// <summary>The level of a tracker showing <paramref name="signal"/>, <paramref name="ms"/> into its figure's life.</summary>
    public static double Of(Signal signal, double ms) => signal switch
    {
        Signal.Coming or Signal.Hunted => LevelOf(Hunted, ms),
        _ => 1,
    };

    /// <summary>The level <paramref name="ms"/> into <paramref name="steps"/> repeating; a ramp's level in a straight line.</summary>
    public static double LevelOf(IReadOnlyList<Step> steps, double ms)
    {
        double period = 0;
        foreach (var step in steps) period += step.Ms;
        double at = (ms % period + period) % period;
        foreach (var step in steps)
        {
            if (at < step.Ms) return step.From + (step.To - step.From) * at / step.Ms;
            at -= step.Ms;
        }
        return steps[^1].To;
    }

    /// <summary>
    /// The light's colour at <paramref name="level"/> over the player's <paramref name="rgb"/> (0..1 each): above 1 its channels
    /// multiplied and kept to 1, below it fading out by alpha.
    /// </summary>
    public static (double R, double G, double B, double A) Lit((double R, double G, double B) rgb, double level) =>
        level >= 1
            ? (Math.Min(1, rgb.R * level), Math.Min(1, rgb.G * level), Math.Min(1, rgb.B * level), 1)
            : (rgb.R, rgb.G, rgb.B, Math.Max(0, level));
}
