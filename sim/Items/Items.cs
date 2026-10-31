using Stillwake.Content;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Knowledge;
using Stillwake.Sim.Rooms;

namespace Stillwake.Sim.Items;

/// <summary>Moves up to <c>Count</c> from <c>Src</c> to <c>Dst</c>, within the sender's inventory, between it and a crate, or between crates.</summary>
public sealed record Move(Place Src, Target Dst, int Count) : Command;

/// <summary>Throws up to <c>Count</c> from a slot from <c>Cell</c>, the sender's as its client reports it, the way it faces: -1 left, 1 right.</summary>
public sealed record Drop(Place Src, int Count, int Cell, int Facing) : Command;

/// <summary>Picks up as much of floor stack <c>Id</c> as fits into the sender's inventory; the rest stays.</summary>
public sealed record PickUp(int Id) : Command;

/// <summary>Switches creative mode, from the dev panel.</summary>
public sealed record SetCreative(bool On) : Command;

/// <summary>
/// Puts up to <c>Count</c> of <c>Item</c> into the sender's inventory out of nothing, as much as fits: creative mode
/// only, from the dev panel's item picker. A battery comes full.
/// </summary>
public sealed record Spawn(string Item, int Count) : Command;

/// <summary>
/// The sender's hero runs electric item <c>Item</c>, one it carries (<c>On</c>), or stops what it runs: sent while
/// its client's run differs from the sim's, as <c>Thrust</c> is. A run draws the item's <c>Draw</c> every tick (0055).
/// </summary>
public sealed record Run(string Item, bool On) : Command;

/// <summary>
/// Sorts the sender's bag, its own slots past the hotbar, or with <c>Crate</c> every slot of that crate (0008):
/// stacks alike merged, then laid from the first slot by group and id. The hotbar and the equipment stay as they are.
/// </summary>
public sealed record Sort(int? Crate = null) : Command;

/// <summary>A peer's inventory and the machines some moves touch, as the moves would leave them.</summary>
/// <param name="Crates">The machines the moves touched, in the order they were first touched.</param>
public sealed record Shown(IReadOnlyList<Stack?> Inventory, IReadOnlyList<(int Crate, IReadOnlyList<Stack?> Slots)> Crates);

/// <summary>What any module may read of the items. Slots are live: a reader never writes them.</summary>
public interface IItemsQueries
{
    IReadOnlyList<Stack?> Inventory(int peer);
    IReadOnlyList<Stack?> Crate(int id);
    bool CanSmelt(int id);
    bool Creative { get; }
    bool SlotOpen(Place place);
    IReadOnlyList<(string Item, int Count)> Totals(int peer);
    string? ShortOf(int peer, IReadOnlyList<(string Item, int Count)> cost);
    string? FullFor(int peer, IReadOnlyList<(string Item, int Count)> cost);
    string? OutputFull(int id, IReadOnlyList<(string Item, int Count)> cost);
    IReadOnlyList<(string Item, int Count)> InputsLack(int id, IReadOnlyList<(string Item, int Count)> cost);
    IReadOnlyList<FloorStack> Floor { get; }
    /// <summary>The items' clock, ticks since the world began: what a floor stack's <c>At</c> counts in.</summary>
    int Ticks { get; }
    string? PickUpProblem(int peer, int id);
    int PickUpAmount(int peer, int id);
    Shown AfterMoves(int peer, IReadOnlyList<Command> moves);
    bool Equipped(int peer, string item);
    string? WornIn(int peer, string kind);
    (int Charge, int Capacity) BeltCharge(int peer);
    bool CanDrain(int peer, int amount);
    string? Running(int peer);
    string? WeaponProblem(int peer, string weapon, bool running);
    int HeldFor(int id, string item);
    int RoomFor(int id, Stack stack);
    string? KeptAt(int id, int slot);
    IReadOnlyList<string> FittedTo(int machine);
    Boost Boost(int machine);
    string? FitProblem(int peer, Place from, int machine);
    Stack? ToolStack(int peer, string item);
    int Damage(int peer, string tool);
    /// <summary>What a hit of weapon <paramref name="tool"/> by <paramref name="peer"/> takes off a wall, back wall or plant.</summary>
    int BlockDamage(int peer, string tool);
}

/// <summary>How a module that depends on the items may change them.</summary>
public interface IItemsOps
{
    IReadOnlyList<(string Item, int Count)> Take(int peer, IReadOnlyList<(string Item, int Count)> cost);
    void Give(int peer, IReadOnlyList<(string Item, int Count)> cost);
    void Smelt(int id, IReadOnlyList<(string Item, int Count)> besides);
    void Consume(int id, int slot);
    void Output(int id, IReadOnlyList<(string Item, int Count)> cost);
    void TakeInputs(int id, IReadOnlyList<(string Item, int Count)> cost);
    IReadOnlyList<(string Item, int Count)> GiveOrDrop(int peer, IReadOnlyList<(string Item, int Count)> cost, int cell);
    void DropAt(int peer, IReadOnlyList<(string Item, int Count)> cost, int cell);
    void Spray(IReadOnlyList<(string Item, int Count)> loot, int cell, int away);
    void Charge(int id, int slot, int amount);
    bool Drain(int peer, int amount);
    void StopRunning(int peer);
    void LeaveBody(int peer, int cell);
    void Kit(int peer);
    Stack? TakeFrom(int id, int slot, int count);
    int PutIn(int id, Stack stack);
    void LayDown(Stack stack, int cell);
}

/// <summary>
/// Every stack of items: inventories, what machines hold, the floor, creative mode
/// (packages/sim/src/modules/items). A cost is a list of items with whole amounts, as content writes them.
/// </summary>
public sealed partial class ItemsModule : Module<ItemsState>, IItemsQueries, IItemsOps, ICarries
{
    private readonly IGridQueries _grid;
    private readonly IRoomsQueries _rooms;

    private readonly IKnowledgeQueries _knowledge;
    private readonly IKnowledgeOps _knowledgeOps;

    /// <summary>An inventory's own slots open now, before its equipment: what giving, taking and picking up use (2026-10-30-room-upgrades).</summary>
    private int Own => OpenOf(Content.Upgrades.Bag, Content.Inventory.Start, Content.Inventory.Slots);

    /// <summary>The first <paramref name="start"/> slots of a kind and what upgrades add, in creative mode all <paramref name="most"/>.</summary>
    private int OpenOf(string kind, int start, int most) =>
        State.Creative ? most : Math.Min(most, start + Content.Upgrades.Added(kind, _knowledge.Granted));

    /// <summary>A trade station's slots open now: those past them take nothing in, as a machine's outputs.</summary>
    private int InputsOf(MachineSpec spec) =>
        spec.Is(MachineTrait.Sells) ? OpenOf(Content.Upgrades.TradeSlots, Content.Upgrades.TradeStart, spec.Slots ?? 0) : spec.Inputs ?? spec.Slots ?? 0;

    /// <summary>Whether <paramref name="place"/> is open: not one waiting on an upgrade, which holds nothing and is not shown.</summary>
    public bool SlotOpen(Place place)
    {
        if (place.Crate is int id) return HolderOf(id) is not { } spec || !spec.Is(MachineTrait.Sells) || place.Slot < InputsOf(spec);
        if (place.Slot < Content.Inventory.Slots) return place.Slot < Own;
        int belt = Array.IndexOf(Belt, place.Slot);
        return belt < 0 || belt < OpenOf(Content.Upgrades.Belt, Content.Inventory.BeltStart, Belt.Length);
    }

    public override string Id => "items";

    public ItemsModule(IGridQueries grid, IRoomsQueries rooms, IKnowledgeQueries knowledge, IKnowledgeOps knowledgeOps)
    {
        _grid = grid;
        _rooms = rooms;
        _knowledge = knowledge;
        _knowledgeOps = knowledgeOps;
        Handles<Drop>(DropProblem, ApplyDrop);
        Handles<PickUp>(c => PickUpProblem(c.From, c.Id), ApplyPickUp);
        Handles<SetCreative>(_ => null, c => State.Creative = c.On);
        Handles<Spawn>(SpawnProblem, ApplySpawn);
        Handles<Move>(MoveProblem, ApplyMove);
        Handles<Run>(RunProblem, ApplyRun);
        Handles<Sort>(SortProblem, ApplySort);
        Handles<Fit>(c => FitProblem(c.From, c.Module, c.Machine), ApplyFit);
        Handles<Unfit>(UnfitProblem, ApplyUnfit);
        // After derivation: falling reads the rooms, rebuilt by then.
        System(Phase.Simulation, () =>
        {
            State.Ticks += 1;
            Runs();
            FloorRules.Settle(State, _grid, _rooms, Rules.Fall(Content.Floor.Gravity));
        });
    }

    /// <summary>
    /// Every run draws its item's <c>Draw</c> from its peer's belt for this tick, in peer order, boosted by the module fitted
    /// into the stack it runs (<see cref="ToolStack"/>, 0098); one whose belt cannot pay the tick, or whose item its peer no
    /// longer carries, stops (0055).
    /// </summary>
    private void Runs()
    {
        var running = State.Running;
        for (int i = 0; i < running.Count;)
        {
            var r = running[i];
            if (ToolStack(r.Peer, r.Item) is { } stack && Drain(r.Peer, DrawOf(stack))) i++;
            else running.RemoveAt(i);
        }
    }

    protected override ItemsState Init(Layout layout) => new();

    /// <summary>A new surface keeps the creative mode of the one it was opened from: a dev setting of the world, kept per surface.</summary>
    protected override void Inherit(Module from) => State.Creative = ((ItemsModule)from).Creative;

    /// <summary>What a peer takes from surface to surface (0076): its slots, equipment and belt with their charge, and the electric item it runs.</summary>
    private sealed record Carried(Stack?[]? Slots, string? Running);

    /// <summary><paramref name="peer"/>'s inventory and its run, gone from this surface; null when it has neither here.</summary>
    public object? Depart(int peer)
    {
        int at = State.Inventories.FindIndex(h => h.Id == peer);
        var slots = at >= 0 ? State.Inventories[at].Slots : null;
        if (at >= 0) State.Inventories.RemoveAt(at);
        string? running = Running(peer);
        StopRunning(peer);
        return slots == null && running == null ? null : new Carried(slots, running);
    }

    /// <summary>Takes what <paramref name="peer"/> carried as it left: the tool it ran runs on here, its belt paying from this tick.</summary>
    public void Arrive(int peer, object? carried, int? cell)
    {
        if (carried is not Carried c) return;
        // A seat's inventory is on one surface only: one here already is our bug.
        if (State.Inventories.Exists(h => h.Id == peer) || Running(peer) != null)
            throw new InvalidOperationException($"items: peer {peer} arrives where its inventory is already");
        if (c.Slots != null) Stacks.SetInventory(State, peer, c.Slots);
        if (c.Running is { } item)
        {
            int at = State.Running.FindIndex(r => r.Peer > peer);
            State.Running.Insert(at < 0 ? State.Running.Count : at, new Runner { Peer = peer, Item = item });
        }
    }

    // Queries.

    /// <summary><paramref name="peer"/>'s slots in order, each a stack or null; live and read-only.</summary>
    public IReadOnlyList<Stack?> Inventory(int peer) => Stacks.InventoryOf(State, peer);

    /// <summary>
    /// The item slot <paramref name="slot"/> of machine <paramref name="id"/> remembers, held or burnt down to nothing:
    /// the last put in an input of a fed station or a fire, until a hand takes it all out (0062); null for none.
    /// </summary>
    public string? KeptAt(int id, int slot) => KeptIndex(id, slot) is int at and >= 0 ? State.Kept[at].Item : null;

    /// <summary>What machine <paramref name="id"/> holds, slot by slot: a crate, a furnace; none for a machine holding no items.</summary>
    public IReadOnlyList<Stack?> Crate(int id) => Stacks.CrateOf(State, id, HolderOf(id)?.Slots ?? 0);

    /// <summary>
    /// Whether fed station <paramref name="id"/> has in its input what smelts or grows, as many as one smelt takes
    /// (<c>ItemSpec.SmeltTakes</c>), the most one may make fitting its outputs (<see cref="MostMade"/>).
    /// </summary>
    public bool CanSmelt(int id) => HolderOf(id)?.Inputs is int inputs && Smelted(Crate(id), inputs) != null;

    public bool Creative => State.Creative;

    /// <summary>How much of each item <paramref name="peer"/> carries in its own slots, its equipment left out, in the order first found: what it could pay with.</summary>
    public IReadOnlyList<(string Item, int Count)> Totals(int peer)
    {
        var totals = new List<(string Item, int Count)>();
        var slots = Inventory(peer);
        for (int i = 0; i < Own; i++)
        {
            var s = slots[i];
            if (s == null) continue;
            int at = totals.FindIndex(t => t.Item == s.Item);
            if (at < 0) totals.Add((s.Item, s.Count));
            else totals[at] = (s.Item, totals[at].Count + s.Count);
        }
        return totals;
    }

    /// <summary><c>items.notEnough</c> when <paramref name="peer"/> cannot pay <paramref name="cost"/>; never in creative mode.</summary>
    public string? ShortOf(int peer, IReadOnlyList<(string Item, int Count)> cost)
    {
        bool cannot = Stacks.Taken(Inventory(peer), cost, Own) == null;
        return cannot && !State.Creative ? "items.notEnough" : null;
    }

    /// <summary><c>items.full</c> when <paramref name="cost"/> does not fit into <paramref name="peer"/>'s inventory, creative mode or not.</summary>
    public string? FullFor(int peer, IReadOnlyList<(string Item, int Count)> cost) =>
        Stacks.Placed(Inventory(peer), cost, Own) == null ? "items.full" : null;

    /// <summary>
    /// <c>items.full</c> when <paramref name="cost"/> does not fit into machine <paramref name="id"/>'s
    /// outputs, its slots past its inputs: where what a crafting machine makes goes.
    /// </summary>
    public string? OutputFull(int id, IReadOnlyList<(string Item, int Count)> cost) =>
        Outputs(id, cost) == null ? "items.full" : null;

    /// <summary>
    /// What of <paramref name="cost"/> machine <paramref name="id"/>'s inputs, its first <c>Inputs</c> slots, lack, item
    /// by item in the order the cost names them; none when they hold it all: a queued station's unit fed from them.
    /// </summary>
    public IReadOnlyList<(string Item, int Count)> InputsLack(int id, IReadOnlyList<(string Item, int Count)> cost)
    {
        var slots = Crate(id);
        int inputs = Math.Min(InputsOf(id), slots.Count);
        var lack = new List<(string Item, int Count)>();
        var asked = new List<(string Item, int Count)>();
        foreach (var (item, need) in Stacks.Entries(cost))
        {
            int have = 0;
            for (int i = 0; i < inputs; i++)
                if (slots[i] is { } s && s.Item == item) have += s.Count;
            // An item named twice is wanted twice: what an earlier naming asks for is not there for this one.
            foreach (var (named, n) in asked)
                if (named == item) have -= n;
            asked.Add((item, need));
            if (have < need) lack.Add((item, need - Math.Max(0, have)));
        }
        return lack;
    }

    /// <summary>The stacks on the floor, in id order; live and read-only.</summary>
    public IReadOnlyList<FloorStack> Floor => State.Floor;

    public int Ticks => State.Ticks;

    /// <summary>Why peer <paramref name="peer"/>'s pick-up of stack <paramref name="id"/> would be refused: gone, too soon, or no room for any.</summary>
    public string? PickUpProblem(int peer, int id)
    {
        var stack = State.Floor.Find(s => s.Id == id);
        if (stack == null) return "items.gone";
        if (stack.By == peer && State.Ticks - stack.At < Content.Floor.OwnPickupDelay) return "items.tooSoon";
        return PickUpAmount(peer, stack) == 0 ? "items.full" : null;
    }

    /// <summary>How much of stack <paramref name="id"/> a pick-up by <paramref name="peer"/> would take now; none for no such stack.</summary>
    public int PickUpAmount(int peer, int id) => State.Floor.Find(s => s.Id == id) is { } stack ? PickUpAmount(peer, stack) : 0;

    private int PickUpAmount(int peer, FloorStack stack)
    {
        var (_, left) = Stacks.PlacedSome(Inventory(peer), stack.AsStack(), Own);
        return stack.Count - left;
    }

    /// <summary>
    /// <paramref name="peer"/>'s inventory and the machines <paramref name="moves"/> touch — <see cref="Move"/>s,
    /// <see cref="Drop"/>s and <see cref="Sort"/>s — as they would be after them, in order, each as the command would apply it and
    /// skipped where it would be refused: what a client shows of its own moves before their tick runs
    /// (0013). Copies; the world is untouched.
    /// </summary>
    public Shown AfterMoves(int peer, IReadOnlyList<Command> moves)
    {
        var shown = new List<(int? Crate, IReadOnlyList<Stack?> Slots)>();
        void Show(int? crate, IReadOnlyList<Stack?> slots)
        {
            int at = shown.FindIndex(s => s.Crate == crate);
            if (at < 0) shown.Add((crate, slots));
            else shown[at] = (crate, slots);
        }
        IReadOnlyList<Stack?> SlotsOf(Target side)
        {
            int at = shown.FindIndex(s => s.Crate == side.Crate);
            return at >= 0 ? shown[at].Slots : SideOf(peer, side);
        }
        foreach (var m in moves)
        {
            switch (m)
            {
                case Drop d:
                    if (DropSide(SlotsOf, d.Src, out var slots) == null
                        && Stacks.TakenFrom(slots, d.Src.Slot, d.Count, out var left, out _) == null)
                        Show(d.Src.Crate, left);
                    break;
                case Move mv:
                    if (Into(mv.Dst, out var dst) != null) break;
                    if (MoveSides(SlotsOf, mv.Src, dst, out var from, out var to, out bool same) != null) break;
                    if (Moved(from, to, same, mv.Src, dst, mv.Count, out var after, out var into) != null) break;
                    Show(mv.Src.Crate, after);
                    Show(mv.Dst.Crate, into);
                    break;
                case Sort so:
                    if (SortProblem(so) == null) Show(so.Crate, Sorted(SlotsOf(new Target(Crate: so.Crate)), so.Crate));
                    break;
                default:
                    throw new ArgumentException($"items.afterMoves: {m.GetType().Name} is no move");
            }
        }
        int own = shown.FindIndex(s => s.Crate == null);
        return new Shown(
            own >= 0 ? shown[own].Slots : Inventory(peer),
            shown.Where(s => s.Crate != null).Select(s => (s.Crate!.Value, s.Slots)).ToList());
    }

    /// <summary>Whether <paramref name="peer"/> wears <paramref name="item"/> in one of its equipment slots: a jetpack on its back.</summary>
    public bool Equipped(int peer, string item)
    {
        var slots = Inventory(peer);
        for (int i = Content.Inventory.Slots; i < slots.Count; i++)
            if (slots[i]?.Item == item) return true;
        return false;
    }

    /// <summary>
    /// The item <paramref name="peer"/> wears in its first equipment slot of <paramref name="kind"/>, or null: the suit it
    /// wears, whichever suit that is (2026-10-27-suits-are-worn).
    /// </summary>
    public string? WornIn(int peer, string kind)
    {
        int at = Content.Inventory.Equipment.ToList().IndexOf(kind);
        return at < 0 ? null : Inventory(peer)[Content.Inventory.Slots + at]?.Item;
    }

    /// <summary>
    /// What the batteries on <paramref name="peer"/>'s belt hold (<c>Content.Charge.Belt</c>), summed, and what
    /// they would hold full: what an electric tool held runs on, and the energy bar shows.
    /// </summary>
    public (int Charge, int Capacity) BeltCharge(int peer)
    {
        int charge = 0, capacity = 0;
        var slots = Inventory(peer);
        foreach (int slot in Belt)
            if (slots[slot] is { } s && Content.Items.Get(s.Item).Capacity is int full)
                (charge, capacity) = (charge + s.Charge, capacity + full);
        return (charge, capacity);
    }

    /// <summary>The inventory's belt slots, in slot order: the batteries an electric tool drains, left to right.</summary>
    private static readonly int[] Belt =
    [
        .. Enumerable.Range(0, Content.Inventory.Equipment.Count)
            .Where(i => Content.Inventory.Equipment[i] == Content.Charge.Belt)
            .Select(i => Content.Inventory.Slots + i),
    ];

    // Operations.

    /// <summary>
    /// Takes <paramref name="cost"/> from <paramref name="peer"/> and returns what it took: all of it, or
    /// nothing in creative mode, so a refund of what was paid never makes anything out of nothing.
    /// Check <see cref="ShortOf"/> first.
    /// </summary>
    public IReadOnlyList<(string Item, int Count)> Take(int peer, IReadOnlyList<(string Item, int Count)> cost)
    {
        // Amounts are checked in creative mode too, so a caller's bug shows in either mode.
        if (Stacks.IsNothing(cost) || State.Creative) return [];
        var left = Stacks.Taken(Inventory(peer), cost, Own)
            ?? throw new InvalidOperationException("items.take: not enough; check ShortOf first");
        Stacks.SetInventory(State, peer, left);
        return [.. cost];
    }

    /// <summary>Gives <paramref name="cost"/> to <paramref name="peer"/>, in creative mode too. The caller checks <see cref="FullFor"/> first.</summary>
    public void Give(int peer, IReadOnlyList<(string Item, int Count)> cost)
    {
        if (Stacks.IsNothing(cost)) return;
        var more = Stacks.Placed(Inventory(peer), cost, Own)
            ?? throw new InvalidOperationException("items.give: no room; check FullFor first");
        Stacks.SetInventory(State, peer, more);
    }

    /// <summary>
    /// Turns one smelt of what fed station <paramref name="id"/> holds in its input, its <c>SmeltTakes</c>, into its yields
    /// and <paramref name="besides"/>, what was drawn (a crop's seeds, byproducts), put into its outputs. The caller checks
    /// <see cref="CanSmelt"/> first, and gives besides no more than <see cref="MostMade"/> allows.
    /// </summary>
    public void Smelt(int id, IReadOnlyList<(string Item, int Count)> besides)
    {
        int inputs = HolderOf(id)?.Inputs ?? 0;
        var slots = Crate(id);
        var outputs = Smelted(slots, inputs, besides);
        if (outputs == null || Stacks.TakenFrom(slots, 0, Content.Items.Get(slots[0]!.Item).SmeltTakes, out var left, out _) != null)
            throw new InvalidOperationException("items.smelt: nothing to smelt; check CanSmelt");
        Obtained(Content.Items.Get(slots[0]!.Item).Yields!);
        Obtained(besides);
        SetMachine(id, [.. left.Take(inputs), .. outputs]);
    }

    /// <summary>Uses up one of what slot <paramref name="slot"/> of machine <paramref name="id"/> holds: fuel burnt. The caller checks it holds one.</summary>
    public void Consume(int id, int slot)
    {
        if (Stacks.TakenFrom(Crate(id), slot, 1, out var left, out _) != null)
            throw new InvalidOperationException("items.consume: nothing there");
        SetMachine(id, left);
    }

    /// <summary>Puts <paramref name="cost"/> into machine <paramref name="id"/>'s outputs: what it made. The caller checks <see cref="OutputFull"/> first.</summary>
    public void Output(int id, IReadOnlyList<(string Item, int Count)> cost)
    {
        if (Stacks.IsNothing(cost)) return;
        var slots = Outputs(id, cost) ?? throw new InvalidOperationException("items.output: no room; check OutputFull first");
        Obtained(cost);
        SetMachine(id, slots);
    }

    /// <summary>
    /// Takes <paramref name="cost"/> from machine <paramref name="id"/>'s inputs, each item from its first slot first: a
    /// unit fed from them begun. In creative mode too, as a smelt takes its item. The caller checks <see cref="InputsLack"/> first.
    /// </summary>
    public void TakeInputs(int id, IReadOnlyList<(string Item, int Count)> cost)
    {
        if (Stacks.IsNothing(cost)) return;
        var left = Stacks.Taken(Crate(id), cost, InputsOf(id), firstFirst: true)
            ?? throw new InvalidOperationException("items.takeInputs: not there; check InputsLack first");
        SetMachine(id, left);
    }

    /// <summary>
    /// Gives <paramref name="peer"/> as much of <paramref name="cost"/> as fits; the rest falls on <paramref name="cell"/>, as if
    /// <paramref name="peer"/> dropped it. Returns what went into the inventory, by item in the order the cost first names it.
    /// </summary>
    public IReadOnlyList<(string Item, int Count)> GiveOrDrop(int peer, IReadOnlyList<(string Item, int Count)> cost, int cell)
    {
        if (Stacks.IsNothing(cost)) return [];
        Obtained(cost);
        var (slots, left) = Stacks.PlacedSome(Inventory(peer), cost, Own);
        Stacks.SetInventory(State, peer, slots);
        DropAt(peer, left, cell);
        var given = new List<(string Item, int Count)>();
        foreach (var (item, _) in cost)
        {
            if (given.Exists(g => g.Item == item)) continue;
            int count = cost.Where(c => c.Item == item).Sum(c => c.Count) - left.Where(l => l.Item == item).Sum(l => l.Count);
            if (count > 0) given.Add((item, count));
        }
        return given;
    }

    /// <summary>What the crew obtains is counted where a stack is born: here, never when one only moves or a payment comes back.</summary>
    private void Obtained(IReadOnlyList<(string Item, int Count)> cost)
    {
        foreach (var (item, count) in Stacks.Entries(cost)) _knowledgeOps.Obtain(item, count);
    }

    /// <summary>Lets <paramref name="cost"/> fall on cell <paramref name="cell"/> as if peer <paramref name="peer"/> dropped it there, still; <see cref="FloorStack.Nobody"/> for a stack anyone may take at once.</summary>
    public void DropAt(int peer, IReadOnlyList<(string Item, int Count)> cost, int cell)
    {
        if (peer == FloorStack.Nobody) Obtained(cost);
        foreach (var (item, count) in Stacks.Entries(cost))
            if (count > 0) FloorRules.LayDown(State, new Stack(item, count), cell, 0, _grid.Width, peer);
    }

    /// <summary>
    /// Throws <paramref name="loot"/> out of cell <paramref name="cell"/> for anyone to take (2026-10-29-kill-feel): in pieces
    /// (<see cref="SprayRules.Pieces"/>), each flying to the side <paramref name="away"/> points, or a drawn side for 0, at a drawn
    /// speed and lift; afloat a share of them, up or down. Drawn from the world's Rng, so every peer throws the same.
    /// </summary>
    public void Spray(IReadOnlyList<(string Item, int Count)> loot, int cell, int away)
    {
        Obtained(loot);
        bool afloat = _rooms.Afloat(cell);
        foreach (var (item, count) in SprayRules.Pieces(Stacks.Entries(loot), Content.Floor.SprayMost))
        {
            int side = away != 0 ? Math.Sign(away) : Random.Below(2) == 0 ? -1 : 1;
            int vx = side * (Content.Floor.SprayLeast + (int)Random.Below(Content.Floor.SprayFastest - Content.Floor.SprayLeast + 1));
            int vy = -(Content.Floor.SprayLiftLeast + (int)Random.Below(Content.Floor.SprayLiftMost - Content.Floor.SprayLiftLeast + 1));
            if (afloat)
            {
                if (Random.Below(2) == 0) vy = -vy;
                (vx, vy) = (vx * Content.Floor.SpraySpaceShare / 1000, vy * Content.Floor.SpraySpaceShare / 1000);
            }
            FloorRules.LayDown(State, new Stack(item, count), cell, vx, _grid.Width, FloorStack.Nobody, vy: vy);
        }
    }

    /// <summary>
    /// Adds up to <paramref name="amount"/> of charge, stored scaled, to what slot <paramref name="slot"/> of machine
    /// <paramref name="id"/> holds, never past its <c>Capacity</c>: a charging station's tick, a drone bay's share for one
    /// drone. The caller checks it holds an item that has one.
    /// </summary>
    public void Charge(int id, int slot, int amount)
    {
        if (amount < 0) throw new ArgumentException($"items.charge: {amount} is no charge");
        var slots = Crate(id);
        if (slot < 0 || slot >= slots.Count || slots[slot] is not { } s || Content.Items.Get(s.Item).Capacity is not int full)
            throw new InvalidOperationException("items.charge: nothing there holds charge");
        var charged = slots.ToArray();
        charged[slot] = s with { Charge = Math.Min(full, s.Charge + amount) };
        SetMachine(id, charged);
    }

    /// <summary>
    /// Takes <paramref name="amount"/> power-ticks from the first battery on <paramref name="peer"/>'s belt,
    /// in slot order, that holds at least that much, and whether one did: a running electric tool's <c>Draw</c> for
    /// a tick (0055). One battery pays it whole, so the belt drains left to right. Nothing is taken in
    /// creative mode.
    /// </summary>
    public bool Drain(int peer, int amount)
    {
        if (Payer(peer, amount) is not int slot) return State.Creative;
        if (State.Creative) return true;
        var drained = Inventory(peer).ToArray();
        drained[slot] = drained[slot]! with { Charge = drained[slot]!.Charge - amount * Content.Charge.Scale };
        Stacks.SetInventory(State, peer, drained);
        return true;
    }

    /// <summary>Whether <see cref="Drain"/> would take <paramref name="amount"/> power-ticks from <paramref name="peer"/>'s belt now: always in creative mode.</summary>
    public bool CanDrain(int peer, int amount) => State.Creative || Payer(peer, amount) != null;

    /// <summary>The electric item <paramref name="peer"/>'s hero runs, drawing from its belt every tick (0055); none while it runs none.</summary>
    public string? Running(int peer) => State.Running.Find(r => r.Peer == peer)?.Item;

    /// <summary>
    /// Why <paramref name="peer"/>'s hit with <paramref name="weapon"/> would be refused, the same for whatever it hits (0054):
    /// <c>items.flat</c> for an electric weapon no battery on its belt can pay a tick of, boosted by its module as it runs
    /// (never in creative mode), and with
    /// <paramref name="running"/>, as a hit applies, <c>items.notRunning</c> for one its hero does not run (0055). The client asks
    /// without: it sends its <c>Run</c> before the hit, in the same frame.
    /// </summary>
    public string? WeaponProblem(int peer, string weapon, bool running)
    {
        if (Content.Items.Get(weapon).Draw is not int draw) return null;
        if (!CanDrain(peer, ToolStack(peer, weapon) is { } stack ? DrawOf(stack) : draw)) return "items.flat";
        return running && Running(peer) != weapon ? "items.notRunning" : null;
    }

    /// <summary>Stops whatever <paramref name="peer"/>'s hero runs: crew's, as the hero passes out or leaves. Nothing while it runs nothing.</summary>
    public void StopRunning(int peer) => State.Running.RemoveAll(r => r.Peer == peer);

    /// <summary>
    /// What a hero comes aboard with (0107), given the first time <paramref name="peer"/> has a hero: <see cref="Content.Inventory.Kit"/>
    /// in its first slots. Nothing for a peer that has an inventory already, emptied or not.
    /// </summary>
    public void Kit(int peer)
    {
        if (State.Inventories.Exists(h => h.Id == peer)) return;
        Give(peer, Content.Inventory.Kit);
    }

    /// <summary>
    /// <paramref name="peer"/>'s hero died in <paramref name="cell"/> (0061): every stack in its own slots that is
    /// not <c>Gear</c> falls there, still, in slot order, for anyone to pick up at once, each a part of one
    /// <see cref="Body"/> named by the peer and this tick. Gear and what is worn stay. Nothing in creative mode,
    /// where nothing is ever taken; nothing to leave leaves no body. The caller passes a cell on the grid.
    /// </summary>
    public void LeaveBody(int peer, int cell)
    {
        if (State.Creative) return;
        var slots = Inventory(peer).ToArray();
        var left = new List<Stack>();
        for (int i = 0; i < Own; i++)
            if (slots[i] is { } s && !Content.Items.Get(s.Item).Gear)
            {
                left.Add(s);
                slots[i] = null;
            }
        if (left.Count == 0) return;
        Stacks.SetInventory(State, peer, slots);
        var body = new Body(peer, State.Ticks);
        foreach (var s in left) FloorRules.LayDown(State, s, cell, 0, _grid.Width, FloorStack.Nobody, body);
    }

    /// <summary>The first belt slot of <paramref name="peer"/>'s, in slot order, whose battery holds at least <paramref name="amount"/> power-ticks; none for none.</summary>
    private int? Payer(int peer, int amount)
    {
        if (amount < 0) throw new ArgumentException($"items.drain: {amount} is no charge");
        var slots = Inventory(peer);
        foreach (int slot in Belt)
            if (slots[slot] is { } s && Content.Items.Get(s.Item).Capacity != null && s.Charge >= amount * Content.Charge.Scale) return slot;
        return null;
    }

    // Commands.

    /// <summary>
    /// <c>items.notElectric</c> for an item with no <c>Draw</c>; turning on, <c>items.notHeld</c> when the sender
    /// carries none, and <c>items.flat</c> when its belt cannot pay a tick of the draw, its module's boost in it (never in
    /// creative mode).
    /// </summary>
    private string? RunProblem(Run c)
    {
        if (Content.Items.Get(c.Item).Draw == null) return "items.notElectric";
        if (!c.On) return null;
        if (ToolStack(c.From, c.Item) is not { } stack) return "items.notHeld";
        return CanDrain(c.From, DrawOf(stack)) ? null : "items.flat";
    }

    /// <summary>A hero runs one thing: on replaces what it ran, off stops whatever it runs.</summary>
    private void ApplyRun(Run c)
    {
        StopRunning(c.From);
        if (!c.On) return;
        int at = State.Running.FindIndex(r => r.Peer > c.From);
        State.Running.Insert(at < 0 ? State.Running.Count : at, new Runner { Peer = c.From, Item = c.Item });
    }

    /// <summary>
    /// <c>items.noCrate</c> for a crate that names no machine holding items, <c>items.notCrate</c> for one holding
    /// them that is not a crate: a machine's slots have their places. Reach is the client's, as for a move.
    /// </summary>
    private string? SortProblem(Sort c)
    {
        if (c.Crate is not int id) return null;
        if (!IsCrate(id)) return "items.noCrate";
        return Content.Machines.Is(_grid.MachineById(id)!.Kind, Content.MachineTrait.Container) ? null : "items.notCrate";
    }

    private void ApplySort(Sort c) => SetSide(c.From, new Target(Crate: c.Crate), Sorted(SideOf(c.From, new Target(Crate: c.Crate)), c.Crate));

    /// <summary><paramref name="slots"/> sorted as <see cref="Sort"/> sorts them: a crate's all, an inventory's own past the hotbar.</summary>
    private Stack?[] Sorted(IReadOnlyList<Stack?> slots, int? crate) =>
        crate == null ? Stacks.Sorted(slots, Content.Inventory.Hotbar, Own) : Stacks.Sorted(slots, 0, slots.Count);

    private string? DropProblem(Drop c)
    {
        var problem = DropSide(side => SideOf(c.From, side), c.Src, out var slots)
            ?? Stacks.TakenFrom(slots, c.Src.Slot, c.Count, out _, out _);
        if (problem != null) return problem;
        return c.Cell < 0 || c.Cell >= _grid.Width * _grid.Height ? "items.noCell" : null;
    }

    private void ApplyDrop(Drop c)
    {
        if (Stacks.TakenFrom(SideOf(c.From, c.Src), c.Src.Slot, c.Count, out var left, out var taken) != null)
            throw new InvalidOperationException("items.drop: refused in apply");
        SetSide(c.From, c.Src, left);
        ForgetEmptied(c.Src);
        FloorRules.LayDown(State, taken, c.Cell, Math.Sign(c.Facing) * Content.Floor.ThrowSpeed, _grid.Width, c.From);
    }

    private string? SpawnProblem(Spawn c)
    {
        if (!State.Creative) return "items.notCreative";
        return Stacks.PlacedSome(Inventory(c.From), Spawned(c), Own).Left == c.Count ? "items.full" : null;
    }

    private void ApplySpawn(Spawn c) =>
        Stacks.SetInventory(State, c.From, Stacks.PlacedSome(Inventory(c.From), Spawned(c), Own).Slots);

    /// <summary>What a spawn makes, as one stack however many slots it takes; a count below 1 is a caller's bug.</summary>
    private static Stack Spawned(Spawn c)
    {
        if (c.Count < 1) throw new InvalidOperationException("items.spawn: a count below 1");
        return new Stack(c.Item, c.Count, Charge: Content.Items.Get(c.Item).Capacity ?? 0);
    }

    private void ApplyPickUp(PickUp c)
    {
        var stack = State.Floor.Find(s => s.Id == c.Id)
            ?? throw new InvalidOperationException("items.pickUp: applied to a stack that is gone");
        var (slots, left) = Stacks.PlacedSome(Inventory(c.From), stack.AsStack(), Own);
        Stacks.SetInventory(State, c.From, slots);
        stack.Count = left;
        if (stack.Count == 0) State.Floor.Remove(stack);
    }

    private string? MoveProblem(Move c) =>
        Into(c.Dst, out var dst)
        ?? MoveSides(side => SideOf(c.From, side), c.Src, dst, out var from, out var to, out bool same)
        ?? Moved(from, to, same, c.Src, dst, c.Count, out _, out _);

    private void ApplyMove(Move c)
    {
        if (MoveProblem(c) != null) throw new InvalidOperationException("items.move: refused in apply");
        Into(c.Dst, out var dst);
        MoveSides(side => SideOf(c.From, side), c.Src, dst, out var from, out var to, out bool same);
        Moved(from, to, same, c.Src, dst, c.Count, out var after, out var into);
        SetSide(c.From, c.Src, after);
        SetSide(c.From, dst, into);
        ForgetEmptied(c.Src);
    }

    // Helpers.

    /// <summary>
    /// Writes machine <paramref name="id"/>'s slots, every write of a machine's: each input slot that remembers (a fed
    /// station's, a trade station's, a fire's, <see cref="Remembers"/>) remembers what it holds now, or, emptied, what it held, so a slot
    /// burnt or smelted down to nothing still remembers its item (0062). Only a hand taking it out forgets.
    /// </summary>
    private void SetMachine(int id, Stack?[] slots)
    {
        if (Remembers(id) is int inputs)
        {
            var was = Crate(id);
            for (int slot = 0; slot < inputs && slot < slots.Length; slot++)
                if ((slots[slot] ?? was[slot])?.Item is { } item) Remember(id, slot, item);
        }
        Stacks.SetCrate(State, id, slots);
    }

    /// <summary>How many of machine <paramref name="id"/>'s first slots remember their item: a fed station's, a trade station's or a fire's inputs, its fuel slot too; null for any other.</summary>
    private int? Remembers(int id)
    {
        if (_grid.MachineById(id) is not { } m || !Machines.TryGet(m.Kind, out var spec) || spec.Slots is not int all) return null;
        bool fed = Content.Stations.TryGet(m.Kind, out var station) && station.Fed;
        return fed || spec.FuelSlot != null || Machines.Is(m.Kind, MachineTrait.Sells) ? spec.Inputs ?? all : null;
    }

    private void Remember(int id, int slot, string item)
    {
        int at = KeptIndex(id, slot);
        if (at >= 0)
        {
            if (State.Kept[at].Item != item) State.Kept[at] = new Kept(id, slot, item);
        }
        else State.Kept.Insert(~at, new Kept(id, slot, item));
    }

    /// <summary>A slot a hand has just taken everything out of forgets its item: <c>Move</c> and <c>Drop</c> from it.</summary>
    private void ForgetEmptied(Place src)
    {
        if (src.Crate is not int id || Crate(id) is var slots && src.Slot < slots.Count && slots[src.Slot] != null) return;
        int at = KeptIndex(id, src.Slot);
        if (at >= 0) State.Kept.RemoveAt(at);
    }

    /// <summary>Where machine <paramref name="id"/>'s slot <paramref name="slot"/> is in <c>Kept</c>, or the complement of where it would go.</summary>
    private int KeptIndex(int id, int slot)
    {
        int lo = 0, hi = State.Kept.Count - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            var k = State.Kept[mid];
            int order = k.Machine != id ? k.Machine.CompareTo(id) : k.Slot.CompareTo(slot);
            if (order == 0) return mid;
            if (order < 0) lo = mid + 1;
            else hi = mid - 1;
        }
        return ~lo;
    }

    /// <summary>What machine <paramref name="id"/> is, when it holds items: a crate, a furnace.</summary>
    private MachineSpec? HolderOf(int id)
    {
        var machine = _grid.MachineById(id);
        return machine != null && Machines.TryGet(machine.Kind, out var spec) && spec.Slots != null ? spec : null;
    }

    private bool IsCrate(int id) => HolderOf(id) != null;

    /// <summary>How many of machine <paramref name="id"/>'s first slots take items in: all of a crate's, none of a machine holding none.</summary>
    private int InputsOf(int id) => HolderOf(id) is { } spec ? InputsOf(spec) : 0;

    /// <summary>Machine <paramref name="id"/>'s slots with <paramref name="cost"/> placed in its outputs, or null when it does not fit or holds no items.</summary>
    private Stack?[]? Outputs(int id, IReadOnlyList<(string Item, int Count)> cost)
    {
        if (HolderOf(id) is not { } spec) return null;
        int inputs = InputsOf(spec);
        var slots = Crate(id);
        return Stacks.Placed(slots.Skip(inputs).ToList(), cost) is { } placed ? [.. slots.Take(inputs), .. placed] : null;
    }

    /// <summary>The slots of one side of a move: machine <c>Crate</c>, or <paramref name="peer"/>'s inventory when there is none.</summary>
    private IReadOnlyList<Stack?> SideOf(int peer, Target side) => side.Crate is int id ? Crate(id) : Inventory(peer);

    private IReadOnlyList<Stack?> SideOf(int peer, Place side) => SideOf(peer, (Target)side);

    /// <summary>Writes one side of a move back.</summary>
    private void SetSide(int peer, Target side, Stack?[] slots)
    {
        if (side.Crate is int id) SetMachine(id, slots);
        else Stacks.SetInventory(State, peer, slots);
    }

    /// <summary>
    /// Where a move puts what it moves: into a machine with inputs, its first slot when none is named,
    /// and never past its inputs. A refusal key for a slot only taken from.
    /// </summary>
    private string? Into(Target dst, out Target into)
    {
        into = dst;
        if (dst.Crate is not int id || HolderOf(id)?.Inputs is not int inputs) return null;
        // A machine that only gives out (the lifepod) takes nothing in, whichever slot is named.
        if (inputs == 0) return "items.takeOnly";
        if (dst.Slot is not int slot) { into = dst with { Slot = 0 }; return null; }
        return slot >= inputs ? "items.takeOnly" : null;
    }

    /// <summary>The slots a drop takes from, or why it is refused before its stack is looked at.</summary>
    private string? DropSide(Func<Target, IReadOnlyList<Stack?>> slotsOf, Place src, out IReadOnlyList<Stack?> slots)
    {
        slots = [];
        if (src.Crate is int id && !IsCrate(id)) return "items.noCrate";
        slots = slotsOf(src);
        return src.Slot >= 0 && src.Slot < slots.Count ? null : "items.noSlot";
    }

    /// <summary>
    /// Why a move is refused before any stack is looked at, or its two sides, each read by
    /// <paramref name="slotsOf"/>: the world's for a command, a fold's own copies for <see cref="AfterMoves"/>.
    /// </summary>
    private string? MoveSides(
        Func<Target, IReadOnlyList<Stack?>> slotsOf, Place src, Target dst,
        out IReadOnlyList<Stack?> from, out IReadOnlyList<Stack?> to, out bool same)
    {
        from = to = [];
        same = false;
        if (src.Crate is int a && !IsCrate(a)) return "items.noCrate";
        if (dst.Crate is int b && !IsCrate(b)) return "items.noCrate";
        from = slotsOf(src);
        to = slotsOf(dst);
        static bool InRange(int slot, IReadOnlyList<Stack?> slots) => slot >= 0 && slot < slots.Count;
        if (!InRange(src.Slot, from) || (dst.Slot is int d && !InRange(d, to))) return "items.noSlot";
        same = src.Crate == dst.Crate;
        // A side as a whole means the other side: within one side a move names its slot.
        if (same && (dst.Slot == null || dst.Slot == src.Slot)) return "items.noSlot";
        return null;
    }

    /// <summary>
    /// A move as <see cref="Stacks.Moved"/> makes it, onto an inventory as a whole into its own slots only,
    /// or why it is refused: also when it leaves an equipment slot of an inventory side holding what is not
    /// worn there (<c>items.notEquipment</c>), or a machine side holding what it does not take: a charging
    /// station anything without a charge to fill (<c>items.notBattery</c>), an arc bench's tool slot anything but a tool a module
    /// fits (<c>items.notTool</c>, 0099), a machine with <c>Takes</c> anything else (<c>items.notTaken</c>), put in or swapped in.
    /// </summary>
    private string? Moved(
        IReadOnlyList<Stack?> from, IReadOnlyList<Stack?> to, bool same, Place src, Target dst, int count,
        out Stack?[] after, out Stack?[] into)
    {
        (after, into) = ([], []);
        if (dst.Slot is int slot && !SlotOpen(new Place(slot, dst.Crate))) return "items.noSlot";
        return Stacks.Moved(from, to, same, src, dst, count, out after, out into, dst.Crate == null ? Own : null)
            ?? (src.Crate is int a ? Stacks.Unfit(HolderOf(a)!, after) : Stacks.Misfit(after))
            ?? (dst.Crate is int b ? Stacks.Unfit(HolderOf(b)!, into) : Stacks.Misfit(into));
    }

    /// <summary>
    /// A fed station's outputs after one smelt of its input: its yields and <paramref name="besides"/> placed, by default
    /// the most it may make (<see cref="MostMade"/>); null when its input holds nothing worked, fewer than one smelt
    /// takes, or they do not fit.
    /// </summary>
    private static Stack?[]? Smelted(IReadOnlyList<Stack?> slots, int inputs, IReadOnlyList<(string Item, int Count)>? besides = null)
    {
        if (slots.Count == 0 || slots[0] is not { } input) return null;
        var spec = Content.Items.Get(input.Item);
        if (spec.Yields == null || input.Count < spec.SmeltTakes) return null;
        // Fewer of anything fit wherever more do, so room for the most is room for any draw.
        IReadOnlyList<(string Item, int Count)> made = besides == null ? MostMade(spec) : [.. spec.Yields, .. besides];
        return Stacks.Placed(slots.Skip(inputs).ToList(), made);
    }

    /// <summary>
    /// The most one smelt of <paramref name="spec"/> makes: its yields, the most of itself its <c>Returns</c> give back,
    /// every byproduct at its <c>Max</c>. What a fed station's outputs must have room for before it works.
    /// </summary>
    public static IReadOnlyList<(string Item, int Count)> MostMade(ItemSpec spec)
    {
        var made = new List<(string Item, int Count)>(spec.Yields ?? []);
        if (spec.Returns is { Count: > 1 } weights) made.Add((spec.Id, weights.Count - 1));
        foreach (var by in spec.Byproducts ?? [])
            if (by.Max > 0) made.Add((by.Item, by.Max));
        return made;
    }
}
