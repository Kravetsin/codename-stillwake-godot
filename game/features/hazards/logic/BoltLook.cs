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
        double wander = (to.Y - from.Y) * 0.22;
        for (int depth = 0; depth < 6; depth++, wander *= 0.52)
        {
            var next = new List<(double X, double Y)>(points.Count * 2) { points[0] };
            for (int i = 1; i < points.Count; i++)
            {
                var a = points[i - 1];
                var b = points[i];
                (double X, double Y) mid = ((a.X + b.X) / 2 + (random.NextDouble() - 0.5) * wander, (a.Y + b.Y) / 2 + (random.NextDouble() - 0.5) * wander * 0.25);
                // Near the ground it settles onto the cell: no kink in the last tenth.
                if (mid.Y > to.Y - (to.Y - from.Y) * 0.06) mid.X = to.X;
                next.Add(mid);
                next.Add(b);
            }
            points = next;
        }
        return points;
    }

    /// <summary>Forks off the channel's upper two thirds, down and aside, each shorter and kinked, ending in air.</summary>
    public static IEnumerable<List<(double X, double Y)>> Branches(Random random, List<(double X, double Y)> channel)
    {
        int count = 3 + random.Next(3);
        double length = (channel[^1].Y - channel[0].Y) * 0.18;
        for (int k = 0; k < count; k++)
        {
            int at = (int)(channel.Count * (0.12 + 0.55 * random.NextDouble()));
            double side = random.Next(2) == 0 ? -1 : 1;
            var start = channel[at];
            var end = (start.X + side * length * (0.5 + random.NextDouble() * 0.6), start.Y + length * (0.6 + random.NextDouble() * 0.7));
            yield return Channel(random, start, end);
        }
    }

    /// <summary>How likely a zone cell sparks an arc this frame: more as the glow grows and nearer the middle, most on the struck cell.</summary>
    public static double GlowRate(double grown, float off, bool struck) =>
        struck ? 0.25 + 0.5 * grown : (0.015 + 0.07 * grown) * (1 - 0.6 * off);

    /// <summary>How bright the glow's arcs are: dim at first, near whole by the strike.</summary>
    public static double GlowIntensity(double grown) => 0.35 + 0.55 * grown;

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
}
