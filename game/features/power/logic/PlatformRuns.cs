using System.Collections.Generic;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Landing;
using Stillwake.Sim.Power;

namespace Stillwake.Game.Features.Power;

/// <summary>
/// The launch platforms on a surface, run by run (<see cref="StructureRuns"/>), and whether each one's lamp is green: its plug, its
/// left end, where every cable to it ends, fed by the power (<see cref="IPowerQueries.Feeds"/>).
/// </summary>
public static class PlatformRuns
{
    /// <summary>Every run within what is built, shut or open, in reading order: its first cell and how many cells it runs.</summary>
    public static List<(int First, int Length)> Of(IGridQueries grid)
    {
        var runs = new List<(int First, int Length)>();
        var box = grid.Built;
        int width = grid.Width;
        for (int y = box.Top; y <= box.Bottom; y++)
            for (int x = box.Left; x <= box.Right; x++)
            {
                int cell = y * width + x;
                if (!Doorways.IsPlatform(grid.StructureAt(cell)) || (x > 0 && Doorways.IsPlatform(grid.StructureAt(cell - 1)))) continue;
                runs.Add(LandingModule.RunOf(grid, cell));
            }
        return runs;
    }

    /// <summary>Whether <paramref name="run"/>'s lamp is green: its plug fed.</summary>
    public static bool Lit(IPowerQueries power, (int First, int Length) run) => power.Feeds(run.First);
}
