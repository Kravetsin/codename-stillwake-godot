using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Energy;

/// <summary>A discharge from the bolt to the tab's frame: its jagged line, a fork when harsh, how bright and how long it has left.</summary>
public sealed class Discharge
{
    internal float Sx, Sy, Ex, Ey;
    internal double Span;

    /// <summary>The line's points in tab pixels, the bolt's end first; a fork off its third point, or null.</summary>
    public (float X, float Y)[] Points { get; internal set; } = [];
    public (float X, float Y)[]? Fork { get; internal set; }
    /// <summary>The grinder's: wider and forked. Its strength, faint while the belt runs low; what is left of it, 1 to 0.</summary>
    public bool Harsh { get; internal init; }
    public float Strength { get; internal init; }
    public double Life { get; internal set; } = 1;
    /// <summary>Where it strikes the frame.</summary>
    public (float X, float Y) End => (Ex, Ey);
}

/// <summary>
/// The energy tab's bolt while charge flows (energy-ring, as picked): it beats with the tool, ten beats a second a power unit of
/// draw, a current running down it; a beat jolts it a pixel off its place, and often throws a thin discharge onto the frame, the grinder's more often and
/// forked. When the belt goes flat it sputters out; a store put on kindles it from below. Plain C#, its randomness the client's.
/// </summary>
public sealed class BoltPulse
{
    /// <summary>Beats a second for each power unit a tick the tool draws: the drill's 1 beats ten times a second.</summary>
    public const double BeatsPerDraw = 10;
    /// <summary>How long a beat's light takes to fade, a sputter to go dark and a kindling to burn up.</summary>
    public const double BeatS = 0.07, SputterS = 0.75, KindleS = 1.2;
    /// <summary>The chance a beat throws a discharge: a draw of 1 and of more; halved while low.</summary>
    public const double ArcChance = 0.25, HarshArcChance = 0.45;
    /// <summary>How far a beat jolts the bolt off its place, in tab pixels at most: a draw of 1 and of more; it settles as the beat fades.</summary>
    public const float JoltPx = 0.7f, HarshJoltPx = 1.2f;

    private readonly Random _random;
    private readonly (float X, float Y, int Side)[] _from;
    private readonly float _height;
    private readonly List<Discharge> _arcs = [];
    private double _beats, _speed, _reshape;
    private int _draw, _units = -1;
    private bool _low;
    private (float X, float Y) _jolt;

    /// <param name="bolt">The bolt's place in the tab, its corner and size.</param>
    /// <param name="height">The tab's height: discharges strike its frame, left, top or bottom.</param>
    public BoltPulse((float X, float Y, float W, float H) bolt, float height, Random random)
    {
        _random = random;
        _height = height;
        (float, float, int) At(float x, float y, int side) => (bolt.X + x * bolt.W, bolt.Y + y * bolt.H, side);
        // Its left elbow, its two top corners and its lower tip, as the picture is drawn; the side each strikes toward.
        _from = [At(0.11f, 0.56f, Left), At(0.36f, 0.03f, Top), At(0.79f, 0.03f, Top), At(0.29f, 0.97f, Bottom), At(0.11f, 0.56f, Bottom)];
    }

    private const int Left = 0, Top = 1, Bottom = 2;

    /// <summary>A beat's light, 1 as it lands to 0.</summary>
    public double Beat { get; private set; }
    /// <summary>How far the current has run down the bolt, in pixels; how much it flows, 0 at rest to 1 drawing.</summary>
    public double Phase { get; private set; }
    public double Level { get; private set; }
    /// <summary>Seconds since the belt went flat while sputtering out, else null; seconds since a store kindled it, else null.</summary>
    public double? Sputter { get; private set; }
    public double? Kindle { get; private set; }
    /// <summary>Dark: nothing on the belt to draw.</summary>
    public bool Dead { get; private set; }
    /// <summary>The discharges on the frame now.</summary>
    public IReadOnlyList<Discharge> Arcs => _arcs;
    /// <summary>Where a beat threw the bolt, in tab pixels; scaled by <see cref="Beat"/>, it rattles while drawing and rests at none.</summary>
    public (float X, float Y) Jolt => ((float)(_jolt.X * Beat), (float)(_jolt.Y * Beat));
    /// <summary>What the discharges add to the bolt's halo.</summary>
    public double Flare { get; private set; }

    /// <summary>A read: the belt's charge in power units, what the tool running draws a tick (0 for none) and whether it runs low.</summary>
    public void Read(int units, int draw, bool low)
    {
        if (_units > 0 && units == 0) (Sputter, Kindle) = (0, null);
        if (_units == 0 && units > 0) (Kindle, Sputter, Dead) = (0, null, false);
        if (_units < 0) Dead = units == 0;
        (_units, _draw, _low) = (units, units > 0 ? draw : 0, low);
        if (_draw > 0) Dead = false;
    }

    /// <summary>Moves it on by <paramref name="seconds"/>: beats, the current, the discharges.</summary>
    public void Step(double seconds)
    {
        Beat = Math.Max(0, Beat - seconds / BeatS);
        _beats += seconds * BeatsPerDraw * _draw;
        for (; _beats >= 1; _beats--)
        {
            // Low, a beat now and then fails to land.
            if (_low && _random.NextDouble() < 0.3) continue;
            Beat = _draw > 1 ? 0.85 : 1;
            float jolt = _draw > 1 ? HarshJoltPx : JoltPx;
            _jolt = (Range(-jolt, jolt), Range(-jolt, jolt));
            double chance = (_draw > 1 ? HarshArcChance : ArcChance) * (_low ? 0.5 : 1);
            if (_random.NextDouble() < chance) Throw(_draw > 1, _low ? 0.6f : 1);
        }
        if (_draw == 0) _beats = 0;
        _speed += (14.0 * _draw - _speed) * Math.Min(1, seconds * 6);
        Phase += seconds * _speed;
        Level += ((_draw > 0 ? 1 : 0) - Level) * Math.Min(1, seconds * 6);
        if (Sputter is double s)
        {
            if (s == 0) Throw(false, 0.5f);
            Sputter = s + seconds >= SputterS ? null : s + seconds;
            if (Sputter == null) Dead = true;
        }
        if (Kindle is double k) Kindle = k + seconds >= KindleS ? null : k + seconds;
        StepArcs(seconds);
    }

    /// <summary>
    /// Whether the bolt is lit and how bright, 0 to 1: a beat brightens it while drawing; sputtering it blinks out.
    /// </summary>
    public (bool On, double Bright) Look()
    {
        bool on = !Dead;
        double bright = 0.78 + 0.22 * Beat * Level;
        if (Sputter is double s)
        {
            int blink = (int)(s * 16);
            on = blink % 3 != 1 && (s < 0.5 || blink % 2 == 0);
            bright = 1 - s;
        }
        return (on, Math.Clamp(bright, 0, 1));
    }

    private void Throw(bool harsh, float strength)
    {
        var (sx, sy, side) = _from[_random.Next(_from.Length)];
        float ex = side == Left ? -0.5f : Math.Clamp(sx + Range(-6, 6), 1, 26);
        float ey = side == Left ? Math.Clamp(sy + Range(-7, 7), 2, _height - 2) : side == Top ? -0.5f : _height + 0.5f;
        var arc = new Discharge { Sx = sx, Sy = sy, Ex = ex, Ey = ey, Harsh = harsh, Strength = strength, Span = harsh ? Range(0.09f, 0.17f) : Range(0.06f, 0.12f) };
        Shape(arc);
        _arcs.Add(arc);
    }

    // A new jag every few hundredths of a second: the line crackles while it lives.
    private void StepArcs(double seconds)
    {
        _reshape -= seconds;
        bool again = _reshape <= 0;
        if (again) _reshape = 0.035;
        double flare = 0;
        foreach (var arc in _arcs)
        {
            arc.Life -= seconds / arc.Span;
            if (again) Shape(arc);
        }
        _arcs.RemoveAll(a => a.Life <= 0);
        foreach (var arc in _arcs) flare = Math.Max(flare, arc.Life * arc.Strength * (arc.Harsh ? 1 : 0.6));
        Flare = 0.22 * flare;
    }

    private void Shape(Discharge a)
    {
        int n = 4 + _random.Next(3);
        float dx = a.Ex - a.Sx, dy = a.Ey - a.Sy, len = MathF.Max(1, MathF.Sqrt(dx * dx + dy * dy));
        float nx = -dy / len, ny = dx / len, amp = a.Harsh ? 2.2f : 1.4f;
        var points = new (float, float)[n + 1];
        points[0] = (a.Sx, a.Sy);
        for (int i = 1; i < n; i++)
        {
            float t = (float)i / n, off = Range(-amp, amp);
            points[i] = (a.Sx + dx * t + nx * off, a.Sy + dy * t + ny * off);
        }
        points[n] = (a.Ex, a.Ey);
        a.Points = points;
        a.Fork = a.Harsh && _random.NextDouble() < 0.6 ? [points[2], (points[2].Item1 + Range(-4, 4), points[2].Item2 + Range(-4, 4))] : null;
    }

    private float Range(float a, float b) => a + (float)_random.NextDouble() * (b - a);
}
