using System;
using System.Collections.Generic;
using Stillwake.Content;

namespace Stillwake.Sim.Items;

/// <summary>
/// Puts every bag stack (own slots past the hotbar) whose item is not in <c>Keep</c> into the crates within
/// <see cref="Content.Inventory.StashReach"/> of <c>Cell</c> that already hold its item, nearest first (2026-11-06-quick-stash).
/// <c>Cell</c> is the sender's hero's, as its client reports it; <c>Keep</c> its favourites. Never refused: nothing to stash moves nothing.
/// </summary>
public sealed record Stash(int Cell, IReadOnlyList<string> Keep) : Command;

/// <summary>One part of a stash: <paramref name="Count"/> of <paramref name="Item"/> from bag slot <paramref name="Slot"/> into crate <paramref name="Crate"/>.</summary>
public sealed record Stashed(int Slot, int Crate, string Item, int Count);

public sealed partial class ItemsModule
{
    private void ApplyStash(Stash c) => Stashes(c.From, c, side => SideOf(c.From, side), (side, slots) => SetSide(c.From, side, slots));

    /// <summary>
    /// What <paramref name="stash"/> by <paramref name="peer"/> would move after <paramref name="before"/>, the peer's own moves in
    /// flight: what the client flies at once. Nothing changes.
    /// </summary>
    public IReadOnlyList<Stashed> StashPlan(int peer, Stash stash, IReadOnlyList<Command> before)
    {
        var parts = new List<Stashed>();
        Fold(peer, [.. before, stash], parts);
        return parts;
    }

    /// <summary>
    /// Stashes as <see cref="Stash"/> says, reading each side through <paramref name="slotsOf"/> and writing each move through
    /// <paramref name="set"/> before the next is planned: the world's for the command, a fold's copies for <see cref="AfterMoves"/>.
    /// </summary>
    private List<Stashed> Stashes(int peer, Stash stash, Func<Target, IReadOnlyList<Stack?>> slotsOf, Action<Target, Stack?[]> set)
    {
        var parts = new List<Stashed>();
        var crates = CratesNear(stash.Cell);
        if (crates.Count == 0) return parts;
        var bag = new Target();
        for (int slot = Content.Inventory.Hotbar; slot < Own(peer); slot++)
        {
            foreach (int crate in crates)
            {
                if (slotsOf(bag)[slot] is not { } stack || stash.Keep.Contains(stack.Item)) break;
                var into = new Target(Crate: crate);
                if (!Holds(slotsOf(into), stack.Item)) continue;
                var src = new Place(slot);
                if (MoveSides(slotsOf, src, into, out var from, out var to, out bool same) != null) continue;
                if (Moved(peer, from, to, same, src, into, stack.Count, out var after, out var filled) != null) continue;
                set(bag, after);
                set(into, filled);
                parts.Add(new Stashed(slot, crate, stack.Item, stack.Count - (after[slot]?.Count ?? 0)));
            }
        }
        return parts;
    }

    private static bool Holds(IReadOnlyList<Stack?> slots, string item)
    {
        foreach (var s in slots)
            if (s?.Item == item) return true;
        return false;
    }

    /// <summary>The crates whose nearest cell lies within <see cref="Content.Inventory.StashReach"/> of <paramref name="cell"/>, through walls: nearest first, then by id.</summary>
    private List<int> CratesNear(int cell)
    {
        var near = new List<(long Distance, int Id)>();
        int width = _grid.Width;
        if (cell < 0 || cell >= width * _grid.Height) return [];
        int x = cell % width, y = cell / width;
        long reach = (long)Content.Inventory.StashReach * Content.Inventory.StashReach;
        foreach (var crate in _grid.MachinesWith(MachineTrait.Container))
        {
            long best = long.MaxValue;
            foreach (int c in _grid.Footprint(crate.Kind, crate.Cell) ?? [crate.Cell])
            {
                long dx = c % width - x, dy = c / width - y;
                best = Math.Min(best, dx * dx + dy * dy);
            }
            if (best <= reach) near.Add((best, crate.Id));
        }
        near.Sort();
        return near.ConvertAll(n => n.Id);
    }
}
