using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Content;
using Stillwake.Sim;
using Stillwake.Sim.Items;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// Where this player's hero stands, as crew last moved it: its feet (<paramref name="X"/>,
/// <paramref name="Y"/>) in thousandths of a cell, the way it faces, its body's cell and whether it floats
/// out in space, where a pick-up is heard as the glove's thump. What a drop is thrown from, and what it
/// walks over and picks up.
/// </summary>
public sealed record HeroSpot(int X, int Y, int Facing, int Cell, bool Floats);

/// <summary>What the hero picks up of items: stacks on the floor it walks over.</summary>
public static class Reach
{
    private const int Unit = 1000;

    /// <summary>The stacks on the floor this peer has a pick-up in flight for: gone from its view at once (0014).</summary>
    public static IReadOnlySet<int> PickingUp(IReadOnlyList<Command> inFlight)
    {
        // Asked every frame, and most have no pick-up in flight: then nothing is built.
        for (int i = 0; i < inFlight.Count; i++)
            if (inFlight[i] is PickUp) return inFlight.OfType<PickUp>().Select(p => p.Id).ToHashSet();
        return None;
    }

    private static readonly IReadOnlySet<int> None = new HashSet<int>();

    /// <summary>
    /// The stacks on the floor peer <paramref name="peer"/>'s hero, its feet at (<paramref name="x"/>,
    /// <paramref name="y"/>), picks up as it goes (pickup.ts): those in any cell its body overlaps, unless
    /// one is in <paramref name="taking"/>, picked up already with the pick-up still in flight, or the sim
    /// would refuse it (its own, dropped a moment ago; no room for any).
    /// </summary>
    public static IReadOnlyList<int> PickUpsOf(IItemsQueries items, int width, int peer, int x, int y, IReadOnlySet<int> taking)
    {
        int half = Hero.Width / 2;
        int left = FloorDiv(x - half, Unit), right = FloorDiv(x + half - 1, Unit);
        int top = FloorDiv(y - Hero.Height, Unit), bottom = FloorDiv(y - 1, Unit);
        return items.Floor
            .Where(s => s.Cell % width >= left && s.Cell % width <= right && s.Cell / width >= top && s.Cell / width <= bottom)
            .Where(s => !taking.Contains(s.Id) && items.PickUpProblem(peer, s.Id) == null)
            .Select(s => s.Id)
            .ToList();
    }

    private static int FloorDiv(int a, int b) => (int)Math.Floor((double)a / b);
}
