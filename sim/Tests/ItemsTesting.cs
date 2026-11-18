using Stillwake.Sim.Items;

namespace Stillwake.Sim.Tests;

/// <summary>packages/sim/src/testing/items.ts: items put straight into a world, as no command makes them out of nothing.</summary>
public static class ItemsTesting
{
    private static ItemsState StateOf(Scenario w) => (ItemsState)w.M.Items.Slice;

    /// <summary>Puts <paramref name="cost"/> into <paramref name="peer"/>'s inventory, pairing it first (<see cref="Scenario.Pair"/>); throws when it does not fit.</summary>
    public static Scenario GiveItems(this Scenario w, int peer, params (string Item, int Count)[] cost)
    {
        w.Pair(peer);
        var state = StateOf(w);
        var slots = Stacks.Placed(Stacks.InventoryOf(state, peer), cost, Content.Inventory.Slots)
            ?? throw new InvalidOperationException($"peer {peer}'s inventory has no room for that");
        Stacks.SetInventory(state, peer, slots);
        return w;
    }

    /// <summary>Puts one <paramref name="item"/> on <paramref name="peer"/> in the first empty equipment slot of its kind: a jetpack on its back.</summary>
    public static Scenario Wear(this Scenario w, int peer, string item)
    {
        var state = StateOf(w);
        var slots = Stacks.InventoryOf(state, peer).ToArray();
        var kind = Content.Items.Get(item).Equip ?? throw new InvalidOperationException($"{item} is not worn");
        int at = Enumerable.Range(0, Content.Inventory.Equipment.Count)
            .First(i => Content.Inventory.Equipment[i] == kind && slots[Content.Inventory.Slots + i] == null);
        slots[Content.Inventory.Slots + at] = new Stack(item, 1);
        Stacks.SetInventory(state, peer, slots);
        return w;
    }

    /// <summary>Fills machine <paramref name="id"/>'s empty slots with <paramref name="cost"/>; throws when it does not fit.</summary>
    public static Scenario FillCrate(this Scenario w, int id, params (string Item, int Count)[] cost)
    {
        var state = StateOf(w);
        var slots = Stacks.Placed(w.M.Items.Crate(id), cost)
            ?? throw new InvalidOperationException($"machine {id} has no room for that");
        Stacks.SetCrate(state, id, slots);
        return w;
    }

    /// <summary>Puts <paramref name="stack"/> in slot <paramref name="slot"/> of <paramref name="peer"/>'s inventory, or of machine <paramref name="crate"/>: a battery holding some charge.</summary>
    public static Scenario Put(this Scenario w, int slot, Stack stack, int peer = 0, int? crate = null)
    {
        if (crate == null) w.Pair(peer);
        var state = StateOf(w);
        var slots = (crate is int id ? w.M.Items.Crate(id) : Stacks.InventoryOf(state, peer)).ToArray();
        slots[slot] = stack;
        if (crate is int c) Stacks.SetCrate(state, c, slots);
        else Stacks.SetInventory(state, peer, slots);
        return w;
    }

    /// <summary>Puts <paramref name="peer"/>'s hand on the first hotbar slot holding <paramref name="item"/>, as its client's <c>Wield</c> would: a test's starting hand.</summary>
    public static Scenario Wields(this Scenario w, string item, int peer = 0)
    {
        var slots = w.M.Items.Inventory(peer);
        int slot = Enumerable.Range(0, Content.Inventory.Hotbar).FirstOrDefault(i => slots[i]?.Item == item, -1);
        if (slot < 0) throw new InvalidOperationException($"peer {peer} has no {item} on its hotbar");
        ItemsModule.SetHand(StateOf(w), peer, slot);
        return w;
    }

    /// <summary>The peers with an inventory kept, and the machines with an entry, in the order the slice keeps them.</summary>
    public static (int[] Inventories, int[] Crates) Stored(this Scenario w)
    {
        var state = StateOf(w);
        return (state.Inventories.Select(h => h.Id).ToArray(), state.Crates.Select(h => h.Id).ToArray());
    }
}
