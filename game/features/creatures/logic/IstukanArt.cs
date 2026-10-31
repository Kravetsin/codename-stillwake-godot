using System;
using System.Collections.Generic;

using static Stillwake.Game.Features.Creatures.TickArt;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// The Istukan («Истукан», candidate 4 of the guard page), the spire's guard: its sprite drawn anew from its pose, the page's
/// <c>Sprite</c> and <c>ISTUKAN.paint</c> ported as they are. Parts painted as masks, each shaded and outlined as it is flushed, so
/// overlapping plates read as plates; its key poses mixed by what it is doing. Pixels of the world, <see cref="Width"/> ×
/// <see cref="Height"/>, its origin the middle of its feet at (<see cref="OriginX"/>, <see cref="OriginY"/>), facing right, y down.
/// Plain C#, tested.
/// </summary>
public sealed class IstukanArt
{
    public const int Width = 72, Height = 60, OriginX = 36, OriginY = 48;

    /// <summary>The page's palettes, the makers' alloy, outline to highlight, and its shade.</summary>
    private static readonly TickArt.Rgb[] Alloy = [new(20, 29, 36), new(38, 53, 62), new(56, 76, 87), new(78, 103, 114), new(110, 138, 147), new(166, 194, 198)];
    private static readonly TickArt.Rgb[] Shade = [new(14, 20, 26), new(20, 29, 36), new(34, 47, 56), new(48, 66, 76), new(66, 88, 98), new(104, 130, 140)];

    /// <summary>The makers' light: a flight's streak, the core in its chest.</summary>
    public static readonly TickArt.Rgb Arc = new(112, 198, 255), Violet = new(180, 147, 255);

    private static readonly TickArt.Rgb VioletLow = new(117, 82, 224), VioletCore = new(253, 250, 255);
    private static readonly TickArt.Rgb Glass = new(10, 15, 19), Engraved = Alloy[1];

    /// <summary>
    /// What it is doing, the page's <c>P</c>, each 0..1 and mixed in this order: folded as a column, crouched, flown, stuck, dead. Its
    /// lamp's colour, none for dark glass, and brightness; how far its seams' light has risen; how far dimmed it is; a clock that
    /// shivers its arms while it lies stuck; whether it stands on a floor that hides what falls below its feet.
    /// </summary>
    public readonly record struct Pose(double Fold, double Crouch, double Fly, double Stuck, double Dead, TickArt.Rgb? Lamp, double LampAmp, double Seam,
        double Dim = 0, double Clock = 0, bool OnFloor = true);

    /// <summary>Where its parts are this frame, in its own pixels (origin the middle of its feet): its lamp's pupil, the violet core in its chest.</summary>
    public readonly record struct Anchors((double X, double Y) Eye, (double X, double Y) Core);

    /// <summary>One key pose: the body's place and turn, its shoulders, its arm's two joints, its feet, its head off the body.</summary>
    private readonly record struct Keys(double X, double Y, double A, double Sh, double Aa, double Ab, double Lx, double Ly, double Rx, double Ry,
        double Hdx = 0, double Hdy = 0, double Hb = 0)
    {
        public static Keys Mix(Keys k, Keys to, double t) => t <= 0 ? k : new(Lerp(k.X, to.X, t), Lerp(k.Y, to.Y, t), Lerp(k.A, to.A, t), Lerp(k.Sh, to.Sh, t),
            Lerp(k.Aa, to.Aa, t), Lerp(k.Ab, to.Ab, t), Lerp(k.Lx, to.Lx, t), Lerp(k.Ly, to.Ly, t), Lerp(k.Rx, to.Rx, t), Lerp(k.Ry, to.Ry, t),
            Lerp(k.Hdx, to.Hdx, t), Lerp(k.Hdy, to.Hdy, t), Lerp(k.Hb, to.Hb, t));
    }

    private static readonly Keys Stand = new(0, -16, 0.03, 9, 0.15, 0.42, -4, 0, 4, 0);
    private static readonly Keys Sleep = new(0, -12, 0, 6.5, -0.1, 2.75, -3, 0, 3, 0);
    private static readonly Keys Crouched = new(-3, -10, 0.32, 9, -0.9, -0.2, -6, 0, 4, 0);
    private static readonly Keys Flown = new(2, -12, 1.12, 8, 3.05, 3.15, -20, -12, -17, -18);
    private static readonly Keys Lodged = new(-3, -10, 0.6, 9, 1.15, 1.0, -13, 0, -8, 0);
    private static readonly Keys Fallen = new(-6, -5, 1.57, 7, 0.15, 0.1, -20, -1, -16, -1, 1, 3, -1.2);

    private static readonly (int X, int Y)[][] Glyphs =
    [
        [(0, 0), (1, 0), (2, 0), (1, 1), (1, 2)],
        [(0, 0), (0, 1), (0, 2), (1, 2), (2, 1)],
        [(1, 0), (0, 1), (2, 1), (1, 2)],
        [(0, 0), (2, 0), (1, 1), (1, 2), (2, 2)],
        [(0, 0), (1, 0), (0, 1), (2, 2), (1, 2)],
    ];

    private const int N = Width * Height;
    private readonly bool[] _mask = new bool[N], _line = new bool[N];
    private readonly uint[] _paint = new uint[N], _glow = new uint[N];
    private readonly List<double> _crossings = new(8);
    private (double A, double B, double C, double D, double E, double F) _at = (1, 0, 0, 1, 0, 0);
    private (double X, double Y) _eye, _core;

    /// <summary>Its picture, lit by the world: RGBA, a byte a channel, row by row.</summary>
    public byte[] Albedo { get; } = new byte[N * 4];

    /// <summary>What glows of it, its lamp and seams, drawn over unlit: RGBA.</summary>
    public byte[] Emission { get; } = new byte[N * 4];

    /// <summary>Its plates in white, its outline left out, for a blow's flash: RGBA.</summary>
    public byte[] Silhouette { get; } = new byte[N * 4];

    /// <summary>Draws it in <paramref name="p"/> into <see cref="Albedo"/>, <see cref="Emission"/> and <see cref="Silhouette"/>; answers where its parts are.</summary>
    public Anchors Draw(Pose p)
    {
        Array.Clear(_mask);
        Array.Clear(_line);
        Array.Clear(_paint);
        Array.Clear(_glow);
        _at = (1, 0, 0, 1, 0, 0);
        Paint(p);
        Compose(p);
        return new Anchors(_eye, _core);
    }

    /// <summary>The page's <c>mixKeys</c>: each stage mixed over the last by how far it has gone.</summary>
    private static Keys KeysOf(Pose p)
    {
        var k = Keys.Mix(Stand, Sleep, p.Fold);
        k = Keys.Mix(k, Crouched, p.Crouch);
        k = Keys.Mix(k, Flown, p.Fly);
        k = Keys.Mix(k, Lodged, p.Stuck);
        return Keys.Mix(k, Fallen, p.Dead);
    }

    /// <summary>The page's <c>paint</c>: far leg, far arm, near leg, trunk, head, near arm.</summary>
    private void Paint(Pose p)
    {
        var q = KeysOf(p);
        var nearHip = Tilt(-3, 9, q.A);
        var farHip = Tilt(3, 9, q.A);
        Leg(q, farHip, q.Rx, q.Ry, Shade);
        var saved = _at;
        At(q.X, q.Y, q.A);
        Arm(q, q.Sh - 2, Shade, p, near: false);
        _at = saved;
        Leg(q, nearHip, q.Lx, q.Ly, Alloy);
        At(q.X, q.Y, q.A);
        Poly([(-q.Sh, -8), (q.Sh, -8), (q.Sh - 1, -3), (6, 8), (-6, 8), (-q.Sh + 1, -3)]);
        Flush(Alloy);
        Rect(-5, 7, 5, 10);
        Flush(Alloy);
        Rivet(-q.Sh + 1, -7);
        Rivet(q.Sh - 2, -7);
        Glyph(-3, -3, 3, p.Seam, Arc);
        Glyph(1, -3, 1, p.Seam, Arc);
        Glyph(-1, 2, 2, p.Seam, Violet);
        Dot(0, 0, VioletLow);
        GlowAt(0, 0, VioletCore, p.Seam);
        _core = Own(0.5, 0.5);
        _at = saved;
        var head = Tilt(1, -13, q.A);
        At(q.X + head.X + q.Hdx, q.Y + head.Y + q.Hdy, q.A + q.Hb);
        Poly([(-5, -4), (-3, -5), (4, -5), (6, -3), (6, 5), (-5, 5)]);
        Flush(Alloy);
        Rect(-2, -7, 3, -4);
        Flush(Alloy);
        for (int i = -3; i <= 5; i++) Dot(i, -1, Engraved);
        Lamp([(1, -1), (3, -1), (4, -1)], p);
        Seam(-3, 2, 4, 2, p.Seam, Violet);
        _at = saved;
        At(q.X, q.Y, q.A);
        Arm(q, -q.Sh + 2, Alloy, p, near: true);
        _at = saved;
    }

    private void Leg(Keys q, (double X, double Y) hip, double fx, double fy, TickArt.Rgb[] ramp)
    {
        Bar(q.X + hip.X, q.Y + hip.Y, fx, fy, 5.5, 5);
        Flush(ramp);
        Poly([(fx - 3, fy - 2), (fx + 4, fy - 2), (fx + 5, fy), (fx - 3, fy)]);
        Flush(ramp);
    }

    private void Arm(Keys q, double shoulder, TickArt.Rgb[] ramp, Pose p, bool near)
    {
        double wig = p.Stuck * (1 - p.Dead) * Math.Sin(p.Clock * 30 + (near ? 0 : 2)) * 0.04;
        var saved = _at;
        At(shoulder, -5, 0);
        double ex = Math.Sin(q.Aa + wig) * 7.5, ey = Math.Cos(q.Aa + wig) * 7.5, fx = ex + Math.Sin(q.Ab + wig) * 9, fy = ey + Math.Cos(q.Ab + wig) * 9;
        Bar(0, 0, ex, ey, 4.5, 4);
        Flush(ramp);
        Bar(ex, ey, fx, fy, 5, 6);
        Flush(ramp);
        var elbow = _at;
        At(fx, fy, -q.Ab);
        Rect(-3.5, -1, 3.5, 4.5);
        Flush(ramp);
        if (near)
        {
            Rivet(-2, 1);
            Rivet(1, 1);
        }
        _at = elbow;
        if (near)
        {
            Seam(ex + (fx - ex) * 0.2, ey + (fy - ey) * 0.2, ex + (fx - ex) * 0.8, ey + (fy - ey) * 0.8, p.Seam, Arc);
            Ell(0, 0, 3, 3, 10);
            Flush(ramp);
            Rivet(0, 0);
        }
        _at = saved;
    }

    /// <summary>The state lamp: dark glass asleep, then its colour; the last point its bright pupil.</summary>
    private void Lamp((int X, int Y)[] points, Pose p)
    {
        for (int k = 0; k < points.Length; k++)
        {
            var (x, y) = points[k];
            bool pupil = k == points.Length - 1;
            if (p.Lamp is not { } lit)
            {
                Dot(x, y, pupil ? Alloy[1] : Glass);
                continue;
            }
            Dot(x, y, Glass);
            GlowAt(x, y, pupil ? TickArt.Rgb.Mix(lit, TickArt.White, 0.55) : TickArt.Rgb.Mix(lit, TickArt.Black, 0.15), p.LampAmp);
        }
        var (ex, ey) = points[^1];
        _eye = Own(ex + 0.5, ey + 0.5);
    }

    /// <summary>An engraved line: dark when dead, its light rising through it with the power.</summary>
    private void Seam(double x0, double y0, double x1, double y1, double g, TickArt.Rgb colour)
    {
        int n = Math.Max(1, Round(Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0))));
        for (int k = 0; k <= n; k++)
        {
            int x = Round(Lerp(x0, x1, (double)k / n)), y = Round(Lerp(y0, y1, (double)k / n));
            Dot(x, y, Engraved);
            if (g > 0.12) GlowAt(x, y, colour, g * 0.9);
        }
    }

    private void Glyph(int x, int y, int n, double g, TickArt.Rgb colour)
    {
        foreach (var (i, j) in Glyphs[n % Glyphs.Length])
        {
            Dot(x + i, y + j, Engraved);
            if (g > 0.12) GlowAt(x + i, y + j, colour, g * 0.85);
        }
    }

    private void Rivet(double x, double y)
    {
        var (px, py) = Buffer(x + 0.5, y + 0.5);
        Set(px, py, Alloy[5]);
        Set(px + 1, py + 1, Alloy[1]);
    }

    private void Dot(double x, double y, TickArt.Rgb c)
    {
        var (px, py) = Buffer(x + 0.5, y + 0.5);
        Set(px, py, c);
    }

    private void GlowAt(double x, double y, TickArt.Rgb c, double a)
    {
        var (px, py) = Buffer(x + 0.5, y + 0.5);
        int i = IndexOf(px, py);
        if (i >= 0 && _paint[i] != 0 && a > 0.02) _glow[i] = Pack(c, Math.Min(1, a));
    }

    /// <summary>The page's <c>_set</c>: paints only where a part is already, and takes back any glow there.</summary>
    private void Set(double px, double py, TickArt.Rgb c)
    {
        int i = IndexOf(px, py);
        if (i < 0 || _paint[i] == 0) return;
        _paint[i] = Pack(c);
        _glow[i] = 0;
        _line[i] = false;
    }

    private static int IndexOf(double px, double py)
    {
        int x = Round(px - 0.5 + 0.0001), y = Round(py - 0.5 + 0.0001);
        return x < 0 || y < 0 || x >= Width || y >= Height ? -1 : y * Width + x;
    }

    /// <summary>A point of the current transform in the buffer.</summary>
    private (double X, double Y) Buffer(double x, double y) =>
        (_at.A * x + _at.C * y + _at.E + OriginX, _at.B * x + _at.D * y + _at.F + OriginY);

    /// <summary>A point of the current transform in its own pixels.</summary>
    private (double X, double Y) Own(double x, double y) => (_at.A * x + _at.C * y + _at.E, _at.B * x + _at.D * y + _at.F);

    /// <summary>Moved to (<paramref name="x"/>, <paramref name="y"/>) and turned by <paramref name="a"/>, within the current transform.</summary>
    private void At(double x, double y, double a)
    {
        var o = _at;
        double c = Math.Cos(a), s = Math.Sin(a);
        _at = (o.A * c + o.C * s, o.B * c + o.D * s, -o.A * s + o.C * c, -o.B * s + o.D * c, o.A * x + o.C * y + o.E, o.B * x + o.D * y + o.F);
    }

    private void Poly((double X, double Y)[] points)
    {
        double top = double.MaxValue, bottom = double.MinValue;
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = Buffer(points[i].X, points[i].Y);
            top = Math.Min(top, points[i].Y);
            bottom = Math.Max(bottom, points[i].Y);
        }
        for (int y = Math.Max(0, (int)Math.Floor(top)); y <= Math.Min(Height - 1, (int)Math.Ceiling(bottom)); y++)
        {
            _crossings.Clear();
            double row = y + 0.5;
            for (int i = 0; i < points.Length; i++)
            {
                var (a, b) = (points[i], points[(i + 1) % points.Length]);
                if (a.Y <= row != b.Y <= row) _crossings.Add(a.X + (row - a.Y) / (b.Y - a.Y) * (b.X - a.X));
            }
            _crossings.Sort();
            for (int k = 0; k + 1 < _crossings.Count; k += 2)
                for (int x = Math.Max(0, (int)Math.Ceiling(_crossings[k] - 0.5)); x <= Math.Min(Width - 1, (int)Math.Floor(_crossings[k + 1] - 0.5)); x++)
                    _mask[y * Width + x] = true;
        }
    }

    private void Ell(double cx, double cy, double rx, double ry, int n)
    {
        var points = new (double, double)[n];
        for (int i = 0; i < n; i++) points[i] = (cx + Math.Cos(i * Math.Tau / n) * rx, cy + Math.Sin(i * Math.Tau / n) * ry);
        Poly(points);
    }

    /// <summary>A tapering bar between two points, its ends square, so it stays a plate.</summary>
    private void Bar(double x0, double y0, double x1, double y1, double w0, double w1)
    {
        double dx = x1 - x0, dy = y1 - y0, d = Math.Sqrt(dx * dx + dy * dy);
        if (d == 0) d = 1e-3;
        double nx = -dy / d, ny = dx / d;
        Poly([(x0 + nx * w0 / 2, y0 + ny * w0 / 2), (x1 + nx * w1 / 2, y1 + ny * w1 / 2), (x1 - nx * w1 / 2, y1 - ny * w1 / 2), (x0 - nx * w0 / 2, y0 - ny * w0 / 2)]);
    }

    private void Rect(double x0, double y0, double x1, double y1) => Poly([(x0, y0), (x1, y0), (x1, y1), (x0, y1)]);

    /// <summary>The page's <c>flush</c>: the part masked so far shaded over what lies under it, its outline round it, the mask emptied.</summary>
    private void Flush(TickArt.Rgb[] ramp)
    {
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                int i = y * Width + x;
                bool up = y > 0 && _mask[i - Width], down = y < Height - 1 && _mask[i + Width], left = x > 0 && _mask[i - 1], right = x < Width - 1 && _mask[i + 1];
                if (_mask[i])
                {
                    _paint[i] = Pack(ramp[Tone(i, up, down, left, right)]);
                    (_glow[i], _line[i]) = (0, false);
                }
                else if (up || down || left || right)
                {
                    _paint[i] = Pack(Alloy[0]);
                    (_glow[i], _line[i]) = (0, true);
                }
            }
        Array.Clear(_mask);
    }

    private int Tone(int i, bool up, bool down, bool left, bool right)
    {
        if (!up) return left ? 4 : 5;
        if (!down) return 1;
        if (!right) return 2;
        if (!left) return 4;
        int t = 0;
        for (int j = i - Width; j >= 0 && _mask[j] && t < 5; j -= Width) t++;
        int k = t <= 2 ? 4 : 3;
        return i + Width * 2 < N && !_mask[i + Width * 2] ? 2 : k;
    }

    private void Compose(Pose p)
    {
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                int i = y * Width + x;
                bool hidden = p.OnFloor && y >= OriginY;
                uint c = hidden ? 0 : _paint[i];
                if (c != 0 && p.Dim > 0) c = Dimmed(c, p.Dim);
                Put(Albedo, i, c);
                Put(Emission, i, hidden ? 0 : _glow[i]);
                Put(Silhouette, i, c != 0 && !_line[i] ? 0xFFFFFFFFu : 0);
            }
    }

    private static uint Dimmed(uint c, double dim) => Pack(TickArt.Rgb.Mix(new TickArt.Rgb(c & 255, (c >> 8) & 255, (c >> 16) & 255), Alloy[0], dim));

    private static (double X, double Y) Tilt(double x, double y, double a) => (x * Math.Cos(a) - y * Math.Sin(a), x * Math.Sin(a) + y * Math.Cos(a));
}
