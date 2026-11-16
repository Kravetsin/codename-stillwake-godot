using System;
using System.Collections.Generic;
using Stillwake.Game.Render;
using Pens = Stillwake.Game.Render.Ink.Draw;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The trail a held blade leaves by its own motion, whatever moves it (blade-trail): its edge sampled every frame where it is in
/// the world, from one end to the other along its shape (a rounded blade's bow among them), the stretch between two samples drawn
/// by how fast the edge went, left where it was swept and gone in <see cref="LifeMs"/>. It shows no reach, only where the blade
/// has been; what it is drawn as is the drawer's.
/// </summary>
public sealed class BladeTrail
{
    /// <summary>How long a stretch of the trail is seen, in ms.</summary>
    public const double LifeMs = 170;

    /// <summary>The edge's speed, in pixels a second, under which it leaves nothing, and at which its trail is whole.</summary>
    public const double Slow = 60, Fast = 280;

    /// <summary>A step longer than this, in pixels, is a jump, not a sweep: the trail breaks.</summary>
    public const double Jump = 30;

    /// <summary>
    /// The edge as it was at <c>At</c> (ms of the trail's clock), how much of the stretch up to it shows, and its place along the
    /// trail: one more each sample, between for those put between (<see cref="Smoothed"/>).
    /// </summary>
    public readonly record struct Sample(double At, (double X, double Y)[] Edge, double Shown, double Place);

    private readonly List<Sample> _samples = [];
    // The edges of samples gone, kept to be filled again: a swing would otherwise make one each frame.
    private readonly Stack<(double X, double Y)[]> _spare = [];
    private readonly double _lifeMs;
    private double _clock;
    private int _made;

    /// <summary>A trail whose stretches are seen <paramref name="lifeMs"/>: the haze's <see cref="LifeMs"/>, an effect skin's its own.</summary>
    public BladeTrail(double lifeMs = LifeMs) => _lifeMs = lifeMs;

    /// <summary>Its samples, oldest first, as long as they are seen: what an effect skin draws its swing from.</summary>
    public IReadOnlyList<Sample> Samples => _samples;

    /// <summary>How much of <paramref name="sample"/>'s life is left, 1 new to 0 gone.</summary>
    public double Left(Sample sample) => Pens.Clamp(1 - (_clock - sample.At) / _lifeMs);

    /// <summary>Whether anything is drawn now.</summary>
    public bool Live
    {
        get
        {
            for (int i = 1; i < _samples.Count; i++)
                if (_samples[i].Shown > 0) return true;
            return false;
        }
    }

    /// <summary>
    /// <paramref name="elapsedMs"/> on, the edge's points from its one end to the other in the world now, as many each frame;
    /// none while no blade is held.
    /// </summary>
    public void Step(double elapsedMs, IReadOnlyList<(double X, double Y)>? edge)
    {
        _clock += elapsedMs;
        int gone = 0;
        while (gone < _samples.Count && _clock - _samples[gone].At > _lifeMs) gone++;
        for (int i = 0; i < gone; i++) _spare.Push(_samples[i].Edge);
        if (gone > 0) _samples.RemoveRange(0, gone);
        if (edge is not { Count: >= 2 }) return;
        var points = _spare.TryPop(out var spare) && spare.Length == edge.Count ? spare : new (double X, double Y)[edge.Count];
        for (int i = 0; i < points.Length; i++) points[i] = edge[i];
        double shown = 0;
        if (_samples.Count > 0 && elapsedMs > 0 && _samples[^1].Edge.Length == points.Length)
        {
            var last = _samples[^1].Edge;
            double step = 0;
            for (int i = 0; i < points.Length; i++) step = Math.Max(step, Distance(last[i], points[i]));
            // How much of a trail this stretch leaves, by how fast the edge went; a jump leaves none.
            shown = step > Jump ? 0 : Pens.Clamp((step * 1000 / elapsedMs - Slow) / (Fast - Slow));
        }
        _samples.Add(new(_clock, points, shown, _made++));
    }

    /// <summary>
    /// All of it as it is now, into <paramref name="ink"/> in <paramref name="tone"/>, fainter as it ages, its points of the
    /// world turned into the drawer's own by <paramref name="local"/>.
    /// </summary>
    public void Draw(Ink ink, Func<(double X, double Y), (double X, double Y)> local, Ink.Tone tone)
    {
        var samples = Smoothed();
        // One sheet a run of stretches that show, from the sample before its first, each edge in its stretch's strength, blended
        // between: one smooth sweep, not a band a frame.
        for (int i = 1; i < samples.Count; i++)
        {
            if (samples[i].Shown <= 0) continue;
            int from = i - 1;
            while (i + 1 < samples.Count && samples[i + 1].Shown > 0) i++;
            _lines.Clear();
            _tones.Clear();
            for (int k = from; k <= i; k++)
            {
                double left = Left(samples[k]);
                if (k - from == _kept.Count) _kept.Add([]);
                var line = _kept[k - from];
                line.Clear();
                foreach (var p in samples[k].Edge) line.Add(local(p));
                _lines.Add(line);
                _tones.Add(tone.Faded((float)(samples[Math.Max(k, from + 1)].Shown * left * left)));
            }
            ink.Sheet(_lines, _tones);
        }
    }

    private readonly List<List<(double X, double Y)>> _kept = [];
    private readonly List<IReadOnlyList<(double X, double Y)>> _lines = [];
    private readonly List<Ink.Tone> _tones = [];

    /// <summary>
    /// Its samples with more put between each two of a stretch that shows, on a curve through them and their neighbours, no step
    /// over <paramref name="stepPx"/>: a swing as one smooth sweep, not a straight stretch a frame. Valid till the next call.
    /// </summary>
    public IReadOnlyList<Sample> Smoothed(double stepPx = 1.5)
    {
        _fine.Clear();
        int used = 0;
        for (int i = 0; i < _samples.Count; i++)
        {
            var now = _samples[i];
            if (i == 0 || now.Shown <= 0)
            {
                _fine.Add(now);
                continue;
            }
            var was = _samples[i - 1];
            // A neighbour bends the curve only across a stretch that shows: never across a jump.
            var before = i >= 2 && was.Shown > 0 ? _samples[i - 2].Edge : null;
            var after = i + 1 < _samples.Count && _samples[i + 1].Shown > 0 ? _samples[i + 1].Edge : null;
            double step = 0;
            for (int j = 0; j < now.Edge.Length; j++) step = Math.Max(step, Distance(was.Edge[j], now.Edge[j]));
            int n = Math.Clamp((int)Math.Ceiling(step / stepPx), 1, MostBetween);
            for (int k = 1; k < n; k++)
            {
                double t = (double)k / n;
                if (used == _fineEdges.Count) _fineEdges.Add(new (double X, double Y)[now.Edge.Length]);
                var edge = _fineEdges[used++];
                if (edge.Length != now.Edge.Length) edge = _fineEdges[used - 1] = new (double X, double Y)[now.Edge.Length];
                for (int j = 0; j < edge.Length; j++) edge[j] = Curve(before?[j], was.Edge[j], now.Edge[j], after?[j], t);
                _fine.Add(new(was.At + (now.At - was.At) * t, edge, now.Shown, was.Place + (now.Place - was.Place) * t));
            }
            _fine.Add(now);
        }
        return _fine;
    }

    /// <summary>The most samples put between two.</summary>
    private const int MostBetween = 16;

    private readonly List<Sample> _fine = [];
    private readonly List<(double X, double Y)[]> _fineEdges = [];

    // Between p1 and p2 at t, its way at each end from the neighbours either side, weighed by the stretches' lengths so an uneven
    // sweep does not overshoot; at an end with none, the other end's way mirrored across the stretch, so a turning sweep keeps turning.
    private static (double X, double Y) Curve((double X, double Y)? p0, (double X, double Y) p1, (double X, double Y) p2, (double X, double Y)? p3, double t)
    {
        double d12 = Distance(p1, p2);
        (double X, double Y) chord = (p2.X - p1.X, p2.Y - p1.Y);
        (double X, double Y)? m1 = p0 is { } a && Distance(a, p1) + d12 > 1e-9 ? Scale(p2.X - a.X, p2.Y - a.Y, d12 / (Distance(a, p1) + d12)) : null;
        (double X, double Y)? m2 = p3 is { } b && Distance(p2, b) + d12 > 1e-9 ? Scale(b.X - p1.X, b.Y - p1.Y, d12 / (d12 + Distance(p2, b))) : null;
        var (w1, w2) = (m1 ?? Mirrored(m2, chord) ?? chord, m2 ?? Mirrored(m1, chord) ?? chord);
        double t2 = t * t, t3 = t2 * t;
        double h00 = 2 * t3 - 3 * t2 + 1, h10 = t3 - 2 * t2 + t, h01 = -2 * t3 + 3 * t2, h11 = t3 - t2;
        return (h00 * p1.X + h10 * w1.X + h01 * p2.X + h11 * w2.X, h00 * p1.Y + h10 * w1.Y + h01 * p2.Y + h11 * w2.Y);
    }

    private static (double X, double Y)? Mirrored((double X, double Y)? m, (double X, double Y) chord)
    {
        if (m is not { } v) return null;
        double l2 = chord.X * chord.X + chord.Y * chord.Y;
        if (l2 < 1e-12) return v;
        double k = 2 * (v.X * chord.X + v.Y * chord.Y) / l2;
        return (k * chord.X - v.X, k * chord.Y - v.Y);
    }

    private static (double X, double Y) Scale(double x, double y, double k) => (x * k, y * k);

    private static double Distance((double X, double Y) a, (double X, double Y) b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
}
