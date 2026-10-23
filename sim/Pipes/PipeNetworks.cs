namespace Stillwake.Sim.Pipes;

/// <summary>How pipe networks are found from the spans, and how a map's drawn pipes become spans (0109).</summary>
internal static class PipeNetworks
{
    /// <summary>
    /// Splits the spans' ends into networks: components of the nodes that carry, joined by spans. A closed valve carries
    /// nothing, so it splits its spans apart and belongs to no network. A machine joins every network one of its ports is a
    /// node of. <paramref name="machines"/> come in id order, so each network's do too; a network's id is its smallest cell.
    /// </summary>
    public static (Dictionary<int, int> NetworkOf, List<Network> Networks) Find(
        IReadOnlyList<PipeSpan> spans, IReadOnlyList<Valve> valves, IReadOnlyList<(int Id, IReadOnlyList<int> Ports)> machines)
    {
        var closed = new HashSet<int>();
        foreach (var v in valves)
            if (!v.Open) closed.Add(v.Cell);
        // Union-find over the nodes, always joining toward the smaller cell, so each root is its component's smallest.
        var parent = new Dictionary<int, int>();
        int Root(int c)
        {
            while (parent[c] != c) c = parent[c] = parent[parent[c]];
            return c;
        }
        var nodes = new List<int>();
        foreach (var s in spans)
            foreach (int end in (int[])[s.A, s.B])
                if (!closed.Contains(end) && parent.TryAdd(end, end)) nodes.Add(end);
        foreach (var s in spans)
        {
            if (closed.Contains(s.A) || closed.Contains(s.B)) continue;
            int a = Root(s.A), b = Root(s.B);
            if (a != b) parent[Math.Max(a, b)] = Math.Min(a, b);
        }
        nodes.Sort();
        var networkOf = new Dictionary<int, int>();
        var cells = new Dictionary<int, List<int>>();
        var ids = new List<int>();
        foreach (int node in nodes)
        {
            int root = Root(node);
            networkOf[node] = root;
            if (!cells.TryGetValue(root, out var list))
            {
                cells[root] = list = [];
                ids.Add(root);
            }
            list.Add(node);
        }
        var joined = ids.ToDictionary(id => id, _ => new List<int>());
        foreach (var (id, ports) in machines)
        {
            var seen = new List<int>();
            foreach (int port in ports)
                if (networkOf.TryGetValue(port, out int network) && !seen.Contains(network))
                {
                    seen.Add(network);
                    joined[network].Add(id);
                }
        }
        // Roots are smallest cells, and nodes were taken in cell order: the ids are ascending.
        return (networkOf, ids.Select(id => new Network(id, cells[id], joined[id])).ToList());
    }

    /// <summary>
    /// The spans and valves of a map's drawn pipes (<c>-</c>, <c>v</c> open, <c>x</c> closed, and <c>-</c> under machines): every
    /// cell that is a valve, a machine's port, an end, a corner or a branch is a node, and each straight run between two nodes a
    /// span, cut by a joint every <see cref="Content.Piping.MaxSpan"/> cells. A pipe joining nothing is dropped. They count as paid.
    /// </summary>
    public static (List<PipeSpan> Spans, List<Valve> Valves) FromLayout(Layout layout)
    {
        int width = layout.Width, height = layout.Height, size = width * height;
        var spans = new List<PipeSpan>();
        var valves = new List<Valve>();
        if (size == 0 || layout.Rows.Count == 0) return (spans, valves);
        var pipe = new bool[size];
        for (int cell = 0; cell < size; cell++)
        {
            char c = layout.At(cell);
            pipe[cell] = c is '-' or 'v' or 'x';
            if (c is 'v' or 'x') valves.Add(new Valve(cell, c == 'v', true));
        }
        foreach (int cell in layout.CellsOf('-', under: true)) pipe[cell] = true;
        var ports = new HashSet<int>();
        foreach (var (kind, cell) in layout.Machines())
            foreach (var (px, py) in Content.Machines.Get(kind).Ports)
                ports.Add(cell + py * width + px);
        bool Pipe(int x, int y) => x >= 0 && y >= 0 && x < width && y < height && pipe[y * width + x];
        bool Node(int cell)
        {
            int x = cell % width, y = cell / width;
            bool l = Pipe(x - 1, y), r = Pipe(x + 1, y), u = Pipe(x, y - 1), d = Pipe(x, y + 1);
            bool straight = l && r && !u && !d || u && d && !l && !r;
            return !straight || ports.Contains(cell) || valves.Exists(v => v.Cell == cell);
        }
        for (int cell = 0; cell < size; cell++)
        {
            if (!pipe[cell] || !Node(cell)) continue;
            int x = cell % width, y = cell / width;
            // Each run is walked once, from its node nearer the top left: rightward or down.
            foreach (var (dx, dy) in ((int, int)[])[(1, 0), (0, 1)])
            {
                if (!Pipe(x + dx, y + dy)) continue;
                int k = 1;
                while (!Node((y + k * dy) * width + x + k * dx)) k++;
                // A run longer than a span is cut by joints every MaxSpan cells from its start.
                int from = cell;
                for (int step = Content.Piping.MaxSpan; ; step += Content.Piping.MaxSpan)
                {
                    int to = step >= k ? (y + k * dy) * width + x + k * dx : (y + step * dy) * width + x + step * dx;
                    spans.Add(new PipeSpan(from, to, true));
                    if (step >= k) break;
                    from = to;
                }
            }
        }
        spans.Sort((p, q) => p.A != q.A ? p.A.CompareTo(q.A) : p.B.CompareTo(q.B));
        // A valve on no span holds nothing.
        valves.RemoveAll(v => !spans.Exists(s => s.A == v.Cell || s.B == v.Cell));
        return (spans, valves);
    }
}
