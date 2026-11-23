using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Wrecked;

/// <summary>A colour with its alpha, each 0 to 1, straight (not premultiplied).</summary>
public readonly record struct Rgba(float R, float G, float B, float A)
{
    /// <summary>A colour of 0xRRGGBB at <paramref name="a"/>.</summary>
    public static Rgba Of(uint rgb, float a = 1) => new(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, a);

    /// <summary>A colour of 0–255 channels at <paramref name="a"/>, as the mockup's <c>rgba()</c>.</summary>
    public static Rgba Of(int r, int g, int b, double a) => new(r / 255f, g / 255f, b / 255f, (float)a);

    /// <summary><paramref name="a"/> toward <paramref name="b"/> by <paramref name="t"/>, channel by channel (the mockup's mixN), as 0xRRGGBB.</summary>
    public static uint Mix(uint a, uint b, double t)
    {
        uint Ch(int s) => (uint)Math.Round(((a >> s) & 255) + ((((b >> s) & 255) - (double)((a >> s) & 255)) * t)) & 255;
        return (Ch(16) << 16) | (Ch(8) << 8) | Ch(0);
    }
}

/// <summary>A picture to lay tinted, as its pixels: a machine's, what a burnt or frosted one is darkened or paled through.</summary>
public sealed record WreckPicture(int Width, int Height, byte[] Rgba);

/// <summary>
/// A canvas a wreck's looks are painted on (vessel-wrecks W-2), a square of <see cref="ChunkCells"/> cells at a time, <see cref="Texels"/>
/// to a cell: shapes in cells, filled with their coverage by two samples a row, clipped to the cells a look allows, laid over what is
/// there. The mockup drew on a 2-D canvas; this is that canvas, Godot-free, so each look ports line for line and a chunk can be painted
/// again when its cells change.
/// </summary>
public sealed class WreckPaint
{
    /// <summary>Texels a cell across: half again the world's pixels, so a hairline still reads.</summary>
    public const int Texels = 12;

    /// <summary>Cells a chunk across, each a picture of its own the view draws only in view.</summary>
    public const int ChunkCells = 32;

    public const int Side = Texels * ChunkCells;

    private readonly float[] _px = new float[Side * Side * 4];
    private readonly float[] _cover = new float[Side];
    private readonly List<double> _cuts = [];

    /// <summary>The chunk painted, by its place: its top-left cell is (<c>ChunkX</c>, <c>ChunkY</c>) × <see cref="ChunkCells"/>.</summary>
    public int ChunkX { get; private set; }

    public int ChunkY { get; private set; }

    /// <summary>Whether anything was laid on the chunk since it began.</summary>
    public bool Painted { get; private set; }

    /// <summary>What every shape laid is multiplied by: an old look dulled.</summary>
    public float Alpha { get; set; } = 1;

    /// <summary>The cells a shape may lay on, null for any: a look kept to the walls or to the rooms.</summary>
    public Func<int, int, bool>? Clip { get; set; }

    /// <summary>The chunk's cells, first and past the last, each way: what a look that walks cells walks.</summary>
    public (int X0, int Y0, int X1, int Y1) Cells => (ChunkX * ChunkCells, ChunkY * ChunkCells, (ChunkX + 1) * ChunkCells, (ChunkY + 1) * ChunkCells);

    /// <summary>The chunk of cell (<paramref name="x"/>, <paramref name="y"/>).</summary>
    public static (int X, int Y) ChunkOf(int x, int y) => (FloorDiv(x, ChunkCells), FloorDiv(y, ChunkCells));

    public static int FloorDiv(int a, int b) => a >= 0 ? a / b : -((-a + b - 1) / b);

    /// <summary>Starts chunk (<paramref name="cx"/>, <paramref name="cy"/>), clear.</summary>
    public void Begin(int cx, int cy)
    {
        (ChunkX, ChunkY, Painted, Alpha, Clip) = (cx, cy, false, 1, null);
        Array.Clear(_px);
    }

    /// <summary>The chunk's texels as straight RGBA8, row by row: what its picture is made of.</summary>
    public byte[] Bytes()
    {
        var bytes = new byte[Side * Side * 4];
        for (int i = 0; i < Side * Side; i++)
        {
            float a = _px[i * 4 + 3];
            if (a <= 0) continue;
            for (int k = 0; k < 3; k++) bytes[i * 4 + k] = (byte)Math.Clamp(_px[i * 4 + k] / a * 255 + 0.5f, 0, 255);
            bytes[i * 4 + 3] = (byte)Math.Clamp(a * 255 + 0.5f, 0, 255);
        }
        return bytes;
    }

    /// <summary>The alpha laid at cell-space point (<paramref name="x"/>, <paramref name="y"/>) of this chunk, 0 to 1: what tests read.</summary>
    public float AlphaAt(double x, double y)
    {
        int tx = (int)Math.Floor((x - ChunkX * ChunkCells) * Texels), ty = (int)Math.Floor((y - ChunkY * ChunkCells) * Texels);
        return tx < 0 || ty < 0 || tx >= Side || ty >= Side ? 0 : _px[(ty * Side + tx) * 4 + 3];
    }

    /// <summary>A polygon filled, even-odd, its points in cells.</summary>
    public void Fill(IReadOnlyList<(double X, double Y)> points, Rgba colour) => Fill(points, colour, null);

    /// <summary>A polygon filled with a colour that runs from <paramref name="inner"/> at (<paramref name="cx"/>, <paramref name="cy"/>) to <paramref name="outer"/> <paramref name="r"/> cells off.</summary>
    public void FillRadial(IReadOnlyList<(double X, double Y)> points, double cx, double cy, double r, Rgba inner, Rgba outer) =>
        Fill(points, inner, (x, y) =>
        {
            double t = Math.Min(1, Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / r);
            return new Rgba(inner.R + (outer.R - inner.R) * (float)t, inner.G + (outer.G - inner.G) * (float)t, inner.B + (outer.B - inner.B) * (float)t,
                inner.A + (outer.A - inner.A) * (float)t);
        });

    public void Rect(double x, double y, double w, double h, Rgba colour) => Fill([(x, y), (x + w, y), (x + w, y + h), (x, y + h)], colour);

    /// <summary>An ellipse filled round (<paramref name="cx"/>, <paramref name="cy"/>), turned <paramref name="turn"/> radians.</summary>
    public void Ellipse(double cx, double cy, double rx, double ry, Rgba colour, double turn = 0) => Fill(Shapes.Ellipse(cx, cy, rx, ry, turn), colour);

    /// <summary>A line through <paramref name="points"/>, <paramref name="width"/> cells wide, each link a band of its own.</summary>
    public void Stroke(IReadOnlyList<(double X, double Y)> points, double width, Rgba colour, bool closed = false)
    {
        int n = points.Count;
        for (int i = 0; i + 1 < n + (closed ? 1 : 0); i++)
        {
            var (x0, y0) = points[i];
            var (x1, y1) = points[(i + 1) % n];
            double dx = x1 - x0, dy = y1 - y0, l = Math.Sqrt(dx * dx + dy * dy);
            if (l < 1e-9) continue;
            double px = -dy / l * width / 2, py = dx / l * width / 2;
            Fill([(x0 + px, y0 + py), (x1 + px, y1 + py), (x1 - px, y1 - py), (x0 - px, y0 - py)], colour);
        }
    }

    /// <summary>
    /// <paramref name="picture"/> laid over cells (<paramref name="x"/>, <paramref name="y"/>) to <paramref name="w"/> by <paramref name="h"/>,
    /// its shape in <paramref name="tint"/>: each of its pixels' alpha times the tint's.
    /// </summary>
    public void Picture(WreckPicture picture, double x, double y, double w, double h, Rgba tint)
    {
        var (ox, oy) = (ChunkX * ChunkCells, ChunkY * ChunkCells);
        int tx0 = Math.Max(0, (int)Math.Floor((x - ox) * Texels)), ty0 = Math.Max(0, (int)Math.Floor((y - oy) * Texels));
        int tx1 = Math.Min(Side, (int)Math.Ceiling((x + w - ox) * Texels)), ty1 = Math.Min(Side, (int)Math.Ceiling((y + h - oy) * Texels));
        for (int ty = ty0; ty < ty1; ty++)
            for (int tx = tx0; tx < tx1; tx++)
            {
                double cx = ox + (tx + 0.5) / Texels, cy = oy + (ty + 0.5) / Texels;
                int px = (int)((cx - x) / w * picture.Width), py = (int)((cy - y) / h * picture.Height);
                if (px < 0 || py < 0 || px >= picture.Width || py >= picture.Height) continue;
                float a = picture.Rgba[(py * picture.Width + px) * 4 + 3] / 255f;
                if (a > 0) Lay(tx, ty, tint, a);
            }
    }

    private void Fill(IReadOnlyList<(double X, double Y)> points, Rgba colour, Func<double, double, Rgba>? shade)
    {
        int n = points.Count;
        if (n < 3 || colour.A * Alpha <= 0 && shade == null) return;
        double ox = ChunkX * ChunkCells, oy = ChunkY * ChunkCells;
        double minY = double.MaxValue, maxY = double.MinValue, minX = double.MaxValue, maxX = double.MinValue;
        foreach (var (x, y) in points)
        {
            (minX, maxX, minY, maxY) = (Math.Min(minX, x), Math.Max(maxX, x), Math.Min(minY, y), Math.Max(maxY, y));
        }
        int r0 = Math.Max(0, (int)Math.Floor((minY - oy) * Texels)), r1 = Math.Min(Side - 1, (int)Math.Ceiling((maxY - oy) * Texels));
        if (r0 > r1 || (maxX - ox) * Texels < 0 || (minX - ox) * Texels > Side) return;
        for (int row = r0; row <= r1; row++)
        {
            int lo = Side, hi = -1;
            for (int s = 0; s < 2; s++)
            {
                double y = oy + (row + 0.25 + 0.5 * s) / Texels;
                _cuts.Clear();
                for (int i = 0; i < n; i++)
                {
                    var (x0, y0) = points[i];
                    var (x1, y1) = points[(i + 1) % n];
                    if ((y0 <= y) == (y1 <= y)) continue;
                    _cuts.Add(((x0 + (y - y0) / (y1 - y0) * (x1 - x0)) - ox) * Texels);
                }
                _cuts.Sort();
                for (int k = 0; k + 1 < _cuts.Count; k += 2)
                {
                    double a = Math.Max(0, _cuts[k]), b = Math.Min(Side, _cuts[k + 1]);
                    if (b <= a) continue;
                    int ta = (int)a, tb = Math.Min(Side - 1, (int)Math.Ceiling(b) - 1);
                    for (int t = ta; t <= tb; t++) _cover[t] += 0.5f * (float)(Math.Min(b, t + 1) - Math.Max(a, t));
                    (lo, hi) = (Math.Min(lo, ta), Math.Max(hi, tb));
                }
            }
            for (int t = lo; t <= hi; t++)
            {
                float cov = Math.Min(1, _cover[t]);
                _cover[t] = 0;
                if (cov <= 0) continue;
                var c = shade?.Invoke(ox + (t + 0.5) / Texels, oy + (row + 0.5) / Texels) ?? colour;
                Lay(t, row, c, cov);
            }
        }
    }

    private void Lay(int tx, int ty, Rgba c, float cover)
    {
        if (Clip != null && !Clip(ChunkX * ChunkCells + tx / Texels, ChunkY * ChunkCells + ty / Texels)) return;
        float a = c.A * Alpha * cover;
        if (a <= 0) return;
        int i = (ty * Side + tx) * 4;
        float keep = 1 - a;
        _px[i] = c.R * a + _px[i] * keep;
        _px[i + 1] = c.G * a + _px[i + 1] * keep;
        _px[i + 2] = c.B * a + _px[i + 2] * keep;
        _px[i + 3] = a + _px[i + 3] * keep;
        Painted = true;
    }
}

/// <summary>Shapes as points in cells: ellipses, blobs, curves flattened.</summary>
public static class Shapes
{
    public static List<(double X, double Y)> Ellipse(double cx, double cy, double rx, double ry, double turn = 0, int n = 16)
    {
        var pts = new List<(double, double)>(n);
        double c = Math.Cos(turn), s = Math.Sin(turn);
        for (int j = 0; j < n; j++)
        {
            double a = j * Math.PI * 2 / n, x = Math.Cos(a) * rx, y = Math.Sin(a) * ry;
            pts.Add((cx + x * c - y * s, cy + x * s + y * c));
        }
        return pts;
    }

    /// <summary>The mockup's blob: fourteen points round a centre, each at 0.72–1.14 of the radius, squashed <paramref name="sq"/> up and down.</summary>
    public static List<(double X, double Y)> Blob(double cx, double cy, double rad, uint seed, double sq = 1)
    {
        var r = new Mulberry(seed);
        var pts = new List<(double, double)>(14);
        for (int j = 0; j < 14; j++)
        {
            double a = j * Math.PI * 2 / 14, rr = rad * (0.72 + r.Next() * 0.42);
            pts.Add((cx + Math.Cos(a) * rr, cy + Math.Sin(a) * rr * sq));
        }
        return pts;
    }

    /// <summary>A quadratic curve from <paramref name="a"/> through control <paramref name="c"/> to <paramref name="b"/>, in <paramref name="n"/> links.</summary>
    public static List<(double X, double Y)> Quad((double X, double Y) a, (double X, double Y) c, (double X, double Y) b, int n = 8)
    {
        var pts = new List<(double, double)>(n + 1);
        for (int i = 0; i <= n; i++)
        {
            double t = (double)i / n, u = 1 - t;
            pts.Add((u * u * a.X + 2 * u * t * c.X + t * t * b.X, u * u * a.Y + 2 * u * t * c.Y + t * t * b.Y));
        }
        return pts;
    }

    /// <summary>A cubic curve from <paramref name="a"/> to <paramref name="d"/>, in <paramref name="n"/> links.</summary>
    public static List<(double X, double Y)> Cubic((double X, double Y) a, (double X, double Y) b, (double X, double Y) c, (double X, double Y) d, int n = 10)
    {
        var pts = new List<(double, double)>(n + 1);
        for (int i = 0; i <= n; i++)
        {
            double t = (double)i / n, u = 1 - t;
            pts.Add((u * u * u * a.X + 3 * u * u * t * b.X + 3 * u * t * t * c.X + t * t * t * d.X,
                u * u * u * a.Y + 3 * u * u * t * b.Y + 3 * u * t * t * c.Y + t * t * t * d.Y));
        }
        return pts;
    }
}

/// <summary>The mockup's seeded numbers (mulberry32): the same look from the same seed on every run, a look's own.</summary>
public sealed class Mulberry(uint seed)
{
    private uint _a = seed;

    /// <summary>The next number in [0, 1).</summary>
    public double Next()
    {
        unchecked
        {
            _a += 0x6d2b79f5;
            uint t = _a;
            t = (t ^ (t >> 15)) * (1 | t);
            t ^= t + (t ^ (t >> 7)) * (61 | t);
            return (t ^ (t >> 14)) / 4294967296.0;
        }
    }

    /// <summary>A whole number below <paramref name="n"/>.</summary>
    public int Int(int n) => (int)(Next() * n);

    public bool Chance(double p) => Next() < p;
}
