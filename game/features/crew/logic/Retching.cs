using System;
using System.Collections.Generic;
using Stillwake.Game.Render;
using Tone = Stillwake.Game.Render.Ink.Tone;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A hero throwing up (food-tuning), drawn as picked (bile-slide): a gag, then three gushes of thin olive bile as the body
/// hunches over them. Bare, it sprays from the mouth and lies in puddles with a dark tide line (<see cref="RetchSpray"/>);
/// in a helmet it splashes over the glass and slides down it in streaks (<see cref="Film"/>). Times in ms from the purge.
/// </summary>
public static class Retching
{
    /// <summary>How long the gag takes before the first gush.</summary>
    public const double GushMs = 500;

    /// <summary>How long after the purge the glass is clear again: the figure has nothing left to show.</summary>
    public const double Ms = GushMs + 40 + 5400;

    /// <summary>Bile: its body, its tide line, its shine.</summary>
    public static readonly (int R, int G, int B) Fill = (172, 164, 62), Rim = (96, 90, 28), Shine = (222, 216, 140);

    public static double Clamp(double x, double least = 0, double most = 1) => Math.Min(most, Math.Max(least, x));

    public static double Smooth(double a, double b, double x)
    {
        double t = Clamp((x - a) / (b - a));
        return t * t * (3 - 2 * t);
    }

    /// <summary>A fixed scatter in [0, 1) for <paramref name="seed"/> and <paramref name="i"/>, as the pick page laid its shapes out.</summary>
    public static double Hash(double seed, int i)
    {
        double v = Math.Sin(seed * 12.9898 + i * 78.233) * 43758.5453;
        return v - Math.Floor(v);
    }

    private static double Gag(double ms) => Math.Sin(Math.PI * Clamp(ms / 300));

    private static double Since(double ms) => (ms - GushMs) / 1000;

    /// <summary>How far the body is hunched over, 0 to 1: down with the first gush, up again after the last.</summary>
    public static double Hunch(double ms)
    {
        double s = Since(ms);
        return Smooth(-0.1, 0.06, s) * (1 - Smooth(0.85, 1.4, s));
    }

    private static double Heave(double ms)
    {
        double s = Since(ms);
        return 0.05 * Math.Sin(Math.PI * Clamp((s - 0.5) / 0.35)) + 0.03 * Math.Sin(Math.PI * Clamp((s - 0.88) / 0.2));
    }

    /// <summary>The torso's lean forward, in radians.</summary>
    public static double Lean(double ms) => ms is < 0 or >= Ms ? 0 : 0.09 * Gag(ms) + 0.4 * Hunch(ms) + Heave(ms);

    /// <summary>The head's nod down on top of the torso's lean, in radians.</summary>
    public static double Nod(double ms) => ms is < 0 or >= Ms ? 0 : 0.05 * Gag(ms) + 0.24 * Hunch(ms) + 0.6 * Heave(ms);

    /// <summary>How hard it is coming up, 0 to 1: three gushes, each weaker.</summary>
    public static double Gush(double ms)
    {
        double s = Since(ms);
        static double Bump(double s, double a, double e, double h) => s > a && s < e ? h * Math.Sin(Math.PI * (s - a) / (e - a)) : 0;
        return Bump(s, 0.02, 0.42, 1) + Bump(s, 0.55, 0.8, 0.75) + Bump(s, 0.92, 1.02, 0.35);
    }

    // The scratch a blob's outline is laid in: copied by the ink at once, so one serves every blob.
    private static readonly List<(double X, double Y)> Outline = [];

    /// <summary>
    /// An irregular blob about (<paramref name="x"/>, <paramref name="y"/>): <paramref name="n"/> points jagged by
    /// <paramref name="jag"/> from <paramref name="seed"/>, joined by curves through their midpoints, each point mapped by
    /// <paramref name="map"/>; filled in <paramref name="tone"/>.
    /// </summary>
    public static void Blob(Ink ink, double x, double y, double rx, double ry, double turn, double seed, double jag, int n, Tone tone, Func<(double X, double Y), (double X, double Y)>? map = null)
    {
        rx = Math.Max(0.02, rx);
        ry = Math.Max(0.02, ry);
        double c = Math.Cos(turn), s = Math.Sin(turn);
        Span<(double X, double Y)> p = stackalloc (double, double)[n];
        for (int i = 0; i < n; i++)
        {
            double a = (double)i / n * Math.PI * 2, j = 1 + jag * (Hash(seed, i) - 0.5) * 2;
            double px = Math.Cos(a) * rx * j, py = Math.Sin(a) * ry * j;
            p[i] = (x + px * c - py * s, y + px * s + py * c);
        }
        Outline.Clear();
        for (int i = 0; i < n; i++)
        {
            var (from, at, to) = (Mid(p[(i + n - 1) % n], p[i]), p[i], Mid(p[i], p[(i + 1) % n]));
            for (int k = 0; k < 4; k++)
            {
                double t = k / 4.0, u = 1 - t;
                var q = (u * u * from.X + 2 * u * t * at.X + t * t * to.X, u * u * from.Y + 2 * u * t * at.Y + t * t * to.Y);
                Outline.Add(map == null ? q : map(q));
            }
        }
        var middle = map == null ? (x, y) : map((x, y));
        ink.Fill(Outline, middle, tone);
    }

    private static (double X, double Y) Mid((double X, double Y) a, (double X, double Y) b) => ((a.X + b.X) / 2, (a.Y + b.Y) / 2);

    /// <summary>
    /// The glass's bile <paramref name="s"/> seconds after it first hits: splashed over it, sliding down from half a second in, the
    /// streaks it leaves fading, gone by 5.4. Laid out on the basic visor's glass in its picture's pixels (x 50 to 64, y 23 to 50)
    /// and placed by <paramref name="map"/> on the glass worn; <paramref name="scale"/> is how wide a picture's pixel is placed.
    /// </summary>
    public static void Film(Ink ink, double s, Func<(double X, double Y), (double X, double Y)> map, double scale)
    {
        if (s is < 0 or > 5.4) return;
        double r = Smooth(0, 0.16, s), slide = s < 0.55 ? 0 : 29 * Math.Pow(Clamp((s - 0.55) / 3.4), 1.6);
        double fade = (1 - Smooth(4.4, 5.3, s)) * 0.93;
        // The streaks left behind as the film slides.
        if (slide > 0)
        {
            var streak = Of(Fill, 0.5 * fade * (1 - Smooth(2.5, 4.6, s)));
            for (int i = 0; i < 6; i++)
            {
                double x = 50.5 + i * 2.6 + Hash(3, i) * 1.2, w = 0.8 + Hash(4, i) * 1.3, top = 25 + Hash(5, i) * 8;
                Outline.Clear();
                foreach (var q in new[] { (x, top), (x + w, top + 1), (x + w * 0.8, top + slide + 6), (x + 0.2, top + slide + 7) }) Outline.Add(map(q));
                ink.Fill(Outline, map((x + w / 2, top + slide / 2 + 3.5)), streak);
            }
        }
        var (rim, fill) = (Of(Rim, fade), Of(Fill, fade));
        foreach (var (tone, g) in new[] { (rim, 0.0), (fill, -0.9) })
        {
            Blob(ink, 57, 44 + slide, 10 * r + g, 13 * r + g, 0, 1, 0.3, 8, tone, map);
            for (int i = 0; i < 7; i++)
                Blob(ink, 50 + Hash(9, i) * 14, 24 + Hash(10, i) * 14 + slide, (2 + Hash(11, i) * 2.4) * r + g, (2.2 + Hash(12, i) * 2.6) * r + g, 0, 20 + i, 0.3, 8, tone, map);
            // Drips hanging from the film's lower edge.
            for (int i = 0; i < 5; i++)
            {
                double length = 2 + Hash(13, i) * 5 * Smooth(0.3, 1.2, s);
                Blob(ink, 51 + i * 3, 50 + slide + length * 0.5, 0.9 + g, length + g, 0, 40 + i, 0.15, 6, tone, map);
            }
            // What has run down gathers on the rim.
            double pool = Math.Min(4.5, slide * 0.25);
            if (pool > 0.2) Blob(ink, 57, 51, 10 + g, pool + g, 0, 77, 0.1, 9, tone, map);
        }
        // The glass's own shine stays over it.
        double shine = (1 - Smooth(4.4, 5.3, s)) * Smooth(0, 0.2, s);
        if (shine <= 0) return;
        var gleam = new Tone(0xee / 255f, 0xf3 / 255f, 0xf6 / 255f, (float)(0.45 * 0.93 * shine));
        var last = map((53.5, 27));
        for (int k = 1; k <= 6; k++)
        {
            double t = k / 6.0, u = 1 - t;
            var at = map((u * u * 53.5 + 2 * u * t * 51.6 + t * t * 52, u * u * 27 + 2 * u * t * 31 + t * t * 36));
            ink.Segment(last, at, 1.2 * scale, gleam);
            last = at;
        }
    }

    public static Tone Of((int R, int G, int B) c, double a) => Tone.Of(c.R, c.G, c.B, (float)a);
}
