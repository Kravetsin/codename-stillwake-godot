using System;
using System.Collections.Generic;
using Stillwake.Game.Render;
using static Stillwake.Game.Render.Ink.Draw;

namespace Stillwake.Game.Features.Debris;

/// <summary>
/// A blow landed on a wreck: where on its picture, from its middle, in pixels, the share of its hit points lost before it, and
/// how many blows have landed there.
/// </summary>
public readonly record struct Blow(double X, double Y, double Before, int Hits = 1);

/// <summary>One line of a wreck's crack network: its points, how far along the cracks from the first blow it starts, how deep a fork it is.</summary>
public sealed record CrackLine(IReadOnlyList<(double X, double Y)> Points, double From, int Depth);

/// <summary>A wreck's crack network, laid out once: its lines, and how far along the cracks its farthest point on the picture is.</summary>
public sealed record CrackNet(IReadOnlyList<CrackLine> Lines, double Far);

/// <summary>
/// A wreck's cracks, the user's pick «Трещины до конца» (cracks-to-end): a dent where each blow lands, deepening with each blow
/// there, and one branching network out of the first blow that runs on with the share of hit points lost until it covers the
/// whole picture by the break. Laid out by the wreck's seed, so a wreck cracks its own way.
/// </summary>
public static class WreckCracks
{
    /// <summary>A blow at (<paramref name="x"/>, <paramref name="y"/>) after <paramref name="before"/> was lost: its own dent, unless it lands within <see cref="DamageLooks.BlowsApart"/> of an earlier one, which deepens.</summary>
    public static void Land(List<Blow> blows, double x, double y, double before)
    {
        for (int i = 0; i < blows.Count; i++)
        {
            var b = blows[i];
            if ((b.X - x) * (b.X - x) + (b.Y - y) * (b.Y - y) < DamageLooks.BlowsApart * DamageLooks.BlowsApart)
            {
                blows[i] = b with { Hits = b.Hits + 1 };
                return;
            }
        }
        blows.Add(new Blow(x, y, before));
    }

    /// <summary>
    /// Where a wreck first seen damaged, with no blow seen (loaded, or hit out of view), shows it was struck: a spot of its middle,
    /// <paramref name="width"/> by <paramref name="height"/>, by its seed, as if struck before anything was lost.
    /// </summary>
    public static Blow Unseen(int seed, double width, double height) =>
        new((Hash(seed, 1) * 0.4 - 0.2) * width, (Hash(seed, 2) * 0.4 - 0.2) * height, 0);

    /// <summary>
    /// The network out of <paramref name="root"/>, the first blow, over a picture <paramref name="solid"/> says is opaque at a point
    /// from its middle: trunks fanned toward the middle, each forking as it runs and ending a little past the picture's edge.
    /// </summary>
    public static CrackNet Network(Blow root, Func<double, double, bool> solid, int seed)
    {
        var r = new Rng(seed * 31 + 5);
        var lines = new List<CrackLine>();
        void Grow(double x, double y, double a, double length, double from, int depth)
        {
            var pts = new List<(double X, double Y)> { (x, y) };
            int outside = 0;
            for (double s = 0; s < length; s += Step)
            {
                a += (r.Next() - 0.5) * (depth > 0 ? DamageLooks.NetWander * 1.6 : DamageLooks.NetWander);
                x += Math.Cos(a) * Step;
                y += Math.Sin(a) * Step;
                pts.Add((x, y));
                if (!solid(x, y)) { if (++outside > 5) break; }
                else outside = 0;
            }
            lines.Add(new CrackLine(pts, from, depth));
            if (depth >= DamageLooks.NetDepth) return;
            for (int i = 8 + (int)Math.Floor(r.Next() * 6); i < pts.Count - 6; i += DamageLooks.NetEvery + (int)Math.Floor(r.Next() * DamageLooks.NetEvery))
            {
                if (r.Next() < 0.3) continue;
                double along = Math.Atan2(pts[i + 1].Y - pts[i].Y, pts[i + 1].X - pts[i].X), side = r.Next() < 0.5 ? -1 : 1;
                Grow(pts[i].X, pts[i].Y, along + side * (0.5 + 0.55 * r.Next()), (pts.Count - i) * Step * (0.35 + 0.4 * r.Next()), from + i * Step, depth + 1);
            }
        }
        double inward = Math.Atan2(-root.Y, -root.X);
        foreach (double spread in DamageLooks.NetFan) Grow(root.X, root.Y, inward + spread + (r.Next() - 0.5) * 0.25, DamageLooks.NetLength, 0, 0);
        double far = 0;
        foreach (var line in lines)
            for (int i = 0; i < line.Points.Count; i++)
                if (solid(line.Points[i].X, line.Points[i].Y)) far = Math.Max(far, line.From + i * Step);
        return new CrackNet(lines, far);
    }

    /// <summary>How far along the cracks <paramref name="net"/> has run with <paramref name="share"/> of the hit points lost: all of it by <see cref="DamageLooks.NetFull"/>.</summary>
    public static double Reach(CrackNet net, double share) =>
        net.Far / Math.Pow(DamageLooks.NetFull, DamageLooks.NetPower) * Math.Pow(Clamp(share), DamageLooks.NetPower);

    /// <summary>The dents of <paramref name="blows"/> and <paramref name="net"/> run as far as <paramref name="share"/> lost says, laid out by <paramref name="seed"/>, into <paramref name="ink"/>.</summary>
    public static void Draw(Ink ink, IReadOnlyList<Blow> blows, CrackNet? net, double share, int seed)
    {
        for (int n = 0; n < blows.Count; n++)
        {
            var h = blows[n];
            double radius = DamageLooks.DentLeast + DamageLooks.DentPerHits * Math.Log2(1 + h.Hits) + DamageLooks.DentRadius * share;
            ink.Fill(Blob(new Rng(seed * 97 + n), h.X, h.Y, radius, 8, 0.5), (h.X, h.Y), DamageLooks.Dent);
        }
        if (net == null) return;
        double reach = Reach(net, share);
        for (int k = 0; k < net.Lines.Count; k++)
        {
            var line = net.Lines[k];
            double f = (reach - line.From) / Step;
            if (f < 1) continue;
            var pts = Part([.. line.Points], Math.Min(1, f / (line.Points.Count - 1)));
            if (pts.Count < 2) continue;
            double width = DamageLooks.NetWidths[Math.Min(line.Depth, DamageLooks.NetWidths.Length - 1)] * (1 + 0.5 * Clamp((reach - line.From) / 30));
            ink.Pen(pts.ConvertAll(p => (p.X, p.Y + DamageLooks.DentLitDrop)), width * 0.55, DamageLooks.DentLit, seed + k, 0.05, 0.5);
            ink.Pen(pts, width, DamageLooks.DentInk, seed + k, 0.05, 0.5);
        }
    }

    /// <summary>A network's step along each line, in pixels.</summary>
    private const double Step = 0.5;
}
