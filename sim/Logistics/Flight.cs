using Stillwake.Content;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Tune = Stillwake.Content.Logistics;

namespace Stillwake.Sim.Logistics;

public sealed partial class LogisticsModule
{
    /// <summary>A bay's numbers as it works now: its zone's reach, its drones' speed, how often it looks.</summary>
    private sealed record BaySpec(int Reach, int Speed, int LookEvery);

    /// <summary>The one place a bay's numbers are read, so machine modules fold in here.</summary>
    private static BaySpec BayOf(Machine bay) => new(Tune.Reach, Tune.Speed, Tune.LookEvery);

    /// <summary>A job a bay found: <c>Count</c> of <c>Item</c> from slot <c>Slot</c> of <c>Source</c> to <c>Target</c>.</summary>
    private readonly record struct Job(int Source, int Slot, int Target, string Item, int Count);

    private const int Unit = 1000;

    // Scratch for the floods and paths, inside one zone box at a time: derived, never saved.
    private int[] _seen = [], _parent = [], _queue = [];
    private int _stamp;
    private Box _box = Box.None;

    // Looking.

    /// <summary>Each bay whose turn it is, in id order, launches its first docked drone on the first job it finds.</summary>
    private void Look()
    {
        foreach (var bay in _grid.MachinesOf(Tune.Bay))
        {
            if ((State.Ticks + bay.Id) % BayOf(bay).LookEvery != 0) continue;
            int slot = DockedSlot(bay.Id);
            if (slot < 0 || !Flood(bay)) continue;
            if (FindJob() is { } job) Launch(bay, slot, job);
        }
    }

    /// <summary>The first slot of bay <paramref name="bay"/> holding a drone, or -1.</summary>
    private int DockedSlot(int bay)
    {
        var slots = _items.Crate(bay);
        for (int i = 0; i < slots.Count; i++)
            if (slots[i]?.Item == Tune.Drone) return i;
        return -1;
    }

    /// <summary>
    /// A job within the flood: the first request short, of the requesters in id order and each one's requests in
    /// order, that a source can serve; else what a fed station made, carried to storage.
    /// </summary>
    private Job? FindJob()
    {
        foreach (var r in State.Requests)
        {
            if (_grid.MachineById(r.Machine) is not { } target || Most(target) == 0 || !InFlood(target)) continue;
            foreach (var request in r.Requests)
            {
                int missing = request.Count - _items.HeldFor(target.Id, request.Item) - InFlight(target.Id, request.Item);
                if (missing <= 0 || Source(target, request.Item, reserved: false) is not { } from) continue;
                var stack = _items.Crate(from.Machine)[from.Slot]!;
                int limit = Content.Items.Get(stack.Item).Stack;
                int carry = Math.Min(Math.Min(missing, stack.Count), _items.RoomFor(target.Id, stack with { Count = limit }));
                if (carry > 0) return new Job(from.Machine, from.Slot, target.Id, request.Item, carry);
            }
        }
        return Push();
    }

    /// <summary>
    /// The first unreserved output of a fed station in the flood, in id and slot order, to a storage crate with room
    /// for it: one holding the item first, then the nearest, then the lowest id. Queued stations' outputs only provide.
    /// </summary>
    private Job? Push()
    {
        foreach (var m in _grid.Machines)
        {
            if (!Stations.TryGet(m.Kind, out var station) || !station.Fed || !InFlood(m)) continue;
            var spec = Machines.Get(m.Kind);
            var slots = _items.Crate(m.Id);
            for (int slot = spec.Inputs ?? 0; slot < slots.Count; slot++)
            {
                if (slots[slot] is not { } stack || Reserved(m.Id, slot)) continue;
                if (Storage(m, stack) is not { } to) continue;
                return new Job(m.Id, slot, to.Crate, stack.Item, Math.Min(stack.Count, to.Room));
            }
        }
        return null;
    }

    /// <summary>The storage crate in the flood to take <paramref name="stack"/> from <paramref name="from"/>, and its room for it; null for none with room.</summary>
    private (int Crate, int Room)? Storage(Machine from, Stack stack)
    {
        (int Crate, int Room, bool Holds, int Distance)? best = null;
        foreach (var crate in _grid.MachinesOf("crate"))
        {
            if (RoleOf(crate.Id) != Role.Storage || !InFlood(crate)) continue;
            int room = _items.RoomFor(crate.Id, stack);
            if (room == 0) continue;
            bool holds = _items.HeldFor(crate.Id, stack.Item) > 0;
            int distance = Distance(from, crate);
            if (best is not { } b || (holds && !b.Holds) || (holds == b.Holds && distance < b.Distance))
                best = (crate.Id, room, holds, distance);
        }
        return best is { } found ? (found.Crate, found.Room) : null;
    }

    /// <summary>
    /// Where <paramref name="target"/> gets <paramref name="item"/> from, within the flood: providers first (machines'
    /// outputs past their inputs, provider crates), then storage crates; the nearest by Manhattan distance between the
    /// machines' top-left cells, then the lowest id, then the lowest slot; only stacks no drone is on its way to take,
    /// unless <paramref name="reserved"/>. Null for none.
    /// </summary>
    private (int Machine, int Slot)? Source(Machine target, string item, bool reserved)
    {
        (int Machine, int Slot, int Tier, int Distance)? best = null;
        foreach (var m in _grid.Machines)
        {
            if (m.Id == target.Id || Offers(m) is not { } offer) continue;
            var (tier, first) = offer;
            int distance = Distance(m, target);
            if (best is { } b && (tier > b.Tier || (tier == b.Tier && distance >= b.Distance))) continue;
            if (!InFlood(m)) continue;
            var slots = _items.Crate(m.Id);
            for (int slot = first; slot < slots.Count; slot++)
            {
                if (slots[slot]?.Item != item || (!reserved && Reserved(m.Id, slot))) continue;
                best = (m.Id, slot, tier, distance);
                break;
            }
        }
        return best is { } found ? (found.Machine, found.Slot) : null;
    }

    /// <summary>What machine <paramref name="m"/> gives drones: its tier (0 a provider, 1 storage) and its first slot taken from; null for nothing.</summary>
    private (int Tier, int First)? Offers(Machine m)
    {
        if (m.Kind == "crate")
            return RoleOf(m.Id) switch { Role.Provider => (0, 0), Role.Storage => (1, 0), _ => null };
        return Machines.TryGet(m.Kind, out var spec) && spec.Inputs is int inputs && inputs < (spec.Slots ?? 0) ? (0, inputs) : null;
    }

    private int Distance(Machine a, Machine b)
    {
        int w = _grid.Width;
        return Math.Abs(a.Cell % w - b.Cell % w) + Math.Abs(a.Cell / w - b.Cell / w);
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

    /// <summary>Launches the drone in slot <paramref name="slot"/> of <paramref name="bay"/> on <paramref name="job"/>, from the bay's first passable cell.</summary>
    private void Launch(Machine bay, int slot, Job job)
    {
        int cell = StartOf(bay);
        var stack = _itemsOps.TakeFrom(bay.Id, slot, 1) ?? throw new InvalidOperationException("logistics.launch: no drone docked");
        var (x, y) = Centre(cell);
        State.Drones.Add(new Drone
        {
            Id = State.NextId++,
            Bay = bay.Id,
            Stack = stack,
            X = x,
            Y = y,
            Cell = cell,
            To = cell,
            Leg = Leg.Source,
            Source = job.Source,
            Slot = job.Slot,
            Target = job.Target,
            Item = job.Item,
            Count = job.Count,
        });
    }

    // Flying.

    /// <summary>Every drone in flight, in id order, flies its bay's speed for the tick.</summary>
    private void Fly()
    {
        // A drone that lands or drops leaves the list itself, and only itself.
        for (int i = 0; i < State.Drones.Count;)
        {
            var d = State.Drones[i];
            Fly(d);
            if (i < State.Drones.Count && State.Drones[i] == d) i++;
        }
    }

    /// <summary>
    /// A tick of one drone. Ahead blocked, it turns back to the centre it left. At a centre it acts: in an airless cell,
    /// its bay gone, or with no path to its goal it drops where it is; in its goal it arrives; else it takes the first
    /// step of a path found from here. Between centres it moves on, cell centre to cell centre.
    /// </summary>
    private void Fly(Drone d)
    {
        if (_grid.MachineById(d.Bay) is not { } bay || bay.Kind != Tune.Bay)
        {
            Drop(d);
            return;
        }
        var box = ZoneBox(bay);
        int budget = BayOf(bay).Speed;
        while (true)
        {
            if (d.To != d.Cell && !Passable(d.To)) d.To = d.Cell;
            var (tx, ty) = Centre(d.To);
            if (d.X == tx && d.Y == ty)
            {
                d.Cell = d.To;
                if (!Passable(d.Cell))
                {
                    Drop(d);
                    return;
                }
                var goal = GoalOf(d, bay);
                if (goal == null) continue;
                if (Covers(goal, d.Cell))
                {
                    if (!Arrive(d, goal, bay)) return;
                    continue;
                }
                int step = FirstStep(d.Cell, goal, box);
                if (step < 0)
                {
                    Drop(d);
                    return;
                }
                d.To = step;
                (tx, ty) = Centre(step);
            }
            if (budget == 0) return;
            int move = Math.Min(budget, Math.Abs(tx - d.X) + Math.Abs(ty - d.Y));
            d.X += Math.Sign(tx - d.X) * move;
            d.Y += Math.Sign(ty - d.Y) * move;
            budget -= move;
        }
    }

    /// <summary>
    /// The machine drone <paramref name="d"/> flies to on its leg. One whose source is gone goes home; one whose target
    /// is gone lays its cargo where it is and goes home: both null, for the caller to look again.
    /// </summary>
    private Machine? GoalOf(Drone d, Machine bay)
    {
        if (d.Leg == Leg.Home) return bay;
        var goal = _grid.MachineById(d.Leg == Leg.Source ? d.Source : d.Target);
        if (goal != null) return goal;
        if (d.Cargo is { } cargo) _itemsOps.LayDown(cargo, d.Cell);
        d.Cargo = null;
        d.Leg = Leg.Home;
        return null;
    }

    /// <summary>
    /// Drone <paramref name="d"/> in its goal. At its source it takes what is there up to its job, nothing if the slot
    /// holds another item now, and with nothing goes home; at its target it puts in what fits and lays the rest down
    /// there; home, it docks in a free slot of its bay, or lies down where it is with none. Whether it still flies.
    /// </summary>
    private bool Arrive(Drone d, Machine goal, Machine bay)
    {
        switch (d.Leg)
        {
            case Leg.Source:
                var taken = _items.Crate(goal.Id) is var slots && d.Slot < slots.Count && slots[d.Slot]?.Item == d.Item
                    ? _itemsOps.TakeFrom(goal.Id, d.Slot, d.Count)
                    : null;
                d.Cargo = taken;
                d.Leg = taken == null ? Leg.Home : Leg.Target;
                return true;
            case Leg.Target:
                int left = _itemsOps.PutIn(goal.Id, d.Cargo!);
                if (left > 0) _itemsOps.LayDown(d.Cargo! with { Count = left }, d.Cell);
                d.Cargo = null;
                d.Leg = Leg.Home;
                return true;
            default:
                if (_itemsOps.PutIn(bay.Id, d.Stack) > 0) _itemsOps.LayDown(d.Stack, d.Cell);
                State.Drones.Remove(d);
                return false;
        }
    }

    /// <summary>Drone <paramref name="d"/> falls where it is, with its cargo, as stacks on the floor for anyone to pick up.</summary>
    private void Drop(Drone d)
    {
        int cell = Down(d.Y, Unit) * _grid.Width + Down(d.X, Unit);
        _itemsOps.LayDown(d.Stack, cell);
        if (d.Cargo is { } cargo) _itemsOps.LayDown(cargo, cell);
        State.Drones.Remove(d);
    }

    private static int Down(int a, int b) => a >= 0 ? a / b : -((-a + b - 1) / b);

    // Cells.

    /// <summary>Where a drone may be: a cell holding air, or a door's, open or closed; drones never open doors.</summary>
    private bool Passable(int cell) =>
        _air.AiredAt(cell) || _grid.StructureAt(cell) is Structure.DoorClosed or Structure.DoorOpen;

    private (int X, int Y) Centre(int cell) => (cell % _grid.Width * Unit + Unit / 2, cell / _grid.Width * Unit + Unit / 2);

    /// <summary>Whether machine <paramref name="m"/> covers <paramref name="cell"/>.</summary>
    private bool Covers(Machine m, int cell)
    {
        var spec = Machines.Get(m.Kind);
        int w = _grid.Width, dx = cell % w - m.Cell % w, dy = cell / w - m.Cell / w;
        return dx >= 0 && dx < spec.Width && dy >= 0 && dy < spec.Height;
    }

    /// <summary>The cells within <c>Reach</c> of bay <paramref name="bay"/>'s footprint, on the grid: its zone, where its drones fly.</summary>
    private Box ZoneBox(Machine bay)
    {
        var spec = Machines.Get(bay.Kind);
        int w = _grid.Width, x = bay.Cell % w, y = bay.Cell / w, reach = BayOf(bay).Reach;
        return new Box(
            Math.Max(0, x - reach), Math.Max(0, y - reach),
            Math.Min(_grid.Width - 1, x + spec.Width - 1 + reach), Math.Min(_grid.Height - 1, y + spec.Height - 1 + reach));
    }

    /// <summary>The first cell of bay <paramref name="bay"/>, in reading order, a drone may be in; -1 for none.</summary>
    private int StartOf(Machine bay)
    {
        var spec = Machines.Get(bay.Kind);
        for (int dy = 0; dy < spec.Height; dy++)
            for (int dx = 0; dx < spec.Width; dx++)
            {
                int cell = bay.Cell + dy * _grid.Width + dx;
                if (Passable(cell)) return cell;
            }
        return -1;
    }

    /// <summary>Starts a search inside <paramref name="box"/>: the scratch sized for it and a new stamp.</summary>
    private void Begin(Box box)
    {
        int size = box.Width * box.Height;
        if (_seen.Length < size)
        {
            _seen = new int[size];
            _parent = new int[size];
            _queue = new int[size];
            _stamp = 0;
        }
        _box = box;
        _stamp += 1;
    }

    private int Local(int cell) => (cell / _grid.Width - _box.Top) * _box.Width + cell % _grid.Width - _box.Left;

    private bool InBox(int cell) => _box.Contains(cell % _grid.Width, cell / _grid.Width);

    /// <summary>
    /// The cells drones of <paramref name="bay"/> reach: breadth first from its passable cells through passable cells
    /// inside its zone. Whether there are any: none for a bay with no air about it. Read with <see cref="InFlood"/>.
    /// </summary>
    private bool Flood(Machine bay)
    {
        Begin(ZoneBox(bay));
        int head = 0, tail = 0;
        Span<int> around = stackalloc int[4];
        var spec = Machines.Get(bay.Kind);
        for (int dy = 0; dy < spec.Height; dy++)
            for (int dx = 0; dx < spec.Width; dx++)
            {
                int cell = bay.Cell + dy * _grid.Width + dx;
                if (!Passable(cell)) continue;
                _seen[Local(cell)] = _stamp;
                _queue[tail++] = cell;
            }
        if (tail == 0)
        {
            _stamp += 1;
            return false;
        }
        while (head < tail)
        {
            int c = _queue[head++];
            Neighbours(c, around);
            foreach (int n in around)
            {
                if (n < 0 || !InBox(n) || _seen[Local(n)] == _stamp || !Passable(n)) continue;
                _seen[Local(n)] = _stamp;
                _queue[tail++] = n;
            }
        }
        return true;
    }

    /// <summary>Whether any cell of machine <paramref name="m"/> is in the last <see cref="Flood"/>.</summary>
    private bool InFlood(Machine m)
    {
        var spec = Machines.Get(m.Kind);
        for (int dy = 0; dy < spec.Height; dy++)
            for (int dx = 0; dx < spec.Width; dx++)
            {
                int cell = m.Cell + dy * _grid.Width + dx;
                if (InBox(cell) && _seen[Local(cell)] == _stamp) return true;
            }
        return false;
    }

    /// <summary>
    /// The first step from <paramref name="from"/> on a shortest path through passable cells inside <paramref name="box"/>
    /// to any cell of <paramref name="goal"/>: breadth first, neighbours up, left, right, down, so ties go to the lowest
    /// cell. -1 for no path.
    /// </summary>
    private int FirstStep(int from, Machine goal, Box box)
    {
        Begin(box);
        if (!InBox(from)) return -1;
        int head = 0, tail = 0;
        Span<int> around = stackalloc int[4];
        _seen[Local(from)] = _stamp;
        _parent[Local(from)] = -1;
        _queue[tail++] = from;
        while (head < tail)
        {
            int c = _queue[head++];
            Neighbours(c, around);
            foreach (int n in around)
            {
                if (n < 0 || !InBox(n) || _seen[Local(n)] == _stamp || !Passable(n)) continue;
                _seen[Local(n)] = _stamp;
                _parent[Local(n)] = c;
                if (Covers(goal, n))
                {
                    int step = n;
                    while (_parent[Local(step)] != from) step = _parent[Local(step)];
                    return step;
                }
                _queue[tail++] = n;
            }
        }
        return -1;
    }

    /// <summary>The four neighbours of <paramref name="cell"/> in the order paths try them, up, left, right, down; -1 past the grid's edge.</summary>
    private void Neighbours(int cell, Span<int> into)
    {
        int w = _grid.Width, x = cell % w;
        into[0] = cell - w;
        into[1] = x > 0 ? cell - 1 : -1;
        into[2] = x < w - 1 ? cell + 1 : -1;
        into[3] = cell + w < w * _grid.Height ? cell + w : -1;
    }
}
