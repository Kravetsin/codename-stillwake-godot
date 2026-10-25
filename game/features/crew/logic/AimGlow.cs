using System;
using System.Collections.Generic;
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
/// Where a tool's aim glows (the fan of rays), as one field of light rather than blocks: a dense sheaf of unseen rays from the
/// hand over the fan's arc at the pointer's own angle, each leaving light where it first meets ground that breaks and running
/// a little into it, fading toward the arc's edges and the end of its reach. It moves as smoothly as the pointer, whatever the
/// sim's few rays strike; the hit itself is the sim's (<see cref="Fan"/>), by block, so the glow shows its area, not its cells.
/// Plants are not lit: their pictures are not cells.
/// </summary>
public static class AimGlow
{
    private const int Cell = Camera.Cell;

    /// <summary>How far apart the unseen rays are, in radians: half a degree.</summary>
    public const double Step = Math.PI / 360;

    /// <summary>How deep into the rock the light runs from where a ray meets it, in world pixels: nearly the next layer.</summary>
    public const double Depth = 8;

    /// <summary>A splash of light's spread round a point, in world pixels, and where it is cut.</summary>
    private const double Spread = 1.8, Splash = 4;

    /// <summary>The share of the arc at either edge, and of the reach at its end, over which the light fades out.</summary>
    private const double EdgeFade = 0.15, EndFade = 0.15;

    /// <summary>
    /// The light of a hit of <paramref name="tool"/> from the hand at (<paramref name="handX"/>, <paramref name="handY"/>), in
    /// thousandths of a cell, toward <paramref name="angle"/> radians, in the front layer or the <paramref name="back"/> one: in
    /// natural walls that break where the sheaf first meets them, or on natural back walls that break in the open cells it
    /// crosses. None where it meets nothing.
    /// </summary>
    public static GlowField? FieldOf(IGridQueries grid, string tool, int handX, int handY, double angle, bool back)
    {
        var weapon = Spec.Weapons.Of(tool)!;
        double ox = handX * (double)Cell / Hero.Unit, oy = handY * (double)Cell / Hero.Unit;
        double range = weapon.Range * (double)Cell / Hero.Unit, arc = weapon.Arc * 2 * Math.PI / Spec.Headings.Turn;
        var splashes = new List<(double X, double Y, double Light)>();
        int rays = Math.Max(1, (int)Math.Round(arc / Step));
        for (int r = 0; r <= rays; r++)
        {
            double along = (double)r / rays, a = angle + arc * (along - 0.5);
            double edge = Math.Min(1, Math.Min(along, 1 - along) / EdgeFade);
            if (rays == 1) edge = 1;
            double ux = Math.Cos(a), uy = Math.Sin(a);
            for (double s = 0; s <= range; s += 0.5)
            {
                double x = ox + ux * s, y = oy + uy * s;
                int cell = CellAt(grid, x, y);
                if (cell < 0) break;
                double light = edge * Math.Min(1, (range - s) / (range * EndFade));
                if (grid.WallAt(cell) is { } wall || grid.Blocks(cell))
                {
                    // In front: the light runs on into what the ray met, if it breaks.
                    if (!back && grid.WallAt(cell) is { Breaks: true })
                        for (double d = 0; d <= Depth; d += 1)
                            splashes.Add((x + ux * d, y + uy * d, light * Math.Pow(1 - d / Depth, 1.4)));
                    break;
                }
                if (back && grid.BackAt(cell) is { Natural: true } behind && behind.Strength != Spec.Material.Unbreakable)
                    splashes.Add((x, y, light * 0.8));
            }
        }
        if (splashes.Count == 0) return null;
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        foreach (var (x, y, _) in splashes) (minX, minY, maxX, maxY) = (Math.Min(minX, x), Math.Min(minY, y), Math.Max(maxX, x), Math.Max(maxY, y));
        int left = (int)Math.Floor(minX - Splash), top = (int)Math.Floor(minY - Splash);
        int w = (int)Math.Ceiling(maxX + Splash) - left + 1, h = (int)Math.Ceiling(maxY + Splash) - top + 1;
        var field = new float[w * h];
        foreach (var (sx, sy, light) in splashes)
            for (int py = (int)Math.Floor(sy - Splash); py <= (int)Math.Ceiling(sy + Splash); py++)
                for (int px = (int)Math.Floor(sx - Splash); px <= (int)Math.Ceiling(sx + Splash); px++)
                {
                    double dx = px + 0.5 - sx, dy = py + 0.5 - sy, d2 = dx * dx + dy * dy;
                    if (d2 > Splash * Splash) continue;
                    int i = (py - top) * w + px - left;
                    field[i] = Math.Max(field[i], (float)(light * Math.Exp(-d2 / (2 * Spread * Spread))));
                }
        // Light stays in what it lights: walls that break in front, back walls that break behind; never the air.
        for (int py = 0; py < h; py++)
            for (int px = 0; px < w; px++)
            {
                int i = py * w + px;
                if (field[i] == 0) continue;
                int cell = CellAt(grid, left + px + 0.5, top + py + 0.5);
                bool lit = cell >= 0 && (back
                    ? grid.WallAt(cell) == null && !grid.Blocks(cell) && grid.BackAt(cell) is { Natural: true } b && b.Strength != Spec.Material.Unbreakable
                    : grid.WallAt(cell) is { Breaks: true });
                if (!lit) field[i] = 0;
            }
        return new GlowField(left, top, w, h, field);
    }

    /// <summary>The cell under world pixel point (<paramref name="x"/>, <paramref name="y"/>), or -1 off the grid.</summary>
    private static int CellAt(IGridQueries grid, double x, double y)
    {
        int cx = (int)Math.Floor(x / Cell), cy = (int)Math.Floor(y / Cell);
        return cx < 0 || cy < 0 || cx >= grid.Width || cy >= grid.Height ? -1 : cy * grid.Width + cx;
    }
}
