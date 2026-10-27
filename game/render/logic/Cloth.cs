using System;
using System.Collections.Generic;

namespace Stillwake.Game.Render;

/// <summary>
/// How a cloth moves, in its caller's units and steps of 1/60 of a second: what each point <see cref="Keep"/>s of its
/// speed a step, what <see cref="Gravity"/> adds to its downward speed, 0 out in space; how hard its diagonal links hold
/// (<see cref="Shear"/>, 0 to 1), so it keeps its shape rather than folding flat; passes over the links a step.
/// </summary>
public sealed record ClothStyle(double Keep, double Gravity, double Shear, int Passes);

/// <summary>
/// A sheet of cloth (a cape): a grid of points, <c>cols</c> across and <c>rows</c> down, joined across, down and
/// diagonally at the lengths they start at (down only pulling, as a thread does), carried on by their speed and pulled by gravity, its pinned points held
/// where they are told. Stepped up to a step ahead of the clock and drawn between its last two steps, as a
/// <see cref="Rope"/> is. Drawn only, never simulated: every peer moves its own.
/// </summary>
public sealed class Cloth
{
    private readonly ClothStyle _style;
    private readonly int _cols, _rows;
    private readonly double[] _x, _y, _px, _py, _bx, _by;
    private readonly bool[] _pinned;
    private readonly List<(int A, int B, double Length, double Stiff, bool PullOnly)> _links = [];
    private double _owed;

    /// <param name="start">Every point where it starts, row by row from the top, at rest: the lengths it keeps.</param>
    public Cloth(ClothStyle style, int cols, int rows, IReadOnlyList<(double X, double Y)> start)
    {
        if (start.Count != cols * rows) throw new ArgumentException("a point for every place of the grid", nameof(start));
        (_style, _cols, _rows) = (style, cols, rows);
        int n = cols * rows;
        (_x, _y, _px, _py, _bx, _by) = (new double[n], new double[n], new double[n], new double[n], new double[n], new double[n]);
        _pinned = new bool[n];
        for (int i = 0; i < n; i++) (_x[i], _y[i], _px[i], _py[i], _bx[i], _by[i]) = (start[i].X, start[i].Y, start[i].X, start[i].Y, start[i].X, start[i].Y);
        void Link(int a, int b, double stiff, bool pullOnly = false) => _links.Add((a, b, Math.Sqrt((_x[a] - _x[b]) * (_x[a] - _x[b]) + (_y[a] - _y[b]) * (_y[a] - _y[b])), stiff, pullOnly));
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                int i = r * cols + c;
                if (c + 1 < cols) Link(i, i + 1, 1);
                if (r + 1 < rows) Link(i, i + cols, 1, pullOnly: true);
                if (c + 1 < cols && r + 1 < rows) { Link(i, i + cols + 1, style.Shear); Link(i + 1, i + cols, style.Shear); }
            }
    }

    public int Cols => _cols;
    public int Rows => _rows;

    /// <summary>Point <paramref name="i"/> (row by row from the top) as drawn: between its last two steps.</summary>
    public (double X, double Y) DrawnAt(int i)
    {
        double t = 1 + _owed / Rope.StepMs;
        return (_bx[i] + (_x[i] - _bx[i]) * t, _by[i] + (_y[i] - _by[i]) * t);
    }

    /// <summary>Point <paramref name="i"/> as it last stepped.</summary>
    public (double X, double Y) At(int i) => (_x[i], _y[i]);

    /// <summary>
    /// Moves the cloth on by <paramref name="elapsedMs"/>, the points <paramref name="pinned"/> names (each by its index)
    /// held at <paramref name="pins"/>. <paramref name="keep"/> moves a free point out of where it may not be, on every
    /// pass, its speed kept: the body it hangs on, the floor.
    /// </summary>
    public void Update(IReadOnlyList<int> pinned, Func<int, (double X, double Y)> pins, double elapsedMs, Func<double, double, (double X, double Y)>? keep = null)
    {
        _owed += elapsedMs;
        int steps = 0;
        while (_owed > 0 && steps < 4)
        {
            Array.Copy(_x, _bx, _x.Length);
            Array.Copy(_y, _by, _y.Length);
            Step(pinned, pins, keep);
            _owed -= Rope.StepMs;
            steps += 1;
        }
        if (_owed > 0) _owed = 0;
        Pin(pinned, pins);
    }

    private void Pin(IReadOnlyList<int> pinned, Func<int, (double X, double Y)> pins)
    {
        foreach (int i in pinned)
        {
            var (x, y) = pins(i);
            (_x[i], _y[i], _px[i], _py[i]) = (x, y, x, y);
        }
    }

    private void Step(IReadOnlyList<int> pinned, Func<int, (double X, double Y)> pins, Func<double, double, (double X, double Y)>? keep)
    {
        var isPinned = _pinned;
        Array.Clear(isPinned);
        foreach (int i in pinned) isPinned[i] = true;
        for (int i = 0; i < _x.Length; i++)
        {
            if (isPinned[i]) continue;
            double vx = (_x[i] - _px[i]) * _style.Keep, vy = (_y[i] - _py[i]) * _style.Keep + _style.Gravity;
            (_px[i], _py[i]) = (_x[i], _y[i]);
            _x[i] += vx;
            _y[i] += vy;
        }
        for (int pass = 0; pass < _style.Passes; pass++)
        {
            Pin(pinned, pins);
            foreach (var (a, b, length, stiff, pullOnly) in _links)
            {
                double dx = _x[b] - _x[a], dy = _y[b] - _y[a], d = Math.Sqrt(dx * dx + dy * dy);
                if (d == 0 || (pullOnly && d <= length)) continue;
                // Across held both ways, so the sheet keeps its width; down only pulled, so it bunches and folds. A pinned
                // point stays, the other takes the whole of it.
                double move = (d - length) / d * stiff;
                double atA = isPinned[a] ? 0 : isPinned[b] ? 1 : 0.5, atB = isPinned[b] ? 0 : isPinned[a] ? 1 : 0.5;
                _x[a] += dx * move * atA;
                _y[a] += dy * move * atA;
                _x[b] -= dx * move * atB;
                _y[b] -= dy * move * atB;
            }
            if (keep == null) continue;
            for (int i = 0; i < _x.Length; i++)
            {
                if (isPinned[i]) continue;
                var (x, y) = keep(_x[i], _y[i]);
                _px[i] += x - _x[i];
                _py[i] += y - _y[i];
                (_x[i], _y[i]) = (x, y);
            }
        }
        Pin(pinned, pins);
    }
}
