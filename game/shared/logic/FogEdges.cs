using System;
using Stillwake.Content;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Shared;

/// <summary>
/// A planet's fog at its sides (fog-edges), as the client reads it from the grid: the <see cref="Fog"/> columns at either side
/// outside the build zone, where a hero glides, and the <see cref="Out"/> columns past each inside it, the fog thick there, where a
/// hero moved across glides on; their seams, vertical lines at each band's middle, in thousandths of a cell; the period between
/// them, which a hero crossing a seam outward is moved by to the other; and where it then arrives. One source for the hero, the
/// fog drawn and the sound.
/// </summary>
/// <param name="Width">The grid's width, in cells.</param>
/// <param name="Fog">Columns in each band: the zone's left column.</param>
/// <param name="Drop">Rows over the ground a moved hero's feet arrive at (<c>Ground.FogDrop</c>).</param>
/// <param name="Out">Columns past a band a moved hero glides on through before it falls (<c>Ground.FogOut</c>).</param>
/// <param name="LeftLine">The left band's ground line beside the zone, its first row with a back wall: grown from the seed alone.</param>
/// <param name="RightLine">The right band's, likewise.</param>
public sealed record FogEdges(int Width, int Fog, int Drop, int Out, int LeftLine, int RightLine)
{
    private const int Unit = 1000;

    /// <summary>
    /// The bands of <paramref name="grid"/> under <paramref name="rules"/>: a planet's whose zone leaves the same columns
    /// at each side; none aboard, on a test map, or on a planet saved before it had bands, whose zone is the whole grid.
    /// </summary>
    public static FogEdges? Of(IGridQueries grid, Surfaces.Kind? rules)
    {
        if (rules is not PlanetType type) return null;
        var zone = grid.Zone;
        int fog = zone.Left;
        if (fog <= 0 || grid.Width - 1 - zone.Right != fog) return null;
        return new(grid.Width, fog, type.Ground.FogDrop, type.Ground.FogOut, LineOf(grid, fog - 1), LineOf(grid, grid.Width - fog));
    }

    private static int LineOf(IGridQueries grid, int x)
    {
        var back = grid.BackLayer;
        for (int y = 0; y < grid.Height; y++)
            if (back[y * grid.Width + x] != Back.None) return y;
        return grid.Height;
    }

    /// <summary>The left band's seam, in thousandths of a cell.</summary>
    public int LeftSeam => Fog / 2 * Unit;

    /// <summary>The right band's seam: a shift by <see cref="Period"/> puts a point on one seam exactly on the other.</summary>
    public int RightSeam => (Width - Fog / 2) * Unit;

    /// <summary>The distance between the seams, in thousandths of a cell.</summary>
    public int Period => RightSeam - LeftSeam;

    /// <summary>Whether a body centre at <paramref name="x"/>, in thousandths of a cell, lies in a band: out of the zone.</summary>
    public bool Outside(int x) => Within(x, Fog);

    /// <summary>Whether a body centre at <paramref name="x"/> lies in a band or the <see cref="Out"/> columns past it, where a moved hero glides on.</summary>
    public bool Carries(int x) => Within(x, Fog + Out);

    private bool Within(int x, int columns)
    {
        int column = x >= 0 ? x / Unit : -1;
        return column < columns || column > Width - 1 - columns;
    }

    /// <summary>
    /// What a step from <paramref name="fromX"/> to <paramref name="toX"/> is moved by, in thousandths of a cell: the period
    /// when it crosses the left seam going left, less it when it crosses the right seam going right, else nothing. Standing
    /// on a seam, or turning back before it, is no crossing.
    /// </summary>
    public int Shift(int fromX, int toX) =>
        fromX >= LeftSeam && toX < LeftSeam ? Period
        : fromX <= RightSeam && toX > RightSeam ? -Period
        : 0;

    /// <summary>
    /// The feet of a hero moved by <paramref name="shift"/>: <see cref="Drop"/> rows over the highest ground it glides on over,
    /// the band's ground line beside the zone and the tops of the <see cref="Out"/> columns past it as <paramref name="topOf"/>
    /// reads them now (built on or dug as they are); in the band it lands in, the right after a positive shift, the left after a
    /// negative; never with its head past the grid's top.
    /// </summary>
    public int Arrive(int shift, Func<int, int> topOf)
    {
        bool right = shift > 0;
        int high = right ? RightLine : LeftLine;
        for (int i = 0; i < Out; i++) high = Math.Min(high, topOf(right ? Width - 1 - Fog - i : Fog + i));
        return Math.Max((high - Drop) * Unit, Stillwake.Content.Hero.Height);
    }
}
