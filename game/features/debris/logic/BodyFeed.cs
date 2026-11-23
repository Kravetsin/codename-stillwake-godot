using System;
using System.Collections.Generic;
using Stillwake.Game.Shared;
using Spec = Stillwake.Content.Debris;

namespace Stillwake.Game.Features.Debris;

/// <summary>
/// Which of the host player's bodies go to the sim in a <c>Moved</c> (debris-physics): those whose worst box corner strays from the
/// sim's pose by more than <c>Debris.FeedSlack</c>, the farthest first, at most <c>Debris.FeedMost</c>; the rest wait a tick.
/// </summary>
public static class BodyFeed
{
    /// <summary>How far the worst corner of a box <paramref name="halfW"/> by <paramref name="halfH"/> half each way lies from itself at <paramref name="a"/> and at <paramref name="b"/>, in thousandths of a cell.</summary>
    public static double Stray(PiecePose a, PiecePose b, double halfW, double halfH)
    {
        double worst = 0;
        for (int corner = 0; corner < 4; corner++)
        {
            double lx = corner % 2 == 0 ? -halfW : halfW, ly = corner < 2 ? -halfH : halfH;
            var (ax, ay) = a.At(lx, ly);
            var (bx, by) = b.At(lx, ly);
            worst = Math.Max(worst, Math.Sqrt((ax - bx) * (ax - bx) + (ay - by) * (ay - by)));
        }
        return worst;
    }

    /// <summary>
    /// The ids of <paramref name="strays"/> to feed this tick: strayed more than <paramref name="slack"/>, the farthest first (at a tie, the
    /// lower id), at most <paramref name="most"/>.
    /// </summary>
    public static List<int> Picked(IReadOnlyList<(int Id, double Stray)> strays, double slack = Spec.FeedSlack, int most = Spec.FeedMost)
    {
        var over = new List<(int Id, double Stray)>();
        foreach (var s in strays)
            if (s.Stray > slack) over.Add(s);
        over.Sort((a, b) => b.Stray != a.Stray ? b.Stray.CompareTo(a.Stray) : a.Id.CompareTo(b.Id));
        var picked = new List<int>(Math.Min(most, over.Count));
        for (int i = 0; i < over.Count && i < most; i++) picked.Add(over[i].Id);
        return picked;
    }
}
