using System;
using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// A drill's own light (0054, 0075, 0094), one a figure: while it runs, the arc's crackle over every mount of it an arc module
/// is fitted at (<see cref="HeldParts.Crackles"/>, 0098), the drill's coil, as fine as a fire's embers
/// (slice 3): hair-thin arcs flicking between its turns, sparks crawling along it and now and then leaping a little way off,
/// micro-glints, a faint blue haze and light pulsing over it, all of it coming in bursts; a plain drill has none. What its
/// hits knock out of what they strike is that object's (<see cref="HitBurst"/>), thrown where it is struck, never at the bit.
/// </summary>
public sealed class DrillLight(Glims glims, Flares flares, Bolts bolts)
{
    private readonly Glims _glims = glims;
    private readonly Flares _flares = flares;
    private readonly Bolts _bolts = bolts;

    /// <summary>
    /// Over a running drill's arc-fitted mount a second at full speed, on average, times <see cref="Fx.Crackle"/>: fine arcs between
    /// its turns, sparks set crawling along it, sparks leaping off it, micro-glints.
    /// </summary>
    private const double ArcsPerSecond = 33, CrawlsPerSecond = 10, LeapsPerSecond = 7, GlintsPerSecond = 3;
    /// <summary>How often the coil's haze pulses, a second; how fast its bursts come and go, a period in ms.</summary>
    private const double PulseHz = 2.6, BurstMs = 260;
    private static readonly (int, int, int) Spark = (205, 236, 255), Haze = (90, 170, 255);

    /// <summary>A spark crawling along the coil, in pixels of the drill's picture: where it set off, which way, how fast, along which row.</summary>
    private sealed class Crawl
    {
        public double Ms, LifeMs, X, Way, Speed, Y, Phase;
    }

    /// <summary>One crackling box's own: its crawling sparks, what is due of each kind, its held light.</summary>
    private sealed class CoilState
    {
        public readonly List<Crawl> Crawling = [];
        public readonly object Light = new();
        public double Arcs, Crawls, Leaps, Glints;
    }

    private readonly Random _random = new();
    private readonly List<CoilState> _coils = [];
    private readonly (double X, double Y)[] _trail = new (double, double)[4];
    private double _clockMs;

    private double Between(double from, double to) => from + (to - from) * _random.NextDouble();

    /// <summary>
    /// The drill <paramref name="elapsedMs"/> on: <paramref name="parts"/> held, its modules in it, none without a drill, spinning
    /// at <paramref name="spin"/> (0 to 1). <paramref name="held"/> takes a point of its picture to the world, as drawn now.
    /// </summary>
    public void Step(HeldParts? parts, double spin, Func<(double X, double Y), Vector2> held, double elapsedMs)
    {
        var boxes = spin > 0.3 && parts != null ? parts.Crackles : [];
        while (_coils.Count < boxes.Count) _coils.Add(new CoilState());
        for (int i = 0; i < _coils.Count; i++)
            if (i >= boxes.Count) _coils[i].Crawling.Clear();
        if (boxes.Count > 0) _clockMs += elapsedMs;
        for (int i = 0; i < boxes.Count; i++) Coil(_coils[i], boxes[i], spin, held, elapsedMs);
    }

    /// <summary>The box <paramref name="coil"/> of a drill's picture, crackling: its arcs, crawling and leaping sparks, glints and haze.</summary>
    private void Coil(CoilState at, (double X0, double Y0, double X1, double Y1) coil, double spin, Func<(double X, double Y), Vector2> held, double elapsedMs)
    {
        // Electricity comes in bursts: every rate swells and ebbs together on slow noise, from a third of itself to twice.
        double burst = 0.3 + 1.7 * LuminousMath.Noise(_clockMs / BurstMs, 3.7);
        double s = elapsedMs / 1000 * spin * Fx.Crackle * burst;
        for (at.Arcs += ArcsPerSecond * s; at.Arcs >= 1; at.Arcs--)
        {
            double x = Between(coil.X0, coil.X1 - 2), y, y1, bulge, bright;
            if (_random.NextDouble() < 0.8)
                // Arched over the stock's top, a pixel above the coil, from one turn to another and out into the dark.
                (y, y1, bulge, bright) = (coil.Y0 - Between(0.8, 1.2), coil.Y0 - Between(0.8, 1.2), -Between(0.2, 0.45), Between(0.6, 1));
            else
                // Or flicked faintly between two turns across its face, which is bright already.
                (y, y1, bulge, bright) = (Between(coil.Y0 + 0.3, coil.Y1 - 0.3), Between(coil.Y0 + 0.3, coil.Y1 - 0.3), Between(-0.35, 0.35), Between(0.3, 0.55));
            (double, double) from = (x, y), to = (x + Between(1.4, 3), y1);
            _bolts.Add(Ends(held, from, to, bulge), _random.Next(2, 5), bright, width: 0.3, fine: true);
        }
        for (at.Crawls += CrawlsPerSecond * s; at.Crawls >= 1; at.Crawls--)
            at.Crawling.Add(new Crawl
            {
                LifeMs = Between(140, 280),
                X = Between(coil.X0, coil.X1),
                Way = _random.NextDouble() < 0.5 ? -1 : 1,
                Speed = Between(28, 55),
                // Along the stock's top, where they stand out against the dark: on the coil's bright face they would wash out.
                Y = coil.Y0 - Between(0.7, 1.2),
                Phase = Between(0, Math.Tau),
            });
        for (int i = at.Crawling.Count - 1; i >= 0; i--)
        {
            var c = at.Crawling[i];
            c.Ms += elapsedMs;
            double u = c.X + c.Way * c.Speed * c.Ms / 1000;
            if (c.Ms < c.LifeMs && u >= coil.X0 && u <= coil.X1)
            {
                Crawled(c, held);
                continue;
            }
            // Off the end of its run: now and then it leaps off where it got to.
            if (_random.NextDouble() < 0.35) Leap(coil, Math.Clamp(u, coil.X0, coil.X1), held);
            at.Crawling.RemoveAt(i);
        }
        for (at.Leaps += LeapsPerSecond * s; at.Leaps >= 1; at.Leaps--) Leap(coil, Between(coil.X0, coil.X1), held);
        for (at.Glints += GlintsPerSecond * s; at.Glints >= 1; at.Glints--)
        {
            var glint = held((Between(coil.X0, coil.X1), Between(coil.Y0, coil.Y1)));
            _flares.Glint(glint.X, glint.Y, Between(1.6, 2.6), Spark, Between(-0.5, 0.5));
        }
        // The haze and its light pulse, a little brighter in a burst.
        double pulse = 0.5 + 0.5 * Math.Sin(_clockMs / 1000 * Math.Tau * PulseHz), lit = spin * Fx.Glow * (0.75 + 0.25 * Math.Min(1, burst));
        var middle = held(((coil.X0 + coil.X1) / 2, (coil.Y0 + coil.Y1) / 2));
        _flares.Bloom(middle.X, middle.Y, 16, Haze, (0.14 + 0.16 * pulse) * lit * Between(0.85, 1));
        _flares.Hold(at.Light, middle.X, middle.Y, Haze, (0.06 + 0.05 * pulse) * lit, 4, 0.671f, 0.7);
    }

    /// <summary>A crawling spark this frame: a short stroke of light along the coil behind it, wavering across its row, flickering.</summary>
    private void Crawled(Crawl c, Func<(double X, double Y), Vector2> held)
    {
        for (int k = 0; k < _trail.Length; k++)
        {
            double ms = Math.Max(0, c.Ms - k * 9);
            var p = held((c.X + c.Way * c.Speed * ms / 1000, c.Y + 0.5 * Math.Sin(c.Phase + ms * 0.05)));
            _trail[k] = (p.X, p.Y);
        }
        double fade = Math.Sqrt(1 - c.Ms / c.LifeMs);
        _flares.Trail(_trail, Spark, 0.85, fade * Between(0.6, 1) * Fx.Glow, 0.3);
    }

    /// <summary>One or two sparks leaping off the coil's top or bottom at <paramref name="x"/> of its picture, out and a little aside.</summary>
    private void Leap((double X0, double Y0, double X1, double Y1) coil, double x, Func<(double X, double Y), Vector2> held)
    {
        bool top = _random.NextDouble() < 0.6;
        double edge = top ? coil.Y0 - 1 : coil.Y1;
        var from = held((x, edge));
        var outward = (held((x, edge + (top ? -1 : 1))) - from).Normalized();
        for (int n = _random.Next(1, 3); n > 0; n--)
        {
            double a = Math.Atan2(outward.Y, outward.X) + Between(-0.8, 0.8), v = Between(30, 65);
            _glims.Add(Luminous.ArcSpark, from.X, from.Y, Math.Cos(a) * v, Math.Sin(a) * v);
        }
    }

    /// <summary>An arc's ends at two points of the drill's picture, wherever it is drawn when asked.</summary>
    private static BoltEnds Ends(Func<(double X, double Y), Vector2> held, (double X, double Y) from, (double X, double Y) to, double bulge) => () =>
    {
        var (a, b) = (held(from), held(to));
        return (a.X, a.Y, b.X, b.Y, bulge);
    };
}
