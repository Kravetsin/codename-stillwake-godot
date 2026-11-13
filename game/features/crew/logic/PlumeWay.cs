using System;

namespace Stillwake.Game.Features.Crew;

/// <summary>Which way a jetpack's plume blows: opposite its thrust, a whole unit long, down with none.</summary>
public static class PlumeWay
{
    /// <summary>
    /// The plume's way from the thrust (<paramref name="tx"/>, <paramref name="ty"/>), normalised in doubles: a slowing
    /// catcher's tiny thrust would square to nothing as floats and fold the plume into a point.
    /// </summary>
    public static (float X, float Y) Of(double tx, double ty)
    {
        double length = Math.Sqrt(tx * tx + ty * ty);
        return length == 0 ? (0, 1) : ((float)(-tx / length), (float)(-ty / length));
    }
}
