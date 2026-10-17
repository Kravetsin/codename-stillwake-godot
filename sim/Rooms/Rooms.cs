using System.Text.Json.Serialization;
using Stillwake.Sim.Grid;

namespace Stillwake.Sim.Rooms;

/// <summary>A room: its smallest cell, how many cells it holds, what it leaks and vents through.</summary>
public sealed record Room(int Id, int Volume, IReadOnlyList<int> Hull, IReadOnlyList<int> Openings);

/// <summary>Everything rooms keep is derived: found again from the grid, never saved.</summary>
public sealed class RoomsState
{
    [JsonIgnore] internal int[] RoomOf { get; set; } = [];
    [JsonIgnore] internal List<Room> Rooms { get; set; } = [];
    /// <summary>The structure and back layers as the rooms were last found from them.</summary>
    [JsonIgnore] internal int[] BuiltFrom { get; set; } = [];
    [JsonIgnore] internal int[] BuiltBack { get; set; } = [];
    /// <summary>The grid's version last looked at.</summary>
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
            State.GridVersion = _grid.Version;
            var structure = Changes(_grid.StructureLayer, State.BuiltFrom);
            var back = Changes(_grid.BackLayer, State.BuiltBack);
            if (structure.Count == 0 && back.Count == 0) return;
            var changed = structure.Count == 0 ? Reopen(back) : back.Count == 0 ? Reflood(structure) : null;
            if (changed == null) FindAll();
            else if (changed.Count > 0)
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
        State.BuiltFrom = _grid.StructureLayer.ToArray();
        State.BuiltBack = _grid.BackLayer.ToArray();
        FindAll();
    }

    private void FindAll()
    {
        (State.RoomOf, State.Rooms) = Find(_grid.Width, _grid.Height, _grid.StructureLayer, _grid.BackLayer);
        State.Version++;
        State.Changed = null;
    }

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

    /// <summary>The cells where <paramref name="now"/> differs from <paramref name="was"/>, ascending; <paramref name="was"/> is brought up to date.</summary>
    private static List<int> Changes(ReadOnlySpan<int> now, int[] was)
    {
        const int block = 1024;
        var cells = new List<int>();
        for (int start = 0; start < now.Length; start += block)
        {
            int end = Math.Min(start + block, now.Length);
            if (now[start..end].SequenceEqual(was.AsSpan(start, end - start))) continue;
            for (int c = start; c < end; c++)
                if (now[c] != was[c]) { cells.Add(c); was[c] = now[c]; }
        }
        return cells;
    }

    private static bool IsSolid(int structure) => structure is Structure.Wall or Structure.DoorClosed;

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
    public static (int[] RoomOf, List<Room> Rooms) Find(int width, int height, ReadOnlySpan<int> structure, ReadOnlySpan<int> back)
    {
        int size = width * height;
        var solid = new bool[size];
        for (int c = 0; c < size; c++) solid[c] = IsSolid(structure[c]);
        var opening = new bool[size];
        for (int c = 0; c < size; c++)
        {
            if (solid[c]) continue;
            int x = c % width;
            bool across = x > 0 && x < width - 1 && solid[c - 1] && solid[c + 1];
            bool down = c >= width && c + width < size && solid[c - width] && solid[c + width];
            opening[c] = structure[c] == Structure.DoorOpen || across || down;
        }
        Span<int> near = stackalloc int[4];

        var roomOf = new int[size];
        for (int c = 0; c < size; c++) roomOf[c] = solid[c] ? Solid : Unseen;
        var queue = new int[size];
        int tail = 0;
        for (int c = 0; c < size; c++)
        {
            int x = c % width;
            bool border = x == 0 || x == width - 1 || c < width || c >= size - width;
            if (border && roomOf[c] == Unseen && !opening[c]) { roomOf[c] = Space; queue[tail++] = c; }
        }
        for (int head = 0; head < tail; head++)
        {
            Neighbours(queue[head], width, size, near);
            foreach (int n in near)
                if (n != Edge && roomOf[n] == Unseen && !opening[n]) { roomOf[n] = Space; queue[tail++] = n; }
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
            rooms.Add(new Room(start, cells.Count, [], []));
        }

        // Openings and hull in one pass in cell order, so both come out sorted.
        for (int c = 0; c < size; c++)
        {
            int here = roomOf[c];
            if (here == Space) continue;
            bool outside = Outside(roomOf, width, c);
            if (here != Solid)
            {
                if (outside || back[c] != Back.Wall) openings[index[here]].Add(c);
                continue;
            }
            if (!outside) continue;
            // A solid cell with the outside beyond it is hull to every room it borders, once each.
            Neighbours(c, width, size, near);
            for (int i = 0; i < 4; i++)
            {
                int id = near[i] == Edge ? Space : roomOf[near[i]];
                if (id < 0) continue;
                bool seen = false;
                for (int j = 0; j < i; j++) if (near[j] != Edge && roomOf[near[j]] == id) seen = true;
                if (!seen) hulls[index[id]].Add(c);
            }
        }
        for (int i = 0; i < rooms.Count; i++) rooms[i] = rooms[i] with { Hull = hulls[i], Openings = openings[i] };
        return (roomOf, rooms);
    }

    private static bool Outside(int[] roomOf, int width, int c)
    {
        int x = c % width;
        if (x == 0 || x == width - 1 || c < width || c >= roomOf.Length - width) return true;
        return roomOf[c - 1] == Space || roomOf[c + 1] == Space || roomOf[c - width] == Space || roomOf[c + width] == Space;
    }

    private bool IsOpening(int c, ReadOnlySpan<int> back) => Outside(State.RoomOf, _grid.Width, c) || back[c] != Back.Wall;

    /// <summary>
    /// Floods again only the rooms beside the <paramref name="changed"/> cells; the ids gone and new.
    /// Null when a changed cell is within two cells of space or the grid's edge: space may move there.
    /// </summary>
    private List<int>? Reflood(List<int> changed)
    {
        int width = _grid.Width, height = _grid.Height, size = width * height;
        var roomOf = State.RoomOf;
        // A change moves openings at most a cell away, and space floods in from a cell beside one.
        foreach (int c in changed)
        {
            int cx = c % width, cy = c / width;
            if (cx < 3 || cx > width - 4 || cy < 3 || cy > height - 4) return null;
            for (int y = cy - 2; y <= cy + 2; y++)
                for (int x = cx - 2; x <= cx + 2; x++)
                    if (roomOf[y * width + x] == Space) return null;
        }

        // Every room beside a changed cell is marked unseen, and so is each changed cell that was solid.
        var gone = new List<int>();
        var region = new List<int>();
        foreach (int c in changed)
            foreach (int at in (ReadOnlySpan<int>)[c, c - 1, c + 1, c - width, c + width])
            {
                int id = roomOf[at];
                if (id < 0) continue;
                gone.Add(id);
                Spread(roomOf, width, id, id, Unseen, region);
            }
        foreach (int c in changed)
            if (roomOf[c] == Solid) { roomOf[c] = Unseen; region.Add(c); }
        region.Sort();
        gone.Sort();
        var structure = _grid.StructureLayer;
        var back = _grid.BackLayer;
        foreach (int c in region) roomOf[c] = IsSolid(structure[c]) ? Solid : Unseen;

        Span<int> near = stackalloc int[4];
        var made = new List<Room>();
        foreach (int start in region)
        {
            if (roomOf[start] != Unseen) continue;
            var cells = new List<int>();
            Spread(roomOf, width, start, Unseen, start, cells);
            cells.Sort();
            var hull = new List<int>();
            var openings = new List<int>();
            foreach (int c in cells)
            {
                if (IsOpening(c, back)) openings.Add(c);
                Neighbours(c, width, size, near);
                foreach (int n in near)
                    if (n != Edge && roomOf[n] == Solid && Outside(roomOf, width, n)) hull.Add(n);
            }
            made.Add(new Room(start, cells.Count, [.. hull.Distinct().Order()], openings));
        }

        var rooms = new List<Room>(State.Rooms.Count - gone.Count + made.Count);
        int g = 0, m = 0;
        foreach (var r in State.Rooms)
        {
            while (m < made.Count && made[m].Id < r.Id) rooms.Add(made[m++]);
            if (g < gone.Count && gone[g] == r.Id) g++;
            else rooms.Add(r);
        }
        while (m < made.Count) rooms.Add(made[m++]);
        State.Rooms = rooms;
        return [.. gone.Concat(made.Select(r => r.Id)).Distinct().Order()];
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
