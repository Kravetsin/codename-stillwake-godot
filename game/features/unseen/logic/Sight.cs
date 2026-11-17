using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Unseen;

/// <summary>
/// What of a derelict's inside its crew has seen (hidden-rooms), over a frame of cells, each dark till seen. An open cell is
/// seen once a hero's body stands in its space: every open cell joined to the hero's through open ones, stopped by what seals
/// (walls, shut doors and hatches), so a door opened beside a hero shows the space past it. Seen stays seen. A sealed cell is
/// dark while unseen space lies on both its sides, within two cells across or up and down: a shut door or a deck's floor
/// between two unseen rooms, never the hull, which has space past it. Godot-free; <c>UnseenView</c> draws it.
/// </summary>
public sealed class Sight
{
    /// <summary>How long a cell's dark takes to go once it is seen, in milliseconds.</summary>
    public const double FadeMs = 300;

    /// <summary>How far a sealed cell looks each way for unseen space: a hull wall is two cells thick (0104).</summary>
    private const int Across = 2;

    private readonly int _width, _height;
    private readonly Func<int, bool> _open, _seals;
    private readonly bool[] _seen, _covered;
    private readonly float[] _dark;
    private readonly int[] _queue, _visited;
    private int _stamp;
    private bool _first = true, _fading;

    /// <param name="open">Whether a cell of the frame is space inside: open, with a back wall.</param>
    /// <param name="seals">Whether a cell of the frame stops sight: a wall, a shut door or hatch.</param>
    public Sight(int width, int height, Func<int, bool> open, Func<int, bool> seals)
    {
        (_width, _height, _open, _seals) = (width, height, open, seals);
        int n = width * height;
        (_seen, _covered, _dark, _queue, _visited) = (new bool[n], new bool[n], new float[n], new int[n], new int[n]);
    }

    public int Width => _width;
    public int Height => _height;

    /// <summary>How dark a cell of the frame is: 1 unseen, 0 seen, between while its dark goes.</summary>
    public float Dark(int cell) => _dark[cell];

    /// <summary>Whether a cell of the frame is still dark: what the pointer tells nothing of.</summary>
    public bool Hides(int cell) => _covered[cell];

    /// <summary>
    /// Looks again from where the heroes stand (<paramref name="heroes"/>, cells of the frame, -1 for one outside it), after
    /// <paramref name="elapsedMs"/>; <paramref name="changed"/> when what is open or sealed may have changed since the last look.
    /// The first look shows at once what the heroes stand in, later ones fade it in. True when any cell's dark changed.
    /// </summary>
    public bool Look(IReadOnlyList<int> heroes, bool changed, double elapsedMs)
    {
        bool again = changed || _first;
        foreach (int c in heroes)
            if (c >= 0 && _open(c) && !_seen[c]) again = true;
        bool moved = false;
        if (again)
        {
            _stamp++;
            foreach (int c in heroes)
                if (c >= 0 && _open(c)) Spread(c);
            moved = Settle(instant: _first);
            _first = false;
        }
        if (_fading) moved |= Fade(elapsedMs);
        return moved;
    }

    /// <summary>Marks seen every open cell joined to <paramref name="from"/>, through seen ones too: a door may have opened past them.</summary>
    private void Spread(int from)
    {
        if (_visited[from] == _stamp) return;
        int head = 0, tail = 0;
        _queue[tail++] = from;
        _visited[from] = _stamp;
        while (head < tail)
        {
            int c = _queue[head++];
            _seen[c] = true;
            int x = c % _width, y = c / _width;
            if (x > 0) Visit(c - 1);
            if (x + 1 < _width) Visit(c + 1);
            if (y > 0) Visit(c - _width);
            if (y + 1 < _height) Visit(c + _width);
        }

        void Visit(int n)
        {
            if (_visited[n] == _stamp || !_open(n)) return;
            _visited[n] = _stamp;
            _queue[tail++] = n;
        }
    }

    private bool Hidden(int cell) => _open(cell) && !_seen[cell];

    /// <summary>
    /// Whether the dark lies a step or two from a sealed <paramref name="cell"/> towards (dx, dy), only sealed cells between: unseen
    /// space, or a sealed cell already dark (the floor under a shut door between unseen rooms).
    /// </summary>
    private bool DarkToward(int cell, int dx, int dy)
    {
        int x = cell % _width, y = cell / _width;
        for (int k = 1; k <= Across; k++)
        {
            int nx = x + dx * k, ny = y + dy * k;
            if (nx < 0 || ny < 0 || nx >= _width || ny >= _height) return false;
            int n = ny * _width + nx;
            if (_covered[n]) return true;
            if (!_seals(n)) return false;
        }
        return false;
    }

    private bool Between(int cell) =>
        (DarkToward(cell, -1, 0) && DarkToward(cell, 1, 0)) || (DarkToward(cell, 0, -1) && DarkToward(cell, 0, 1));

    /// <summary>
    /// What is covered now: unseen space, then sealed cells with the dark on both sides, again till none joins (a few rounds: the
    /// dark reaches at most a door's height through what seals). A cell newly covered goes dark at once, one uncovered fades, or goes
    /// at once on the first look.
    /// </summary>
    private bool Settle(bool instant)
    {
        for (int c = 0; c < _covered.Length; c++) _covered[c] = Hidden(c);
        for (bool grew = true; grew;)
        {
            grew = false;
            for (int c = 0; c < _covered.Length; c++)
                if (!_covered[c] && _seals(c) && Between(c)) _covered[c] = grew = true;
        }
        bool moved = false;
        for (int c = 0; c < _dark.Length; c++)
        {
            bool covered = _covered[c];
            if (covered && _dark[c] < 1)
                (_dark[c], moved) = (1, true);
            else if (!covered && _dark[c] > 0)
            {
                if (instant) (_dark[c], moved) = (0, true);
                else _fading = true;
            }
        }
        return moved;
    }

    private bool Fade(double elapsedMs)
    {
        float step = (float)(elapsedMs / FadeMs);
        bool moved = false, left = false;
        for (int c = 0; c < _dark.Length; c++)
        {
            if (_covered[c] || _dark[c] <= 0) continue;
            _dark[c] = Math.Max(0, _dark[c] - step);
            moved = true;
            left |= _dark[c] > 0;
        }
        _fading = left;
        return moved;
    }
}
