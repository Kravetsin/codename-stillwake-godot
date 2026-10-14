using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Pipes;
using Stillwake.Sim.Power;
using Stillwake.Sim.Rooms;

namespace Stillwake.Sim.Atmosphere;

/// <summary>Puts one canister from the sender's inventory into tank <c>Tank</c>.</summary>
public sealed record LoadCanister(int Tank) : Command;

/// <summary>What any module may read of the air.</summary>
public interface IAtmosphereQueries
{
    /// <summary>The oxygen in tank or generator <paramref name="tank"/>; none for one never filled, or no tank at all.</summary>
    int OxygenIn(int tank);
    /// <summary>Why peer <paramref name="peer"/>'s canister would not go into machine <paramref name="tank"/>: what the client shows first.</summary>
    string? CanisterProblem(int tank, int peer);
    /// <summary>Room <paramref name="room"/>'s state (TypeScript's <c>roomStatus</c>); vacuum for no room. One survey a call: for every room, read <see cref="RoomsAir"/>.</summary>
    RoomStatus StatusOf(int room);
    /// <summary>Whether <paramref name="cell"/> holds air.</summary>
    bool AiredAt(int cell);
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
    private readonly IRoomsQueries _rooms;
    private readonly IPipesQueries _pipes;
    private readonly IPowerQueries _power;

    /// <summary>Derived: each room's leak by room id, found for the rooms list <see cref="_leaksFor"/>.</summary>
    private readonly Dictionary<int, int> _leaks = new();
    private IReadOnlyList<Room>? _leaksFor;

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
        Handles<LoadCanister>(c => CanisterProblem(c.Tank, c.From), c =>
        {
            itemsOps.Take(c.From, Canister);
            var held = State.Tanks.Find(t => t.Tank == c.Tank);
            if (held != null) held.Oxygen += Content.Air.Canister;
            else
            {
                State.Tanks.Add(new TankAir { Tank = c.Tank, Oxygen = Content.Air.Canister });
                State.Tanks.Sort((a, b) => a.Tank.CompareTo(b.Tank));
            }
        });
        // A tank taken apart takes its oxygen with it; ids are never reused, so this is only tidying.
        System(Phase.Construction, () =>
        {
            var machines = _grid.Machines;
            var live = new HashSet<int>(machines.Count);
            foreach (var m in machines) live.Add(m.Id);
            if (State.Tanks.TrueForAll(t => live.Contains(t.Tank))) return;
            State.Tanks = State.Tanks.FindAll(t => live.Contains(t.Tank));
        });
        // After derivation, so rooms and networks are those of this tick.
        System(Phase.Simulation, () =>
        {
            State.Clock += 1;
            // Rooms or vents moved: the orders of those rooms are found again; the air stays put.
            var vents = VentCells();
            var changed = Order.ChangedRooms(State.OrderedRooms, _rooms.RoomOfCells, State.OrderedVents, vents);
            if (changed.Count > 0)
            {
                Reorder(vents, changed);
                Strand();
            }
            StepAir();
        });
    }

    protected override AtmosphereState Init(Layout layout) => new() { Tanks = StartingTanks(layout) };

    public override void Rebuild()
    {
        _leaksFor = null;
        Reorder(VentCells(), null);
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
            if (kind != "tank") continue;
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

    /// <summary>The cells of the vents, in machine id order: where each room's air lies around.</summary>
    private List<int> VentCells()
    {
        var vents = new List<int>();
        foreach (var m in _grid.Machines) if (m.Kind == "vent") vents.Add(m.Cell);
        return vents;
    }

    /// <summary>
    /// Finds the orders again: of every room, or only of the <paramref name="changed"/> ones, dropping
    /// those of rooms gone. A door moves one or two rooms of hundreds.
    /// </summary>
    private void Reorder(List<int> vents, List<int>? changed)
    {
        var roomOf = _rooms.RoomOfCells;
        int width = _grid.Width;
        if (changed == null)
        {
            State.Orders = new Dictionary<int, int[]>();
            foreach (var (id, cells) in Order.Rooms(roomOf, width, _rooms.All, vents)) State.Orders[id] = cells;
        }
        else
        {
            var wanted = new HashSet<int>(changed);
            var rooms = _rooms.All.Where(r => wanted.Contains(r.Id)).ToList();
            // A room gone is among the changed: its cells were in it before.
            foreach (int id in changed) State.Orders.Remove(id);
            foreach (var (id, cells) in Order.Rooms(roomOf, width, rooms, vents)) State.Orders[id] = cells;
        }
        State.OrderedRooms = roomOf.ToArray();
        State.OrderedVents = vents;
    }

    private bool IsTank(int id) => _grid.Machines.Any(m => m.Id == id && m.Kind == "tank");

    // Queries.

    public int OxygenIn(int tank) => State.Tanks.Find(t => t.Tank == tank)?.Oxygen ?? 0;

    /// <summary>Why peer <paramref name="peer"/> cannot put a canister into machine <paramref name="tank"/>: it is no tank, it would spill, or the hero carries none.</summary>
    public string? CanisterProblem(int tank, int peer)
    {
        if (!IsTank(tank)) return "atmosphere.noTank";
        if (OxygenIn(tank) + Content.Air.Canister > Content.Air.TankCapacity) return "atmosphere.tankFull";
        return _items.ShortOf(peer, Canister);
    }

    public RoomStatus StatusOf(int room)
    {
        var found = _rooms.RoomById(room);
        return found == null ? RoomStatus.Vacuum : Status(Gather(), found);
    }

    public bool AiredAt(int cell) => State.Aired.BinarySearch(cell) >= 0;
    public IReadOnlyList<int> AiredCells => State.Aired;
    public IReadOnlyList<RoomAir> RoomsAir() => AllRoomsAir();

    // Operations.

    public int Breathe(int cell, int amount) => BreatheAt(cell, amount);
}
