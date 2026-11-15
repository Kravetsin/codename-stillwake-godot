using System;
using System.Collections.Generic;

namespace Stillwake.Game.Shared;

/// <summary>
/// A square swept like a clock's hand (canister-mark): the canister's slot gets its colour back clockwise from the top as its
/// cooldown passes. Plain C#: points in the unit square, both where the wedge is drawn and the part of the picture it shows.
/// </summary>
public static class ClockWipe
{
    /// <summary>
    /// The wedge swept by <paramref name="passed"/> of a turn, its centre first, then along the square's edge from the top
    /// through each corner passed. Empty for none or a whole turn, which the picture's own drawing shows better.
    /// </summary>
    public static IReadOnlyList<(float X, float Y)> Of(double passed)
    {
        if (passed is <= 0.005 or >= 0.995) return [];
        double start = -Math.PI / 2, end = start + Math.Tau * passed;
        var points = new List<(float X, float Y)> { (0.5f, 0.5f), Edge(start) };
        for (int k = 0; k < 4; k++)
        {
            double corner = -Math.PI / 4 + k * Math.PI / 2;
            if (corner < end - 1e-4) points.Add(Edge(corner));
        }
        points.Add(Edge(end));
        return points;
    }

    /// <summary>Where a ray from the square's centre at <paramref name="angle"/>, clockwise as the screen's y runs down, meets its edge.</summary>
    public static (float X, float Y) Edge(double angle)
    {
        double c = Math.Cos(angle), s = Math.Sin(angle), t = 0.5 / Math.Max(Math.Abs(c), Math.Abs(s));
        return ((float)(0.5 + c * t), (float)(0.5 + s * t));
    }
}
