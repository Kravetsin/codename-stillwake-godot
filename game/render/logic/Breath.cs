using System;

namespace Stillwake.Game.Render;

/// <summary>
/// How what is aimed at breathes (no ADR): its outline and the faces a tool would strike glow up and down together, faint at
/// rest so they never shout, on one clock so every one of them is in step.
/// </summary>
public static class Breath
{
    /// <summary>A breath's length, in milliseconds.</summary>
    public const double PeriodMs = 1900;

    /// <summary>The faintest and the brightest a breath gets.</summary>
    public const double Least = 0.6, Most = 1;

    /// <summary>How bright a breath is <paramref name="clockMs"/> into the game, from <see cref="Least"/> to <see cref="Most"/>.</summary>
    public static double Alpha(double clockMs) =>
        Least + (Most - Least) * (0.5 + 0.5 * Math.Sin(clockMs / PeriodMs * 2 * Math.PI));
}
