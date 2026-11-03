using System;
using System.Collections.Generic;
using Stillwake.Game.I18n;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Debris;

/// <summary>A debris net without a word, and its word on demand: its front by how many it holds, where each lies, its count by the pointer.</summary>
public static class NetShows
{
    /// <summary>How many a net is drawn holding: what it holds, at most a full net's (a picture for each, <c>net_front_*</c>).</summary>
    private static int Drawn(int held) => Math.Clamp(held, 0, NetInk.Places.Length - 1);

    /// <summary>The net in front of what it holds, sagging by <paramref name="held"/>.</summary>
    public static string FrontOf(int held) => $"net_front_{Drawn(held)}";

    /// <summary>Where the <paramref name="k"/>-th piece caught lies in a net holding <paramref name="held"/> (<see cref="NetInk.Places"/>).</summary>
    public static (float X, float Y, float Turn) Place(int held, int k) => NetInk.Places[Drawn(held)][k];

    /// <summary>
    /// Where each of the pieces <paramref name="held"/>, by id in catch order, is drawn in a net whose top-left is <paramref name="cell"/>
    /// of a grid <paramref name="width"/> wide, in the world's pixels: where a collect's loot flies home from (<c>OwnCatches</c>).
    /// </summary>
    public static (int Piece, (double X, double Y) At)[] Lying(IReadOnlyList<int> held, int cell, int width)
    {
        var lying = new (int Piece, (double X, double Y) At)[held.Count];
        for (int k = 0; k < held.Count; k++)
        {
            var (x, y, _) = Place(held.Count, k);
            lying[k] = (held[k], (cell % width * Camera.Cell + x, cell / width * Camera.Cell + y));
        }
        return lying;
    }

    /// <summary>The pointer's tip over a machine of <paramref name="spec"/>: a net's <paramref name="held"/> pieces of how many it holds; null for any other.</summary>
    public static string[]? Says(Spec.MachineSpec spec, int held) =>
        spec.Is(Spec.MachineTrait.Catches) ? [En.T("debris.netHolds", ("held", held), ("of", Spec.Debris.NetHolds))] : null;
}
