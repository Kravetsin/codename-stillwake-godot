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
/// out in space, where a pick-up is heard as the glove's thump; whether it is passed out, when it picks up
/// nothing, so it never takes back the body it has just left (0061). What a drop is thrown from, and what it
/// walks over and picks up.
/// </summary>
public sealed record HeroSpot(int X, int Y, int Facing, int Cell, bool Floats, bool Out = false);

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
    /// <paramref name="y"/>), picks up as it goes (pickup.ts): those in any cell its body overlaps, and those nobody dropped
    /// within <see cref="Floor.PickUpReach"/> cells of it, pulled in (2026-10-29-kill-feel), and a body's within
    /// <see cref="Floor.BodyReach"/>, all it holds (2026-10-29-body-bag); unless one is <see cref="Hidden"/>, in
    /// <paramref name="taking"/>, picked up already with the pick-up still in flight, or the sim would refuse it (its own,
    /// dropped a moment ago; no room for any).
    /// </summary>
    public static IReadOnlyList<int> PickUpsOf(IItemsQueries items, int width, int peer, int x, int y, IReadOnlySet<int> taking)
    {
        int half = Hero.Width / 2;
        int left = FloorDiv(x - half, Unit), right = FloorDiv(x + half - 1, Unit);
        int top = FloorDiv(y - Hero.Height, Unit), bottom = FloorDiv(y - 1, Unit);
        int reach = Floor.PickUpReach;
        bool Within(FloorStack s, int by) =>
            s.Cell % width >= left - by && s.Cell % width <= right + by && s.Cell / width >= top - by && s.Cell / width <= bottom + by;
        return items.Floor
            .Where(s => Within(s, 0) || Pulled(s) && Within(s, reach) || s.Body != null && Within(s, Floor.BodyReach))
            .Where(s => !Hidden(s, items.Ticks) && !taking.Contains(s.Id) && items.PickUpProblem(peer, s.Id) == null)
            .Select(s => s.Id)
            .ToList();
    }

    /// <summary>Whether the magnet pulls <paramref name="s"/> in: loot nobody dropped, never a stack let go for a friend nor a hero's body.</summary>
    public static bool Pulled(FloorStack s) => s.By == FloorStack.Nobody && s.Body == null;

    /// <summary>How many ticks a killed creature is held white before it bursts: <see cref="KillLooks.PopMs"/>, in whole ticks.</summary>
    public static readonly int PopTicks = (int)Math.Ceiling(KillLooks.PopMs * Sim.World.TicksPerSecond / 1000);

    /// <summary>
    /// Whether <paramref name="s"/> is loot not yet out of the creature it sprays from, the items' clock at <paramref name="ticks"/>:
    /// nobody's, younger than <see cref="PopTicks"/>, drawn and taken only once its creature has burst (2026-10-29-kill-feel).
    /// </summary>
    public static bool Hidden(FloorStack s, int ticks) => Pulled(s) && ticks - s.At < PopTicks;

    private static int FloorDiv(int a, int b) => (int)Math.Floor((double)a / b);
}
