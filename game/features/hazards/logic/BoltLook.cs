using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Hazards.Logic;

/// <summary>
/// How a storm's lightning is seen, as numbers (surface-storms, «Ступенчатый лидер» over «Зарница»): the channel's shape, the
/// glow's arcs, the leader's steps, the strokes' timing and brightness. Godot-free.
/// </summary>
public static class BoltLook
{
    /// <summary>Seconds before the strike the leader steps down, and its steps.</summary>
    public const double Leader = 0.18;
    public const int LeaderSteps = 5;

    /// <summary>Seconds the return stroke takes to climb the channel.</summary>
    public const double Climb = 0.012;

    /// <summary>Seconds after the strike discharges run over the zone, and after which the bolt is done.</summary>
    public const double Discharges = 0.35, Done = 0.7;

    /// <summary>The strokes on one channel: each its start and end in seconds after the strike and its brightness; the channel glows dim between them.</summary>
    public static readonly (double From, double To, double Bright)[] Strokes = [(0, 0.09, 1), (0.15, 0.2, 0.9), (0.27, 0.31, 0.75)];

    /// <summary>The channel's brightness between strokes, and the time it fades over after the last.</summary>
    public const double Afterglow = 0.28, Fade = 0.3;

    /// <summary>
    /// A crooked channel from <paramref name="from"/> down to <paramref name="to"/> by midpoint displacement: sharp kinks at
    /// irregular spacing, wandering sideways by up to a fifth of its length, its last few pixels straight onto the cell.
    /// </summary>
    public static List<(double X, double Y)> Channel(Random random, (double X, double Y) from, (double X, double Y) to)
    {
        var points = new List<(double X, double Y)> { from, to };
        double length = to.Y - from.Y, wander = length * 0.22, settle = Math.Min(length * 0.06, 20);
        int depths = Math.Max(6, (int)Math.Ceiling(Math.Log2(Math.Max(1, length) / 8)));
        for (int depth = 0; depth < depths; depth++, wander *= 0.52)
        {
            var next = new List<(double X, double Y)>(points.Count * 2) { points[0] };
            for (int i = 1; i < points.Count; i++)
            {
                var a = points[i - 1];
                var b = points[i];
                (double X, double Y) mid = ((a.X + b.X) / 2 + (random.NextDouble() - 0.5) * wander, (a.Y + b.Y) / 2 + (random.NextDouble() - 0.5) * wander * 0.25);
                // Near the ground it settles onto the cell: no kink in its last few pixels.
                if (mid.Y > to.Y - settle) mid.X = to.X;
                next.Add(mid);
                next.Add(b);
            }
            points = next;
        }
        return points;
    }

    /// <summary>
    /// Forks off the channel between <paramref name="reach"/> and a sixth of it above the ground, where a view sees them however
    /// high the channel starts: down and aside, each shorter and kinked, ending in air.
    /// </summary>
    public static IEnumerable<List<(double X, double Y)>> Branches(Random random, List<(double X, double Y)> channel, double reach)
    {
        int count = 3 + random.Next(3);
        double length = reach * 0.18, ground = channel[^1].Y;
        int first = channel.FindIndex(p => p.Y >= ground - reach), last = channel.FindLastIndex(p => p.Y <= ground - reach / 6);
        if (first < 0 || last <= first) yield break;
        for (int k = 0; k < count; k++)
        {
            int at = first + random.Next(last - first);
            double side = random.Next(2) == 0 ? -1 : 1;
            var start = channel[at];
            var end = (start.X + side * length * (0.5 + random.NextDouble() * 0.6), start.Y + length * (0.6 + random.NextDouble() * 0.7));
            yield return Channel(random, start, end);
        }
    }

    /// <summary>How many of the channel's points the leader has reached, <paramref name="seconds"/> before the strike, in whole steps.</summary>
    public static int LeaderReach(int points, double seconds)
    {
        double k = Math.Clamp(1 - seconds / Leader, 0, 1);
        return (int)Math.Ceiling(points * Math.Ceiling(k * LeaderSteps) / LeaderSteps);
    }

    /// <summary>How many of the channel's points, from the ground up, the return stroke has climbed <paramref name="since"/> seconds after the strike.</summary>
    public static int ReturnReach(int points, double since) => since >= Climb ? points : Math.Max(2, (int)(points * since / Climb));

    /// <summary>The channel's brightness <paramref name="since"/> seconds after the strike: whole in a stroke, dim between, fading after the last.</summary>
    public static double Brightness(double since)
    {
        if (since < 0) return 0;
        foreach (var (from, to, bright) in Strokes)
            if (since >= from && since < to) return bright;
        double last = Strokes[^1].To;
        return since < last ? Afterglow : Math.Max(0, Afterglow * (1 - (since - last) / Fade));
    }

    /// <summary>Whether the branches burn: in the first stroke only.</summary>
    public static bool Branched(double since) => since >= 0 && since < Strokes[0].To;

    /// <summary>
    /// How hard the channel lights the scene and shades the rest («Свет от канала»), 0 to 1, <paramref name="since"/> seconds after
    /// the strike: whole in a stroke, stronger than the channel's own glow between, a faint tail to <see cref="LightTail"/>.
    /// </summary>
    public static double ChannelLight(double since)
    {
        if (since < 0) return 0;
        double bright = Brightness(since), tail = 0.12 * (1 - Smooth(0, LightTail, since));
        bool stroke = Array.Exists(Strokes, s => since >= s.From && since < s.To);
        return Math.Clamp((stroke ? bright : bright * 1.4) + tail, 0, 1);
    }

    /// <summary>Seconds the scene stays lit by a strike, its tail included.</summary>
    public const double LightTail = 0.9;

    /// <summary>How strong the zone's fires show («Огни святого Эльма»): plain early and to the zone's edge, so its reach reads at a glance.</summary>
    public static double FireStrength(double grown, float off) => (0.25 + 0.75 * grown) * (1 - 0.45 * off);

    /// <summary>Whether the fire seeded <paramref name="seed"/> burns yet: more of them light as the glow grows.</summary>
    public static bool FireLit(int seed, double grown) => Hash(seed, 12) < 0.35 + 0.65 * grown;

    /// <summary>A hero's helmet fire's strength: lit past a third of the glow, flickering.</summary>
    public static double HelmetFire(double grown, double seconds) =>
        grown <= 0.35 ? 0 : Math.Clamp((grown - 0.35) * 1.6, 0, 1) * (0.7 + 0.3 * Math.Sin(seconds * 4.1));

    /// <summary>
    /// The channel's afterimage over a hero's view in the zone («Ожог на сетчатке»), 0 to 1, <paramref name="since"/> seconds
    /// after the strike: in by 0.3 s, held, paling from 4 s, out by <see cref="BurnDone"/> (the user's, 2026-11-16); and its white edge flash, 0 to 1, gone by <see cref="EdgeFlash"/>.
    /// </summary>
    public static double Burn(double since) => Smooth(0.03, 0.3, since) * (1 - Smooth(4, BurnDone, since));

    public static double Edge(double since) => since < 0 || since >= EdgeFlash ? 0 : 1 - since / EdgeFlash;

    public const double EdgeFlash = 0.16, BurnDone = 6;

    /// <summary>A number in [0, 1) fixed by <paramref name="a"/> and <paramref name="b"/>: the same fire in the same place each frame.</summary>
    public static double Hash(int a, int b)
    {
        uint h = unchecked((uint)a * 374761393u + (uint)b * 668265263u);
        h = unchecked((h ^ (h >> 13)) * 1274126177u);
        return (h ^ (h >> 16)) / 4294967296.0;
    }

    /// <summary>A jagged line from <paramref name="from"/> to <paramref name="to"/>, kinked across its way by up to <paramref name="rough"/> of its length.</summary>
    public static List<(double X, double Y)> Jag(Random random, (double X, double Y) from, (double X, double Y) to, double rough, int depth)
    {
        var points = new List<(double X, double Y)> { from, to };
        double nx = from.Y - to.Y, ny = to.X - from.X, wander = rough;
        for (int d = 0; d < depth; d++, wander *= 0.55)
        {
            var next = new List<(double X, double Y)>(points.Count * 2) { points[0] };
            for (int i = 1; i < points.Count; i++)
            {
                var (a, b) = (points[i - 1], points[i]);
                double k = (random.NextDouble() - 0.5) * wander;
                next.Add(((a.X + b.X) / 2 + nx * k, (a.Y + b.Y) / 2 + ny * k));
                next.Add(b);
            }
            points = next;
        }
        return points;
    }

    private static double Smooth(double from, double to, double x)
    {
        double t = Math.Clamp((x - from) / (to - from), 0, 1);
        return t * t * (3 - 2 * t);
    }
}
