using Stillwake.Content;

namespace Stillwake.Sim.Items;

public sealed partial class ItemsModule
{
    // Machine to machine, for drones (0062): no hero's inventory, and no slot named but the one taken from.

    /// <summary>
    /// How much of <paramref name="item"/> machine <paramref name="id"/> holds where a delivery of it goes
    /// (<see cref="Receiving"/>): its fuel slot for a fuel, its other inputs for anything else, a crate's every slot.
    /// </summary>
    public int HeldFor(int id, string item)
    {
        if (HolderOf(id) is not { } spec) return 0;
        var slots = Crate(id);
        int held = 0;
        foreach (int i in Receiving(spec, item))
            if (slots[i] is { } s && s.Item == item) held += s.Count;
        return held;
    }

    /// <summary>How many of <paramref name="stack"/> a <see cref="PutIn"/> would put into machine <paramref name="id"/> now.</summary>
    public int RoomFor(int id, Stack stack) => HolderOf(id) is { } spec ? stack.Count - Filled(id, spec, Crate(id), stack, out _) : 0;

    /// <summary>
    /// Takes up to <paramref name="count"/> from slot <paramref name="slot"/> of machine <paramref name="id"/>, any slot,
    /// outputs too, and returns what it took with all it holds of its own; null when the slot holds nothing.
    /// In creative mode too: nothing is made or lost, only moved.
    /// </summary>
    public Stack? TakeFrom(int id, int slot, int count)
    {
        var slots = Crate(id);
        if (slot < 0 || slot >= slots.Count || Stacks.TakenFrom(slots, slot, count, out var left, out var taken) != null) return null;
        SetMachine(id, left);
        return taken;
    }

    /// <summary>
    /// Puts as much of <paramref name="stack"/> as fits into machine <paramref name="id"/> as a whole, where
    /// <see cref="Receiving"/> says, onto stacks alike in slot order and then into empty slots, one remembering this item first,
    /// one remembering another last; returns how many did not fit.
    /// </summary>
    public int PutIn(int id, Stack stack)
    {
        if (HolderOf(id) is not { } spec) return stack.Count;
        int left = Filled(id, spec, Crate(id), stack, out var slots);
        SetMachine(id, slots);
        return left;
    }

    /// <summary>Lays <paramref name="stack"/> on cell <paramref name="cell"/>, still, with all it holds of its own, for anyone to pick up at once.</summary>
    public void LayDown(Stack stack, int cell)
    {
        if (stack.Count > 0) FloorRules.LayDown(State, stack, cell, 0, _grid.Width, FloorStack.Nobody);
    }

    /// <summary>
    /// The slots of a machine of <paramref name="spec"/> a delivery of <paramref name="item"/> goes into: a fuel only its
    /// fuel slot, anything else its inputs but the fuel slot, so a furnace's fuel and what it smelts never mix; a
    /// machine with no inputs listed (a crate, a bay) all its slots; a charging station nothing without a capacity; one that
    /// takes one item nothing else.
    /// </summary>
    private IEnumerable<int> Receiving(MachineSpec spec, string item)
    {
        var what = Content.Items.Get(item);
        if (spec.Charges && what.Capacity == null) return [];
        if (spec.Takes is { } only && item != only) return [];
        int inputs = InputsOf(spec);
        if (spec.FuelSlot is not int fuel) return Enumerable.Range(0, inputs);
        return what.BurnTicks != null ? [fuel] : Enumerable.Range(0, inputs).Where(i => i != fuel);
    }

    /// <summary><paramref name="slots"/> with as much of <paramref name="stack"/> as fits put in, as <see cref="PutIn"/> puts it, and how many did not fit.</summary>
    private int Filled(int id, MachineSpec spec, IReadOnlyList<Stack?> slots, Stack stack, out Stack?[] into)
    {
        into = [.. slots];
        int limit = Content.Items.Get(stack.Item).Stack, left = stack.Count;
        var receiving = Receiving(spec, stack.Item).ToList();
        foreach (int i in receiving)
        {
            if (left == 0) break;
            if (into[i] is not { } s || !Stacks.Alike(s, stack) || s.Count >= limit) continue;
            int n = Math.Min(left, limit - s.Count);
            into[i] = s with { Count = s.Count + n };
            left -= n;
        }
        // A slot seeded with another item keeps it (0062): written into, it would remember this one instead.
        foreach (int i in receiving.OrderBy(i => KeptAt(id, i) is not { } kept ? 1 : kept == stack.Item ? 0 : 2))
        {
            if (left == 0) break;
            if (into[i] != null) continue;
            int n = Math.Min(left, limit);
            into[i] = stack with { Count = n };
            left -= n;
        }
        return left;
    }
}
