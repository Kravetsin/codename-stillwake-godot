using System;
using System.Collections.Generic;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Shared;

/// <summary>
/// The cells a strike covers (the brush; building has none): a square <see cref="Spec.Builds.Brush"/> cells a side round the
/// cell corner nearest the pointer, or one cell in precise mode, Ctrl held. Each cell is sent as its own command, in
/// ascending order, each judged alone by the sim. Written for any side, so it is 1 at 16 px cells and 2 at 8 px.
/// </summary>
public static class Brush
{
    /// <summary>The side of the brush, in cells: one cell when <paramref name="precise"/>, else <see cref="Spec.Builds.Brush"/>.</summary>
    public static int SideOf(bool precise) => precise ? 1 : Spec.Builds.Brush;

    /// <summary>
    /// The top-left column and row of a brush <paramref name="w"/> by <paramref name="h"/> cells round <paramref name="p"/>, in
    /// cells, kept on a grid <paramref name="width"/> by <paramref name="height"/>: an even side round the cell line nearest the
    /// point, an odd side centred on the point's cell, so a side of 1 is the cell under it.
    /// </summary>
    public static (int X, int Y) Corner(int width, int height, (double X, double Y) p, int w, int h) =>
        (Math.Clamp((int)Math.Floor(p.X - (w - 1) / 2.0), 0, Math.Max(0, width - w)),
         Math.Clamp((int)Math.Floor(p.Y - (h - 1) / 2.0), 0, Math.Max(0, height - h)));

    /// <summary>The cells of a brush <paramref name="w"/> by <paramref name="h"/> from top-left <paramref name="corner"/>, ascending, none off the grid.</summary>
    public static List<int> CellsFrom(int width, int height, (int X, int Y) corner, int w, int h)
    {
        var cells = new List<int>(w * h);
        for (int y = corner.Y; y < Math.Min(corner.Y + h, height); y++)
            for (int x = corner.X; x < Math.Min(corner.X + w, width); x++)
                cells.Add(y * width + x);
        return cells;
    }

    /// <summary>The cells of a square brush <paramref name="side"/> cells across round <paramref name="p"/>, in cells, ascending (<see cref="Corner"/>).</summary>
    public static List<int> Cells(int width, int height, (double X, double Y) p, int side) =>
        CellsFrom(width, height, Corner(width, height, p, side, side), side, side);
}
