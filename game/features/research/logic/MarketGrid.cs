using System;

namespace Stillwake.Game.Features.Research;

/// <summary>Where the market's parts lie in the view: the order down the left, the tabs over the tiles, and the tiles ([market-grid](../../../../docs/decisions/2026-11-03-market-grid.md)).</summary>
public static class MarketGrid
{
    /// <summary>The order's width and its gap to the tiles; the tabs' height; the gap between tiles; the least a tile is, wide and tall.</summary>
    public const int SideWide = 234, SideGap = 10, TabsTall = 30, Gap = 4, LeastWide = 75, LeastTall = 85;

    /// <summary>How many tiles go across and down a grid <paramref name="wide"/> by <paramref name="tall"/>, and each tile's size: they fill it exactly, never under the least.</summary>
    public static (int Columns, int Rows, float Wide, float Tall) Of(float wide, float tall)
    {
        int columns = Math.Max(1, (int)((wide + Gap) / (LeastWide + Gap))), rows = Math.Max(1, (int)((tall + Gap) / (LeastTall + Gap)));
        return (columns, rows, (wide - Gap * (columns - 1)) / columns, (tall - Gap * (rows - 1)) / rows);
    }

    /// <summary>The grid's size in a view <paramref name="wide"/> by <paramref name="tall"/>: what the order and the tabs leave.</summary>
    public static (float Wide, float Tall) GridIn(float wide, float tall) => (wide - SideWide - SideGap, tall - TabsTall);

    /// <summary>How many tiles a view <paramref name="wide"/> by <paramref name="tall"/> holds.</summary>
    public static int Capacity(float wide, float tall)
    {
        var (gridWide, gridTall) = GridIn(wide, tall);
        var (columns, rows, _, _) = Of(gridWide, gridTall);
        return columns * rows;
    }
}
