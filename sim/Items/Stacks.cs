using Stillwake.Content;

namespace Stillwake.Sim.Items;

/// <summary>
/// The rules on slots (packages/sim/src/modules/items/stacks.ts): giving, taking and moving. Stacks are
/// immutable records, so a copy of the slots is a copy of the array.
/// </summary>
internal static class Stacks
{
    private static readonly Stack?[] NoInventory = new Stack?[Inventory.Slots];

    /// <summary>The items of <paramref name="cost"/> with their amounts; a negative amount is a caller's bug.</summary>
    public static IReadOnlyList<(string Item, int Count)> Entries(IReadOnlyList<(string Item, int Count)> cost)
    {
        foreach (var (item, amount) in cost)
            if (amount < 0) throw new ArgumentException($"items: {amount} {item} is not a whole amount");
        return cost;
    }

    /// <summary>Whether <paramref name="cost"/> names nothing above zero.</summary>
    public static bool IsNothing(IReadOnlyList<(string Item, int Count)> cost) => Entries(cost).All(e => e.Count == 0);

    /// <summary><paramref name="peer"/>'s inventory: empty until its first change.</summary>
    public static IReadOnlyList<Stack?> InventoryOf(ItemsState state, int peer) =>
        state.Inventories.Find(h => h.Id == peer)?.Slots ?? NoInventory;

    /// <summary>What machine <paramref name="id"/>, of <paramref name="slots"/> slots, holds: all empty when it holds nothing.</summary>
    public static IReadOnlyList<Stack?> CrateOf(ItemsState state, int id, int slots) =>
        state.Crates.Find(h => h.Id == id)?.Slots ?? new Stack?[slots];

    /// <summary>Replaces <paramref name="peer"/>'s inventory, keeping the inventories in peer order.</summary>
    public static void SetInventory(ItemsState state, int peer, Stack?[] slots)
    {
        int at = state.Inventories.FindIndex(h => h.Id >= peer);
        if (at >= 0 && state.Inventories[at].Id == peer) state.Inventories[at].Slots = slots;
        else state.Inventories.Insert(at < 0 ? state.Inventories.Count : at, new Held { Id = peer, Slots = slots });
    }

    /// <summary>Replaces what machine <paramref name="id"/> holds, keeping them in id order and none that hold nothing.</summary>
    public static void SetCrate(ItemsState state, int id, Stack?[] slots)
    {
        int at = state.Crates.FindIndex(h => h.Id >= id);
        bool holds = slots.Any(s => s != null);
        if (at >= 0 && state.Crates[at].Id == id)
        {
            if (holds) state.Crates[at].Slots = slots;
            else state.Crates.RemoveAt(at);
        }
        else if (holds) state.Crates.Insert(at < 0 ? state.Crates.Count : at, new Held { Id = id, Slots = slots });
    }

    private static Stack?[] Copy(IReadOnlyList<Stack?> slots) => [.. slots];

    /// <summary>Adds <paramref name="count"/> of <paramref name="item"/> to <paramref name="slots"/> in place: onto its stacks in slot order, then into empty slots. What did not fit.</summary>
    private static int Fill(Stack?[] slots, string item, int count)
    {
        int limit = Content.Items.Get(item).Stack;
        int left = count;
        for (int i = 0; i < slots.Length && left > 0; i++)
        {
            var s = slots[i];
            if (s == null || s.Item != item || s.Count >= limit) continue;
            int n = Math.Min(left, limit - s.Count);
            slots[i] = s with { Count = s.Count + n };
            left -= n;
        }
        for (int i = 0; i < slots.Length && left > 0; i++)
        {
            if (slots[i] != null) continue;
            int n = Math.Min(left, limit);
            slots[i] = new Stack(item, n);
            left -= n;
        }
        return left;
    }

    /// <summary>
    /// <paramref name="slots"/> with <paramref name="cost"/> added, or null when it does not fit: onto
    /// the stacks of each item in slot order up to their limit, then into the empty slots in slot order.
    /// </summary>
    public static Stack?[]? Placed(IReadOnlyList<Stack?> slots, IReadOnlyList<(string Item, int Count)> cost)
    {
        var into = Copy(slots);
        foreach (var (item, amount) in Entries(cost))
            if (Fill(into, item, amount) > 0) return null;
        return into;
    }

    /// <summary>
    /// <paramref name="slots"/> with as much of <paramref name="cost"/> added as fits, as <see cref="Placed"/>
    /// adds it, and what did not fit: what falls to the floor when an inventory is full.
    /// </summary>
    public static (Stack?[] Slots, List<(string Item, int Count)> Left) PlacedSome(
        IReadOnlyList<Stack?> slots, IReadOnlyList<(string Item, int Count)> cost)
    {
        var into = Copy(slots);
        var left = new List<(string, int)>();
        foreach (var (item, amount) in Entries(cost))
        {
            int over = Fill(into, item, amount);
            if (over > 0) left.Add((item, over));
        }
        return (into, left);
    }

    /// <summary>
    /// <paramref name="slots"/> with up to <paramref name="count"/> taken from slot <paramref name="slot"/>,
    /// and what was taken; or why not: nothing there, or a count below 1. The caller checks the slot is in range.
    /// </summary>
    public static string? TakenFrom(IReadOnlyList<Stack?> slots, int slot, int count, out Stack?[] left, out Stack taken)
    {
        left = Copy(slots);
        taken = null!;
        var s = left[slot];
        if (s == null || count < 1) return "items.empty";
        int n = Math.Min(count, s.Count);
        left[slot] = s.Count == n ? null : s with { Count = s.Count - n };
        taken = s with { Count = n };
        return null;
    }

    /// <summary>
    /// <paramref name="slots"/> with <paramref name="cost"/> taken out, or null when they hold too little.
    /// Taken from the last slots first, so the first row, the hotbar, is the last to empty.
    /// </summary>
    public static Stack?[]? Taken(IReadOnlyList<Stack?> slots, IReadOnlyList<(string Item, int Count)> cost)
    {
        var from = Copy(slots);
        foreach (var (item, amount) in Entries(cost))
        {
            int left = amount;
            for (int i = from.Length - 1; i >= 0 && left > 0; i--)
            {
                var s = from[i];
                if (s == null || s.Item != item) continue;
                int n = Math.Min(left, s.Count);
                left -= n;
                from[i] = s.Count == n ? null : s with { Count = s.Count - n };
            }
            if (left > 0) return null;
        }
        return from;
    }

    /// <summary>
    /// Moves the lesser of <paramref name="count"/>, what <paramref name="src"/> holds and what fits to
    /// <paramref name="dst"/>: onto a slot, where a whole stack onto another item swaps with it, or with
    /// no slot onto that side's stacks of the item and then its empty slots, in slot order. Both sides
    /// are one array when <paramref name="same"/>. Slots and crates are checked by the caller.
    /// </summary>
    public static string? Moved(
        IReadOnlyList<Stack?> fromSlots, IReadOnlyList<Stack?> toSlots, bool same, Place src, Target dst, int count,
        out Stack?[] from, out Stack?[] to)
    {
        from = Copy(fromSlots);
        to = same ? from : Copy(toSlots);
        var s = from[src.Slot];
        if (s == null || count < 1) return "items.empty";
        int n = Math.Min(count, s.Count);
        int kept = s.Count;
        if (dst.Slot is not int slot)
        {
            int left = Fill(to, s.Item, n);
            if (left == n) return "items.full";
            kept -= n - left;
        }
        else
        {
            var d = to[slot];
            if (d == null)
            {
                to[slot] = s with { Count = n };
                kept -= n;
            }
            else if (d.Item == s.Item)
            {
                int k = Math.Min(n, Content.Items.Get(d.Item).Stack - d.Count);
                if (k == 0) return "items.full";
                to[slot] = d with { Count = d.Count + k };
                kept -= k;
            }
            else
            {
                if (n < s.Count) return "items.full";
                from[src.Slot] = d;
                to[slot] = s;
                return null;
            }
        }
        from[src.Slot] = kept == 0 ? null : s with { Count = kept };
        return null;
    }
}
