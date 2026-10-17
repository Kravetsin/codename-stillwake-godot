using System.Text.Json.Serialization;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Rooms;
using static Stillwake.Sim.Power.PowerNodes;

namespace Stillwake.Sim.Power;

/// <summary>Lays a cable between the nodes at cells <c>A</c> and <c>B</c>, paid by the sender.</summary>
public sealed record Lay(int A, int B) : Command;

/// <summary>Takes the cable between node cells <c>A</c> and <c>B</c> away, giving back what was paid.</summary>
public sealed record Cut(int A, int B) : Command;

/// <summary>Moves every cable ending at node <c>Node</c> (TypeScript's <c>from</c>), or only <c>Cable</c>, to the node at cell <c>To</c>.</summary>
public sealed record Move(int Node, int To, Ends? Cable = null) : Command;

/// <summary>
/// The power slice. Public so modules can be typed by it, its members internal: others go through
/// <see cref="IPowerQueries"/> and <see cref="IPowerOps"/>.
/// </summary>
public sealed class PowerState
{
    /// <summary>Every cable, ordered by its ends.</summary>
    [JsonInclude] internal List<Cable> Cables { get; set; } = [];
    /// <summary>The machines told to rest by the module they work for, by id, sorted: they draw and make nothing.</summary>
    [JsonInclude] internal List<int> Resting { get; set; } = [];
    /// <summary>Derived: the networks the cables join, in id order.</summary>
    [JsonIgnore] internal List<PowerNetwork> Networks { get; set; } = [];
    /// <summary>Derived: each cabled user's satisfaction in thousandths, by machine id; only looked up.</summary>
    [JsonIgnore] internal Dictionary<int, int> Satisfaction { get; set; } = new();
}

/// <summary>What any module may read of the power. Lists are live: a reader never writes them.</summary>
public interface IPowerQueries
{
    /// <summary>Every cable, ordered by its ends.</summary>
    IReadOnlyList<Cable> Cables { get; }
    /// <summary>The networks the cables join, in id order.</summary>
    IReadOnlyList<PowerNetwork> Networks { get; }
    /// <summary>The node a cable laid at <paramref name="cell"/> would end on, or null.</summary>
    int? NodeAt(int cell);
    /// <summary>The network node <paramref name="node"/> is on, if a cable ends there.</summary>
    PowerNetwork? NetworkOf(int node);
    /// <summary>Whether a cable ends on node <paramref name="node"/>: a machine there is not taken apart.</summary>
    bool Cabled(int node);
    /// <summary>The share of its draw machine <paramref name="machineId"/> gets, in thousandths; 0 off every network.</summary>
    int Satisfaction(int machineId);
    /// <summary>
    /// Whether charging station <paramref name="machineId"/> fills what it holds this tick: an item short of its
    /// capacity, on a network that gives it power. What its light and its panel's bolt show.
    /// </summary>
    bool Charges(int machineId);
    /// <summary>Why peer <paramref name="peer"/>'s <see cref="Lay"/> from <paramref name="a"/> to <paramref name="b"/> would be refused: what the cable ghost shows.</summary>
    string? LayProblem(int peer, int a, int b);
    /// <summary>Why a <see cref="Move"/> from node <paramref name="from"/> to cell <paramref name="to"/> would be refused: what a dragged end shows.</summary>
    string? MoveProblem(int from, int to, Ends? cable = null);
}

/// <summary>How a module that depends on the power may change it.</summary>
public interface IPowerOps
{
    /// <summary>Tells machine <paramref name="machineId"/> to rest, drawing or making nothing from the next derivation on, or to work again.</summary>
    void Rest(int machineId, bool resting);
}

/// <summary>
/// Electricity: cables between nodes, the networks they join, and what share of its users' draw each
/// network can give (packages/sim/src/modules/power).
/// </summary>
public sealed partial class PowerModule : Module<PowerState>, IPowerQueries, IPowerOps
{
    private readonly IGridQueries _grid;
    private readonly IRoomsQueries _rooms;
    private readonly IItemsQueries _items;
    private readonly IItemsOps _itemsOps;
    /// <summary>Made once, so the tick's derivation allocates no delegates.</summary>
    private readonly Predicate<int> _standing;
    private readonly Func<Machine, bool> _drawing, _making;

    public override string Id => "power";

    public PowerModule(IGridQueries grid, IRoomsQueries rooms, IItemsQueries items, IItemsOps itemsOps)
    {
        _grid = grid;
        _rooms = rooms;
        _items = items;
        _itemsOps = itemsOps;
        _standing = id => _grid.MachineById(id) != null;
        _drawing = Drawing;
        _making = m => !IsResting(m.Id);
        Handles<Lay>(c => LayProblem(c.From, c.A, c.B), ApplyLay);
        Handles<Move>(c => MoveProblem(c.Node, c.To, c.Cable), ApplyMove);
        Handles<Cut>(CutProblem, ApplyCut);
        // After derivation, at this tick's satisfaction: a charging station fills what it holds.
        System(Phase.Simulation, Charge);
        // Every tick: a wall built or broken moves a panel in or out of space, a machine comes or goes.
        System(Phase.Derivation, () =>
        {
            // A machine taken apart rests no more: nothing else tells it to work again.
            if (!State.Resting.TrueForAll(_standing)) State.Resting = State.Resting.FindAll(_standing);
            Rebuild();
        });
    }

    protected override PowerState Init(Layout layout) => new() { Cables = StartingCables(layout) };

    public override void Rebuild() => (State.Networks, State.Satisfaction) = Find(_grid, _rooms, State.Cables, _drawing, _making);

    /// <summary>
    /// The cables a map lays, each end resolved to its node as <see cref="Lay"/> would: a machine drawn
    /// there, by its top-left, or the cell. A map whose cable ends on nothing a cable can end on does not load.
    /// </summary>
    private static List<Cable> StartingCables(Layout layout)
    {
        var machines = layout.Machines();
        int Resolve(int cell)
        {
            var none = new FormatException($"Map cell {cell}: a cable ends on no node.");
            if (cell < 0 || cell >= layout.Width * layout.Height) throw none;
            int cx = cell % layout.Width, cy = cell / layout.Width;
            foreach (var (kind, top) in machines)
            {
                var s = Content.Machines.Get(kind);
                int x = top % layout.Width, y = top / layout.Width;
                if (cx < x || cx >= x + s.Width || cy < y || cy >= y + s.Height) continue;
                return IsPowered(s) ? top : throw none;
            }
            // Every character but a space has a back wall to mount on, a machine's included.
            return layout.At(cell) == ' ' ? throw none : cell;
        }
        var cables = (layout.Cables ?? [])
            .Select(c => Ordered(Resolve(c.A), Resolve(c.B)))
            .Select(e => new Cable(e.A, e.B, true))
            .ToList();
        cables.Sort(ByEnds);
        return cables;
    }

    /// <summary>
    /// Whether a user draws now: one that works on what is put in it only while its first slot holds
    /// something, a charging station only while that is short of full, and none told to rest.
    /// </summary>
    private bool Drawing(Machine m)
    {
        if (IsResting(m.Id)) return false;
        var spec = Spec(m);
        if (spec.Slots == null) return true;
        return _items.Crate(m.Id)[0] is { } held && (!spec.Charges || Short(held));
    }

    /// <summary>Whether <paramref name="held"/> holds less charge than it can: a battery not yet full.</summary>
    private static bool Short(Stack held) => Content.Items.Get(held.Item).Capacity is int full && held.Charge < full;

    /// <summary>The kinds of machine that charge what they hold.</summary>
    private static readonly string[] Chargers = [.. Content.Machines.All.Where(s => s.Charges).Select(s => s.Kind)];

    /// <summary>
    /// Each charging station, in id order, puts what it draws into what it holds: its <c>Draws</c> in
    /// power-ticks times its satisfaction, stored scaled (<c>Charge.Scale</c>), rounded down, never past
    /// full. Drawing 3, it fills a battery of 1800 in 600 ticks at full power, twice that at half.
    /// </summary>
    private void Charge()
    {
        foreach (var kind in Chargers)
            foreach (var m in _grid.MachinesOf(kind))
            {
                if (!Charges(m.Id)) continue;
                // Both are never negative, so integer division is the floor.
                int amount = (Spec(m).Draws ?? 0) * Content.Charge.Scale * Satisfaction(m.Id) / 1000;
                if (amount > 0) _itemsOps.Charge(m.Id, amount);
            }
    }

    private bool IsResting(int machineId) => State.Resting.BinarySearch(machineId) >= 0;

    // Queries.

    public IReadOnlyList<Cable> Cables => State.Cables;
    public IReadOnlyList<PowerNetwork> Networks => State.Networks;
    public int? NodeAt(int cell) => PowerNodes.NodeAt(_grid, cell);
    public PowerNetwork? NetworkOf(int node) => State.Networks.Find(n => n.Nodes.Contains(node));
    public bool Cabled(int node) => State.Cables.Exists(c => c.A == node || c.B == node);
    public int Satisfaction(int machineId) => State.Satisfaction.TryGetValue(machineId, out int s) ? s : 0;

    public bool Charges(int machineId) =>
        _grid.MachineById(machineId) is { } m && Spec(m).Charges && Drawing(m) && Satisfaction(machineId) > 0;

    public string? LayProblem(int peer, int a, int b)
    {
        var p = NodeAt(a);
        var q = NodeAt(b);
        if (p == null || q == null) return "power.noNode";
        if (p == q) return "power.sameNode";
        if (!InSpan(_grid, p.Value, q.Value)) return "power.tooLong";
        var ends = Ordered(p.Value, q.Value);
        if (State.Cables.Exists(c => c.A == ends.A && c.B == ends.B)) return "power.twice";
        return _items.ShortOf(peer, Content.Power.CableCost);
    }

    /// <summary>
    /// Why cables cannot move from node <paramref name="from"/> to the node at cell <paramref name="to"/>:
    /// none ends there, no node is there, one would end where it starts or join a pair already joined,
    /// or one would be pulled taut, its ends farther apart than a cable spans.
    /// </summary>
    public string? MoveProblem(int from, int to, Ends? cable = null)
    {
        var taken = Moved(from, cable);
        if (taken.Count == 0) return "power.noCable";
        var target = NodeAt(to);
        if (target == null) return "power.noNode";
        // Cables are unique by their ends, so value equality picks out exactly the ones taken.
        var pairs = State.Cables.Where(c => !taken.Contains(c)).Select(c => (c.A, c.B)).ToList();
        foreach (var c in taken)
        {
            int other = c.A == from ? c.B : c.A;
            if (other == target) return "power.sameNode";
            var ends = Ordered(other, target.Value);
            if (pairs.Contains(ends)) return "power.twice";
            pairs.Add(ends);
            if (!InSpan(_grid, other, target.Value)) return "power.taut";
        }
        return null;
    }

    /// <summary>The cables a move from node <paramref name="from"/> takes: the one named, or every one ending there.</summary>
    private List<Cable> Moved(int from, Ends? cable)
    {
        var named = cable == null ? ((int A, int B)?)null : Ordered(cable.A, cable.B);
        return State.Cables.FindAll(c =>
            (c.A == from || c.B == from) && (named == null || (c.A == named.Value.A && c.B == named.Value.B)));
    }

    // Operations.

    public void Rest(int machineId, bool resting)
    {
        int at = State.Resting.BinarySearch(machineId);
        if ((at >= 0) == resting) return;
        if (resting) State.Resting.Insert(~at, machineId);
        else State.Resting.RemoveAt(at);
    }

    // Commands.

    private void ApplyLay(Lay c)
    {
        var ends = Ordered(NodeAt(c.A)!.Value, NodeAt(c.B)!.Value);
        var taken = _itemsOps.Take(c.From, Content.Power.CableCost);
        State.Cables.Add(new Cable(ends.A, ends.B, taken.Count > 0));
        State.Cables.Sort(ByEnds);
    }

    private void ApplyMove(Move c)
    {
        int target = NodeAt(c.To)!.Value;
        var taken = Moved(c.Node, c.Cable);
        State.Cables = State.Cables.Select(cable =>
        {
            if (!taken.Contains(cable)) return cable;
            var ends = Ordered(cable.A == c.Node ? cable.B : cable.A, target);
            return new Cable(ends.A, ends.B, cable.Paid);
        }).ToList();
        State.Cables.Sort(ByEnds);
    }

    private Cable? CableAt(int a, int b)
    {
        var ends = Ordered(a, b);
        return State.Cables.Find(c => c.A == ends.A && c.B == ends.B);
    }

    private string? CutProblem(Cut c)
    {
        var cable = CableAt(c.A, c.B);
        if (cable == null) return "power.noCable";
        return cable.Paid ? _items.FullFor(c.From, Content.Power.CableCost) : null;
    }

    private void ApplyCut(Cut c)
    {
        var cable = CableAt(c.A, c.B)!;
        if (cable.Paid) _itemsOps.Give(c.From, Content.Power.CableCost);
        State.Cables.Remove(cable);
    }
}
