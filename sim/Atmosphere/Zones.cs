using Stillwake.Sim.Grid;
using Stillwake.Sim.Rooms;

namespace Stillwake.Sim.Atmosphere;

/// <summary>
/// The air zones (2026-11-05-air-bubbles): each room's cells less its doorway cells, door or hatch, open or shut, split
/// where that divides them, each keyed by its least cell. A zone's hull is its room's hull beside it and every doorway
/// beside it; its openings, its room's: a zone on a way to space through open doors vents. An inner door moved changes no zone's
/// cells. Derived, followed from the rooms whenever read; walked like rooms by the module.
/// </summary>
internal sealed class Zones
{
    private const int Unseen = -3;
    private static readonly (int Dx, int Dy)[] Near = [(0, -1), (-1, 0), (1, 0), (0, 1), (-1, -1), (1, -1), (-1, 1), (1, 1)];

    private readonly IRoomsQueries _rooms;
    private readonly IGridQueries _grid;
    private int[] _of = [];
    private readonly List<Room> _all = [];
    private readonly Dictionary<int, Room> _byKey = new();
    private readonly Dictionary<int, int[]> _cells = new();
    private readonly Dictionary<int, int> _roomOf = new();
    private int[] _mark = [];
    private int _stamp;
    private int? _seen, _gridSeen;
    private int _version, _fullAt;
    /// <summary>The zones each version gone or new, the last few: what <see cref="ChangedSince"/> answers from.</summary>
    private readonly List<(int Version, List<int> Keys)> _log = [];

    public Zones(IRoomsQueries rooms, IGridQueries grid)
    {
        _rooms = rooms;
        _grid = grid;
    }

    /// <summary>Per cell: its zone's key, or <see cref="RoomsModule.Space"/>, or <see cref="RoomsModule.Solid"/> (a doorway too).</summary>
    public ReadOnlySpan<int> RoomOfCells
    {
        get
        {
            Follow();
            return _of;
        }
    }

    /// <summary>Every zone, in key order.</summary>
    public IReadOnlyList<Room> All
    {
        get
        {
            Follow();
            return _all;
        }
    }

    /// <summary>Bumped each time a zone is gone or new.</summary>
    public int Version
    {
        get
        {
            Follow();
            return _version;
        }
    }

    public Room? RoomById(int key)
    {
        Follow();
        return _byKey.GetValueOrDefault(key);
    }

    /// <summary>The room zone <paramref name="key"/> lies in.</summary>
    public int RoomOf(int key)
    {
        Follow();
        return _roomOf[key];
    }

    /// <summary>The zone <paramref name="cell"/> is in, <see cref="RoomsModule.Space"/>, or null for a solid or doorway cell.</summary>
    public int? RoomAt(int cell)
    {
        Follow();
        if (cell < 0 || cell >= _of.Length || _of[cell] == RoomsModule.Solid) return null;
        return _of[cell];
    }

    /// <summary>The keys of the zones gone or new since <paramref name="version"/>, ascending; null when all were found again since.</summary>
    public IReadOnlyList<int>? ChangedSince(int version)
    {
        Follow();
        if (version == _version) return [];
        if (version < _fullAt || _log.Count == 0 || version < _log[0].Version - 1) return null;
        var keys = new SortedSet<int>();
        foreach (var (v, changed) in _log)
            if (v > version) keys.UnionWith(changed);
        return [.. keys];
    }

    private void Follow()
    {
        if (_seen == _rooms.Version) return;
        var named = _seen is int at ? _rooms.ChangedSince(at) : null;
        var cellsChanged = _gridSeen is int g ? _grid.ChangedSince(g) : null;
        _seen = _rooms.Version;
        _gridSeen = _grid.Version;
        var roomOf = _rooms.RoomOfCells;
        bool all = named == null || cellsChanged == null || _of.Length != roomOf.Length;
        if (!all && named!.Count == 0) return;
        var changed = new List<int>();
        var fresh = new List<int>();
        if (all)
        {
            _of = new int[roomOf.Length];
            _mark = new int[roomOf.Length];
            _all.Clear();
            _byKey.Clear();
            _cells.Clear();
            _roomOf.Clear();
            for (int c = 0; c < roomOf.Length; c++) Place(c, roomOf, fresh);
        }
        else
        {
            _stamp++;
            foreach (int id in named!)
                if (id >= 0 && id < _mark.Length) _mark[id] = _stamp;
            // Zones of the rooms named go, their cells found again below; cells that left every room take what they are now.
            foreach (var zone in _all)
            {
                if (_mark[_roomOf[zone.Id]] != _stamp) continue;
                changed.Add(zone.Id);
                foreach (int c in _cells[zone.Id]) _of[c] = roomOf[c] < 0 ? roomOf[c] : Unseen;
                _byKey.Remove(zone.Id);
                _cells.Remove(zone.Id);
                _roomOf.Remove(zone.Id);
            }
            // Space and solid move only where the grid changed, but for rooms gone or new, walked below.
            foreach (var change in cellsChanged!)
                if (roomOf[change.Cell] < 0 || Doorways.IsDoorway(_grid.StructureAt(change.Cell))) Place(change.Cell, roomOf, fresh);
            // Each room named that stands now, from its id, its least cell, through its cells.
            var queue = new List<int>();
            int width = _grid.Width;
            foreach (int id in named!)
            {
                if (_rooms.RoomById(id) == null || _mark[id] == -_stamp) continue;
                _mark[id] = -_stamp;
                queue.Clear();
                queue.Add(id);
                var walked = new HashSet<int> { id };
                for (int head = 0; head < queue.Count; head++)
                {
                    int cell = queue[head];
                    Place(cell, roomOf, fresh);
                    int x = cell % width;
                    if (cell >= width) Walk(cell - width, id, roomOf, walked, queue);
                    if (x > 0) Walk(cell - 1, id, roomOf, walked, queue);
                    if (x < width - 1) Walk(cell + 1, id, roomOf, walked, queue);
                    if (cell + width < roomOf.Length) Walk(cell + width, id, roomOf, walked, queue);
                }
            }
        }
        fresh.Sort();
        int w = _grid.Width;
        var zoneQueue = new List<int>();
        var added = new List<int>();
        foreach (int start in fresh)
        {
            if (_of[start] != Unseen) continue;
            int room = roomOf[start];
            zoneQueue.Clear();
            zoneQueue.Add(start);
            _of[start] = start;
            for (int head = 0; head < zoneQueue.Count; head++)
            {
                int cell = zoneQueue[head], x = cell % w;
                Visit(cell - w, room, start, roomOf, zoneQueue);
                if (x > 0) Visit(cell - 1, room, start, roomOf, zoneQueue);
                if (x < w - 1) Visit(cell + 1, room, start, roomOf, zoneQueue);
                Visit(cell + w, room, start, roomOf, zoneQueue);
            }
            var cells = zoneQueue.ToArray();
            Array.Sort(cells);
            _cells[start] = cells;
            _roomOf[start] = room;
            changed.Add(start);
            added.Add(start);
        }
        foreach (int key in added) _byKey[key] = Describe(key, _cells[key], _rooms.RoomById(_roomOf[key])!);
        // A zone found again under its old key is a new record: the old one goes by reference.
        _all.RemoveAll(z => !ReferenceEquals(_byKey.GetValueOrDefault(z.Id), z));
        foreach (int key in added) _all.Add(_byKey[key]);
        _all.Sort((a, b) => a.Id.CompareTo(b.Id));
        _version++;
        if (all)
        {
            _fullAt = _version;
            _log.Clear();
            return;
        }
        changed.Sort();
        _log.Add((_version, changed));
        if (_log.Count > 8) _log.RemoveAt(0);
    }

    /// <summary>Cell <paramref name="c"/> as the rooms have it: space or solid as is, a doorway solid, a room's cell to be zoned.</summary>
    private void Place(int c, ReadOnlySpan<int> roomOf, List<int> fresh)
    {
        if (Doorways.IsDoorway(_grid.StructureAt(c))) _of[c] = RoomsModule.Solid;
        else if (roomOf[c] < 0) _of[c] = roomOf[c];
        else
        {
            _of[c] = Unseen;
            fresh.Add(c);
        }
    }

    private static void Walk(int cell, int room, ReadOnlySpan<int> roomOf, HashSet<int> walked, List<int> queue)
    {
        if (roomOf[cell] == room && walked.Add(cell)) queue.Add(cell);
    }

    private void Visit(int cell, int room, int key, ReadOnlySpan<int> roomOf, List<int> queue)
    {
        if (cell < 0 || cell >= _of.Length || _of[cell] != Unseen || roomOf[cell] != room) return;
        _of[cell] = key;
        queue.Add(cell);
    }

    /// <summary>A zone as a room: its volume, the hull beside it (its room's and its doorways) and its openings, in cell order.</summary>
    private Room Describe(int key, int[] cells, Room room)
    {
        var hull = new SortedSet<int>();
        foreach (int h in room.Hull)
            if (Beside(h, key)) hull.Add(h);
        int width = _grid.Width, height = _of.Length / width;
        foreach (int cell in cells)
        {
            int x = cell % width, y = cell / width;
            foreach (var (dx, dy) in Near)
            {
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                int n = ny * width + nx;
                if (Doorways.IsDoorway(_grid.StructureAt(n))) hull.Add(n);
            }
        }
        // Open through open doors to space, its room vents it: an open door costs air only on a way to space.
        return new Room(key, cells.Length, [.. hull], room.Openings);
    }

    private bool Beside(int cell, int key)
    {
        int width = _grid.Width, height = _of.Length / width, x = cell % width, y = cell / width;
        foreach (var (dx, dy) in Near)
        {
            int nx = x + dx, ny = y + dy;
            if (nx >= 0 && ny >= 0 && nx < width && ny < height && _of[ny * width + nx] == key) return true;
        }
        return false;
    }
}
