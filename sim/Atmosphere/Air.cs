using System.Runtime.InteropServices;
using Stillwake.Content;
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
    /// Who is where, so the rules read lookups instead of scanning the ship once per room; kept between
    /// ticks while the pipes and the rooms stay, what changes every tick read anew.
    /// </summary>
    private sealed class Survey
    {
        /// <summary>The networks of the vents standing in each room, or in space, in vent id order.</summary>
        public readonly Dictionary<int, List<int>> VentsIn = new();
        /// <summary>The vaporizers feeding each network, their output port on it, in id order: draws take from their stores.</summary>
        public readonly Dictionary<int, List<int>> StoresOn = new();
        /// <summary>The tanks on each vaporizer's input network, by vaporizer id, in id order: what it turns into air.</summary>
        public readonly Dictionary<int, List<int>> Behind = new();
        /// <summary>The satisfaction, in thousandths, of the vaporizers feeding each network, summed: 0 when none is powered.</summary>
        public readonly Dictionary<int, int> MadeOn = new();
        /// <summary>Each vaporizer's satisfaction, in thousandths, by id: read with <see cref="MadeOn"/>.</summary>
        public readonly Dictionary<int, int> Power = new();
        /// <summary>How many cells of each room are aired, by room id: rooms are named by cells.</summary>
        public int[] AiredIn = [];
        /// <summary>Per cell, whether it holds air: kept in step with every change of the aired cells.</summary>
        public bool[] AiredAt = [];
        /// <summary>Each tank holding oxygen, by id: the very entries of the slice's tanks.</summary>
        public readonly Dictionary<int, TankAir> Held = new();
        /// <summary>Each vent's cell with a network one of its ports stands on, in machine id order: what <see cref="VentsIn"/> is gathered from.</summary>
        public readonly List<(int Cell, int Network)> Vents = [];
        /// <summary>Each vaporizer with its output port on a network, in machine id order: what <see cref="MadeOn"/> is summed from.</summary>
        public readonly List<(int Network, int Generator)> Generators = [];
        /// <summary>The versions of the pipes and of the rooms it was gathered at.</summary>
        public int Pipes, Rooms;
    }

    /// <summary>What a tick does to the aired list, applied once at the end: the list is long.</summary>
    private sealed class Edits
    {
        public readonly List<int> Removed = [];
        public readonly List<int> Added = [];
    }

    /// <summary>Up to this many edits of the aired cells go into the list in place; more, by one merge into a new list.</summary>
    private const int InPlace = 32;

    /// <summary>The survey kept between ticks; null when it must be gathered again.</summary>
    private Survey? _survey;

    /// <summary>The survey the generators' power and the tanks holding oxygen were last read into; null, to read them again.</summary>
    private Survey? _readFor;

    /// <summary>
    /// The survey, gathered again only when the pipes or the rooms are new; the generators' power and the
    /// tanks holding oxygen read again only after they may have changed: a canister loaded, tanks taken
    /// apart, or a new tick, its power found again in derivation.
    /// </summary>
    private Survey Gather()
    {
        if (_survey == null || _survey.Pipes != _pipes.Version || _survey.Rooms != _rooms.Version) _survey = GatherAll();
        var s = _survey;
        // Tanks emptied since are still held, at none: they give nothing, as if they were gone.
        if (s == _readFor) return s;
        _readFor = s;
        s.MadeOn.Clear();
        s.Power.Clear();
        foreach (var (network, generator) in s.Generators)
        {
            int satisfied = _power.Satisfaction(generator);
            s.Power[generator] = satisfied;
            s.MadeOn[network] = s.MadeOn.GetValueOrDefault(network) + satisfied;
        }
        s.Held.Clear();
        foreach (var t in State.Tanks) s.Held[t.Tank] = t;
        return s;
    }

    private Survey GatherAll()
    {
        var s = new Survey { Pipes = _pipes.Version, Rooms = _rooms.Version };
        foreach (var m in _grid.MachinesWith(MachineTrait.Vents))
            foreach (int port in _grid.PortsOf(m))
                if (_pipes.NetworkAt(port) is int network) s.Vents.Add((m.Cell, network));
        var tanksOn = new Dictionary<int, List<int>>();
        foreach (var m in _grid.MachinesWith(MachineTrait.Gas))
            foreach (int port in _grid.PortsOf(m))
                if (_pipes.NetworkAt(port) is int network) AddTo(tanksOn, network, m.Id);
        // A vaporizer's ports come in its spec's order: its output feeds a network, its input draws the tanks on another.
        foreach (var m in _grid.MachinesWith(MachineTrait.AirSource))
        {
            var cells = _grid.PortsOf(m);
            var ports = Content.Machines.Get(m.Kind).Ports;
            for (int i = 0; i < cells.Count; i++)
            {
                if (_pipes.NetworkAt(cells[i]) is not int network) continue;
                if (ports[i].Way == Way.Out)
                {
                    AddTo(s.StoresOn, network, m.Id);
                    s.Generators.Add((network, m.Id));
                }
                else
                    foreach (int tank in ListAt(tanksOn, network)) AddTo(s.Behind, m.Id, tank);
            }
        }
        foreach (var (cell, network) in s.Vents)
            if (_rooms.RoomAt(cell) is int place) AddTo(s.VentsIn, place, network);
        var roomOf = _rooms.RoomOfCells;
        s.AiredIn = new int[roomOf.Length];
        s.AiredAt = new bool[roomOf.Length];
        foreach (int cell in State.Aired)
        {
            s.AiredAt[cell] = true;
            int room = roomOf[cell];
            if (room >= 0) s.AiredIn[room] += 1;
        }
        return s;
    }

    /// <summary>
    /// After rooms found around a change: <paramref name="stranded"/> cells lose their air, and the survey's
    /// counts and vents and the leaks of the <paramref name="changed"/> rooms are redone; the rest are kept.
    /// </summary>
    private void KeepAround(List<int> changed, List<Room> rooms, List<(int Room, int[] Cells)> orders, List<int> stranded, int was)
    {
        var s = _survey != null && _survey.Pipes == _pipes.Version && _survey.Rooms == was ? _survey : null;
        _survey = s;
        if (stranded.Count > 0)
        {
            EditAired(stranded, []);
            if (s != null) foreach (int cell in stranded) s.AiredAt[cell] = false;
        }
        if (s != null)
        {
            foreach (int id in changed)
            {
                s.AiredIn[id] = 0;
                s.VentsIn.Remove(id);
            }
            foreach (var (id, cells) in orders)
                foreach (int cell in cells)
                    if (s.AiredAt[cell]) s.AiredIn[id] += 1;
            var roomOf = _rooms.RoomOfCells;
            // Space moves with a change beside it: the vents standing in it are gathered again too.
            s.VentsIn.Remove(RoomsModule.Space);
            foreach (var (cell, network) in s.Vents)
                if (roomOf[cell] == RoomsModule.Space || (roomOf[cell] >= 0 && changed.BinarySearch(roomOf[cell]) >= 0))
                    AddTo(s.VentsIn, roomOf[cell], network);
            s.Rooms = _rooms.Version;
        }
        if (_leaksFor == was)
        {
            foreach (int id in changed) _leaks.Remove(id);
            for (int i = 0; i < rooms.Count; i++)
            {
                int leak = HullLeak(rooms[i]);
                foreach (int cell in orders[i].Cells) if (_grid.BackAt(cell) is { } material) leak += material.Leak;
                _leaks[rooms[i].Id] = leak;
            }
            _leaksFor = _rooms.Version;
        }
        else _leaksFor = null;
    }

    private static void AddTo(Dictionary<int, List<int>> lists, int key, int value)
    {
        if (lists.TryGetValue(key, out var list)) list.Add(value);
        else lists[key] = [value];
    }

    // Spans and arrays, so the few loops over them for each room every tick allocate no enumerator.
    private static ReadOnlySpan<int> ListAt(Dictionary<int, List<int>> lists, int key) =>
        lists.TryGetValue(key, out var list) ? CollectionsMarshal.AsSpan(list) : [];

    private int[] OrderOf(int room) => State.Orders.TryGetValue(room, out var order) ? order : [];

    /// <summary>
    /// Takes <paramref name="removed"/> out of the sorted aired cells and puts <paramref name="added"/> in, a
    /// cell in both ending aired: a few in place, as a few shifts of a long list cost less than a copy.
    /// </summary>
    private void EditAired(List<int> removed, List<int> added)
    {
        var aired = State.Aired;
        if (removed.Count + added.Count <= InPlace)
        {
            foreach (int cell in removed)
            {
                int at = aired.BinarySearch(cell);
                if (at >= 0) aired.RemoveAt(at);
            }
            foreach (int cell in added)
            {
                int at = aired.BinarySearch(cell);
                if (at < 0) aired.Insert(~at, cell);
            }
            return;
        }
        removed.Sort();
        added.Sort();
        var merged = new List<int>(aired.Count + added.Count);
        int r = 0, a = 0;
        foreach (int cell in aired)
        {
            while (r < removed.Count && removed[r] < cell) r++;
            if (r < removed.Count && removed[r] == cell) continue;
            while (a < added.Count && added[a] < cell) merged.Add(added[a++]);
            merged.Add(cell);
        }
        while (a < added.Count) merged.Add(added[a++]);
        State.Aired = merged;
    }

    /// <summary>What <paramref name="machines"/> hold together: gas in vaporizers' stores, or liquid in tanks.</summary>
    private static int HeldBy(Survey s, ReadOnlySpan<int> machines)
    {
        int sum = 0;
        foreach (int id in machines) sum += s.Held.TryGetValue(id, out var held) ? held.Oxygen : 0;
        return sum;
    }

    /// <summary>The gas in the stores of the vaporizers feeding <paramref name="network"/>.</summary>
    private static int GasOn(Survey s, int network) => HeldBy(s, ListAt(s.StoresOn, network));

    /// <summary>The liquid in the tanks on vaporizer <paramref name="vaporizer"/>'s input network.</summary>
    private static int LiquidBehind(Survey s, int vaporizer) => HeldBy(s, ListAt(s.Behind, vaporizer));

    /// <summary>
    /// Whether <paramref name="network"/> is supplied: gas in a store feeding it, or a vaporizer feeding it powered with
    /// liquid behind it, so a room does not flicker to vacuum while the store refills. Read anew each call.
    /// </summary>
    private static bool Supplied(Survey s, int network)
    {
        if (GasOn(s, network) > 0) return true;
        foreach (int vaporizer in ListAt(s.StoresOn, network))
            if (s.Power.GetValueOrDefault(vaporizer) > 0 && LiquidBehind(s, vaporizer) > 0) return true;
        return false;
    }

    /// <summary>
    /// Takes up to <paramref name="amount"/> from a network, store by store of the vaporizers feeding it in id order;
    /// returns what it took. Stores left empty stay listed until the end of the tick.
    /// </summary>
    private static int Draw(Survey s, int network, int amount)
    {
        int left = amount;
        foreach (int tank in ListAt(s.StoresOn, network))
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
        // A powered vaporizer's store sits empty between the units it makes: its room is filling all the same.
        foreach (int n in ListAt(s.VentsIn, room.Id))
            if (Supplied(s, n)) return RoomStatus.Filling;
        return RoomStatus.Vacuum;
    }

    /// <summary>Takes up to <paramref name="count"/> cells of air from a room, the aired ones last in its order first.</summary>
    private int TakeFar(Survey s, int room, int count, Edits edits)
    {
        var order = OrderOf(room);
        int taken = 0;
        for (int i = order.Length - 1; i >= 0 && taken < count && s.AiredIn[room] > 0; i--)
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

    private void ApplyEdits(Survey s, Edits edits)
    {
        if (edits.Removed.Count == 0 && edits.Added.Count == 0) return;
        EditAired(edits.Removed, edits.Added);
        // A cell aired and taken again on one tick stays aired (TypeScript's applyEdits): so in the survey.
        var roomOf = _rooms.RoomOfCells;
        foreach (int cell in edits.Added)
        {
            if (s.AiredAt[cell]) continue;
            s.AiredAt[cell] = true;
            s.AiredIn[roomOf[cell]] += 1;
        }
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

    /// <summary>Every room's air, in rooms order, from one survey.</summary>
    private List<RoomAir> AllRoomsAir()
    {
        var s = Gather();
        var roomOf = _rooms.RoomOfCells;
        // Vents standing in each room, piped or not: the survey keeps only the piped ones.
        var standing = new HashSet<int>();
        foreach (var m in _grid.MachinesWith(MachineTrait.Vents)) standing.Add(roomOf[m.Cell]);
        var all = new List<RoomAir>(_rooms.All.Count);
        foreach (var room in _rooms.All)
        {
            var networks = ListAt(s.VentsIn, room.Id).ToArray();
            var once = networks.Distinct().ToList();
            int made = 0;
            var vaporizers = new SortedSet<int>();
            foreach (int n in once)
            {
                made += s.MadeOn.GetValueOrDefault(n) * Content.Air.GeneratorRate / 1000;
                foreach (int v in ListAt(s.StoresOn, n)) vaporizers.Add(v);
            }
            // The liquid behind the vaporizers feeding the room, each tank counted once.
            var tanks = new SortedSet<int>();
            foreach (int v in vaporizers) foreach (int t in ListAt(s.Behind, v)) tanks.Add(t);
            int? oxygen = vaporizers.Count == 0 ? null : HeldBy(s, [.. tanks]);
            var now = Status(s, room);
            int leak = LeakOf(room);
            // Venting, each vent drains its network, so a network under two vents drains twice.
            long draw = now == RoomStatus.Aired ? leak
                : now == RoomStatus.Venting ? (long)Content.Air.VentDrain * networks.Length * Content.Air.LeakEvery
                : 0;
            // Liquid leaves at the smaller of what the room draws and what its vaporizers make, both a period.
            long rate = Math.Min(draw, made);
            int? lasts = oxygen is int liquid && rate > 0 ? (int)Math.Min(int.MaxValue, (long)liquid * Content.Air.LeakEvery / rate) : null;
            string? problem =
                now != RoomStatus.Vacuum ? null
                : !standing.Contains(room.Id) ? "atmosphere.noVent"
                : vaporizers.Count == 0 ? "atmosphere.notPiped"
                : oxygen == 0 ? "atmosphere.dry"
                : "atmosphere.unpowered";
            all.Add(new RoomAir(room.Id, now, s.AiredIn[room.Id], leak, oxygen, made, lasts, problem));
        }
        return all;
    }

    /// <summary>
    /// The cells of air a room loses every <c>Air.LeakEvery</c> ticks through what its air touches (0020),
    /// kept per room while the rooms' version stays: a material changes only by being rebuilt.
    /// </summary>
    private int LeakOf(Room room)
    {
        if (_leaksFor != _rooms.Version) FindLeaks();
        return _leaks.GetValueOrDefault(room.Id);
    }

    /// <summary>Every room's leak, in one pass over the cells.</summary>
    private void FindLeaks()
    {
        var roomOf = _rooms.RoomOfCells;
        var behind = new int[roomOf.Length];
        for (int cell = 0; cell < roomOf.Length; cell++)
        {
            int id = roomOf[cell];
            if (id < 0) continue;
            var material = _grid.BackAt(cell);
            if (material != null) behind[id] += material.Leak;
        }
        _leaks.Clear();
        foreach (var r in _rooms.All) _leaks[r.Id] = behind[r.Id] + HullLeak(r);
        _leaksFor = _rooms.Version;
    }

    /// <summary>What a room's hull leaks: each wall by its material, anything else by its own <c>Leak</c> (a doorway's <c>Air.DoorLeak</c>).</summary>
    private int HullLeak(Room room)
    {
        int leak = 0;
        foreach (int cell in room.Hull)
        {
            var spec = Structures.Of(_grid.StructureAt(cell));
            leak += spec.Has(Trait.Terrain) ? _grid.WallAt(cell)!.Leak : spec.Leak;
        }
        return leak;
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
    /// Each vaporizer, in id order, after lower ids have drawn, works toward its next unit by <c>Air.GeneratorRate</c> ×
    /// its power's satisfaction, but only while its store has room and the tanks behind it hold liquid; else its work
    /// stands, so nothing is made from nothing and nothing lost. Each unit it finishes takes a unit of liquid from those
    /// tanks in id order into its store; what room or liquid leaves unmade stays in its work.
    /// </summary>
    private void Produce(Survey s)
    {
        const int unit = Content.Air.LeakEvery * 1000;
        var making = new List<GeneratorWork>();
        var was = State.Making;
        // The tanks are in id order until the first one added below, which no later vaporizer's id finds.
        int sorted = State.Tanks.Count, next = 0;
        bool added = false;
        foreach (var m in _grid.MachinesWith(MachineTrait.AirSource))
        {
            // The work kept and the vaporizers are both in id order: one pass finds each one's.
            while (next < was.Count && was[next].Generator < m.Id) next++;
            int part = next < was.Count && was[next].Generator == m.Id ? was[next].Part : 0;
            var store = TankOf(m.Id, sorted);
            int room = Content.Air.GeneratorStore - (store?.Oxygen ?? 0), liquid = LiquidBehind(s, m.Id);
            if (room > 0 && liquid > 0)
            {
                part += Content.Air.GeneratorRate * _power.Satisfaction(m.Id);
                int made = Math.Min(part / unit, Math.Min(room, liquid));
                part -= made * unit;
                if (made > 0)
                {
                    int left = made;
                    foreach (int tank in ListAt(s.Behind, m.Id))
                    {
                        if (left == 0) break;
                        if (!s.Held.TryGetValue(tank, out var held)) continue;
                        int taken = Math.Min(held.Oxygen, left);
                        held.Oxygen -= taken;
                        left -= taken;
                    }
                    if (store != null) store.Oxygen += made;
                    else
                    {
                        State.Tanks.Add(new TankAir { Tank = m.Id, Oxygen = made });
                        added = true;
                    }
                }
            }
            making.Add(new GeneratorWork { Generator = m.Id, Part = part });
        }
        State.Making = making;
        if (added) State.Tanks.Sort((a, b) => a.Tank.CompareTo(b.Tank));
    }

    /// <summary>Tank <paramref name="id"/>'s oxygen among the first <paramref name="count"/> tanks, which are in id order; null for none.</summary>
    private TankAir? TankOf(int id, int count)
    {
        var tanks = State.Tanks;
        int lo = 0, hi = count - 1;
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) >> 1);
            if (tanks[mid].Tank == id) return tanks[mid];
            if (tanks[mid].Tank < id) lo = mid + 1;
            else hi = mid - 1;
        }
        return null;
    }

    /// <summary>
    /// One tick of air: vaporizers turn their share of liquid into air; a room open to space loses <c>Air.VentLoss</c>
    /// cells from the far edge and every vent in it or in space drains its network; on a fill beat each
    /// filling room airs the first <c>Air.CellsPerFill</c> empty cells of its order, each paid from the first of its vents that can
    /// pay; then every other room holding air uses its leak, spread over each <c>Air.LeakEvery</c> ticks.
    /// </summary>
    private void StepAir()
    {
        Produce(Gather());
        // What the vaporizers made and took is read again: a store may be new, a tank emptied.
        _readFor = null;
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
                // Up to CellsPerFill of its first empty cells, in order, each paid as the one before it.
                int filled = 0;
                foreach (int next in OrderOf(room.Id))
                {
                    if (filled == Content.Air.CellsPerFill) break;
                    if (s.AiredAt[next]) continue;
                    int? network = null;
                    foreach (int n in vents) if (GasOn(s, n) > 0) { network = n; break; }
                    if (network == null) break;
                    Draw(s, network.Value, 1);
                    edits.Added.Add(next);
                    s.AiredAt[next] = true;
                    s.AiredIn[room.Id] += 1;
                    filled++;
                }
            }
        }

        foreach (var room in rooms)
        {
            if (room.Openings.Count > 0 || s.AiredIn[room.Id] == 0) continue;
            Use(s, room.Id, DueAt(LeakOf(room), State.Clock), edits);
        }

        ApplyEdits(s, edits);
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
        ApplyEdits(s, edits);
        DropEmptyTanks();
        return had;
    }
}
