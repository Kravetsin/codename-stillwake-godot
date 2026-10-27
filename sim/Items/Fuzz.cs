using Stillwake.Content;

namespace Stillwake.Sim.Items;

public sealed partial class ItemsModule
{
    /// <summary>
    /// Mostly moves, from the sender's own inventory or a crate holding something, so most are taken;
    /// now and then from an empty slot, a slot out of range, a crate that is not there, or onto a full
    /// or other stack, or into an equipment slot that does not wear it, so every refusal is drawn too; now
    /// and then a battery into a charging station or onto the belt (<see cref="ToStationOrBelt"/>), or a sort
    /// of the bag or of a machine, a crate or not (<c>items.notCrate</c>) or none (<c>items.noCrate</c>).
    /// Creative mode switches now and then, mostly off; now and then a run of an electric tool (<see cref="RunOrStop"/>). Now and then a drop onto a cell, off the grid
    /// too, or a pick-up of a stack on the floor or of one long gone. Now and then a module fitted or taken out
    /// (<see cref="FitOrUnfit"/>). Now and then a spawn of any item, up to two stacks of it, refused outside creative mode.
    /// </summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        if (rng.Below(10) == 0) return new SetCreative(rng.Below(4) == 0) { From = from };
        if (rng.Below(20) == 0)
        {
            var item = Content.Items.All[(int)rng.Below((uint)Content.Items.All.Count)];
            return new Spawn(item.Id, 1 + (int)rng.Below((uint)item.Stack * 2)) { From = from };
        }
        if (rng.Below(10) == 0) return RunOrStop(rng, from);
        var own = State.Inventories.Find(h => h.Id == from)?.Slots;
        if (rng.Below(10) == 0) return FitOrUnfit(rng, from, own);
        if (rng.Below(12) == 0 && SuitOnOrOff(from, own) is { } dress) return dress;
        // Mostly a machine that holds something; the wreck's other machines and missing ids otherwise.
        int AnyCrate() => State.Crates.Count > 0 && rng.Below(4) != 0
            ? State.Crates[(int)rng.Below((uint)State.Crates.Count)].Id
            : (int)rng.Below(6);
        // Now and then a sort: of the bag, or of a machine, mostly one holding something; refused for a furnace or none.
        if (rng.Below(12) == 0) return new Sort(rng.Below(3) == 0 ? null : AnyCrate()) { From = from };
        int SlotIn(Stack?[]? slots, int size)
        {
            var filled = new List<int>();
            for (int i = 0; i < (slots?.Length ?? 0); i++) if (slots![i] != null) filled.Add(i);
            return filled.Count > 0 && rng.Below(5) != 0
                ? filled[(int)rng.Below((uint)filled.Count)]
                : (int)rng.Below((uint)size + 2) - 1;
        }
        int? Side() => rng.Below(2) == 0 ? null : AnyCrate();
        int crateSlots = Machines.All.First(m => m.Is(MachineTrait.Container)).Slots ?? 0;

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

    private static readonly int SuitSlot = Content.Inventory.Slots + Content.Inventory.Equipment.ToList().IndexOf(Suit.Slot);

    /// <summary>A suit carried in the bag put on, else the one worn taken off into the bag; none when neither, so suits change outside creative.</summary>
    private static Move? SuitOnOrOff(int from, Stack?[]? own)
    {
        if (own == null) return null;
        for (int i = 0; i < Content.Inventory.Slots; i++)
            if (own[i] is { } s && Content.Items.Get(s.Item).Equip == Suit.Slot) return new Move(new Place(i), new Target(SuitSlot), 1) { From = from };
        return own[SuitSlot] != null ? new Move(new Place(SuitSlot), new Target(Crate: null), 1) { From = from } : null;
    }

    private static readonly ItemSpec[] Electric = [.. Content.Items.All.Where(i => i.Draw != null)];

    /// <summary>
    /// A module fitted, mostly from a slot holding one, the sender's or a machine's, into a machine that takes it, an arc bench
    /// (into its tool, 0099) or any machine (<c>items.notModule</c>, <c>items.noTool</c>, or <c>items.fitted</c> where one is), now
    /// and then from any slot or into a machine that is not there; or taken out, mostly one fitted, a machine's or a bench's
    /// tool's, into a slot of the sender's or wherever it fits (<c>items.full</c>, <c>items.noSlot</c>), now and then one that
    /// is not there (<c>items.notFitted</c>); or a stack moved onto a bench's tool slot, mostly a tool it takes, now and then
    /// anything (<c>items.notTool</c>).
    /// </summary>
    private Command FitOrUnfit(Rng rng, int from, Stack?[]? own)
    {
        int Below(int n) => (int)rng.Below((uint)n);
        var machines = _grid.Machines;
        int AnyMachine() => machines.Count > 0 && Below(6) != 0 ? machines[Below(machines.Count)].Id : Below(60);
        var benches = machines.Where(m => Machines.Get(m.Kind).Tunes).ToList();
        if (benches.Count > 0 && Below(4) == 0)
        {
            var tools = new List<int>();
            for (int i = 0; i < (own?.Length ?? 0); i++)
                if (Modules.Hosted(own![i]?.Item)) tools.Add(i);
            int slot = tools.Count > 0 && Below(4) != 0 ? tools[Below(tools.Count)] : Below(Content.Inventory.Total);
            return new Move(new Place(slot), new Target(0, benches[Below(benches.Count)].Id), 1) { From = from };
        }
        if (Below(2) == 0)
        {
            // What is fitted: a machine's, and a bench's tool's.
            var fitted = State.Fitted.ToList();
            foreach (var bench in benches)
                if (ToolOn(bench.Id)?.Module is { } on) fitted.Add(new Fitted(bench.Id, on));
            var (machine, module) = fitted.Count > 0 && Below(4) != 0
                ? (fitted[Below(fitted.Count)] is var f ? (f.Machine, f.Module) : default)
                : (AnyMachine(), Below(3) == 0 ? "iron" : Modules.All[Below(Modules.All.Count)].Item);
            int? slot = Below(2) == 0 ? null : Below(Content.Inventory.Total + 1) - 1;
            return new Unfit(machine, module, slot) { From = from };
        }
        var holding = new List<Place>();
        for (int i = 0; i < (own?.Length ?? 0); i++)
            if (Modules.TryGet(own![i]?.Item, out _)) holding.Add(new Place(i));
        foreach (var held in State.Crates)
            for (int i = 0; i < held.Slots.Length; i++)
                if (Modules.TryGet(held.Slots[i]?.Item, out _)) holding.Add(new Place(i, held.Id));
        var what = holding.Count > 0 && Below(5) != 0 ? holding[Below(holding.Count)] : new Place(Below(Content.Inventory.Total + 1) - 1);
        var hosts = machines.Where(m => Modules.Hosted(m.Kind) || Machines.Get(m.Kind).Tunes).ToList();
        int into = hosts.Count > 0 && Below(3) != 0 ? hosts[Below(hosts.Count)].Id : AnyMachine();
        return new Fit(what, into) { From = from };
    }

    /// <summary>
    /// A run of an electric item turned on, mostly, or off; now and then of any item, so <c>items.notElectric</c> is
    /// drawn. Turning on is refused <c>items.notHeld</c> by a sender carrying none and <c>items.flat</c> by one whose
    /// belt cannot pay; taken, it drains the belt every tick till it is turned off or the belt is flat.
    /// </summary>
    private static Run RunOrStop(Rng rng, int from)
    {
        var all = Content.Items.All;
        string item = rng.Below(6) == 0 ? all[(int)rng.Below((uint)all.Count)].Id : Electric[(int)rng.Below((uint)Electric.Length)].Id;
        return new Run(item, rng.Below(3) != 0) { From = from };
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
