using Stillwake.Content;
using Stillwake.Sim.Grid;
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
    IReadOnlyList<(string Item, int Count)> Totals(int peer);
    string? ShortOf(int peer, IReadOnlyList<(string Item, int Count)> cost);
    string? FullFor(int peer, IReadOnlyList<(string Item, int Count)> cost);
    string? OutputFull(int id, IReadOnlyList<(string Item, int Count)> cost);
    IReadOnlyList<FloorStack> Floor { get; }
    string? PickUpProblem(int peer, int id);
    int PickUpAmount(int peer, int id);
    Shown AfterMoves(int peer, IReadOnlyList<Command> moves);
}

/// <summary>How a module that depends on the items may change them.</summary>
public interface IItemsOps
{
    IReadOnlyList<(string Item, int Count)> Take(int peer, IReadOnlyList<(string Item, int Count)> cost);
    void Give(int peer, IReadOnlyList<(string Item, int Count)> cost);
    void Smelt(int id);
    void Consume(int id, int slot);
    void Output(int id, IReadOnlyList<(string Item, int Count)> cost);
    void GiveOrDrop(int peer, IReadOnlyList<(string Item, int Count)> cost, int cell);
    void DropAt(int peer, IReadOnlyList<(string Item, int Count)> cost, int cell);
}

/// <summary>
/// Every stack of items: inventories, what machines hold, the floor, creative mode
/// (packages/sim/src/modules/items). A cost is a list of items with whole amounts, as content writes them.
/// </summary>
public sealed partial class ItemsModule : Module<ItemsState>, IItemsQueries, IItemsOps
{
    private readonly IGridQueries _grid;
    private readonly IRoomsQueries _rooms;

    public override string Id => "items";

    public ItemsModule(IGridQueries grid, IRoomsQueries rooms)
    {
        _grid = grid;
        _rooms = rooms;
        Handles<Drop>(DropProblem, ApplyDrop);
        Handles<PickUp>(c => PickUpProblem(c.From, c.Id), ApplyPickUp);
        Handles<SetCreative>(_ => null, c => State.Creative = c.On);
        Handles<Move>(MoveProblem, ApplyMove);
        // After derivation: falling reads the rooms, rebuilt by then.
        System(Phase.Simulation, () =>
        {
            State.Ticks += 1;
            FloorRules.Settle(State, _grid, _rooms);
        });
    }

    protected override ItemsState Init(Layout layout) => new();

    // Queries.

    /// <summary><paramref name="peer"/>'s slots in order, each a stack or null; live and read-only.</summary>
    public IReadOnlyList<Stack?> Inventory(int peer) => Stacks.InventoryOf(State, peer);

    /// <summary>What machine <paramref name="id"/> holds, slot by slot: a crate, a furnace; none for a machine holding no items.</summary>
    public IReadOnlyList<Stack?> Crate(int id) => Stacks.CrateOf(State, id, HolderOf(id)?.Slots ?? 0);

    /// <summary>Whether furnace <paramref name="id"/> has salvage in its input whose yields fit its outputs.</summary>
    public bool CanSmelt(int id) => HolderOf(id)?.Inputs is int inputs && Smelted(Crate(id), inputs) != null;

    public bool Creative => State.Creative;

    /// <summary>How much of each item <paramref name="peer"/> carries over all its slots, in the order first found: what it could pay with.</summary>
    public IReadOnlyList<(string Item, int Count)> Totals(int peer)
    {
        var totals = new List<(string Item, int Count)>();
        foreach (var s in Inventory(peer))
        {
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
        bool cannot = Stacks.Taken(Inventory(peer), cost) == null;
        return cannot && !State.Creative ? "items.notEnough" : null;
    }

    /// <summary><c>items.full</c> when <paramref name="cost"/> does not fit into <paramref name="peer"/>'s inventory, creative mode or not.</summary>
    public string? FullFor(int peer, IReadOnlyList<(string Item, int Count)> cost) =>
        Stacks.Placed(Inventory(peer), cost) == null ? "items.full" : null;

    /// <summary>
    /// <c>items.full</c> when <paramref name="cost"/> does not fit into machine <paramref name="id"/>'s
    /// outputs, its slots past its inputs: where what a crafting machine makes goes.
    /// </summary>
    public string? OutputFull(int id, IReadOnlyList<(string Item, int Count)> cost) =>
        Outputs(id, cost) == null ? "items.full" : null;

    /// <summary>The stacks on the floor, in id order; live and read-only.</summary>
    public IReadOnlyList<FloorStack> Floor => State.Floor;

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
        var (_, left) = Stacks.PlacedSome(Inventory(peer), [(stack.Item, stack.Count)]);
        return stack.Count - (left.Count > 0 ? left[0].Count : 0);
    }

    /// <summary>
    /// <paramref name="peer"/>'s inventory and the machines <paramref name="moves"/> touch — <see cref="Move"/>s
    /// and <see cref="Drop"/>s — as they would be after them, in order, each as the command would apply it and
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
                    if (Stacks.Moved(from, to, same, mv.Src, dst, mv.Count, out var after, out var into) != null) break;
                    Show(mv.Src.Crate, after);
                    Show(mv.Dst.Crate, into);
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
        var left = Stacks.Taken(Inventory(peer), cost)
            ?? throw new InvalidOperationException("items.take: not enough; check ShortOf first");
        Stacks.SetInventory(State, peer, left);
        return [.. cost];
    }

    /// <summary>Gives <paramref name="cost"/> to <paramref name="peer"/>, in creative mode too. The caller checks <see cref="FullFor"/> first.</summary>
    public void Give(int peer, IReadOnlyList<(string Item, int Count)> cost)
    {
        if (Stacks.IsNothing(cost)) return;
        var more = Stacks.Placed(Inventory(peer), cost)
            ?? throw new InvalidOperationException("items.give: no room; check FullFor first");
        Stacks.SetInventory(State, peer, more);
    }

    /// <summary>Turns one of what furnace <paramref name="id"/> holds in its input into its yields, put into its outputs. The caller checks <see cref="CanSmelt"/> first.</summary>
    public void Smelt(int id)
    {
        int inputs = HolderOf(id)?.Inputs ?? 0;
        var slots = Crate(id);
        var outputs = Smelted(slots, inputs);
        if (outputs == null || Stacks.TakenFrom(slots, 0, 1, out var left, out _) != null)
            throw new InvalidOperationException("items.smelt: nothing to smelt; check CanSmelt");
        Stacks.SetCrate(State, id, [.. left.Take(inputs), .. outputs]);
    }

    /// <summary>Uses up one of what slot <paramref name="slot"/> of machine <paramref name="id"/> holds: fuel burnt. The caller checks it holds one.</summary>
    public void Consume(int id, int slot)
    {
        if (Stacks.TakenFrom(Crate(id), slot, 1, out var left, out _) != null)
            throw new InvalidOperationException("items.consume: nothing there");
        Stacks.SetCrate(State, id, left);
    }

    /// <summary>Puts <paramref name="cost"/> into machine <paramref name="id"/>'s outputs: what it made. The caller checks <see cref="OutputFull"/> first.</summary>
    public void Output(int id, IReadOnlyList<(string Item, int Count)> cost)
    {
        if (Stacks.IsNothing(cost)) return;
        var slots = Outputs(id, cost) ?? throw new InvalidOperationException("items.output: no room; check OutputFull first");
        Stacks.SetCrate(State, id, slots);
    }

    /// <summary>Gives <paramref name="peer"/> as much of <paramref name="cost"/> as fits; the rest falls on <paramref name="cell"/>, as if <paramref name="peer"/> dropped it.</summary>
    public void GiveOrDrop(int peer, IReadOnlyList<(string Item, int Count)> cost, int cell)
    {
        if (Stacks.IsNothing(cost)) return;
        var (slots, left) = Stacks.PlacedSome(Inventory(peer), cost);
        Stacks.SetInventory(State, peer, slots);
        DropAt(peer, left, cell);
    }

    /// <summary>Lets <paramref name="cost"/> fall on cell <paramref name="cell"/> as if peer <paramref name="peer"/> dropped it there, still; <see cref="FloorStack.Nobody"/> for a stack anyone may take at once.</summary>
    public void DropAt(int peer, IReadOnlyList<(string Item, int Count)> cost, int cell)
    {
        foreach (var (item, count) in Stacks.Entries(cost))
            if (count > 0) FloorRules.LayDown(State, item, count, cell, 0, _grid.Width, peer);
    }

    // Commands.

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
        FloorRules.LayDown(State, taken.Item, taken.Count, c.Cell, Math.Sign(c.Facing) * Content.Floor.ThrowSpeed, _grid.Width, c.From);
    }

    private void ApplyPickUp(PickUp c)
    {
        var stack = State.Floor.Find(s => s.Id == c.Id)
            ?? throw new InvalidOperationException("items.pickUp: applied to a stack that is gone");
        var (slots, left) = Stacks.PlacedSome(Inventory(c.From), [(stack.Item, stack.Count)]);
        Stacks.SetInventory(State, c.From, slots);
        stack.Count = left.Count > 0 ? left[0].Count : 0;
        if (stack.Count == 0) State.Floor.Remove(stack);
    }

    private string? MoveProblem(Move c) =>
        Into(c.Dst, out var dst)
        ?? MoveSides(side => SideOf(c.From, side), c.Src, dst, out var from, out var to, out bool same)
        ?? Stacks.Moved(from, to, same, c.Src, dst, c.Count, out _, out _);

    private void ApplyMove(Move c)
    {
        if (MoveProblem(c) != null) throw new InvalidOperationException("items.move: refused in apply");
        Into(c.Dst, out var dst);
        MoveSides(side => SideOf(c.From, side), c.Src, dst, out var from, out var to, out bool same);
        Stacks.Moved(from, to, same, c.Src, dst, c.Count, out var after, out var into);
        SetSide(c.From, c.Src, after);
        SetSide(c.From, dst, into);
    }

    // Helpers.

    /// <summary>What machine <paramref name="id"/> is, when it holds items: a crate, a furnace.</summary>
    private MachineSpec? HolderOf(int id)
    {
        var machine = _grid.MachineById(id);
        return machine != null && Machines.TryGet(machine.Kind, out var spec) && spec.Slots != null ? spec : null;
    }

    private bool IsCrate(int id) => HolderOf(id) != null;

    /// <summary>Machine <paramref name="id"/>'s slots with <paramref name="cost"/> placed in its outputs, or null when it does not fit or holds no items.</summary>
    private Stack?[]? Outputs(int id, IReadOnlyList<(string Item, int Count)> cost)
    {
        if (HolderOf(id) is not { } spec) return null;
        int inputs = spec.Inputs ?? spec.Slots ?? 0;
        var slots = Crate(id);
        return Stacks.Placed(slots.Skip(inputs).ToList(), cost) is { } placed ? [.. slots.Take(inputs), .. placed] : null;
    }

    /// <summary>The slots of one side of a move: machine <c>Crate</c>, or <paramref name="peer"/>'s inventory when there is none.</summary>
    private IReadOnlyList<Stack?> SideOf(int peer, Target side) => side.Crate is int id ? Crate(id) : Inventory(peer);

    private IReadOnlyList<Stack?> SideOf(int peer, Place side) => SideOf(peer, (Target)side);

    /// <summary>Writes one side of a move back.</summary>
    private void SetSide(int peer, Target side, Stack?[] slots)
    {
        if (side.Crate is int id) Stacks.SetCrate(State, id, slots);
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

    /// <summary>A furnace's outputs after smelting one of its input, or null when its input holds no salvage or its yields do not fit.</summary>
    private static Stack?[]? Smelted(IReadOnlyList<Stack?> slots, int inputs)
    {
        if (slots.Count == 0 || slots[0] is not { } input) return null;
        var yields = Content.Items.Get(input.Item).Yields;
        return yields == null ? null : Stacks.Placed(slots.Skip(inputs).ToList(), yields);
    }
}
