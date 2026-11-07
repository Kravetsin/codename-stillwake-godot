using L = Stillwake.Content.Liquids;

namespace Stillwake.Sim.Liquids;

/// <summary>
/// The flow (docs/plans/liquids.md, Approach): whole rows from the bottom up, along each in the way the clock gives; a cell
/// falls through up to <c>Fall</c> cells, then spreads half its lead to a side neighbour at least 2 lower. Sleeping chunks
/// are skipped, which never changes the result: a chunk sleeps only while none of its cells can move, and any change wakes
/// the chunks round it at once, ahead of the sweep too.
/// </summary>
public sealed partial class LiquidsModule
{
    /// <summary>A cell's byte while it was given liquid sideways this tick, and may pass none sideways: cleared after the sweep.</summary>
    private const byte Given = 0x80;

    /// <summary>The grid's version read last: every change is read the tick it is made, so none is pending at a save.</summary>
    private int _version;

    /// <summary>Derived: whether each chunk may hold a cell that can move, and how many in each row of chunks do.</summary>
    private bool[] _awake = [];
    private int[] _awakeInRow = [];
    private int _chunksAcross;

    /// <summary>Derived: the chunks woken this tick, which stay awake into the next.</summary>
    private bool[] _stirred = [];

    /// <summary>Derived: the cells given liquid sideways this tick.</summary>
    private readonly List<int> _given = [];

    /// <summary>Derived: the cells liquid fell into this tick, in the sweep's order.</summary>
    private readonly List<int> _falls = [];

    private void Tick()
    {
        int dir = State.Clock % 2 == 0 ? 1 : -1;
        State.Clock++;
        _falls.Clear();
        var layer = State.Layer;
        if (layer == null)
        {
            _version = _grid.Version;
            return;
        }
        ReadGrid(layer);
        Sweep(layer, dir);
        foreach (int cell in _given) layer[cell] &= unchecked((byte)~Given);
        _given.Clear();
        Settle(layer);
    }

    /// <summary>What the grid changed since last read: a cell now blocking loses its liquid; every change wakes what is round it.</summary>
    private void ReadGrid(byte[] layer)
    {
        var changes = _grid.ChangedSince(_version);
        _version = _grid.Version;
        if (changes == null)
        {
            Recheck();
            return;
        }
        foreach (var change in changes)
        {
            int cell = change.Cell;
            if (layer[cell] != L.None && _grid.Blocks(cell)) Set(layer, cell, L.None);
            Wake(cell);
        }
    }

    /// <summary>Every cell looked at again: liquid in a blocking cell gone, then every chunk with a cell that can move awake.</summary>
    private void Recheck()
    {
        var layer = State.Layer!;
        for (int cell = 0; cell < layer.Length; cell++)
            if (layer[cell] != L.None && _grid.Blocks(cell)) Set(layer, cell, L.None);
        Array.Clear(_awake);
        Array.Clear(_stirred);
        Array.Clear(_awakeInRow);
        for (int chunk = 0; chunk < _awake.Length; chunk++)
            if (AnyCanMove(layer, chunk % _chunksAcross, chunk / _chunksAcross))
            {
                _awake[chunk] = true;
                _awakeInRow[chunk / _chunksAcross]++;
            }
    }

    private void Sweep(byte[] layer, int dir)
    {
        const int c = L.Chunk;
        for (int cy = _awakeInRow.Length - 1; cy >= 0; cy--)
            for (int y = Math.Min(_height, (cy + 1) * c) - 1; y >= cy * c && _awakeInRow[cy] > 0; y--)
                for (int k = 0; k < _chunksAcross; k++)
                {
                    int cx = dir > 0 ? k : _chunksAcross - 1 - k;
                    if (!_awake[cy * _chunksAcross + cx]) continue;
                    int from = cx * c, to = Math.Min(_width, from + c) - 1;
                    for (int x = dir > 0 ? from : to; x >= from && x <= to; x += dir) Step(layer, y * _width + x, x, y, dir);
                }
    }

    /// <summary>One cell's turn: it falls, then what did not fall spreads, the sweep's way first.</summary>
    private void Step(byte[] layer, int cell, int x, int y, int dir)
    {
        byte at = layer[cell];
        int a = L.AmountOf(at);
        if (a == 0) return;
        byte kind = L.KindOf(at);
        bool given = (at & Given) != 0;
        if (y + 1 < _height && Takes(layer, cell + _width, kind) && L.AmountOf(layer[cell + _width]) < L.Full)
        {
            int to = cell + _width;
            for (int k = 1; k < L.Fall; k++)
            {
                int under = to + _width;
                if (L.AmountOf(layer[to]) != 0 || under >= layer.Length || !Takes(layer, under, kind) || L.AmountOf(layer[under]) >= L.Full) break;
                to = under;
            }
            int f = Math.Min(a, L.Full - L.AmountOf(layer[to]));
            a -= f;
            Put(layer, cell, kind, a, given);
            Pour(layer, to, kind, f, false);
            _falls.Add(to);
        }
        if (a < 2 || given) return;
        for (int side = 0, s = dir; side < 2; side++, s = -s)
        {
            int nx = x + s, n = cell + s;
            if (nx < 0 || nx >= _width || !Takes(layer, n, kind)) continue;
            int b = L.AmountOf(layer[n]);
            if (b > a - 2) continue;
            int f = (a - b) / 2;
            a -= f;
            Put(layer, cell, kind, a, false);
            Pour(layer, n, kind, f, true);
        }
    }

    /// <summary>Whether <paramref name="cell"/> takes liquid of <paramref name="kind"/>: open, and dry or of that kind.</summary>
    private bool Takes(byte[] layer, int cell, byte kind) =>
        !_grid.Blocks(cell) && L.KindOf(layer[cell]) is var k && (k == L.None || k == kind);

    /// <summary>Whether the liquid in <paramref name="cell"/> can move: fall, or spread to a side neighbour at least 2 lower.</summary>
    private bool CanMove(byte[] layer, int cell, int x, int y)
    {
        int a = L.AmountOf(layer[cell]);
        if (a == 0) return false;
        byte kind = L.KindOf(layer[cell]);
        if (y + 1 < _height && Takes(layer, cell + _width, kind) && L.AmountOf(layer[cell + _width]) < L.Full) return true;
        if (a < 2) return false;
        return x > 0 && Takes(layer, cell - 1, kind) && L.AmountOf(layer[cell - 1]) <= a - 2
            || x + 1 < _width && Takes(layer, cell + 1, kind) && L.AmountOf(layer[cell + 1]) <= a - 2;
    }

    private bool AnyCanMove(byte[] layer, int cx, int cy)
    {
        const int c = L.Chunk;
        for (int y = cy * c; y < Math.Min(_height, (cy + 1) * c); y++)
            for (int x = cx * c; x < Math.Min(_width, (cx + 1) * c); x++)
                if (CanMove(layer, y * _width + x, x, y)) return true;
        return false;
    }

    /// <summary>After the sweep: an awake chunk not woken this tick sleeps once none of its cells can move.</summary>
    private void Settle(byte[] layer)
    {
        for (int cy = 0; cy < _awakeInRow.Length; cy++)
        {
            if (_awakeInRow[cy] == 0) continue;
            for (int cx = 0; cx < _chunksAcross; cx++)
            {
                int chunk = cy * _chunksAcross + cx;
                if (!_awake[chunk]) continue;
                if (_stirred[chunk]) _stirred[chunk] = false;
                else if (!AnyCanMove(layer, cx, cy))
                {
                    _awake[chunk] = false;
                    _awakeInRow[cy]--;
                }
            }
        }
    }

    /// <summary>The cell left with <paramref name="amount"/> of <paramref name="kind"/>.</summary>
    private void Put(byte[] layer, int cell, byte kind, int amount, bool given) =>
        Set(layer, cell, (byte)(L.Of(kind, amount) | (given && amount > 0 ? Given : 0)));

    /// <summary>
    /// <paramref name="amount"/> of <paramref name="kind"/> added to <paramref name="cell"/>; outside the zone it is gone (the fog
    /// drains). <paramref name="sideways"/>: given from beside, so it passes none sideways this tick.
    /// </summary>
    private void Pour(byte[] layer, int cell, byte kind, int amount, bool sideways)
    {
        if (!_grid.Zone.Contains(cell % _width, cell / _width))
        {
            Wake(cell);
            return;
        }
        bool given = sideways || (layer[cell] & Given) != 0;
        if (sideways && (layer[cell] & Given) == 0) _given.Add(cell);
        Put(layer, cell, kind, L.AmountOf(layer[cell]) + amount, given);
    }

    /// <summary>A cell written: noted for the save, and the chunks round it woken.</summary>
    private void Set(byte[] layer, int cell, byte value)
    {
        if (layer[cell] == value) return;
        Touch(cell, (byte)(layer[cell] & ~Given));
        layer[cell] = value;
        Wake(cell);
    }

    /// <summary>The chunks of <paramref name="cell"/> and of its four neighbours woken, and kept awake into the next tick.</summary>
    private void Wake(int cell)
    {
        int x = cell % _width, y = cell / _width;
        WakeChunk(x, y);
        if (x > 0) WakeChunk(x - 1, y);
        if (x + 1 < _width) WakeChunk(x + 1, y);
        if (y > 0) WakeChunk(x, y - 1);
        if (y + 1 < _height) WakeChunk(x, y + 1);
    }

    /// <summary>For tests: the chunks awake now.</summary>
    internal int AwakeChunks => _awakeInRow.Sum();

    /// <summary>For tests: whether every chunk with a cell that can move is awake, as the flow's skipping needs.</summary>
    internal bool AwakeWhereItCanMove()
    {
        if (State.Layer is not { } layer) return true;
        for (int chunk = 0; chunk < _awake.Length; chunk++)
            if (!_awake[chunk] && AnyCanMove(layer, chunk % _chunksAcross, chunk / _chunksAcross)) return false;
        return true;
    }

    private void WakeChunk(int x, int y)
    {
        if (_awake.Length == 0) return;
        int cy = y / L.Chunk, chunk = cy * _chunksAcross + x / L.Chunk;
        _stirred[chunk] = true;
        if (_awake[chunk]) return;
        _awake[chunk] = true;
        _awakeInRow[cy]++;
    }
}
