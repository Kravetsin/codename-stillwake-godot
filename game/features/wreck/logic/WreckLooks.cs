using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Stillwake.Content;
using Stillwake.Sim;
using P = System.Collections.Generic.List<(double X, double Y)>;

namespace Stillwake.Game.Features.Wrecked;

/// <summary>Where a look lies: over the rooms' open cells, kept to them (soot, rime, smoke), or over the hull and what stands (frost, petals, cables).</summary>
public enum WreckLayer { Rooms, Over }

/// <summary>
/// One look of a wreck (vessel-wrecks W-2): its name, layer and box in cells, and how it paints itself on a chunk. Each paints again
/// the same from its own seed, so a chunk whose cells changed is painted anew.
/// </summary>
public sealed record WreckMark(string Look, WreckLayer Layer, double X0, double Y0, double X1, double Y1, Action<WreckPaint> Draw)
{
    public bool Meets(int x0, int y0, int x1, int y1) => X0 < x1 && X1 > x0 && Y0 < y1 && Y1 > y0;
}

/// <summary>
/// The looks a wreck leaves (the mockup's looks.js), each from <see cref="VesselWreck"/> and the built vessel alone, never the damage run
/// again: per event in time order, dulled when its group is old (<see cref="Dull"/>); then the scavengers' pits and patches. Floats and
/// the mockup's own seeded numbers are fine here: these are drawings, the client's own.
/// </summary>
public static class WreckLooks
{
    /// <summary>What an old group's dull looks are laid at, against a fresh one's (the mockup's drawLooks).</summary>
    public const float Dull = 0.55f;

    /// <summary>The looks dulled when old; the others have old colours of their own.</summary>
    public static readonly IReadOnlySet<string> Dulled = new HashSet<string> { "soot", "streaks", "heat", "scorch", "burnRoom", "plume", "beads" };

    private static readonly Regex Wet = new("LifeTank|salvageTanks|Pump|Valves|Manifold|Riser|vesselTank|Stove|Bar");
    private static readonly Regex Soft = new("Chair|Lounger|RoundTable|vesselTable|Vending|Console|vesselBed|vesselBunk|Counter");

    private static readonly Rgba Ink = Rgba.Of(0x101216);

    /// <summary>
    /// Every look of <paramref name="scene"/>'s wreck, in drawing order; <paramref name="hull"/> its livery's hull colour (faded when
    /// scavenged), <paramref name="picture"/> a machine's picture and its reach and rise in cells, null where there is none.
    /// </summary>
    /// <summary>A hanging cable's or pipe's lower end, by its event: where W-3's sparks and frozen spray come from.</summary>
    public sealed record End(int Event, double X, double Y, bool Pipe, bool Stern);

    /// <param name="ends">Filled, when given, with every cable's and pipe's end the looks hang.</param>
    public static List<WreckMark> Of(WreckScene scene, uint hull, Func<string, (WreckPicture Picture, int Reach, int Rise)?> picture, List<End>? ends = null)
    {
        var marks = new List<WreckMark>();
        var w = scene.Wreck;
        bool big = scene.Built.Vessel.Class is "liner" or "tanker";
        uint seed = scene.Built.Vessel.Seed;
        for (int k = 0; k < w.Events.Count; k++)
        {
            var e = w.Events[k];
            var r = new Mulberry(seed * 2654435761u + (uint)k * 40503u + 17u);
            var ctx = new Ctx(scene, marks, e, r, hull, picture, big, k == 0) { Index = k, Ends = ends };
            switch (e.Kind)
            {
                case WreckKind.Explosion: Blast(ctx); break;
                case WreckKind.Fire: Fire(ctx); break;
                case WreckKind.Icing: Icing(ctx); break;
                case WreckKind.Asteroid: Rock(ctx); break;
                default: Break(ctx); break;
            }
            Cables(ctx);
        }
        if (w.Scavenged) Scavenged(scene, marks, hull, new Mulberry(seed ^ 0x5ca7e9u));
        return marks;
    }

    /// <summary>What an event's looks are made from: its scene, record and own numbers, the cable spots it leaves.</summary>
    private sealed class Ctx(WreckScene scene, List<WreckMark> marks, WreckEvent e, Mulberry r, uint hull,
        Func<string, (WreckPicture Picture, int Reach, int Rise)?> picture, bool big, bool first)
    {
        public readonly WreckScene S = scene;
        public readonly List<WreckMark> Marks = marks;
        public readonly WreckEvent E = e;
        public readonly Mulberry R = r;
        public readonly uint Hull = hull;
        public readonly Func<string, (WreckPicture Picture, int Reach, int Rise)?> Picture = picture;
        public readonly bool Big = big, First = first;
        public readonly List<Cable> Spots = [];
        public int Index;
        public List<End>? Ends;
        public bool Old => E.Old;

        public uint Seed() => (uint)R.Int(1_000_000_000);

        /// <summary>A look added, dulled when old if it is a dull one.</summary>
        public void Add(string look, WreckLayer layer, double x0, double y0, double x1, double y1, Action<WreckPaint> draw)
        {
            float alpha = Old && Dulled.Contains(look) ? Dull : 1;
            Marks.Add(new(look, layer, x0, y0, x1, y1, p =>
            {
                (p.Alpha, p.Clip) = (alpha, null);
                draw(p);
                (p.Alpha, p.Clip) = (1, null);
            }));
        }

        /// <summary>The frame cells the event carved, as cells.</summary>
        public IEnumerable<(int X, int Y)> Carved => E.Carved.Select(i => (i % S.Width, i / S.Width));
    }

    /// <summary>A cable or pipe hanging from where it tore: its top, how far it hangs and swings; null for the event's own draw.</summary>
    private sealed record Cable(double X, double Y, double? Len = null, double? Sway = null, bool Pipe = false, bool Stern = true);

    private static double Hyp(double x, double y) => Math.Sqrt(x * x + y * y);

    // ---------- explosion ----------

    private static void Blast(Ctx c)
    {
        var (s, e, r) = (c.S, c.E, c.R);
        double cx = e.X + 0.5, cy = e.Y + 0.5, R = e.Radius;
        bool stern = e.DirX == -1;
        Petals(c, stern ? (x, y) => (-1, Math.Sign(y - cy) * 0.4) : (x, y) => Unit(x - cx, y - cy), stern ? 0.5 : 0.6);
        foreach (var (x, y) in c.Carved)
            if (InRib(s, x, y) && r.Chance(0.35))
            {
                var (ax, ay) = Unit(x - cx, y - cy);
                RibStub(c, x + 0.5, y + 0.5, -ax, -ay, 1.2 + r.Next() * 1.5, ax * 0.8, ay * 0.8);
            }
        var beads = new List<(double X, double Y, double S, bool Hang)>();
        foreach (var (x, y) in c.Carved)
        {
            if (s.At(x, y) != WreckScene.Space) continue;
            if (s.Walled(x, y - 1) && r.Chance(0.35)) beads.Add((x + 0.2 + r.Next() * 0.6, y + 0.05, 0.12 + r.Next() * 0.16, true));
            if (s.Walled(x, y + 1) && r.Chance(0.25)) beads.Add((x + r.Next(), y + 0.85, 0.1 + r.Next() * 0.14, false));
        }
        if (beads.Count > 0)
            c.Add("beads", WreckLayer.Over, beads.Min(b => b.X) - 1, beads.Min(b => b.Y) - 1, beads.Max(b => b.X) + 1, beads.Max(b => b.Y) + 1, p => Beads(p, beads));
        c.Add("heat", WreckLayer.Over, cx - R * 2.6, cy - R * 2.6, cx + R * 2.6, cy + R * 2.6, p => Heat(p, s, cx, cy, R * 2.3));
        uint scorch = c.Seed();
        c.Add("scorch", WreckLayer.Over, cx - R * 2, cy - R * 2, cx + R * 2, cy + R * 2, p => Scorch(p, s, cx, cy, R * 1.8, scorch));
        Streaks(c, cx, cy, R);
        uint soot = c.Seed();
        c.Add("soot", WreckLayer.Rooms, cx - R * 2.5, cy - R * 2.5, cx + R * 2.5, cy + R * 2.5, p => Soot(p, s.Vented, cx, cy, R * 2.2, 0.9, soot));
        c.Spots.Add(new(cx - R * 0.9, cy - R * 0.6));
        c.Spots.Add(new(cx + R * 0.8, cy - R * 0.7));
    }

    private static (double X, double Y) Unit(double x, double y)
    {
        double l = Hyp(x, y);
        return l < 1e-9 ? (0, 0) : (x / l, y / l);
    }

    private static bool InRib(WreckScene s, int x, int y) =>
        s.Built.Vessel.Sections.Any(k => k.Kind == Vessel.SectionKind.Rib && x >= k.Left && x < k.Left + k.Width && y >= k.Top && y <= k.Bottom);

    /// <summary>Soot dragged toward the breaches near the blast: streaks in the rooms the air left, each from a cell toward its nearest hole.</summary>
    private static void Streaks(Ctx c, double cx, double cy, double R)
    {
        var (s, r) = (c.S, c.R);
        var near = new List<(int X, int Y)>();
        for (int y = (int)(cy - R * 2.5); y <= cy + R * 2.5; y++)
            for (int x = (int)(cx - R * 2.5); x <= cx + R * 2.5; x++)
                if (s.At(x, y) == WreckScene.Space && (s.Vented(x + 1, y) || s.Vented(x - 1, y) || s.Vented(x, y + 1) || s.Vented(x, y - 1))
                    && Hyp(x - cx, y - cy) < R * 2.5) near.Add((x, y));
        if (near.Count == 0) return;
        var pts = new List<(double X0, double Y0, double X1, double Y1, double W)>();
        for (int k = 0; k < (c.Big ? 160 : 90); k++)
        {
            double x = cx + (r.Next() - 0.5) * R * 6, y = cy + (r.Next() - 0.5) * R * 3;
            if (!s.Vented((int)Math.Floor(x), (int)Math.Floor(y))) continue;
            var best = near[0];
            double bd = double.MaxValue;
            foreach (var h in near)
            {
                double d = Hyp(h.X - x, h.Y - y);
                if (d < bd) (bd, best) = (d, h);
            }
            if (bd > R * 3.5) continue;
            double l = Math.Min(bd, 2 + r.Next() * 6);
            pts.Add((x, y, x + (best.X - x) / bd * l, y + (best.Y - y) / bd * l, 0.15 + r.Next() * 0.3));
        }
        c.Add("streaks", WreckLayer.Rooms, cx - R * 3.5, cy - R * 2, cx + R * 3.5, cy + R * 2, p =>
        {
            p.Clip = s.Vented;
            foreach (var (x0, y0, x1, y1, wd) in pts)
            {
                double dx = x1 - x0, dy = y1 - y0, l = Hyp(dx, dy) is var h && h > 0 ? h : 1, px = -dy / l * wd, py = dx / l * wd;
                p.Fill([(x0 + px, y0 + py), (x1, y1), (x0 - px, y0 - py)], Rgba.Of(12, 9, 7, 0.55));
            }
        });
    }

    // ---------- fire ----------

    private static void Fire(Ctx c)
    {
        var (s, e, r) = (c.S, c.E, c.R);
        var heat = new Dictionary<int, double>();
        for (int k = 0; k < e.Compartments.Count; k++) heat[e.Compartments[k]] = (k < e.Heats.Count ? e.Heats[k] : 100) / 100.0;
        double Heat(int room) => room >= 0 && heat.TryGetValue(room, out double h) ? h : 0;
        int origin = s.RoomAt(e.X, e.Y);
        var passes = new List<(WreckScene.Doorway D, int Dir, double I)>();
        var borders = new List<(WreckScene.Doorway D, int Side, double I)>();
        foreach (var d in s.Doors)
        {
            double a = Heat(s.RoomAt(d.X - 1, d.Y + 1)), b = Heat(s.RoomAt(d.X + 2, d.Y + 1));
            if (d.Open && a > 0 && b > 0) passes.Add((d, b < a ? 1 : -1, Math.Max(a, b)));
            else if (!d.Open && (a > 0.06) != (b > 0.06)) borders.Add((d, a > b ? -1 : 1, Math.Max(a, b)));
        }
        foreach (int room in e.Compartments)
        {
            double I = Heat(room);
            foreach (var box in s.Rooms[room])
            {
                var rect = (X: (double)box.Left, Y: (double)box.Top, W: (double)box.Width, H: (double)box.Height);
                bool Mask(int x, int y) => s.Open(x, y) && x >= box.Left - 2 && x < box.Left + box.Width + 2 && y >= box.Top && y < box.Floor;
                var doors = passes.Where(q => q.D.X == box.Left - 2 || q.D.X == box.Left + box.Width)
                    .Select(q => (X: q.D.X == box.Left - 2 ? (double)box.Left : box.Left + box.Width, Out: (q.D.X == box.Left - 2) == (q.Dir < 0))).ToList();
                double? seat = room == origin && box.Holds(e.X, e.Y) ? e.X + 0.5 : null;
                uint sd = c.Seed();
                bool old = c.Old;
                c.Add("burnRoom", WreckLayer.Rooms, rect.X - 3, rect.Y - 2, rect.X + rect.W + 3, rect.Y + rect.H + 2, p => BurnRoom(p, rect, I, sd, Mask, seat, doors, old));
                if (!c.Old) c.Add("smoke", WreckLayer.Rooms, rect.X - 3, rect.Y, rect.X + rect.W + 3, rect.Y + rect.H, p =>
                {
                    p.Clip = Mask;
                    p.Rect(rect.X - 2, rect.Y, rect.W + 4, rect.H, Rgba.Of(78, 72, 68, 0.12 + 0.12 * I));
                    p.Rect(rect.X - 2, rect.Y, rect.W + 4, rect.H * 0.45, Rgba.Of(60, 56, 54, 0.12 + 0.1 * I));
                });
            }
        }
        foreach (var (d, dir, I) in passes)
        {
            double x = dir > 0 ? d.X + 2 : d.X, y = d.Y, h = d.Rows;
            c.Add("plume", WreckLayer.Rooms, x - 13, y - 1, x + 13, y + h + 1, p =>
            {
                p.Clip = s.Open;
                p.Fill([(x, y + h * 0.5), (x + dir * 2, y - 0.2), (x + dir * (6 + 6 * I), y - 0.2), (x + dir * (5 + 3 * I), y + 1.6), (x + dir * 2, y + h * 0.55)],
                    Rgba.Of(14, 10, 8, 0.35 + 0.3 * I));
            });
            uint sd = c.Seed();
            c.Add("warpDoor", WreckLayer.Over, d.X - 1, d.Y, d.X + 3, d.Y + d.Rows, p => WarpDoor(p, d.X, d.Y, d.Rows, I, sd));
        }
        foreach (var (d, side, I) in borders)
            c.Add("doorScorch", WreckLayer.Over, d.X, d.Y, d.X + 2, d.Y + d.Rows, p =>
            {
                double x = side < 0 ? d.X : d.X + 1;
                p.Rect(x, d.Y, 1, d.Rows, Rgba.Of(9, 7, 5, 0.55 + 0.3 * I));
                p.Rect(side < 0 ? d.X + 0.9 : d.X + 1, d.Y, 0.12, d.Rows, Rgba.Of(160, 100, 45, 0.35));
                p.Stroke([(d.X + 1, d.Y), (d.X + 1, d.Y + d.Rows)], 0.06, Ink);
            });
        foreach (var m in s.Machines)
        {
            double v = Heat(s.RoomAt(m.X + m.W / 2, m.Y + m.H - 1));
            if (v <= 0.1 || m.Kind == Vessels.Lamp) continue;
            var thing = m;
            if (c.Picture(m.Kind) is { } pic)
            {
                var tint = v > 0.6 ? Rgba.Of(0x3b2f27, 0.78f) : Rgba.Of(0x6b5a4c, 0.5f);
                c.Add("charred", WreckLayer.Over, m.X - pic.Reach, m.Y - pic.Rise, m.X + m.W, m.Y + m.H, p =>
                {
                    if (thing.Stands) p.Picture(pic.Picture, thing.X - pic.Reach, thing.Y - pic.Rise, thing.W + pic.Reach, thing.H + pic.Rise, tint);
                });
            }
            if (v > 0.45)
            {
                uint sd = c.Seed();
                c.Add("charMarks", WreckLayer.Over, m.X, m.Y, m.X + m.W, m.Y + m.H, p => { if (thing.Stands) CharMarks(p, thing, sd); });
            }
            if (v > 0.35 && Soft.IsMatch(m.Kind))
            {
                uint sd = c.Seed();
                c.Add("sag", WreckLayer.Over, m.X, m.Y, m.X + m.W, m.Y + m.H + 1, p => { if (thing.Stands) Sag(p, thing, v, sd); });
            }
        }
        for (int y = 0; y < s.Height; y++)
            for (int x = 0; x < s.Width; x++)
            {
                if (s.At(x, y) != WreckScene.Glass) continue;
                if (!N4(x, y).Any(n => s.Open(n.X, n.Y) && Heat(s.RoomAt(n.X, n.Y)) > 0.3)) continue;
                int gx = x, gy = y;
                c.Add("melted", WreckLayer.Over, gx, gy, gx + 1, gy + 1.3, p =>
                {
                    if (s.At(gx, gy) != WreckScene.Glass) return;
                    p.Rect(gx, gy, 1, 1, Rgba.Of(30, 20, 13, 0.62));
                    p.Fill(Shapes.Quad((gx + 0.15, gy + 0.85), (gx + 0.5, gy + 1.25), (gx + 0.85, gy + 0.85)), Rgba.Of(0x5a3c22));
                    p.Stroke([(gx + 0.2, gy + 0.2), (gx + 0.55, gy + 0.5), (gx + 0.45, gy + 0.8)], 0.05, Ink);
                });
            }
    }

    private static IEnumerable<(int X, int Y)> N4(int x, int y) => [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)];

    /// <summary>
    /// A burnt room (the mockup's burnRoom): smoke laid from the ceiling in tongues reaching toward the doors it went through, a V up
    /// the wall from the seat, the floor charred, blistered paint; long since, dusted over. Hard steps of tone.
    /// </summary>
    private static void BurnRoom(WreckPaint p, (double X, double Y, double W, double H) R, double I0, uint seed, Func<int, int, bool> mask, double? seat,
        List<(double X, bool Out)> doors, bool old)
    {
        var r = new Mulberry(seed);
        p.Clip = mask;
        double Tongue(double x)
        {
            double d = 0;
            foreach (var (dx, o) in doors) d += Math.Max(0, 1 - Math.Abs(x - dx) / (R.W * 0.55)) * R.H * (o ? 0.35 : 0.2);
            return d;
        }
        foreach (var (f, a) in (ReadOnlySpan<(double, double)>)[(0.62, 0.2), (0.42, 0.28), (0.22, 0.45)])
        {
            double b = R.H * f * (0.3 + 0.7 * I0);
            var pts = new P { (R.X - 2, R.Y - 1), (R.X + R.W + 2, R.Y - 1) };
            int n = Math.Max(8, (int)Math.Round(R.W * 1.4));
            for (int j = n; j >= 0; j--)
            {
                double x = R.X - 2 + (R.W + 4) * j / n;
                double wob = (Math.Sin(x * 1.3 + seed % 97) * 0.5 + Math.Sin(x * 0.47 + seed % 13)) * 0.7 * f + (r.Next() - 0.5) * 0.6;
                pts.Add((x, R.Y + Math.Min(R.H + 1, Math.Max(0.3, b + Tongue(x) * f + wob))));
            }
            p.Fill(pts, Rgba.Of(10, 8, 6, a * (0.6 + 0.6 * I0)));
        }
        if (seat is double sx)
        {
            double fy = R.Y + R.H;
            foreach (var (w, a) in (ReadOnlySpan<(double, double)>)[(1, 0.42), (0.62, 0.5), (0.28, 0.72)])
                p.Fill([(sx - 0.8 * w - 0.3, fy), (sx - R.H * 0.8 * w - 0.5, R.Y - 0.5), (sx + R.H * 0.8 * w + 0.5, R.Y - 0.5), (sx + 0.8 * w + 0.3, fy)], Rgba.Of(7, 5, 4, a));
        }
        p.Rect(R.X - 2, R.Y + R.H - 0.3 - I0 * 0.5, R.W + 4, 1.2, Rgba.Of(9, 7, 5, 0.3 + 0.4 * I0));
        if (old)
            for (int j = 0; j < R.W * R.H * 0.6; j++) p.Rect(R.X + r.Next() * R.W, R.Y + r.Next() * R.H, 0.12, 0.08, Rgba.Of(120, 114, 106, 0.28));
        int nb = (int)Math.Round(R.W * R.H * 0.45 * (1 - Math.Abs(I0 - 0.55)));
        for (int j = 0; j < nb; j++)
        {
            double x = R.X + r.Next() * R.W, y = R.Y + R.H * (0.3 + r.Next() * 0.6), rad = 0.07 + r.Next() * 0.15;
            p.Ellipse(x, y, rad, rad, Rgba.Of(150, 136, 118, 0.55));
            p.Stroke(Arc(x + 0.02, y + 0.03, rad, 0.2, Math.PI * 1.2), 0.04, Rgba.Of(18, 12, 9, 0.7));
            if (j % 6 == 0) p.Stroke(Arc(x + 0.3, y, 0.22, Math.PI, Math.PI * 1.8), 0.06, Rgba.Of(170, 158, 140, 0.6));
        }
    }

    private static P Arc(double cx, double cy, double rad, double a0, double a1, int n = 6)
    {
        var pts = new P();
        for (int i = 0; i <= n; i++)
        {
            double a = a0 + (a1 - a0) * i / n;
            pts.Add((cx + Math.Cos(a) * rad, cy + Math.Sin(a) * rad));
        }
        return pts;
    }

    private static void WarpDoor(WreckPaint p, int x, int y, int h, double I, uint seed)
    {
        var r = new Mulberry(seed);
        p.Rect(x, y, 2, h, Rgba.Of(14, 10, 8, 0.25 + 0.35 * I));
        foreach (double ex in (ReadOnlySpan<double>)[x + 0.12, x + 1.88])
        {
            var pts = new P();
            for (double t = 0; t <= h; t += 0.25) pts.Add((ex + Math.Sin(t * 2.3 + r.Next() * 0.6) * 0.12 * I, y + t));
            p.Stroke(pts, 0.14, Rgba.Of(176, 112, 52, 0.55));
            p.Stroke(pts, 0.05, Ink);
        }
    }

    private static void Sag(WreckPaint p, WreckScene.Thing m, double I, uint seed)
    {
        var r = new Mulberry(seed);
        var top = new P();
        for (int j = 0; j <= 8; j++) top.Add((m.X + m.W * j / 8.0, m.Y + 0.15 + Math.Sin(j / 8.0 * Math.PI) * 0.35 * I));
        p.Stroke(top, 0.1, Rgba.Of(20, 14, 10, 0.8));
        for (int j = 0, n = 2 + r.Int(3); j < n; j++)
        {
            double x = m.X + 0.3 + r.Next() * (m.W - 0.6), y = m.Y + m.H * (0.45 + r.Next() * 0.5), len = 0.25 + r.Next() * 0.7 * I;
            var drip = Shapes.Quad((x - 0.1, y), (x - 0.12, y + len), (x, y + len + 0.12));
            drip.AddRange(Shapes.Quad((x, y + len + 0.12), (x + 0.12, y + len), (x + 0.1, y)));
            p.Fill(drip, Rgba.Of(0x2a1f17));
            p.Rect(x - 0.03, y + len * 0.4, 0.04, len * 0.4, Rgba.Of(200, 170, 130, 0.4));
        }
    }

    private static void CharMarks(WreckPaint p, WreckScene.Thing m, uint seed)
    {
        var r = new Mulberry(seed);
        for (int j = 0; j < 3 + Math.Round(m.W * m.H * 0.15); j++)
        {
            double x = m.X + r.Next() * m.W, y = m.Y + m.H * (0.2 + r.Next() * 0.8), w = 0.3 + r.Next() * 0.8;
            p.Ellipse(x, y, w, w * 0.45, Rgba.Of(6, 5, 4, 0.55), r.Next());
        }
        for (int j = 0; j < 3; j++)
        {
            double x = m.X + r.Next() * m.W, y = m.Y + r.Next() * m.H;
            p.Stroke([(x, y), (x + (r.Next() - 0.5) * 1.2, y + r.Next() * 0.8)], 0.06, Rgba.Of(10, 8, 6, 0.8));
        }
    }

    // ---------- icing ----------

    private static void Icing(Ctx c)
    {
        var e = c.E;
        if (c.First && e.Radius == 0) PowerLoss(c);
        else Freeze(c, e.X + 0.5, e.Y + 0.5, e.DirX, e.DirY, e.Radius);
    }

    /// <summary>
    /// After a breach (the mockup's freeze): a fan of frozen spray out of it, frost on the plates round it, rime in the compartments its
    /// cold froze, their machines frosted, burst pipes and tanks in them frozen in streams, icicles under their ceilings.
    /// </summary>
    private static void Freeze(Ctx c, double bx, double by, int dirX, int dirY, int reach)
    {
        var (s, r) = (c.S, c.R);
        var (ux, uy) = Unit(dirX * 0.55, dirY == 0 ? -1 : dirY);
        double len = (c.Big ? 24 : 16) * 0.7, fr = reach * 0.42;
        uint fan = c.Seed(), frost = c.Seed();
        bool old = c.Old;
        c.Add("iceFan", WreckLayer.Over, bx - len, by - len, bx + len, by + len, p => IceFan(p, bx, by, ux, uy, len, 1.0, fan, old));
        c.Add("hullFrost", WreckLayer.Over, bx - fr, by - fr, bx + fr, by + fr, p => HullFrost(p, s, bx, by, fr, frost, old));
        var frozen = new HashSet<int>(c.E.Compartments);
        bool Mask(int x, int y) => s.Open(x, y) && frozen.Contains(s.RoomAt(x, y));
        c.Add("hoar", WreckLayer.Rooms, bx - reach, by - reach, bx + reach + 1, by + reach + 1, p => Hoar(p, s, Mask, bx, by, reach, old, even: false));
        Frosted(c, Mask);
        var wet = s.Machines.Where(m => Wet.IsMatch(m.Kind) && Covers(m, Mask)).Take(4).ToList();
        Pipes(c, wet);
        int n = 0;
        for (int k = 0; k < 400 && n < (c.Big ? 8 : 5); k++)
        {
            int x = (int)Math.Floor(bx + (r.Next() - 0.5) * reach * 1.6), y = (int)Math.Floor(by + (r.Next() - 0.3) * reach);
            if (!Mask(x, y) || !s.Walled(x, y - 1)) continue;
            Icicle(c, x + 0.5, y, 0.8 + r.Next() * 1.8);
            n++;
        }
    }

    private static bool Covers(WreckScene.Thing m, Func<int, int, bool> mask)
    {
        for (int y = m.Y; y < m.Y + m.H; y++)
            for (int x = m.X; x < m.X + m.W; x++)
                if (mask(x, y)) return true;
        return false;
    }

    /// <summary>Power lost, the hull whole (the mockup's powerLoss): every room rimed alike, every window frosted, every machine paled, pipes burst.</summary>
    private static void PowerLoss(Ctx c)
    {
        var (s, r) = (c.S, c.R);
        bool old = c.Old;
        c.Add("hoar", WreckLayer.Rooms, -WreckScene.Pad, -WreckScene.Pad, s.Width + WreckScene.Pad, s.Height + WreckScene.Pad,
            p => Hoar(p, s, s.Open, s.Width / 2.0, s.Height / 2.0, 1e5, old, even: true));
        for (int y = 0; y < s.Height; y++)
            for (int x = 0; x < s.Width; x++)
            {
                if (s.At(x, y) != WreckScene.Glass) continue;
                int gx = x, gy = y;
                c.Add("frostGlass", WreckLayer.Over, gx, gy, gx + 1, gy + 1, p => { if (s.At(gx, gy) == WreckScene.Glass) FrostGlass(p, gx, gy, old); });
            }
        Frosted(c, s.Open);
        var wet = s.Machines.Where(m => Wet.IsMatch(m.Kind)).ToList();
        for (int k = wet.Count - 1; k > 0; k--)
        {
            int j = r.Int(k + 1);
            (wet[k], wet[j]) = (wet[j], wet[k]);
        }
        Pipes(c, wet.Take(c.Big ? 7 : 3).ToList());
        int n = 0;
        for (int k = 0; k < 2000 && n < (c.Big ? 22 : 8); k++)
        {
            int x = r.Int(s.Width), y = r.Int(s.Height);
            if (!s.Open(x, y) || !s.Walled(x, y - 1)) continue;
            Icicle(c, x + 0.5, y, 0.6 + r.Next() * 1.4);
            n++;
        }
    }

    /// <summary>Every machine but a lamp with a cell in <paramref name="mask"/> paled by frost, rime along its top.</summary>
    private static void Frosted(Ctx c, Func<int, int, bool> mask)
    {
        foreach (var m in c.S.Machines)
        {
            if (m.Kind == Vessels.Lamp || !Covers(m, mask) || c.Picture(m.Kind) is not { } pic) continue;
            var thing = m;
            uint sd = c.Seed();
            var tint = c.Old ? Rgba.Of(170, 180, 186, 0.35) : Rgba.Of(0x8fa8bc, 0.45f);
            c.Add("frosted", WreckLayer.Over, m.X - pic.Reach, m.Y - pic.Rise, m.X + m.W, m.Y + m.H, p =>
            {
                if (!thing.Stands) return;
                p.Picture(pic.Picture, thing.X - pic.Reach, thing.Y - pic.Rise, thing.W + pic.Reach, thing.H + pic.Rise, tint);
                var r = new Mulberry(sd);
                for (int j = 0; j < Math.Round(thing.W * 3.0); j++)
                    p.Rect(thing.X + r.Next() * thing.W, thing.Y + r.Next() * 0.35, 0.08 + r.Next() * 0.12, 0.06, Rgba.Of(232, 244, 252, 0.8));
            });
        }
    }

    /// <summary>A stream frozen from each burst machine up to the ceiling over it.</summary>
    private static void Pipes(Ctx c, List<WreckScene.Thing> burst)
    {
        foreach (var m in burst)
        {
            double x = m.X + m.W * (0.3 + c.R.Next() * 0.4);
            int cy = m.Y - 1;
            while (cy > -WreckScene.Pad && c.S.Open((int)Math.Floor(x), cy)) cy--;
            double y0 = cy + 1, y1 = m.Y;
            if (y1 - y0 <= 0.2) continue;
            uint sd = c.Seed();
            bool old = c.Old;
            c.Add("stream", WreckLayer.Over, x - 1, y0, x + 1, y1, p => Stream(p, x, y0, y1, sd, old));
        }
    }

    private static void Icicle(Ctx c, double x, double y, double len)
    {
        uint sd = c.Seed();
        bool old = c.Old;
        c.Add("icicle", WreckLayer.Over, x - 1.2, y, x + 1.2, y + len + 0.1, p =>
        {
            var r = new Mulberry(sd);
            var fill = old ? Rgba.Of(170, 180, 186, 0.7) : Rgba.Of(220, 238, 250, 0.88);
            for (int k = 0, n = 3 + r.Int(3); k < n; k++)
            {
                double ix = x + (r.Next() - 0.5) * 1.4, w = 0.1 + r.Next() * 0.16, l = len * (0.4 + r.Next() * 0.6);
                P tri = [(ix - w, y), (ix, y + l), (ix + w, y)];
                p.Fill(tri, fill);
                p.Stroke(tri, 0.03, Rgba.Of(60, 80, 95, 0.55), closed: true);
            }
        });
    }

    private static void Stream(WreckPaint p, double x, double y, double y1, uint seed, bool old)
    {
        double h = y1 - y;
        var pts = new P();
        for (int k = 0; k <= 6; k++) pts.Add((x - 0.15 - Math.Sin(k * 1.7 + seed % 7) * 0.12 - k / 6.0 * 0.1, y + h * k / 6));
        for (int k = 6; k >= 0; k--) pts.Add((x + 0.15 + Math.Sin(k * 1.3 + seed % 5) * 0.12 + k / 6.0 * 0.1, y + h * k / 6));
        p.Fill(pts, old ? Rgba.Of(170, 180, 186, 0.7) : Rgba.Of(220, 238, 250, 0.88));
        p.Stroke(pts, 0.04, Rgba.Of(60, 80, 95, 0.6), closed: true);
        p.Rect(x - 0.05, y + 0.2, 0.06, Math.Max(0, h - 0.4), Rgba.Of(255, 255, 255, 0.6));
    }

    private static void IceFan(WreckPaint p, double x, double y, double ux, double uy, double len, double spread, uint seed, bool old)
    {
        var r = new Mulberry(seed);
        double b = Math.Atan2(uy, ux), a0 = old ? 0.5 : 1;
        var wedge = new P { (x, y) };
        for (int i = 0; i <= 10; i++)
        {
            double a = b - spread / 2 + spread * i / 10;
            wedge.Add((x + Math.Cos(a) * len, y + Math.Sin(a) * len));
        }
        p.FillRadial(wedge, x, y, len, Rgba.Of(225, 240, 250, 0.22 * a0), Rgba.Of(225, 240, 250, 0));
        for (int j = 0; j < 30; j++)
        {
            double a = b + (r.Next() - 0.5) * spread, l = len * (0.3 + r.Next() * 0.7);
            p.Stroke([(x + Math.Cos(a) * 0.5, y + Math.Sin(a) * 0.5), (x + Math.Cos(a) * l, y + Math.Sin(a) * l)], 0.05,
                Rgba.Of(235, 247, 255, (0.15 + r.Next() * 0.25) * a0));
        }
    }

    private static void HullFrost(WreckPaint p, WreckScene s, double cx, double cy, double rad, uint seed, bool old)
    {
        var r = new Mulberry(seed);
        p.Clip = s.Walled;
        int n = (int)Math.Round(rad * rad * 3);
        for (int j = 0; j < n; j++)
        {
            double a = r.Next() * Math.PI * 2, d = Math.Pow(r.Next(), 0.6) * rad;
            double x = cx + Math.Cos(a) * d, y = cy + Math.Sin(a) * d * 0.8, sz = 0.05 + r.Next() * 0.12;
            p.Rect(x, y, sz, sz, old ? Rgba.Of(170, 178, 184, 0.5) : Rgba.Of(232, 244, 252, 0.85 - d / rad * 0.5));
        }
    }

    /// <summary>Rime in the cells of <paramref name="mask"/> round a breach (the mockup's hoar): crust on floors, spikes under ceilings, specks on walls.</summary>
    private static void Hoar(WreckPaint p, WreckScene s, Func<int, int, bool> mask, double cx, double cy, double reach, bool old, bool even)
    {
        var (X0, Y0, X1, Y1) = p.Cells;
        var (cr, cg, cb) = old ? (168, 176, 182) : (226, 240, 250);
        int x0 = Math.Max(X0, (int)Math.Floor(cx - reach)), x1 = Math.Min(X1 - 1, (int)Math.Ceiling(cx + reach));
        int y0 = Math.Max(Y0, (int)Math.Floor(cy - reach)), y1 = Math.Min(Y1 - 1, (int)Math.Ceiling(cy + reach));
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                if (!mask(x, y)) continue;
                double f = even ? 0.7 : 1 - Hyp(x - cx, y - cy) / reach;
                if (f <= 0) continue;
                var col = Rgba.Of(cr, cg, cb, (old ? 0.35 : 0.7) * (0.35 + 0.65 * f));
                uint i = (uint)(x * 73856093) ^ (uint)(y * 19349663);
                double H(int k, int salt) => Hash01(i, k, salt);
                if (s.Walled(x, y + 1))
                {
                    p.Rect(x, y + 0.8, 1, 0.2, col);
                    for (int k = 0; k < 3; k++)
                    {
                        double px = x + H(k, 3);
                        p.Fill([(px - 0.12, y + 0.8), (px, y + 0.6 - H(k, 4) * 0.15), (px + 0.12, y + 0.8)], col);
                    }
                }
                if (s.Walled(x, y - 1))
                    for (int k = 0; k < 4; k++)
                    {
                        double px = x + H(k, 5);
                        p.Fill([(px - 0.07, y), (px, y + 0.15 + H(k, 6) * 0.3 * f), (px + 0.07, y)], col);
                    }
                if (s.Walled(x - 1, y) || s.Walled(x + 1, y))
                {
                    double ex = s.Walled(x - 1, y) ? x : x + 0.82;
                    for (int k = 0; k < 3; k++) p.Rect(ex + H(k, 7) * 0.18, y + H(k, 8), 0.08, 0.08, col);
                }
                if (H(0, 9) < 0.35 * f) p.Rect(x + H(0, 10), y + H(0, 11), 0.07, 0.07, col);
            }
    }

    /// <summary>A number in [0, 1) from a cell, a draw and a salt: the same each painting.</summary>
    public static double Hash01(uint cell, int k, int salt)
    {
        uint h = cell * 0x9E3779B1u ^ (uint)k * 0x85EBCA77u ^ (uint)salt * 0xC2B2AE3Du;
        h ^= h >> 15;
        h *= 0x2C1B3C6Du;
        h ^= h >> 12;
        h *= 0x297A2D39u;
        h ^= h >> 15;
        return h / 4294967296.0;
    }

    private static void FrostGlass(WreckPaint p, int x0, int y0, bool old)
    {
        var r = new Mulberry((uint)(x0 * 7919 + y0));
        p.Clip = (x, y) => x == x0 && y == y0;
        p.Rect(x0, y0, 1, 1, old ? Rgba.Of(180, 190, 196, 0.5) : Rgba.Of(214, 234, 246, 0.55));
        var line = old ? Rgba.Of(210, 214, 216, 0.6) : Rgba.Of(250, 253, 255, 0.9);
        for (int j = 0; j < 5; j++)
        {
            double x = x0 + r.Next(), y = y0 + (r.Next() < 0.5 ? 1 : r.Next()), a = -Math.PI / 2 + (r.Next() - 0.5) * 1.6;
            for (int s = 0; s < 3; s++)
            {
                double nx = x + Math.Cos(a) * 0.22, ny = y + Math.Sin(a) * 0.22;
                p.Stroke([(x, y), (nx, ny)], 0.05, line);
                p.Stroke([(nx, ny), (nx + Math.Cos(a + 0.9) * 0.1, ny + Math.Sin(a + 0.9) * 0.1)], 0.05, line);
                p.Stroke([(nx, ny), (nx + Math.Cos(a - 0.9) * 0.1, ny + Math.Sin(a - 0.9) * 0.1)], 0.05, line);
                (x, y, a) = (nx, ny, a + (r.Next() - 0.5) * 0.6);
            }
        }
    }

    // ---------- asteroid ----------

    private static void Rock(Ctx c)
    {
        var (s, e, r) = (c.S, c.E, c.R);
        var (ux, _) = Unit(e.DirX, e.DirY);
        Petals(c, (_, _) => (ux * 0.6, 1), 0.8);
        foreach (var (x, y) in c.Carved)
            if (InRib(s, x, y) && r.Chance(0.4)) RibStub(c, x + 0.5, y + 0.5, ux * 0.3, 1, 1.5 + r.Next() * 2, (r.Next() - 0.5) * 1.5, 0.6);
        // The strike freezes its own breach (the user's pick): its fan out along the strike, back up.
        Freeze(c, e.X + 0.5, e.Y, e.DirX, -1, 4 * e.Radius + 10);
        c.Spots.Add(new(e.X - e.Radius * 0.7, e.Y + 2));
        c.Spots.Add(new(e.X + e.Radius * 0.6, e.Y + 3));
    }

    // ---------- break ----------

    private static void Break(Ctx c)
    {
        var (s, e, r) = (c.S, c.E, c.R);
        var b = e.Break!;
        double gcx = e.X;
        Petals(c, (x, _) => (x < gcx ? -0.6 : 0.6, 0.4), 0.45);
        // The faces as they stand: from the gap's middle out to the first cell either way, a row at a time.
        int Face(int y, bool stern)
        {
            int x = e.X;
            for (int k = 0; k < b.Gap + 12; k++, x += stern ? -1 : 1)
                if (s.At(x, y) != WreckScene.Space) return stern ? x + 1 : x;
            return stern ? e.X - b.Gap / 2 : e.X + (b.Gap - b.Gap / 2);
        }
        var bodies = s.Built.Vessel.Sections.Where(k => k.Kind == Vessel.SectionKind.Body).ToList();
        int top = bodies.Min(k => k.Top), bottom = bodies.Max(k => k.Bottom);
        foreach (bool stern in (ReadOnlySpan<bool>)[true, false])
        {
            int dy = stern ? b.SternRows - b.BowRows : 0;
            var rows = new List<int> { (stern ? bodies[0].Top : bodies[^1].Top) };
            rows.AddRange(s.Built.Vessel.Decks.Select(d => d.Floor + dy));
            foreach (int band in rows)
            {
                int y = band + 1;
                int fx = Face(y, stern);
                for (int k = 0, n = 1 + r.Int(3); k < n; k++)
                    c.Spots.Add(new(stern ? fx - 0.4 - k * 0.6 : fx + 0.4 + k * 0.6, y + r.Next() * 0.5, 3 + r.Next() * 6,
                        (stern ? 1 : -1) * (0.6 + r.Next() * 2.2), r.Chance(0.35), stern));
                if (r.Chance(0.6)) RibStub(c, fx, band + 0.5, stern ? 1 : -1, 0.2, 1.5 + r.Next() * 2.5, 0, 0.4 + r.Next() * 1.4);
            }
        }
        int gcy = e.Y, fs = Face(gcy, true), fb = Face(gcy, false);
        if (e.Cause == null)
        {
            // Overload: the keel folded before it broke, stress cracks across the plates from the faces.
            c.Add("folds", WreckLayer.Over, fs - 12, -WreckScene.Pad, fs, s.Height + WreckScene.Pad, p => Folds(p, s, fs - 12, fs - 1));
            c.Add("folds", WreckLayer.Over, fb + 1, -WreckScene.Pad, fb + 11, s.Height + WreckScene.Pad, p => Folds(p, s, fb + 1, fb + 10));
            foreach (bool stern in (ReadOnlySpan<bool>)[true, false])
                for (int k = 0; k < 4; k++)
                {
                    double y = top + 1 + r.Next() * (bottom - top - 2), len = 6 + r.Next() * 12;
                    int fx = Face((int)Math.Round(y), stern), dir = stern ? -1 : 1;
                    uint sd = c.Seed();
                    c.Add("stress", WreckLayer.Over, fx - len - 4, y - len, fx + len + 4, y + len, p => Stress(p, s, fx, y, dir, len, sd));
                }
        }
        else if (e.Cause == WreckKind.Explosion)
            foreach (int fx in (ReadOnlySpan<int>)[fs, fb])
            {
                uint sd = c.Seed();
                c.Add("heat", WreckLayer.Over, fx - 10, gcy - 10, fx + 10, gcy + 10, p => Heat(p, s, fx, gcy, 9));
                c.Add("scorch", WreckLayer.Over, fx - 9, gcy - 9, fx + 9, gcy + 9, p => Scorch(p, s, fx, gcy, 8, sd));
            }
    }

    private static void Folds(WreckPaint p, WreckScene s, double x0, double x1)
    {
        p.Clip = s.Walled;
        var (_, Y0, _, Y1) = p.Cells;
        for (double x = x0; x < x1; x += 1.3)
            foreach (var (col, off) in (ReadOnlySpan<(Rgba, double)>)[(Rgba.Of(16, 16, 18, 0.6), 0), (Rgba.Of(240, 238, 230, 0.35), 0.1)])
            {
                var pts = new P();
                int k = (int)Math.Floor(Y0 / 0.7);
                for (double y = k * 0.7; y < Y1 + 0.7; y += 0.7, k++) pts.Add((x + off + (k % 2 != 0 ? 0.45 : 0), y));
                p.Stroke(pts, 0.08, col);
            }
    }

    private static void Stress(WreckPaint p, WreckScene s, double x, double y, int dir, double len, uint seed)
    {
        var r = new Mulberry(seed);
        p.Clip = s.Walled;
        void Crack(double cx, double cy, double a, double l, int depth)
        {
            var pts = new P { (cx, cy) };
            var at = new List<(double X, double Y, double A)>();
            for (double t = 0; t < l; t += 0.5)
            {
                a += (r.Next() - 0.5) * 0.7;
                cx += Math.Cos(a) * 0.5;
                cy += Math.Sin(a) * 0.5;
                pts.Add((cx, cy));
                at.Add((cx, cy, a));
            }
            p.Stroke(pts, 0.07 + depth * 0.03, Rgba.Of(14, 14, 16, 0.85));
            if (depth > 0)
                foreach (var q in at)
                    if (r.Next() < 0.12) Crack(q.X, q.Y, q.A + (r.Next() < 0.5 ? 0.8 : -0.8), l * 0.4, depth - 1);
        }
        Crack(x, y, dir > 0 ? 0 : Math.PI, len, 2);
    }

    // ---------- shared ----------

    /// <summary>
    /// Plates torn at a hole's edge (the mockup's petals): for each carved cell still open to space, each hull cell beside it bent the way
    /// <paramref name="force"/> says, <paramref name="odds"/> of them.
    /// </summary>
    private static void Petals(Ctx c, Func<double, double, (double X, double Y)> force, double odds)
    {
        var (s, r) = (c.S, c.R);
        foreach (var (x, y) in c.Carved)
        {
            if (s.At(x, y) != WreckScene.Space) continue;
            foreach (var (dx, dy) in (ReadOnlySpan<(int, int)>)[(1, 0), (-1, 0), (0, 1), (0, -1)])
            {
                int wx = x - dx, wy = y - dy;
                if (!s.Walled(wx, wy) || !r.Chance(odds)) continue;
                var (fx, fy) = force(wx, wy);
                double len = 0.7 + r.Next() * 0.9;
                uint sd = c.Seed();
                uint hull = c.Hull;
                c.Add("petal", WreckLayer.Over, wx - 2, wy - 2, wx + 3, wy + 3, p => { if (s.Walled(wx, wy)) Petal(p, wx, wy, dx, dy, fx, fy, len, sd, hull); });
            }
        }
    }

    private static void Petal(WreckPaint p, int x, int y, int sx, int sy, double fx, double fy, double len, uint seed, uint hull)
    {
        var r = new Mulberry(seed);
        double ex = x + 0.5 + sx * 0.5, ey = y + 0.5 + sy * 0.5, px = -sy, py = sx, w = 0.4 + r.Next() * 0.1;
        var b1 = (X: ex - px * w, Y: ey - py * w);
        var b2 = (X: ex + px * w, Y: ey + py * w);
        double tx = ex + sx * len * 0.55 + fx * len * 0.75 + px * (r.Next() - 0.5) * 0.3, ty = ey + sy * len * 0.55 + fy * len * 0.75 + py * (r.Next() - 0.5) * 0.3;
        double mx = (b1.X + tx) / 2 + fx * 0.12, my = (b1.Y + ty) / 2 + fy * 0.12;
        p.Fill([b1, (mx, my), (tx, ty), b2], Rgba.Of(Rgba.Mix(hull, 0xffffff, 0.12)));
        p.Fill([b2, (tx, ty), ((b1.X + b2.X) / 2, (b1.Y + b2.Y) / 2)], Rgba.Of(Rgba.Mix(hull, 0x000000, 0.45)));
        p.Stroke([b1, (mx, my), (tx, ty), b2], 0.07, Ink);
    }

    private static void RibStub(Ctx c, double x, double y, double dx, double dy, double len, double bx, double by)
    {
        uint hull = c.Hull;
        c.Add("ribStub", WreckLayer.Over, x - len - 2, y - len - 2, x + len + 2, y + len + 2, p =>
        {
            var path = Shapes.Quad((x, y), (x + dx * len * 0.6, y + dy * len * 0.6), (x + dx * len + bx, y + dy * len + by));
            p.Stroke(path, 0.75, Ink);
            p.Stroke(path, 0.6, Rgba.Of(Rgba.Mix(hull, 0x303030, 0.4)));
        });
    }

    private static void Beads(WreckPaint p, List<(double X, double Y, double S, bool Hang)> beads)
    {
        foreach (var (x, y, s, hang) in beads)
        {
            P shape = hang
                ? [.. Shapes.Quad((x - s * 0.5, y), (x - s, y + s * 1.6), (x, y + s * 2)), .. Shapes.Quad((x, y + s * 2), (x + s, y + s * 1.6), (x + s * 0.5, y))]
                : Shapes.Ellipse(x, y, s, s * 0.6);
            p.Fill(shape, Rgba.Of(0x9ea2a6));
            p.Stroke(shape, 0.04, Ink, closed: true);
            p.Rect(x - s * 0.3, y + (hang ? s : -s * 0.2), s * 0.3, s * 0.3, Rgba.Of(255, 255, 255, 0.7));
        }
    }

    private static void Soot(WreckPaint p, Func<int, int, bool> mask, double cx, double cy, double rad, double strength, uint seed)
    {
        p.Clip = mask;
        for (int s = 0; s < 3; s++) p.Fill(Shapes.Blob(cx, cy, rad * (1 - s * 0.3), seed + (uint)s, 0.75), Rgba.Of(16, 11, 8, 0.3 * strength));
    }

    private static void Scorch(WreckPaint p, WreckScene s, double cx, double cy, double rad, uint seed)
    {
        p.Clip = s.Walled;
        for (int k = 0; k < 2; k++) p.Fill(Shapes.Blob(cx, cy, rad * (1 - k * 0.4), seed + (uint)k, 0.7), Rgba.Of(20, 14, 10, 0.42));
    }

    /// <summary>Temper colours on the metal round a blast: straw, bronze, blue nearest; hard steps, on the walls only.</summary>
    private static void Heat(WreckPaint p, WreckScene s, double cx, double cy, double rad)
    {
        p.Clip = s.Walled;
        foreach (var (f, col) in (ReadOnlySpan<(double, Rgba)>)[(1, Rgba.Of(206, 176, 96, 0.32)), (0.68, Rgba.Of(158, 94, 42, 0.38)), (0.38, Rgba.Of(72, 88, 168, 0.42))])
            p.Fill(Shapes.Blob(cx, cy, rad * f, (uint)Math.Round(cx * 31 + f * 7), 0.8), col);
    }

    /// <summary>The event's cables and pipes hanging from where it tore (the mockup's finishEvent): an old one's shorter and stiller.</summary>
    private static void Cables(Ctx c)
    {
        foreach (var spot in c.Spots.Take(14))
        {
            double len = (spot.Len ?? 2 + c.R.Next() * 4) * (c.Old ? 0.75 : 1), sway = (spot.Sway ?? (c.R.Next() - 0.5) * 1.6) * (c.Old ? 0.5 : 1);
            var (x, y, pipe) = (spot.X, spot.Y, spot.Pipe);
            c.Ends?.Add(new(c.Index, x + sway * 1.1, y + len, pipe, spot.Stern));
            c.Add(pipe ? "pipe" : "cable", WreckLayer.Over, x - 3, y - 1, x + Math.Abs(sway) * 1.2 + 3, y + len + 1, p => Hang(p, x, y, len, sway, pipe));
        }
    }

    private static void Hang(WreckPaint p, double x, double y, double len, double sway, bool pipe)
    {
        double ex = x + sway * 1.1, ey = y + len;
        P Path(double off) => Shapes.Cubic((x + off, y), (x + off + sway * 0.3, y + len * 0.4), (x + off + sway, y + len * 0.7), (ex + off, ey));
        if (pipe)
        {
            p.Stroke(Path(0), 0.42, Ink);
            p.Stroke(Path(0), 0.3, Rgba.Of(0x7a7e82));
            p.Stroke(Path(-0.06), 0.06, Rgba.Of(230, 230, 225, 0.35));
            p.Rect(ex - 0.28, ey - 0.08, 0.56, 0.16, Rgba.Of(0x55595d));
            return;
        }
        for (int k = 0; k < 2; k++) p.Stroke(Path(k * 0.22), 0.16, Rgba.Of(k == 0 ? 0x15171au : 0x7a2420u));
        p.Stroke([(ex, ey), (ex - 0.12, ey + 0.25)], 0.05, Rgba.Of(0xc4823e));
        p.Stroke([(ex, ey), (ex + 0.1, ey + 0.28)], 0.05, Rgba.Of(0xc4823e));
    }

    // ---------- scavengers ----------

    /// <summary>The scavengers' marks after an old first group: micrometeor pits over every outer plate, crude patches over the holes they mended.</summary>
    private static void Scavenged(WreckScene s, List<WreckMark> marks, uint hull, Mulberry r)
    {
        marks.Add(new("pits", WreckLayer.Over, -WreckScene.Pad, -WreckScene.Pad, s.Width + WreckScene.Pad, s.Height + WreckScene.Pad, p => Pits(p, s)));
        foreach (var patch in s.Wreck.Patches)
        {
            uint sd = (uint)r.Int(1_000_000_000);
            marks.Add(new("patch", WreckLayer.Over, patch.X - 1, patch.Y - 1, patch.X + patch.Width + 1, patch.Y + patch.Height + 1, p => Patch(p, patch, hull, sd)));
        }
    }

    private static void Pits(WreckPaint p, WreckScene s)
    {
        var (X0, Y0, X1, Y1) = p.Cells;
        for (int y = Y0; y < Y1; y++)
            for (int x = X0; x < X1; x++)
            {
                if (!s.Skin(x, y)) continue;
                uint i = (uint)(x * 73856093) ^ (uint)(y * 19349663);
                int n = 2 + (int)(Hash01(i, 0, 41) * 5);
                for (int j = 0; j < n; j++)
                {
                    double px = x + Hash01(i, j, 1), py = y + Hash01(i, j, 2), rr = 0.035 + Hash01(i, j, 3) * 0.06;
                    p.Ellipse(px, py, rr, rr, Rgba.Of(22, 22, 24, 0.75));
                    p.Ellipse(px - rr * 0.6, py - rr * 0.6, rr * 0.5, rr * 0.5, Rgba.Of(235, 235, 230, 0.35));
                }
                if (Hash01(i, 0, 43) < 1 / 9.0) p.Rect(x, y, 1, 1, Rgba.Of(40, 36, 30, 0.18));
            }
    }

    private static void Patch(WreckPaint p, WreckPatch patch, uint hull, uint seed)
    {
        var r = new Mulberry(seed);
        double turn = (r.Next() - 0.5) * 0.06, cx = patch.X + patch.Width / 2.0, cy = patch.Y + patch.Height / 2.0, w = patch.Width + 0.5, h = patch.Height + 0.4;
        (double, double) At(double u, double v) => (cx + u * Math.Cos(turn) - v * Math.Sin(turn), cy + u * Math.Sin(turn) + v * Math.Cos(turn));
        P Box(double u0, double v0, double u1, double v1) => [At(u0, v0), At(u1, v0), At(u1, v1), At(u0, v1)];
        p.Fill(Box(-w / 2, -h / 2, w / 2, h / 2), Rgba.Of(Rgba.Mix(Rgba.Mix(hull, 0x5a4a3a, 0.35), 0x000000, 0.15)));
        p.Fill(Box(-w / 2, h / 2 - 0.25, w / 2, h / 2), Rgba.Of(140, 80, 40, 0.35));
        p.Stroke(Box(-w / 2, -h / 2, w / 2, h / 2), 0.07, Ink, closed: true);
        for (double u = -w / 2 + 0.2; u < w / 2; u += 0.55)
        {
            p.Fill(Box(u, -h / 2 + 0.12, u + 0.09, -h / 2 + 0.21), Rgba.Of(0x2a2a2a));
            p.Fill(Box(u, h / 2 - 0.21, u + 0.09, h / 2 - 0.12), Rgba.Of(0x2a2a2a));
        }
    }
}
