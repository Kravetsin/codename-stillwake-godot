using System;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// A capback's glob (lurker-attacks) as drawn: the sim's arc lingers while it flies and lays the puddle as it lands; the client flies the
/// glob from the capback to where the arc ends along a lob, by the share of its lingering gone. Pixels of the world, y down. Plain C#, tested.
/// </summary>
public static class GlobFlight
{
    /// <summary>How high a lob rises over its line, a share of its length across, at most <see cref="MostRise"/> pixels.</summary>
    public const double Rise = 0.22, MostRise = 28;
    /// <summary>Where the glob is <paramref name="t"/> of the way (0..1) along its lob, and the way it heads, in radians.</summary>
    public static (double X, double Y, double Heading) At(double x0, double y0, double x1, double y1, double t)
    {
        t = Math.Clamp(t, 0, 1);
        double rise = Math.Min(MostRise, Rise * Math.Abs(x1 - x0));
        double x = x0 + (x1 - x0) * t, y = y0 + (y1 - y0) * t - 4 * rise * t * (1 - t);
        double dx = x1 - x0, dy = y1 - y0 - 4 * rise * (1 - 2 * t);
        return (x, y, Math.Atan2(dy, dx));
    }
}
