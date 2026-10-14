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
    [JsonIgnore] internal int[] BuiltFrom { get; set; } = [];
    [JsonIgnore] internal int[] BuiltBack { get; set; } = [];
}

/// <summary>
/// Rooms: the spaces air fills, closed by walls and closed doors (packages/sim/src/modules/rooms).
/// It reads the grid through <see cref="IGridQueries"/> only: it cannot change it, nor see its state.
/// </summary>
public sealed class RoomsModule : Module<RoomsState>
{
    public const int Space = -1, Solid = -2;
    private const int Unseen = -3, Edge = -1;
    private readonly IGridQueries _grid;

    public override string Id => "rooms";

    public RoomsModule(IGridQueries grid)
    {
        _grid = grid;
        // Compared every tick rather than told by the grid, so no change to a structure is missed.
        System(Phase.Derivation, () =>
        {
            if (!_grid.StructureLayer.SequenceEqual(State.BuiltFrom)) Rebuild();
            else if (!_grid.BackLayer.SequenceEqual(State.BuiltBack))
            {
                // Only back walls changed: the rooms stand, their openings move.
                State.Rooms = Reopen(State.RoomOf, State.Rooms, _grid.Width, _grid.BackLayer);
                State.BuiltBack = _grid.BackLayer.ToArray();
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

    public Room? RoomById(int id) => State.Rooms.Find(r => r.Id == id);
    public IReadOnlyList<Room> All => State.Rooms;

    public override void Rebuild()
    {
        (State.RoomOf, State.Rooms) = Find(_grid.Width, _grid.Height, _grid.StructureLayer, _grid.BackLayer);
        State.BuiltFrom = _grid.StructureLayer.ToArray();
        State.BuiltBack = _grid.BackLayer.ToArray();
    }

    /// <summary>
    /// Splits the grid into space and rooms. An open cell in a wall line is an opening; space floods
    /// from the border never through one; a room is a component of what is left, openings included.
    /// </summary>
    public static (int[] RoomOf, List<Room> Rooms) Find(int width, int height, ReadOnlySpan<int> structure, ReadOnlySpan<int> back)
    {
        int size = width * height;
        var solid = new bool[size];
        for (int c = 0; c < size; c++) solid[c] = structure[c] is Structure.Wall or Structure.DoorClosed;
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
        void Neighbours(int c, Span<int> into)
        {
            int x = c % width;
            into[0] = x > 0 ? c - 1 : Edge;
            into[1] = x < width - 1 ? c + 1 : Edge;
            into[2] = c >= width ? c - width : Edge;
            into[3] = c + width < size ? c + width : Edge;
        }

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
            Neighbours(queue[head], near);
            foreach (int n in near)
                if (n != Edge && roomOf[n] == Unseen && !opening[n]) { roomOf[n] = Space; queue[tail++] = n; }
        }

        var rooms = new List<Room>();
        var hulls = new List<List<int>>();
        var openings = new List<List<int>>();
        var index = new int[size];
        for (int start = 0; start < size; start++)
        {
            if (roomOf[start] != Unseen) continue;
            int count = 0;
            roomOf[start] = start;
            queue[count++] = start;
            for (int head = 0; head < count; head++)
            {
                Neighbours(queue[head], near);
                foreach (int n in near)
                    if (n != Edge && roomOf[n] == Unseen) { roomOf[n] = start; queue[count++] = n; }
            }
            index[start] = rooms.Count;
            hulls.Add([]);
            openings.Add([]);
            rooms.Add(new Room(start, count, [], []));
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
            Neighbours(c, near);
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

    /// <summary>The rooms with their openings found again for a new back layer: no flood.</summary>
    private static List<Room> Reopen(int[] roomOf, List<Room> rooms, int width, ReadOnlySpan<int> back)
    {
        var at = new Dictionary<int, List<int>>();
        foreach (var r in rooms) at[r.Id] = [];
        for (int c = 0; c < roomOf.Length; c++)
        {
            int id = roomOf[c];
            if (id < 0) continue;
            if (back[c] != Back.Wall || Outside(roomOf, width, c)) at[id].Add(c);
        }
        return rooms.Select(r => r with { Openings = at[r.Id] }).ToList();
    }
}
