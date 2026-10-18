using System.Collections.Generic;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Dev;

/// <summary>A script for recordings: every doorway toggled at tick 20 and again at tick 40, so shadows and doors are seen to follow.</summary>
public static class Autopilot
{
    public static IReadOnlyList<Command> At(int tick, IGridQueries grid)
    {
        if (tick != 20 && tick != 40) return [];
        var toggles = new List<Command>();
        var s = grid.StructureLayer;
        int w = grid.Width;
        bool IsDoor(int c) => c >= 0 && c < grid.StructureLayer.Length && grid.StructureLayer[c] is Structure.DoorClosed or Structure.DoorOpen;
        for (int cell = 0; cell < s.Length; cell++)
            if (IsDoor(cell) && DoorShape.Leads(Blueprint.LinksOf(cell, w, IsDoor))) toggles.Add(new ToggleDoor(cell));
        return toggles;
    }
}
