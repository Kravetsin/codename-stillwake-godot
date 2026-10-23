using System;
using System.Collections.Generic;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Render;

/// <summary>How plans join the plans beside them when drawn (features/building/blueprint.ts).</summary>
public static class Blueprint
{
    /// <summary>Which sides of a cell: up, right, down, left, a bit each, as the ship's pictures are numbered.</summary>
    public const int Up = 1, Right = 2, Down = 4, Left = 8;

    /// <summary>
    /// The sides of <paramref name="cell"/> whose neighbour <paramref name="joins"/>, as bits: where a
    /// planned door joins and a planned wall runs on.
    /// </summary>
    public static int LinksOf(int cell, int width, Func<int, bool> joins)
    {
        int col = cell % width, sides = 0;
        if (joins(cell - width)) sides |= Up;
        if (col < width - 1 && joins(cell + 1)) sides |= Right;
        if (joins(cell + width)) sides |= Down;
        if (col > 0 && joins(cell - 1)) sides |= Left;
        return sides;
    }

    /// <summary>
    /// The sides <paramref name="place"/> at <paramref name="cell"/> joins, of a grid <paramref name="width"/> wide and
    /// <paramref name="size"/> cells, where <paramref name="joins"/> says its like stands: every side. A door is planned a
    /// piece whole (0104), joining nothing.
    /// </summary>
    public static int LinksOf(Placement place, int cell, int width, int size, Func<int, bool> joins) => LinksOf(cell, width, joins);


    /// <summary>Whether two placements join when drawn side by side: walls of any material, as the grid draws them built.</summary>
    public static bool Joins(Placement a, Placement b) => a.Kind == "wall" && b.Kind == a.Kind;
}
