using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Content;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// Where this player's hero stands, as crew last moved it: its feet (<paramref name="X"/>,
/// <paramref name="Y"/>) in thousandths of a cell, the way it faces, its body's cell, and which cells
/// its hands reach. What a drop is thrown from, what it walks over and picks up, which crates it opens.
/// </summary>
public sealed record HeroSpot(int X, int Y, int Facing, int Cell, Func<int, bool> Reaches);

/// <summary>What the hero's hands reach of items: crates to open, stacks on the floor to pick up.</summary>
public static class Reach
{
    private const int Unit = 1000;

    /// <summary>Whether a machine of <paramref name="kind"/> holds items, opened with E: a crate, a furnace, an analyzer.</summary>
    public static bool HoldsItems(string kind) => Machines.TryGet(kind, out var spec) && spec.Slots != null;

    /// <summary>The machine holding items at <paramref name="cell"/>, which E opens; none elsewhere (aim.ts).</summary>
    public static int? CrateAt(IGridQueries grid, int cell) =>
        grid.MachineAt(cell) is { } m && HoldsItems(m.Kind) ? m.Id : null;

    /// <summary>Whether crate <paramref name="id"/> still stands with a cell of it within <paramref name="reaches"/> (<c>crateInReach</c>).</summary>
    public static bool CrateInReach(IGridQueries grid, int id, Func<int, bool> reaches)
    {
        var crate = grid.Machines.FirstOrDefault(m => m.Id == id && HoldsItems(m.Kind));
        if (crate == null) return false;
        var spec = Machines.Get(crate.Kind);
        for (int dy = 0; dy < spec.Height; dy++)
            for (int dx = 0; dx < spec.Width; dx++)
                if (reaches(crate.Cell + dy * grid.Width + dx)) return true;
        return false;
    }

    /// <summary>The stacks on the floor this peer has a pick-up in flight for: gone from its view at once (0014).</summary>
    public static IReadOnlySet<int> PickingUp(IReadOnlyList<Command> inFlight) =>
        inFlight.OfType<PickUp>().Select(p => p.Id).ToHashSet();

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
