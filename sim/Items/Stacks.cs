using Stillwake.Content;

namespace Stillwake.Sim.Items;

/// <summary>
/// The rules on slots (packages/sim/src/modules/items/stacks.ts): giving, taking and moving. Stacks are
/// immutable records, so a copy of the slots is a copy of the array.
/// </summary>
internal static class Stacks
{
    private static readonly IReadOnlyList<Stack?> NoInventory = Array.AsReadOnly(new Stack?[Inventory.Total]);

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

    /// <summary>All-empty slots by their count, shared by every world: read-only collections, so no caller can write them.</summary>
    private static readonly IReadOnlyList<Stack?>[] NoneHeld =
        [.. Enumerable.Range(0, Machines.All.Max(m => m.Slots ?? 0) + 1).Select(n => Array.AsReadOnly(new Stack?[n]))];

    /// <summary>What machine <paramref name="id"/>, of <paramref name="slots"/> slots, holds: all empty when it holds nothing.</summary>
    public static IReadOnlyList<Stack?> CrateOf(ItemsState state, int id, int slots)
    {
        int at = IndexOf(state.Crates, id);
        return at >= 0 ? state.Crates[at].Slots : NoneHeld[slots];
    }

    /// <summary>Where holder <paramref name="id"/> is in <paramref name="held"/>, a list in id order; the complement of where it would go when absent.</summary>
    private static int IndexOf(List<Held> held, int id)
    {
        int lo = 0, hi = held.Count - 1;
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) >> 1);
            if (held[mid].Id == id) return mid;
            if (held[mid].Id < id) lo = mid + 1;
            else hi = mid - 1;
        }
        return ~lo;
    }

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
        int at = IndexOf(state.Crates, id);
        bool holds = slots.Any(s => s != null);
        if (at >= 0)
        {
            if (holds) state.Crates[at].Slots = slots;
            else state.Crates.RemoveAt(at);
        }
        else if (holds) state.Crates.Insert(~at, new Held { Id = id, Slots = slots });
    }

    private static Stack?[] Copy(IReadOnlyList<Stack?> slots) => [.. slots];

    /// <summary>
    /// Whether two stacks are of one item and hold alike of their own (<see cref="Stack.Charge"/>, <see cref="Stack.Module"/>):
    /// only those merge, onto a slot or on the floor; a move of one onto the other otherwise swaps them.
    /// </summary>
    public static bool Alike(Stack a, Stack b) => a.Alike(b);

    /// <summary>
    /// Adds <paramref name="count"/> of <paramref name="like"/>, its own count aside, to the first
    /// <paramref name="usable"/> of <paramref name="slots"/> in place: onto the stacks alike in slot order, then
    /// into empty slots, each a copy of it, so what its item holds of its own goes with it. What did not fit.
    /// </summary>
    private static int Fill(Stack?[] slots, Stack like, int count, int usable)
    {
        int limit = Content.Items.Get(like.Item).Stack;
        int left = count, end = Math.Min(usable, slots.Length);
        for (int i = 0; i < end && left > 0; i++)
        {
            var s = slots[i];
            if (s == null || !Alike(s, like) || s.Count >= limit) continue;
            int n = Math.Min(left, limit - s.Count);
            slots[i] = s with { Count = s.Count + n };
            left -= n;
        }
        for (int i = 0; i < end && left > 0; i++)
        {
            if (slots[i] != null) continue;
            int n = Math.Min(left, limit);
            slots[i] = like with { Count = n };
            left -= n;
        }
        return left;
    }

    /// <summary>
    /// <paramref name="slots"/> with <paramref name="cost"/> added, or null when it does not fit: onto
    /// the stacks of each item in slot order up to their limit, then into the empty slots in slot order;
    /// only into the first <paramref name="usable"/> slots, an inventory's own (<see cref="Inventory.Slots"/>),
    /// all of them with none.
    /// </summary>
    public static Stack?[]? Placed(IReadOnlyList<Stack?> slots, IReadOnlyList<(string Item, int Count)> cost, int? usable = null)
    {
        var into = Copy(slots);
        foreach (var (item, amount) in Entries(cost))
            if (Fill(into, new Stack(item, amount), amount, usable ?? into.Length) > 0) return null;
        return into;
    }

    /// <summary>
    /// <paramref name="slots"/> with as much of <paramref name="cost"/> added as fits, as <see cref="Placed"/>
    /// adds it, and what did not fit: what falls to the floor when an inventory is full.
    /// </summary>
    public static (Stack?[] Slots, List<(string Item, int Count)> Left) PlacedSome(
        IReadOnlyList<Stack?> slots, IReadOnlyList<(string Item, int Count)> cost, int? usable = null)
    {
        var into = Copy(slots);
        var left = new List<(string, int)>();
        foreach (var (item, amount) in Entries(cost))
        {
            int over = Fill(into, new Stack(item, amount), amount, usable ?? into.Length);
            if (over > 0) left.Add((item, over));
        }
        return (into, left);
    }

    /// <summary>
    /// <paramref name="slots"/> with as much of <paramref name="stack"/> added as fits, as <see cref="Placed"/>
    /// adds a cost but each copy keeping what its item holds of its own, and how many did not fit: a pick-up.
    /// </summary>
    public static (Stack?[] Slots, int Left) PlacedSome(IReadOnlyList<Stack?> slots, Stack stack, int? usable = null)
    {
        var into = Copy(slots);
        int left = Fill(into, stack, stack.Count, usable ?? into.Length);
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
    /// Taken from the last slots first, so the first row, the hotbar, is the last to empty, or with
    /// <paramref name="firstFirst"/> from the first, as a machine takes from its inputs; only from the first
    /// <paramref name="usable"/> slots, as <see cref="Placed"/> gives.
    /// </summary>
    public static Stack?[]? Taken(IReadOnlyList<Stack?> slots, IReadOnlyList<(string Item, int Count)> cost, int? usable = null, bool firstFirst = false)
    {
        var from = Copy(slots);
        int end = Math.Min(usable ?? from.Length, from.Length);
        foreach (var (item, amount) in Entries(cost))
        {
            int left = amount;
            for (int k = 0; k < end && left > 0; k++)
            {
                int i = firstFirst ? k : end - 1 - k;
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
    /// no slot onto that side's stacks of the item and then its empty slots, in slot order, its first
    /// <paramref name="usable"/> only (an inventory's own). Both sides are one array when <paramref name="same"/>.
    /// Slots and crates are checked by the caller, and what equipment slots may hold (<see cref="Misfit"/>).
    /// </summary>
    public static string? Moved(
        IReadOnlyList<Stack?> fromSlots, IReadOnlyList<Stack?> toSlots, bool same, Place src, Target dst, int count,
        out Stack?[] from, out Stack?[] to, int? usable = null)
    {
        from = Copy(fromSlots);
        to = same ? from : Copy(toSlots);
        var s = from[src.Slot];
        if (s == null || count < 1) return "items.empty";
        int n = Math.Min(count, s.Count);
        int kept = s.Count;
        if (dst.Slot is not int slot)
        {
            int left = Fill(to, s, n, usable ?? to.Length);
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
            else if (Alike(d, s))
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

    /// <summary>
    /// <paramref name="slots"/> with those from <paramref name="start"/> up to <paramref name="end"/> sorted (0008):
    /// stacks alike (<see cref="Alike"/>) merged up to their limit, the full ones first, then laid from
    /// <paramref name="start"/> in order of <see cref="ItemGroup"/>, item id (ordinal), module (ordinal, none first), charge,
    /// fullest first, and count, largest first; empty slots after. Every item, charge and module kept; the other slots untouched.
    /// </summary>
    public static Stack?[] Sorted(IReadOnlyList<Stack?> slots, int start, int end)
    {
        var merged = new List<(Stack Like, int Count)>();
        for (int i = start; i < end; i++)
        {
            if (slots[i] is not { } s) continue;
            int at = merged.FindIndex(m => Alike(m.Like, s));
            if (at < 0) merged.Add((s, s.Count));
            else merged[at] = (merged[at].Like, merged[at].Count + s.Count);
        }
        var stacks = new List<Stack>();
        foreach (var (like, count) in merged)
        {
            int limit = Content.Items.Get(like.Item).Stack;
            for (int left = count; left > 0; left -= limit) stacks.Add(like with { Count = Math.Min(left, limit) });
        }
        stacks.Sort(SortOrder);
        var sorted = Copy(slots);
        for (int i = start; i < end; i++) sorted[i] = i - start < stacks.Count ? stacks[i - start] : null;
        return sorted;
    }

    /// <summary>A total order of stacks: only stacks equal in every field tie, and those are interchangeable.</summary>
    private static int SortOrder(Stack a, Stack b)
    {
        int order = Content.Items.Get(a.Item).Group.CompareTo(Content.Items.Get(b.Item).Group);
        if (order == 0) order = string.CompareOrdinal(a.Item, b.Item);
        // A tool with no module first, then by its module, ordinal: null sorts before any string.
        if (order == 0) order = string.CompareOrdinal(a.Module, b.Module);
        if (order == 0) order = b.Charge.CompareTo(a.Charge);
        if (order == 0) order = b.Count.CompareTo(a.Count);
        return order;
    }

    /// <summary>
    /// <c>items.notEquipment</c> when an equipment slot of <paramref name="inventory"/>, an inventory's
    /// slots, holds an item not worn in its kind of slot (<c>ItemSpec.Equip</c>); null when each holds its own.
    /// </summary>
    public static string? Misfit(IReadOnlyList<Stack?> inventory)
    {
        for (int i = 0; i < Inventory.Equipment.Count; i++)
            if (inventory[Inventory.Slots + i] is { } s && Content.Items.Get(s.Item).Equip != Inventory.Equipment[i])
                return "items.notEquipment";
        return null;
    }

    /// <summary>
    /// Why a machine of <paramref name="spec"/>, its slots <paramref name="slots"/>, holds what it does not take: a charging
    /// station only items with a <c>Capacity</c> (<c>items.notBattery</c>); an arc bench's first slot only a tool a module
    /// fits (<c>items.notTool</c>, 0099). Null when each holds what it may.
    /// </summary>
    public static string? Unfit(MachineSpec spec, IReadOnlyList<Stack?> slots)
    {
        if (spec.Tunes) return slots.Count > 0 && slots[0] is { } tool && !Modules.Hosted(tool.Item) ? "items.notTool" : null;
        if (!spec.Charges) return null;
        foreach (var s in slots)
            if (s != null && Content.Items.Get(s.Item).Capacity == null) return "items.notBattery";
        return null;
    }
}
