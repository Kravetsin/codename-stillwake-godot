using System;

namespace Stillwake.Game.Features.Crew;

/// <summary>Which way a jetpack's plume blows: opposite its thrust, a whole unit long, down with none; its core's outline that way.</summary>
public static class PlumeWay
{
    /// <summary>The core's corners: each station's two and the tip between them.</summary>
    public const int Corners = 9;

    /// <summary>The core's width at its widest, in pixels.</summary>
    private const float CoreWidth = 1.5f;

    /// <summary>The stations along the core, nozzle to tip, each a share of its length.</summary>
    public static ReadOnlySpan<float> Stations => [0, 0.2f, 0.45f, 0.7f];

    /// <summary>
    /// The plume's way from the thrust (<paramref name="tx"/>, <paramref name="ty"/>), normalised in doubles: a slowing
    /// catcher's tiny thrust would square to nothing as floats and fold the plume into a point.
    /// </summary>
    public static (float X, float Y) Of(double tx, double ty)
    {
        double length = Math.Sqrt(tx * tx + ty * ty);
        return length == 0 ? (0, 1) : ((float)(-tx / length), (float)(-ty / length));
    }

    /// <summary>
    /// Fills <paramref name="outline"/> with the core blown <paramref name="way"/> from <paramref name="nozzle"/>: two curved flanks
    /// meeting at its tip, a quadratic swell from the nozzle's width to nothing. Returns the point the outline is measured from.
    /// </summary>
    /// <remarks>From the nozzle: in world pixels Godot's float area of so small a shape loses its sign, and it fills nothing.</remarks>
    public static (float X, float Y) Core(Span<(float X, float Y)> outline, (float X, float Y) nozzle, (float X, float Y) way, float length)
    {
        for (int i = 0; i < Stations.Length; i++)
        {
            float t = Stations[i], u = 1 - t, half = CoreWidth * (0.6f * u * u + 2 * u * t * 1.15f);
            float x = way.X * (length * t), y = way.Y * (length * t);
            outline[i] = (x - way.Y * half, y + way.X * half);
            outline[Corners - 1 - i] = (x + way.Y * half, y - way.X * half);
        }
        outline[Stations.Length] = (way.X * length, way.Y * length);
        return nozzle;
    }
}
