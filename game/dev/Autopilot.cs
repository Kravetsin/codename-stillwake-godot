using System.Collections.Generic;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Dev;

/// <summary>A script for recordings: every doorway opened at tick 20 and shut at tick 40, so shadows and doors are seen to follow.</summary>
public static class Autopilot
{
    /// <summary><c>-- --segments</c>: a door of three segments in a room's wall and a hatch of one in its floor (0104), for <c>--doors</c>.</summary>
    public static readonly Layout Segments = Layout.Parse(
    [
        "#############",
        "#.LL..#..LL.#",
        "#.LL..D..LL.#",
        "#.....D.....#",
        "#.....D..S..#",
        "###D#########",
        "#...........#",
        "#############",
    ]);

    public static IReadOnlyList<Command> At(int tick, IGridQueries grid)
    {
        if (tick != 20 && tick != 40) return [];
        var toggles = new List<Command>();
        var s = grid.StructureLayer;
        int w = grid.Width;
        for (int cell = 0; cell < s.Length; cell++)
            if (Doorways.IsDoor(s[cell]) && DoorShape.Leads(Doorways.LinksOf(cell, w, s.Length, grid.StructureAt))) toggles.Add(new SetDoor(cell, tick == 20));
        return toggles;
    }
}
