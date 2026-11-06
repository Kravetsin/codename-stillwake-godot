using System;
using System.Collections.Generic;

namespace Stillwake.Game.Shared;

/// <summary>
/// A lamp of the suit gauge lost whole (air-feedback, as picked): it flashes white, a chip of it jumps off, tumbles and falls out
/// of the tab, and pale sparks scatter, so each lamp lost is seen from the corner of the eye. Plain C#, in the tab's pixels; its
/// randomness the client's.
/// </summary>
public sealed class LampFlakes(Random random)
{
    /// <summary>How long a lost lamp flashes white and its light round it lasts; a chip's life and its fall.</summary>
    public const double FlashS = 0.3, GlowS = 0.6, ChipS = 1.1, ChipGravity = 230;
    /// <summary>How many sparks a lamp throws.</summary>
    public const int Sparks = 6;

    /// <summary>A chip off a lamp: where it is, how it turns, its size; its age.</summary>
    public sealed class Chip
    {
        public float X, Y, Vx, Vy, Turn, Spin, W, H;
        public double Age;
    }

    /// <summary>A spark: where it is and how it flies, its age and life.</summary>
    public sealed class Spark
    {
        public float X, Y, Vx, Vy;
        public double Age, Life;
    }

    private readonly List<Chip> _chips = [];
    private readonly List<Spark> _sparks = [];
    private readonly List<(int Lamp, double Age)> _pops = [];
    private int? _last;
    private double _fill;

    public IReadOnlyList<Chip> Chips => _chips;
    public IReadOnlyList<Spark> SparksFlying => _sparks;
    /// <summary>The lamps lost lately and how long since.</summary>
    public IReadOnlyList<(int Lamp, double Age)> Pops => _pops;

    /// <summary>
    /// Moves it on by <paramref name="seconds"/> as the gauge shows <paramref name="fill"/>, its last lit lamp
    /// <paramref name="lastLit"/>; a lamp gone dark while the fill fell pops off <paramref name="cells"/>, each its left and
    /// width, the lamps' row from <paramref name="top"/> <paramref name="height"/> high.
    /// </summary>
    public void Step(double seconds, double fill, int lastLit, IReadOnlyList<(float X, float W)> cells, float top, float height)
    {
        for (int i = 0; i < _pops.Count; i++) _pops[i] = (_pops[i].Lamp, _pops[i].Age + seconds);
        _pops.RemoveAll(p => p.Age >= GlowS);
        if (_last is int was && lastLit < was && fill < _fill)
            for (int i = Math.Max(0, lastLit + 1); i <= was && i < cells.Count; i++) Pop(i, cells[i], top, height);
        (_last, _fill) = (lastLit, fill);
        float s = (float)seconds;
        foreach (var c in _chips)
        {
            c.Age += seconds;
            c.Vy += (float)ChipGravity * s;
            (c.X, c.Y, c.Turn) = (c.X + c.Vx * s, c.Y + c.Vy * s, c.Turn + c.Spin * s);
        }
        _chips.RemoveAll(c => c.Age >= ChipS);
        float drag = MathF.Exp(-3 * s);
        foreach (var p in _sparks)
        {
            p.Age += seconds;
            (p.Vx, p.Vy) = (p.Vx * drag, p.Vy * drag + 60 * s);
            (p.X, p.Y) = (p.X + p.Vx * s, p.Y + p.Vy * s);
        }
        _sparks.RemoveAll(p => p.Age >= p.Life);
    }

    private void Pop(int lamp, (float X, float W) cell, float top, float height)
    {
        _pops.Add((lamp, 0));
        _chips.Add(new Chip
        {
            X = cell.X + cell.W * Range(0.45f, 0.7f),
            Y = top + height * 0.4f,
            Vx = Range(-18, 26),
            Vy = Range(-75, -50),
            Turn = Range(-0.3f, 0.3f),
            Spin = Range(-11, 11),
            W = Range(4.5f, 6),
            H = Range(2.6f, 3.4f),
        });
        for (int k = 0; k < Sparks; k++)
            _sparks.Add(new Spark
            {
                X = cell.X + cell.W * Range(0.2f, 0.9f),
                Y = top + Range(1, height - 1),
                Vx = Range(-70, 70),
                Vy = Range(-95, -15),
                Life = Range(0.22f, 0.5f),
            });
    }

    private float Range(float a, float b) => a + (float)random.NextDouble() * (b - a);
}
