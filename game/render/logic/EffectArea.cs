using System;
using System.Collections.Generic;

namespace Stillwake.Game.Render;

/// <summary>
/// Where a held tool's effect lives (spear-effects): one area of any shape, the drawn pixels of its picture inside a box (a
/// module's crackle box, <see cref="HeldParts.Crackles"/>), never a few fixed points. An effect draws its places from it,
/// anywhere inside or on its outline, the outline's points with the way out of the area, in pixels of the picture.
/// </summary>
public sealed class EffectArea
{
    private readonly List<(double X, double Y)> _inside = [];
    private readonly List<(double X, double Y, double Nx, double Ny)> _edge = [];

    /// <summary>Every drawn pixel's middle, in pixels of the picture.</summary>
    public IReadOnlyList<(double X, double Y)> Inside => _inside;

    /// <summary>Every pixel on its outline, its middle and the unit way out of the area there, in pixels of the picture.</summary>
    public IReadOnlyList<(double X, double Y, double Nx, double Ny)> Edge => _edge;

    /// <summary>How many of the picture's own pixels make a pixel of the picture: the size of one of its pixels.</summary>
    public int PerPixel { get; private init; } = 1;

    /// <summary>Whether it has no pixel: nothing drawn in its box.</summary>
    public bool Empty => _inside.Count == 0;

    /// <summary>
    /// The area of a picture <paramref name="width"/> by <paramref name="height"/> of its own pixels, <paramref name="perPixel"/>
    /// of them to a pixel of the picture (a hi-res picture's scale), whose drawn ones <paramref name="drawn"/> says, inside
    /// <paramref name="box"/> in pixels of the picture.
    /// </summary>
    public static EffectArea Of(int width, int height, Func<int, int, bool> drawn, int perPixel, (double X0, double Y0, double X1, double Y1) box)
    {
        var area = new EffectArea { PerPixel = perPixel };
        int x0 = Math.Max(0, (int)Math.Floor(box.X0 * perPixel)), y0 = Math.Max(0, (int)Math.Floor(box.Y0 * perPixel));
        int x1 = Math.Min(width, (int)Math.Ceiling(box.X1 * perPixel)), y1 = Math.Min(height, (int)Math.Ceiling(box.Y1 * perPixel));
        bool Drawn(int x, int y) => x >= 0 && x < width && y >= 0 && y < height && drawn(x, y);
        bool In(int x, int y) => x >= x0 && x < x1 && y >= y0 && y < y1 && drawn(x, y);
        for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
            {
                if (!In(x, y)) continue;
                (double X, double Y) middle = ((x + 0.5) / perPixel, (y + 0.5) / perPixel);
                area._inside.Add(middle);
                // On the outline where a neighbour is the picture's empty, not where the box cuts it; out is the way to those, summed.
                double nx = 0, ny = 0;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                        if ((dx != 0 || dy != 0) && !Drawn(x + dx, y + dy)) (nx, ny) = (nx + dx, ny + dy);
                double l = Math.Sqrt(nx * nx + ny * ny);
                if (l > 0) area._edge.Add((middle.X, middle.Y, nx / l, ny / l));
            }
        return area;
    }

    /// <summary>A place anywhere inside, every pixel as likely, from <paramref name="random"/>'s draw (0 to 1), jittered within its pixel.</summary>
    public (double X, double Y) AnyInside(Func<double> random)
    {
        var (x, y) = _inside[Math.Min(_inside.Count - 1, (int)(random() * _inside.Count))];
        return (x + (random() - 0.5) / PerPixel, y + (random() - 0.5) / PerPixel);
    }

    /// <summary>A place on the outline, every outline pixel as likely, with the way out there.</summary>
    public (double X, double Y, double Nx, double Ny) AnyOnEdge(Func<double> random) => _edge[Math.Min(_edge.Count - 1, (int)(random() * _edge.Count))];
}
