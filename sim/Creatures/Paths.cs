using static Stillwake.Sim.Creatures.CreaturesModule;
using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>How a step of a path reaches its node (catcher-paths): saved by number, so never renumber one.</summary>
internal enum StepKind
{
    /// <summary>Along the ground, up or down a step, ground under the feet at both ends.</summary>
    Run = 0,
    /// <summary>Through open air, any of eight ways.</summary>
    Fly = 1,
    /// <summary>Straight into what it breaks on its way (<c>Building.Wear</c>).</summary>
    Cut = 2,
}

/// <summary>
/// The path search (catcher-paths): weighted A* over the cells a flyer's body stands on, its bottom middle, each step costing the
/// hundredths of a tick it takes: running, flying, or flying while breaking what is in the way. Its heuristic, the run's cost
/// times the Chebyshev distance, times 3/2 rounded down, weighs fewer places for a path at most half as long again as the best.
/// It looks in a window round start and goal, at most <see cref="Size"/> square round its start, and stops at
/// <see cref="Spec.Paths.Nodes"/> places; short of the goal, it ends at the place weighed nearest it (least estimate, then least
/// cost, then least cell). Its memory is reused, kept apart from search to search by a stamp, and never saved.
/// </summary>
internal sealed class PathFinder
{
    private const int Size = 256, Cells = Size * Size, Pad = 8, Span = Size + 2 * Pad;
    private readonly int[] _g = new int[Cells], _from = new int[Cells], _seen = new int[Cells], _shut = new int[Cells];
    private readonly byte[] _kind = new byte[Cells], _what = new byte[Span * Span];
    private readonly int[] _known = new int[Span * Span];
    private readonly List<(int F, int H, int At)> _heap = [];
    private int _stamp;

    // The window, and what this search moves: set by Find, read by the steps.
    private int _x0, _y0, _x1, _y1, _lo, _hi, _height, _run, _fly, _cutEvery, _wear;
    private CreaturesModule _m = null!;

    /// <summary>
    /// Fills <paramref name="path"/> with the steps from (<paramref name="sx"/>, <paramref name="sy"/>) toward (<paramref name="gx"/>,
    /// <paramref name="gy"/>) for a body of <paramref name="f"/> flying at <paramref name="speed"/>, each a cell times four plus its
    /// <see cref="StepKind"/>; empty where it stands best already. Whether it reached the goal.
    /// </summary>
    internal bool Find(CreaturesModule m, Spec.Flyer f, int speed, int sx, int sy, int gx, int gy, List<int> path)
    {
        _m = m;
        int width = m.GridWidth;
        _lo = Down(Unit / 2 - f.Width * Unit / 2, Unit);
        _hi = Down(Unit / 2 + f.Width * Unit / 2 - 1, Unit);
        _height = f.Height;
        _fly = Up(100_000, speed);
        _run = f.Run > 0 ? Up(100_000, f.Run) : 0;
        (_wear, _cutEvery) = (f.Wear, f.CutEvery);
        _x0 = Math.Max(Math.Max(0, Math.Min(sx, gx) - Spec.Paths.Margin), sx - Size / 2);
        _x1 = Math.Min(Math.Min(width - 1, Math.Max(sx, gx) + Spec.Paths.Margin), _x0 + Size - 1);
        _y0 = Math.Max(Math.Max(0, Math.Min(sy, gy) - Spec.Paths.Margin), sy - Size / 2);
        _y1 = Math.Min(Math.Min(m.GridHeight - 1, Math.Max(sy, gy) + Spec.Paths.Margin), _y0 + Size - 1);
        path.Clear();
        if (sx < _x0 || sx > _x1 || sy < _y0 || sy > _y1) return false;
        _stamp += 1;
        _heap.Clear();
        int unit = _run > 0 ? _run : _fly;
        int start = Local(sx, sy), best = start, bestH = Estimate(sx, sy, gx, gy, unit), bestG = 0;
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
            Expand(x, y, g, gx, gy, unit);
        }
        for (int at = best; _from[at] >= 0; at = _from[at]) path.Add(Global(at, width) * 4 + _kind[at]);
        path.Reverse();
        return reached;
    }

    /// <summary>Every step from (<paramref name="x"/>, <paramref name="y"/>), in a fixed order: runs, then flights and cuts.</summary>
    private void Expand(int x, int y, int g, int gx, int gy, int unit)
    {
        int here = Local(x, y);
        if (_run > 0 && Grounded(x, y))
            for (int dx = -1; dx <= 1; dx += 2)
                for (int dy = -Spec.StepUp; dy <= Spec.StepUp; dy++)
                    if (RunsTo(x, y, dx, dy)) Offer(here, x + dx, y + dy, g + _run, StepKind.Run, gx, gy, unit);
        for (int way = 0; way < 8; way++)
        {
            var (dx, dy) = Ways[way];
            if (dx != 0 && dy != 0)
            {
                if (Open(x, y, dx, 0) && Open(x, y, 0, dy) && !Solid(dx > 0 ? x + _hi + 1 : x + _lo - 1, dy > 0 ? y + 1 : y - _height))
                    Offer(here, x + dx, y + dy, g + _fly, StepKind.Fly, gx, gy, unit);
            }
            else if (Open(x, y, dx, dy)) Offer(here, x + dx, y + dy, g + _fly, StepKind.Fly, gx, gy, unit);
            else if (Strikes(x, y, dx, dy) is int strikes) Offer(here, x + dx, y + dy, g + _fly + strikes * _cutEvery * 100, StepKind.Cut, gx, gy, unit);
        }
    }

    /// <summary>The eight ways, straight first.</summary>
    private static readonly (int X, int Y)[] Ways = [(1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)];

    /// <summary>A step to (<paramref name="x"/>, <paramref name="y"/>) from <paramref name="from"/> at cost <paramref name="g"/>: kept only where strictly cheaper.</summary>
    private void Offer(int from, int x, int y, int g, StepKind kind, int gx, int gy, int unit)
    {
        if (x < _x0 || x > _x1 || y < _y0 || y > _y1) return;
        int at = Local(x, y);
        if (_shut[at] == _stamp || _seen[at] == _stamp && _g[at] <= g) return;
        (_g[at], _from[at], _seen[at], _kind[at]) = (g, from, _stamp, (byte)kind);
        int h = Estimate(x, y, gx, gy, unit);
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

    /// <summary>How many of its strikes break the dearest of the cells it newly covers moving straight; null where one does not break.</summary>
    private int? Strikes(int x, int y, int dx, int dy)
    {
        if (_wear == 0) return null;
        int most = 0;
        if (dx != 0)
        {
            int column = dx > 0 ? x + _hi + 1 : x + _lo - 1;
            for (int row = y - _height + 1; row <= y; row++)
                if (!Take(column, row, ref most)) return null;
        }
        else
        {
            int line = dy > 0 ? y + 1 : y - _height;
            for (int column = x + _lo; column <= x + _hi; column++)
                if (!Take(column, line, ref most)) return null;
        }
        return most;
    }

    private bool Take(int column, int row, ref int most)
    {
        int what = What(column, row, strikes: true);
        if (what == Free) return true;
        if (what == Holds) return false;
        most = Math.Max(most, what - Breaks);
        return true;
    }

    // What a cell is to this search, looked up once a search: free, solid not yet weighed, solid for good, or Breaks + its strikes.
    private const int Unknown = 0, Free = 1, Unweighed = 2, Holds = 3, Breaks = 4;

    private bool Solid(int column, int row) => What(column, row, strikes: false) != Free;

    private int What(int column, int row, bool strikes)
    {
        int lx = column - _x0 + Pad, ly = row - _y0 + Pad;
        if ((uint)lx >= Span || (uint)ly >= Span) return Look(column, row, strikes);
        int at = ly * Span + lx;
        if (_known[at] != _stamp) (_what[at], _known[at]) = (Unknown, _stamp);
        if (_what[at] == Unknown || strikes && _what[at] == Unweighed) _what[at] = (byte)Look(column, row, strikes);
        return _what[at];
    }

    private int Look(int column, int row, bool strikes)
    {
        if (!_m.Solid(column, row)) return Free;
        if (!strikes) return Unweighed;
        int width = _m.GridWidth;
        if (column < 0 || row < 0 || column >= width || row >= _m.GridHeight || _m.Strikes(row * width + column, _wear) is not int s) return Holds;
        return Breaks + Math.Min(s, 250);
    }

    private static int Estimate(int x, int y, int gx, int gy, int unit) =>
        (int)Math.Min(int.MaxValue / 4, (long)unit * Math.Max(Math.Abs(x - gx), Math.Abs(y - gy)) * 3 / 2);

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
