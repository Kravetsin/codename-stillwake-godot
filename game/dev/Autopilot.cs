using System.Collections.Generic;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Dev;

/// <summary>A script for recordings: every doorway opened at tick 20 and shut at tick 40, so shadows and doors are seen to follow.</summary>
public static class Autopilot
{
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
