using System;
using System.Collections.Generic;
using Stillwake.Game.Render;
using static Stillwake.Game.Render.Ink.Draw;

namespace Stillwake.Game.Features.Building;

/// <summary>
/// A dug block's cracks (damage-looks, "fault"): one ragged split across the whole cell, top to bottom, at each of three stages
/// (0079): a hairline, then thicker with a branch off it, then a second branch and the split opened into a dark gap, its lower
/// lip lit. Laid out by the cell's seed, each cell its own; kept within its cell.
/// </summary>
public static class BlockFault
{
    private const double Cell = 8;

    /// <summary>The seed a cell's fault is laid out by: its index and its layer.</summary>
    public static int SeedOf(int cell, bool back) => (int)(Hash(cell, back ? 2 : 1) * int.MaxValue);

    /// <summary>The fault of <paramref name="stage"/>, 0 to 2, of the cell whose top left is (<paramref name="x"/>, <paramref name="y"/>), into <paramref name="ink"/>.</summary>
    public static void Draw(Ink ink, int stage, int seed, DamageLooks.FaultInk tones, double x, double y)
    {
        var r = new Rng(seed);
        double gap = DamageLooks.FaultGap[stage], width = DamageLooks.FaultWidth[stage];
        double x0 = 1.5 + r.Next() * 2, x1 = 4.5 + r.Next() * 2.5;
        var line = new List<(double X, double Y)>();
        for (int i = 0; i <= DamageLooks.FaultKinks; i++)
        {
            double q = (double)i / DamageLooks.FaultKinks;
            line.Add((x0 + (x1 - x0) * q + (r.Next() - 0.5) * 1.4, -0.2 + q * 8.4));
        }
        var branch = Walk(r, line[2].X, line[2].Y, 0.2 + r.Next() * 0.5, 3.2, 0.45, 0.5);
        var branch2 = Walk(r, line[4].X, line[4].Y, Math.PI + 0.3 + r.Next() * 0.5, 2.6, 0.45, 0.5);
        (ink.Shift, ink.Box) = ((x, y), (x, y, x + Cell, y + Cell));
        if (gap > 0)
        {
            var lip = line.ConvertAll(p => (p.X + gap, p.Y + gap * 0.3));
            ink.Band(line, lip, tones.Deep);
            ink.Pen(lip.ConvertAll(p => (p.Item1 + 0.15, p.Item2 + 0.25)), 0.28, tones.Lit, seed + 3, 0.1, 0.1);
        }
        ink.Pen(line, width, tones.Ink, seed, 0.1, 0.1);
        if (stage >= 1) ink.Pen(branch, 0.34, tones.Ink, seed + 1, 0.05, 0.5);
        if (stage >= 2) ink.Pen(branch2, 0.3, tones.Ink, seed + 2, 0.05, 0.5);
        (ink.Shift, ink.Box) = ((0, 0), null);
    }
}
