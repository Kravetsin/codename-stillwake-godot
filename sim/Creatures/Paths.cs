using static Stillwake.Sim.Creatures.CreaturesModule;
using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// How a step of a path reaches its node (catcher-paths): saved by number, so never renumber one. A flight (1) and a cut (2) went with
/// the catcher's drill (catcher-combat); an older save's are searched again.
/// </summary>
internal enum StepKind
{
    /// <summary>Along the ground, up or down a step, ground under the feet at both ends.</summary>
    Run = 0,

    /// <summary>Flown where things float (catcher-motion): a cell or a diagonal across, the body free all the way.</summary>
    Fly = 3,
}

/// <summary>
/// The path search (catcher-paths): weighted A* over the cells a body stands on, its bottom middle, each run step costing the
/// hundredths of a tick it takes. Its heuristic, the run's cost times the Chebyshev distance, times 3/2 rounded down, weighs fewer
/// places for a path at most half as long again as the best. It looks in a window round start and goal, at most <see cref="Size"/>
/// square round its start, and stops at <see cref="Spec.Paths.Nodes"/> places; short of the goal, it ends at the place weighed nearest
/// it (least estimate, then least cost, then least cell). Its memory is reused, kept apart from search to search by a stamp, and never
/// saved.
/// </summary>
internal sealed class PathFinder
{
    private const int Size = 256, Cells = Size * Size, Pad = 8, Span = Size + 2 * Pad;
    private readonly int[] _g = new int[Cells], _from = new int[Cells], _seen = new int[Cells], _shut = new int[Cells];
    private readonly byte[] _what = new byte[Span * Span];
    private readonly int[] _known = new int[Span * Span];
    private readonly List<(int F, int H, int At)> _heap = [];
    private int _stamp;

    // The window, and what this search moves: set by Find, read by the steps.
    private int _x0, _y0, _x1, _y1, _lo, _hi, _height, _run;
    private CreaturesModule _m = null!;
    private bool _flies;

    /// <summary>
    /// Fills <paramref name="path"/> with the run steps from (<paramref name="sx"/>, <paramref name="sy"/>) toward (<paramref name="gx"/>,
    /// <paramref name="gy"/>) for a body of <paramref name="f"/>, each a cell times four plus its <see cref="StepKind"/>; empty where it
    /// stands best already, or for a body that never runs. One that <paramref name="flies"/> (catcher-motion) takes flown steps, a cell
    /// across, up, down or diagonally, a room's cells as walls. Whether it reached the goal.
    /// </summary>
    internal bool Find(CreaturesModule m, Spec.Flyer f, int sx, int sy, int gx, int gy, List<int> path, bool flies = false)
    {
        (_m, _flies) = (m, flies);
        int width = m.GridWidth;
        _lo = Down(Unit / 2 - f.Width * Unit / 2, Unit);
        _hi = Down(Unit / 2 + f.Width * Unit / 2 - 1, Unit);
        _height = f.Height;
        _run = f.Run > 0 ? Up(100_000, f.Run) : 0;
        int margin = flies ? Spec.Paths.FlownMargin : Spec.Paths.Margin;
        _x0 = Math.Max(Math.Max(0, Math.Min(sx, gx) - margin), sx - Size / 2);
        _x1 = Math.Min(Math.Min(width - 1, Math.Max(sx, gx) + margin), _x0 + Size - 1);
        _y0 = Math.Max(Math.Max(0, Math.Min(sy, gy) - margin), sy - Size / 2);
        _y1 = Math.Min(Math.Min(m.GridHeight - 1, Math.Max(sy, gy) + margin), _y0 + Size - 1);
        path.Clear();
        if (_run == 0 || sx < _x0 || sx > _x1 || sy < _y0 || sy > _y1) return false;
        _stamp += 1;
        _heap.Clear();
        int start = Local(sx, sy), best = start, bestH = Estimate(sx, sy, gx, gy), bestG = 0;
        (_g[start], _from[start], _seen[start]) = (0, -1, _stamp);
        Push((bestH, bestH, start));
        int weighed = 0;
        bool reached = false;
        while (_heap.Count > 0)
        {
            var (_, h, at) = Pop();
            if (_shut[at] == _stamp) continue;
            _shut[at] = _stamp;
            int g = _g[at], x = _x0 + at % Size, y = _y0 + at / Size;
            if (h < bestH || h == bestH && (g < bestG || g == bestG && Global(at, width) < Global(best, width))) (best, bestH, bestG) = (at, h, g);
            if (x == gx && y == gy)
            {
                (best, reached) = (at, true);
                break;
            }
            if (++weighed >= Spec.Paths.Nodes) break;
            Expand(x, y, g, gx, gy);
        }
        for (int at = best; _from[at] >= 0; at = _from[at]) path.Add(Global(at, width) * 4 + (int)(_flies ? StepKind.Fly : StepKind.Run));
        path.Reverse();
        return reached;
    }

    /// <summary>Every run from (<paramref name="x"/>, <paramref name="y"/>), standing on ground, in a fixed order: left then right, each from the highest step.</summary>
    private void Expand(int x, int y, int g, int gx, int gy)
    {
        if (_flies)
        {
            Fly(x, y, g, gx, gy);
            return;
        }
        if (!Grounded(x, y)) return;
        int here = Local(x, y);
        for (int dx = -1; dx <= 1; dx += 2)
            for (int dy = -Spec.StepUp; dy <= Spec.StepUp; dy++)
                if (RunsTo(x, y, dx, dy)) Offer(here, x + dx, y + dy, g + _run, gx, gy);
    }

    /// <summary>
    /// Every flown step from (<paramref name="x"/>, <paramref name="y"/>), in a fixed order, rows from the top, columns from the left: a
    /// diagonal one only where the body is free going either way round its corner, costing seven fifths of a straight one.
    /// </summary>
    private void Fly(int x, int y, int g, int gx, int gy)
    {
        int here = Local(x, y);
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                bool free = dx == 0 ? Open(x, y, 0, dy)
                    : dy == 0 ? Open(x, y, dx, 0)
                    : Open(x, y, dx, 0) && Open(x + dx, y, 0, dy) && Open(x, y, 0, dy) && Open(x, y + dy, dx, 0);
                if (free) Offer(here, x + dx, y + dy, g + (dx != 0 && dy != 0 ? _run * 7 / 5 : _run), gx, gy);
            }
    }

    /// <summary>A step to (<paramref name="x"/>, <paramref name="y"/>) from <paramref name="from"/> at cost <paramref name="g"/>: kept only where strictly cheaper.</summary>
    private void Offer(int from, int x, int y, int g, int gx, int gy)
    {
        if (x < _x0 || x > _x1 || y < _y0 || y > _y1) return;
        int at = Local(x, y);
        if (_shut[at] == _stamp || _seen[at] == _stamp && _g[at] <= g) return;
        (_g[at], _from[at], _seen[at]) = (g, from, _stamp);
        int h = Estimate(x, y, gx, gy);
        Push((g + h, h, at));
    }
    /// <summary>
    /// Whether a run from (<paramref name="x"/>, <paramref name="y"/>) one column <paramref name="dx"/> and <paramref name="dy"/> rows
    /// lands on ground with the body free all the way: up a step it rises first, down one it moves across first.
    /// </summary>
    private bool RunsTo(int x, int y, int dx, int dy)
    {
        if (dy < 0)
        {
            for (int k = 1; k <= -dy; k++)
                if (!Open(x, y - k + 1, 0, -1)) return false;
            return Open(x, y + dy, dx, 0) && Grounded(x + dx, y + dy);
        }
        if (!Open(x, y, dx, 0)) return false;
        for (int k = 1; k <= dy; k++)
            if (!Open(x + dx, y + k - 1, 0, 1)) return false;
        return Grounded(x + dx, y + dy);
    }

    /// <summary>Whether the body standing on (<paramref name="x"/>, <paramref name="y"/>) has something solid under its feet.</summary>
    private bool Grounded(int x, int y)
    {
        for (int column = x + _lo; column <= x + _hi; column++)
            if (Solid(column, y + 1)) return true;
        return false;
    }

    /// <summary>Whether the cells the body newly covers moving one cell (<paramref name="dx"/>, <paramref name="dy"/>), straight, are open.</summary>
    private bool Open(int x, int y, int dx, int dy)
    {
        if (dx != 0)
        {
            int column = dx > 0 ? x + _hi + 1 : x + _lo - 1;
            for (int row = y - _height + 1; row <= y; row++)
                if (Solid(column, row)) return false;
            return true;
        }
        int line = dy > 0 ? y + 1 : y - _height;
        for (int column = x + _lo; column <= x + _hi; column++)
            if (Solid(column, line)) return false;
        return true;
    }

    // What a cell is to this search, looked up once a search.
    private const byte Unknown = 0, Free = 1, Held = 2;

    private bool Solid(int column, int row)
    {
        int lx = column - _x0 + Pad, ly = row - _y0 + Pad;
        if ((uint)lx >= Span || (uint)ly >= Span) return Blocks(column, row);
        int at = ly * Span + lx;
        if (_known[at] != _stamp) (_what[at], _known[at]) = (Unknown, _stamp);
        if (_what[at] == Unknown) _what[at] = Blocks(column, row) ? Held : Free;
        return _what[at] == Held;
    }

    /// <summary>What stops the body: anything solid; flying, a room's cell too, where it never goes (catchers-space).</summary>
    private bool Blocks(int column, int row) =>
        _m.Solid(column, row) || _flies && column >= 0 && row >= 0 && column < _m.GridWidth && row < _m.GridHeight && _m.InRoom(row * _m.GridWidth + column);

    private int Estimate(int x, int y, int gx, int gy) =>
        (int)Math.Min(int.MaxValue / 4, (long)_run * Math.Max(Math.Abs(x - gx), Math.Abs(y - gy)) * 3 / 2);

    private int Local(int x, int y) => (x - _x0) + (y - _y0) * Size;

    private int Global(int at, int width) => (_y0 + at / Size) * width + _x0 + at % Size;

    private static int Up(int a, int b) => (a + b - 1) / b;

    private void Push((int F, int H, int At) item)
    {
        _heap.Add(item);
        for (int i = _heap.Count - 1; i > 0;)
        {
            int parent = (i - 1) / 2;
            if (_heap[parent].CompareTo(_heap[i]) <= 0) break;
            (_heap[parent], _heap[i]) = (_heap[i], _heap[parent]);
            i = parent;
        }
    }

    private (int F, int H, int At) Pop()
    {
        var top = _heap[0];
        _heap[0] = _heap[^1];
        _heap.RemoveAt(_heap.Count - 1);
        for (int i = 0; ;)
        {
            int l = 2 * i + 1, r = l + 1, least = i;
            if (l < _heap.Count && _heap[l].CompareTo(_heap[least]) < 0) least = l;
            if (r < _heap.Count && _heap[r].CompareTo(_heap[least]) < 0) least = r;
            if (least == i) break;
            (_heap[least], _heap[i]) = (_heap[i], _heap[least]);
            i = least;
        }
        return top;
    }
}
