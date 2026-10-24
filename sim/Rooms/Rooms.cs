using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Stillwake.Sim.Grid;
using Structure = Stillwake.Content.Structure;

namespace Stillwake.Sim.Rooms;

/// <summary>A room: its smallest cell, how many cells it holds, what it leaks and vents through.</summary>
public sealed record Room(int Id, int Volume, IReadOnlyList<int> Hull, IReadOnlyList<int> Openings);

/// <summary>Everything rooms keep is derived: found again from the grid, never saved.</summary>
public sealed class RoomsState
{
    [JsonIgnore] internal int[] RoomOf { get; set; } = [];
    [JsonIgnore] internal List<Room> Rooms { get; set; } = [];
    /// <summary>The grid's version the rooms were last found at: its log names the cells changed since.</summary>
    [JsonIgnore] internal int GridVersion { get; set; }
    /// <summary>Bumped each time rooms are found again, around a change or all of them.</summary>
    [JsonIgnore] internal int Version { get; set; }
    /// <summary>The ids found again at <see cref="Version"/>, ascending; null when every room was.</summary>
    [JsonIgnore] internal List<int>? Changed { get; set; }
}

/// <summary>What any module may read of the rooms.</summary>
public interface IRoomsQueries
{
    /// <summary>The room <paramref name="cell"/> is in, <see cref="RoomsModule.Space"/>, or null for a solid cell or one off the grid.</summary>
    int? RoomAt(int cell);
    /// <summary>
    /// Whether something in <paramref name="cell"/> floats: in space on a surface whose kind lets things float there,
    /// the ship's; never on a planet, where gravity holds everywhere (0076).
    /// </summary>
    bool Afloat(int cell);
    Room? RoomById(int id);
    /// <summary>Every room, in id order.</summary>
    IReadOnlyList<Room> All { get; }
    /// <summary>
    /// Per cell, live and read-only: a room's id (never negative), or <see cref="RoomsModule.Space"/> or
    /// <see cref="RoomsModule.Solid"/>. For a module walking every cell; one at a time, use <see cref="RoomAt"/>.
    /// </summary>
    ReadOnlySpan<int> RoomOfCells { get; }
    /// <summary>Bumped each time rooms or their openings are found again; never saved.</summary>
    int Version { get; }
    /// <summary>
    /// The ids of the rooms found again since <paramref name="version"/>, ascending: those gone and those
    /// new, an id that is both once. Empty while <paramref name="version"/> is current; null when every
    /// room was found again since, or <paramref name="version"/> is older than the last change. A module
    /// keeping something per room redoes these and keeps the rest.
    /// </summary>
    IReadOnlyList<int>? ChangedSince(int version);
}

/// <summary>
/// Rooms: the spaces air fills, closed by walls and closed doors (packages/sim/src/modules/rooms).
/// It reads the grid through <see cref="IGridQueries"/> only: it cannot change it, nor see its state.
/// </summary>
public sealed class RoomsModule : Module<RoomsState>, IRoomsQueries
{
    public const int Space = -1, Solid = -2;
    private const int Unseen = -3, Edge = -1;
    private readonly IGridQueries _grid;

    public override string Id => "rooms";

    public RoomsModule(IGridQueries grid)
    {
        _grid = grid;
        System(Phase.Derivation, () =>
        {
            if (_grid.Version == State.GridVersion) return;
            var log = _grid.ChangedSince(State.GridVersion);
            State.GridVersion = _grid.Version;
            // The log let go of what changed: every room found again, as after a load.
            if (log == null)
            {
                Rebuild();
                return;
            }
            var structure = new List<int>();
            var back = new List<int>();
            foreach (var change in log)
            {
                if ((change.Layers & GridLayers.Structure) != 0) structure.Add(change.Cell);
                if ((change.Layers & GridLayers.Back) != 0) back.Add(change.Cell);
            }
            if (structure.Count == 0 && back.Count == 0) return;
            // On grown ground a back wall decides whether its cell is outside: found again as a change of structure is.
            var changed = structure.Count == 0 && !Grown ? Reopen(back) : Reflow(Grown ? [.. structure, .. back] : structure, back);
            if (changed.Count > 0)
            {
                State.Version++;
                State.Changed = changed;
            }
        });
    }

    protected override RoomsState Init(Layout layout) => new();

    /// <summary>The room <paramref name="cell"/> is in, <see cref="Space"/>, or null for a solid cell or one off the grid.</summary>
    public int? RoomAt(int cell)
    {
        if (cell < 0 || cell >= State.RoomOf.Length || State.RoomOf[cell] == Solid) return null;
        return State.RoomOf[cell];
    }

    public bool Afloat(int cell) => Rules.Floats && RoomAt(cell) == Space;


    public Room? RoomById(int id)
    {
        int at = IndexOf(id);
        return at >= 0 ? State.Rooms[at] : null;
    }

    public IReadOnlyList<Room> All => State.Rooms;
    public ReadOnlySpan<int> RoomOfCells => State.RoomOf;
    public int Version => State.Version;

    public IReadOnlyList<int>? ChangedSince(int version) =>
        version == State.Version ? [] : version == State.Version - 1 ? State.Changed : null;

    public override void Rebuild()
    {
        State.GridVersion = _grid.Version;
        State.RoomOf = new int[_grid.Width * _grid.Height];
        Array.Fill(State.RoomOf, Space);
        // Past what is built every cell is empty, so space: flooded within it, as Find over the whole grid.
        var built = _grid.Built;
        (var local, State.Rooms) = FindIn(_grid.Width, _grid.StructureLayer, _grid.BackLayer, built, _grid.BackMaterialLayer, Grown);
        for (int y = built.Top; y <= built.Bottom; y++)
            local.AsSpan((y - built.Top) * built.Width, built.Width).CopyTo(State.RoomOf.AsSpan(y * _grid.Width + built.Left));
        State.Version++;
        State.Changed = null;
    }

    private static bool Same(Room a, Room b) =>
        a.Volume == b.Volume && a.Hull.SequenceEqual(b.Hull) && a.Openings.SequenceEqual(b.Openings);

    /// <summary>Where room <paramref name="id"/> is in the rooms, which are in id order; negative for none.</summary>
    private int IndexOf(int id)
    {
        int low = 0, high = State.Rooms.Count - 1;
        while (low <= high)
        {
            int mid = (low + high) / 2, at = State.Rooms[mid].Id;
            if (at == id) return mid;
            if (at < id) low = mid + 1;
            else high = mid - 1;
        }
        return -1;
    }

    private static bool IsSolid(int structure) => structure is Structure.Wall or Structure.DoorClosed or Structure.HatchClosed;

    /// <summary>
    /// Whether this surface's ground grew from a seed (0077): there an open cell under the sky, with no back wall, or before
    /// natural ground, which holds no air, is outside, wherever it is, so caves are not rooms and a dig costs what is round it.
    /// </summary>
    private bool Grown => Rules is Content.PlanetType;

    private static readonly bool[] Natural = Enumerable.Range(0, Content.Materials.All.Max(m => m.Code) + 1)
        .Select(code => Content.Materials.All.Any(m => m.Code == code && m.Natural)).ToArray();

    /// <summary>Whether an open cell with this back wall is outside on grown ground: none, or a natural one.</summary>
    private static bool IsPorous(int back, int material) => back != Back.Wall || material >= 0 && material < Natural.Length && Natural[material];

    /// <summary>Whether <paramref name="c"/> is an open cell outside on grown ground (<see cref="IsPorous"/>).</summary>
    private bool Porous(int c) => Grown && !IsSolid(_grid.StructureAt(c)) && IsPorous(_grid.BackLayer[c], _grid.BackMaterialLayer[c]);

    private static void Neighbours(int c, int width, int size, Span<int> into)
    {
        int x = c % width;
        into[0] = x > 0 ? c - 1 : Edge;
        into[1] = x < width - 1 ? c + 1 : Edge;
        into[2] = c >= width ? c - width : Edge;
        into[3] = c + width < size ? c + width : Edge;
    }

    /// <summary>
    /// Floods from <paramref name="start"/> through the cells marked <paramref name="from"/>, marking
    /// each <paramref name="to"/> and adding it to <paramref name="cells"/>.
    /// </summary>
    private static void Spread(int[] roomOf, int width, int start, int from, int to, List<int> cells)
    {
        Span<int> near = stackalloc int[4];
        int head = cells.Count;
        roomOf[start] = to;
        cells.Add(start);
        for (; head < cells.Count; head++)
        {
            Neighbours(cells[head], width, roomOf.Length, near);
            foreach (int n in near)
                if (n != Edge && roomOf[n] == from) { roomOf[n] = to; cells.Add(n); }
        }
    }

    /// <summary>
    /// Splits the grid into space and rooms. An open cell in a wall line is an opening; space floods
    /// from the border never through one; a room is a component of what is left, openings included.
    /// </summary>
    public static (int[] RoomOf, List<Room> Rooms) Find(int width, int height, ReadOnlySpan<int> structure, ReadOnlySpan<int> back) =>
        FindIn(width, structure, back, new Box(0, 0, width - 1, height - 1), [], false);

    /// <summary>
    /// <see cref="Find"/> over <paramref name="region"/> alone, its cells numbered within it, row by row.
    /// Past its sides lies the grid's edge, or empty space, which a cell on a side sees as it sees the edge:
    /// outside beyond, nothing solid.
    /// </summary>
    private static (int[] RoomOf, List<Room> Rooms) FindIn(
        int gridWidth, ReadOnlySpan<int> structure, ReadOnlySpan<int> back, Box region, ReadOnlySpan<int> backMaterial, bool grown)
    {
        int width = region.Width, size = width * region.Height;
        int Cell(int c) => (region.Top + c / width) * gridWidth + region.Left + c % width;
        var solid = new bool[size];
        for (int c = 0; c < size; c++) solid[c] = IsSolid(structure[Cell(c)]);
        var opening = new bool[size];
        for (int c = 0; c < size; c++)
        {
            if (solid[c]) continue;
            int x = c % width;
            bool across = x > 0 && x < width - 1 && solid[c - 1] && solid[c + 1];
            bool down = c >= width && c + width < size && solid[c - width] && solid[c + width];
            opening[c] = Doorways.IsOpen(structure[Cell(c)]) || across || down;
        }
        // On grown ground an open cell under the sky or before natural ground is outside, wherever it is (0077).
        var porous = new bool[size];
        if (grown)
            for (int c = 0; c < size; c++) porous[c] = !solid[c] && IsPorous(back[Cell(c)], backMaterial[Cell(c)]);
        Span<int> near = stackalloc int[4];

        var roomOf = new int[size];
        for (int c = 0; c < size; c++) roomOf[c] = solid[c] ? Solid : Unseen;
        var queue = new int[size];
        int tail = 0;
        for (int c = 0; c < size; c++)
        {
            int x = c % width;
            bool border = x == 0 || x == width - 1 || c < width || c >= size - width;
            if (roomOf[c] == Unseen && (porous[c] || border && !opening[c])) { roomOf[c] = Space; queue[tail++] = c; }
        }
        for (int head = 0; head < tail; head++)
        {
            Neighbours(queue[head], width, size, near);
            foreach (int n in near)
                if (n != Edge && roomOf[n] == Unseen && (!opening[n] || porous[n])) { roomOf[n] = Space; queue[tail++] = n; }
        }

        var rooms = new List<Room>();
        var hulls = new List<List<int>>();
        var openings = new List<List<int>>();
        var index = new int[size];
        var cells = new List<int>();
        for (int start = 0; start < size; start++)
        {
            if (roomOf[start] != Unseen) continue;
            cells.Clear();
            Spread(roomOf, width, start, Unseen, start, cells);
            index[start] = rooms.Count;
            hulls.Add([]);
            openings.Add([]);
            rooms.Add(new Room(Cell(start), cells.Count, [], []));
        }

        // Openings and hull in one pass in cell order, so both come out sorted.
        for (int c = 0; c < size; c++)
        {
            int here = roomOf[c];
            if (here == Space) continue;
            bool outside = Outside(roomOf, width, c);
            if (here != Solid)
            {
                if (outside || back[Cell(c)] != Back.Wall) openings[index[here]].Add(Cell(c));
                continue;
            }
            // A solid cell with the outside beyond it is hull to every room it borders, once each; one with a wall cell beyond it
            // away from a room, the outside beyond that, is that room's too: a wall two cells thick, a 16 px cell (0104).
            Neighbours(c, width, size, near);
            for (int i = 0; i < 4; i++)
            {
                int id = near[i] == Edge ? Space : roomOf[near[i]];
                if (id < 0 || !outside && !Through(roomOf, width, near[i ^ 1])) continue;
                bool seen = false;
                for (int j = 0; j < i; j++)
                    if (near[j] != Edge && roomOf[near[j]] == id && (outside || Through(roomOf, width, near[j ^ 1]))) seen = true;
                if (!seen) hulls[index[id]].Add(Cell(c));
            }
        }
        for (int i = 0; i < rooms.Count; i++) rooms[i] = rooms[i] with { Hull = hulls[i], Openings = openings[i] };
        for (int c = 0; c < size; c++) if (roomOf[c] >= 0) roomOf[c] = Cell(roomOf[c]);
        return (roomOf, rooms);
    }

    private static bool Outside(int[] roomOf, int width, int c)
    {
        int x = c % width;
        if (x == 0 || x == width - 1 || c < width || c >= roomOf.Length - width) return true;
        return roomOf[c - 1] == Space || roomOf[c + 1] == Space || roomOf[c - width] == Space || roomOf[c + width] == Space;
    }

    /// <summary>
    /// Whether <paramref name="c"/>, beyond a solid cell from a room, is solid with the outside beyond it: the room's wall two cells
    /// thick, a 16 px cell's (0104), its inner layer in the hull as a wall of one is. <see cref="Edge"/> is not.
    /// </summary>
    private static bool Through(int[] roomOf, int width, int c) => c != Edge && roomOf[c] == Solid && Outside(roomOf, width, c);

    private bool IsOpening(int c, ReadOnlySpan<int> back) => Outside(State.RoomOf, _grid.Width, c) || back[c] != Back.Wall;

    /// <summary>
    /// Labels again only what a change of structure can move: the <paramref name="changed"/> cells and those
    /// beside them, whose solidity or opening may change, and every room within two cells of one, which they
    /// may join or split. Space floods in from the grid's edge and from the space round them; the rooms
    /// whose hull or openings space moving reaches, and those at the <paramref name="back"/> cells changed,
    /// have them found again. Space a change closes off, a pocket that no longer reaches the outside, is
    /// found by a walk through it and becomes a room. The ids gone, new and found again, ascending.
    /// </summary>
    private List<int> Reflow(List<int> changed, List<int> back)
    {
        int width = _grid.Width, height = _grid.Height;
        var roomOf = State.RoomOf;
        var structure = _grid.StructureLayer;
        var backLayer = _grid.BackLayer;
        // The cells labelled again, each with its label before, and the same cells only looked up.
        var region = new List<int>();
        var was = new List<int>();
        var inRegion = new HashSet<int>();
        var gone = new List<int>();
        // The cells of each room taken apart, ascending; only looked up.
        var cellsOf = new Dictionary<int, int[]>();
        var kept = new HashSet<int>();
        var cells = new List<int>();
        // A cell of space or solid labelled again, unseen.
        void Take(int c)
        {
            if (!inRegion.Add(c)) return;
            region.Add(c);
            was.Add(roomOf[c]);
            roomOf[c] = Unseen;
        }
        // Every cell of room id labelled again, unseen.
        void TakeRoom(int id)
        {
            gone.Add(id);
            cells.Clear();
            Spread(roomOf, width, id, id, Unseen, cells);
            cellsOf[id] = [.. cells.Order()];
            foreach (int r in cells)
            {
                inRegion.Add(r);
                region.Add(r);
                was.Add(id);
            }
        }
        foreach (int c in changed)
        {
            int cx = c % width, cy = c / width;
            for (int dy = -2; dy <= 2; dy++)
                for (int dx = -2; dx <= 2; dx++)
                {
                    int far = Math.Abs(dx) + Math.Abs(dy), x = cx + dx, y = cy + dy;
                    if (far > 2 || x < 0 || x >= width || y < 0 || y >= height) continue;
                    int at = y * width + x;
                    if (roomOf[at] >= 0) TakeRoom(roomOf[at]);
                    else if (far <= 1) Take(at);
                }
        }
        foreach (int c in region) roomOf[c] = IsSolid(structure[c]) ? Solid : Unseen;

        // Space comes in from the grid's edge and the space round the region, through open cells that are no opening.
        Span<int> near = stackalloc int[4], by = stackalloc int[4];
        var flood = new List<int>();
        foreach (int c in region)
        {
            if (roomOf[c] != Unseen || !Passes(structure, c)) continue;
            Neighbours(c, width, roomOf.Length, near);
            bool outside = Porous(c);
            foreach (int n in near) outside |= n == Edge || roomOf[n] == Space;
            if (!outside) continue;
            roomOf[c] = Space;
            flood.Add(c);
        }
        for (int head = 0; head < flood.Count; head++)
        {
            Neighbours(flood[head], width, roomOf.Length, near);
            foreach (int n in near)
                if (n != Edge && roomOf[n] == Unseen && Passes(structure, n)) { roomOf[n] = Space; flood.Add(n); }
        }

        // Space a change cut through may no longer reach the outside: a pocket of it is a room now, with
        // every room whose openings touch it. Its cells were space, so none is solid.
        var pocket = new List<int>();
        for (int i = 0; i < region.Count; i++)
        {
            if (was[i] != Space || roomOf[region[i]] == Space) continue;
            Neighbours(region[i], width, roomOf.Length, near);
            foreach (int n in near)
            {
                if (n == Edge || roomOf[n] != Space || ReachesOutside(n, pocket)) continue;
                foreach (int p in pocket)
                {
                    Take(p);
                    roomOf[p] = Unseen;
                }
                foreach (int p in pocket)
                {
                    Neighbours(p, width, roomOf.Length, by);
                    foreach (int q in by)
                        if (q != Edge && roomOf[q] >= 0) TakeRoom(roomOf[q]);
                }
            }
        }

        var order = region.ToArray();
        Array.Sort(order);
        var made = new List<Room>();
        foreach (int start in order)
        {
            if (roomOf[start] != Unseen) continue;
            cells.Clear();
            Spread(roomOf, width, start, Unseen, start, cells);
            var room = Shell(start, cells, backLayer);
            made.Add(room);
            if (cellsOf.TryGetValue(start, out var before) && before.AsSpan().SequenceEqual(CollectionsMarshal.AsSpan(cells)) && Same(room, State.Rooms[IndexOf(start)]))
                kept.Add(start);
        }
        var madeIds = made.Select(r => r.Id).ToList();

        // Rooms whose cells stayed: their hull and openings, where space came or went near them or a back wall changed. Its hull
        // reaches two cells into a wall, so a room three cells from where space moved may see its hull change.
        var again = new List<int>();
        for (int i = 0; i < region.Count; i++)
        {
            int c = region[i];
            if ((was[i] == Space) == (roomOf[c] == Space)) continue;
            int cx = c % width, cy = c / width;
            for (int dy = -3; dy <= 3; dy++)
                for (int dx = -3; dx <= 3; dx++)
                {
                    int x = cx + dx, y = cy + dy;
                    if (Math.Abs(dx) + Math.Abs(dy) > 3 || x < 0 || x >= width || y < 0 || y >= height) continue;
                    int id = roomOf[y * width + x];
                    if (id >= 0 && madeIds.BinarySearch(id) < 0) again.Add(id);
                }
        }
        // A room found again just as it was, its cells, hull and openings, is not named.
        var ids = new List<int>();
        foreach (int id in gone) if (!kept.Contains(id)) ids.Add(id);
        foreach (int id in madeIds) if (!kept.Contains(id)) ids.Add(id);
        foreach (int c in back)
        {
            int id = roomOf[c];
            if (id < 0) continue;
            ids.Add(id);
            if (madeIds.BinarySearch(id) < 0) again.Add(id);
        }
        var reshelled = new List<Room>();
        foreach (int id in again.Distinct().Order())
        {
            cells.Clear();
            Spread(roomOf, width, id, id, Unseen, cells);
            cells.Clear();
            Spread(roomOf, width, id, Unseen, id, cells);
            var room = Shell(id, cells, backLayer);
            if (Same(room, State.Rooms[IndexOf(id)])) continue;
            reshelled.Add(room);
            ids.Add(id);
        }

        gone.Sort();
        var rooms = new List<Room>(State.Rooms.Count - gone.Count + made.Count);
        int g = 0, m = 0, s = 0;
        foreach (var r in State.Rooms)
        {
            while (m < made.Count && made[m].Id < r.Id) rooms.Add(made[m++]);
            if (g < gone.Count && gone[g] == r.Id) g++;
            else if (s < reshelled.Count && reshelled[s].Id == r.Id) rooms.Add(reshelled[s++]);
            else rooms.Add(r);
        }
        while (m < made.Count) rooms.Add(made[m++]);
        State.Rooms = rooms;
        return [.. ids.Distinct().Order()];
    }

    /// <summary>Whether space passes <paramref name="c"/>: open, and no opening.</summary>
    private bool Passes(ReadOnlySpan<int> structure, int c)
    {
        if (IsSolid(structure[c])) return false;
        if (Porous(c)) return true;
        if (Doorways.IsOpen(structure[c])) return false;
        int width = _grid.Width, size = structure.Length, x = c % width;
        bool across = x > 0 && x < width - 1 && IsSolid(structure[c - 1]) && IsSolid(structure[c + 1]);
        bool down = c >= width && c + width < size && IsSolid(structure[c - width]) && IsSolid(structure[c + width]);
        return !across && !down;
    }

    /// <summary>
    /// Whether the space at <paramref name="start"/> reaches the grid's edge or past what is built, where all
    /// is space that does, through space, walking the space between into <paramref name="seen"/>: on false, the pocket.
    /// </summary>
    private bool ReachesOutside(int start, List<int> seen)
    {
        var roomOf = State.RoomOf;
        int width = _grid.Width, size = roomOf.Length;
        var built = _grid.Built;
        seen.Clear();
        seen.Add(start);
        if (_walked.Length != size) _walked = new bool[size];
        _walked[start] = true;
        Span<int> near = stackalloc int[4];
        bool reaches = false;
        for (int head = 0; head < seen.Count && !reaches; head++)
        {
            int c = seen[head];
            Neighbours(c, width, size, near);
            reaches = !built.Contains(c % width, c / width) || Porous(c);
            foreach (int n in near)
            {
                if (n == Edge) reaches = true;
                else if (roomOf[n] == Space && !_walked[n]) { _walked[n] = true; seen.Add(n); }
            }
        }
        foreach (int c in seen) _walked[c] = false;
        return reaches;
    }

    /// <summary>Scratch for <see cref="ReachesOutside"/>: a flag a cell, all false between calls.</summary>
    private bool[] _walked = [];

    /// <summary>Room <paramref name="id"/> of <paramref name="cells"/>, as labelled now: its openings and hull, both in cell order.</summary>
    private Room Shell(int id, List<int> cells, ReadOnlySpan<int> back)
    {
        int width = _grid.Width, size = State.RoomOf.Length;
        var roomOf = State.RoomOf;
        cells.Sort();
        Span<int> near = stackalloc int[4], beyond = stackalloc int[4];
        var hull = new List<int>();
        var openings = new List<int>();
        foreach (int c in cells)
        {
            if (IsOpening(c, back)) openings.Add(c);
            Neighbours(c, width, size, near);
            for (int i = 0; i < 4; i++)
            {
                int n = near[i];
                if (n == Edge || roomOf[n] != Solid) continue;
                Neighbours(n, width, size, beyond);
                if (Outside(roomOf, width, n) || Through(roomOf, width, beyond[i])) hull.Add(n);
            }
        }
        return new Room(id, cells.Count, [.. hull.Distinct().Order()], openings);
    }

    /// <summary>
    /// Each cell whose back wall changed joins or leaves its room's openings, as it is now one or not;
    /// the ids of those rooms. Their cells and the outside stay, so nothing else does.
    /// </summary>
    private List<int> Reopen(List<int> changed)
    {
        var roomOf = State.RoomOf;
        var back = _grid.BackLayer;
        var ids = new List<int>();
        var copies = new Dictionary<int, List<int>>();
        foreach (int c in changed)
        {
            int id = roomOf[c];
            if (id < 0) continue;
            if (!copies.TryGetValue(id, out var openings))
            {
                copies[id] = openings = [.. State.Rooms[IndexOf(id)].Openings];
                ids.Add(id);
            }
            int at = openings.BinarySearch(c);
            if (IsOpening(c, back) && at < 0) openings.Insert(~at, c);
            else if (!IsOpening(c, back) && at >= 0) openings.RemoveAt(at);
        }
        if (ids.Count == 0) return ids;
        ids.Sort();
        var rooms = new List<Room>(State.Rooms);
        foreach (int id in ids)
        {
            int at = IndexOf(id);
            rooms[at] = rooms[at] with { Openings = copies[id] };
        }
        State.Rooms = rooms;
        return ids;
    }
}
