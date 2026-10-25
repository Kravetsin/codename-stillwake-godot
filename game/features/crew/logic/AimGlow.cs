using System;
using System.Collections.Generic;
using Stillwake.Sim;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Crew;

/// <summary>The light of a tool's aim over a box of the world's pixels: each pixel's from 0 to 1, row by row.</summary>
public sealed record GlowField(int Left, int Top, int Width, int Height, float[] Light)
{
    /// <summary>The light at world pixel (<paramref name="x"/>, <paramref name="y"/>); none outside the box.</summary>
    public float At(int x, int y) =>
        x < Left || y < Top || x >= Left + Width || y >= Top + Height ? 0 : Light[(y - Top) * Width + x - Left];
}

/// <summary>
/// Where a tool's aim glows (the fan of rays), as a field of light rather than blocks: each ray of the fan touches what it would
/// strike at a point, and light spreads round those points into the thickness of what breaks, fading with depth from the air
/// and away from the points, so the glow is one area that slides as the aim moves and turns inner corners whole. It lies only on
/// what breaks; damage stays by block, so a glow's soft end may lie a few pixels on a block the hit spares.
/// </summary>
public static class AimGlow
{
    private const int Cell = Camera.Cell;

    /// <summary>How far light spreads round a point a ray touches, in world pixels: its spread, and where it is cut.</summary>
    public const double Spread = 4, Reach = 11;

    /// <summary>How deep into the rock the light runs, in world pixels: nearly to the next layer of blocks.</summary>
    public const double Depth = 8;

    /// <summary>
    /// The points, in world pixels, where <paramref name="fan"/>'s rays touch what they strike: a wall where they enter it, a plant
    /// where they enter its cell, a back wall along the ray through it every few pixels; none where a ray strikes nothing.
    /// </summary>
    public static List<(double X, double Y)> HitsOf(IGridQueries grid, BuildingModule building, Fan fan)
    {
        var hits = new List<(double, double)>();
        var hew = fan.Ground;
        var weapon = Spec.Weapons.Of(hew.Tool)!;
        var struck = new HashSet<int>(fan.Hewn.Cells);
        var plants = new HashSet<Plant>(fan.Hewn.Plants);
        double ox = hew.X * (double)Cell / Hero.Unit, oy = hew.Y * (double)Cell / Hero.Unit;
        int width = grid.Width;
        for (int i = 0; i < weapon.Rays; i++)
        {
            int heading = Rays.HeadingOf(weapon, hew.Heading, i);
            var (dx, dy) = Rays.Direction(heading);
            double length = Math.Sqrt((double)dx * dx + (double)dy * dy);
            double ux = dx / length, uy = dy / length;
            (double X, double Y) Entry(int cell)
            {
                double left = cell % width * Cell, top = cell / width * Cell;
                double t = Math.Max(0, Math.Max(Enter(ox, ux, left, left + Cell), Enter(oy, uy, top, top + Cell)));
                return (ox + ux * t, oy + uy * t);
            }
            Rays.Cast(width, grid.Height, hew.X, hew.Y, heading, weapon.Range, cell =>
            {
                if (grid.WallAt(cell) != null || grid.Blocks(cell))
                {
                    if (!hew.Back && struck.Contains(cell)) hits.Add(Entry(cell));
                    return true;
                }
                if (hew.Back && struck.Contains(cell))
                {
                    var (x, y) = Entry(cell);
                    for (double s = 0; s < Cell && Inside(x + ux * s, y + uy * s, cell, width); s += 3) hits.Add((x + ux * s, y + uy * s));
                }
                else if (!hew.Back && building.PlantAt(cell) is { } plant && plants.Contains(plant)) hits.Add(Entry(cell));
                return false;
            });
        }
        return hits;
    }

    /// <summary>
    /// The light of <paramref name="fan"/>'s aim round the points its rays touch (<see cref="HitsOf"/>): in a natural wall that
    /// breaks, the nearer the air and the points the brighter; over a struck plant's cells and struck back walls, by the points
    /// alone. None where no ray touches anything.
    /// </summary>
    public static GlowField? FieldOf(IGridQueries grid, BuildingModule building, Fan fan)
    {
        var hits = HitsOf(grid, building, fan);
        if (hits.Count == 0) return null;
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        foreach (var (x, y) in hits) (minX, minY, maxX, maxY) = (Math.Min(minX, x), Math.Min(minY, y), Math.Max(maxX, x), Math.Max(maxY, y));
        int left = (int)Math.Floor(minX - Reach), top = (int)Math.Floor(minY - Reach);
        int w = (int)Math.Ceiling(maxX + Reach) - left, h = (int)Math.Ceiling(maxY + Reach) - top;
        var light = new float[w * h];
        bool back = fan.Ground.Back;
        var struck = new HashSet<int>(fan.Hewn.Cells);
        var plants = new HashSet<Plant>(fan.Hewn.Plants);
        int width = grid.Width;
        for (int py = 0; py < h; py++)
            for (int px = 0; px < w; px++)
            {
                int x = left + px, y = top + py;
                if (x < 0 || y < 0 || x >= width * Cell || y >= grid.Height * Cell) continue;
                int cell = y / Cell * width + x / Cell;
                double depth = back ? (struck.Contains(cell) ? 0 : Depth)
                    : grid.WallAt(cell) is { Breaks: true } ? DepthAt(grid, x + 0.5, y + 0.5)
                    : building.PlantAt(cell) is { } plant && plants.Contains(plant) ? 0
                    : Depth;
                if (depth >= Depth) continue;
                double sum = 0;
                foreach (var (hx, hy) in hits)
                {
                    double d2 = (x + 0.5 - hx) * (x + 0.5 - hx) + (y + 0.5 - hy) * (y + 0.5 - hy);
                    if (d2 < Reach * Reach) sum += Math.Exp(-d2 / (2 * Spread * Spread));
                }
                light[py * w + px] = (float)(Math.Min(1, sum) * Math.Pow(1 - depth / Depth, 1.4));
            }
        return new GlowField(left, top, w, h, light);
    }

    /// <summary>How far world point (<paramref name="x"/>, <paramref name="y"/>), in a wall, is from the air: the nearest open cell round it; <see cref="Depth"/> past that.</summary>
    private static double DepthAt(IGridQueries grid, double x, double y)
    {
        int cx = (int)(x / Cell), cy = (int)(y / Cell), width = grid.Width;
        double best = Depth;
        for (int ny = cy - 1; ny <= cy + 1; ny++)
            for (int nx = cx - 1; nx <= cx + 1; nx++)
            {
                if (nx < 0 || ny < 0 || nx >= width || ny >= grid.Height) continue;
                int n = ny * width + nx;
                if (grid.WallAt(n) != null || grid.Blocks(n)) continue;
                double dx = Math.Max(0, Math.Max(nx * Cell - x, x - (nx + 1) * Cell));
                double dy = Math.Max(0, Math.Max(ny * Cell - y, y - (ny + 1) * Cell));
                best = Math.Min(best, Math.Sqrt(dx * dx + dy * dy));
            }
        return best;
    }

    /// <summary>Where along a ray from <paramref name="o"/> going <paramref name="u"/> it enters the span <paramref name="from"/> to <paramref name="to"/>.</summary>
    private static double Enter(double o, double u, double from, double to) =>
        u == 0 ? (o >= from && o <= to ? double.MinValue : double.MaxValue) : Math.Min((from - o) / u, (to - o) / u);

    private static bool Inside(double x, double y, int cell, int width) =>
        (int)Math.Floor(x / Cell) == cell % width && (int)Math.Floor(y / Cell) == cell / width;
}
