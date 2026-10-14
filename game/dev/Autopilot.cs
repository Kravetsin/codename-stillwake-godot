using System.Collections.Generic;
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
        for (int cell = 0; cell < s.Length; cell++)
        {
            bool door = s[cell] is Structure.DoorClosed or Structure.DoorOpen;
            bool below = cell >= grid.Width && s[cell - grid.Width] is Structure.DoorClosed or Structure.DoorOpen;
            if (door && !below) toggles.Add(new ToggleDoor(cell));
        }
        return toggles;
    }
}
