using System;
using System.Collections.Generic;
using Stillwake.Sim.Items;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// One body on the floor (0060): the stacks one death left, as the sim tags them, by the peer that died and the
/// items' tick it died on, and where they lie together: the middle of their bottoms, in thousandths of a cell.
/// </summary>
public readonly record struct Pile(int Peer, int Died, double X, double Y);

/// <summary>The bodies on the floor, read from its stacks' tags, without Godot, so it is tested.</summary>
public static class Bodies
{
    /// <summary>
    /// Every body among <paramref name="floor"/>'s stacks into <paramref name="into"/>, cleared first, the oldest
    /// death first and by peer on a tie; each at the mean of its stacks' points, as <paramref name="at"/> places a
    /// stack (between ticks, in a view), or where the sim has it.
    /// </summary>
    public static void Of(IReadOnlyList<FloorStack> floor, List<Pile> into, Func<FloorStack, (double X, double Y)>? at = null)
    {
        into.Clear();
        for (int i = 0; i < floor.Count; i++)
            if (floor[i].Body is { } body && !into.Exists(p => p.Peer == body.Peer && p.Died == body.Died))
                into.Add(new Pile(body.Peer, body.Died, 0, 0));
        // Bodies are few: each is summed over the floor apart.
        for (int k = 0; k < into.Count; k++)
        {
            double x = 0, y = 0;
            int n = 0;
            for (int i = 0; i < floor.Count; i++)
            {
                if (floor[i].Body is not { } b || b.Peer != into[k].Peer || b.Died != into[k].Died) continue;
                var (sx, sy) = at?.Invoke(floor[i]) ?? (floor[i].X, floor[i].Y);
                (x, y, n) = (x + sx, y + sy, n + 1);
            }
            into[k] = into[k] with { X = x / n, Y = y / n };
        }
        into.Sort((a, b) => a.Died != b.Died ? a.Died.CompareTo(b.Died) : a.Peer.CompareTo(b.Peer));
    }

    /// <summary>
    /// <paramref name="peer"/>'s most recent body, the newest death of its among <paramref name="floor"/>'s stacks,
    /// where the sim has its stacks; none while it has none. What its marker points at: older bodies get none.
    /// </summary>
    public static Pile? Latest(IReadOnlyList<FloorStack> floor, int peer)
    {
        int? died = null;
        for (int i = 0; i < floor.Count; i++)
            if (floor[i].Body is { } b && b.Peer == peer && (died == null || b.Died > died)) died = b.Died;
        if (died is not int newest) return null;
        double x = 0, y = 0;
        int n = 0;
        for (int i = 0; i < floor.Count; i++)
            if (floor[i].Body is { } b && b.Peer == peer && b.Died == newest)
                (x, y, n) = (x + floor[i].X, y + floor[i].Y, n + 1);
        return new Pile(peer, newest, x / n, y / n);
    }
}
