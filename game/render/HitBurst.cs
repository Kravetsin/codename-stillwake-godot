using System;

namespace Stillwake.Game.Render;

/// <summary>
/// A hit's look thrown where it lands (0094): each of its throws out of the struck face, its glint, shock line and flash. A
/// share of a whole hit throws that share of each count on average, rounded by chance so a drill's tenth-sized hits still add
/// up, and its glint and shock line by the same chance; its light flares to the share's root, so a run of quick hits holds a
/// glow about as bright as one hit's. Thrown into the world's particles, or a drill's card's own.
/// </summary>
public sealed class HitBurst(Glims glims, Flares flares)
{
    private const double FrameMs = 1000.0 / 60, FlashDecay = 0.84;
    private static readonly (int, int, int) ShockColour = (255, 236, 210);

    /// <summary>Thrown into the world's particles, which the effects layer draws.</summary>
    public static HitBurst Shared { get; } = new(Glims.Shared, Flares.Shared);

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
        if (look.Flash > 0) flares.Flash(x, y, look.FlashColour, look.Flash * Fx.Glow * Math.Sqrt(share), 2, 0.45f, FlashDecay);
    }

    /// <summary><paramref name="expected"/> rounded down or up by chance, so as many come on average.</summary>
    private int ByChance(double expected) => (int)Math.Floor(expected + glims.Between(0, 1));
}
