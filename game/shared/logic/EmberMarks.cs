using System;
using System.Collections.Generic;
using I = Stillwake.Game.Shared.DroneInk;

namespace Stillwake.Game.Shared;

/// <summary>
/// What an ember look draws this frame, gathered so its drawer makes a few calls (the drone's windows' corners, the thread icons): lines
/// as runs of points, squares and halos, each burnt by its share from the coal's ember into the player's colour (<see cref="Salts"/>), paled
/// toward white by another, at an alpha, added over what is under; a spark's dark square is the spark's dark, laid under. Client only.
/// </summary>
public sealed class EmberMarks
{
    /// <summary>A run of <see cref="Count"/> points from <see cref="From"/> in <see cref="Points"/>, so wide, so pale, at that alpha, so burnt; a run's of an icon.</summary>
    public readonly record struct Line(int From, int Count, float Width, float Pale, float Alpha, float Burn = 1, bool Run = false);

    /// <summary>A square about (<see cref="X"/>, <see cref="Y"/>), so pale, at that alpha, so burnt; a dark one is a spark's dark, laid under.</summary>
    public readonly record struct Square(float X, float Y, float Side, float Pale, float Alpha, bool Dark = false, float Burn = 1, bool Run = false);

    /// <summary>A soft disc, at its alpha in the middle fading to none at its rim.</summary>
    public readonly record struct Halo(float X, float Y, float Radius, float Pale, float Alpha, float Burn = 1, bool Run = false);

    private readonly List<float> _points = [];
    private readonly List<Line> _lines = [];
    private readonly List<Square> _squares = [];
    private readonly List<Halo> _halos = [];
    private int _from = -1;

    /// <summary>The lines' points, x and y in turn.</summary>
    public IReadOnlyList<float> Points => _points;

    public IReadOnlyList<Line> Lines => _lines;

    public IReadOnlyList<Square> Squares => _squares;

    public IReadOnlyList<Halo> Halos => _halos;

    /// <summary>Whether anything lights: a line, a glowing square or a halo above the faintest.</summary>
    public bool Lit
    {
        get
        {
            foreach (var line in _lines)
                if (line.Alpha > 0.004f) return true;
            foreach (var square in _squares)
                if (!square.Dark && square.Alpha > 0.004f) return true;
            foreach (var halo in _halos)
                if (halo.Alpha > 0.004f) return true;
            return false;
        }
    }

    public void Clear()
    {
        _points.Clear();
        _lines.Clear();
        _squares.Clear();
        _halos.Clear();
        _from = -1;
    }

    /// <summary>Starts a run of points; <see cref="To"/> adds to it, <see cref="End"/> ends it.</summary>
    public void Begin() => _from = _points.Count;

    public void To(float x, float y)
    {
        _points.Add(x);
        _points.Add(y);
    }

    /// <summary>Ends the run begun, a line if it has two points, else nothing.</summary>
    public void End(float width, float pale, float alpha, float burn = 1, bool run = false)
    {
        int count = (_points.Count - _from) / 2;
        if (count >= 2 && alpha > 0.003f) _lines.Add(new Line(_from, count, width, pale, alpha, burn, run));
        else _points.RemoveRange(_from, _points.Count - _from);
        _from = -1;
    }

    /// <summary>A straight line from one point to another.</summary>
    public void Segment(float x1, float y1, float x2, float y2, float width, float pale, float alpha, float burn = 1, bool run = false)
    {
        if (alpha <= 0.003f) return;
        Begin();
        To(x1, y1);
        To(x2, y2);
        End(width, pale, alpha, burn, run);
    }

    public void Add(Square square)
    {
        if (square.Alpha > 0.003f) _squares.Add(square);
    }

    public void Add(Halo halo)
    {
        if (halo.Alpha > 0.005f) _halos.Add(halo);
    }

    /// <summary>
    /// The kit's spark at (<paramref name="x"/>, <paramref name="y"/>) heading (<paramref name="dx"/>, <paramref name="dy"/>): its dark
    /// square at <paramref name="dark"/>, then <paramref name="heat"/> hot a square and two behind it, paler and fainter (the threads' tail).
    /// </summary>
    public void Spark(float x, float y, float dx, float dy, float heat, float side = I.Spark, float dark = 1, float burn = 1)
    {
        if (dark > 0.003f) Add(new Square(x, y, side, 0, dark, Dark: true));
        if (heat <= 0.02f) return;
        heat = Math.Min(1, heat);
        for (int m = 0; m < I.Tail; m++)
        {
            float back = m * I.TailStep;
            Add(new Square(x - dx * back, y - dy * back, side, I.TailPale * (1 - m / (float)I.Tail), (I.TailAlpha - m * I.TailFade) * heat, Burn: burn));
        }
    }

    /// <summary>The kit's glowing square, <paramref name="heat"/> hot, <paramref name="burn"/> burnt.</summary>
    public void Glow(float x, float y, float side, float heat, float burn = 1)
    {
        if (heat <= 0.02f) return;
        heat = Math.Min(1, heat);
        Add(new Square(x, y, side, 0.25f * heat, 0.95f * heat, Burn: burn));
    }
}
