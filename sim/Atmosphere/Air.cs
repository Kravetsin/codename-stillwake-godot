using Stillwake.Sim.Grid;
using Stillwake.Sim.Rooms;

namespace Stillwake.Sim.Atmosphere;

/// <summary>
/// The rules of air (air.ts): air stays where it is; vents air the first empty cell of their room's
/// order (<see cref="Order"/>), and whatever takes air takes the last aired one.
/// </summary>
public sealed partial class AtmosphereModule
{
    /// <summary>
    /// Who is where, gathered in one pass over the machines and the aired cells, so the rules read
    /// lookups instead of scanning the ship once per room: the reference world has hundreds of rooms.
    /// </summary>
    private sealed class Survey
    {
        /// <summary>The networks of the vents standing in each room, or in space, in vent id order.</summary>
        public readonly Dictionary<int, List<int>> VentsIn = new();
        /// <summary>The tanks and generators on each network, in id order: draws take from both alike.</summary>
        public readonly Dictionary<int, List<int>> TanksOn = new();
        /// <summary>The satisfaction, in thousandths, of the generators on each network, summed: 0 when none is powered.</summary>
        public readonly Dictionary<int, int> MadeOn = new();
        /// <summary>How many cells of each room are aired, by room id: rooms are named by cells.</summary>
        public int[] AiredIn = [];
        /// <summary>Per cell, whether it holds air: kept current as the rules change it.</summary>
        public bool[] AiredAt = [];
        /// <summary>Each tank holding oxygen, by id: the very entries of the slice's tanks.</summary>
        public readonly Dictionary<int, TankAir> Held = new();
    }

    /// <summary>What a tick does to the aired list, applied once at the end: the list is long.</summary>
    private sealed class Edits
    {
        public readonly HashSet<int> Removed = [];
        public readonly List<int> Added = [];
    }

    /// <summary>Scratch for <see cref="Gather"/>, and for <see cref="LeakOf"/>: never saved, never read across calls.</summary>
    private int[] _airedIn = [], _behind = [];
    private bool[] _airedAt = [];

    private Survey Gather()
    {
        var s = new Survey();
        foreach (var m in _grid.Machines)
        {
            if (m.Kind is not ("vent" or "tank" or "generator")) continue;
            foreach (int port in _grid.PortsOf(m))
            {
                var network = _pipes.NetworkAt(port);
                if (network == null) continue;
                if (m.Kind != "vent")
                {
                    AddTo(s.TanksOn, network.Value, m.Id);
                    if (m.Kind == "generator")
                        s.MadeOn[network.Value] = s.MadeOn.GetValueOrDefault(network.Value) + _power.Satisfaction(m.Id);
                    continue;
                }
                var place = _rooms.RoomAt(m.Cell);
                if (place == null) continue;
                AddTo(s.VentsIn, place.Value, network.Value);
            }
        }
        var roomOf = _rooms.RoomOfCells;
        // Per-cell arrays are big enough to be costly to allocate every tick: one pair, cleared, serves
        // every survey, as no survey outlives the call that gathered it.
        if (_airedIn.Length != roomOf.Length)
        {
            _airedIn = new int[roomOf.Length];
            _airedAt = new bool[roomOf.Length];
        }
        else
        {
            Array.Clear(_airedIn);
            Array.Clear(_airedAt);
        }
        s.AiredIn = _airedIn;
        s.AiredAt = _airedAt;
        foreach (int cell in State.Aired)
        {
            s.AiredAt[cell] = true;
            int room = roomOf[cell];
            if (room >= 0) s.AiredIn[room] += 1;
        }
        foreach (var t in State.Tanks) s.Held[t.Tank] = t;
        return s;
    }

    private static void AddTo(Dictionary<int, List<int>> lists, int key, int value)
    {
        if (lists.TryGetValue(key, out var list)) list.Add(value);
        else lists[key] = [value];
    }

    private static IReadOnlyList<int> ListAt(Dictionary<int, List<int>> lists, int key) =>
        lists.TryGetValue(key, out var list) ? list : [];

    private IReadOnlyList<int> OrderOf(int room) => State.Orders.TryGetValue(room, out var order) ? order : [];

    /// <summary>Two sorted lists of cells, with no cell in both, as one sorted list.</summary>
    private static List<int> Merge(List<int> a, List<int> b)
    {
        var merged = new List<int>(a.Count + b.Count);
        int i = 0, j = 0;
        while (i < a.Count || j < b.Count)
        {
            if (j >= b.Count || (i < a.Count && a[i] < b[j])) merged.Add(a[i++]);
            else merged.Add(b[j++]);
        }
        return merged;
    }

    private static int OxygenOn(Survey s, int network)
    {
        int sum = 0;
        foreach (int tank in ListAt(s.TanksOn, network)) sum += s.Held.TryGetValue(tank, out var held) ? held.Oxygen : 0;
        return sum;
    }

    /// <summary>
    /// Takes up to <paramref name="amount"/> from a network, tank by tank in id order; returns what it
    /// took. Tanks left empty stay listed until the end of the tick.
    /// </summary>
    private static int Draw(Survey s, int network, int amount)
    {
        int left = amount;
        foreach (int tank in ListAt(s.TanksOn, network))
        {
            if (!s.Held.TryGetValue(tank, out var held) || left == 0) continue;
            int taken = Math.Min(held.Oxygen, left);
            held.Oxygen -= taken;
            left -= taken;
        }
        return amount - left;
    }

    private static RoomStatus Status(Survey s, Room room)
    {
        if (room.Openings.Count > 0) return RoomStatus.Venting;
        if (s.AiredIn[room.Id] == room.Volume) return RoomStatus.Aired;
        // A powered generator's store sits empty between the units it makes: its room is filling all the same.
        foreach (int n in ListAt(s.VentsIn, room.Id))
            if (s.MadeOn.GetValueOrDefault(n) > 0 || OxygenOn(s, n) > 0) return RoomStatus.Filling;
        return RoomStatus.Vacuum;
    }

    /// <summary>Takes up to <paramref name="count"/> cells of air from a room, the aired ones last in its order first.</summary>
    private int TakeFar(Survey s, int room, int count, Edits edits)
    {
        var order = OrderOf(room);
        int taken = 0;
        for (int i = order.Count - 1; i >= 0 && taken < count && s.AiredIn[room] > 0; i--)
        {
            int cell = order[i];
            if (!s.AiredAt[cell]) continue;
            s.AiredAt[cell] = false;
            s.AiredIn[room] -= 1;
            edits.Removed.Add(cell);
            taken++;
        }
        return taken;
    }

    /// <summary>The first cell of a room's order without air: where its vents put the next.</summary>
    private int? NextEmpty(Survey s, int room)
    {
        foreach (int cell in OrderOf(room)) if (!s.AiredAt[cell]) return cell;
        return null;
    }

    private void ApplyEdits(Edits edits)
    {
        if (edits.Removed.Count == 0 && edits.Added.Count == 0) return;
        var kept = edits.Removed.Count == 0 ? State.Aired : State.Aired.FindAll(cell => !edits.Removed.Contains(cell));
        // A cell taken and aired again on one tick is in both: dropped above, back below.
        edits.Added.Sort();
        State.Aired = Merge(kept, edits.Added);
    }

    /// <summary>Emptied tanks leave the list: only tanks holding oxygen are kept.</summary>
    private void DropEmptyTanks()
    {
        if (State.Tanks.Exists(t => t.Oxygen == 0)) State.Tanks = State.Tanks.FindAll(t => t.Oxygen > 0);
    }

    /// <summary>
    /// Uses up to <paramref name="amount"/> of a room's air: paid by the networks of its vents in vent id
    /// order, as if they put back at once what is used, and what they cannot pay taken as cells from the
    /// far edge. Returns how much was had.
    /// </summary>
    private int Use(Survey s, int room, int amount, Edits edits)
    {
        int had = 0;
        foreach (int network in ListAt(s.VentsIn, room)) had += Draw(s, network, amount - had);
        return had + TakeFar(s, room, amount - had, edits);
    }

    /// <summary>Takes the air out of cells no longer in a room: a wall built on them, a room open wide.</summary>
    private void Strand()
    {
        var roomOf = _rooms.RoomOfCells;
        bool stranded = false;
        foreach (int cell in State.Aired) if (roomOf[cell] < 0) { stranded = true; break; }
        if (!stranded) return;
        var kept = new List<int>(State.Aired.Count);
        foreach (int cell in State.Aired) if (roomOf[cell] >= 0) kept.Add(cell);
        State.Aired = kept;
    }

    /// <summary>Every room's air, in rooms order, from one survey.</summary>
    private List<RoomAir> AllRoomsAir()
    {
        var s = Gather();
        var roomOf = _rooms.RoomOfCells;
        // Vents standing in each room, piped or not: the survey keeps only the piped ones.
        var standing = new HashSet<int>();
        foreach (var m in _grid.Machines) if (m.Kind == "vent") standing.Add(roomOf[m.Cell]);
        var all = new List<RoomAir>(_rooms.All.Count);
        foreach (var room in _rooms.All)
        {
            var networks = ListAt(s.VentsIn, room.Id);
            var once = networks.Distinct().ToList();
            int oxygen = 0, made = 0;
            foreach (int n in once)
            {
                oxygen += OxygenOn(s, n);
                made += s.MadeOn.GetValueOrDefault(n) * Content.Air.GeneratorRate / 1000;
            }
            var now = Status(s, room);
            int leak = LeakOf(room);
            // Venting, each vent drains its network, so a network under two vents drains twice.
            int? lasts =
                made > 0 ? null
                : now == RoomStatus.Aired && leak > 0 ? (int)((long)oxygen * Content.Air.LeakEvery / leak)
                : now == RoomStatus.Venting && networks.Count > 0 ? CeilDiv(oxygen, Content.Air.VentDrain * networks.Count)
                : null;
            string? problem =
                now != RoomStatus.Vacuum ? null
                : !standing.Contains(room.Id) ? "atmosphere.noVent"
                : networks.Count == 0 ? "atmosphere.notPiped"
                : networks.Any(s.MadeOn.ContainsKey) ? "atmosphere.unpowered"
                : "atmosphere.dry";
            all.Add(new RoomAir(room.Id, now, s.AiredIn[room.Id], leak, oxygen, made, lasts, problem));
        }
        return all;
    }

    /// <summary><paramref name="a"/> over <paramref name="b"/>, rounded up; neither is negative.</summary>
    private static int CeilDiv(int a, int b) => (a + b - 1) / b;

    /// <summary>
    /// The cells of air a room loses every <c>Air.LeakEvery</c> ticks through what its air touches
    /// (0020): the back wall behind each of its cells, and its hull, each by its material, a door by
    /// <c>Air.DoorLeak</c>. Found once per rooms list, every room's in one pass over the cells: rooms
    /// are new whenever walls or back walls change, and a material changes only by being rebuilt.
    /// </summary>
    private int LeakOf(Room room)
    {
        var rooms = _rooms.All;
        if (ReferenceEquals(rooms, _leaksFor) && _leaks.TryGetValue(room.Id, out int known)) return known;
        var roomOf = _rooms.RoomOfCells;
        if (_behind.Length != roomOf.Length) _behind = new int[roomOf.Length];
        else Array.Clear(_behind);
        var behind = _behind;
        for (int cell = 0; cell < roomOf.Length; cell++)
        {
            int id = roomOf[cell];
            if (id < 0) continue;
            var material = _grid.BackAt(cell);
            if (material != null) behind[id] += material.Leak;
        }
        _leaks.Clear();
        foreach (var r in rooms)
        {
            int leak = behind[r.Id];
            foreach (int cell in r.Hull)
            {
                int structure = _grid.StructureAt(cell);
                if (structure == Structure.Wall) leak += _grid.WallAt(cell)!.Leak;
                else if (structure is Structure.DoorClosed or Structure.DoorOpen) leak += Content.Air.DoorLeak;
            }
            _leaks[r.Id] = leak;
        }
        _leaksFor = rooms;
        return _leaks.GetValueOrDefault(room.Id);
    }

    /// <summary>A tick's share of <paramref name="perPeriod"/>, spread over each <c>Air.LeakEvery</c> ticks so a period's total is exact.</summary>
    internal static int DueAt(int perPeriod, int clock)
    {
        int beat = clock % Content.Air.LeakEvery;
        // Neither is negative, so division rounds down.
        long DueBy(long ticks) => perPeriod * ticks / Content.Air.LeakEvery;
        return (int)(DueBy(beat + 1) - DueBy(beat));
    }

    /// <summary>
    /// Each generator, in id order, works toward its next unit by <c>Air.GeneratorRate</c> × its power's
    /// satisfaction, and puts each unit it finishes into its own store, up to <c>Air.GeneratorStore</c>;
    /// what does not fit is lost. At full power each <c>Air.LeakEvery</c> ticks make exactly the rate.
    /// </summary>
    private void Produce()
    {
        const int unit = Content.Air.LeakEvery * 1000;
        var making = new List<GeneratorWork>();
        bool added = false;
        foreach (var m in _grid.Machines)
        {
            if (m.Kind != "generator") continue;
            int part = (State.Making.Find(g => g.Generator == m.Id)?.Part ?? 0)
                + Content.Air.GeneratorRate * _power.Satisfaction(m.Id);
            int made = part / unit;
            part -= made * unit;
            making.Add(new GeneratorWork { Generator = m.Id, Part = part });
            if (made == 0) continue;
            var store = State.Tanks.Find(t => t.Tank == m.Id);
            if (store != null) store.Oxygen = Math.Min(Content.Air.GeneratorStore, store.Oxygen + made);
            else
            {
                State.Tanks.Add(new TankAir { Tank = m.Id, Oxygen = Math.Min(Content.Air.GeneratorStore, made) });
                added = true;
            }
        }
        State.Making = making;
        if (added) State.Tanks.Sort((a, b) => a.Tank.CompareTo(b.Tank));
    }

    /// <summary>
    /// One tick of air: generators make their share; a room open to space loses <c>Air.VentLoss</c>
    /// cells from the far edge and every vent in it or in space drains its network; on a fill beat each
    /// filling room airs the first empty cell of its order, paid from the first of its vents that can
    /// pay; then every other room holding air uses its leak, spread over each <c>Air.LeakEvery</c> ticks.
    /// </summary>
    private void StepAir()
    {
        Produce();
        var s = Gather();
        var edits = new Edits();
        var rooms = _rooms.All;

        foreach (var room in rooms)
        {
            if (room.Openings.Count == 0) continue;
            TakeFar(s, room.Id, Content.Air.VentLoss, edits);
            foreach (int network in ListAt(s.VentsIn, room.Id)) Draw(s, network, Content.Air.VentDrain);
        }
        foreach (int network in ListAt(s.VentsIn, RoomsModule.Space)) Draw(s, network, Content.Air.VentDrain);

        if (State.Clock % Content.Air.TicksPerCell == 0)
        {
            foreach (var room in rooms)
            {
                if (!s.VentsIn.TryGetValue(room.Id, out var vents) || Status(s, room) != RoomStatus.Filling) continue;
                int? network = null;
                foreach (int n in vents) if (OxygenOn(s, n) > 0) { network = n; break; }
                var next = NextEmpty(s, room.Id);
                if (network == null || next == null) continue;
                Draw(s, network.Value, 1);
                edits.Added.Add(next.Value);
                s.AiredAt[next.Value] = true;
                s.AiredIn[room.Id] += 1;
            }
        }

        foreach (var room in rooms)
        {
            if (room.Openings.Count > 0 || s.AiredIn[room.Id] == 0) continue;
            Use(s, room.Id, DueAt(LeakOf(room), State.Clock), edits);
        }

        ApplyEdits(edits);
        DropEmptyTanks();
    }

    /// <summary>
    /// Takes up to <paramref name="amount"/> of air for one in <paramref name="cell"/>, if that cell holds
    /// air: paid by the networks of the room's vents, what they cannot pay taken from the far edge.
    /// </summary>
    private int BreatheAt(int cell, int amount)
    {
        var roomOf = _rooms.RoomOfCells;
        int room = cell >= 0 && cell < roomOf.Length ? roomOf[cell] : -1;
        if (room < 0) return 0;
        var s = Gather();
        if (!s.AiredAt[cell]) return 0;
        var edits = new Edits();
        int had = Use(s, room, amount, edits);
        ApplyEdits(edits);
        DropEmptyTanks();
        return had;
    }
}
