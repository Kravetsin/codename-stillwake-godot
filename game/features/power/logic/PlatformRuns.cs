using System.Collections.Generic;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Power;

namespace Stillwake.Game.Features.Power;

/// <summary>
/// The launch platforms on a surface, run by run (<see cref="StructureRuns"/>), and whether each one's lamp is green: any of its
/// cells fed by the power (<see cref="IPowerQueries.Feeds"/>), as a cable may end on any of them.
/// </summary>
public static class PlatformRuns
{
    /// <summary>The structure code of a launch platform's deck.</summary>
    public static readonly int Code = Content.Structures.Placed(Content.Builds.Platform)!.Value;

    /// <summary>Every run within what is built, in reading order: its first cell and how many cells it runs.</summary>
    public static List<(int First, int Length)> Of(IGridQueries grid)
    {
        var runs = new List<(int First, int Length)>();
        var box = grid.Built;
        int width = grid.Width;
        for (int y = box.Top; y <= box.Bottom; y++)
            for (int x = box.Left; x <= box.Right; x++)
            {
                int cell = y * width + x;
                if (grid.StructureAt(cell) != Code || (x > 0 && grid.StructureAt(cell - 1) == Code)) continue;
                runs.Add(StructureRuns.RunOf(cell, width, grid.StructureAt));
            }
        return runs;
    }

    /// <summary>Whether <paramref name="run"/>'s lamp is green: a cell of it fed.</summary>
    public static bool Lit(IPowerQueries power, (int First, int Length) run)
    {
        for (int i = 0; i < run.Length; i++)
            if (power.Feeds(run.First + i)) return true;
        return false;
    }
}
