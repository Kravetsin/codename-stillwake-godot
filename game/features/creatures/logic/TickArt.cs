using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// The tick («Клещ», candidate 3 of the sentry page): its sprite drawn anew each frame from its pose, the page's <c>drawTick</c>
/// and its pixel buffer ported as they are. A plated abdomen with a red mark and seams, a small head, a nozzle at its back, six
/// hooked legs bent by two-bone IK (the near three over the body, the far three behind it), the eye lamp three pixels and a
/// pupil. Pixels of the world, <see cref="Width"/> × <see cref="Height"/>, its origin the middle of its feet at
/// (<see cref="OriginX"/>, <see cref="OriginY"/>), facing right, y down. Plain C#, tested.
/// </summary>
public sealed class TickArt
{
    public const int Width = 44, Height = 34, OriginX = 22, OriginY = 27;

    /// <summary>A colour, 0..255 a channel.</summary>
    public readonly record struct Rgb(double R, double G, double B)
    {
        public static Rgb Mix(Rgb a, Rgb b, double t) => new(a.R + (b.R - a.R) * t, a.G + (b.G - a.G) * t, a.B + (b.B - a.B) * t);
    }

    /// <summary>The page's palettes, outline to highlight: the body's dark steel, its mark's crimson.</summary>
    public static readonly Rgb[] Dark = [new(10, 10, 14), new(40, 40, 54), new(64, 66, 86), new(98, 102, 124), new(146, 152, 174), new(202, 206, 222)];
    public static readonly Rgb[] Crimson = [new(28, 9, 11), new(92, 22, 26), new(156, 42, 38), new(198, 78, 62), new(224, 138, 118), new(244, 198, 188)];
    public static readonly Rgb Outline = new(10, 11, 16), Black = new(0, 0, 0), White = new(255, 255, 255);

    /// <summary>
    /// What it is doing, the page's <c>P</c>: crouched (1, −0.5 a hop up), walking (0..1) and how far its legs have gone, its legs
    /// folded for flight (0..1), its eye's colour and brightness (lit at 1 and over), where it looks across (−1..1, its own way),
    /// how far its death's cracks have spread (0..1, none at 0).
    /// </summary>
    public readonly record struct Pose(double Crouch, double Walk, double Gait, double Fly, Rgb Eye, double EyeAmp, double Look, double Cracks = 0);

    /// <summary>
    /// Where its parts are this frame, in its own pixels (origin the middle of its feet): the eye, its nozzle, its near feet, the
    /// claw that cuts, the points its wind-up's arcs jump between (knees, then near feet), the middle of its body.
    /// </summary>
    public sealed record Anchors((double X, double Y) Eye, (double X, double Y) Nozzle, IReadOnlyList<(double X, double Y)> Feet,
        (double X, double Y) Cut, IReadOnlyList<(double X, double Y)> Arcs, (double X, double Y) Centre);

    private const int N = Width * Height;
    private readonly byte[] _mat = new byte[N];
    private readonly uint[] _det = new uint[N], _far = new uint[N], _near = new uint[N], _emi = new uint[N];

    /// <summary>Its picture, lit by the world: RGBA, a byte a channel, row by row.</summary>
    public byte[] Albedo { get; } = new byte[N * 4];

    /// <summary>What glows of it, the eye and the cracks, drawn over unlit: RGBA.</summary>
    public byte[] Emission { get; } = new byte[N * 4];

    /// <summary>Its silhouette in white, for a blow's flash: RGBA.</summary>
    public byte[] Silhouette { get; } = new byte[N * 4];

    /// <summary>Draws it in <paramref name="p"/> into <see cref="Albedo"/>, <see cref="Emission"/> and <see cref="Silhouette"/>; answers where its parts are.</summary>
    public Anchors Draw(Pose p)
    {
        Array.Clear(_mat);
        Array.Clear(_det);
        Array.Clear(_far);
        Array.Clear(_near);
        Array.Clear(_emi);
        var anchors = Tick(p);
        if (p.Cracks > 0) Cracks(p.Cracks, anchors.Centre);
        Compose();
        return anchors;
    }

    private Anchors Tick(Pose p)
    {
        var pal = Dark;
        int bob = p.Walk > 0 ? Round(Math.Max(0, Math.Sin(p.Gait * 2)) * 0.7 * p.Walk) : 0;
        int by = -Round(4 - p.Crouch * 2.5 + p.Fly * 0.5) - bob, top = by - 7;
        (int Hx, int Hy, int B, double Ph, bool Near)[] legs =
        [
            (-7, by - 1, -12, 0, true), (-3, by - 1, -4, Math.PI, true), (1, by - 1, 9, 0, true),
            (-6, by - 2, -10, Math.PI, false), (-2, by - 2, -1, 0, false), (2, by - 2, 6, Math.PI, false),
        ];
        int[] order = [3, 4, 5, 0, 1, 2];
        var feet = new List<(double, double)>(3);
        var knees = new List<(double, double)>(9);
        foreach (int k in order)
        {
            var l = legs[k];
            var (fx, fy) = StepFoot(l.B, p.Gait * 1.3 + l.Ph, p.Walk, 1.5, 2);
            int folded = Math.Sign(l.B - l.Hx);
            int x = Round(Lerp(fx, l.Hx + folded * 2, p.Fly)), y = Round(Lerp(fy, l.Hy + 2, p.Fly));
            var layer = l.Near ? _near : _far;
            int s = Math.Sign(x - l.Hx);
            if (s == 0) s = 1;
            knees.Add(Leg(layer, (l.Hx, l.Hy), (x, y), 5, l.Near ? pal[4] : pal[2], l.Near ? pal[3] : pal[1]));
            Px(layer, x - s, y, l.Near ? pal[3] : pal[1]); // the hooked claw
            if (l.Near) feet.Add((x, y));
        }
        Rows(top, [(-7, -1), (-9, 1), (-10, 2), (-11, 2), (-11, 2), (-10, 2), (-9, 1), (-7, -1)], 1);
        Span(top + 1, -6, -3, 2);
        Span(top + 2, -5, -4, 2);
        foreach (int x in (int[])[-8, -4, 0])
            for (int y = top + 3; y <= top + 6; y++) DetIn(x, y, pal[2]);
        Rows(top + 3, [(2, 5), (2, 7), (2, 8), (2, 8), (2, 6)], 3);
        Rows(top + 3, [(-13, -12), (-13, -12)], 3);
        Lamp([(6, top + 4), (7, top + 4), (7, top + 5)], (6 + (LookX(p.Look) > 0 ? 1 : 0), top + 4), p);
        Line(_near, 8, top + 6, 9, top + 8, pal[3]);
        var arcs = new List<(double, double)>(knees);
        arcs.AddRange(feet);
        return new Anchors((7, top + 4.5), (-13, top + 4), feet, (10, -1), arcs, (-3, top + 4));
    }

    /// <summary>The page's cracks: four glowing paths wandering out from its middle, longer as <paramref name="u"/> grows, on the body only.</summary>
    private void Cracks(double u, (double X, double Y) centre)
    {
        static double Rnd(double s)
        {
            double x = Math.Sin(s * 127.1) * 43758.5;
            return x - Math.Floor(x);
        }
        var colour = Rgb.Mix(new Rgb(255, 120, 40), new Rgb(255, 240, 200), u);
        for (int k = 0; k < 4; k++)
        {
            int x = (int)centre.X + Round((Rnd(k) - 0.5) * 8), y = (int)centre.Y + Round((Rnd(k + 9) - 0.5) * 4);
            int n = Round(u * 7);
            for (int i = 0; i < n; i++)
            {
                int at = Index(x, y);
                if (at >= 0 && _mat[at] != 0) Emit(x, y, colour, 0.5 + 0.5 * u);
                x += Rnd(k * 31 + i) < 0.5 ? 1 : -1;
                y += Rnd(k * 17 + i * 3) < 0.4 ? (Rnd(i + k) < 0.5 ? 1 : -1) : 0;
            }
        }
    }

    private void Lamp((int X, int Y)[] points, (int X, int Y) pupil, Pose p)
    {
        double a = Math.Clamp(p.EyeAmp, 0, 1);
        foreach (var (x, y) in points) Emit(x, y, Rgb.Mix(p.Eye, Black, 0.3), a);
        Emit(pupil.X, pupil.Y, Rgb.Mix(p.Eye, White, 0.55), a);
    }

    private static int LookX(double look) => Round(Math.Clamp(look, -1, 1));

    /// <summary>The page's shading: each body pixel's tone by which of its sides are open, its outline round it, then seams and legs over it.</summary>
    private void Compose()
    {
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                int i = y * Width + x;
                uint c = _far[i];
                byte m = _mat[i];
                if (m != 0)
                {
                    if (m == 4) c = Pack(new Rgb(7, 8, 12));
                    else
                    {
                        var ramp = m == 2 ? Crimson : Dark;
                        byte up = y > 0 ? _mat[i - Width] : (byte)0, down = y < Height - 1 ? _mat[i + Width] : (byte)0;
                        byte left = x > 0 ? _mat[i - 1] : (byte)0, right = x < Width - 1 ? _mat[i + 1] : (byte)0;
                        int k;
                        if (up == 0) k = left != 0 ? 4 : 5;
                        else if (down == 0) k = 1;
                        else if (left == 0) k = 3;
                        else if (right == 0) k = 2;
                        else
                        {
                            int d = 0;
                            for (int j = i - Width; j >= 0 && _mat[j] != 0 && d < 4; j -= Width) d++;
                            k = d <= 2 ? 3 : 2;
                        }
                        if (m == 3) k = Math.Max(1, k - 1);
                        c = Pack(ramp[k]);
                    }
                }
                else if (x > 0 && _mat[i - 1] != 0 || x < Width - 1 && _mat[i + 1] != 0 || y > 0 && _mat[i - Width] != 0 || y < Height - 1 && _mat[i + Width] != 0)
                    c = Pack(Outline);
                if (_det[i] != 0) c = _det[i];
                if (_near[i] != 0) c = _near[i];
                Put(Albedo, i, c);
                Put(Emission, i, _emi[i]);
                Put(Silhouette, i, c != 0 ? 0xFFFFFFFFu : 0);
            }
    }

    private static void Put(byte[] into, int i, uint c)
    {
        into[i * 4] = (byte)(c & 255);
        into[i * 4 + 1] = (byte)((c >> 8) & 255);
        into[i * 4 + 2] = (byte)((c >> 16) & 255);
        into[i * 4 + 3] = (byte)(c >> 24);
    }

    /// <summary>A colour packed as the page packs it: alpha high, red low; 0 is nothing.</summary>
    private static uint Pack(Rgb c, double a = 1) =>
        (uint)Round(Math.Clamp(a, 0, 1) * 255) << 24 | (uint)Math.Clamp(Round(c.B), 0, 255) << 16 | (uint)Math.Clamp(Round(c.G), 0, 255) << 8
        | (uint)Math.Clamp(Round(c.R), 0, 255);

    /// <summary>JavaScript's <c>Math.round</c>: halves up, as the page rounds.</summary>
    private static int Round(double v) => (int)Math.Floor(v + 0.5);

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;

    private static int Index(double x, double y)
    {
        int ix = Round(x) + OriginX, iy = Round(y) + OriginY;
        return ix < 0 || iy < 0 || ix >= Width || iy >= Height ? -1 : iy * Width + ix;
    }

    private void Mat(int x, int y, byte m)
    {
        int i = Index(x, y);
        if (i >= 0) _mat[i] = m;
    }

    private void Span(int y, int x0, int x1, byte m)
    {
        for (int x = x0; x <= x1; x++) Mat(x, y, m);
    }

    private void Rows(int top, (int From, int To)[] rows, byte m)
    {
        for (int k = 0; k < rows.Length; k++) Span(top + k, rows[k].From, rows[k].To, m);
    }

    private void DetIn(int x, int y, Rgb c)
    {
        int i = Index(x, y);
        if (i >= 0 && _mat[i] != 0) _det[i] = Pack(c);
    }

    private static void Px(uint[] layer, int x, int y, Rgb c)
    {
        int i = Index(x, y);
        if (i >= 0) layer[i] = Pack(c);
    }

    private void Emit(double x, double y, Rgb c, double a)
    {
        int i = Index(x, y);
        if (i >= 0 && a > 0.01) _emi[i] = Pack(c, a);
    }

    /// <summary>The page's line: Bresenham's from one rounded end to the other, eighty pixels at most.</summary>
    private static void Line(uint[] layer, double ax, double ay, double bx, double by, Rgb c)
    {
        int x0 = Round(ax), y0 = Round(ay), x1 = Round(bx), y1 = Round(by);
        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1, dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1, e = dx + dy;
        for (int n = 0; n < 80; n++)
        {
            Px(layer, x0, y0, c);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * e;
            if (e2 >= dy)
            {
                e += dy;
                x0 += sx;
            }
            if (e2 <= dx)
            {
                e += dx;
                y0 += sy;
            }
        }
    }

    /// <summary>A leg from its hip to its foot, its knee bent up by two-bone IK with both bones <paramref name="bone"/> long; answers the knee.</summary>
    private static (double, double) Leg(uint[] layer, (double X, double Y) hip, (double X, double Y) foot, double bone, Rgb upper, Rgb lower)
    {
        var knee = Knee(hip, foot, bone, bone);
        Line(layer, hip.X, hip.Y, knee.X, knee.Y, upper);
        Line(layer, knee.X, knee.Y, foot.X, foot.Y, lower);
        return knee;
    }

    /// <summary>The page's <c>ik</c>, its knee bent upward.</summary>
    public static (double X, double Y) Knee((double X, double Y) hip, (double X, double Y) foot, double upper, double lower)
    {
        double dx = foot.X - hip.X, dy = foot.Y - hip.Y, dd = Math.Sqrt(dx * dx + dy * dy);
        if (dd == 0) dd = 1e-3;
        double d = Math.Min(dd, upper + lower - 0.01);
        double a = (upper * upper - lower * lower + d * d) / (2 * d), h = Math.Sqrt(Math.Max(0, upper * upper - a * a));
        double ux = dx / dd, uy = dy / dd, px = -uy, py = ux;
        if (py > 0) (px, py) = (-px, -py);
        return (hip.X + ux * a + px * h, hip.Y + uy * a + py * h);
    }

    /// <summary>The page's <c>stepFoot</c>: a foot <paramref name="phase"/> through its step, lifted only on its way forward.</summary>
    public static (double X, double Y) StepFoot(double rest, double phase, double walk, double stride, double lift)
    {
        double s = Math.Sin(phase), c = Math.Cos(phase);
        return (rest - c * stride * walk, s > 0 ? -Round(s * lift * walk) : 0);
    }
}
