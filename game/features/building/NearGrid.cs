using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Building;

/// <summary>One cell edge of the grid near the pointer, in cells, and how strongly it is drawn.</summary>
public readonly record struct Edge(int X1, int Y1, int X2, int Y2, double Alpha);

/// <summary>The grid shown around the pointer while a card is out (features/building/near-grid.ts).</summary>
public static class NearGrid
{
    /// <summary>How far around the pointer the grid shows, in cells.</summary>
    public const int Near = 14;
    /// <summary>How strongly an edge right at the pointer is drawn.</summary>
    private const double Strongest = 0.35;

    /// <summary>
    /// The cell edges around <paramref name="pointer"/>, fading with the distance of each edge's middle
    /// from the pointer to nothing at <see cref="Near"/>; none past the grid's edges.
    /// </summary>
    public static List<Edge> Around((double X, double Y) pointer, int width, int height)
    {
        var edges = new List<Edge>();
        double Fade(double mx, double my)
        {
            double d = Math.Sqrt((mx - pointer.X) * (mx - pointer.X) + (my - pointer.Y) * (my - pointer.Y));
            return d < Near ? Strongest * (1 - d / Near) : 0;
        }
        int cx = (int)Math.Floor(pointer.X), cy = (int)Math.Floor(pointer.Y);
        int x0 = Math.Max(0, cx - Near), x1 = Math.Min(width, cx + Near + 1);
        int y0 = Math.Max(0, cy - Near), y1 = Math.Min(height, cy + Near + 1);
        for (int x = x0; x <= x1; x++)
            for (int y = y0; y < y1; y++)
            {
                double alpha = Fade(x, y + 0.5);
                if (alpha > 0) edges.Add(new Edge(x, y, x, y + 1, alpha));
            }
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x < x1; x++)
            {
                double alpha = Fade(x + 0.5, y);
                if (alpha > 0) edges.Add(new Edge(x, y, x + 1, y, alpha));
            }
        return edges;
    }
}
