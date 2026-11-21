using System;
using I = Stillwake.Game.Shared.DroneInk;

namespace Stillwake.Game.Features.Scanner;

/// <summary>A gauge dot as drawn, world px: where, its heading while it flies, how hot and how opaque, where its thread runs from.</summary>
public readonly record struct GaugeDot(float X, float Y, float Dx, float Dy, float Heat, float Alpha, bool Flying, float FromX, float FromY);

/// <summary>An ember a hit burst off the row, world px: where, its side's share of a dot's, how hot.</summary>
public readonly record struct GaugeEmber(float X, float Y, float Side, float Heat);

/// <summary>
/// The scanner's gauge (drone-threads, the pick «Нити», gauge dots): a row of <see cref="Count"/> ember dots over the wreck read,
/// the lit ones its hit points left. The dots fly in from the spyglass's mouth and out to the drone's eye, and from where they are
/// whenever the pointer leaves, comes back or moves to another wreck; a hit shudders the row and bursts the dots it put out. World
/// px and seconds. Client only.
/// </summary>
public sealed class ScanDots
{
    /// <summary>How many dots the row holds.</summary>
    public const int Count = I.Dots;

    private const float W = I.WorldPx;

    private enum From { Mouth, Home, Fixed }

    private readonly From[] _from = new From[Count];
    private readonly (float X, float Y)[] _fixed = new (float X, float Y)[Count];
    private readonly float[] _alpha = new float[Count], _lost = new float[Count];
    private readonly double[] _burst = new double[Count];
    private Readout? _shown;
    private bool _open;
    private double _e = I.DotsOut, _hit = double.PositiveInfinity, _time;
    private (float X, float Y)? _mouth, _eye;

    public ScanDots() => Array.Fill(_burst, double.PositiveInfinity);

    /// <summary>
    /// How lit dot <paramref name="j"/> is for <paramref name="left"/> of <paramref name="max"/>, 0..1: each dot a
    /// <see cref="Count"/>-th of the maximum, the last lit one partly; none at no maximum.
    /// </summary>
    public static float Lit(int left, int max, int j) =>
        max <= 0 ? 0 : (float)Math.Clamp(Math.Clamp(left, 0, max) * (double)Count / max - j, 0, 1);

    /// <summary>The readout the row shows, the last one while it leaves; none before the first.</summary>
    public Readout? Shown => _shown;

    /// <summary>Whether any of it shows.</summary>
    public bool Visible => _shown != null && (_open || _e < I.DotsOut);

    /// <summary>
    /// Steps it <paramref name="seconds"/> on to <paramref name="now"/>, what the scanner reads (none: it leaves), the spyglass's
    /// <paramref name="mouth"/> and the drone's <paramref name="eye"/> where they are (none: the dots come and go in place).
    /// </summary>
    public void Step(Readout? now, (float X, float Y)? mouth, (float X, float Y)? eye, double seconds)
    {
        bool open = now != null, another = now is { } read && _shown is { } was && was.Id != read.Id;
        if (open != _open || another)
        {
            // Every dot leaves for its new goal from where it is; one sitting home leaves from home, wherever that goes.
            bool none = !Visible;
            for (int j = 0; j < Count; j++)
            {
                var dot = Flight(j);
                if (none) (_from[j], _alpha[j]) = (From.Mouth, 1);
                else
                {
                    _from[j] = dot.Home && !another ? From.Home : From.Fixed;
                    _fixed[j] = (dot.Dot.X, dot.Dot.Y);
                    _alpha[j] = dot.Dot.Alpha;
                }
            }
            if (another || !open) Calm();
            _open = open;
            _e = 0;
        }
        else if (now is { } same && _shown is { } before && same.Left < before.Left) Hit(before, same);
        if (now != null) _shown = now;
        (_mouth, _eye) = (mouth, eye);
        _e += seconds;
        _time += seconds;
        _hit += seconds;
        for (int j = 0; j < Count; j++) _burst[j] += seconds;
    }

    /// <summary>Dot <paramref name="j"/> as drawn now.</summary>
    public GaugeDot Dot(int j) => Flight(j).Dot;

    /// <summary>The ember dot <paramref name="j"/> burst off at the last hit, falling; none once it has gone out or the dot is away.</summary>
    public GaugeEmber? Ember(int j)
    {
        if (_burst[j] >= I.Hit || _shown is not { } read || !Flight(j).Home || !_open) return null;
        float hk = (float)(_burst[j] / I.Hit), b = (float)(Hash(j + 11) * Math.PI - Math.PI), v = I.BurstSpeed + I.BurstMore * (float)Hash(j + 12);
        var (x, y) = HomeOf(read, j);
        return new GaugeEmber(x + MathF.Cos(b) * v * hk * W, y + (MathF.Sin(b) * v * hk + I.BurstFall * hk * hk) * W, 1 - 0.5f * hk, _lost[j] * (I.BurstHeat - hk));
    }

    /// <summary>A hit from <paramref name="was"/> to <paramref name="now"/>: the row shudders, and each dot sitting home that lost light bursts.</summary>
    private void Hit(Readout was, Readout now)
    {
        _hit = 0;
        for (int j = 0; j < Count; j++)
        {
            float lost = Lit(was.Left, was.Max, j) - Lit(now.Left, now.Max, j);
            if (lost <= 0.01f || !Flight(j).Home) continue;
            // A dot hit again while its ember still falls adds to it, so none jumps back.
            if (_burst[j] < I.Hit) _lost[j] += lost;
            else (_burst[j], _lost[j]) = (0, lost);
        }
    }

    /// <summary>Drops the shudder and the falling embers.</summary>
    private void Calm()
    {
        _hit = double.PositiveInfinity;
        Array.Fill(_burst, double.PositiveInfinity);
    }

    /// <summary>Dot <paramref name="j"/> now, and whether it sits home.</summary>
    private (GaugeDot Dot, bool Home) Flight(int j)
    {
        if (_shown is not { } read) return (default, false);
        var home = HomeOf(read, j);
        float q, alpha;
        (float X, float Y) goal;
        if (_open)
        {
            q = Sm(Math.Clamp(((float)(_e / I.DotsIn) - (float)Hash(j + 5) * I.DotLag) / I.DotSpan, 0, 1));
            goal = home;
            alpha = _alpha[j] + (1 - _alpha[j]) * q;
        }
        else
        {
            q = Sm(Math.Clamp(((float)(_e / I.DotsOut) - (float)Hash(j + 2) * I.DotLagOut) / I.DotSpanOut, 0, 1));
            goal = _eye ?? home;
            alpha = _alpha[j] * (1 - q);
        }
        var start = _from[j] switch { From.Mouth => _mouth ?? home, From.Home => home, _ => _fixed[j] };
        bool there = _open ? q >= 1 : _from[j] == From.Home && q <= 0;
        float e = Sm(q), wx = goal.X - start.X, wy = goal.Y - start.Y, d = MathF.Sqrt(wx * wx + wy * wy);
        bool flying = !there && d > 1e-4f;
        var (x, y) = there ? home : (start.X + wx * e, start.Y + wy * e);
        return (new GaugeDot(x, y, flying ? wx / d : 0, flying ? wy / d : 0, Heat(read, j) * alpha, alpha, flying, start.X, start.Y), there);
    }

    /// <summary>How hot dot <paramref name="j"/> burns for <paramref name="read"/>: lit by its share, flickering; hotter as a hit lands.</summary>
    private float Heat(Readout read, int j)
    {
        float lit = Lit(read.Left, read.Max, j);
        if (lit <= 0) return I.DotDark;
        float flicker = I.DotFlicker + I.DotFlickerSwing * MathF.Sin((float)_time * I.DotFlickerPace + j * I.DotFlickerStep);
        float heat = (I.DotLit + I.DotLitMore * lit) * flicker;
        return _hit < I.Hit ? heat + I.HitHeat * (1 - (float)(_hit / I.Hit)) : heat;
    }

    /// <summary>Dot <paramref name="j"/>'s place over <paramref name="read"/>'s point, the row shuddering as a hit lands.</summary>
    private (float X, float Y) HomeOf(Readout read, int j)
    {
        float hk = (float)(_hit / I.Hit), shudder = hk < 1 ? MathF.Sin(hk * I.ShudderPace) * I.Shudder * (1 - hk) : 0;
        return ((float)(read.X * Camera.Cell) + ((j - (Count - 1) / 2f) * I.DotStep + shudder) * W, (float)(read.Y * Camera.Cell) - I.DotsAbove * W);
    }

    private static float Sm(float q) => q * q * (3 - 2 * q);

    /// <summary>The picks' hash: the fraction of a big sine.</summary>
    private static double Hash(double i)
    {
        double x = Math.Sin(i * 127.1 + 311.7) * 43758.5453;
        return x - Math.Floor(x);
    }
}
