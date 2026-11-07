using L = Stillwake.Content.Liquids;

namespace Stillwake.Sim.Liquids;

/// <summary>
/// Pressure (docs/plans/liquids.md, the playtest of 2026-11-07): a body, the full cells of a kind joined side to side, levels as a whole. Each tick it moves up
/// to <c>Liquids.Press</c> units, a unit at a time, from its highest free surface to the lowest open cell beside it while the
/// two are 2 units apart or more, so a U-tube rises and a pool settles flat. Every body is planned from the layer as the
/// sweep left it, then the moves are made in the order of the bodies' first cells; a move the one before spoilt is dropped.
/// Only bodies with a full cell in an awake chunk are planned, which never changes the result: a body out of level always has
/// one, since whatever puts it out of level wakes the chunks round it and a body that moves keeps its own awake.
/// </summary>
public sealed partial class LiquidsModule
{
    /// <summary>Derived: the cells seen this tick, put in a body or beside one; only added to and looked up, never walked.</summary>
    private readonly HashSet<int> _seen = [];
    private readonly HashSet<int> _near = [];

    /// <summary>Derived: the bodies found this tick, each its first cell and where its cells lie in <see cref="_cells"/>.</summary>
    private readonly List<(int First, int Start, int Count)> _bodies = [];
    private readonly List<int> _cells = [];

    /// <summary>Derived: a body's surfaces and the open cells beside it, and the units it moves, from one cell to another.</summary>
    private readonly List<int> _tops = [];
    private readonly List<int> _sides = [];
    private readonly List<(int Cell, int By)> _change = [];
    private readonly List<(int From, int To)> _moves = [];

    private void Press(byte[] layer)
    {
        Find(layer, everywhere: false);
        _moves.Clear();
        foreach (var body in _bodies) Plan(layer, body.Start, body.Count, L.Press);
        foreach (var (from, to) in _moves)
        {
            byte kind = L.KindOf(layer[from]);
            int a = L.AmountOf(layer[from]);
            if (a == 0 || !Takes(layer, to, kind) || L.AmountOf(layer[to]) >= L.Full) continue;
            Put(layer, from, kind, a - 1, false);
            Pour(layer, to, kind, 1, false);
        }
    }

    /// <summary>The bodies with a full cell in an awake chunk, or <paramref name="everywhere"/>, ordered by their first cells.</summary>
    private void Find(byte[] layer, bool everywhere)
    {
        _seen.Clear();
        _cells.Clear();
        _bodies.Clear();
        const int c = L.Chunk;
        for (int chunk = 0; chunk < _awake.Length; chunk++)
        {
            if (!everywhere && !_awake[chunk]) continue;
            int cx = chunk % _chunksAcross, cy = chunk / _chunksAcross;
            for (int y = cy * c; y < Math.Min(_height, (cy + 1) * c); y++)
                for (int x = cx * c; x < Math.Min(_width, (cx + 1) * c); x++)
                    if (Full(layer, y * _width + x) && _seen.Add(y * _width + x)) Fill(layer, y * _width + x);
        }
        _bodies.Sort((a, b) => a.First.CompareTo(b.First));
    }

    /// <summary>The body holding <paramref name="seed"/>, its cells added to <see cref="_cells"/>.</summary>
    private void Fill(byte[] layer, int seed)
    {
        int start = _cells.Count, first = seed;
        byte kind = L.KindOf(layer[seed]);
        _cells.Add(seed);
        for (int i = start; i < _cells.Count; i++)
        {
            int cell = _cells[i], x = cell % _width;
            first = Math.Min(first, cell);
            if (cell >= _width) Join(cell - _width);
            if (x > 0) Join(cell - 1);
            if (x + 1 < _width) Join(cell + 1);
            if (cell + _width < layer.Length) Join(cell + _width);
        }
        _bodies.Add((first, start, _cells.Count - start));

        void Join(int n)
        {
            if (L.KindOf(layer[n]) == kind && Full(layer, n) && _seen.Add(n)) _cells.Add(n);
        }
    }

    /// <summary>
    /// The moves of the body at <paramref name="start"/>, up to <paramref name="press"/> units, added to <see cref="_moves"/>;
    /// whether it has any. Its free surfaces are its cells open and dry above and the cells of its kind beside it short of full;
    /// the open cells beside it short of full take, but not those under it, which its falling fills. Heights in units from the bottom; a tie goes to the lower cell index.
    /// </summary>
    private bool Plan(byte[] layer, int start, int count, int press)
    {
        _near.Clear();
        _tops.Clear();
        _sides.Clear();
        _change.Clear();
        byte kind = L.KindOf(layer[_cells[start]]);
        for (int i = start; i < start + count; i++)
        {
            int cell = _cells[i], x = cell % _width;
            if (cell >= _width && layer[cell - _width] == L.None && !_grid.Blocks(cell - _width)) _tops.Add(cell);
            if (cell >= _width) Beside(cell - _width);
            if (x > 0) Beside(cell - 1);
            if (x + 1 < _width) Beside(cell + 1);
        }
        bool any = false;
        for (int moved = 0; moved < press; moved++)
        {
            int from = -1, to = -1, high = int.MinValue, low = int.MaxValue;
            foreach (int cell in _tops)
                if (Height(layer, cell) is var h && Now(layer, cell) > 0 && (h > high || h == high && cell < from)) (from, high) = (cell, h);
            foreach (int cell in _sides)
                if (Height(layer, cell) is var h && Now(layer, cell) < L.Full && (h < low || h == low && cell < to)) (to, low) = (cell, h);
            if (from < 0 || to < 0 || high - low < 2) break;
            _moves.Add((from, to));
            _change.Add((from, -1));
            _change.Add((to, 1));
            any = true;
        }
        return any;

        void Beside(int n)
        {
            if (_grid.Blocks(n) || !_near.Add(n)) return;
            byte k = L.KindOf(layer[n]);
            if (k != L.None && k != kind || Full(layer, n)) return;
            _sides.Add(n);
            if (k == kind) _tops.Add(n);
        }
    }

    /// <summary>The units in <paramref name="cell"/> with this plan's moves so far.</summary>
    private int Now(byte[] layer, int cell)
    {
        int a = L.AmountOf(layer[cell]);
        foreach (var (at, by) in _change)
            if (at == cell) a += by;
        return a;
    }

    /// <summary>The top of the liquid in <paramref name="cell"/>, in units from the bottom of the surface.</summary>
    private int Height(byte[] layer, int cell) => (_height - 1 - cell / _width) * L.Full + Now(layer, cell);

    private static bool Full(byte[] layer, int cell) => L.AmountOf(layer[cell]) == L.Full;

    /// <summary>Every body out of level woken where it lies: after a load, or a grid change too big for the log.</summary>
    private void WakeUneven(byte[] layer)
    {
        Find(layer, everywhere: true);
        foreach (var body in _bodies)
        {
            _moves.Clear();
            if (Plan(layer, body.Start, body.Count, 1)) Wake(_cells[body.Start]);
        }
        _moves.Clear();
    }

    /// <summary>For tests: whether every body out of level has a full cell in an awake chunk, as the pressure's skipping needs.</summary>
    private bool UnevenAwake(byte[] layer)
    {
        Find(layer, everywhere: true);
        var uneven = new List<(int Start, int Count)>();
        foreach (var body in _bodies)
        {
            _moves.Clear();
            if (Plan(layer, body.Start, body.Count, 1)) uneven.Add((body.Start, body.Count));
        }
        _moves.Clear();
        foreach (var (start, count) in uneven)
        {
            bool awake = false;
            for (int i = start; i < start + count && !awake; i++)
                awake = _awake[_cells[i] / _width / L.Chunk * _chunksAcross + _cells[i] % _width / L.Chunk];
            if (!awake) return false;
        }
        return true;
    }
}
