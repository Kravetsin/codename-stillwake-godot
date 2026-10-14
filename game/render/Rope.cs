using System;
using System.Collections.Generic;

namespace Stillwake.Game.Render;

/// <summary>
/// How a rope moves, in its caller's units and in steps of 1/60 of a second: <see cref="Links"/>
/// between its points, more bending more smoothly; what each point <see cref="Keep"/>s of its speed a
/// step; what <see cref="Gravity"/> adds to each point's downward speed a step, 0 out in space; how hard
/// (<see cref="Drift"/>) and how slowly (<see cref="DriftSpeed"/>) each point is nudged about, so a
/// loose rope never rests; and how far apart in phase neighbouring points drift, the length of the
/// <see cref="Wave"/>s along it.
/// </summary>
public sealed record RopeStyle(int Links, double Keep, double Gravity, double Drift, double DriftSpeed, double Wave);

/// <summary>
/// A rope between two ends (apps/game/src/render/rope.ts): points carried on by their speed, pulled by
/// gravity and nudged by a slow drift, its links held to their length and its ends pinned. It starts
/// straight from one end to the other. Drawn only, never simulated: every peer moves its own.
/// Bodies brushing it along come with cables (port G-08), its second caller.
/// </summary>
public sealed class Rope
{
    /// <summary>A step: the rope moves at 60 steps a second, whatever the frame rate.</summary>
    public const double StepMs = 1000.0 / 60;
    /// <summary>Passes over the links a step: more keep the rope's length truer.</summary>
    private const int Passes = 12;
    /// <summary>Steps a frame may take at most, so a long stall does not run the rope on for seconds.</summary>
    private const int MostSteps = 4;

    /// <summary>A point of a rope, and where it was a step before: its speed, as a Verlet rope keeps it.</summary>
    public sealed class Knot
    {
        public double X, Y, Px, Py;
    }

    private readonly RopeStyle _style;
    private readonly Knot[] _knots;
    private readonly double[] _phases;
    private double _time, _owed;

    public Rope(RopeStyle style, (double X, double Y) from, (double X, double Y) to, int seed = 0)
    {
        _style = style;
        _knots = new Knot[style.Links + 1];
        _phases = new double[style.Links + 1];
        for (int i = 0; i <= style.Links; i++)
        {
            double t = (double)i / style.Links;
            double x = from.X + (to.X - from.X) * t, y = from.Y + (to.Y - from.Y) * t;
            _knots[i] = new Knot { X = x, Y = y, Px = x, Py = y };
            // Neighbours drift nearly alike, a wave along the rope: it bends in long curves, never kinks.
            _phases[i] = seed * 1.7 + i * style.Wave;
        }
    }

    public IReadOnlyList<Knot> Points => _knots;

    /// <summary>
    /// Moves the rope on by <paramref name="elapsedMs"/>, its ends at <paramref name="a"/> and
    /// <paramref name="b"/>, as long as <paramref name="lengthFor"/> says each step given the way its
    /// ends span.
    /// </summary>
    public void Update((double X, double Y) a, (double X, double Y) b, double elapsedMs, Func<double, double> lengthFor)
    {
        _owed += elapsedMs;
        int steps = 0;
        while (_owed >= StepMs && steps < MostSteps)
        {
            double span = Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
            Step(a, b, Math.Max(lengthFor(span), 0));
            _owed -= StepMs;
            steps += 1;
        }
        if (steps == MostSteps) _owed = 0;
        Pin(a, b);
    }

    private void Pin((double X, double Y) a, (double X, double Y) b)
    {
        var first = _knots[0];
        var last = _knots[^1];
        first.X = first.Px = a.X;
        first.Y = first.Py = a.Y;
        last.X = last.Px = b.X;
        last.Y = last.Py = b.Y;
    }

    private void Step((double X, double Y) a, (double X, double Y) b, double length)
    {
        int links = _style.Links;
        _time += 1;
        for (int i = 1; i < links; i++)
        {
            var k = _knots[i];
            double angle = _time * _style.DriftSpeed + _phases[i];
            double vx = (k.X - k.Px) * _style.Keep + Math.Cos(angle) * _style.Drift;
            double vy = (k.Y - k.Py) * _style.Keep + Math.Sin(angle * 1.3) * _style.Drift + _style.Gravity;
            k.Px = k.X;
            k.Py = k.Y;
            k.X += vx;
            k.Y += vy;
        }
        double link = length / links;
        for (int pass = 0; pass < Passes; pass++)
        {
            Pin(a, b);
            for (int i = 0; i < links; i++)
            {
                var p = _knots[i];
                var q = _knots[i + 1];
                double dx = q.X - p.X, dy = q.Y - p.Y;
                double d = Math.Sqrt(dx * dx + dy * dy);
                // A rope only pulls: links shorter than their length are left to fold.
                if (d <= link || d == 0) continue;
                double pull = (d - link) / d;
                // A pinned end stays; the other end of its link takes the whole pull.
                double atP = i == 0 ? 0 : i + 1 == links ? 1 : 0.5;
                double atQ = i + 1 == links ? 0 : i == 0 ? 1 : 0.5;
                p.X += dx * pull * atP;
                p.Y += dy * pull * atP;
                q.X -= dx * pull * atQ;
                q.Y -= dy * pull * atQ;
            }
        }
        Pin(a, b);
    }

    /// <summary>
    /// The rope as a smooth line, <paramref name="scale"/> turning its unit into pixels: through the
    /// middle of each link and bending at the points, so no corners show where links meet.
    /// </summary>
    public List<(double X, double Y)> Trace(double scale, int perLink = 4)
    {
        var line = new List<(double X, double Y)> { (_knots[0].X * scale, _knots[0].Y * scale) };
        var from = line[0];
        for (int i = 1; i < _knots.Length - 1; i++)
        {
            var p = (X: _knots[i].X * scale, Y: _knots[i].Y * scale);
            var to = ((_knots[i].X + _knots[i + 1].X) / 2 * scale, (_knots[i].Y + _knots[i + 1].Y) / 2 * scale);
            // A quadratic curve from the last link's middle, bent toward this point, to the next middle.
            for (int s = 1; s <= perLink; s++)
            {
                double t = (double)s / perLink, u = 1 - t;
                line.Add((u * u * from.X + 2 * u * t * p.X + t * t * to.Item1, u * u * from.Y + 2 * u * t * p.Y + t * t * to.Item2));
            }
            from = to;
        }
        line.Add((_knots[^1].X * scale, _knots[^1].Y * scale));
        return line;
    }
}
