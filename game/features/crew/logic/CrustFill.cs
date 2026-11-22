using System;
using System.Collections.Generic;
using Xy = (float X, float Y);

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The fill of a piece of the thermometer's crust that Godot cannot triangulate: a crest's tip just inside the bulb's rim, too
/// small for its ear clipping to tell which way round it runs, or a piece pinched where the wave touches the rim.
/// </summary>
public static class CrustFill
{
    /// <summary>
    /// The inside of <paramref name="piece"/> cut at each corner's x into upright strips, two triangles for each stretch inside:
    /// it needs neither an ear nor a winding.
    /// </summary>
    public static (Xy[] Points, int[] Indices) Upright(Xy[] piece)
    {
        float[] xs = Array.ConvertAll(piece, p => p.X);
        Array.Sort(xs);
        var points = new List<Xy>();
        var indices = new List<int>();
        var sides = new List<(float Left, float Right)>();
        for (int i = 0; i + 1 < xs.Length; i++)
        {
            float left = xs[i], right = xs[i + 1];
            if (left == right) continue;
            sides.Clear();
            for (int j = 0; j < piece.Length; j++)
            {
                var (a, b) = (piece[j], piece[(j + 1) % piece.Length]);
                if (Math.Min(a.X, b.X) > left || Math.Max(a.X, b.X) < right) continue;
                float slope = (b.Y - a.Y) / (b.X - a.X);
                sides.Add((a.Y + (left - a.X) * slope, a.Y + (right - a.X) * slope));
            }
            sides.Sort((a, b) => (a.Left + a.Right).CompareTo(b.Left + b.Right));
            for (int j = 0; j + 1 < sides.Count; j += 2)
            {
                int n = points.Count;
                points.AddRange([(left, sides[j].Left), (right, sides[j].Right), (right, sides[j + 1].Right), (left, sides[j + 1].Left)]);
                indices.AddRange([n, n + 1, n + 2, n, n + 2, n + 3]);
            }
        }
        return ([.. points], [.. indices]);
    }
}
