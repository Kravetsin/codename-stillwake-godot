using System;

namespace Stillwake.Game.Render;

/// <summary>
/// A hit's look thrown where it lands (0094): each of its throws out of the struck face, its glint, shock line and flash. A
/// share of a whole hit throws that share of each count on average, rounded by chance so a drill's tenth-sized hits still add
/// up, and its glint and shock line by the same chance; its light flares to the share's root, so a run of quick hits holds a
/// glow about as bright as one hit's. Thrown into a stage's particles, or a drill's card's own.
/// </summary>
public sealed class HitBurst(Glims glims, Flares flares)
{
    private const double FrameMs = 1000.0 / 60, FlashDecay = 0.84;
    private static readonly (int, int, int) ShockColour = (255, 236, 210);

    /// <summary>
    /// <paramref name="look"/>, <paramref name="share"/> of it (0 to 1), at (<paramref name="x"/>, <paramref name="y"/>) in pixels of the
    /// world, out of a face turned (<paramref name="nx"/>, <paramref name="ny"/>), chips falling to <paramref name="floor"/> if given.
    /// </summary>
    public void Throw(HitLook look, double x, double y, double nx, double ny, double share, double? floor = null)
    {
        foreach (var t in look.Throws)
        {
            int count = ByChance(t.Count * Fx.Count * share);
            if (count == 0) continue;
            double heading = Math.Atan2(ny - t.Up, nx);
            for (int i = 0; i < count; i++)
            {
                double a = heading + glims.Between(-t.Spread, t.Spread), s = glims.Between(t.Speed.From, t.Speed.To);
                double? size = t.Size is { } z ? glims.Between(z.From, z.To) : null;
                glims.Add(t.Look, x, y, Math.Cos(a) * s, Math.Sin(a) * s, floor, size: size);
            }
        }
        if (look.Glint > 0 && glims.Between(0, 1) < share) flares.Glint(x, y, look.Glint, look.GlintColour, glims.Between(-0.2, 0.2));
        if (look.Shock > 0 && glims.Between(0, 1) < share) flares.Shock(x, y, 2, 17, 11 * FrameMs, ShockColour, look.Shock * Fx.Glow);
        if (look.Flash > 0) flares.Flash(x, y, look.FlashColour, look.Flash * Fx.Glow * Math.Sqrt(share), 4, 0.671f, FlashDecay);
    }

    /// <summary><paramref name="expected"/> rounded down or up by chance, so as many come on average.</summary>
    private int ByChance(double expected) => (int)Math.Floor(expected + glims.Between(0, 1));

    /// <summary>
    /// <paramref name="look"/>, <paramref name="share"/> of it, out of a tool's haze where it lies in what it hit (spark-rule): from
    /// up to <paramref name="most"/> of <paramref name="points"/>, spread along them, each along the haze's way there and a
    /// like share; chips falling to <paramref name="floor"/> if given.
    /// </summary>
    public void ThrowFrom(HitLook look, System.Collections.Generic.IReadOnlyList<HazePoint> points, double share, int most, double? floor = null)
    {
        int n = Math.Min(most, points.Count);
        for (int k = 0; k < n; k++)
        {
            var p = points[n == 1 ? points.Count / 2 : k * (points.Count - 1) / (n - 1)];
            Throw(look, p.X, p.Y, p.Dx, p.Dy, share / n, floor);
        }
    }
}
