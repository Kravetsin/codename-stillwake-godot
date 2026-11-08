using System;

namespace Stillwake.Game.Features.Effects;

/// <summary>
/// A chip's triangle (0075), its corners turned and sized where it flies, so a frame's chips go to the screen as one list
/// of triangles: a polygon and a transform drawn for each took most of a frame of drilling at ×4 (planet-frame-rate).
/// </summary>
public static class ChipShape
{
    /// <summary>The corners of a chip of size 1, unturned, about its middle.</summary>
    public static readonly (float X, float Y)[] Corners = [(-1, -0.6f), (1.2f, -0.3f), (0, 0.9f)];

    /// <summary>The corners of a chip at (<paramref name="x"/>, <paramref name="y"/>), turned <paramref name="turn"/> radians and <paramref name="scale"/> times its size, into <paramref name="into"/>.</summary>
    public static void Place(double x, double y, double turn, double scale, Span<(float X, float Y)> into)
    {
        double cos = Math.Cos(turn) * scale, sin = Math.Sin(turn) * scale;
        for (int i = 0; i < Corners.Length; i++)
        {
            var (cx, cy) = Corners[i];
            into[i] = ((float)(x + cos * cx - sin * cy), (float)(y + sin * cx + cos * cy));
        }
    }
}
