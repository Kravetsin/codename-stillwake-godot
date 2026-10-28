using Stillwake.Content;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Power;
using Stillwake.Sim.Research;
using Cost = System.Collections.Generic.IReadOnlyList<(string Item, int Count)>;

namespace Stillwake.Sim.Crafting;

/// <summary>
/// Queues <c>Count</c> units of the craft making <c>Recipe</c> at station <c>Machine</c>: paid by the sender at once,
/// or with <c>FromInputs</c> paid by nothing, each unit taking its cost from the station's inputs as it begins.
/// </summary>
public sealed record Craft(int Machine, string Recipe, int Count, bool FromInputs = false) : Command;

/// <summary>
/// Takes entry <c>Entry</c> off station <c>Machine</c>'s queue, giving the sender what was paid for it and not made: a
/// paid one's units left, the one at work included; one fed from the inputs, the cost of its unit begun, if any.
/// </summary>
public sealed record Cancel(int Machine, int Entry) : Command;

/// <summary>
/// Makes entry <c>Entry</c> of station <c>Machine</c>'s queue, one fed from the inputs, repeat while they last
/// (<c>On</c>), or count down again. Set, not toggled: a click sent twice before its tick sets it once.
/// </summary>
public sealed record SetEndless(int Machine, int Entry, bool On) : Command;

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
    /// Whether fed station <paramref name="machine"/> has what it can work: an item of its process in its input, at
    /// most as hot as it is, as many as one smelt takes, the most it may make fitting its outputs. What lights a furnace's fire.
    /// </summary>
    bool CanSmelt(int machine);
    /// <summary>
    /// Whether fed station <paramref name="machine"/>'s input holds what it works, hot enough, but fewer than one smelt
    /// takes (<c>ItemSpec.SmeltTakes</c>): it waits for more. What its panel says.
    /// </summary>
    bool TooFew(int machine);
    /// <summary>
    /// The work one of what fed station <paramref name="machine"/>'s input holds takes there, in thousandths of a tick
    /// at full speed, hot enough or not; null when it holds nothing, or nothing of the station's process. What its
    /// panel's bar and a plant's growth are measured against.
    /// </summary>
    int? WorkOf(int machine);
    /// <summary>
    /// Whether fed station <paramref name="machine"/>'s input holds what is of its process, but hotter than it is: taken
    /// in, never worked, kept (0057). What its panel and its mark in the world say.
    /// </summary>
    bool TooCold(int machine);
}

/// <summary>How a module that depends on the crafting may change it.</summary>
public interface ICraftingOps
{
    /// <summary>Fed station <paramref name="machine"/>'s fire burns this tick: told before crafting works, forgotten after.</summary>
    void Heat(int machine);
}

/// <summary>
/// Stations (0057): every crafting machine on one model, its work counted in thousandths of a tick at its
/// speed times the share of its energy it gets. A queued station makes a craft one unit at a time into its outputs
/// (0028), paid when queued or each unit fed from its inputs as it begins; a fed one smelts what its input holds into that item's yields.
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
        Handles<SetEndless>(EndlessProblem, ApplyEndless);
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

    public bool TooCold(int machine) =>
        _grid.MachineById(machine) is { } m && StationOf(m) is { Fed: true } station && InputOf(machine) is { } input
        && Worked(station, input) is { } spec && spec.Heat > station.Heat;

    public bool TooFew(int machine) =>
        _grid.MachineById(machine) is { } m && StationOf(m) is { Fed: true } station
        && _items.Crate(machine) is [{ } input, ..] && Processes(station, input.Item)
        && input.Count < Content.Items.Get(input.Item).SmeltTakes;

    public int? WorkOf(int machine) =>
        _grid.MachineById(machine) is { } m && StationOf(m) is { Fed: true } station && InputOf(machine) is { } input
        && Worked(station, input) is { } spec ? spec.SmeltTicks!.Value * 1000 : null;

    /// <summary>
    /// Whether station <paramref name="machine"/> works now with a share of its energy: a queued one on a unit not
    /// done, a fed one on what it can smelt. What its picture's motion and its panel's screen and bolt show. A
    /// furnace's fire is told to crafting only within a tick, so between ticks a furnace reads as cold here: the
    /// furnace module shows its fire.
    /// </summary>
    public bool Works(int machine)
    {
        if (_grid.MachineById(machine) is not { } m || StationOf(m) is not { } station) return false;
        if (station.Fed) return InputOf(machine) is { } input && CanSmelt(m, station, input) && Gain(m, station) > 0;
        return QueueOf(machine) is { } queue && HasWork(m.Id, queue) && Gain(m, station) > 0;
    }

    /// <summary>
    /// What queued station <paramref name="machine"/>'s inputs lack for its first entry's next unit, while that entry is
    /// fed from them and waits for it, the whole queue behind it; none otherwise. What dims the entry and its tip names.
    /// </summary>
    public IReadOnlyList<(string Item, int Count)> Lacks(int machine) =>
        QueueOf(machine) is { } queue ? LacksOf(machine, queue) : [];

    // Operations.

    public void Heat(int machine) => State.Heated.Add(machine);

    // Commands.

    private string? CraftProblem(Craft c)
    {
        if (StationOf(c.Machine) is not { } station) return "crafting.noStation";
        if (!Content.Crafts.TryGet(c.Recipe, out var craft) || !_research.Knows(c.Recipe)) return "crafting.unknown";
        if (!Makes(station, c.Recipe)) return "crafting.notHere";
        if (Queue(c.Machine).Count >= station.QueueMost) return "crafting.full";
        if (!c.FromInputs) return _items.ShortOf(c.From, Times(craft.Cost, c.Count));
        // The pod is paid by hand by design (0057): with no inputs, an entry fed from them would wait for ever; the arc bench's
        // one input is its tool's (0099).
        var spec = Content.Machines.Get(station.Machine);
        return (spec.Inputs ?? 0) == 0 || spec.Tunes ? "crafting.noInputs" : null;
    }

    private void ApplyCraft(Craft c)
    {
        var queue = QueueOf(c.Machine) ?? Added(c.Machine);
        if (!c.FromInputs) _itemsOps.Take(c.From, Times(Content.Crafts.Get(c.Recipe).Cost, c.Count));
        queue.Entries.Add(new Entry(queue.Next, c.Recipe, c.Count, c.From, c.FromInputs));
        queue.Next += 1;
    }

    private string? CancelProblem(Cancel c)
    {
        if (StationOf(c.Machine) is not { } station) return "crafting.noStation";
        if (station.Fed) return "crafting.notHere";
        var queue = QueueOf(c.Machine);
        int at = queue?.Entries.FindIndex(e => e.Id == c.Entry) ?? -1;
        return at >= 0 ? _items.FullFor(c.From, Refund(queue!, at)) : "crafting.noEntry";
    }

    private void ApplyCancel(Cancel c)
    {
        var queue = QueueOf(c.Machine)!;
        int at = queue.Entries.FindIndex(e => e.Id == c.Entry);
        var refund = Refund(queue, at);
        queue.Entries.RemoveAt(at);
        // The unit at work is given back with the rest: the next entry starts from nothing.
        if (at == 0) queue.Progress = 0;
        // To whoever cancels, not whoever paid: friends share a ship, and no record of who paid is kept.
        _itemsOps.Give(c.From, refund);
    }

    private string? EndlessProblem(SetEndless c)
    {
        if (StationOf(c.Machine) is not { } station) return "crafting.noStation";
        if (station.Fed) return "crafting.notHere";
        if (Queue(c.Machine).FirstOrDefault(e => e.Id == c.Entry) is not { } entry) return "crafting.noEntry";
        // A paid entry's units are paid for: repeating it would make what nobody paid for.
        return entry.FromInputs ? null : "crafting.paid";
    }

    private void ApplyEndless(SetEndless c)
    {
        var queue = QueueOf(c.Machine)!;
        int at = queue.Entries.FindIndex(e => e.Id == c.Entry);
        queue.Entries[at] = queue.Entries[at] with { Endless = c.On };
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
    /// A queued station's first entry works a tick, one fed from the inputs taking its unit's cost from them on its
    /// first tick with work; a unit done goes into its outputs, or waits, done, for room there. A unit fed from the
    /// inputs that lack its cost waits at nothing done, and the whole queue behind it (FIFO). One that draws power
    /// rests while it has nothing to work on.
    /// </summary>
    private void Make(Machine m, Station station)
    {
        var queue = QueueOf(m.Id);
        if (queue is { Entries.Count: > 0 })
        {
            var first = queue.Entries[0];
            var craft = Content.Crafts.Get(first.Recipe);
            int work = craft.Ticks * 1000;
            int gain = HasWork(m.Id, queue) ? Gain(m, station) : 0;
            if (gain > 0)
            {
                // Begun only with work done, so a station without power takes nothing in.
                if (first.FromInputs && queue.Progress == 0) _itemsOps.TakeInputs(m.Id, craft.Cost);
                queue.Progress = Math.Min(work, queue.Progress + gain);
            }
            Cost unit = [(craft.Item, 1)];
            if (queue.Progress >= work && _items.OutputFull(m.Id, unit) == null)
            {
                _itemsOps.Output(m.Id, unit);
                queue.Progress = 0;
                // An endless entry keeps its count: it repeats while its inputs last.
                if (!first.Endless && first.Count > 1) queue.Entries[0] = first with { Count = first.Count - 1 };
                else if (!first.Endless) queue.Entries.RemoveAt(0);
            }
        }
        // Idle: nothing queued, a unit done waiting for room, which needs no power to go out, or one waiting for its inputs.
        Rest(m, queue == null || !HasWork(m.Id, queue));
    }

    /// <summary>
    /// Whether <paramref name="queue"/>, station <paramref name="machine"/>'s, has work now: a first entry whose unit is
    /// not done, and, fed from the inputs, begun or with its cost there.
    /// </summary>
    private bool HasWork(int machine, Queue queue) =>
        queue.Entries.Count > 0
        && queue.Progress < Content.Crafts.Get(queue.Entries[0].Recipe).Ticks * 1000
        && LacksOf(machine, queue).Count == 0;

    /// <summary>What the inputs of <paramref name="machine"/> lack for <paramref name="queue"/>'s first unit, fed from them and not begun; none otherwise.</summary>
    private IReadOnlyList<(string Item, int Count)> LacksOf(int machine, Queue queue) =>
        queue.Entries.Count > 0 && queue.Entries[0].FromInputs && queue.Progress == 0
            ? _items.InputsLack(machine, Content.Crafts.Get(queue.Entries[0].Recipe).Cost)
            : [];

    /// <summary>
    /// A fed station a tick on from <paramref name="was"/>: work on another item, or none, begins again; with
    /// what it can work and its energy, it works a tick, and at the item's work turns one smelt of it into its yields
    /// (<c>IItemsOps.Smelt</c>) and what the world's Rng draws besides (<see cref="Drawn"/>). Null when nothing is part-way.
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
        var spec = Content.Items.Get(input!);
        if (progress < spec.SmeltTicks!.Value * 1000) return new Smelt(m.Id, input!, progress);
        _itemsOps.Smelt(m.Id, Drawn(spec));
        // Rested at once when that was its last: it draws nothing from the next tick, as it did by its empty input.
        Rest(m, !(InputOf(m.Id) is { } next && CanSmelt(m, station, next)));
        return null;
    }

    /// <summary>
    /// What one smelt of <paramref name="spec"/> makes besides its yields as its work completes, drawn from the world's
    /// Rng, stations in machine id order: the seeds its <c>Returns</c> give back (<see cref="Returned"/>), then its
    /// <c>Byproducts</c> entry by entry, as loot is (<see cref="Loots.Roll"/>). No draw for an item without either.
    /// </summary>
    private IReadOnlyList<(string Item, int Count)> Drawn(ItemSpec spec)
    {
        int back = Returned(spec);
        var by = spec.Byproducts is { } entries ? Loots.Roll(entries, Random) : [];
        return back > 0 ? [(spec.Id, back), .. by] : by;
    }

    /// <summary>
    /// How many of <paramref name="spec"/> one gives back as its work completes: drawn from the world's Rng by its
    /// <c>Returns</c> weights; none, and no draw, for an item without them.
    /// </summary>
    private int Returned(ItemSpec spec)
    {
        if (spec.Returns is not { } weights) return 0;
        int roll = (int)Random.Below((ulong)weights.Sum());
        for (int n = 0; n < weights.Count; n++)
        {
            if (roll < weights[n]) return n;
            roll -= weights[n];
        }
        throw new InvalidOperationException($"crafting: a roll past {spec.Id}'s returns");
    }

    // Helpers.

    /// <summary>
    /// The station <paramref name="m"/> is, as it works now: its entry in <c>Content.Stations</c> with the modules fitted
    /// into it folded in (0086): its speed × (1000 + their speed) / 1000, rounded down, a gain; a fed one's heat plus
    /// theirs. The one place a station's speed, queue size and heat are read.
    /// </summary>
    private Station? StationOf(Machine m)
    {
        if (!Stations.TryGet(m.Kind, out var station)) return null;
        var boost = _items.Boost(m.Id);
        if (boost == default) return station;
        // Heat only where it means something: a queued station's 0 says it is not fed.
        return station with { Speed = Boost.Gain(station.Speed, boost.Speed), Heat = station.Fed ? station.Heat + boost.Heat : 0 };
    }

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

    /// <summary>Whether fed <paramref name="station"/> <paramref name="m"/> can work <paramref name="input"/>: it processes such things, its yields fitting.</summary>
    private bool CanSmelt(Machine m, Station station, string input) =>
        Processes(station, input) && _items.CanSmelt(m.Id);

    /// <summary>
    /// Whether fed <paramref name="station"/> processes <paramref name="item"/> at all, room aside: it has yields, of
    /// the station's process, and the station is hot enough for it. Static, from content: what the client marks in
    /// the inventory while such a station is open.
    /// </summary>
    public static bool Processes(Station station, string item) => Worked(station, item) is { } spec && spec.Heat <= station.Heat;

    /// <summary><paramref name="input"/>'s spec when fed <paramref name="station"/> works such things: it has yields, of the station's process (0060).</summary>
    private static ItemSpec? Worked(Station station, string input) =>
        Content.Items.Get(input) is { Yields: not null } spec && spec.Process == station.Process ? spec : null;

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

    /// <summary>
    /// What entry <paramref name="at"/> of <paramref name="queue"/> paid and did not make, as content prices it now: a paid
    /// one's units left, the one at work included; one fed from the inputs, its unit begun, if it is first and begun.
    /// </summary>
    private static Cost Refund(Queue queue, int at)
    {
        var entry = queue.Entries[at];
        var cost = Content.Crafts.Get(entry.Recipe).Cost;
        if (!entry.FromInputs) return Times(cost, entry.Count);
        return at == 0 && queue.Progress > 0 ? cost : [];
    }

    private static Cost Times(Cost cost, int count) => cost.Select(c => (c.Item, c.Count * count)).ToList();
}
