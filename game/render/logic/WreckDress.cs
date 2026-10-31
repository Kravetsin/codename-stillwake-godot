using System.Collections.Generic;
using Stillwake.Content;
using Stillwake.Sim;

namespace Stillwake.Game.Render;

/// <summary>
/// How the wreck's original plates show their age (<see cref="Wreck.Dress"/>), a rule of drawing, never the sim's: rust and stripes over the wall, and a
/// torn cell's own picture while the sides open round it are those it was drawn with (2026-10-31-the-scoop).
/// </summary>
public static class WreckDress
{
    /// <summary>The sides of a cell, as <c>GridView</c> counts them.</summary>
    private const int Up = 1, Right = 2, Down = 4, Left = 8;

    /// <summary>The material code that is dressed.</summary>
    public static readonly int Code = Materials.Get("wreckScrap").Code;

    /// <summary>Where the placed maps' corner lies in a grid of this size: a new world centres them, a dev world of the wreck alone holds them as they are.</summary>
    public static (int X, int Y) CornerIn(int width, int height) =>
        width == Wreck.Width && height == Wreck.Height ? (0, 0) : Layout.OffsetInWorld(Wreck.Width, Wreck.Height);

    /// <summary>The drawing's cell of a grid cell at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    public static (int X, int Y) DrawnAt(int x, int y, (int X, int Y) corner) => (x - corner.X - Wreck.Left, y - corner.Y - Wreck.Top);

    /// <summary>The dress of a cell of the drawing, a space for none: past its map, as on a plate of a saved world the map no longer names.</summary>
    public static char At(int x, int y)
    {
        int row = y + Wreck.Top, column = x + Wreck.Left;
        return row >= 0 && row < Wreck.Dress.Count && column >= 0 && column < Wreck.Dress[row].Length ? Wreck.Dress[row][column] : ' ';
    }

    /// <summary>The sides of a cell of the drawing with no wall drawn beside it: what its tear was drawn for.</summary>
    public static int OpenAsDrawn(int x, int y) =>
        (IsWall(x, y - 1) ? 0 : Up) | (IsWall(x + 1, y) ? 0 : Right) | (IsWall(x, y + 1) ? 0 : Down) | (IsWall(x - 1, y) ? 0 : Left);

    /// <summary>Whether a torn cell shows its tear with <paramref name="open"/> sides open now; a neighbour built or taken away ends it.</summary>
    public static bool ShowsTear(int x, int y, int open) => At(x, y) == 'T' && open == OpenAsDrawn(x, y);

    private static bool IsWall(int x, int y)
    {
        int row = y + Wreck.Top, column = x + Wreck.Left;
        return row >= 0 && row < Wreck.Rows.Count && column >= 0 && column < Wreck.Rows[row].Length && Wreck.Rows[row][column] == '#';
    }

    /// <summary>A rust or stripes picture's name.</summary>
    public static string OverName(int x, int y) => $"wreck_over_{x}_{y}";

    /// <summary>A torn cell's picture's name.</summary>
    public static string TornName(int x, int y) => $"wreck_torn_{x}_{y}";

    /// <summary>The cells of the drawing that are dressed, with their look.</summary>
    public static IEnumerable<(int X, int Y, char Look)> Cells()
    {
        for (int row = 0; row < Wreck.Dress.Count; row++)
            for (int column = 0; column < Wreck.Dress[row].Length; column++)
                if (Wreck.Dress[row][column] != ' ') yield return (column - Wreck.Left, row - Wreck.Top, Wreck.Dress[row][column]);
    }

    /// <summary>
    /// A torn cell's RGBA8 pixels: the wall's with the mask laid over, where a clear pixel keeps the wall, magenta cuts it to nothing, and any other
    /// colour paints it.
    /// </summary>
    public static byte[] Torn(byte[] wall, byte[] mask)
    {
        var torn = (byte[])wall.Clone();
        for (int i = 0; i < torn.Length; i += 4)
        {
            if (mask[i + 3] == 0) continue;
            bool cut = mask[i] == 255 && mask[i + 1] == 0 && mask[i + 2] == 255;
            for (int k = 0; k < 4; k++) torn[i + k] = cut ? (byte)0 : mask[i + k];
        }
        return torn;
    }
}
