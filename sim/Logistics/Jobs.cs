using Stillwake.Content;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Tune = Stillwake.Content.Logistics;

namespace Stillwake.Sim.Logistics;

public sealed partial class LogisticsModule
{
    /// <summary>A job found: <c>Count</c> of <c>Item</c> from slot <c>Slot</c> of <c>Source</c> to <c>Target</c>.</summary>
    private readonly record struct Job(int Source, int Slot, int Target, string Item, int Count);

    /// <summary>A stack drones may take, as one look found it: in the flood, its machine served, no drone on its way to it.</summary>
    private readonly record struct Offer(int Machine, int Slot, Stack Stack, int Distance, bool Output);

    // Scratch for one look, never saved: what may be taken, and what one machine wants.
    private readonly List<Offer> _offers = [];
    private readonly List<(string Item, int Want)> _wants = [];

    /// <summary>
    /// The job a drone at <paramref name="from"/> takes, within the last flood: of every refill a served machine wants
    /// and every output a crate has room for, the one whose source is nearest by Manhattan distance, then the lowest
    /// source id and slot, a refill before an output carried off, then the lowest target id. Null for no work.
    /// </summary>
    private Job? FindJob(int from)
    {
        _offers.Clear();
        foreach (var m in _grid.Machines)
        {
            int? first = m.Kind == "crate" ? 0 : OutputsOf(m);
            if (first is not int start || !Served(m.Id) || !InFlood(m)) continue;
            var slots = _items.Crate(m.Id);
            int distance = Distance(from, m.Cell);
            for (int slot = start; slot < slots.Count; slot++)
                if (slots[slot] is { } stack && !Reserved(m.Id, slot)) _offers.Add(new Offer(m.Id, slot, stack, distance, m.Kind != "crate"));
        }
        if (_offers.Count == 0) return null;
        Job? best = null;
        (int Distance, int Source, int Slot, int Push, int Target) key = default;
        void Consider(Offer o, int push, int target, int count)
        {
            var mine = (o.Distance, o.Machine, o.Slot, push, target);
            if (best != null && mine.CompareTo(key) >= 0) return;
            best = new Job(o.Machine, o.Slot, target, o.Stack.Item, count);
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
                    if (o.Stack.Item == item && o.Machine != t.Id)
                        Consider(o, 0, t.Id, Math.Min(Math.Min(want, o.Stack.Count), room));
            }
        }
        foreach (var o in _offers)
            if (o.Output && Storage(o.Machine, o.Stack) is { } to) Consider(o, 1, to.Crate, to.Room);
        return best;
    }

    /// <summary>
    /// What machine <paramref name="t"/> wants brought now, into <see cref="_wants"/>, each item once. An input or fuel
    /// slot holding an item wants it kept at a stack: short when what it holds and what drones bring is under half. A
    /// queued station's head entry fed from its inputs wants its units' cost, at most a stack of each: short under half
    /// that, or under one unit's. An empty input, or a head entry paid, wants nothing.
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
            if (slots[i] is { } held) Short(t, held.Item, Content.Items.Get(held.Item).Stack, 1);
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
        if (!Machines.TryGet(m.Kind, out var spec) || spec.Charges) return false;
        if (Stations.TryGet(m.Kind, out var station)) return station.Fed || spec.Inputs > 0;
        return spec.FuelSlot != null;
    }

    /// <summary>The first output slot of a machine of <paramref name="m"/>'s kind, its slots past its inputs; null for one with none.</summary>
    private static int? OutputsOf(Machine m) =>
        Machines.TryGet(m.Kind, out var spec) && spec.Inputs is int inputs && inputs < (spec.Slots ?? 0) ? inputs : null;

    /// <summary>
    /// The crate in the flood to take <paramref name="stack"/> from machine <paramref name="from"/>, and its room for a
    /// stack of it: one already holding the item first, then the nearest, then the lowest id; served, with room. Null for none.
    /// </summary>
    private (int Crate, int Room)? Storage(int from, Stack stack)
    {
        int at = _grid.MachineById(from)!.Cell;
        var full = stack with { Count = Content.Items.Get(stack.Item).Stack };
        (int Crate, int Room, bool Holds, int Distance)? best = null;
        foreach (var crate in _grid.MachinesOf("crate"))
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

    /// <summary>Whether a drone is on its way to take what slot <paramref name="slot"/> of <paramref name="machine"/> holds.</summary>
    private bool Reserved(int machine, int slot)
    {
        foreach (var d in State.Drones)
            if (d.Leg == Leg.Source && d.Source == machine && d.Slot == slot) return true;
        return false;
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

    /// <summary>Ticks a drone takes over a cell, at its bay's speed, rounded up.</summary>
    private static int TicksPerCell(Machine bay) => (Unit + BayOf(bay).Speed - 1) / BayOf(bay).Speed;

    /// <summary>The cells a drone may be short of home by and still turn back: the one it is crossing and one to spare.</summary>
    private const int Spare = 2;

    /// <summary>What a drone of <paramref name="bay"/> spends flying <paramref name="cells"/> cells, stored.</summary>
    private static int Cost(Machine bay, int cells) => cells * TicksPerCell(bay) * Tune.Draw * Content.Charge.Scale;

    /// <summary>
    /// Whether <paramref name="charge"/> covers <paramref name="job"/> from <paramref name="from"/> and the way home after:
    /// the Manhattan lengths from there to the source, on to the target and home to the bay, and <see cref="Spare"/>.
    /// Paths may be longer: in flight a drone turns home with what it has left (<c>Fly</c>).
    /// </summary>
    private bool Affords(int charge, int from, Job job, Machine bay)
    {
        int source = _grid.MachineById(job.Source)!.Cell, target = _grid.MachineById(job.Target)!.Cell;
        return charge >= Cost(bay, Distance(from, source) + Distance(source, target) + Distance(target, bay.Cell) + Spare);
    }
}
