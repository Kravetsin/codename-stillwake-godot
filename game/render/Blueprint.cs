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
    /// The arms of a pipe toward <paramref name="sides"/>, as rectangles (x, y, w, h) of a cell <paramref name="cell"/>
    /// pixels square: from its <paramref name="joint"/> out to the cell's edge, as wide as the joint. A built pipe's stub
    /// toward plans is drawn only there, its joint left to the grid, so the pipe still reads as built.
    /// </summary>
    public static List<(int X, int Y, int W, int H)> ArmsOf(int sides, (int X, int Y, int W, int H) joint, int cell)
    {
        var arms = new List<(int X, int Y, int W, int H)>();
        int right = joint.X + joint.W, bottom = joint.Y + joint.H;
        if ((sides & Up) != 0) arms.Add((joint.X, 0, joint.W, joint.Y));
        if ((sides & Right) != 0) arms.Add((right, joint.Y, cell - right, joint.H));
        if ((sides & Down) != 0) arms.Add((joint.X, bottom, joint.W, cell - bottom));
        if ((sides & Left) != 0) arms.Add((0, joint.Y, joint.X, joint.H));
        return arms;
    }

    /// <summary>
    /// The sides <paramref name="place"/> at <paramref name="cell"/> joins, of a grid <paramref name="width"/> wide and
    /// <paramref name="size"/> cells, where <paramref name="joins"/> says its like stands: a door the straight run of its
    /// doorway, as the sim toggles it (<see cref="Doorways.LinksOf"/>); anything else every side.
    /// </summary>
    public static int LinksOf(Placement place, int cell, int width, int size, Func<int, bool> joins) => place.Kind == "door"
        ? Doorways.LinksOf(cell, width, size, c => joins(c) ? Structure.DoorClosed : Structure.Empty)
        : LinksOf(cell, width, joins);


    /// <summary>Whether two placements join when drawn side by side: pipes and valves, doors, or walls of any material, as the grid draws them built.</summary>
    public static bool Joins(Placement a, Placement b) =>
        a.Layer == "conduit" ? b.Layer == "conduit" : a.Kind is "door" or "wall" && b.Kind == a.Kind;
}
