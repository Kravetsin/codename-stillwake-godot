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
    /// <summary>What each store holds, by machine id, sorted; only those holding something (0101).</summary>
    [JsonInclude] internal List<StoreCharge> Stores { get; set; } = [];
    /// <summary>The machines a magnetic flare broke, by id, sorted: they make, draw and store nothing till repaired.</summary>
    [JsonIgnore] internal List<int> Broken { get; set; } = [];
    /// <summary>Broken as saved: none written while none is, so a world no flare struck saves as before flares.</summary>
    [JsonInclude, JsonPropertyName("broken"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    internal List<int>? SavedBroken { get => Broken.Count > 0 ? Broken : null; set => Broken = value ?? []; }
    /// <summary>Derived: the networks the cables join, in id order.</summary>
    [JsonIgnore] internal List<PowerNetwork> Networks { get; set; } = [];
    /// <summary>Derived: each cabled user's satisfaction in thousandths, by machine id; only looked up.</summary>
    [JsonIgnore] internal Dictionary<int, int> Satisfaction { get; set; } = new();
    /// <summary>Derived: what goes into each cabled store this tick, out below 0, network by network.</summary>
    [JsonIgnore] internal List<(int Machine, int Flow)> Flows { get; set; } = [];
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
    /// <summary>The share of its draw machine <paramref name="machineId"/> gets, in thousandths; 0 off every network or broken.</summary>
    int Satisfaction(int machineId);
    /// <summary>
    /// Whether a structure a cable ends on at <paramref name="cell"/> has power enough: its network has some to give, made or
    /// stored, and covers all its machines draw. A launch platform's lamp, green; red uncabled, short, or on a network with none.
    /// </summary>
    bool Feeds(int cell);
    /// <summary>
    /// What machine <paramref name="machineId"/> draws while it draws: its <c>Draws</c> × (1000 + its modules' draw) / 1000,
    /// rounded up, a cost (0086); 0 for one drawing none. What its panel's bolt says.
    /// </summary>
    int Draw(int machineId);
    /// <summary>
    /// Whether charging station <paramref name="machineId"/> fills what it holds this tick: an item short of its
    /// capacity, on a network that gives it power. What its light and its panel's bolt show, a drone bay's too.
    /// </summary>
    bool Charges(int machineId);
    /// <summary>
    /// What store <paramref name="machineId"/> holds of full and what goes into it this tick, out below 0 (0101); null for a
    /// machine that stores nothing. What its gauge shows.
    /// </summary>
    StoreLevel? Store(int machineId);
    /// <summary>
    /// Whether a fire on machine <paramref name="machineId"/>'s network is wanted: it draws, or its stores could take in,
    /// more than its panels make (<see cref="PowerNetwork.Steady"/>, 0101). A burner generator lights its next fuel only
    /// then; one on no network never. Fires are not weighed against each other, so two never hand over a tick dark.
    /// </summary>
    bool Needed(int machineId);
    /// <summary>
    /// What charging station <paramref name="machineId"/> puts into what it holds this tick, stored scaled (<c>Charge.Scale</c>):
    /// its draw, its modules in it, times its share, rounded down; 0 while it charges nothing. What its panel's rate says.
    /// </summary>
    int ChargeRate(int machineId);
    /// <summary>The machines a magnetic flare broke, by id, sorted (2026-10-30-magnetic-flare).</summary>
    IReadOnlyList<int> Broken { get; }
    /// <summary>Whether machine <paramref name="machineId"/> is broken: it makes, draws and stores nothing till repaired.</summary>
    bool IsBroken(int machineId);
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

    /// <summary>
    /// Machine <paramref name="machineId"/> is about to be taken apart (0110): every cable plugged into it stays, its end a peg
    /// at the cell its plug is in.
    /// </summary>
    void Unplug(int machineId);

    /// <summary>Machine <paramref name="machineId"/> was just built (0110): every cable ending on a peg at its plug's cell plugs into it.</summary>
    void Plug(int machineId);

    /// <summary>
    /// Breaks powered machine <paramref name="machineId"/> (2026-10-30-magnetic-flare): every query sees it at once, the networks
    /// from the next derivation. Its cables, slots and what its store holds stay.
    /// </summary>
    void Break(int machineId);

    /// <summary>Machine <paramref name="machineId"/> is repaired: it works again, its networks from the next derivation.</summary>
    void Mend(int machineId);
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
    private readonly Func<Machine, int> _draw;
    private readonly Func<Machine, bool> _making;
    private readonly Func<int, int> _stored;
    private readonly Predicate<StoreCharge> _storeStanding;
    private readonly Func<int, bool> _broken;

    public override string Id => "power";

    public PowerModule(IGridQueries grid, IRoomsQueries rooms, IItemsQueries items, IItemsOps itemsOps)
    {
        _grid = grid;
        _rooms = rooms;
        _items = items;
        _itemsOps = itemsOps;
        _standing = id => _grid.MachineById(id) != null;
        _draw = m => Drawing(m) ? DrawOf(m) : 0;
        _making = m => !IsResting(m.Id);
        _stored = StoredIn;
        _storeStanding = s => _grid.MachineById(s.Machine) != null;
        _broken = IsBroken;
        Handles<Lay>(c => LayProblem(c.From, c.A, c.B), ApplyLay);
        Handles<Move>(c => MoveProblem(c.Node, c.To, c.Cable), ApplyMove);
        Handles<Cut>(CutProblem, ApplyCut);
        // After derivation, at this tick's satisfaction: a charging station fills what it holds, a store what it settled.
        System(Phase.Simulation, Charge);
        System(Phase.Simulation, Settle);
        // Every tick: a wall built or broken moves a panel in or out of space, a machine comes or goes.
        System(Phase.Derivation, () =>
        {
            // A machine taken apart rests no more: nothing else tells it to work again. A store taken apart loses what it held.
            if (!State.Resting.TrueForAll(_standing)) State.Resting = State.Resting.FindAll(_standing);
            if (!State.Stores.TrueForAll(_storeStanding)) State.Stores = State.Stores.FindAll(_storeStanding);
            if (!State.Broken.TrueForAll(_standing)) State.Broken = State.Broken.FindAll(_standing);
            Rebuild();
        });
    }

    protected override PowerState Init(Layout layout) => new() { Cables = StartingCables(layout) };

    public override void Rebuild() => (State.Networks, State.Satisfaction, State.Flows) = Find(_grid, _rooms, State.Cables, _draw, _making, _stored, _broken);

    /// <summary>Where store <paramref name="machineId"/> stands in <c>Stores</c>, or its complement where it would go.</summary>
    private int StoreIndex(int machineId)
    {
        var stores = State.Stores;
        int lo = 0, hi = stores.Count - 1;
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) >> 1);
            if (stores[mid].Machine == machineId) return mid;
            if (stores[mid].Machine < machineId) lo = mid + 1;
            else hi = mid - 1;
        }
        return ~lo;
    }

    /// <summary>What store <paramref name="machineId"/> holds, in power-ticks; 0 for one holding nothing.</summary>
    private int StoredIn(int machineId) => StoreIndex(machineId) is int at and >= 0 ? State.Stores[at].Energy : 0;

    /// <summary>
    /// Each cabled store takes in or gives out what its network settled in this tick's derivation (0101): read there, from what
    /// it held as the tick began, so a world loaded settles the same. Never below 0 or past full: <c>Settle</c> keeps to both.
    /// </summary>
    private void Settle()
    {
        foreach (var (machine, flow) in State.Flows)
        {
            if (flow == 0) continue;
            int at = StoreIndex(machine);
            int energy = (at >= 0 ? State.Stores[at].Energy : 0) + flow;
            if (energy < 0 || energy > Spec(_grid.MachineById(machine)!).Store!.Capacity)
                throw new InvalidOperationException($"Store {machine} would hold {energy}.");
            if (at < 0) State.Stores.Insert(~at, new StoreCharge(machine, energy));
            else if (energy == 0) State.Stores.RemoveAt(at);
            else State.Stores[at] = new StoreCharge(machine, energy);
        }
    }

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
            // A run with a plug, a launch platform's, is plugged at its left end, as NodeAt has it.
            if (Content.Structures.OfChar(layout.At(cell)) is int code && Content.Structures.Of(code).Plug != null)
                while (cx > 0 && Content.Structures.OfChar(layout.At(cell - 1)) is int left && Content.Structures.Of(left).Group == Content.Structures.Of(code).Group) (cell, cx) = (cell - 1, cx - 1);
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
    /// Whether a user draws now: none told to rest; a station (<c>Content.Stations</c>) whenever crafting does not rest
    /// it, whatever its inputs hold, for a queued one may have a paid unit to work; a charging station (the charger, a
    /// drone bay) while any slot holds something short of full; any other with slots (the engine) while its first
    /// slot holds something.
    /// </summary>
    private bool Drawing(Machine m)
    {
        if (IsResting(m.Id)) return false;
        var spec = Spec(m);
        if (spec.Slots == null || Content.Stations.TryGet(m.Kind, out _)) return true;
        var slots = _items.Crate(m.Id);
        if (!spec.Charges) return slots[0] != null;
        foreach (var held in slots)
            if (held != null && Short(held)) return true;
        return false;
    }

    /// <summary>Whether <paramref name="held"/> holds less charge than it can: a battery not yet full.</summary>
    private static bool Short(Stack held) => Content.Items.Get(held.Item).Capacity is int full && held.Charge < full;

    /// <summary>The kinds of machine that charge what they hold.</summary>
    private static readonly string[] Chargers = [.. Content.Machines.All.Where(s => s.Charges).Select(s => s.Kind)];

    /// <summary>
    /// Each charging station, in id order, puts what it draws into what it holds short of full: its <c>Draws</c> in
    /// power-ticks times its satisfaction, stored scaled (<c>Charge.Scale</c>), rounded down, shared evenly between
    /// those slots, the rest of the division one each to the first in slot order, never past full. Drawing 3, the
    /// charger fills a battery of 1800 in 600 ticks at full power, twice that at half; a bay shares its 4 between its drones.
    /// </summary>
    private void Charge()
    {
        foreach (var kind in Chargers)
            foreach (var m in _grid.MachinesOf(kind))
            {
                int amount = ChargeRate(m.Id);
                if (amount <= 0) continue;
                var slots = _items.Crate(m.Id);
                int shortOf = 0;
                foreach (var held in slots)
                    if (held != null && Short(held)) shortOf += 1;
                int share = amount / shortOf, rest = amount % shortOf, nth = 0;
                for (int slot = 0; slot < slots.Count; slot++)
                {
                    if (slots[slot] is not { } held || !Short(held)) continue;
                    _itemsOps.Charge(m.Id, slot, share + (nth++ < rest ? 1 : 0));
                }
            }
    }

    private bool IsResting(int machineId) => State.Resting.BinarySearch(machineId) >= 0;

    public bool IsBroken(int machineId) => State.Broken.BinarySearch(machineId) >= 0;

    /// <summary>What <paramref name="m"/> draws while it draws, its modules folded in, rounded up (0086).</summary>
    private int DrawOf(Machine m) => Spec(m).Draws is int draws ? Boost.Cost(draws, _items.Boost(m.Id).Draw) : 0;

    // Queries.

    public IReadOnlyList<Cable> Cables => State.Cables;
    public IReadOnlyList<PowerNetwork> Networks => State.Networks;
    public int? NodeAt(int cell) => PowerNodes.NodeAt(_grid, cell);
    public PowerNetwork? NetworkOf(int node) => State.Networks.Find(n => n.Nodes.Contains(node));
    public IReadOnlyList<int> Broken => State.Broken;

    // Broken checked here too: a flare strikes in construction, after this tick's networks were found.
    public int Satisfaction(int machineId) => !IsBroken(machineId) && State.Satisfaction.TryGetValue(machineId, out int s) ? s : 0;

    public bool Feeds(int cell) =>
        NodeAt(cell) == cell && NetworkOf(cell) is { } n && (n.Supply > 0 || n.Stored > 0) && n.Satisfaction >= 1000;
    public int Draw(int machineId) => _grid.MachineById(machineId) is { } m ? DrawOf(m) : 0;

    public bool Charges(int machineId) =>
        _grid.MachineById(machineId) is { } m && Spec(m).Charges && Drawing(m) && Satisfaction(machineId) > 0;

    public StoreLevel? Store(int machineId)
    {
        if (_grid.MachineById(machineId) is not { } m || Spec(m).Store is not { } spec) return null;
        int flow = State.Flows.Find(f => f.Machine == machineId).Flow;
        return new StoreLevel(StoredIn(machineId), spec.Capacity, flow);
    }

    public bool Needed(int machineId) =>
        !IsBroken(machineId) && _grid.MachineById(machineId) is { } m && NetworkOf(m.Cell) is { } network && network.Demand + network.Intake > network.Steady;

    // Both are never negative, so integer division is the floor.
    public int ChargeRate(int machineId) =>
        Charges(machineId) ? DrawOf(_grid.MachineById(machineId)!) * Content.Charge.Scale * Satisfaction(machineId) / 1000 : 0;

    public string? LayProblem(int peer, int a, int b)
    {
        // Cables are laid aboard only (0110): away from the ship the plan only builds.
        if (Rules != Content.Surfaces.Ship) return "power.notAboard";
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
        if (Rules != Content.Surfaces.Ship) return "power.notAboard";
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

    public void Unplug(int machineId)
    {
        var m = _grid.MachineById(machineId)!;
        Rehome(m.Cell, PlugCell(_grid, m));
        Mend(machineId);
    }

    public void Plug(int machineId)
    {
        var m = _grid.MachineById(machineId)!;
        if (IsPowered(Spec(m))) Rehome(PlugCell(_grid, m), m.Cell);
    }

    public void Break(int machineId)
    {
        if (_grid.MachineById(machineId) is not { } m || !IsPowered(Spec(m)))
            throw new InvalidOperationException($"Machine {machineId} is not a powered machine to break.");
        int at = State.Broken.BinarySearch(machineId);
        if (at < 0) State.Broken.Insert(~at, machineId);
    }

    public void Mend(int machineId)
    {
        int at = State.Broken.BinarySearch(machineId);
        if (at >= 0) State.Broken.RemoveAt(at);
    }

    /// <summary>
    /// Every cable ending at node <paramref name="from"/> to end at <paramref name="to"/> instead, in the order they lie; one that
    /// would then end where it starts or join a pair already joined stays where it was.
    /// </summary>
    private void Rehome(int from, int to)
    {
        if (from == to) return;
        var joined = State.Cables.Select(c => (c.A, c.B)).ToHashSet();
        for (int i = 0; i < State.Cables.Count; i++)
        {
            var cable = State.Cables[i];
            if (cable.A != from && cable.B != from) continue;
            int other = cable.A == from ? cable.B : cable.A;
            var ends = Ordered(other, to);
            if (other == to || !joined.Add(ends)) continue;
            State.Cables[i] = new Cable(ends.A, ends.B, cable.Paid);
        }
        State.Cables.Sort(ByEnds);
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
