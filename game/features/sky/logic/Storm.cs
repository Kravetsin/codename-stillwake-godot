using System;

namespace Stillwake.Game.Features.Sky;

/// <summary>
/// How far a planet's weather (docs/plans/surface-storms.md) has come, as the pick page phased it: <see cref="Warn"/> rises over the
/// warning and holds while the storm runs, <see cref="Share"/> rises from just before the storm to some seconds in; both ease back to
/// nothing over <see cref="Ease"/> once it has gone. Read from the sky's <c>SkyNow</c> through <see cref="StretchShown"/>, so every
/// peer, a guest joining mid-storm and a world loaded mid-storm see the same. Plain C#.
/// </summary>
public readonly record struct Storm(float Warn, float Share)
{
    /// <summary>Seconds before the storm's start its share begins to rise, and after it that it is whole: the pick's, at the game's pace.</summary>
    public const double RiseFrom = -2, RiseTo = 9;

    /// <summary>Seconds the weather takes to ease back to nothing once it has gone.</summary>
    public const double Ease = 22;

    /// <summary>No weather: what a stretch without a weather look, or none, shows.</summary>
    public static readonly Storm None = new(0, 0);

    /// <summary>The weather <paramref name="shown"/> is in now: none unless its look has a <c>Weather</c>.</summary>
    public static Storm Of(StretchShown shown) =>
        shown.Look?.Weather == null ? None : At(shown.Grow, shown.Since, shown.Warning, shown.After * StretchLooks.Thaw);

    /// <summary>
    /// The weather <paramref name="grow"/> of its warning on, <paramref name="since"/> ticks after its warning began, its warning
    /// <paramref name="warning"/> ticks long; <paramref name="after"/> ticks since it went, 0 while it is coming or running.
    /// </summary>
    public static Storm At(float grow, int since, int warning, double after)
    {
        double into = (since - warning) / (double)Sim.World.TicksPerSecond;
        double fading = 1 - Smooth(0, Ease, after / Sim.World.TicksPerSecond);
        return new((float)(Smooth(0, 1, grow) * fading), (float)(Smooth(RiseFrom, RiseTo, into) * fading));
    }

    /// <summary>How hard the storm's own wind blows over the calm, in the pick's units: a third in the warning, nine tenths at its height.</summary>
    public float Wind => 0.32f * Warn + 0.58f * Share;

    /// <summary>The wind's share of its height, 0 to 1: how loud its loop plays, silent with no storm.</summary>
    public float Loudness => Math.Clamp(Wind / 0.9f, 0, 1);

    /// <summary>How loud <paramref name="weather"/>'s wind plays now, 0 to 1 of its cue: silent with no storm, and for a weather with no wind.</summary>
    public float Heard(StretchLooks.Weather? weather) => weather?.Wind == null ? 0 : Loudness;

    /// <summary>How far the sky and the land behind have turned toward the haze, 0 to 1 of the weather's <c>HazeMost</c>.</summary>
    public float Hazed => 0.35f * Warn + 0.65f * Share;

    /// <summary>How far daylight has dimmed toward the weather's <c>Dim</c>: a little in the warning, all of it in the storm.</summary>
    public float Dimmed => Math.Clamp(0.12f * Warn + 0.88f * Share, 0, 1);

    /// <summary>What daylight is multiplied by now under <paramref name="weather"/>: white with no storm.</summary>
    public (float R, float G, float B) Dim(StretchLooks.Weather weather)
    {
        float d = Dimmed;
        return (1 + (weather.Dim.R - 1) * d, 1 + (weather.Dim.G - 1) * d, 1 + (weather.Dim.B - 1) * d);
    }

    /// <summary>How many of <paramref name="weather"/>'s streaks the pick's view holds now.</summary>
    public int Streaks(StretchLooks.Weather weather) => (int)MathF.Round(weather.Count * Share);

    /// <summary>The haze the sky takes at the top of the screen, a share of the horizon's: the pick's gradient, whole from the horizon down.</summary>
    public const float SkyTop = 0.55f;

    /// <summary>How much faster the high cloud drifts on the storm's wind, pixels of art a second for each of its units: the pick's.</summary>
    public const float CloudBlow = 16;

    /// <summary>The pick's haze on its land's layers, far to near: the farthest takes all of it.</summary>
    private static readonly float[] Depths = [1, 0.85f, 0.65f];

    /// <summary>The share of the haze layer <paramref name="layer"/> of <paramref name="layers"/> takes, far to near: the pick's, spread over as many.</summary>
    public static float Veil(int layer, int layers)
    {
        if (layers <= 1) return Depths[0];
        float at = (float)layer / (layers - 1) * (Depths.Length - 1);
        int below = Math.Min((int)at, Depths.Length - 2);
        return Depths[below] + (Depths[below + 1] - Depths[below]) * (at - below);
    }

    private static double Smooth(double from, double to, double x)
    {
        double t = Math.Clamp((x - from) / (to - from), 0, 1);
        return t * t * (3 - 2 * t);
    }
}
