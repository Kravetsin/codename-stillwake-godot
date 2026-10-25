using System;

namespace Stillwake.Game.Shared;

/// <summary>
/// Where a drag's stamp lies round the pointer (building's brush, a piece's size, 0104): an even side round the cell line
/// nearest the point, an odd side centred on the point's cell, kept on the grid. A tool's hit has no brush: it is a fan of rays.
/// </summary>
public static class Brush
{
    /// <summary>
    /// The top-left column and row of a stamp <paramref name="w"/> by <paramref name="h"/> cells round <paramref name="p"/>, in
    /// cells, kept on a grid <paramref name="width"/> by <paramref name="height"/>: an even side round the cell line nearest the
    /// point, an odd side centred on the point's cell, so a side of 1 is the cell under it.
    /// </summary>
    public static (int X, int Y) Corner(int width, int height, (double X, double Y) p, int w, int h) =>
        (Math.Clamp((int)Math.Floor(p.X - (w - 1) / 2.0), 0, Math.Max(0, width - w)),
         Math.Clamp((int)Math.Floor(p.Y - (h - 1) / 2.0), 0, Math.Max(0, height - h)));
}
