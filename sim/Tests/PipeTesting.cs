using Stillwake.Sim.Pipes;

namespace Stillwake.Sim.Tests;

/// <summary>Pipes laid in tests (0109): a span between each two cells given in turn, through joints where they meet.</summary>
public static class PipeTesting
{
    /// <summary>Lays a span from each of <paramref name="cells"/> to the next, sent as peer 0, unrun.</summary>
    public static Scenario Pipe(this Scenario w, params int[] cells)
    {
        for (int i = 0; i + 1 < cells.Length; i++) w.Do(new LayPipe(cells[i], cells[i + 1]));
        return w;
    }
}
