using Stillwake.Content;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Power;
using Stillwake.Sim.Research;
using Cost = System.Collections.Generic.IReadOnlyList<(string Item, int Count)>;

namespace Stillwake.Sim.Crafting;

/// <summary>Queues <c>Count</c> units of the craft making <c>Recipe</c> at station <c>Machine</c>, all their cost paid by the sender at once.</summary>
public sealed record Craft(int Machine, string Recipe, int Count) : Command;

/// <summary>Takes entry <c>Entry</c> off station <c>Machine</c>'s queue, giving the sender what its units left cost, the one at work included.</summary>
public sealed record Cancel(int Machine, int Entry) : Command;

/// <summary>What any module may read of the crafting. Lists are live: a reader never writes them.</summary>
public interface ICraftingQueries
{
    /// <summary>The station machine <paramref name="machine"/> is, as it works now; null for a machine that is none, or none at all.</summary>
    Station? StationOf(int machine);
    /// <summary>Queued station <paramref name="machine"/>'s queue, the first entry at work; none for any other machine.</summary>
    IReadOnlyList<Entry> Queue(int machine);
    /// <summary>
    /// Work on station <paramref name="machine"/>'s unit, in thousandths of a tick at full speed: a queued one's
    /// first entry (its full work while that unit waits for room), a fed one's input.
    /// </summary>
    int Progress(int machine);
    /// <summary>
    /// Whether fed station <paramref name="machine"/> has what it can smelt: an item that smelts in its input, at
    /// most as hot as it is, its yields fitting its outputs. What lights a furnace's fire.
    /// </summary>
    bool CanSmelt(int machine);
}

/// <summary>How a module that depends on the crafting may change it.</summary>
public interface ICraftingOps
{
    /// <summary>Fed station <paramref name="machine"/>'s fire burns this tick: told before crafting works, forgotten after.</summary>
    void Heat(int machine);
}

/// <summary>
/// Stations (0057): every crafting machine on one model, its work counted in thousandths of a tick at its
/// speed times the share of its energy it gets. A queued station is paid when a craft is queued and makes it
/// one unit at a time into its outputs (0028); a fed one smelts what its input holds into that item's yields.
/// A module of its own, not part of <c>Items</c>: it asks <c>Research</c> what is known, and research depends
/// on items.
/// </summary>
public sealed partial class CraftingModule : Module<CraftingState>, ICraftingQueries, ICraftingOps
{
    private static readonly IReadOnlyList<Entry> Nothing = [];

    private readonly IGridQueries _grid;
    private readonly IItemsQueries _items;
    private readonly IItemsOps _itemsOps;
    private readonly IResearchQueries _research;
    private readonly IPowerQueries _power;
    private readonly IPowerOps _powerOps;

    public override string Id => "crafting";

    public CraftingModule(
        IGridQueries grid, IItemsQueries items, IItemsOps itemsOps, IResearchQueries research,
        IPowerQueries power, IPowerOps powerOps)
    {
        _grid = grid;
        _items = items;
        _itemsOps = itemsOps;
        _research = research;
        _power = power;
        _powerOps = powerOps;
        Handles<Craft>(CraftProblem, ApplyCraft);
        Handles<Cancel>(CancelProblem, ApplyCancel);
        // After power's derivation: a powered station works at this tick's satisfaction.
        System(Phase.Simulation, Work);
    }

    protected override CraftingState Init(Layout layout) => new();

    // Queries.

    public Station? StationOf(int machine) => _grid.MachineById(machine) is { } m ? StationOf(m) : null;

    public IReadOnlyList<Entry> Queue(int machine) => QueueOf(machine)?.Entries ?? Nothing;

    public int Progress(int machine)
    {
        if (QueueOf(machine) is { } queue) return queue.Progress;
        return SmeltOf(machine) is { } smelt && smelt.Item == InputOf(machine) ? smelt.Progress : 0;
    }

    public bool CanSmelt(int machine) =>
        _grid.MachineById(machine) is { } m && StationOf(m) is { Fed: true } station && InputOf(machine) is { } input && CanSmelt(m, station, input);

    // Operations.

    public void Heat(int machine) => State.Heated.Add(machine);

    // Commands.

    private string? CraftProblem(Craft c)
    {
        if (StationOf(c.Machine) is not { } station) return "crafting.noStation";
        if (!Content.Crafts.TryGet(c.Recipe, out var craft) || !_research.Knows(c.Recipe)) return "crafting.unknown";
        if (!Makes(station, c.Recipe)) return "crafting.notHere";
        if (Queue(c.Machine).Count >= station.QueueMost) return "crafting.full";
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
        if (StationOf(c.Machine) is not { } station) return "crafting.noStation";
        if (station.Fed) return "crafting.notHere";
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

    /// <summary>Every station a tick, in machine id order: a queued one on its first entry, a fed one on its input.</summary>
    private void Work()
    {
        var smelts = new List<Smelt>();
        var was = State.Smelts;
        int next = 0;
        foreach (var m in _grid.Machines)
        {
            if (StationOf(m) is not { } station) continue;
            if (!station.Fed)
            {
                Make(m, station);
                continue;
            }
            // Smelts and stations are both in id order: one pass finds each station's.
            while (next < was.Count && was[next].Machine < m.Id) next++;
            if (Smelt(m, station, next < was.Count && was[next].Machine == m.Id ? was[next] : null) is { } smelt) smelts.Add(smelt);
        }
        State.Smelts = smelts;
        State.Heated.Clear();
    }

    /// <summary>
    /// A queued station's first entry works a tick; a unit done goes into its outputs, or waits, done, for room
    /// there. One that draws power rests while it has nothing to work on.
    /// </summary>
    private void Make(Machine m, Station station)
    {
        var queue = QueueOf(m.Id);
        if (queue is { Entries.Count: > 0 })
        {
            var first = queue.Entries[0];
            var craft = Content.Crafts.Get(first.Recipe);
            int work = craft.Ticks * 1000;
            if (queue.Progress < work) queue.Progress = Math.Min(work, queue.Progress + Gain(m, station));
            Cost unit = [(craft.Item, 1)];
            if (queue.Progress >= work && _items.OutputFull(m.Id, unit) == null)
            {
                _itemsOps.Output(m.Id, unit);
                queue.Progress = 0;
                if (first.Count > 1) queue.Entries[0] = first with { Count = first.Count - 1 };
                else queue.Entries.RemoveAt(0);
            }
        }
        // Idle: nothing queued, or a unit done waiting for room, which needs no power to go out.
        bool works = queue is { Entries.Count: > 0 } && queue.Progress < Content.Crafts.Get(queue.Entries[0].Recipe).Ticks * 1000;
        Rest(m, !works);
    }

    /// <summary>
    /// A fed station a tick on from <paramref name="was"/>: work on another item, or none, begins again; with
    /// what it can smelt and its energy, it works a tick, and at the item's work turns one of it into its yields
    /// (<c>IItemsOps.Smelt</c>). Null when nothing is part-way.
    /// </summary>
    private Smelt? Smelt(Machine m, Station station, Smelt? was)
    {
        var input = InputOf(m.Id);
        var kept = was != null && was.Item == input ? was : null;
        bool works = input != null && CanSmelt(m, station, input);
        Rest(m, !works);
        int gain = works ? Gain(m, station) : 0;
        if (gain == 0) return kept;
        int progress = (kept?.Progress ?? 0) + gain;
        if (progress < Content.Items.Get(input!).SmeltTicks!.Value * 1000) return new Smelt(m.Id, input!, progress);
        _itemsOps.Smelt(m.Id);
        return null;
    }

    // Helpers.

    /// <summary>
    /// The station <paramref name="m"/> is, as it works now: its entry in <c>Content.Stations</c>. The one place a
    /// station's speed, queue size and heat are read, so what upgrades a machine folds in here.
    /// </summary>
    private static Station? StationOf(Machine m) => Stations.TryGet(m.Kind, out var station) ? station : null;

    /// <summary>Whether queued <paramref name="station"/> makes <paramref name="recipe"/>: it is in its rows.</summary>
    private static bool Makes(Station station, string recipe)
    {
        foreach (var row in station.Rows)
            if (row.Contains(recipe)) return true;
        return false;
    }

    /// <summary>
    /// The work station <paramref name="m"/> adds this tick: its speed times the share of its energy it gets, in
    /// thousandths, rounded down. Drawing power, its network's satisfaction; with a fire, all while it burns
    /// (<see cref="Heat"/>), none while it is out; with neither (the pod), all.
    /// </summary>
    private int Gain(Machine m, Station station)
    {
        var spec = Content.Machines.Get(m.Kind);
        int share = spec.Draws != null ? _power.Satisfaction(m.Id)
            : spec.FuelSlot != null ? (State.Heated.Contains(m.Id) ? 1000 : 0)
            : 1000;
        // Both are never negative, so integer division is the floor.
        return station.Speed * share / 1000;
    }

    /// <summary>Tells power a station that draws to rest while it has nothing to work on: it draws nothing from the next tick.</summary>
    private void Rest(Machine m, bool idle)
    {
        if (Content.Machines.Get(m.Kind).Draws != null) _powerOps.Rest(m.Id, idle);
    }

    /// <summary>Whether fed <paramref name="station"/> <paramref name="m"/> can smelt <paramref name="input"/>: it smelts, hot enough there, its yields fitting.</summary>
    private bool CanSmelt(Machine m, Station station, string input) =>
        Content.Items.Get(input) is { Yields: not null } spec && spec.Heat <= station.Heat && _items.CanSmelt(m.Id);

    /// <summary>What machine <paramref name="machine"/>'s first slot holds: a fed station's input.</summary>
    private string? InputOf(int machine)
    {
        var slots = _items.Crate(machine);
        return slots.Count == 0 ? null : slots[0]?.Item;
    }

    private Queue? QueueOf(int machine) => State.Queues.Find(q => q.Machine == machine);

    private Smelt? SmeltOf(int machine) => State.Smelts.Find(s => s.Machine == machine);

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
