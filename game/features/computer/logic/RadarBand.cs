using System;

namespace Stillwake.Game.Features.Computer;

/// <summary>
/// Where the radar draws a stretch of space coming (docs/plans/stretches.md): a band along the ship's orbit, <see cref="Width"/> of the
/// screen wide, ahead of the ship (to its right, the way it flies) as the warning starts, sliding toward it as the warning runs, over
/// it as the stretch comes and while it runs.
/// </summary>
public static class RadarBand
{
    /// <summary>The band's width, a share of the screen's.</summary>
    public const float Width = 0.36f;

    /// <summary>How much of the band shows inside the screen's right edge as its warning starts.</summary>
    public const float Peeks = 0.06f;

    /// <summary>
    /// The band's near and far edges across a screen <paramref name="width"/> wide, the ship at <paramref name="shipX"/>, with
    /// <paramref name="grow"/> of the warning run (1 while it runs): from just inside the right edge to centred on the ship.
    /// </summary>
    public static (float Near, float Far) Across(float grow, float width, float shipX)
    {
        float band = Width * width, t = Math.Clamp(grow, 0, 1);
        float startNear = width - Peeks * width, endNear = shipX - band / 2;
        float near = startNear + (endNear - startNear) * t;
        return (near, near + band);
    }
}
