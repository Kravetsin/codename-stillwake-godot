using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Debris;

namespace Stillwake.Sim.Tests;

/// <summary>Hits aimed as tests mean them: the fan of rays cast from inside a cell or a wreck, so it strikes that one.</summary>
public static class HewTesting
{
    /// <summary>Straight up (<see cref="Headings"/>).</summary>
    public const int Up = 192;

    /// <summary>
    /// A hit of <paramref name="tool"/> from the middle of <paramref name="cell"/>, aimed at <paramref name="heading"/> or, with
    /// none, the first heading from straight up round the turn whose fan strikes what is there alone: a wall there by itself (every
    /// ray stops in it at once), a plant there by itself, or, with <paramref name="Back"/>, that cell's back wall by itself; where
    /// none does, or nothing is there, straight up. Off the grid, nothing is struck. On <paramref name="surface"/>'s grid.
    /// </summary>
    public static Hew Dig(Scenario w, int cell, string tool, bool Back = false, int? heading = null, int surface = 0)
    {
        var m = w.On(surface);
        int width = m.Grid.Width;
        var (x, y) = cell < 0 ? (-500, 500) : (cell % width * 1000 + 500, cell / width * 1000 + 500);
        return new Hew(tool, x, y, heading ?? AloneFrom(m, cell, tool, x, y, Back), Back);
    }

    /// <summary>
    /// The first heading from straight up whose fan from (<paramref name="x"/>, <paramref name="y"/>) strikes <paramref name="cell"/>'s
    /// own thing alone; for a plant, failing that, the first that strikes no other plant, walls only cracked besides.
    /// </summary>
    private static int AloneFrom(Registry m, int cell, string tool, int x, int y, bool back)
    {
        var building = m.Building;
        bool open = cell >= 0 && cell < m.Grid.Width * m.Grid.Height && m.Grid.WallAt(cell) == null;
        var plant = open && !back ? building.PlantAt(cell) : null;
        int? amongWalls = null;
        for (int step = 0; step < Headings.Turn; step++)
        {
            int heading = (Up + step) & (Headings.Turn - 1);
            var struck = building.Struck(tool, x, y, heading, back);
            if (plant == null)
            {
                if (struck.Plants.Count == 0 && struck.Cells.Count == 1 && struck.Cells[0] == cell) return heading;
                continue;
            }
            if (struck.Plants.Count != 1 || struck.Plants[0] != plant) continue;
            if (struck.Cells.Count == 0) return heading;
            amongWalls ??= heading;
        }
        return amongWalls ?? Up;
    }

    /// <summary>A hit of <paramref name="tool"/> from the middle of piece <paramref name="id"/> (a far corner of the grid when gone): it strikes that wreck.</summary>
    public static HewWrecks Hit(Scenario w, int id, string tool)
    {
        var piece = w.M.Debris.Pieces.FirstOrDefault(p => p.Id == id);
        return piece == null ? new HewWrecks(tool, 500, 500, 0) : new HewWrecks(tool, piece.X, piece.Y, 0);
    }

    /// <summary>Why <see cref="Dig"/>'s hit would be refused, as the client asks it.</summary>
    public static string? DigProblem(Scenario w, int peer, int cell, string tool, bool Back = false, int? heading = null)
    {
        var hew = Dig(w, cell, tool, Back, heading);
        return w.M.Building.HewProblem(peer, tool, hew.X, hew.Y, hew.Heading, Back);
    }

    /// <summary>Why <see cref="Hit"/>'s hit would be refused, as the client asks it.</summary>
    public static string? HitProblem(Scenario w, int peer, int id, string tool)
    {
        var hew = Hit(w, id, tool);
        return w.M.Debris.HewProblem(peer, tool, hew.X, hew.Y, hew.Heading);
    }
}
