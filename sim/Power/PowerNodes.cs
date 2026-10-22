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
/// <param name="Stored">Power-ticks its stores hold as the tick begins (0101).</param>
/// <param name="Capacity">What its stores hold full.</param>
/// <param name="Flow">What goes into its stores this tick, in power-ticks; below 0, what they give out.</param>
/// <param name="Intake">What its stores could take in this tick: each its <c>In</c>, or the room it has left if less.</param>
/// <param name="Steady">What of its supply its makers that burn nothing make: its panels under the stars.</param>
public sealed record PowerNetwork(
    int Id, IReadOnlyList<int> Nodes, int Supply, int Demand, int Satisfaction, int Stored = 0, int Capacity = 0, int Flow = 0, int Intake = 0, int Steady = 0);

/// <summary>A store on a network as it settles a tick: what it holds as the tick begins, and its limits.</summary>
internal readonly record struct Banked(int Energy, StoreSpec Spec);

/// <summary>What store <c>Machine</c> holds, in power-ticks, above 0 (0101).</summary>
public sealed record StoreCharge(int Machine, int Energy);

/// <summary>A store as its picture and the overlay show it: what it holds of full, and what goes in this tick, out below 0.</summary>
public sealed record StoreLevel(int Energy, int Capacity, int Flow);

/// <summary>Nodes, spans and networks (packages/sim/src/modules/power/nodes.ts).</summary>
internal static class PowerNodes
{
    public static MachineSpec Spec(Machine m) => Content.Machines.Get(m.Kind);
    public static bool IsPowered(MachineSpec s) => s.Powered;

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
    /// while one rests, its modules folded in (0086). Its stores, holding what <paramref name="stored"/> says by machine
    /// id, settle the tick (<see cref="Settle"/>, 0101); satisfaction is <c>floor(1000 × served / demand)</c>, served
    /// being supply and what the stores gave, at most 1000, and 1000 with nothing drawn. Flows are each cabled store's
    /// this tick, by machine id: in above 0, out below. A powered machine with no cable is on no network.
    /// </summary>
    public static (List<PowerNetwork> Networks, Dictionary<int, int> Satisfaction, List<(int Machine, int Flow)> Flows) Find(
        IGridQueries grid, IRoomsQueries rooms, IReadOnlyList<Cable> cables, Func<Machine, int> draw, Func<Machine, bool> making,
        Func<int, int> stored)
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
        var steady = new int[lists.Count];
        var users = new List<(int Id, int Network)>();
        // In node order, so a network's stores come in the reading order of their cells.
        var stores = new List<(int Id, int Network, Banked Banked)>();
        // A network's machines are those whose top-left a cable ends on: found from the ends, not every machine.
        for (int at = 0; at < nodes.Length; at++)
        {
            var m = grid.MachineAt(nodes[at]);
            if (m == null || m.Cell != nodes[at]) continue;
            var s = Spec(m);
            if (!IsPowered(s)) continue;
            int network = networkOf[at];
            // A fire needs no stars: its module rests it while it is out.
            if (s.Makes is int makes && making(m) && (s.FuelSlot != null || UnderStars(grid, rooms, m)))
            {
                supply[network] += makes;
                if (s.FuelSlot == null) steady[network] += makes;
            }
            if (s.Draws != null)
            {
                demand[network] += draw(m);
                users.Add((m.Id, network));
            }
            if (s.Store is { } store) stores.Add((m.Id, network, new Banked(stored(m.Id), store)));
        }

        var networks = new List<PowerNetwork>(lists.Count);
        var flows = new List<(int Machine, int Flow)>(stores.Count);
        var held = new List<Banked>();
        var ids = new List<int>();
        var flowed = new int[stores.Count];
        for (int n = 0; n < lists.Count; n++)
        {
            held.Clear();
            ids.Clear();
            foreach (var (id, network, h) in stores)
                if (network == n) { held.Add(h); ids.Add(id); }
            var flow = flowed.AsSpan(0, held.Count);
            int served = Settle(supply[n], demand[n], held, flow);
            int energy = 0, capacity = 0, moved = 0, intake = 0;
            for (int i = 0; i < held.Count; i++)
            {
                flows.Add((ids[i], flow[i]));
                (energy, capacity, moved, intake) = (energy + held[i].Energy, capacity + held[i].Spec.Capacity, moved + flow[i], intake + Intake(held[i]));
            }
            // Both are never negative, so integer division is the floor.
            int satisfaction = demand[n] == 0 ? 1000 : Math.Min(1000, 1000 * served / demand[n]);
            networks.Add(new PowerNetwork(lists[n][0], lists[n], supply[n], demand[n], satisfaction, energy, capacity, moved, intake, steady[n]));
        }
        var byMachine = new Dictionary<int, int>(users.Count);
        foreach (var (id, network) in users) byMachine[id] = networks[network].Satisfaction;
        return (networks, byMachine, flows);
    }

    /// <summary>What store <paramref name="h"/> could take in this tick: its <c>In</c>, or the room it has left if less.</summary>
    public static int Intake(Banked h) => Math.Max(0, Math.Min(h.Spec.In, h.Spec.Capacity - h.Energy));

    /// <summary>
    /// How a network's <paramref name="stores"/> settle a tick (0101), in the order given: with <paramref name="supply"/> at
    /// or above <paramref name="demand"/>, what is spare goes into them, each up to its <see cref="Intake"/>, the rest wasted;
    /// short, they give out, each up to its <c>Out</c> and what it holds, until the shortfall is met. Writes each store's
    /// flow into <paramref name="flows"/>, in above 0 and out below, and returns what the users are served: supply and
    /// what the stores gave. A store never takes from another: only spare supply goes in.
    /// </summary>
    public static int Settle(int supply, int demand, IReadOnlyList<Banked> stores, Span<int> flows)
    {
        bool spare = supply >= demand;
        int left = spare ? supply - demand : demand - supply, served = supply;
        for (int i = 0; i < stores.Count; i++)
        {
            var h = stores[i];
            int moved = Math.Min(left, spare ? Intake(h) : Math.Min(h.Spec.Out, h.Energy));
            flows[i] = spare ? moved : -moved;
            left -= moved;
            if (!spare) served += moved;
        }
        return served;
    }
}
