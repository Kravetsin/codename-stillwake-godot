using System;

namespace Stillwake.Game.Features.Light;

/// <summary>
/// How brightly a lamp short of power shines (features/light/flicker.ts): at the share of power it
/// gets, and short of all of it flickering, dark as often as power is short — at half of it half the
/// time, near none near always — fading between lit and dark rather than jumping.
/// </summary>
public static class Flicker
{
    /// <summary>How long a lamp holds lit or dark before it may change, in ms.</summary>
    private const double StepMs = 250;
    /// <summary>How long it takes to go from lit to dark or back, in ms: a sharp change tires the eye.</summary>
    private const double FadeMs = 90;
    /// <summary>How bright a lamp is in a dip, as a share of what it shines at otherwise.</summary>
    private const float Dip = 0.05f;

    /// <summary>A number in [0, 1) for <paramref name="seed"/> and <paramref name="step"/>, the same each time they are.</summary>
    private static double Noise(int seed, long step)
    {
        uint h = unchecked(((uint)seed ^ 0x9e3779b9u) * 0x85ebca6bu ^ (uint)step * 0xc2b2ae35u);
        h = unchecked((h ^ (h >> 16)) * 0x7feb352du);
        h = unchecked((h ^ (h >> 15)) * 0x846ca68bu);
        return (h ^ (h >> 16)) / 4294967296.0;
    }

    private static float StepOf(float share, int seed, long step) => Noise(seed, step) < 1 - share ? share * Dip : share;

    /// <summary>
    /// How brightly a lamp shines <paramref name="timeMs"/> in, given <paramref name="share"/> of the
    /// power it draws; <paramref name="seed"/> (its machine id) keeps lamps out of step.
    /// </summary>
    public static float BrightnessOf(float share, double timeMs, int seed)
    {
        if (share >= 1 || share <= 0) return Math.Clamp(share, 0, 1);
        long step = (long)Math.Floor(timeMs / StepMs);
        float was = StepOf(share, seed, step - 1), now = StepOf(share, seed, step);
        float t = (float)Math.Min(1, (timeMs - step * StepMs) / FadeMs);
        return was + (now - was) * t;
    }
}
