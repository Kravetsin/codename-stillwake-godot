using Stillwake.Content;

namespace Stillwake.Sim.Items;

public sealed partial class ItemsModule
{
    /// <summary>
    /// Mostly moves, from the sender's own inventory or a crate holding something, so most are taken;
    /// now and then from an empty slot, a slot out of range, a crate that is not there, or onto a full
    /// or other stack, or into an equipment slot that does not wear it, so every refusal is drawn too; now
    /// and then a battery into a charging station or onto the belt (<see cref="ToStationOrBelt"/>).
    /// Creative mode switches now and then, mostly off. Now and then a drop onto a cell, off the grid
    /// too, or a pick-up of a stack on the floor or of one long gone.
    /// </summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        if (rng.Below(10) == 0) return new SetCreative(rng.Below(4) == 0) { From = from };
        var own = State.Inventories.Find(h => h.Id == from)?.Slots;
        // Mostly a machine that holds something; the wreck's other machines and missing ids otherwise.
        int AnyCrate() => State.Crates.Count > 0 && rng.Below(4) != 0
            ? State.Crates[(int)rng.Below((uint)State.Crates.Count)].Id
            : (int)rng.Below(6);
        int SlotIn(Stack?[]? slots, int size)
        {
            var filled = new List<int>();
            for (int i = 0; i < (slots?.Length ?? 0); i++) if (slots![i] != null) filled.Add(i);
            return filled.Count > 0 && rng.Below(5) != 0
                ? filled[(int)rng.Below((uint)filled.Count)]
                : (int)rng.Below((uint)size + 2) - 1;
        }
        int? Side() => rng.Below(2) == 0 ? null : AnyCrate();
        int crateSlots = Machines.Get("crate").Slots ?? 0;

        var at = Side();
        var src = at is int id
            ? new Place(SlotIn(State.Crates.Find(h => h.Id == id)?.Slots, crateSlots), id)
            : new Place(SlotIn(own, Content.Inventory.Total));
        var to = Side();
        var dst = rng.Below(3) == 0
            ? new Target(Crate: to)
            : new Target((int)rng.Below((uint)(to == null ? Content.Inventory.Total : crateSlots) + 1), to);
        // Whole stacks, halves and single items, and now and then none.
        int count = rng.Below(5) switch { 0 => 999, 1 => 1, 2 => 3, 3 => (int)rng.Below(40), _ => 0 };
        uint roll = rng.Below(8);
        if (roll == 0)
        {
            // Mostly within the grid; now and then off it.
            int size = Math.Max(1, _grid.Width * _grid.Height);
            int cell = rng.Below(10) == 0 ? -1 : (int)rng.Below((uint)size);
            return new Drop(src, count, cell, rng.Below(2) == 0 ? -1 : 1) { From = from };
        }
        if (roll == 2 && rng.Below(2) == 0) return ToStationOrBelt(rng, from, own, src, count);
        if (roll == 1)
        {
            var floor = State.Floor;
            int pick = floor.Count > 0 && rng.Below(4) != 0 ? floor[(int)rng.Below((uint)floor.Count)].Id : (int)rng.Below(50);
            return new PickUp(pick) { From = from };
        }
        return new Move(src, dst, count) { From = from };
    }

    /// <summary>
    /// A move of what holds charge, from wherever one lies, the sender's own or a machine's, into a charging
    /// station, a belt slot or another equipment slot (refused); with none about, whatever <paramref name="src"/>
    /// holds, mostly refused as no battery. So charge moves between belts and stations, and is filled there.
    /// </summary>
    private Move ToStationOrBelt(Rng rng, int from, Stack?[]? own, Place src, int count)
    {
        var holding = new List<Place>();
        for (int i = 0; i < (own?.Length ?? 0); i++)
            if (own![i] is { } s && Content.Items.Get(s.Item).Capacity != null) holding.Add(new Place(i));
        foreach (var held in State.Crates)
            for (int i = 0; i < held.Slots.Length; i++)
                if (held.Slots[i] is { } s && Content.Items.Get(s.Item).Capacity != null) holding.Add(new Place(i, held.Id));
        var what = holding.Count > 0 ? holding[(int)rng.Below((uint)holding.Count)] : src;
        var stations = Machines.All.Where(m => m.Charges).SelectMany(m => _grid.MachinesOf(m.Kind)).ToList();
        Target to = rng.Below(3) switch
        {
            0 when stations.Count > 0 => new Target(0, stations[(int)rng.Below((uint)stations.Count)].Id),
            0 or 1 => new Target(Belt[(int)rng.Below((uint)Belt.Length)]),
            _ => new Target(Content.Inventory.Slots + (int)rng.Below((uint)Content.Inventory.Equipment.Count)),
        };
        return new Move(what, to, count == 0 ? 1 : count) { From = from };
    }
}
