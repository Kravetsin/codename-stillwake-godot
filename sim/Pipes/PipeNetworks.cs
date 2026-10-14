using Stillwake.Sim.Grid;

namespace Stillwake.Sim.Pipes;

/// <summary>The flood that finds pipe networks (packages/sim/src/modules/pipes/networks.ts).</summary>
internal static class PipeNetworks
{
    /// <summary>What <c>networkOf</c> holds for a cell in no network: no conduit, or a closed valve.</summary>
    public const int None = -1;

    /// <summary>
    /// Splits the conduits into networks: components of the cells that carry, joined side by side. A
    /// closed valve carries nothing, so it splits a run in two. A machine joins every network one of
    /// its ports stands on. <paramref name="machines"/> come in id order, so each network's do too.
    /// </summary>
    public static (int[] NetworkOf, List<Network> Networks) Find(
        int width, int height, ReadOnlySpan<int> conduit, IReadOnlyList<(int Id, IReadOnlyList<int> Ports)> machines)
    {
        int size = width * height;
        var carries = new bool[size];
        for (int cell = 0; cell < size; cell++) carries[cell] = conduit[cell] is Conduit.Pipe or Conduit.ValveOpen;
        var networkOf = new int[size];
        Array.Fill(networkOf, None);
        var cells = new List<int[]>();
        var index = new int[size];
        var queue = new int[size];

        for (int start = 0; start < size; start++)
        {
            if (!carries[start] || networkOf[start] != None) continue;
            networkOf[start] = start;
            queue[0] = start;
            int count = 1;
            for (int head = 0; head < count; head++)
            {
                int cell = queue[head], x = cell % width;
                if (x > 0) count = Visit(cell - 1, start, count);
                if (x < width - 1) count = Visit(cell + 1, start, count);
                if (cell >= width) count = Visit(cell - width, start, count);
                if (cell + width < size) count = Visit(cell + width, start, count);
            }
            var found = queue[..count];
            Array.Sort(found);
            index[start] = cells.Count;
            cells.Add(found);
        }

        var joined = cells.Select(_ => new List<int>()).ToList();
        foreach (var (id, ports) in machines)
        {
            var seen = new List<int>();
            foreach (int port in ports)
            {
                int network = networkOf[port];
                if (network == None || seen.Contains(network)) continue;
                seen.Add(network);
                joined[index[network]].Add(id);
            }
        }
        var networks = cells.Select((c, i) => new Network(c[0], c, joined[i])).ToList();
        return (networkOf, networks);

        int Visit(int next, int id, int count)
        {
            if (!carries[next] || networkOf[next] != None) return count;
            networkOf[next] = id;
            queue[count] = next;
            return count + 1;
        }
    }
}
