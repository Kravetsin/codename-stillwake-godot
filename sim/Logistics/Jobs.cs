using Stillwake.Content;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Tune = Stillwake.Content.Logistics;

namespace Stillwake.Sim.Logistics;

public sealed partial class LogisticsModule
{
    /// <summary>A job found: <c>Count</c> of <c>Item</c> from <c>Source</c> to <c>Target</c>.</summary>
    private readonly record struct Job(int Source, int Target, string Item, int Count);

    /// <summary>
    /// What drones may take of one item at one machine, as one look found it: in the flood, its machine served, summed
    /// over the slots offered (<see cref="FirstOffered"/>) less what drones on their way there will take. <c>First</c> is
    /// the first slot's stack, <c>Slot</c> its slot: what orders offers of one machine.
    /// </summary>
    private readonly record struct Offer(int Machine, int Slot, Stack First, int Count, int Distance, bool Output);

    // Scratch for one look, never saved: what may be taken, and what one machine wants.
    private readonly List<Offer> _offers = [];
    private readonly List<(string Item, int Want)> _wants = [];

    /// <summary>
    /// The job a drone at <paramref name="from"/> takes, within the last flood: of every refill a served machine wants
    /// and every output a crate has room for, the one whose source is nearest by Manhattan distance, then the lowest
    /// source id and the first slot of the item there, a refill before an output carried off, then the lowest target id.
    /// Null for no work.
    /// </summary>
    private Job? FindJob(int from)
    {
        _offers.Clear();
        foreach (var m in _grid.Machines)
        {
            if (FirstOffered(m) is not int start || !Served(m.Id) || !InFlood(m)) continue;
            var slots = _items.Crate(m.Id);
            int distance = Distance(from, m.Cell), mine = _offers.Count;
            for (int slot = start; slot < slots.Count; slot++)
            {
                if (slots[slot] is not { } stack) continue;
                int at = mine;
                while (at < _offers.Count && _offers[at].First.Item != stack.Item) at++;
                if (at < _offers.Count) _offers[at] = _offers[at] with { Count = _offers[at].Count + stack.Count };
                else _offers.Add(new Offer(m.Id, slot, stack, stack.Count, distance, !Machines.Is(m.Kind, MachineTrait.Container)));
            }
            for (int i = _offers.Count - 1; i >= mine; i--)
            {
                int left = _offers[i].Count - Spoken(m, _offers[i].First.Item);
                if (left > 0) _offers[i] = _offers[i] with { Count = left };
                else _offers.RemoveAt(i);
            }
        }
        if (_offers.Count == 0) return null;
        Job? best = null;
        (int Distance, int Source, int Slot, int Push, int Target) key = default;
        void Consider(Offer o, int push, int target, int count)
        {
            var mine = (o.Distance, o.Machine, o.Slot, push, target);
            if (best != null && mine.CompareTo(key) >= 0) return;
            best = new Job(o.Machine, target, o.First.Item, count);
            key = mine;
        }
        foreach (var t in _grid.Machines)
        {
            if (!Takes(t) || !Served(t.Id) || !InFlood(t)) continue;
            Wants(t);
            foreach (var (item, want) in _wants)
            {
                int room = _items.RoomFor(t.Id, new Stack(item, Content.Items.Get(item).Stack));
                if (room == 0) continue;
                foreach (var o in _offers)
                    if (o.First.Item == item && o.Machine != t.Id)
                        Consider(o, 0, t.Id, Math.Min(Math.Min(want, o.Count), room));
            }
        }
        foreach (var o in _offers)
            if (o.Output && Storage(o.Machine, o.First) is { } to) Consider(o, 1, to.Crate, to.Room);
        return best;
    }

    /// <summary>The first slot drones take from at machine <paramref name="m"/>: a crate's first, a machine's first output; null for one with none.</summary>
    private static int? FirstOffered(Machine m) => Machines.Is(m.Kind, MachineTrait.Container) ? 0 : OutputsOf(m);

    /// <summary>
    /// What drones on their way to machine <paramref name="m"/> will take of <paramref name="item"/>: at a crate, the sum
    /// of their counts; at a machine's outputs, all of it while one is on its way, as a carry-off takes all there is.
    /// </summary>
    private int Spoken(Machine m, string item)
    {
        int sum = 0;
        foreach (var d in State.Drones)
            if (d.Leg == Leg.Source && d.Source == m.Id && d.Item == item)
            {
                if (!Machines.Is(m.Kind, MachineTrait.Container)) return int.MaxValue;
                sum += d.Count;
            }
        return sum;
    }

    /// <summary>
    /// What machine <paramref name="t"/> wants brought now, into <see cref="_wants"/>, each item once. An input or fuel
    /// slot holding an item, or remembering one it burnt down to nothing (<c>KeptAt</c>), wants it kept at a stack: short
    /// when what it holds and what drones bring is under half, or under one smelt's (<c>SmeltTakes</c>). A
    /// queued station's head entry fed from its inputs wants its units' cost, at most a stack of each: short under half
    /// that, or under one unit's. An empty input remembering nothing, or a head entry paid, wants nothing.
    /// </summary>
    private void Wants(Machine t)
    {
        _wants.Clear();
        if (Stations.TryGet(t.Kind, out var station) && !station.Fed)
        {
            var queue = _crafting.Queue(t.Id);
            if (queue.Count == 0 || queue[0] is not { FromInputs: true } head) return;
            // The unit begun took its cost as it began.
            long units = head.Endless ? long.MaxValue / 1024 : head.Count - (_crafting.Progress(t.Id) > 0 ? 1 : 0);
            if (units <= 0) return;
            foreach (var (item, cost) in Crafts.Get(head.Recipe).Cost)
                Short(t, item, (int)Math.Min(Content.Items.Get(item).Stack, cost * units), cost);
            return;
        }
        var slots = _items.Crate(t.Id);
        int inputs = Machines.Get(t.Kind).Inputs ?? slots.Count;
        for (int i = 0; i < inputs; i++)
            if ((slots[i]?.Item ?? _items.KeptAt(t.Id, i)) is { } item)
                Short(t, item, Content.Items.Get(item).Stack, Content.Items.Get(item).SmeltTakes);
    }

    /// <summary>Adds <paramref name="item"/> to what <paramref name="t"/> wants, once, when what it holds and what drones bring is under half <paramref name="keep"/> or one <paramref name="unit"/>.</summary>
    private void Short(Machine t, string item, int keep, int unit)
    {
        foreach (var w in _wants)
            if (w.Item == item) return;
        int have = _items.HeldFor(t.Id, item) + InFlight(t.Id, item);
        if (have * 2 < keep || have < unit) _wants.Add((item, keep - have));
    }

    /// <summary>
    /// Whether drones bring anything to a machine of <paramref name="m"/>'s kind: a fed station's input, a fire's fuel,
    /// a queued station's inputs. Not a crate, a bay, the analyzer or the charging station.
    /// </summary>
    private static bool Takes(Machine m)
    {
        // An arc bench takes a tool by hand and is paid by hand (0099): drones never touch it.
        if (!Machines.TryGet(m.Kind, out var spec) || spec.Charges || spec.Tunes) return false;
        if (Stations.TryGet(m.Kind, out var station)) return station.Fed || spec.Inputs > 0;
        return spec.FuelSlot != null;
    }

    /// <summary>The first output slot of a machine of <paramref name="m"/>'s kind, its slots past its inputs; null for one with none.</summary>
    private static int? OutputsOf(Machine m) =>
        Machines.TryGet(m.Kind, out var spec) && !spec.Tunes && spec.Inputs is int inputs && inputs < (spec.Slots ?? 0) ? inputs : null;

    /// <summary>
    /// The crate in the flood to take <paramref name="stack"/> from machine <paramref name="from"/>, and its room for a
    /// stack of it: one already holding the item first, then the nearest, then the lowest id; served, with room. Null for none.
    /// </summary>
    private (int Crate, int Room)? Storage(int from, Stack stack)
    {
        int at = _grid.MachineById(from)!.Cell;
        var full = stack with { Count = Content.Items.Get(stack.Item).Stack };
        (int Crate, int Room, bool Holds, int Distance)? best = null;
        foreach (var crate in _grid.MachinesWith(MachineTrait.Container))
        {
            if (!Served(crate.Id) || !InFlood(crate)) continue;
            int room = _items.RoomFor(crate.Id, full);
            if (room == 0) continue;
            bool holds = _items.HeldFor(crate.Id, stack.Item) > 0;
            int distance = Distance(at, crate.Cell);
            if (best is not { } b || (holds && !b.Holds) || (holds == b.Holds && distance < b.Distance))
                best = (crate.Id, room, holds, distance);
        }
        return best is { } found ? (found.Crate, found.Room) : null;
    }

    /// <summary>The Manhattan distance between cells <paramref name="a"/> and <paramref name="b"/>.</summary>
    private int Distance(int a, int b)
    {
        int w = _grid.Width;
        return Math.Abs(a % w - b % w) + Math.Abs(a / w - b / w);
    }

    /// <summary>How much of <paramref name="item"/> drones are bringing to <paramref name="target"/>: their jobs on the way to the source, their cargo after.</summary>
    private int InFlight(int target, string item)
    {
        int sum = 0;
        foreach (var d in State.Drones)
        {
            if (d.Target != target || d.Item != item) continue;
            if (d.Leg == Leg.Source) sum += d.Count;
            else if (d.Leg == Leg.Target) sum += d.Cargo?.Count ?? 0;
        }
        return sum;
    }

    // Charge.

    /// <summary>Ticks a drone takes over <paramref name="cells"/> cells, at its bay's speed, rounded up.</summary>
    private static int TicksOver(Machine bay, int cells) => (cells * Unit + BayOf(bay).Speed - 1) / BayOf(bay).Speed;

    /// <summary>The cells a drone may be short of home by and still turn back: the one it is crossing and three to spare, the 16 px cell's two (0104).</summary>
    private const int Spare = 4;

    /// <summary>What a drone of <paramref name="bay"/> spends flying <paramref name="cells"/> cells, stored.</summary>
    private static int Cost(Machine bay, int cells) => TicksOver(bay, cells) * Tune.Draw * Content.Charge.Scale;

    /// <summary>
    /// Whether <paramref name="charge"/> covers <paramref name="job"/> from <paramref name="from"/> and the way home after:
    /// the Manhattan lengths from there to the source's top-left, on to the target's and home to the bay's, each leg from
    /// a machine plus the most a cell of it lies from its top-left (a drone arrives at any of its cells), and
    /// <see cref="Spare"/>. Paths round walls may be longer: in flight a drone turns home with what it has left (<c>Fly</c>).
    /// </summary>
    private bool Affords(int charge, int from, Job job, Machine bay)
    {
        Machine source = _grid.MachineById(job.Source)!, target = _grid.MachineById(job.Target)!;
        int cells = Distance(from, source.Cell) + Distance(source.Cell, target.Cell) + Across(source)
            + Distance(target.Cell, bay.Cell) + Across(target) + Spare;
        return charge >= Cost(bay, cells);
    }

    /// <summary>How far the farthest cell of machine <paramref name="m"/> lies from its top-left: its width and height less one each.</summary>
    private static int Across(Machine m) => Machines.Get(m.Kind) is var spec ? spec.Width + spec.Height - 2 : 0;
}
