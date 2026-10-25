using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Features.Atmosphere;

/// <summary>
/// Rooms the sim turned to space at once, air and all (a launch platform opened, a floor torn open), drawn venting a while as a
/// room venting through a door is: the sim drops their air in a tick, so the client lets it go over a while (<see cref="MsPerCell"/>), the
/// cells farthest from the breach first, its air drawn toward the hole. Each such room keeps its last id; its breach is the space beside its cells that was
/// not its own, the hole it went out through. Purely drawn: nothing here reaches the sim.
/// </summary>
public sealed class Decompression
{
    /// <summary>How long a room's air takes to go, by its cells, and the least and most it takes.</summary>
    public const double MsPerCell = 15, LeastMs = 1500, MostMs = 6000;

    /// <summary>Each room going, by its last id: its cells with when each lets go, and the cells of its breach, ascending.</summary>
    private readonly SortedDictionary<int, (Dictionary<int, double> Cells, int[] Breach, AirStream[] Streams)> _going = [];
    private int[] _aired = [];
    private int[] _airedRoom = [];

    /// <summary>The time the drain is counted in, in milliseconds.</summary>
    private double _now;

    /// <summary>The air still going, ascending: cells the sim calls space that are drawn as holding air.</summary>
    public IReadOnlyList<int> Cells { get; private set; } = [];

    /// <summary>Each room going, by its last id, and the cells of its breach: what its air runs toward.</summary>
    public IReadOnlyDictionary<int, IReadOnlyList<int>> Breaches { get; private set; } = new Dictionary<int, IReadOnlyList<int>>();

    /// <summary>Air streaming out of each breach while its room's air goes, as out of a door's opening (<see cref="AirStream.Of"/>).</summary>
    public IReadOnlyList<AirStream> Streams { get; private set; } = [];

    /// <summary>Whether any room's air is still going.</summary>
    public bool Any => _going.Count > 0;

    /// <summary>The room <paramref name="cell"/>'s air is going from, by its last id; null for a cell with none going.</summary>
    public int? RoomOf(int cell)
    {
        foreach (var (room, going) in _going)
            if (going.Cells.ContainsKey(cell)) return room;
        return null;
    }

    /// <summary>
    /// What the sim says now, <paramref name="elapsedMs"/> after the last survey: <paramref name="aired"/> the cells holding air,
    /// ascending, and <paramref name="roomAt"/> each cell's room, <paramref name="space"/> for space and null for a solid cell or
    /// off the grid. Air the sim dropped where space now lies starts going; air gone, or back in a room, stops.
    /// </summary>
    public void Survey(IReadOnlyList<int> aired, Func<int, int?> roomAt, int space, int width, double elapsedMs)
    {
        _now += elapsedMs;
        foreach (var room in _going.Keys.ToArray())
        {
            var cells = _going[room].Cells;
            foreach (var cell in cells.Keys.Where(c => cells[c] <= _now || roomAt(c) != space).ToArray()) cells.Remove(cell);
            if (cells.Count == 0) _going.Remove(room);
        }
        var dropped = new SortedDictionary<int, List<int>>();
        for (int i = 0, j = 0; i < _aired.Length; i++)
        {
            while (j < aired.Count && aired[j] < _aired[i]) j++;
            if (j < aired.Count && aired[j] == _aired[i]) continue;
            if (roomAt(_aired[i]) != space) continue;
            if (!dropped.TryGetValue(_airedRoom[i], out var cells)) dropped[_airedRoom[i]] = cells = [];
            cells.Add(_aired[i]);
        }
        foreach (var (room, cells) in dropped) Start(room, cells, roomAt, space, width);
        _aired = [.. aired];
        _airedRoom = [.. aired.Select(c => roomAt(c) ?? space)];
        Cells = [.. _going.Values.SelectMany(g => g.Cells.Keys).Order()];
        Breaches = _going.ToDictionary(g => g.Key, g => (IReadOnlyList<int>)g.Value.Breach);
        Streams = [.. _going.Values.SelectMany(g => g.Streams)];
    }

    /// <summary>A room's air dropped at once, <paramref name="cells"/>, set going: the farthest from its breach first, the nearest last.</summary>
    private void Start(int room, List<int> cells, Func<int, int?> roomAt, int space, int width)
    {
        var own = cells.ToHashSet();
        var breach = new SortedSet<int>();
        foreach (int c in cells)
            foreach (int n in Near(c, width))
                if (!own.Contains(n) && roomAt(n) == space) breach.Add(n);
        if (breach.Count == 0) return;
        double drain = Math.Clamp(cells.Count * MsPerCell, LeastMs, MostMs);
        // A cell lets go by how far it lies from the breach, straight: the air's edge comes round toward the hole, not stepped.
        var far = cells.ToDictionary(c => c, c => breach.Min(b => Math.Sqrt(Square(c % width - b % width) + Square(c / width - b / width))));
        double farthest = far.Values.Max();
        var going = new Dictionary<int, double>();
        foreach (int c in cells) going[c] = _now + drain * (farthest + 1 - far[c]) / farthest;
        // Each breach cell streams out toward the space past it: neither the room nor the breach itself.
        double perSecond = Math.Min(AirStream.PerOpening, AirStream.PerRoom / breach.Count);
        var streams = breach.Select(b =>
        {
            int dx = 0, dy = 0;
            foreach (var (nx, ny) in (ReadOnlySpan<(int, int)>)[(0, -1), (-1, 0), (1, 0), (0, 1)])
            {
                int n = b + ny * width + nx;
                if ((nx != 0 && (b % width + nx < 0 || b % width + nx >= width)) || own.Contains(n) || breach.Contains(n) || roomAt(n) != space) continue;
                (dx, dy) = (dx + nx, dy + ny);
            }
            return new AirStream(b % width + 0.5, b / width + 0.5, dx == 0 && dy == 0 ? null : Math.Atan2(dy, dx), perSecond);
        }).ToArray();
        _going[room] = (going, [.. breach], streams);
    }

    private static double Square(int a) => (double)a * a;

    private static IEnumerable<int> Near(int cell, int width)
    {
        int x = cell % width;
        yield return cell - width;
        yield return cell + width;
        if (x > 0) yield return cell - 1;
        if (x < width - 1) yield return cell + 1;
    }
}
