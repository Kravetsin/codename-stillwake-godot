using System;

namespace Stillwake.Game.Features.Items;

/// <summary>How a stack on the floor moves as it lands (2026-10-29-kill-feel): plain C#, so its curve is tested.</summary>
public static class FloorFeel
{
    /// <summary>
    /// How high a stack that landed <paramref name="sinceMs"/> ago hops, in pixels, and how flat it is squashed, a share of its
    /// height: squashed first, then two hops, the second lower; nothing once done, or never landed.
    /// </summary>
    public static (float Hop, float Squash) Landing(double sinceMs)
    {
        if (double.IsNaN(sinceMs) || sinceMs < 0) return (0, 0);
        if (sinceMs < KillLooks.SquashMs) return (0, (float)(KillLooks.Squash * (1 - sinceMs / KillLooks.SquashMs)));
        double u = (sinceMs - KillLooks.SquashMs) / KillLooks.HopMs;
        if (u >= 1) return (0, 0);
        return u < 0.6 ? ((float)(KillLooks.HopFirst * Math.Sin(Math.PI * u / 0.6)), 0) : ((float)(KillLooks.HopSecond * Math.Sin(Math.PI * (u - 0.6) / 0.4)), 0);
    }
}
