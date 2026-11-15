using System;
using System.Collections.Generic;

namespace Stillwake.Game.Render;

/// <summary>
/// The arc module's charge on a struck blade, the user's pick «Срыв разряда» (spear-effects): at rest the blade's area only
/// fizzes, tiny sparks over it; moving, it builds a charge by how far it goes, and past an uneven threshold the charge lets go
/// in a burst of bolts torn off its trailing edge over a few frames, mostly short, now and then a long forked one, left in the
/// world where they tore. It lives in one <see cref="EffectArea"/>, never at points, stepped at 60 Hz as the page was.
/// </summary>
public sealed class ArcBurst(EffectArea area, Func<double>? random = null)
{
    /// <summary>Fizz sparks a second at rest and what a full charge adds; their length, world px, and life, ms.</summary>
    public const double FizzPerSecond = 20, FizzCharged = 40;
    public static readonly (double From, double To) SparkPx = (0.35, 1.25), SparkMs = (35, 90), SparkAlpha = (0.4, 0.7);
    /// <summary>World px the blade travels for a whole unit of charge; below <see cref="StillPx"/> a second it leaks <see cref="Leak"/> a frame.</summary>
    public const double PxPerCharge = 24, StillPx = 4, Leak = 0.985;
    /// <summary>The thresholds a charge lets go at, drawn anew each time; the first.</summary>
    public static readonly (double From, double To) Threshold = (0.45, 1.55);
    public const double FirstThreshold = 0.8;
    /// <summary>A torn bolt's length in world px, a power law cut to this range with this tail; forked from <see cref="ForkPx"/>; its life, ms.</summary>
    public static readonly (double From, double To) BoltPx = (1, 12), BoltMs = (260, 640);
    public const double Tail = 1.2, ForkPx = 5.5;
    /// <summary>How far the blade's middle may move in a frame before it counts as a jump, not a motion, world px.</summary>
    public const double JumpPx = 10;

    private const double FrameMs = 1000.0 / 60;

    /// <summary>A spark of fizz: where on the area, in pixels of the picture, its turn there, length (world px), life (ms), brightness.</summary>
    public readonly record struct Spark((double X, double Y) At, double Turn, double Px, double Ms, double Alpha);

    /// <summary>A bolt torn off: its root in the world, the unit way it goes, its length (world px), life (ms), whether it forks.</summary>
    public readonly record struct Torn((double X, double Y) Root, (double X, double Y) Way, double Px, double Ms, bool Forked);

    private readonly Func<double> _random = random ?? Random.Shared.NextDouble;
    private readonly (double X, double Y) _middle = MiddleOf(area);
    private (double X, double Y)? _was;
    private double _owedMs, _charge, _threshold = FirstThreshold, _speed;
    private (double X, double Y) _way = (1, 0);
    private int _queued;

    /// <summary>How full the charge is toward its threshold, 0 to 1: what the blade's glow shows.</summary>
    public double Charged => Math.Clamp(_charge / _threshold, 0, 1);

    /// <summary>The area's middle, in pixels of the picture: where the blade's glow sits.</summary>
    public (double X, double Y) Middle => _middle;

    /// <summary>
    /// <paramref name="elapsedMs"/> on, the blade drawn where <paramref name="held"/> takes its picture's points into the world,
    /// none while it is not shown: what it throws this frame into <paramref name="sparks"/> and <paramref name="torn"/>.
    /// </summary>
    public void Step(double elapsedMs, Func<(double X, double Y), (double X, double Y)>? held, List<Spark> sparks, List<Torn> torn)
    {
        if (held == null || area.Empty)
        {
            (_was, _charge, _queued, _owedMs) = (null, 0, 0, 0);
            return;
        }
        var now = held(_middle);
        if (_was is { } was && elapsedMs > 0)
        {
            double dx = now.X - was.X, dy = now.Y - was.Y, d = Math.Sqrt(dx * dx + dy * dy);
            _speed = d > JumpPx ? 0 : d / elapsedMs * 1000;
            if (d > 1e-9 && d <= JumpPx) _way = (dx / d, dy / d);
        }
        _was = now;
        for (_owedMs += elapsedMs; _owedMs >= FrameMs; _owedMs -= FrameMs) Frame(held, sparks, torn);
    }

    /// <summary>One 60 Hz frame: the charge built or leaking, a burst queued and let go unevenly, the fizz.</summary>
    private void Frame(Func<(double X, double Y), (double X, double Y)> held, List<Spark> sparks, List<Torn> torn)
    {
        _charge += FrameMs / 1000 * _speed / PxPerCharge;
        if (_speed < StillPx) _charge *= Leak;
        if (_charge >= _threshold)
        {
            _queued += 1 + (int)Math.Floor(_random() * 2.2 + _charge * 1.6);
            _charge -= _threshold;
            _threshold = Between(Threshold);
        }
        while (_queued > 0 && _random() < 0.55)
        {
            _queued--;
            if (area.Edge.Count > 0) torn.Add(Tear(held));
        }
        double rate = FizzPerSecond + FizzCharged * Charged;
        for (int n = (int)Math.Floor(rate * FrameMs / 1000 + _random()); n > 0; n--)
            sparks.Add(new Spark(area.AnyInside(_random), _random() * Math.Tau, Between(SparkPx), Between(SparkMs), Between(SparkAlpha)));
    }

    /// <summary>A bolt off the trailing edge: the best of three outline places by how far it faces back, flung back and out.</summary>
    private Torn Tear(Func<(double X, double Y), (double X, double Y)> held)
    {
        (double X, double Y) root = default, outward = default;
        double best = double.MinValue;
        for (int k = 0; k < 3; k++)
        {
            var e = area.AnyOnEdge(_random);
            var at = held((e.X, e.Y));
            var o = held((e.X + e.Nx * 0.5, e.Y + e.Ny * 0.5));
            var n = Unit(o.X - at.X, o.Y - at.Y);
            double score = -(n.X * _way.X + n.Y * _way.Y) + _random() * 0.4;
            if (score > best) (best, root, outward) = (score, at, n);
        }
        var way = Unit(-_way.X * 0.7 + outward.X * 0.5, -_way.Y * 0.7 + outward.Y * 0.5);
        double turn = (_random() - 0.5) * 1.2, c = Math.Cos(turn), s = Math.Sin(turn), px = LengthOf(_random());
        return new Torn(root, (way.X * c - way.Y * s, way.X * s + way.Y * c), px, Between(BoltMs), px >= ForkPx);
    }

    /// <summary>A torn bolt's length for a draw <paramref name="u"/> (0 to 1): a power law cut to <see cref="BoltPx"/>, most short, a few long.</summary>
    public static double LengthOf(double u)
    {
        double k = 1 - Math.Pow(BoltPx.From / BoltPx.To, Tail);
        return BoltPx.From / Math.Pow(1 - u * k, 1 / Tail);
    }

    private double Between((double From, double To) range) => range.From + (range.To - range.From) * _random();

    private static (double X, double Y) Unit(double x, double y)
    {
        double l = Math.Sqrt(x * x + y * y);
        return l > 1e-9 ? (x / l, y / l) : (1, 0);
    }

    private static (double X, double Y) MiddleOf(EffectArea area)
    {
        if (area.Empty) return (0, 0);
        double x = 0, y = 0;
        foreach (var p in area.Inside) (x, y) = (x + p.X, y + p.Y);
        return (x / area.Inside.Count, y / area.Inside.Count);
    }
}
