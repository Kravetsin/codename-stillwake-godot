using Stillwake.Sim.Rooms;

namespace Stillwake.Sim.Atmosphere;

/// <summary>The order of a room's cells: where its air lies, nearest its vents first.</summary>
internal static class Order
{
    /// <summary>
    /// Every room's cells, breadth first through the room from the vents standing in it, in the order
    /// <paramref name="vents"/> lists them, or from its middle when it has none; in <paramref name="rooms"/>
    /// order. Neighbours go up, left, right, down. Vents air the first empty cell of the order and
    /// whatever takes air takes the last aired one, so a room's air gathers around its vents.
    /// <paramref name="seen"/> is scratch, a flag a cell, all false before and after.
    /// </summary>
    public static List<(int Room, int[] Cells)> Rooms(
        ReadOnlySpan<int> roomOf, int width, IReadOnlyList<Room> rooms, IReadOnlyList<int> vents, bool[] seen)
    {
        var starts = new Dictionary<int, List<int>>();
        foreach (int cell in vents)
        {
            int room = cell >= 0 && cell < roomOf.Length ? roomOf[cell] : -1;
            if (room < 0) continue;
            if (starts.TryGetValue(room, out var list)) list.Add(cell);
            else starts[room] = [cell];
        }
        var orders = new List<(int, int[])>(rooms.Count);
        foreach (var room in rooms)
        {
            var order = new int[room.Volume];
            if (!starts.TryGetValue(room.Id, out var from))
            {
                // A room with no vent is ordered from its middle, found from its cells.
                int count = Spread(roomOf, width, room.Id, [room.Id], order, seen);
                for (int i = 0; i < count; i++) seen[order[i]] = false;
                from = [MiddleOf(order.AsSpan(0, count), width)];
            }
            int tail = Spread(roomOf, width, room.Id, from, order, seen);
            for (int i = 0; i < tail; i++) seen[order[i]] = false;
            if (tail != room.Volume) throw new InvalidOperationException($"Room {room.Id} is not connected.");
            orders.Add((room.Id, order));
        }
        return orders;
    }

    /// <summary>
    /// The cells of <paramref name="room"/> breadth first from <paramref name="from"/>, neighbours up,
    /// left, right, down, into <paramref name="order"/>, each marked in <paramref name="seen"/>; how many.
    /// </summary>
    private static int Spread(ReadOnlySpan<int> roomOf, int width, int room, List<int> from, int[] order, bool[] seen)
    {
        int tail = 0;
        foreach (int cell in from)
        {
            if (seen[cell]) continue;
            seen[cell] = true;
            order[tail++] = cell;
        }
        for (int head = 0; head < tail; head++)
        {
            int cell = order[head];
            for (int i = 0; i < 4; i++)
            {
                int next = i switch
                {
                    0 => cell >= width ? cell - width : -1,
                    1 => cell % width != 0 ? cell - 1 : -1,
                    2 => (cell + 1) % width != 0 ? cell + 1 : -1,
                    _ => cell + width < roomOf.Length ? cell + width : -1,
                };
                if (next < 0 || seen[next] || roomOf[next] != room) continue;
                seen[next] = true;
                order[tail++] = next;
            }
        }
        return tail;
    }

    /// <summary>
    /// The cell nearest the mean of <paramref name="cells"/>, counting steps along rows and columns; the
    /// smallest such cell on a tie. The mean is rounded down (sums are never negative).
    /// </summary>
    private static int MiddleOf(ReadOnlySpan<int> cells, int width)
    {
        long sumX = 0, sumY = 0;
        foreach (int cell in cells)
        {
            sumX += cell % width;
            sumY += cell / width;
        }
        long meanX = sumX / cells.Length, meanY = sumY / cells.Length;
        int best = -1;
        long bestOff = 0;
        foreach (int cell in cells)
        {
            long off = Math.Abs(cell % width - meanX) + Math.Abs(cell / width - meanY);
            if (best < 0 || off < bestOff || (off == bestOff && cell < best)) { best = cell; bestOff = off; }
        }
        return best;
    }

    /// <summary>
    /// The rooms whose order may differ from the one found from <paramref name="before"/>: each a cell
    /// left or joined, as it was and is, and those of <see cref="VentRoomsMoved"/>; ids gone included.
    /// </summary>
    public static List<int> ChangedRooms(
        int[] before, ReadOnlySpan<int> roomOf, IReadOnlyList<int> ventsBefore, IReadOnlyList<int> vents)
    {
        var changed = new List<int>();
        // Most ticks nothing moved: one vectorised compare instead of a walk.
        if (roomOf.SequenceEqual(before) && vents.SequenceEqual(ventsBefore)) return changed;
        for (int cell = 0; cell < roomOf.Length; cell++)
        {
            int was = before[cell];
            if (roomOf[cell] == was) continue;
            if (roomOf[cell] >= 0) changed.Add(roomOf[cell]);
            if (was >= 0) changed.Add(was);
        }
        changed.AddRange(VentRoomsMoved(roomOf, ventsBefore, vents));
        return changed;
    }

    /// <summary>
    /// The rooms, as they are now, whose vents in order differ between <paramref name="before"/> and
    /// <paramref name="vents"/>: a vent came, went, or moved in the order, which its air spreads from.
    /// </summary>
    public static List<int> VentRoomsMoved(ReadOnlySpan<int> roomOf, IReadOnlyList<int> before, IReadOnlyList<int> vents)
    {
        var moved = new List<int>();
        if (before.SequenceEqual(vents)) return moved;
        var had = ByRoom(roomOf, before);
        var has = ByRoom(roomOf, vents);
        foreach (int cell in (IEnumerable<int>)[.. before, .. vents])
        {
            int room = roomOf[cell];
            if (room < 0) continue;
            if (!had.TryGetValue(room, out var was) || !has.TryGetValue(room, out var now) || !was.SequenceEqual(now))
                moved.Add(room);
        }
        return moved;
    }

    private static Dictionary<int, List<int>> ByRoom(ReadOnlySpan<int> roomOf, IReadOnlyList<int> vents)
    {
        var byRoom = new Dictionary<int, List<int>>();
        foreach (int cell in vents)
        {
            if (roomOf[cell] < 0) continue;
            if (byRoom.TryGetValue(roomOf[cell], out var list)) list.Add(cell);
            else byRoom[roomOf[cell]] = [cell];
        }
        return byRoom;
    }
}
