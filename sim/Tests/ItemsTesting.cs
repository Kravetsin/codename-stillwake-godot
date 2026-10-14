using Stillwake.Sim.Items;

namespace Stillwake.Sim.Tests;

/// <summary>packages/sim/src/testing/items.ts: items put straight into a world, as no command makes them out of nothing.</summary>
public static class ItemsTesting
{
    private static ItemsState StateOf(Scenario w) => (ItemsState)w.M.Items.Slice;

    /// <summary>Puts <paramref name="cost"/> into <paramref name="peer"/>'s inventory; throws when it does not fit.</summary>
    public static Scenario GiveItems(this Scenario w, int peer, params (string Item, int Count)[] cost)
    {
        var state = StateOf(w);
        var slots = Stacks.Placed(Stacks.InventoryOf(state, peer), cost)
            ?? throw new InvalidOperationException($"peer {peer}'s inventory has no room for that");
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

    /// <summary>The peers with an inventory kept, and the machines with an entry, in the order the slice keeps them.</summary>
    public static (int[] Inventories, int[] Crates) Stored(this Scenario w)
    {
        var state = StateOf(w);
        return (state.Inventories.Select(h => h.Id).ToArray(), state.Crates.Select(h => h.Id).ToArray());
    }
}
