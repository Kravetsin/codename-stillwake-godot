using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Features.Pipes;

/// <summary>A link of pipe oxygen is drawn flowing along: from the middle of one cell to the next, in cells; <c>Steps</c> is how far along the pipes its start is.</summary>
public readonly record struct Run(double X, double Y, int Dx, int Dy, int Steps);

/// <summary>Oxygen as the pipes overlay shows it flowing (features/pipes/flow.ts, oxygen.ts).</summary>
public static class Flow
{
    /// <summary>Networks take these in turn in the overlay, so neighbours rarely share one.</summary>
    public static readonly uint[] Palette = [0x4fc3f7, 0xffb74d, 0xba68c8, 0x81c784, 0xf06292, 0xfff176, 0x4db6ac, 0xa1887f];

    /// <summary>The overlay colour of the <paramref name="index"/>-th network, as 0xRRGGBB.</summary>
    public static uint ColourOf(int index) => Palette[index % Palette.Length];

    /// <summary>
    /// How far along the pipes each cell of a network is from where oxygen comes into it: 0 at each
    /// source, one more each cell along, by the fewest cells. Cells no source reaches are left out.
    /// Oxygen is drawn flowing from lower to higher.
    /// </summary>
    public static Dictionary<int, int> StepsFrom(IReadOnlyList<int> cells, int width, IReadOnlyList<int> sources)
    {
        var own = cells.ToHashSet();
        var steps = new Dictionary<int, int>();
        var front = sources.Where(own.Contains).ToList();
        foreach (int cell in front) steps[cell] = 0;
        for (int d = 1; front.Count > 0; d++)
        {
            var next = new List<int>();
            foreach (int cell in front)
            {
                int x = cell % width;
                foreach (int n in (int[])[x > 0 ? cell - 1 : -1, x < width - 1 ? cell + 1 : -1, cell - width, cell + width])
                {
                    if (!own.Contains(n) || steps.ContainsKey(n)) continue;
                    steps[n] = d;
                    next.Add(n);
                }
            }
            front = next;
        }
        return steps;
    }

    /// <summary>Every link from a cell to a neighbour one step farther from the sources, in cells.</summary>
    public static List<Run> RunsOf(IReadOnlyDictionary<int, int> steps, int width)
    {
        var runs = new List<Run>();
        foreach (var (cell, d) in steps)
        {
            int x = cell % width, y = cell / width;
            foreach (var (dx, dy) in ((int, int)[])[(1, 0), (-1, 0), (0, 1), (0, -1)])
            {
                if ((dx == 1 && x == width - 1) || (dx == -1 && x == 0)) continue;
                if (steps.TryGetValue(cell + dy * width + dx, out int there) && there == d + 1)
                    runs.Add(new Run(x + 0.5, y + 0.5, dx, dy, d));
            }
        }
        return runs;
    }

    /// <summary>
    /// Where dots flowing along <paramref name="runs"/> are when the flow has gone <paramref name="along"/>
    /// cells: one every <paramref name="every"/> cells, each moving on a cell as <paramref name="along"/>
    /// grows by one, so they run smoothly through every bend.
    /// </summary>
    public static List<(double X, double Y)> DotsOf(IReadOnlyList<Run> runs, double along, int every)
    {
        var dots = new List<(double X, double Y)>();
        foreach (var run in runs)
        {
            double u = ((along - run.Steps) % every + every) % every;
            if (u >= 1) continue;
            dots.Add((run.X + run.Dx * u, run.Y + run.Dy * u));
        }
        return dots;
    }
}
