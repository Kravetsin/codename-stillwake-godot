using System;

namespace Stillwake.Game.Features.Hazards;

/// <summary>
/// How a magnetic flare is seen (2026-10-30-magnetic-flare), as numbers the views read: how far its warning has run, how the
/// aurora comes in, how the lamps falter and the cables glow through it, and the flash at the strike.
/// </summary>
public static class FlareLook
{
    /// <summary>How long the flash at the strike takes to whiten the screen, in ms: quick, but not a cut.</summary>
    public const double FlashRiseMs = 120;

    /// <summary>How long the flash lasts in all, its rise and its fade, in ms.</summary>
    public const double FlashMs = 1400;

    /// <summary>How long the aurora takes to fade once the flare has struck, in ms.</summary>
    public const double AuroraFadeMs = 2500;

    /// <summary>The share of the warning over which the aurora comes in from nothing.</summary>
    private const double AuroraIn = 0.4;

    /// <summary>
    /// How far a warning with <paramref name="left"/> ticks left of <paramref name="warning"/> has run, the frame
    /// <paramref name="progress"/> into the next tick: 0 as it starts, 1 at the strike; 0 with none coming.
    /// </summary>
    public static double Near(int left, double progress, int warning) =>
        left <= 0 || warning <= 0 ? 0 : Math.Clamp(1 - (left - progress) / warning, 0, 1);

    /// <summary>
    /// How strong the aurora is through a warning <paramref name="near"/> run: coming in from nothing, eased, over its first
    /// <see cref="AuroraIn"/>, and growing on to whole at the strike.
    /// </summary>
    public static double Aurora(double near) => near <= 0 ? 0 : Smooth(near / AuroraIn) * (0.45 + 0.55 * Math.Min(near, 1));

    /// <summary>What is left of the aurora <paramref name="sinceMs"/> after the strike, of what it was then: eased out to nothing.</summary>
    public static double AuroraLeft(double sinceMs) => 1 - Smooth(sinceMs / AuroraFadeMs);

    /// <summary>
    /// The share of their power the lamps shine at through a warning <paramref name="near"/> run: short of full from the start, so
    /// they flicker as a lamp short of power does, more often the nearer the strike.
    /// </summary>
    public static float LampShare(double near) => near <= 0 ? 1f : (float)(0.9 - 0.55 * near);

    /// <summary>
    /// How brightly the cables glow through a warning <paramref name="near"/> run, <paramref name="timeMs"/> in: rising toward the
    /// strike, pulsing faster as it nears; 0 with none coming.
    /// </summary>
    public static double CableGlow(double near, double timeMs) =>
        near <= 0 ? 0 : near * (0.65 + 0.35 * Math.Sin(timeMs / 1000 * Math.Tau * (1 + 3 * near)));

    /// <summary>
    /// How white the screen is <paramref name="sinceMs"/> after the strike: eased up to whole over <see cref="FlashRiseMs"/>, then
    /// eased out to nothing by <see cref="FlashMs"/>, its tail long and soft.
    /// </summary>
    public static double Flash(double sinceMs)
    {
        if (sinceMs <= 0 || sinceMs >= FlashMs) return 0;
        if (sinceMs < FlashRiseMs) return Smooth(sinceMs / FlashRiseMs);
        return 1 - Smooth((sinceMs - FlashRiseMs) / (FlashMs - FlashRiseMs));
    }

    /// <summary>Smoothstep of <paramref name="t"/>, clamped: 0 to 1 with no jolt at either end.</summary>
    private static double Smooth(double t)
    {
        t = Math.Clamp(t, 0, 1);
        return t * t * (3 - 2 * t);
    }
}
