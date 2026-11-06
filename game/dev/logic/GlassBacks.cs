using System.Collections.Generic;
using System.Linq;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Rooms;

namespace Stillwake.Game.Dev;

/// <summary>
/// A dev world's rooms behind glass: their vaporizers are few for rooms this big, and glass leaks a fifth of what scrap
/// does (leaks-tripled). Laid straight through the grid as the world is made, before anything runs on it.
/// </summary>
public static class GlassBacks
{
    /// <summary>Rebuilds in glass every back wall behind a cell of a room on the world these <paramref name="modules"/> run.</summary>
    public static void Lay(IReadOnlyList<Module> modules)
    {
        var grid = modules.OfType<GridModule>().Single();
        var rooms = modules.OfType<RoomsModule>().Single();
        var cells = new List<int>();
        for (int cell = 0; cell < rooms.RoomOfCells.Length; cell++)
            if (rooms.RoomOfCells[cell] >= 0 && grid.BackAt(cell) != null) cells.Add(cell);
        var glass = Placement.BackWall("glass");
        foreach (int cell in cells)
        {
            grid.Clear(cell, "back");
            grid.Place(cell, glass);
        }
    }
}
