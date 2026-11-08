using System;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// A capback's glob (lurker-attacks) as drawn: the sim strikes its arc at once and lays the puddle the same tick, so the client flies the
/// glob from the capback to where the arc ended along a lob, and shows the puddle from its landing. Pixels of the world, y down;
/// times in seconds. Plain C#, tested.
/// </summary>
public static class GlobFlight
{
    /// <summary>How long a glob flies: a base, and more the farther it goes, at most <see cref="MostS"/>.</summary>
    public const double LeastS = 0.18, PerPxS = 0.0016, MostS = 0.45;

    /// <summary>How high a lob rises over its line, a share of its length across, at most <see cref="MostRise"/> pixels.</summary>
    public const double Rise = 0.22, MostRise = 28;

    /// <summary>The seconds a glob from (<paramref name="x0"/>, <paramref name="y0"/>) to (<paramref name="x1"/>, <paramref name="y1"/>) flies.</summary>
    public static double Seconds(double x0, double y0, double x1, double y1) =>
        Math.Min(MostS, LeastS + PerPxS * Math.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0)));

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
