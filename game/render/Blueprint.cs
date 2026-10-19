using System;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Render;

/// <summary>How plans join the plans beside them when drawn (features/building/blueprint.ts).</summary>
public static class Blueprint
{
    /// <summary>Which sides of a cell: up, right, down, left, a bit each, as the ship's pictures are numbered.</summary>
    public const int Up = 1, Right = 2, Down = 4, Left = 8;

    /// <summary>
    /// The sides of <paramref name="cell"/> whose neighbour <paramref name="joins"/>, as bits: where a
    /// planned pipe reaches, a planned door joins and a planned wall runs on.
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
    /// <paramref name="size"/> cells, where <paramref name="joins"/> says its like stands: a door the straight run of its
    /// doorway, as the sim toggles it (<see cref="Doorways.LinksOf"/>); anything else every side.
    /// </summary>
    public static int LinksOf(Placement place, int cell, int width, int size, Func<int, bool> joins) => place.Kind == "door"
        ? Doorways.LinksOf(cell, width, size, c => joins(c) ? Structure.DoorClosed : Structure.Empty)
        : LinksOf(cell, width, joins);

    /// <summary>
    /// The sides of a wall at <paramref name="cell"/>, of a grid <paramref name="width"/> wide and <paramref name="size"/>
    /// cells, that run on into a wall beside it: one whose own side facing it is whole (0069), as the grid draws built
    /// walls; <paramref name="shapeAt"/> the shape of the wall at a cell, planned or built, null for none.
    /// </summary>
    public static int WallLinksOf(int cell, int width, int size, Func<int, int?> shapeAt)
    {
        int col = cell % width, sides = 0;
        bool Closes(int n, int facing) => n >= 0 && n < size && shapeAt(n) is int shape && (Shape.ClosedSides(shape) & facing) != 0;
        if (Closes(cell - width, Down)) sides |= Up;
        if (col < width - 1 && Closes(cell + 1, Left)) sides |= Right;
        if (Closes(cell + width, Up)) sides |= Down;
        if (col > 0 && Closes(cell - 1, Right)) sides |= Left;
        return sides;
    }

    /// <summary>Whether two placements join when drawn side by side: pipes and valves, doors, or walls of any material, as the grid draws them built.</summary>
    public static bool Joins(Placement a, Placement b) =>
        a.Layer == "conduit" ? b.Layer == "conduit" : a.Kind is "door" or "wall" && b.Kind == a.Kind;
}
