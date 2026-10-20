using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A drill's light (0054, 0075), one a figure. Where its bit bites a wreck, the axe's hit in small and without end: a
/// steady spray of sparks thrown back off the bit, slow metal dust, now and then a tumbling chip, a small glint every
/// so often and a warm light held there. The arc drill's coil besides, while it runs: short arcs crawling over it and
/// now and then one leaping off it into the air, the electric spear's bolts (<see cref="Bolts"/>); the plain drill has none.
/// </summary>
public sealed class DrillLight
{
    /// <summary>What a biting bit throws a second, times <see cref="Fx.Count"/>, and how often it glints.</summary>
    private const double SparksPerSecond = 26, DustPerSecond = 6, ChipsPerSecond = 1.5, GlintEveryMs = 240;
    /// <summary>Arcs a second over a running arc drill's coil at full speed, crawling and leaping off it.</summary>
    private const double CrawlsPerSecond = 9, LeapsPerSecond = 1.6;
    private static readonly (int, int, int) Warm = (255, 200, 140), Glinted = (255, 214, 150);

    private readonly Random _random = new();
    private double _sparks, _dust, _chips, _sinceGlint = GlintEveryMs, _crawls, _leaps;

    private double Between(double from, double to) => from + (to - from) * _random.NextDouble();

    /// <summary>
    /// The drill <paramref name="elapsedMs"/> on: <paramref name="look"/> held, none without a drill, spinning at
    /// <paramref name="spin"/> (0 to 1); biting a wreck at <paramref name="tip"/> while <paramref name="bites"/>, its bit
    /// pointing <paramref name="barrel"/>. <paramref name="held"/> takes a point of its picture to the world, as drawn now.
    /// </summary>
    public void Step(HeldLook? look, double spin, bool bites, Vector2? tip, Vector2 barrel, Func<(double X, double Y), Vector2> held, double elapsedMs)
    {
        if (bites && tip is { } at) Bite(at, barrel, elapsedMs);
        else _sinceGlint = GlintEveryMs;
        if (look?.Coil is { } coil && spin > 0.3) Arcs(coil, spin, held, elapsedMs);
    }

    /// <summary>Sparks thrown back off the bit and spread, dust, a chip now and then, a glint every so often, a warm light.</summary>
    private void Bite(Vector2 tip, Vector2 barrel, double elapsedMs)
    {
        var glims = Glims.Shared;
        double s = elapsedMs / 1000 * Fx.Count, back = Math.Atan2(-barrel.Y, -barrel.X);
        for (_sparks += SparksPerSecond * s; _sparks >= 1; _sparks--)
        {
            double a = back + Between(-1.1, 1.1), v = Between(45, 120);
            glims.Add(Luminous.DrillSpark, tip.X, tip.Y, Math.Cos(a) * v, Math.Sin(a) * v);
        }
        for (_dust += DustPerSecond * s; _dust >= 1; _dust--)
        {
            double a = back + Between(-1.8, 1.8), v = Between(4, 16);
            glims.Add(Luminous.HitDust, tip.X, tip.Y, Math.Cos(a) * v, Math.Sin(a) * v, size: Between(0.35, 0.55));
        }
        for (_chips += ChipsPerSecond * s; _chips >= 1; _chips--)
        {
            double a = back + Between(-1.1, 1.1), v = Between(12, 26);
            glims.Add(Luminous.Chip, tip.X, tip.Y, Math.Cos(a) * v, Math.Sin(a) * v, size: Luminous.Chip.Size * 0.65);
        }
        if ((_sinceGlint += elapsedMs) >= GlintEveryMs)
        {
            _sinceGlint = 0;
            Flares.Shared.Glint(tip.X, tip.Y, 4, Glinted, Between(-0.2, 0.2));
        }
        Flares.Shared.Hold(this, tip.X, tip.Y, Warm, 0.22 * Fx.Glow, 2, 0.45f, 0.7);
    }

    /// <summary>The arc drill's coil, the box <paramref name="coil"/> of its picture: arcs crawling across it, now and then one leaping off.</summary>
    private void Arcs((double X0, double Y0, double X1, double Y1) coil, double spin, Func<(double X, double Y), Vector2> held, double elapsedMs)
    {
        double s = elapsedMs / 1000 * spin;
        for (_crawls += CrawlsPerSecond * s; _crawls >= 1; _crawls--)
        {
            double x = Between(coil.X0, coil.X1 - 2), bulge = (_random.NextDouble() < 0.5 ? -1 : 1) * 0.45;
            (double, double) from = (x, coil.Y0), to = (x + Between(1.5, 3.5), coil.Y1);
            Bolts.Shared.Add(Ends(held, from, to, bulge), _random.Next(5, 11), 0.8, 0.4);
        }
        for (_leaps += LeapsPerSecond * s; _leaps >= 1; _leaps--)
        {
            double x = Between(coil.X0, coil.X1);
            (double, double) from = (x, coil.Y0), to = (x + Between(-3, 3), coil.Y0 - Between(4, 8));
            Bolts.Shared.Add(Ends(held, from, to, Between(-0.15, 0.15)), _random.Next(4, 7), 0.9, 0.6, branchy: true);
        }
    }

    /// <summary>An arc's ends at two points of the drill's picture, wherever it is drawn when asked.</summary>
    private static BoltEnds Ends(Func<(double X, double Y), Vector2> held, (double X, double Y) from, (double X, double Y) to, double bulge) => () =>
    {
        var (a, b) = (held(from), held(to));
        return (a.X, a.Y, b.X, b.Y, bulge);
    };
}
