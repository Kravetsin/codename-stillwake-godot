using System;

namespace Stillwake.Game.Features.Hazards;

/// <summary>
/// How a magnetic flare is seen (2026-10-30-magnetic-flare), as numbers the views read: how far its warning has run, how the
/// lamps falter and the cables glow through it, and the flash at the strike.
/// </summary>
public static class FlareLook
{
    /// <summary>How long the flash at the strike takes to fade, in ms.</summary>
    public const double FlashMs = 700;

    /// <summary>
    /// How far a warning with <paramref name="left"/> ticks left of <paramref name="warning"/> has run, the frame
    /// <paramref name="progress"/> into the next tick: 0 as it starts, 1 at the strike; 0 with none coming.
    /// </summary>
    public static double Near(int left, double progress, int warning) =>
        left <= 0 || warning <= 0 ? 0 : Math.Clamp(1 - (left - progress) / warning, 0, 1);

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

    /// <summary>How white the screen is <paramref name="sinceMs"/> after the strike: whole at it, gone after <see cref="FlashMs"/>.</summary>
    public static double Flash(double sinceMs) => sinceMs < 0 || sinceMs >= FlashMs ? 0 : 1 - sinceMs / FlashMs;
}
