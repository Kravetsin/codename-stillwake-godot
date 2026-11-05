using System;
using System.Collections.Generic;
using Stillwake.Game.Render;
using static Stillwake.Game.Render.Ink.Draw;

namespace Stillwake.Game.Features.Debris;

/// <summary>A blow landed on a wreck: where on its picture, from its middle, in pixels, and the share of its hit points lost before it.</summary>
public readonly record struct Blow(double X, double Y, double Before);

/// <summary>
/// A wreck's cracks (damage-looks, "penDent"): a dent where each blow lands and six wandering pen lines out of it, each over a
/// thin lit line, all growing as the wreck loses hit points after that blow; laid out by its seed, so a wreck cracks its own way.
/// </summary>
public static class WreckCracks
{
    /// <summary>
    /// A blow at (<paramref name="x"/>, <paramref name="y"/>) after <paramref name="before"/> was lost: its own cracks, unless one
    /// lands within <see cref="DamageLooks.BlowsApart"/> of an earlier one, which then only grows, or the wreck shows its most.
    /// </summary>
    public static void Land(List<Blow> blows, double x, double y, double before)
    {
        if (blows.Count >= DamageLooks.MostBlows) return;
        foreach (var b in blows)
            if ((b.X - x) * (b.X - x) + (b.Y - y) * (b.Y - y) < DamageLooks.BlowsApart * DamageLooks.BlowsApart) return;
        blows.Add(new Blow(x, y, before));
    }

    /// <summary>
    /// Where a wreck first seen damaged, with no blow seen (loaded, or hit out of view), shows it was struck: a spot of its middle,
    /// <paramref name="width"/> by <paramref name="height"/>, by its seed, as if struck before anything was lost.
    /// </summary>
    public static Blow Unseen(int seed, double width, double height) =>
        new((Hash(seed, 1) * 0.4 - 0.2) * width, (Hash(seed, 2) * 0.4 - 0.2) * height, 0);

    /// <summary>How far a blow's cracks have grown with <paramref name="share"/> of the hit points lost: over the next <see cref="DamageLooks.DentGrow"/>.</summary>
    public static double Grown(Blow blow, double share) => Clamp((share - blow.Before) / DamageLooks.DentGrow);

    /// <summary>The cracks of <paramref name="blows"/> with <paramref name="share"/> lost, laid out by <paramref name="seed"/>, into <paramref name="ink"/>.</summary>
    public static void Draw(Ink ink, IReadOnlyList<Blow> blows, double share, int seed)
    {
        for (int n = 0; n < blows.Count; n++)
        {
            var h = blows[n];
            double k = Grown(h, share);
            var r = new Rng(seed * 97 + n);
            ink.Fill(Blob(r, h.X, h.Y, DamageLooks.DentLeast + DamageLooks.DentRadius * k, 8, 0.5), (h.X, h.Y), DamageLooks.Dent);
            for (int i = 0; i < DamageLooks.DentLines; i++)
            {
                double a = r.Next() * 6.283, length = DamageLooks.DentLongest * (0.45 + 0.55 * r.Next()), show = Clamp((k - i * 0.1) / 0.5);
                var line = Part(Walk(r, h.X, h.Y, a, length, 0.45, 0.7), show);
                if (line.Count < 2) continue;
                var lit = line.ConvertAll(p => (p.X, p.Y + DamageLooks.DentLitDrop));
                ink.Pen(lit, DamageLooks.DentWidth * 0.55, DamageLooks.DentLit, seed + i, 0.05, 0.5);
                ink.Pen(line, DamageLooks.DentWidth, DamageLooks.DentInk, seed + i, 0.05, 0.5);
            }
        }
    }
}
