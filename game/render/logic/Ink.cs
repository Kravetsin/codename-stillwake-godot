using System;
using System.Collections.Generic;

namespace Stillwake.Game.Render;

/// <summary>
/// The ink the damage and work looks are drawn in (damage-looks), as the pick page drew them: a pen's stroke that swells and
/// thins as a nib does, a ragged blob, a band, laid down as triangles with their colours, Godot-free, for one draw call. Edges
/// fade over <see cref="Feather"/>, a pixel of the screen, which smooths them where nothing antialiases.
/// </summary>
public sealed class Ink
{
    /// <summary>A colour with its alpha, 0 to 1.</summary>
    public readonly record struct Tone(float R, float G, float B, float A)
    {
        /// <summary>A colour from 0–255 channels and an alpha.</summary>
        public static Tone Of(int r, int g, int b, float a) => new(r / 255f, g / 255f, b / 255f, a);

        public Tone Faded(float k) => this with { A = A * k };
    }

    public readonly List<(float X, float Y)> Points = [];
    public readonly List<Tone> Tones = [];
    public readonly List<int> Indices = [];

    /// <summary>How far an edge fades to nothing, in the drawing's units: a pixel of the screen.</summary>
    public float Feather { get; set; } = 0.25f;

    /// <summary>Every point laid down is moved by this first: where the drawing is.</summary>
    public (double X, double Y) Shift { get; set; }

    /// <summary>Every point laid down is kept within this box, if set: a cell's drawing stays in its cell.</summary>
    public (double Left, double Top, double Right, double Bottom)? Box { get; set; }

    private readonly List<(double X, double Y)> _dense = [];

    public bool Empty => Indices.Count == 0;

    public void Clear()
    {
        Points.Clear();
        Tones.Clear();
        Indices.Clear();
    }

    private int Add(double x, double y, Tone tone)
    {
        x += Shift.X;
        y += Shift.Y;
        if (Box is var (left, top, right, bottom)) (x, y) = (Math.Clamp(x, left, right), Math.Clamp(y, top, bottom));
        Points.Add(((float)x, (float)y));
        Tones.Add(tone);
        return Points.Count - 1;
    }

    private void Quad(int a, int b, int c, int d) => Indices.AddRange([a, b, c, b, d, c]);

    /// <summary>
    /// The pen: a line <paramref name="width"/> wide along <paramref name="line"/> that swells and thins as a nib does, lifting off at
    /// its ends (<paramref name="lift0"/>, <paramref name="lift1"/>: the share of its length it takes to reach full width from the
    /// start and to lift off at the end); its swell's phase from <paramref name="seed"/>.
    /// </summary>
    public void Pen(IReadOnlyList<(double X, double Y)> line, double width, Tone tone, int seed = 1, double lift0 = 0.2, double lift1 = 0.4)
    {
        if (line.Count < 2) return;
        var pts = Dense(line);
        int n = pts.Count;
        Span<double> along = n <= 256 ? stackalloc double[n] : new double[n];
        along[0] = 0;
        for (int i = 1; i < n; i++) along[i] = along[i - 1] + Distance(pts[i - 1], pts[i]);
        double total = along[n - 1] > 0 ? along[n - 1] : 1, phase = Draw.Hash(seed, 7) * 6.283;
        int first = Points.Count;
        for (int i = 0; i < n; i++)
        {
            double s = along[i] / total;
            var (a, b) = (pts[Math.Max(0, i - 1)], pts[Math.Min(n - 1, i + 1)]);
            double nx = -(b.Y - a.Y), ny = b.X - a.X, l = Math.Sqrt(nx * nx + ny * ny);
            if (l > 0) (nx, ny) = (nx / l, ny / l);
            double profile = Math.Min(1, Math.Min(lift0 > 0 ? 0.12 + s / lift0 : 1, lift1 > 0 ? (1 - s) / lift1 : 1));
            double half = width / 2 * Math.Max(0, profile) * (1 + 0.3 * Math.Sin(s * 9 + phase));
            Across(pts[i].X, pts[i].Y, nx, ny, half, tone);
        }
        for (int i = 0; i + 1 < n; i++)
            for (int k = 0; k < 3; k++)
            {
                int at = first + 4 * i + k;
                Quad(at, at + 1, at + 4, at + 5);
            }
    }

    /// <summary>A straight stroke of even <paramref name="width"/> from one point to another, its edges feathered.</summary>
    public void Segment((double X, double Y) from, (double X, double Y) to, double width, Tone tone)
    {
        double dx = to.X - from.X, dy = to.Y - from.Y, l = Math.Sqrt(dx * dx + dy * dy);
        if (l < 1e-6) return;
        double nx = -dy / l, ny = dx / l;
        int first = Points.Count;
        Across(from.X, from.Y, nx, ny, width / 2, tone);
        Across(to.X, to.Y, nx, ny, width / 2, tone);
        for (int k = 0; k < 3; k++) Quad(first + k, first + k + 1, first + 4 + k, first + 5 + k);
    }

    // Four points across the line at (x, y): its two faded edges and its body, which straddle the true edge; one thinner than the fade is fainter instead.
    private void Across(double x, double y, double nx, double ny, double half, Tone tone)
    {
        double f = Feather, body = Math.Max(0, half - f / 2), edge = half + f / 2;
        var ink = tone.Faded((float)Math.Min(1, 2 * half / f));
        Add(x - nx * edge, y - ny * edge, tone with { A = 0 });
        Add(x - nx * body, y - ny * body, ink);
        Add(x + nx * body, y + ny * body, ink);
        Add(x + nx * edge, y + ny * edge, tone with { A = 0 });
    }

    /// <summary>A shape round <paramref name="middle"/>, filled from it (a blob is), its rim feathered outward.</summary>
    public void Fill(IReadOnlyList<(double X, double Y)> shape, (double X, double Y) middle, Tone tone)
    {
        int n = shape.Count;
        if (n < 3) return;
        int centre = Add(middle.X, middle.Y, tone);
        for (int i = 0; i < n; i++)
        {
            var p = shape[i];
            double dx = p.X - middle.X, dy = p.Y - middle.Y, l = Math.Sqrt(dx * dx + dy * dy);
            Add(p.X, p.Y, tone);
            Add(p.X + (l > 0 ? dx / l : 0) * Feather, p.Y + (l > 0 ? dy / l : 0) * Feather, tone with { A = 0 });
        }
        for (int i = 0; i < n; i++)
        {
            int a = centre + 1 + 2 * i, b = centre + 1 + 2 * ((i + 1) % n);
            Indices.AddRange([centre, a, b]);
            Quad(a, a + 1, b, b + 1);
        }
    }

    /// <summary>The band between two lines of as many points: a crack's gap between its lips.</summary>
    public void Band(IReadOnlyList<(double X, double Y)> one, IReadOnlyList<(double X, double Y)> other, Tone tone)
    {
        int n = Math.Min(one.Count, other.Count), first = Points.Count;
        for (int i = 0; i < n; i++)
        {
            Add(one[i].X, one[i].Y, tone);
            Add(other[i].X, other[i].Y, tone);
        }
        for (int i = 0; i + 1 < n; i++) Quad(first + 2 * i, first + 2 * i + 1, first + 2 * i + 2, first + 2 * i + 3);
    }

    // A line of few points is cut into at least 16 pieces, so the nib's swell and lift run along it.
    private List<(double X, double Y)> Dense(IReadOnlyList<(double X, double Y)> line)
    {
        var dense = _dense;
        dense.Clear();
        if (line.Count >= 17)
        {
            dense.AddRange(line);
            return dense;
        }
        int per = (int)Math.Ceiling(16.0 / (line.Count - 1));
        for (int i = 1; i < line.Count; i++)
            for (int k = 0; k < per; k++)
            {
                double q = (double)k / per;
                var (a, b) = (line[i - 1], line[i]);
                dense.Add((a.X + (b.X - a.X) * q, a.Y + (b.Y - a.Y) * q));
            }
        dense.Add(line[^1]);
        return dense;
    }

    private static double Distance((double X, double Y) a, (double X, double Y) b) => Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));

    /// <summary>The pick page's shapes and randomness, so a look drawn here is the one picked, seed for seed.</summary>
    public static class Draw
    {
        /// <summary>The page's seeded randomness (mulberry32), 0 to 1.</summary>
        public sealed class Rng(int seed)
        {
            private uint _a = (uint)seed;

            public double Next()
            {
                _a += 0x6d2b79f5;
                uint t = (_a ^ (_a >> 15)) * (1 | _a);
                t = (t + ((t ^ (t >> 7)) * (61 | t))) ^ t;
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        }

        /// <summary>Two integers hashed to 0 to 1, as the page's <c>hash</c>.</summary>
        public static double Hash(int a, int b = 0)
        {
            uint h = unchecked((uint)a * 0x85ebca77u ^ (uint)(b + 0x9e37) * 0xc2b2ae3du);
            h ^= h >> 15;
            h *= 0x2c1b3c6du;
            h ^= h >> 12;
            h *= 0x297a2d39u;
            h ^= h >> 15;
            return h / 4294967296.0;
        }

        public static double Clamp(double v, double least = 0, double most = 1) => Math.Max(least, Math.Min(most, v));

        /// <summary>A line that wanders as it runs, <paramref name="length"/> in steps of <paramref name="step"/> from a point at an angle, turning by up to <paramref name="wander"/> a step.</summary>
        public static List<(double X, double Y)> Walk(Rng r, double x, double y, double angle, double length, double step = 0.5, double wander = 0.5)
        {
            var pts = new List<(double X, double Y)> { (x, y) };
            for (double d = 0; d < length; d += step)
            {
                angle += (r.Next() - 0.5) * wander;
                x += Math.Cos(angle) * step;
                y += Math.Sin(angle) * step;
                pts.Add((x, y));
            }
            return pts;
        }

        /// <summary>The first share <paramref name="k"/> of a line's points, by count, the last cut to fit: a line growing.</summary>
        public static List<(double X, double Y)> Part(List<(double X, double Y)> pts, double k)
        {
            if (k >= 1) return pts;
            if (k <= 0) return [];
            double f = (pts.Count - 1) * k, q = f - Math.Floor(f);
            int n = (int)Math.Floor(f);
            var outs = pts.GetRange(0, n + 1);
            if (n + 1 < pts.Count)
            {
                var (a, b) = (pts[n], pts[n + 1]);
                outs.Add((a.X + (b.X - a.X) * q, a.Y + (b.Y - a.Y) * q));
            }
            return outs;
        }

        /// <summary>A ragged round shape: <paramref name="k"/> points round a point, each at <paramref name="radius"/> times 1 ± <paramref name="jag"/>/2.</summary>
        public static List<(double X, double Y)> Blob(Rng r, double x, double y, double radius, int k = 9, double jag = 0.4, double sx = 1, double sy = 1)
        {
            var pts = new List<(double X, double Y)>(k);
            double a0 = r.Next() * 6.283;
            for (int i = 0; i < k; i++)
            {
                double a = a0 + (double)i / k * 6.283, q = radius * (1 - jag / 2 + jag * r.Next());
                pts.Add((x + Math.Cos(a) * q * sx, y + Math.Sin(a) * q * sy));
            }
            return pts;
        }
    }
}
