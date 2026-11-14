using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The arc spear's wake, the user's pick «След за клинком» (spear-effects): a faint violet ghost of where three points of its blade
/// went over the last <see cref="LifeMs"/>, brighter the faster and the more level it moved (raising and lowering the arm sweep
/// it fainter), and motes shed off the moving blade every <see cref="MoteEveryMs"/>, drifting up and dying. Standing, nothing.
/// </summary>
public sealed class SpearWake(Func<double>? random = null)
{
    /// <summary>How long the ghost reaches back; how often a moving blade sheds a mote and how long one lives, in ms.</summary>
    public const double LifeMs = 280, MoteEveryMs = 70, MoteMs = 360;
    /// <summary>The speeds a ghost starts and is full at, and a mote's, pixels of the world a second.</summary>
    public const double GhostFrom = 8.6, GhostFull = 44, MoteFrom = 7.5, MoteFull = 32.5;

    /// <summary>The points of the spear's picture the ghost follows, the point first, and how bright each one's is.</summary>
    public static readonly ((double X, double Y) At, double Weight)[] Points = [((63.1, 3.9), 1), ((56, 6.3), 0.7), ((53, 3.6), 0.5)];

    /// <summary>A mote shed: where it is in the world, how fast it rises, how fast the blade moved, its age.</summary>
    public sealed class Mote
    {
        public double X, Y, Rise, Speed, AgeMs;

        /// <summary>Its brightness now, 0 to 1.</summary>
        public double Alpha => 0.35 * Speed * (1 - AgeMs / MoteMs);
    }

    private readonly Func<double> _random = random ?? Random.Shared.NextDouble;
    private readonly List<(double Ms, (double X, double Y)[] At)> _seen = [];
    private readonly List<(double X, double Y)>[] _trails = [[], [], []];
    private readonly double[] _strength = new double[Points.Length];
    private readonly List<Mote> _motes = [];
    private double _clockMs, _dueMs;

    /// <summary>The ghost behind point <paramref name="i"/> of <see cref="Points"/>, newest first, in the world.</summary>
    public IReadOnlyList<(double X, double Y)> Trail(int i) => _trails[i];

    /// <summary>How bright the ghost behind point <paramref name="i"/> is now, 0 to 1, its weight in.</summary>
    public double Strength(int i) => _strength[i];

    /// <summary>The motes alive.</summary>
    public IReadOnlyList<Mote> Motes => _motes;

    /// <summary>
    /// <paramref name="elapsedMs"/> on, the spear drawn where <paramref name="held"/> takes its picture's points into the world;
    /// none while it is not shown, so the ghost lets go at once and the motes die out.
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
            _seen.Clear();
            for (int i = 0; i < Points.Length; i++) (_strength[i], _dueMs) = (0, 0);
            foreach (var t in _trails) t.Clear();
            return;
        }
        var now = new (double X, double Y)[Points.Length];
        for (int i = 0; i < Points.Length; i++) now[i] = held(Points[i].At);
        _seen.Insert(0, (_clockMs, now));
        while (_seen.Count > 2 && _clockMs - _seen[^2].Ms > LifeMs) _seen.RemoveAt(_seen.Count - 1);
        for (int i = 0; i < Points.Length; i++)
        {
            _trails[i].Clear();
            double strongest = 0;
            for (int k = 0; k < _seen.Count; k++)
            {
                _trails[i].Add(_seen[k].At[i]);
                if (k == 0) continue;
                double age = _clockMs - _seen[k - 1].Ms, dt = _seen[k - 1].Ms - _seen[k].Ms;
                if (dt <= 0 || age > LifeMs) continue;
                var (a, b) = (_seen[k - 1].At[i], _seen[k].At[i]);
                double dx = a.X - b.X, dy = a.Y - b.Y, moved = Math.Sqrt(dx * dx + dy * dy);
                if (moved <= 0) continue;
                double level = dx * dx / (moved * moved), fast = Ramp(moved / dt * 1000, GhostFrom, GhostFull);
                strongest = Math.Max(strongest, level * fast * Math.Pow(1 - age / LifeMs, 1.5));
            }
            _strength[i] = Points[i].Weight * strongest;
        }
        // The blade's speed over the last few frames sheds the motes: none off a blade at rest.
        double speed = Ramp(SpeedOfPoint(), MoteFrom, MoteFull);
        for (_dueMs += elapsedMs; _dueMs >= MoteEveryMs; _dueMs -= MoteEveryMs)
        {
            if (speed <= 0) continue;
            var at = held((54 + _random() * 9, 4 + _random() * 2.5));
            _motes.Add(new Mote { X = at.X + (_random() - 0.5) * 1.5, Y = at.Y, Rise = 3 + _random() * 4, Speed = speed });
        }
    }

    /// <summary>How fast the point has moved over the last 20 ms or so, pixels of the world a second.</summary>
    private double SpeedOfPoint()
    {
        for (int k = 1; k < _seen.Count; k++)
        {
            double dt = _seen[0].Ms - _seen[k].Ms;
            if (dt < 20 && k < _seen.Count - 1) continue;
            if (dt <= 0) return 0;
            var (a, b) = (_seen[0].At[0], _seen[k].At[0]);
            return Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y)) / dt * 1000;
        }
        return 0;
    }

    private static double Ramp(double v, double from, double full) => Math.Clamp((v - from) / (full - from), 0, 1);
}
