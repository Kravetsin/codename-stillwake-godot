using Stillwake.Content;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Rooms;

namespace Stillwake.Sim.Power;

/// <summary>A cable between two nodes, named by their cells, the lower first.</summary>
/// <param name="Paid">Whether its cost was taken from whoever laid it, and so is given back when it is cut.</param>
public sealed record Cable(int A, int B, bool Paid);

/// <summary>A cable as a command names it: the cells of its two ends, in either order.</summary>
public sealed record Ends(int A, int B);

/// <summary>A network: the nodes cables join, named by its lowest node cell, and how its power stands.</summary>
/// <param name="Nodes">Its node cells, ascending.</param>
/// <param name="Supply">Power its makers make a tick.</param>
/// <param name="Demand">Power its users would draw a tick.</param>
/// <param name="Satisfaction">What share of its draw each user gets, in thousandths: all slowed alike when short.</param>
public sealed record PowerNetwork(int Id, IReadOnlyList<int> Nodes, int Supply, int Demand, int Satisfaction);

/// <summary>Nodes, spans and networks (packages/sim/src/modules/power/nodes.ts).</summary>
internal static class PowerNodes
{
    public static MachineSpec Spec(Machine m) => Content.Machines.Get(m.Kind);
    public static bool IsPowered(MachineSpec s) => s.Makes != null || s.Draws != null;

    /// <summary>
    /// The node a cable laid at <paramref name="cell"/> ends on: a machine that makes or draws power,
    /// named by its top-left cell; or an anchor, the cell itself, where a wall, a floor, a door or a back
    /// wall stands and no machine does. Nothing anywhere else.
    /// </summary>
    public static int? NodeAt(IGridQueries grid, int cell)
    {
        if (cell < 0 || cell >= grid.Width * grid.Height) return null;
        var machine = grid.MachineAt(cell);
        if (machine != null) return IsPowered(Spec(machine)) ? machine.Cell : null;
        bool mounted = grid.StructureAt(cell) != Structure.Empty || grid.BackLayer[cell] == Back.Wall;
        return mounted ? cell : null;
    }

    /// <summary>The cells a node covers, as a box: a machine's footprint, or an anchor's one cell.</summary>
    private static (int X0, int Y0, int X1, int Y1) BoxOf(IGridQueries grid, int node)
    {
        int x = node % grid.Width, y = node / grid.Width;
        var machine = grid.MachineAt(node);
        var (w, h) = machine != null && machine.Cell == node ? (Spec(machine).Width, Spec(machine).Height) : (1, 1);
        return (x, y, x + w - 1, y + h - 1);
    }

    /// <summary>Whether two nodes' nearest cells are within a cable's span: straight, in whole cells.</summary>
    public static bool InSpan(IGridQueries grid, int a, int b)
    {
        var p = BoxOf(grid, a);
        var q = BoxOf(grid, b);
        int dx = Math.Max(0, Math.Max(q.X0 - p.X1, p.X0 - q.X1));
        int dy = Math.Max(0, Math.Max(q.Y0 - p.Y1, p.Y0 - q.Y1));
        return dx * dx + dy * dy <= Content.Power.MaxSpan * Content.Power.MaxSpan;
    }

    /// <summary>A pair of nodes as a cable names them, the lower first.</summary>
    public static (int A, int B) Ordered(int a, int b) => a < b ? (a, b) : (b, a);

    /// <summary>A total order on cables: by their first node, then their second.</summary>
    public static int ByEnds(Cable p, Cable q) => p.A != q.A ? p.A.CompareTo(q.A) : p.B.CompareTo(q.B);

    /// <summary>Whether a machine making power sees the stars: every cell of it in space with no back wall behind it.</summary>
    private static bool UnderStars(IGridQueries grid, IRoomsQueries rooms, Machine m)
    {
        var s = Spec(m);
        int x = m.Cell % grid.Width, y = m.Cell / grid.Width;
        for (int dy = 0; dy < s.Height; dy++)
            for (int dx = 0; dx < s.Width; dx++)
            {
                int cell = (y + dy) * grid.Width + x + dx;
                if (rooms.RoomAt(cell) != RoomsModule.Space || grid.BackAt(cell) != null) return false;
            }
        return true;
    }

    /// <summary>
    /// The networks the cables join, in id order, with each user's satisfaction by machine id. Supply is
    /// what the makers make, each only while <paramref name="making"/> says it works: one that burns fuel
    /// anywhere, any other under the stars; demand what the users draw now, as <paramref name="draw"/> says: none
    /// while one rests, its modules folded in (0086). Satisfaction is <c>floor(1000 × supply / demand)</c>, at
    /// most 1000, and 1000 with nothing drawn. A powered machine with no cable is on no network.
    /// </summary>
    public static (List<PowerNetwork> Networks, Dictionary<int, int> Satisfaction) Find(
        IGridQueries grid, IRoomsQueries rooms, IReadOnlyList<Cable> cables, Func<Machine, int> draw, Func<Machine, bool> making)
    {
        var ends = new List<int>(cables.Count * 2);
        foreach (var c in cables) { ends.Add(c.A); ends.Add(c.B); }
        ends.Sort();
        var nodes = ends.Distinct().ToArray();

        // Union-find over the nodes' indices, ascending by cell: the lowest stays the root, so a
        // network's id is its lowest node. Tens of nodes, so no path halving.
        var parent = new int[nodes.Length];
        for (int i = 0; i < parent.Length; i++) parent[i] = i;
        int Root(int i)
        {
            while (parent[i] != i) i = parent[i];
            return i;
        }
        foreach (var c in cables)
        {
            int ra = Root(Array.BinarySearch(nodes, c.A)), rb = Root(Array.BinarySearch(nodes, c.B));
            if (ra != rb) parent[Math.Max(ra, rb)] = Math.Min(ra, rb);
        }

        // A root is its network's lowest node, so networks come in id order.
        var networkOf = new int[nodes.Length];
        var lists = new List<List<int>>();
        for (int i = 0; i < nodes.Length; i++)
        {
            int r = Root(i);
            if (r == i) { networkOf[i] = lists.Count; lists.Add([nodes[i]]); }
            else { networkOf[i] = networkOf[r]; lists[networkOf[r]].Add(nodes[i]); }
        }

        var supply = new int[lists.Count];
        var demand = new int[lists.Count];
        var users = new List<(int Id, int Network)>();
        // A network's machines are those whose top-left a cable ends on: found from the ends, not every machine.
        for (int at = 0; at < nodes.Length; at++)
        {
            var m = grid.MachineAt(nodes[at]);
            if (m == null || m.Cell != nodes[at]) continue;
            var s = Spec(m);
            if (!IsPowered(s)) continue;
            int network = networkOf[at];
            // A fire needs no stars: its module rests it while it is out.
            if (s.Makes is int makes && making(m) && (s.FuelSlot != null || UnderStars(grid, rooms, m))) supply[network] += makes;
            if (s.Draws != null)
            {
                demand[network] += draw(m);
                users.Add((m.Id, network));
            }
        }

        var networks = new List<PowerNetwork>(lists.Count);
        for (int n = 0; n < lists.Count; n++)
        {
            // Both are never negative, so integer division is the floor.
            int satisfaction = demand[n] == 0 ? 1000 : Math.Min(1000, 1000 * supply[n] / demand[n]);
            networks.Add(new PowerNetwork(lists[n][0], lists[n], supply[n], demand[n], satisfaction));
        }
        var byMachine = new Dictionary<int, int>(users.Count);
        foreach (var (id, network) in users) byMachine[id] = networks[network].Satisfaction;
        return (networks, byMachine);
    }
}
