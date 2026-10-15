using Stillwake.Content;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Research;
using Cost = System.Collections.Generic.IReadOnlyList<(string Item, int Count)>;

namespace Stillwake.Sim.Crafting;

/// <summary>Queues <c>Count</c> units of the craft making <c>Recipe</c> at machine <c>Machine</c>, all their cost paid by the sender at once.</summary>
public sealed record Craft(int Machine, string Recipe, int Count) : Command;

/// <summary>Takes entry <c>Entry</c> off machine <c>Machine</c>'s queue, giving the sender what its units left cost, the one at work included.</summary>
public sealed record Cancel(int Machine, int Entry) : Command;

/// <summary>
/// Crafting by machine only (ADR 0028): each crafting machine's queue, shared by the crew, paid when
/// queued, made one unit at a time into the machine's outputs. A module of its own, not part of
/// <c>Items</c>: it asks <c>Research</c> what is known, and research depends on items.
/// </summary>
public sealed partial class CraftingModule : Module<CraftingState>
{
    /// <summary>The one crafting machine while it is the only one: a craft names its machine once a second comes.</summary>
    private const string Pod = "lifepod";

    private static readonly IReadOnlyList<Entry> Nothing = [];

    private readonly IGridQueries _grid;
    private readonly IItemsQueries _items;
    private readonly IItemsOps _itemsOps;
    private readonly IResearchQueries _research;

    public override string Id => "crafting";

    public CraftingModule(IGridQueries grid, IItemsQueries items, IItemsOps itemsOps, IResearchQueries research)
    {
        _grid = grid;
        _items = items;
        _itemsOps = itemsOps;
        _research = research;
        Handles<Craft>(CraftProblem, ApplyCraft);
        Handles<Cancel>(CancelProblem, ApplyCancel);
        System(Phase.Simulation, Work);
    }

    protected override CraftingState Init(Layout layout) => new();

    // Queries.

    /// <summary>Machine <paramref name="machine"/>'s queue, the first entry at work; live and read-only.</summary>
    public IReadOnlyList<Entry> Queue(int machine) => QueueOf(machine)?.Entries ?? Nothing;

    /// <summary>
    /// Work on machine <paramref name="machine"/>'s first entry's unit, in thousandths of a tick at full
    /// speed: its craft's ticks × 1000 while the unit waits for room.
    /// </summary>
    public int Progress(int machine) => QueueOf(machine)?.Progress ?? 0;

    /// <summary>Whether machine <paramref name="machine"/> crafts: the lifepod, while it is the only crafting machine.</summary>
    public bool Crafts(int machine) => _grid.Machines.FirstOrDefault(m => m.Id == machine)?.Kind == Pod;

    // Commands.

    private string? CraftProblem(Craft c)
    {
        if (!Crafts(c.Machine)) return "crafting.noStation";
        if (!Content.Crafts.TryGet(c.Recipe, out var craft) || !_research.Knows(c.Recipe)) return "crafting.unknown";
        if (Queue(c.Machine).Count >= Content.Crafting.QueueMost) return "crafting.full";
        return _items.ShortOf(c.From, Times(craft.Cost, c.Count));
    }

    private void ApplyCraft(Craft c)
    {
        var queue = QueueOf(c.Machine) ?? Added(c.Machine);
        _itemsOps.Take(c.From, Times(Content.Crafts.Get(c.Recipe).Cost, c.Count));
        queue.Entries.Add(new Entry(queue.Next, c.Recipe, c.Count, c.From));
        queue.Next += 1;
    }

    private string? CancelProblem(Cancel c)
    {
        if (!Crafts(c.Machine)) return "crafting.noStation";
        return Queue(c.Machine).FirstOrDefault(e => e.Id == c.Entry) is { } entry
            ? _items.FullFor(c.From, Refund(entry))
            : "crafting.noEntry";
    }

    private void ApplyCancel(Cancel c)
    {
        var queue = QueueOf(c.Machine)!;
        int at = queue.Entries.FindIndex(e => e.Id == c.Entry);
        var entry = queue.Entries[at];
        queue.Entries.RemoveAt(at);
        // The unit at work is given back with the rest: the next entry starts from nothing.
        if (at == 0) queue.Progress = 0;
        // To whoever cancels, not whoever paid: friends share a ship, and no record of who paid is kept.
        _itemsOps.Give(c.From, Refund(entry));
    }

    // Systems.

    /// <summary>
    /// Each machine's first entry works a tick at the pod's speed; a unit done goes into its outputs, or
    /// waits, done, for room there.
    /// </summary>
    private void Work()
    {
        foreach (var queue in State.Queues)
        {
            if (queue.Entries.Count == 0) continue;
            var first = queue.Entries[0];
            var craft = Content.Crafts.Get(first.Recipe);
            int work = craft.Ticks * 1000;
            if (queue.Progress < work) queue.Progress = Math.Min(work, queue.Progress + Content.Crafts.PodSpeed);
            if (queue.Progress < work) continue;
            Cost unit = [(craft.Item, 1)];
            if (_items.OutputFull(queue.Machine, unit) != null) continue;
            _itemsOps.Output(queue.Machine, unit);
            queue.Progress = 0;
            if (first.Count > 1) queue.Entries[0] = first with { Count = first.Count - 1 };
            else queue.Entries.RemoveAt(0);
        }
    }

    // Helpers.

    private Queue? QueueOf(int machine) => State.Queues.Find(q => q.Machine == machine);

    /// <summary>A new queue for <paramref name="machine"/>, kept in machine order.</summary>
    private Queue Added(int machine)
    {
        var queue = new Queue { Machine = machine };
        int at = State.Queues.FindIndex(q => q.Machine > machine);
        State.Queues.Insert(at < 0 ? State.Queues.Count : at, queue);
        return queue;
    }

    /// <summary>What <paramref name="entry"/>'s units left cost, as content prices them now.</summary>
    private static Cost Refund(Entry entry) => Times(Content.Crafts.Get(entry.Recipe).Cost, entry.Count);

    private static Cost Times(Cost cost, int count) => cost.Select(c => (c.Item, c.Count * count)).ToList();
}
