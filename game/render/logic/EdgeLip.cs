using System;
using System.Collections.Generic;

namespace Stillwake.Game.Render;

/// <summary>
/// A held blade's lip (hero-skins): the outline of its area round the edge (<see cref="HeldLook.EdgeBox"/>) that faces the way the
/// edge does, out of its bow, ordered from the edge's one end to the other. What an effect skin's rest state grows from.
/// </summary>
public static class EdgeLip
{
    /// <summary>A point of the lip, in pixels of the picture: where, the way out there, and its share along the edge, 0 to 1.</summary>
    public readonly record struct Point(double X, double Y, double Nx, double Ny, double T);

    /// <summary>The lip of <paramref name="area"/> on <paramref name="look"/>; its whole outline if few of it face out, the edge's ends if it has none.</summary>
    public static IReadOnlyList<Point> Of(EffectArea area, HeldLook look)
    {
        double dx = look.EdgeTo.X - look.EdgeFrom.X, dy = look.EdgeTo.Y - look.EdgeFrom.Y, length2 = Math.Max(1e-9, dx * dx + dy * dy), length = Math.Sqrt(length2);
        var (ox, oy) = (-dy / length, dx / length);
        var lip = new List<Point>();
        foreach (var (x, y, nx, ny) in area.Edge)
            if (nx * ox + ny * oy > 0.25) lip.Add(new(x, y, nx, ny, ((x - look.EdgeFrom.X) * dx + (y - look.EdgeFrom.Y) * dy) / length2));
        if (lip.Count < 4)
        {
            lip.Clear();
            foreach (var (x, y, nx, ny) in area.Edge) lip.Add(new(x, y, nx, ny, ((x - look.EdgeFrom.X) * dx + (y - look.EdgeFrom.Y) * dy) / length2));
        }
        if (lip.Count == 0) return [new(look.EdgeFrom.X, look.EdgeFrom.Y, ox, oy, 0), new(look.EdgeTo.X, look.EdgeTo.Y, ox, oy, 1)];
        lip.Sort((a, b) => a.T.CompareTo(b.T));
        return lip;
    }
}
