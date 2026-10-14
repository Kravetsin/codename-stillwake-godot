using System;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Building;

/// <summary>How plans join the plans beside them when drawn (features/building/blueprint.ts).</summary>
public static class Blueprint
{
    /// <summary>Which sides of a cell: up, right, down, left, a bit each, as the ship's pictures are numbered.</summary>
    public const int Up = 1, Right = 2, Down = 4, Left = 8;

    /// <summary>
    /// The sides of <paramref name="cell"/> whose neighbour <paramref name="joins"/>, as bits: where a
    /// planned pipe reaches and a planned door joins.
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

    /// <summary>Whether two placements join when drawn side by side: pipes and valves, or doors.</summary>
    public static bool Joins(Placement a, Placement b) =>
        a.Layer == "conduit" ? b.Layer == "conduit" : a.Kind == "door" && b.Kind == "door";
}
