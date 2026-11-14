using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The arc spear's wake (spear-effects): lightning thrown back from three points of the moving blade to where they were a moment
/// ago (<see cref="Behind"/>), brighter the faster and the more level it moves (raising and lowering the arm sweep it fainter),
/// and motes shed off it every <see cref="MoteEveryMs"/>, drifting up and dying. Standing, nothing; a jump of the blade (the hero
/// turning, landing hard, set down elsewhere) lets the wake go, so it never spans the jump.
/// </summary>
public sealed class SpearWake(Func<double>? random = null)
{
    /// <summary>How far back the wake reaches; how often a moving blade sheds a mote and how long one lives, in ms.</summary>
    public const double LifeMs = 160, MoteEveryMs = 70, MoteMs = 360;
    /// <summary>The speeds lightning starts and is full at, and a mote's, pixels of the world a second.</summary>
    public const double BoltFrom = 20, BoltFull = 90, MoteFrom = 7.5, MoteFull = 32.5;
    /// <summary>How far a point may move in a frame before it counts as a jump, and the longest wake bolt, pixels of the world.</summary>
    public const double JumpPx = 10, LongestPx = 18;

    /// <summary>The points of the spear's picture the wake follows, the point first, and how bright each one's is.</summary>
    public static readonly ((double X, double Y) At, double Weight)[] Points = [((63.1, 3.9), 1), ((56, 6.3), 0.8), ((53, 3.6), 0.6)];

    /// <summary>A mote shed: where it is in the world, how fast it rises, how fast the blade moved, its age.</summary>
    public sealed class Mote
    {
        public double X, Y, Rise, Speed, AgeMs;

        /// <summary>Its brightness now, 0 to 1.</summary>
        public double Alpha => 0.35 * Speed * (1 - AgeMs / MoteMs);
    }

    private readonly Func<double> _random = random ?? Random.Shared.NextDouble;
    private readonly List<(double Ms, (double X, double Y)[] At)> _seen = [];
    private readonly List<Mote> _motes = [];
    private double _clockMs, _dueMs, _strength;

    /// <summary>How hard the blade is moving now, 0 to 1: how bright its lightning is, before each point's weight.</summary>
    public double Strength => _strength;

    /// <summary>The motes alive.</summary>
    public IReadOnlyList<Mote> Motes => _motes;

    /// <summary>
    /// Where point <paramref name="i"/> of <see cref="Points"/> was <paramref name="ms"/> ago, in the world; none past what is
    /// seen since the last jump, or no farther from where it is than a spark, or farther than <see cref="LongestPx"/>.
    /// </summary>
    public (double X, double Y)? Behind(int i, double ms)
    {
        if (_seen.Count < 2) return null;
        double at = _clockMs - ms;
        for (int k = 1; k < _seen.Count; k++)
        {
            if (_seen[k].Ms > at) continue;
            var (newer, older) = (_seen[k - 1], _seen[k]);
            double t = newer.Ms > older.Ms ? (at - older.Ms) / (newer.Ms - older.Ms) : 0;
            var (a, b) = (older.At[i], newer.At[i]);
            (double X, double Y) p = (a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
            var now = _seen[0].At[i];
            double d = Math.Sqrt((p.X - now.X) * (p.X - now.X) + (p.Y - now.Y) * (p.Y - now.Y));
            return d < 1.5 || d > LongestPx ? null : p;
        }
        return null;
    }

    /// <summary>
    /// <paramref name="elapsedMs"/> on, the spear drawn where <paramref name="held"/> takes its picture's points into the world;
    /// none while it is not shown, so the wake lets go at once and the motes die out.
    /// </summary>
    public void Step(double elapsedMs, Func<(double X, double Y), (double X, double Y)>? held)
    {
        _clockMs += elapsedMs;
        for (int i = _motes.Count - 1; i >= 0; i--)
        {
            var m = _motes[i];
            m.AgeMs += elapsedMs;
            m.Y -= m.Rise * elapsedMs / 1000;
            if (m.AgeMs >= MoteMs) _motes.RemoveAt(i);
        }
        if (held == null)
        {
            (_strength, _dueMs) = (0, 0);
            _seen.Clear();
            return;
        }
        var now = new (double X, double Y)[Points.Length];
        for (int i = 0; i < Points.Length; i++) now[i] = held(Points[i].At);
        if (_seen.Count > 0 && Distance(now[0], _seen[0].At[0]) > JumpPx) _seen.Clear();
        _seen.Insert(0, (_clockMs, now));
        while (_seen.Count > 2 && _clockMs - _seen[^2].Ms > LifeMs) _seen.RemoveAt(_seen.Count - 1);
        var (speed, level) = Motion();
        _strength = Ramp(speed, BoltFrom, BoltFull) * (0.35 + 0.65 * level);
        // The motes shed off a moving blade only.
        double shed = Ramp(speed, MoteFrom, MoteFull);
        for (_dueMs += elapsedMs; _dueMs >= MoteEveryMs; _dueMs -= MoteEveryMs)
        {
            if (shed <= 0) continue;
            var at = held((54 + _random() * 9, 4 + _random() * 2.5));
            _motes.Add(new Mote { X = at.X + (_random() - 0.5) * 1.5, Y = at.Y, Rise = 3 + _random() * 4, Speed = shed });
        }
    }

    /// <summary>How fast the point has moved over the last 30 ms or so, pixels of the world a second, and how level, 0 to 1.</summary>
    private (double Speed, double Level) Motion()
    {
        for (int k = 1; k < _seen.Count; k++)
        {
            double dt = _seen[0].Ms - _seen[k].Ms;
            if (dt < 30 && k < _seen.Count - 1) continue;
            var (a, b) = (_seen[0].At[0], _seen[k].At[0]);
            double d = Distance(a, b);
            return dt <= 0 || d <= 0 ? (0, 0) : (d / dt * 1000, (a.X - b.X) * (a.X - b.X) / (d * d));
        }
        return (0, 0);
    }

    private static double Distance((double X, double Y) a, (double X, double Y) b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

    private static double Ramp(double v, double from, double full) => Math.Clamp((v - from) / (full - from), 0, 1);
}
