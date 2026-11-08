using Stillwake.Content;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Pipes;
using Stillwake.Sim.Power;
using Stillwake.Sim.Rooms;

namespace Stillwake.Sim.Atmosphere;

/// <summary>Puts one canister from the sender's inventory into tank <c>Tank</c>.</summary>
public sealed record LoadCanister(int Tank) : Command;

/// <summary>Takes <c>Count</c> canisters' liquid out of tank <c>Tank</c> as canisters into the sender's inventory, one at least (industrial-oxygen).</summary>
public sealed record DrawCanister(int Tank, int Count = 1) : Command;

/// <summary>What any module may read of the air.</summary>
public interface IAtmosphereQueries
{
    /// <summary>Whether a pipe joins vent <paramref name="vent"/> to a vaporizer's output: only then does its room's air grow round it.</summary>
    bool Fed(int vent);
    /// <summary>
    /// Whether vent <paramref name="vent"/> has air to give: gas in a store feeding it, or a vaporizer feeding it powered
    /// with liquid behind it. Only then does it blow and hum. One survey a call.
    /// </summary>
    bool Supplied(int vent);
    /// <summary>The liquid in tank <paramref name="tank"/>, or the gas in vaporizer <paramref name="tank"/>'s store; none for one never filled, or no tank at all.</summary>
    int OxygenIn(int tank);
    /// <summary>
    /// How fast air source <paramref name="source"/> (a vaporizer, the pod) makes air while its store has room, in thousandths of a
    /// unit a tick: none unpiped, with no liquid behind it or no power. What the pointer tells of it.
    /// </summary>
    int MakesAt(int source);
    /// <summary>Why peer <paramref name="peer"/>'s canister would not go into machine <paramref name="tank"/>: what the client shows first.</summary>
    string? CanisterProblem(int tank, int peer);
    /// <summary>Why peer <paramref name="peer"/> could not draw <paramref name="count"/> canisters out of machine <paramref name="tank"/>: no tank, fewer in it, or no room.</summary>
    string? DrawProblem(int tank, int peer, int count = 1);

    /// <summary>The liquid oxygen in the tanks whose output is on the network of machine <paramref name="machine"/>'s liquid input: what a blown fire takes from.</summary>
    int LiquidFor(int machine);
    /// <summary>Room <paramref name="room"/>'s state (TypeScript's <c>roomStatus</c>); vacuum for no room. One survey a call: for every room, read <see cref="RoomsAir"/>.</summary>
    RoomStatus StatusOf(int room);
    /// <summary>Whether <paramref name="cell"/> holds air.</summary>
    bool AiredAt(int cell);
    /// <summary>
    /// Whether the room <paramref name="cell"/> lies in holds any air: where drones fly, so a bubble shrinking or flowing
    /// never drops one (2026-11-05-air-bubbles). False outside every room.
    /// </summary>
    bool HoldsAir(int cell);
    /// <summary>Every cell holding air, sorted: live and read-only, for drawing the whole ship's air at once.</summary>
    IReadOnlyList<int> AiredCells { get; }
    /// <summary>Every room's air, in rooms order, for the air overlay: one survey for all rooms, so read it on a timer.</summary>
    IReadOnlyList<RoomAir> RoomsAir();
}

/// <summary>How a module that depends on the air may change it.</summary>
public interface IAtmosphereOps
{
    /// <summary>
    /// Takes up to <paramref name="amount"/> cells of air for a hero whose head is in <paramref name="cell"/>,
    /// or a furnace's fire there, from the far edge of its room's air, if that cell holds air; returns
    /// how many, none anywhere else. Emptied tanks leave the list at once.
    /// </summary>
    int Breathe(int cell, int amount);

    /// <summary>
    /// Puts up to <paramref name="amount"/> of liquid oxygen into tank <paramref name="tank"/>, as much as it has room for (an
    /// electrolyser's, water); returns how much. None into anything but a tank.
    /// </summary>
    int Fill(int tank, int amount);

    /// <summary>
    /// A fire burning unblown in machine cell <paramref name="cell"/> this tick reserves <c>Furnace.Reserve</c> of its room's budget
    /// (industrial-oxygen); called in the construction phase, cleared at its start.
    /// </summary>
    void Reserve(int cell);

    /// <summary>
    /// Takes <paramref name="amount"/> of liquid from the tanks behind machine <paramref name="machine"/>'s liquid input, in tank id
    /// order, if they hold that much; returns whether it did, taking none otherwise (a blown fire, industrial-oxygen).
    /// </summary>
    bool BurnLiquid(int machine, int amount);
}

/// <summary>
/// Oxygen: what the ship's tanks and generators hold, and how it fills rooms, is used in them and vents
/// into space (packages/sim/src/modules/atmosphere).
/// </summary>
public sealed partial class AtmosphereModule : Module<AtmosphereState>, IAtmosphereQueries, IAtmosphereOps
{
    private static readonly IReadOnlyList<(string Item, int Count)> Canister = [("canister", 1)];

    private readonly IGridQueries _grid;
    private readonly IItemsQueries _items;
    private readonly IItemsOps _itemsOps;
    private readonly IRoomsQueries _rooms;
    private readonly IPipesQueries _pipes;
    private readonly IPowerQueries _power;
    /// <summary>Whether a tank's machine still stands; made once, so construction allocates nothing a tick.</summary>
    private readonly Predicate<TankAir> _standing;
    /// <summary>Derived: the grid's version the tanks were last checked at; null for not yet.</summary>
    private int? _tanksCheckedAt;

    /// <summary>Scratch for the live vents listed each tick, so a tick in which none turned allocates nothing.</summary>
    private readonly List<int> _liveNow = [];

    /// <summary>Derived: the cells of the fires burning this tick, each reserving its share of its room's budget, in call order.</summary>
    private readonly List<int> _reserves = [];

    /// <summary>
    /// Derived: what each cell of a room leaks while it holds air, its back wall and the hull cells given to it, found for the
    /// rooms' version <see cref="_leaksFor"/>; null for none yet.
    /// </summary>
    private int[] _cellLeak = [];
    private int? _leaksFor;

    public override string Id => "atmosphere";

    public AtmosphereModule(
        IGridQueries grid, IItemsQueries items, IItemsOps itemsOps, IRoomsQueries rooms, IPipesQueries pipes,
        IPowerQueries power)
    {
        _grid = grid;
        _items = items;
        _rooms = rooms;
        _pipes = pipes;
        _power = power;
        _itemsOps = itemsOps;
        Handles<LoadCanister>(c => CanisterProblem(c.Tank, c.From), c =>
        {
            itemsOps.Take(c.From, Canister);
            Fill(c.Tank, Content.Air.Canister);
        });
        Handles<DrawCanister>(c => DrawProblem(c.Tank, c.From, c.Count), c =>
        {
            int count = Math.Max(1, c.Count);
            itemsOps.Give(c.From, [(Content.Crew.Canister, count)]);
            var held = State.Tanks.Find(t => t.Tank == c.Tank)!;
            held.Oxygen -= count * Content.Air.Canister;
            _readFor = null;
            DropEmptyTanks();
        });
        // A tank taken apart takes its oxygen with it; ids are never reused, so this is only tidying.
        // Only a machine taken apart leaves a tank with none: looked for when the grid's log names a machine's cell.
        _standing = t => _grid.MachineById(t.Tank) != null;
        System(Phase.Construction, () =>
        {
            // Before Furnace's construction, which reserves anew for the fires burning now.
            _reserves.Clear();
            if (_tanksCheckedAt == _grid.Version) return;
            bool moved = _tanksCheckedAt is not int at || MachinesMoved(_grid.ChangedSince(at));
            _tanksCheckedAt = _grid.Version;
            if (!moved || (State.Tanks.TrueForAll(_standing) && State.Own.TrueForAll(_standing))) return;
            State.Tanks = State.Tanks.FindAll(_standing);
            State.Own = State.Own.FindAll(_standing);
            _readFor = null;
        });
        // After derivation, so rooms and networks are those of this tick.
        System(Phase.Simulation, () =>
        {
            // Power was found again in derivation, and the air steps with what this tick holds.
            _readFor = null;
            State.Clock += 1;
            Follow();
            StepAir();
        });
    }

    protected override AtmosphereState Init(Layout layout) => new() { Tanks = StartingTanks(layout) };

    public override void Rebuild()
    {
        _leaksFor = null;
        _survey = null;
        _tanksCheckedAt = null;
        // Only orders: the air stays where it lies and only flows in a tick, so a loaded world holds what was saved.
        Reorder(State.Live ?? []);
        State.OrderedGrid = _grid.Version;
        State.OrderedVersion = _rooms.Version;
    }

    /// <summary>
    /// At the start of the simulation phase: rooms found again get new orders, their air kept where it lies; then the
    /// live vents are listed, and the rooms of those that turned get theirs (2026-11-05-air-bubbles). The first tick of
    /// a world with none listed yet orders every room.
    /// </summary>
    private void Follow()
    {
        var roomOf = _rooms.RoomOfCells;
        // A vent built or taken away reaches the survey's vents only when it is gathered again.
        if (_grid.Version != State.OrderedGrid && MachinesMoved(_grid.ChangedSince(State.OrderedGrid))) _survey = null;
        State.OrderedGrid = _grid.Version;
        if (State.Live is not { } live)
        {
            State.Live = LiveVents(Gather(), []);
            Reorder(State.Live);
            State.OrderedVersion = _rooms.Version;
            // Air an older save holds off every room goes.
            var stranded = new List<int>();
            foreach (int c in State.Aired) if (roomOf[c] < 0) stranded.Add(c);
            if (stranded.Count > 0)
            {
                EditAired(stranded, []);
                _survey = null;
            }
            return;
        }
        if (_rooms.Version != State.OrderedVersion)
        {
            int was = State.OrderedVersion;
            var found = _rooms.ChangedSince(was);
            List<int> changed;
            if (found == null)
            {
                // Every room was found again, hull and openings too: what is kept per room is gathered afresh.
                _leaksFor = null;
                _survey = null;
                changed = Order.ChangedRooms(State.OrderedRooms, roomOf, live, live);
            }
            else changed = [.. found];
            State.OrderedVersion = _rooms.Version;
            if (changed.Count > 0) ReorderAround(live, [.. changed.Distinct().Order()], was);
        }
        // Every tick: a chain runs dry or is refilled with no version moving.
        LiveVents(Gather(), _liveNow);
        if (_liveNow.SequenceEqual(live)) return;
        var turned = Order.VentRoomsMoved(roomOf, live, _liveNow);
        State.Live = [.. _liveNow];
        if (turned.Count > 0) ReorderAround(State.Live, [.. turned.Distinct().Order()], _rooms.Version);
    }

    /// <summary>
    /// The tanks the map under the map fills: a canister's worth in each tank drawn over an <c>o</c>. Ids
    /// are found as the grid gives them, in the order <see cref="Layout.Machines"/> lists a map's machines.
    /// </summary>
    private static List<TankAir> StartingTanks(Layout layout)
    {
        var oxygen = layout.CellsOf('o', under: true).ToList();
        var tanks = new List<TankAir>();
        var filled = new HashSet<int>();
        var machines = layout.Machines();
        for (int i = 0; i < machines.Count; i++)
        {
            var (kind, top) = machines[i];
            if (!Content.Machines.Is(kind, MachineTrait.Gas)) continue;
            var spec = Content.Machines.Get(kind);
            int x = top % layout.Width, y = top / layout.Width;
            var cells = new List<int>();
            for (int dy = 0; dy < spec.Height; dy++)
                for (int dx = 0; dx < spec.Width; dx++)
                    cells.Add((y + dy) * layout.Width + x + dx);
            if (!cells.Exists(oxygen.Contains)) continue;
            tanks.Add(new TankAir { Tank = i + 1, Oxygen = Content.Air.Canister });
            foreach (int c in cells) filled.Add(c);
        }
        foreach (int cell in oxygen)
            if (!filled.Contains(cell)) throw new FormatException($"Map cell {cell} under: 'o' is under no tank.");
        return tanks;
    }

    /// <summary>Whether a machine came or went among the grid's <paramref name="changes"/>; true when the log no longer knows.</summary>
    private static bool MachinesMoved(IReadOnlyList<CellChange>? changes)
    {
        if (changes == null) return true;
        foreach (var c in changes)
            if ((c.Layers & GridLayers.Machine) != 0) return true;
        return false;
    }

    public bool Fed(int vent)
    {
        foreach (int id in _pipes.NetworksOf(vent))
            if (_pipes.Network(id) is { } network)
                foreach (int on in network.Machines)
                    if (_grid.MachineById(on) is { } m && Feeds(m, id)) return true;
        return false;
    }

    /// <summary>Whether <paramref name="m"/> is a vaporizer whose output port stands on network <paramref name="network"/>: its input there feeds nothing.</summary>
    private bool Feeds(Machine m, int network)
    {
        if (!Content.Machines.Is(m.Kind, MachineTrait.AirSource)) return false;
        var cells = _grid.PortsOf(m);
        var ports = Content.Machines.Get(m.Kind).Ports;
        for (int i = 0; i < cells.Count; i++)
            if (ports[i].Way == Way.Out && _pipes.NetworkAt(cells[i]) == network) return true;
        return false;
    }

    public bool Supplied(int vent)
    {
        var s = Gather();
        foreach (int id in _pipes.NetworksOf(vent))
            if (Supplied(s, id)) return true;
        return false;
    }

    /// <summary>Every room's order found again, as after a load.</summary>
    private void Reorder(List<int> vents)
    {
        var roomOf = _rooms.RoomOfCells;
        State.Orders = new Dictionary<int, int[]>();
        foreach (var (id, cells) in Order.Rooms(roomOf, _grid.Width, _rooms.All, vents, Seen())) State.Orders[id] = cells;
        State.OrderedRooms = roomOf.ToArray();
    }

    /// <summary>
    /// The orders of the <paramref name="changed"/> rooms (ascending ids, gone and new) found again, and
    /// what is kept for them; their air stays where it lies. Only cells those rooms held can have left every
    /// room, losing their air.
    /// </summary>
    private void ReorderAround(List<int> vents, List<int> changed, int was)
    {
        var roomOf = _rooms.RoomOfCells;
        var held = new List<int>();
        foreach (int id in changed)
            if (State.Orders.Remove(id, out var cells)) held.AddRange(cells);
        var rooms = new List<Room>(changed.Count);
        foreach (var r in _rooms.All) if (changed.BinarySearch(r.Id) >= 0) rooms.Add(r);
        var theirs = new List<int>();
        foreach (int cell in vents) if (roomOf[cell] >= 0 && changed.BinarySearch(roomOf[cell]) >= 0) theirs.Add(cell);
        var orders = Order.Rooms(roomOf, _grid.Width, rooms, theirs, Seen());
        foreach (var (id, cells) in orders)
        {
            State.Orders[id] = cells;
            foreach (int cell in cells) State.OrderedRooms[cell] = roomOf[cell];
        }
        var stranded = new List<int>();
        foreach (int cell in held)
        {
            State.OrderedRooms[cell] = roomOf[cell];
            if (roomOf[cell] < 0 && State.Aired.BinarySearch(cell) >= 0) stranded.Add(cell);
        }
        KeepAround(changed, rooms, orders, stranded, was);
    }


    private bool[] _seen = [];

    /// <summary>Scratch for <see cref="Order.Rooms"/>, run on every change of rooms: a flag a cell, all false between calls.</summary>
    private bool[] Seen()
    {
        if (_seen.Length != _grid.Width * _grid.Height) _seen = new bool[_grid.Width * _grid.Height];
        return _seen;
    }

    private bool IsTank(int id) => _grid.MachineById(id) is { } m && Content.Machines.Is(m.Kind, MachineTrait.Gas);

    // Queries.

    public int OxygenIn(int tank) => State.Tanks.Find(t => t.Tank == tank)?.Oxygen ?? 0;

    /// <summary>Why peer <paramref name="peer"/> cannot put a canister into machine <paramref name="tank"/>: it is no tank, it would spill, or the hero carries none.</summary>
    public string? CanisterProblem(int tank, int peer)
    {
        if (!IsTank(tank)) return "atmosphere.noTank";
        if (!CanisterFits(tank)) return "atmosphere.tankFull";
        return _items.ShortOf(peer, Canister);
    }

    public string? DrawProblem(int tank, int peer, int count = 1)
    {
        // A count under one is no rule of the game: it is taken as one.
        count = Math.Max(1, count);
        if (!IsTank(tank)) return "atmosphere.noTank";
        if ((long)OxygenIn(tank) < (long)count * Content.Air.Canister) return "atmosphere.tankLow";
        return _items.FullFor(peer, [(Content.Crew.Canister, count)]);
    }

    /// <summary>Whether a canister's liquid fits in tank <paramref name="tank"/>: by hand or from its hopper alike.</summary>
    private bool CanisterFits(int tank) => OxygenIn(tank) + Content.Air.Canister <= Content.Air.TankCapacity;

    /// <summary>
    /// Each tank in id order pours the canister in its hopper (<c>SlotTakes</c>) when it fits, as one loaded by hand; each
    /// vaporizer with none of its own liquid left pours one into its own (industrial-oxygen): one a tick at most.
    /// </summary>
    private void PourHoppers()
    {
        foreach (var m in _grid.MachinesWith(MachineTrait.AirSource))
        {
            if (Content.Machines.Get(m.Kind).SlotTakes is not (int at, string canister) || State.Own.Exists(t => t.Tank == m.Id)) continue;
            var held = _items.Crate(m.Id);
            if (at >= held.Count || held[at]?.Item != canister) continue;
            _itemsOps.Consume(m.Id, at);
            State.Own.Add(new TankAir { Tank = m.Id, Oxygen = Content.Air.Canister });
            State.Own.Sort((a, b) => a.Tank.CompareTo(b.Tank));
            _readFor = null;
        }
        foreach (var m in _grid.MachinesWith(MachineTrait.Gas))
        {
            if (Content.Machines.Get(m.Kind).SlotTakes is not (int slot, string item)) continue;
            var slots = _items.Crate(m.Id);
            if (slot >= slots.Count || slots[slot]?.Item != item || !CanisterFits(m.Id)) continue;
            _itemsOps.Consume(m.Id, slot);
            Fill(m.Id, Content.Air.Canister);
        }
    }

    public RoomStatus StatusOf(int room)
    {
        var found = _rooms.RoomById(room);
        return found == null ? RoomStatus.Vacuum : Status(Gather(), found);
    }

    public bool AiredAt(int cell) => State.Aired.BinarySearch(cell) >= 0;
    public bool HoldsAir(int cell) => _rooms.RoomOfCells[cell] is int room and >= 0 && Gather().AiredIn[room] > 0;
    public IReadOnlyList<int> AiredCells => State.Aired;
    public IReadOnlyList<RoomAir> RoomsAir() => AllRoomsAir();

    // Operations.

    public int Breathe(int cell, int amount) => BreatheAt(cell, amount);

    public int Fill(int tank, int amount)
    {
        if (!IsTank(tank)) return 0;
        int added = Math.Min(amount, Content.Air.TankCapacity - OxygenIn(tank));
        if (added <= 0) return 0;
        _readFor = null;
        var held = State.Tanks.Find(t => t.Tank == tank);
        if (held != null) held.Oxygen += added;
        else
        {
            State.Tanks.Add(new TankAir { Tank = tank, Oxygen = added });
            State.Tanks.Sort((a, b) => a.Tank.CompareTo(b.Tank));
        }
        return added;
    }

    public void Reserve(int cell) => _reserves.Add(cell);

    public int LiquidFor(int machine) => LiquidBehind(Gather(), machine);

    public bool BurnLiquid(int machine, int amount)
    {
        var s = Gather();
        if (LiquidBehind(s, machine) < amount) return false;
        int left = amount;
        foreach (int tank in ListAt(s.Behind, machine))
        {
            if (left == 0) break;
            if (!s.Held.TryGetValue(tank, out var held)) continue;
            int taken = Math.Min(held.Oxygen, left);
            held.Oxygen -= taken;
            left -= taken;
        }
        _readFor = null;
        DropEmptyTanks();
        return true;
    }
}
