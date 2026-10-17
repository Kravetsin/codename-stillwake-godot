using System;
using System.Collections.Generic;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Atmosphere;

/// <summary>
/// The air as the overlay draws it, cell by cell: how much of each cell's air shows, eased toward what
/// the sim says so a cell comes and goes over <see cref="EaseMs"/> rather than at once, how red it is
/// for its room venting, and which way a venting room's air runs, toward its nearest opening. The first
/// survey shows the world as it is, eased from nothing only after. Purely drawn: the sim stays per cell,
/// on or off. Kept over <see cref="Region"/> only, everything built so far and a cell of space round it:
/// past it no cell is in a room. A survey looks again only at the cells whose room or air moved, and a
/// frame eases and writes only the cells still on their way.
/// </summary>
public sealed class AirField(int width, int height)
{
    /// <summary>A cell closes the gap to what it should show at this pace: most of the way in about this long.</summary>
    public const double EaseMs = 220;

    public int Width { get; } = width;
    public int Height { get; } = height;

    /// <summary>The cells kept and written to <see cref="Texels"/>, row by row; it only grows.</summary>
    public Box Region { get; private set; } = Box.None;

    /// <summary>Four bytes a cell of <see cref="Region"/> for the overlay's shader (<see cref="Write"/>).</summary>
    public byte[] Texels { get; private set; } = [];

    private float[] _air = [], _red = [], _flow = [];
    private bool[] _aired = [], _venting = [], _inRoom = [], _moving = [], _dirty = [];
    /// <summary>Each cell's room as last surveyed, to tell the cells whose room moved.</summary>
    private int[] _roomOf = [];
    /// <summary>The cells easing toward what they should show, and those whose texel must be written again.</summary>
    private readonly List<int> _easing = [], _written = [];
    private int[] _airedCells = [];
    private IReadOnlyDictionary<int, IReadOnlyList<int>> _ventingRooms = new Dictionary<int, IReadOnlyList<int>>();
    private bool _surveyed;

    /// <summary>Whether every cell shows what it should, so the texture need not be written again.</summary>
    public bool Settled => _easing.Count == 0 && _written.Count == 0;

    /// <summary>Where <paramref name="cell"/> is kept; -1 outside <see cref="Region"/>.</summary>
    private int At(int cell)
    {
        if (cell < 0 || cell >= Width * Height) return -1;
        int x = cell % Width, y = cell / Width;
        return Region.Contains(x, y) ? (y - Region.Top) * Region.Width + x - Region.Left : -1;
    }

    private int CellOf(int at) => (Region.Top + at / Region.Width) * Width + Region.Left + at % Region.Width;

    /// <summary>How much of <paramref name="cell"/>'s air shows, 0 to 1.</summary>
    public float Air(int cell) => At(cell) is int at and >= 0 ? _air[at] : 0;

    /// <summary>Whether the sim says <paramref name="cell"/> holds air.</summary>
    public bool Aired(int cell) => At(cell) is int at and >= 0 && _aired[at];

    /// <summary>Whether <paramref name="cell"/> lies in a room, not in a wall or space.</summary>
    public bool InRoom(int cell) => At(cell) is int at and >= 0 && _inRoom[at];

    /// <summary>The heading, in radians screen-wise, a venting room's air runs at <paramref name="cell"/>; null outside one.</summary>
    public double? FlowAt(int cell) => At(cell) is int at and >= 0 && _inRoom[at] && _venting[at] ? _flow[at] : null;

    /// <summary>
    /// What the sim says now: <paramref name="roomOf"/> each cell's room (negative for none),
    /// <paramref name="aired"/> the cells holding air, ascending, each venting room's openings by room id,
    /// and the box <paramref name="built"/> outside which no cell is in a room.
    /// </summary>
    public void Survey(ReadOnlySpan<int> roomOf, IReadOnlyList<int> aired, IReadOnlyDictionary<int, IReadOnlyList<int>> venting, Box built)
    {
        Grow(built);
        // Rooms whose openings changed have every cell looked at again; the rest only where the room moved.
        var reopened = new HashSet<int>();
        foreach (var (room, openings) in venting)
            if (!_ventingRooms.TryGetValue(room, out var was) || !Same(was, openings)) reopened.Add(room);
        foreach (var room in _ventingRooms.Keys)
            if (!venting.ContainsKey(room)) reopened.Add(room);
        _ventingRooms = venting;
        var r = Region;
        for (int y = r.Top; y <= r.Bottom; y++)
        {
            int start = y * Width + r.Left, at0 = (y - r.Top) * r.Width;
            var row = roomOf.Slice(start, r.Width);
            if (reopened.Count == 0 && row.SequenceEqual(_roomOf.AsSpan(at0, r.Width))) continue;
            for (int i = 0; i < r.Width; i++)
            {
                int room = row[i], at = at0 + i;
                if (room == _roomOf[at] && (room < 0 || !reopened.Contains(room))) continue;
                _roomOf[at] = room;
                bool inRoom = room >= 0, wasVenting = _venting[at];
                float flow = _flow[at];
                _venting[at] = inRoom && venting.TryGetValue(room, out var openings) && openings.Count > 0;
                if (_venting[at]) _flow[at] = (float)Toward(start + i, venting[room]);
                if (inRoom != _inRoom[at] || flow != _flow[at]) Write(at);
                _inRoom[at] = inRoom;
                if (_venting[at] != wasVenting) Ease(at);
            }
        }
        // The aired cells now and at the last survey, both ascending: a cell in one only came or went. The
        // sim airs only cells of rooms, so a cell leaving its room leaves the list too.
        int a = 0, b = 0;
        while (a < aired.Count || b < _airedCells.Length)
        {
            int now = a < aired.Count ? aired[a] : int.MaxValue, then = b < _airedCells.Length ? _airedCells[b] : int.MaxValue;
            if (now == then) { a++; b++; continue; }
            int cell = Math.Min(now, then);
            if (now < then) a++;
            else b++;
            int at = At(cell);
            if (at < 0) continue;
            bool on = now == cell;
            if (_aired[at] != on) { _aired[at] = on; Ease(at); }
        }
        _airedCells = [.. aired];
        if (!_surveyed)
        {
            _surveyed = true;
            for (int at = 0; at < _air.Length; at++)
            {
                _air[at] = _aired[at] ? 1 : 0;
                _red[at] = _venting[at] ? 1 : 0;
            }
            foreach (int at in _easing) _moving[at] = false;
            _easing.Clear();
            for (int at = 0; at < _air.Length; at++) Write(at);
        }
    }

    private static bool Same(IReadOnlyList<int> a, IReadOnlyList<int> b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++) if (a[i] != b[i]) return false;
        return true;
    }

    private void Ease(int at)
    {
        if (_moving[at]) return;
        _moving[at] = true;
        _easing.Add(at);
    }

    private void Write(int at)
    {
        if (_dirty[at]) return;
        _dirty[at] = true;
        _written.Add(at);
    }

    /// <summary>
    /// The region grown to hold <paramref name="built"/> and a cell round it: a cell of space between the
    /// rooms and its edge, so the shader blends a room's edge with no room, as over the whole world.
    /// </summary>
    private void Grow(Box built)
    {
        if (built.IsEmpty) return;
        var wanted = new Box(Math.Max(0, built.Left - 1), Math.Max(0, built.Top - 1), Math.Min(Width - 1, built.Right + 1), Math.Min(Height - 1, built.Bottom + 1));
        var next = Region.Union(wanted);
        if (next == Region) return;
        var was = Region;
        float[] air = _air, red = _red, flow = _flow;
        bool[] aired = _aired, venting = _venting, inRoom = _inRoom;
        int[] roomOf = _roomOf;
        Region = next;
        int size = next.Width * next.Height;
        (_air, _red, _flow) = (new float[size], new float[size], new float[size]);
        (_aired, _venting, _inRoom, _moving, _dirty) = (new bool[size], new bool[size], new bool[size], new bool[size], new bool[size]);
        _roomOf = new int[size];
        Array.Fill(_roomOf, -1);
        Texels = new byte[size * 4];
        // What was kept moves to its place in the larger region; every texel is written again.
        for (int at = 0; at < was.Width * was.Height; at++)
        {
            int to = At(was.Top * Width + was.Left + at / was.Width * Width + at % was.Width);
            (_air[to], _red[to], _flow[to]) = (air[at], red[at], flow[at]);
            (_aired[to], _venting[to], _inRoom[to], _roomOf[to]) = (aired[at], venting[at], inRoom[at], roomOf[at]);
        }
        var easing = _easing.ToArray();
        _easing.Clear();
        foreach (int at in easing) Ease(At(was.Top * Width + was.Left + at / was.Width * Width + at % was.Width));
        _written.Clear();
        for (int at = 0; at < size; at++) Write(at);
    }

    /// <summary>The heading from <paramref name="cell"/> to the nearest of <paramref name="openings"/>, the first on a tie.</summary>
    private double Toward(int cell, IReadOnlyList<int> openings)
    {
        int x = cell % Width, y = cell / Width, best = openings[0], bestD = int.MaxValue;
        foreach (int o in openings)
        {
            int dx = o % Width - x, dy = o / Width - y, d = dx * dx + dy * dy;
            if (d < bestD) (best, bestD) = (o, d);
        }
        return Math.Atan2(best / Width - y, best % Width - x);
    }

    /// <summary>Moves every cell on its way by <paramref name="elapsedMs"/> toward what it should show.</summary>
    public void Ease(double elapsedMs)
    {
        if (_easing.Count == 0) return;
        float k = (float)Math.Min(1, elapsedMs / EaseMs);
        int kept = 0;
        for (int i = 0; i < _easing.Count; i++)
        {
            int at = _easing[i];
            bool settled = Step(_air, at, _aired[at] ? 1 : 0, k) & Step(_red, at, _venting[at] ? 1 : 0, k);
            Write(at);
            if (settled) _moving[at] = false;
            else _easing[kept++] = at;
        }
        _easing.RemoveRange(kept, _easing.Count - kept);
    }

    private static bool Step(float[] values, int at, float target, float k)
    {
        float gap = target - values[at];
        if (Math.Abs(gap) < 0.004f)
        {
            values[at] = target;
            return true;
        }
        values[at] += gap * k;
        return false;
    }

    /// <summary>
    /// Writes into <see cref="Texels"/> the cells that changed since: the air showing, the red of
    /// venting, the heading of the flow as a share of a turn, and whole where the cell is in a room.
    /// Whether any did, so the texture is uploaded again.
    /// </summary>
    public bool Write()
    {
        if (_written.Count == 0) return false;
        foreach (int at in _written)
        {
            _dirty[at] = false;
            int k = at * 4;
            Texels[k] = (byte)Math.Round(_air[at] * 255);
            Texels[k + 1] = (byte)Math.Round(_red[at] * 255);
            double turn = _flow[at] / (2 * Math.PI);
            Texels[k + 2] = (byte)Math.Round((turn - Math.Floor(turn)) * 255);
            Texels[k + 3] = (byte)(_inRoom[at] ? 255 : 0);
        }
        _written.Clear();
        return true;
    }
}
