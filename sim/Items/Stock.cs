using Stillwake.Content;

namespace Stillwake.Sim.Items;

/// <summary>
/// The stock (craft-from-crates): what a peer carries in its own slots, then every crate on its surface by machine id,
/// however far. What a paid craft takes and a cancel gives back.
/// </summary>
public sealed partial class ItemsModule
{
    /// <summary>How much of each item <paramref name="peer"/>'s stock holds, its own slots first, in the order first found.</summary>
    public IReadOnlyList<(string Item, int Count)> StockTotals(int peer)
    {
        var totals = new List<(string Item, int Count)>(Totals(peer));
        foreach (var crate in _grid.MachinesWith(MachineTrait.Container))
            foreach (var s in Crate(crate.Id))
                if (s != null) Add(totals, s.Item, s.Count);
        return totals;
    }

    /// <summary><c>items.notEnough</c> when <paramref name="peer"/>'s stock cannot pay <paramref name="cost"/>; never in creative mode.</summary>
    public string? ShortOfStock(int peer, IReadOnlyList<(string Item, int Count)> cost)
    {
        var totals = StockTotals(peer);
        var needs = new List<(string Item, int Count)>();
        foreach (var (item, amount) in Stacks.Entries(cost)) Add(needs, item, amount);
        foreach (var (item, amount) in needs)
            if (CountIn(totals, item) < amount) return Creative(peer) ? null : "items.notEnough";
        return null;
    }

    /// <summary><c>items.full</c> when <paramref name="cost"/> does not fit into <paramref name="peer"/>'s stock, creative mode or not.</summary>
    public string? FullForStock(int peer, IReadOnlyList<(string Item, int Count)> cost) =>
        Spread(peer, cost) == null ? "items.full" : null;

    /// <summary>
    /// Takes <paramref name="cost"/> from <paramref name="peer"/>'s stock, its own slots as <see cref="Take"/> does, then each
    /// crate by id, and returns what it took: all of it, a creative peer's taken from nothing as <see cref="Take"/> does. Check <see cref="ShortOfStock"/> first.
    /// </summary>
    public IReadOnlyList<(string Item, int Count)> TakeStock(int peer, IReadOnlyList<(string Item, int Count)> cost)
    {
        if (Stacks.IsNothing(cost)) return [];
        if (Creative(peer)) return [.. cost];
        var left = new List<(string Item, int Count)>(Stacks.Entries(cost));
        var inventory = Inventory(peer);
        if (TakenPart(inventory, left, Own(peer)) is { } slots) Stacks.SetInventory(State, peer, slots);
        foreach (var crate in _grid.MachinesWith(MachineTrait.Container))
        {
            if (Stacks.IsNothing(left)) break;
            var held = Crate(crate.Id);
            if (TakenPart(held, left, held.Count) is { } taken) Stacks.SetCrate(State, crate.Id, taken);
        }
        if (!Stacks.IsNothing(left)) throw new InvalidOperationException("items.takeStock: not enough; check ShortOfStock first");
        return [.. cost];
    }

    /// <summary>Gives <paramref name="cost"/> to <paramref name="peer"/>'s stock, its own slots first, in creative mode too. Check <see cref="FullForStock"/> first.</summary>
    public void GiveStock(int peer, IReadOnlyList<(string Item, int Count)> cost)
    {
        if (Stacks.IsNothing(cost)) return;
        var (inventory, crates) = Spread(peer, cost)
            ?? throw new InvalidOperationException("items.giveStock: no room; check FullForStock first");
        Stacks.SetInventory(State, peer, inventory);
        foreach (var (id, slots) in crates) Stacks.SetCrate(State, id, slots);
    }

    /// <summary>
    /// <paramref name="peer"/>'s own slots and the crates that change, with <paramref name="cost"/> placed into its own
    /// slots as far as it fits, the rest into each crate by id; null when not all of it fits.
    /// </summary>
    private (Stack?[] Inventory, List<(int Id, Stack?[] Slots)> Crates)? Spread(int peer, IReadOnlyList<(string Item, int Count)> cost)
    {
        var (inventory, left) = Stacks.PlacedSome(Inventory(peer), cost, Own(peer));
        var crates = new List<(int Id, Stack?[] Slots)>();
        foreach (var crate in _grid.MachinesWith(MachineTrait.Container))
        {
            if (Stacks.IsNothing(left)) break;
            var (slots, rest) = Stacks.PlacedSome(Crate(crate.Id), left, null);
            if (rest.Sum(r => r.Count) < left.Sum(l => l.Count)) crates.Add((crate.Id, slots));
            left = rest;
        }
        return Stacks.IsNothing(left) ? (inventory, crates) : null;
    }

    /// <summary>
    /// <paramref name="slots"/> with as much of <paramref name="left"/> taken out as their first <paramref name="usable"/>
    /// hold, <paramref name="left"/> lessened by it; null when they hold none of it.
    /// </summary>
    private static Stack?[]? TakenPart(IReadOnlyList<Stack?> slots, List<(string Item, int Count)> left, int usable)
    {
        var part = new List<(string Item, int Count)>();
        for (int i = 0; i < left.Count; i++)
        {
            var (item, amount) = left[i];
            int n = Math.Min(amount, CountIn(slots, item, usable) - CountIn(part, item));
            if (n <= 0) continue;
            part.Add((item, n));
            left[i] = (item, amount - n);
        }
        return part.Count == 0 ? null : Stacks.Taken(slots, part, usable);
    }

    private static int CountIn(IReadOnlyList<Stack?> slots, string item, int usable)
    {
        int n = 0;
        for (int i = 0; i < Math.Min(usable, slots.Count); i++)
            if (slots[i] is { } s && s.Item == item) n += s.Count;
        return n;
    }

    private static int CountIn(IReadOnlyList<(string Item, int Count)> totals, string item)
    {
        int n = 0;
        foreach (var t in totals) if (t.Item == item) n += t.Count;
        return n;
    }

    private static void Add(List<(string Item, int Count)> totals, string item, int count)
    {
        int at = totals.FindIndex(t => t.Item == item);
        if (at < 0) totals.Add((item, count));
        else totals[at] = (item, totals[at].Count + count);
    }
}
