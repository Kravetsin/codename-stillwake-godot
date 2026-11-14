using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// How the arc spear's blade moves (spear-effects): how hard, 0 to 1 (<see cref="Strength"/>), brighter the faster and the more
/// level it moves (raising and lowering the arm sweep it fainter), and which way (<see cref="Heading"/>): what tears pieces of
/// arc off it. Motes shed off it every <see cref="MoteEveryMs"/>, drifting up and dying. Standing, nothing; a jump of the blade
/// (the hero turning, landing hard, set down elsewhere) is no motion, so it never throws a burst.
/// </summary>
public sealed class SpearWake(Func<double>? random = null)
{
    /// <summary>How far back the motion is read; how often a moving blade sheds a mote and how long one lives, in ms.</summary>
    public const double LifeMs = 160, MoteEveryMs = 70, MoteMs = 360;
    /// <summary>The speeds pieces start tearing off and tear off fully at, and a mote's, pixels of the world a second.</summary>
    public const double TearFrom = 20, TearFull = 90, MoteFrom = 7.5, MoteFull = 32.5;
    /// <summary>How far the point may move in a frame before it counts as a jump, pixels of the world.</summary>
    public const double JumpPx = 10;

    /// <summary>The spear's point in its picture: the motion read.</summary>
    public static readonly (double X, double Y) Point = (63.1, 3.9);

    /// <summary>A mote shed: where it is in the world, how fast it rises, how fast the blade moved, its age.</summary>
    public sealed class Mote
    {
        public double X, Y, Rise, Speed, AgeMs;

        /// <summary>Its brightness now, 0 to 1.</summary>
        public double Alpha => 0.35 * Speed * (1 - AgeMs / MoteMs);
    }

    private readonly Func<double> _random = random ?? Random.Shared.NextDouble;
    private readonly List<(double Ms, (double X, double Y) At)> _seen = [];
    private readonly List<Mote> _motes = [];
    private double _clockMs, _dueMs;

    /// <summary>How hard the blade is moving now, 0 to 1.</summary>
    public double Strength { get; private set; }

    /// <summary>The way the point moves, a unit vector in the world; along +x while it is still.</summary>
    public (double X, double Y) Heading { get; private set; } = (1, 0);

    /// <summary>The motes alive.</summary>
    public IReadOnlyList<Mote> Motes => _motes;

    /// <summary>
    /// <paramref name="elapsedMs"/> on, the spear drawn where <paramref name="held"/> takes its picture's points into the world;
    /// none while it is not shown, so it is still at once and the motes die out.
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
            (Strength, _dueMs) = (0, 0);
            _seen.Clear();
            return;
        }
        var now = held(Point);
        if (_seen.Count > 0 && Distance(now, _seen[0].At) > JumpPx) _seen.Clear();
        _seen.Insert(0, (_clockMs, now));
        while (_seen.Count > 2 && _clockMs - _seen[^2].Ms > LifeMs) _seen.RemoveAt(_seen.Count - 1);
        var (speed, level) = Motion();
        Strength = Ramp(speed, TearFrom, TearFull) * (0.35 + 0.65 * level);
        // The motes shed off a moving blade only.
        double shed = Ramp(speed, MoteFrom, MoteFull);
        for (_dueMs += elapsedMs; _dueMs >= MoteEveryMs; _dueMs -= MoteEveryMs)
        {
            if (shed <= 0) continue;
            var at = held((54 + _random() * 9, 4 + _random() * 2.5));
            _motes.Add(new Mote { X = at.X + (_random() - 0.5) * 1.5, Y = at.Y, Rise = 3 + _random() * 4, Speed = shed });
        }
    }

    /// <summary>How fast the point has moved over the last 30 ms or so, pixels of the world a second, and how level, 0 to 1; its heading kept.</summary>
    private (double Speed, double Level) Motion()
    {
        for (int k = 1; k < _seen.Count; k++)
        {
            double dt = _seen[0].Ms - _seen[k].Ms;
            if (dt < 30 && k < _seen.Count - 1) continue;
            var (a, b) = (_seen[0].At, _seen[k].At);
            double d = Distance(a, b);
            if (dt <= 0 || d <= 0) return (0, 0);
            Heading = ((a.X - b.X) / d, (a.Y - b.Y) / d);
            return (d / dt * 1000, Heading.X * Heading.X);
        }
        return (0, 0);
    }

    private static double Distance((double X, double Y) a, (double X, double Y) b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

    private static double Ramp(double v, double from, double full) => Math.Clamp((v - from) / (full - from), 0, 1);
}
