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
        /// <summary>The tanks whose output is on each vaporizer's input network, by vaporizer id, in id order: what it turns into air.</summary>
        public readonly Dictionary<int, List<int>> Behind = new();
        /// <summary>Each tank with its output on a network, in id order, and the tanks whose input is on that network: what it pours into.</summary>
        public readonly List<(int Tank, List<int> Into)> Pours = [];
        /// <summary>The satisfaction, in thousandths, of the vaporizers feeding each network, summed: 0 when none is powered.</summary>
        public readonly Dictionary<int, int> MadeOn = new();
        /// <summary>Each vaporizer's satisfaction, in thousandths, by id: read with <see cref="MadeOn"/>.</summary>
        public readonly Dictionary<int, int> Power = new();
        /// <summary>The air sources that take no liquid in, the pod: they make air from nothing (2026-10-31-the-scoop). Only looked up.</summary>
        public readonly HashSet<int> Endless = [];
        /// <summary>Each vent's cell with each network one of its ports stands on, in machine id order then port order, by room.</summary>
        public readonly Dictionary<int, List<(int Cell, int Network)>> FillsIn = new();
        /// <summary>How many cells of each room are aired, by room id: rooms are named by cells.</summary>
        public int[] AiredIn = [];
        /// <summary>What each room's aired cells leak every <c>Air.LeakEvery</c> ticks, by room id: kept with <see cref="AiredIn"/>.</summary>
        public int[] LeakIn = [];
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

    /// <summary>The list the next tick's owed parts are written into, the last tick's once they are read; and the dues found.</summary>
    private List<LeakOwed> _owedSpare = [];
    private readonly List<(Room Room, int Due)> _dues = [];

    /// <summary>
    /// The survey, gathered again only when the pipes or the rooms are new; the generators' power and the
    /// tanks holding oxygen read again only after they may have changed: a canister loaded, tanks taken
    /// apart, or a new tick, its power found again in derivation.
    /// </summary>
    private Survey Gather()
    {
        // The cells' leaks first: the survey counts each room's from them.
        if (_leaksFor != _rooms.Version)
        {
            FindLeaks();
            _survey = null;
        }
        if (_survey == null || _survey.Pipes != _pipes.Version || _survey.Rooms != _rooms.Version) _survey = GatherAll();
        var s = _survey;
        // Tanks emptied since are still held, at none: they give nothing, as if they were gone.
        if (s == _readFor) return s;
        _readFor = s;
        s.MadeOn.Clear();
        s.Power.Clear();
        foreach (var (network, generator) in s.Generators)
        {
            int satisfied = MadeShare(generator);
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
        // A tank gives through its output and takes through its input, its ports in its spec's order.
        var tanksOn = new Dictionary<int, List<int>>();
        var takersOn = new Dictionary<int, List<int>>();
        var outputs = new List<(int Tank, int Network)>();
        foreach (var m in _grid.MachinesWith(MachineTrait.Gas))
        {
            var cells = _grid.PortsOf(m);
            var ports = Content.Machines.Get(m.Kind).Ports;
            for (int i = 0; i < cells.Count; i++)
            {
                if (_pipes.NetworkAt(cells[i]) is not int network) continue;
                if (ports[i].Way == Way.In) AddTo(takersOn, network, m.Id);
                else
                {
                    AddTo(tanksOn, network, m.Id);
                    outputs.Add((m.Id, network));
                }
            }
        }
        foreach (var (tank, network) in outputs)
            if (takersOn.TryGetValue(network, out var into)) s.Pours.Add((tank, into));
        // A vaporizer's ports come in its spec's order: its output feeds a network, its input draws the tanks on another.
        foreach (var m in _grid.MachinesWith(MachineTrait.AirSource))
        {
            var cells = _grid.PortsOf(m);
            var ports = Content.Machines.Get(m.Kind).Ports;
            if (!ports.Any(p => p.Way == Way.In)) s.Endless.Add(m.Id);
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
            if (_rooms.RoomAt(cell) is int place)
            {
                AddTo(s.VentsIn, place, network);
                AddTo(s.FillsIn, place, (cell, network));
            }
        var roomOf = _rooms.RoomOfCells;
        s.AiredIn = new int[roomOf.Length];
        s.LeakIn = new int[roomOf.Length];
        s.AiredAt = new bool[roomOf.Length];
        foreach (int cell in State.Aired)
        {
            s.AiredAt[cell] = true;
            int room = roomOf[cell];
            if (room < 0) continue;
            s.AiredIn[room] += 1;
            s.LeakIn[room] += _cellLeak[cell];
        }
        return s;
    }

    /// <summary>
    /// After rooms found around a change: <paramref name="stranded"/> cells lose their air, and the survey's
    /// counts and vents and the leaks of the <paramref name="changed"/> rooms are redone; the rest are kept.
    /// </summary>
    private void KeepAround(List<int> changed, List<Room> rooms, List<(int Room, int[] Cells)> orders, List<int> stranded, int was)
    {
        // The cells' leaks first, for the rooms changed: the survey counts each room's from them.
        if (_leaksFor == was)
        {
            for (int i = 0; i < rooms.Count; i++) LeaksOf(rooms[i], orders[i].Cells);
            _leaksFor = _rooms.Version;
        }
        else _leaksFor = null;
        var s = _leaksFor != null && _survey != null && _survey.Pipes == _pipes.Version && _survey.Rooms == was ? _survey : null;
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
                s.LeakIn[id] = 0;
                s.VentsIn.Remove(id);
                s.FillsIn.Remove(id);
            }
            foreach (var (id, cells) in orders)
                foreach (int cell in cells)
                    if (s.AiredAt[cell])
                    {
                        s.AiredIn[id] += 1;
                        s.LeakIn[id] += _cellLeak[cell];
                    }
            var roomOf = _rooms.RoomOfCells;
            // Space moves with a change beside it: the vents standing in it are gathered again too.
            s.VentsIn.Remove(RoomsModule.Space);
            s.FillsIn.Remove(RoomsModule.Space);
            foreach (var (cell, network) in s.Vents)
                if (roomOf[cell] == RoomsModule.Space || (roomOf[cell] >= 0 && changed.BinarySearch(roomOf[cell]) >= 0))
                {
                    AddTo(s.VentsIn, roomOf[cell], network);
                    AddTo(s.FillsIn, roomOf[cell], (cell, network));
                }
            s.Rooms = _rooms.Version;
        }
    }

    private static void AddTo<T>(Dictionary<int, List<T>> lists, int key, T value)
    {
        if (lists.TryGetValue(key, out var list)) list.Add(value);
        else lists[key] = [value];
    }

    // Spans and arrays, so the few loops over them for each room every tick allocate no enumerator.
    private static ReadOnlySpan<T> ListAt<T>(Dictionary<int, List<T>> lists, int key) =>
        lists.TryGetValue(key, out var list) ? CollectionsMarshal.AsSpan(list) : [];

    /// <summary>A cell takes air: the survey's counts for <paramref name="room"/> move with it.</summary>
    private void AirCell(Survey s, int cell, int room)
    {
        s.AiredAt[cell] = true;
        s.AiredIn[room] += 1;
        s.LeakIn[room] += _cellLeak[cell];
    }

    /// <summary>A cell loses its air: the survey's counts for <paramref name="room"/> move with it.</summary>
    private void EmptyCell(Survey s, int cell, int room)
    {
        s.AiredAt[cell] = false;
        s.AiredIn[room] -= 1;
        s.LeakIn[room] -= _cellLeak[cell];
    }

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
            if (s.Power.GetValueOrDefault(vaporizer) > 0 && (s.Endless.Contains(vaporizer) || LiquidBehind(s, vaporizer) > 0)) return true;
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

    /// <summary>Takes up to <paramref name="count"/> cells of air from a room, the aired ones last in its order first: from its far edge.</summary>
    private int TakeFar(Survey s, int room, int count, Edits edits)
    {
        var order = OrderOf(room);
        int taken = 0;
        // The air is the first cells of the order, so the last aired one stands at its count less one.
        for (int i = Math.Min(order.Length, s.AiredIn[room]) - 1; i >= 0 && taken < count && s.AiredIn[room] > 0; i--)
        {
            int cell = order[i];
            if (!s.AiredAt[cell]) continue;
            EmptyCell(s, cell, room);
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
            if (!s.AiredAt[cell]) AirCell(s, cell, roomOf[cell]);
    }

    /// <summary>Emptied tanks leave the list: only tanks holding oxygen are kept.</summary>
    private void DropEmptyTanks()
    {
        if (State.Tanks.Exists(t => t.Oxygen == 0)) State.Tanks = State.Tanks.FindAll(t => t.Oxygen > 0);
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
            // None when nothing feeds the room, or when a source that takes no liquid does: its air never runs out.
            int? oxygen = vaporizers.Count == 0 || vaporizers.Overlaps(s.Endless) ? null : HeldBy(s, [.. tanks]);
            var now = Status(s, room);
            // What its aired cells leak now (2026-11-05-air-as-a-tank).
            int leak = s.LeakIn[room.Id];
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
    /// What every cell of a room leaks while it holds air (2026-11-05-air-as-a-tank): kept while the rooms' version stays, as
    /// a material changes only by being rebuilt.
    /// </summary>
    private void FindLeaks()
    {
        var roomOf = _rooms.RoomOfCells;
        if (_cellLeak.Length != roomOf.Length) _cellLeak = new int[roomOf.Length];
        // From the rooms as they are, not the orders, which a query may read before they follow a change.
        for (int cell = 0; cell < roomOf.Length; cell++) _cellLeak[cell] = roomOf[cell] >= 0 ? _grid.BackAt(cell)?.Leak ?? 0 : 0;
        foreach (var r in _rooms.All) HullLeaks(r);
        _leaksFor = _rooms.Version;
    }

    /// <summary>
    /// The leak of each of <paramref name="room"/>'s <paramref name="cells"/> while it holds air, through what that air touches
    /// (0020): the back wall behind it by its material, and each hull cell given to it (<see cref="HullLeaks"/>).
    /// </summary>
    private void LeaksOf(Room room, int[] cells)
    {
        foreach (int cell in cells) _cellLeak[cell] = _grid.BackAt(cell)?.Leak ?? 0;
        HullLeaks(room);
    }

    /// <summary>
    /// Each of <paramref name="room"/>'s hull cells' leak, a wall by its material, anything else by its own <c>Leak</c> (a
    /// doorway's <c>Air.DoorLeak</c>), added to the cell of the room it is given to: its first neighbour there, up, left, right,
    /// down, then the corners the same way. A room counts each of its hull cells once.
    /// </summary>
    private void HullLeaks(Room room)
    {
        var roomOf = _rooms.RoomOfCells;
        int width = _grid.Width;
        foreach (int hull in room.Hull)
        {
            var spec = Structures.Of(_grid.StructureAt(hull));
            int leak = spec.Has(Trait.Terrain) ? _grid.WallAt(hull)!.Leak : spec.Leak;
            if (NeighbourIn(roomOf, width, hull, room.Id) is int to) _cellLeak[to] += leak;
        }
    }

    /// <summary>The first cell beside <paramref name="cell"/> in <paramref name="room"/>: up, left, right, down, then up-left, up-right, down-left, down-right.</summary>
    private static int? NeighbourIn(ReadOnlySpan<int> roomOf, int width, int cell, int room)
    {
        int x = cell % width, y = cell / width, height = roomOf.Length / width;
        ReadOnlySpan<(int Dx, int Dy)> near = [(0, -1), (-1, 0), (1, 0), (0, 1), (-1, -1), (1, -1), (-1, 1), (1, 1)];
        foreach (var (dx, dy) in near)
        {
            int nx = x + dx, ny = y + dy;
            if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
            if (roomOf[ny * width + nx] == room) return ny * width + nx;
        }
        return null;
    }

    /// <summary>
    /// What each sealed room holding air owes this tick of its leak, <c>Air.LeakEvery</c> ticks' worth spread over them: its
    /// part owed so far, kept in <c>Owed</c>, grows by the leak of its aired cells now, and whole units fall due (rounded down,
    /// the rest kept). Exact however the leak moves; a room with no air owes nothing. In the rooms' order, before any is paid.
    /// The rooms and the parts both run in id order, so one walk pairs them; the parts are kept, not made anew each tick.
    /// </summary>
    private List<(Room Room, int Due)> Dues(Survey s, IReadOnlyList<Room> rooms)
    {
        var was = State.Owed;
        var owed = _owedSpare;
        owed.Clear();
        _dues.Clear();
        int next = 0;
        foreach (var room in rooms)
        {
            while (next < was.Count && was[next].Room < room.Id) next++;
            var kept = next < was.Count && was[next].Room == room.Id ? was[next++] : null;
            if (room.Openings.Count > 0 || s.AiredIn[room.Id] == 0) continue;
            long part = (kept?.Part ?? 0) + (long)s.LeakIn[room.Id];
            int due = (int)(part / Content.Air.LeakEvery);
            int rest = (int)(part - (long)due * Content.Air.LeakEvery);
            if (rest > 0)
            {
                kept ??= new LeakOwed { Room = room.Id };
                kept.Part = rest;
                owed.Add(kept);
            }
            if (due > 0) _dues.Add((room, due));
        }
        _owedSpare = was;
        State.Owed = owed;
        return _dues;
    }

    /// <summary>How much of its rate generator <paramref name="generator"/> makes, in thousandths: as much as its power, or all of it if it draws none.</summary>
    public int MadeShare(int generator) =>
        _grid.MachineById(generator) is { } m && Content.Machines.Get(m.Kind).Draws is null or 0 ? 1000 : _power.Satisfaction(generator);

    /// <summary>
    /// Each air source, in id order, after lower ids have drawn, works toward its next unit by <c>Air.GeneratorRate</c> ×
    /// <see cref="MadeShare"/>, but only while its store has room and, if it takes liquid in (the vaporizer), the tanks
    /// behind it hold some; else its work stands. Each unit a vaporizer finishes takes a unit of liquid from those tanks in
    /// id order; one with no input (the piped pod) makes its air from nothing. What room or liquid leaves unmade stays.
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
            // A pod on no pipe is only a pod: nothing to store, nothing saved.
            if (Content.Machines.Get(m.Kind).Draws is null or 0 && !OnAPipe(m)) continue;
            // The work kept and the sources are both in id order: one pass finds each one's.
            while (next < was.Count && was[next].Generator < m.Id) next++;
            int part = next < was.Count && was[next].Generator == m.Id ? was[next].Part : 0;
            bool endless = s.Endless.Contains(m.Id);
            var store = TankOf(m.Id, sorted);
            int room = Content.Air.GeneratorStore - (store?.Oxygen ?? 0), liquid = endless ? int.MaxValue : LiquidBehind(s, m.Id);
            if (room > 0 && liquid > 0)
            {
                part += Content.Air.GeneratorRate * MadeShare(m.Id);
                int made = Math.Min(part / unit, Math.Min(room, liquid));
                part -= made * unit;
                if (made > 0)
                {
                    int left = endless ? 0 : made;
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

    /// <summary>
    /// Tanks in a line (tanks-in-a-line): each tank holding liquid, in id order, pours up to <c>Air.TankFlow</c> into the tanks
    /// whose input is on its output's network, in id order, as much as each has room for. A later tank pours on in the same
    /// tick, so a line fills from its last tank.
    /// </summary>
    private void Pour(Survey s)
    {
        bool added = false;
        foreach (var (tank, into) in s.Pours)
        {
            if (!s.Held.TryGetValue(tank, out var giver)) continue;
            int left = Math.Min(giver.Oxygen, Content.Air.TankFlow);
            foreach (int other in into)
            {
                if (left == 0) break;
                if (other == tank) continue;
                var taker = s.Held.GetValueOrDefault(other);
                int poured = Math.Min(left, Content.Air.TankCapacity - (taker?.Oxygen ?? 0));
                if (poured <= 0) continue;
                if (taker == null)
                {
                    State.Tanks.Add(taker = new TankAir { Tank = other });
                    s.Held[other] = taker;
                    added = true;
                }
                taker.Oxygen += poured;
                giver.Oxygen -= poured;
                left -= poured;
            }
        }
        if (added) State.Tanks.Sort((a, b) => a.Tank.CompareTo(b.Tank));
    }

    /// <summary>
    /// How fast air source <paramref name="source"/> makes air while its store has room, in thousandths of a unit a tick, rounded
    /// down: its rate by its <see cref="MadeShare"/> while it is piped and has liquid behind it (the pod needs none); else none.
    /// </summary>
    public int MakesAt(int source)
    {
        if (_grid.MachineById(source) is not { } m || !Content.Machines.Get(m.Kind).Is(MachineTrait.AirSource) || !OnAPipe(m)) return 0;
        var s = Gather();
        if (!s.Endless.Contains(source) && LiquidBehind(s, source) == 0) return 0;
        return (int)((long)Content.Air.GeneratorRate * MadeShare(source) / Content.Air.LeakEvery);
    }

    private bool OnAPipe(Machine m)
    {
        foreach (int port in _grid.PortsOf(m))
            if (_pipes.NetworkAt(port) != null) return true;
        return false;
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
    /// One tick of air (2026-11-05-air-as-a-tank): vaporizers turn their share of liquid into air; a room open to space
    /// loses <c>Air.VentLoss</c> cells from the far edge and every vent in it or in space drains its network; every sealed
    /// room holding air loses what its aired cells leak, from the far edge; then on a fill beat each live vent of a room
    /// not full airs up to <c>Air.CellsPerFill</c> of its room's first empty cells, a unit each from its own network.
    /// </summary>
    private void StepAir()
    {
        Pour(Gather());
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

        // Every room holding air loses what its aired cells leak, before the vents put any back: the tanks feel it only so.
        foreach (var (room, due) in Dues(s, rooms)) TakeFar(s, room.Id, due, edits);

        if (State.Clock % Content.Air.TicksPerCell == 0)
        {
            foreach (var room in rooms)
            {
                if (room.Openings.Count > 0 || s.AiredIn[room.Id] == room.Volume) continue;
                var order = OrderOf(room.Id);
                // The air is the first cells of the order: the next empty one follows the last aired.
                int next = s.AiredIn[room.Id];
                // Each vent, in machine id order then port order, up to CellsPerFill units of its own network's gas.
                foreach (var (_, network) in ListAt(s.FillsIn, room.Id))
                {
                    for (int filled = 0; filled < Content.Air.CellsPerFill; filled++)
                    {
                        while (next < order.Length && s.AiredAt[order[next]]) next++;
                        if (next == order.Length || Draw(s, network, 1) == 0) break;
                        edits.Added.Add(order[next]);
                        AirCell(s, order[next], room.Id);
                    }
                }
            }
        }

        ApplyEdits(s, edits);
        DropEmptyTanks();
    }

    /// <summary>
    /// Takes up to <paramref name="amount"/> of air for one in <paramref name="cell"/>, if that cell holds air: from the
    /// room's far edge, never from the tanks (2026-11-05-air-as-a-tank). Returns how much was had.
    /// </summary>
    private int BreatheAt(int cell, int amount)
    {
        var roomOf = _rooms.RoomOfCells;
        int room = cell >= 0 && cell < roomOf.Length ? roomOf[cell] : -1;
        if (room < 0) return 0;
        var s = Gather();
        if (!s.AiredAt[cell]) return 0;
        var edits = new Edits();
        int had = TakeFar(s, room, amount, edits);
        ApplyEdits(s, edits);
        return had;
    }

    /// <summary>
    /// Into <paramref name="into"/>, cleared first: the cells of the live vents, in machine id order. A vent is live while
    /// one of its ports' networks is fed and holds oxygen: gas in a store on it, a vaporizer on it with liquid behind,
    /// powered or not, or one that takes none in (the pod). Power does not count, so a night does not move the air.
    /// </summary>
    private static List<int> LiveVents(Survey s, List<int> into)
    {
        into.Clear();
        foreach (var (cell, network) in s.Vents)
            if ((into.Count == 0 || into[^1] != cell) && Holds(s, network)) into.Add(cell);
        return into;
    }

    /// <summary>Whether network <paramref name="network"/> is fed and holds oxygen, as <see cref="LiveVents"/> reads it.</summary>
    private static bool Holds(Survey s, int network)
    {
        foreach (int vaporizer in ListAt(s.StoresOn, network))
            if ((s.Held.TryGetValue(vaporizer, out var gas) && gas.Oxygen > 0) || s.Endless.Contains(vaporizer) || LiquidBehind(s, vaporizer) > 0)
                return true;
        return false;
    }

    /// <summary>
    /// Each of <paramref name="orders"/>' rooms counts its aired cells and airs that many of the first cells of its order,
    /// the rest emptied: its air laid out round its live vents (2026-11-05-air-as-a-tank). The survey moves with it if kept.
    /// </summary>
    private void LayOut(List<(int Room, int[] Cells)> orders)
    {
        var s = _survey;
        var removed = new List<int>();
        var added = new List<int>();
        foreach (var (_, cells) in orders)
        {
            int count = 0;
            foreach (int cell in cells) if (IsAired(s, cell)) count++;
            for (int i = 0; i < cells.Length; i++)
            {
                bool aired = IsAired(s, cells[i]);
                if (i < count && !aired) added.Add(cells[i]);
                else if (i >= count && aired) removed.Add(cells[i]);
            }
        }
        if (removed.Count == 0 && added.Count == 0) return;
        EditAired(removed, added);
        if (s == null) return;
        var roomOf = _rooms.RoomOfCells;
        foreach (int cell in removed) EmptyCell(s, cell, roomOf[cell]);
        foreach (int cell in added) AirCell(s, cell, roomOf[cell]);
    }

    private bool IsAired(Survey? s, int cell) => s != null ? s.AiredAt[cell] : State.Aired.BinarySearch(cell) >= 0;
}
