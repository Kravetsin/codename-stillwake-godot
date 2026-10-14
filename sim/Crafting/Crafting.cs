using Stillwake.Content;
using Stillwake.Sim.Items;
using Stillwake.Sim.Research;
using Cost = System.Collections.Generic.IReadOnlyList<(string Item, int Count)>;

namespace Stillwake.Sim.Crafting;

/// <summary>Queues <c>Count</c> units of the craft making <c>Recipe</c>, all their cost paid at once.</summary>
public sealed record Craft(string Recipe, int Count) : Command;

/// <summary>Takes entry <c>Entry</c> off the sender's queue, giving back what its units left cost, the one at work included.</summary>
public sealed record Cancel(int Entry) : Command;

/// <summary>
/// Crafting by hand: each peer's queue of crafts, paid when queued, made one unit at a time into its
/// inventory. A module of its own, not part of <c>Items</c>: it asks <c>Research</c> what is known,
/// and research depends on items.
/// </summary>
public sealed partial class CraftingModule : Module<CraftingState>
{
    private static readonly IReadOnlyList<Entry> Nothing = [];

    private readonly IItemsQueries _items;
    private readonly IItemsOps _itemsOps;
    private readonly IResearchQueries _research;

    public override string Id => "crafting";

    public CraftingModule(IItemsQueries items, IItemsOps itemsOps, IResearchQueries research)
    {
        _items = items;
        _itemsOps = itemsOps;
        _research = research;
        Handles<Craft>(CraftProblem, ApplyCraft);
        Handles<Cancel>(CancelProblem, ApplyCancel);
        System(Phase.Simulation, Work);
    }

    protected override CraftingState Init(Layout layout) => new();

    // Queries.

    /// <summary><paramref name="peer"/>'s queue, the first entry at work; live and read-only.</summary>
    public IReadOnlyList<Entry> Queue(int peer) => QueueOf(peer)?.Entries ?? Nothing;

    /// <summary>Ticks of work on <paramref name="peer"/>'s first entry's unit: its craft's ticks while the unit waits for room.</summary>
    public int Progress(int peer) => QueueOf(peer)?.Progress ?? 0;

    // Commands.

    private string? CraftProblem(Craft c)
    {
        if (!Crafts.TryGet(c.Recipe, out var craft) || !_research.Knows(c.Recipe)) return "crafting.unknown";
        if (Queue(c.From).Count >= Content.Crafting.QueueMost) return "crafting.full";
        return _items.ShortOf(c.From, Times(craft.Cost, c.Count));
    }

    private void ApplyCraft(Craft c)
    {
        var queue = QueueOf(c.From) ?? Added(c.From);
        _itemsOps.Take(c.From, Times(Crafts.Get(c.Recipe).Cost, c.Count));
        queue.Entries.Add(new Entry(queue.Next, c.Recipe, c.Count));
        queue.Next += 1;
    }

    private string? CancelProblem(Cancel c) =>
        Queue(c.From).FirstOrDefault(e => e.Id == c.Entry) is { } entry ? _items.FullFor(c.From, Refund(entry)) : "crafting.noEntry";

    private void ApplyCancel(Cancel c)
    {
        var queue = QueueOf(c.From)!;
        int at = queue.Entries.FindIndex(e => e.Id == c.Entry);
        var entry = queue.Entries[at];
        queue.Entries.RemoveAt(at);
        // The unit at work is given back with the rest: the next entry starts from nothing.
        if (at == 0) queue.Progress = 0;
        _itemsOps.Give(c.From, Refund(entry));
    }

    // Systems.

    /// <summary>Each queue's first entry works a tick; at its ticks a unit goes into the inventory, or waits for room.</summary>
    private void Work()
    {
        foreach (var queue in State.Queues)
        {
            if (queue.Entries.Count == 0) continue;
            var first = queue.Entries[0];
            var craft = Crafts.Get(first.Recipe);
            if (queue.Progress < craft.Ticks) queue.Progress += 1;
            if (queue.Progress < craft.Ticks) continue;
            Cost unit = [(craft.Item, 1)];
            if (_items.FullFor(queue.Peer, unit) != null) continue;
            _itemsOps.Give(queue.Peer, unit);
            queue.Progress = 0;
            if (first.Count > 1) queue.Entries[0] = first with { Count = first.Count - 1 };
            else queue.Entries.RemoveAt(0);
        }
    }

    // Helpers.

    private Queue? QueueOf(int peer) => State.Queues.Find(q => q.Peer == peer);

    /// <summary>A new queue for <paramref name="peer"/>, kept in peer order.</summary>
    private Queue Added(int peer)
    {
        var queue = new Queue { Peer = peer };
        int at = State.Queues.FindIndex(q => q.Peer > peer);
        State.Queues.Insert(at < 0 ? State.Queues.Count : at, queue);
        return queue;
    }

    /// <summary>What <paramref name="entry"/>'s units left cost, as content prices them now.</summary>
    private static Cost Refund(Entry entry) => Times(Crafts.Get(entry.Recipe).Cost, entry.Count);

    private static Cost Times(Cost cost, int count) => cost.Select(c => (c.Item, c.Count * count)).ToList();
}
