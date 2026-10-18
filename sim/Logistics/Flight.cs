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

    private const int Unit = 1000;

    // Scratch for the floods and paths, inside one zone box at a time: derived, never saved.
    private int[] _seen = [], _parent = [], _queue = [], _dist = [];
    private int _stamp;
    private Box _box = Box.None;

    // Looking.

    /// <summary>
    /// Each bay whose turn it is, in id order, launches its first docked drone charged to <c>LaunchAt</c> on the job
    /// found from the bay, when its charge covers it.
    /// </summary>
    private void Look()
    {
        foreach (var bay in _grid.MachinesOf(Tune.Bay))
        {
            if ((State.Ticks + bay.Id) % BayOf(bay).LookEvery != 0) continue;
            int slot = ReadySlot(bay.Id);
            if (slot < 0 || !Flood(bay)) continue;
            if (FindJob(bay.Cell) is { } job && Affords(_items.Crate(bay.Id)[slot]!.Charge, bay.Cell, job, bay)) Launch(bay, slot, job);
        }
    }

    /// <summary>The first slot of bay <paramref name="bay"/> holding a drone charged to <c>LaunchAt</c>, or -1.</summary>
    private int ReadySlot(int bay)
    {
        var slots = _items.Crate(bay);
        for (int i = 0; i < slots.Count; i++)
            if (slots[i] is { } s && s.Item == Tune.Drone && s.Charge >= Tune.LaunchAt) return i;
        return -1;
    }

    /// <summary>Drone <paramref name="d"/> free in the air, its job done or gone: the next job from where it is, when its charge covers it; else home.</summary>
    private void Next(Drone d, Machine bay)
    {
        d.Leg = Leg.Home;
        if (!Flood(d.Cell, ZoneBox(bay)) || FindJob(d.Cell) is not { } job) return;
        if (Affords(d.Stack.Charge, d.Cell, job, bay)) Take(d, job);
        else d.Low = true;
    }

    /// <summary>Sets drone <paramref name="d"/> on <paramref name="job"/>, on its way to the source.</summary>
    private static void Take(Drone d, Job job)
    {
        (d.Leg, d.Low) = (Leg.Source, false);
        (d.Source, d.Slot, d.Target, d.Item, d.Count) = (job.Source, job.Slot, job.Target, job.Item, job.Count);
    }

    /// <summary>Launches the drone in slot <paramref name="slot"/> of <paramref name="bay"/> on <paramref name="job"/>, from the bay's first passable cell.</summary>
    private void Launch(Machine bay, int slot, Job job)
    {
        int cell = StartOf(bay);
        var stack = _itemsOps.TakeFrom(bay.Id, slot, 1) ?? throw new InvalidOperationException("logistics.launch: no drone docked");
        var (x, y) = Centre(cell);
        var d = new Drone { Id = State.NextId++, Bay = bay.Id, Stack = stack, X = x, Y = y, Cell = cell, To = cell };
        Take(d, job);
        State.Drones.Add(d);
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
    /// A tick of one drone: it pays the tick's draw from its charge, then flies. Ahead blocked, it turns back to the
    /// centre it left. At a centre it acts: in an airless cell, its bay gone, or with no way home it drops where it is;
    /// in its goal it arrives; on a job whose goal it can no longer reach, or with no more charge than the way home and
    /// <see cref="Spare"/> cells, it turns home, its cargo with it; else it takes the first step of a path found from
    /// here. Between centres it moves on.
    /// </summary>
    private void Fly(Drone d)
    {
        if (_grid.MachineById(d.Bay) is not { } bay || bay.Kind != Tune.Bay)
        {
            Drop(d);
            return;
        }
        // Flat only where a wall built since lengthened its way home past what it kept for it.
        int draw = Tune.Draw * Content.Charge.Scale;
        if (d.Stack.Charge < draw)
        {
            Drop(d);
            return;
        }
        d.Stack = d.Stack with { Charge = d.Stack.Charge - draw };
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
                var (step, home) = Search(d.Cell, goal, bay, box);
                if (home < 0)
                {
                    Drop(d);
                    return;
                }
                if (d.Leg != Leg.Home && (step < 0 || d.Stack.Charge < Cost(bay, home + Spare)))
                {
                    (d.Leg, d.Low) = (Leg.Home, step >= 0);
                    continue;
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
    /// The machine drone <paramref name="d"/> flies to on its leg. One whose source is gone looks for the next job; one
    /// whose target is gone lays its cargo where it is and looks: both null, for the caller to look again.
    /// </summary>
    private Machine? GoalOf(Drone d, Machine bay)
    {
        if (d.Leg == Leg.Home) return bay;
        var goal = _grid.MachineById(d.Leg == Leg.Source ? d.Source : d.Target);
        if (goal != null) return goal;
        if (d.Leg == Leg.Target && d.Cargo is { } cargo) _itemsOps.LayDown(cargo, d.Cell);
        d.Cargo = null;
        Next(d, bay);
        return null;
    }

    /// <summary>
    /// Drone <paramref name="d"/> in its goal. At its source it takes what is there up to its job, nothing if the slot
    /// holds another item now, and with nothing looks for the next job; at its target it puts in what fits, carries the
    /// rest on to a crate with room (<c>Storage</c>) or with none lays it down there, and looks for the next job; home, it docks in a free slot of its bay, or lies down where it is with
    /// none, and lays down any cargo it turned home with. Whether it still flies.
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
                if (taken == null) Next(d, bay);
                else d.Leg = Leg.Target;
                return true;
            case Leg.Target:
                int left = _itemsOps.PutIn(goal.Id, d.Cargo!);
                if (left > 0)
                {
                    // What did not fit goes on to a crate with room, else lies here.
                    var rest = d.Cargo! with { Count = left };
                    if (Flood(d.Cell, ZoneBox(bay)) && Storage(goal.Id, rest) is { } to && to.Crate != goal.Id)
                    {
                        (d.Cargo, d.Target) = (rest, to.Crate);
                        return true;
                    }
                    _itemsOps.LayDown(rest, d.Cell);
                }
                d.Cargo = null;
                Next(d, bay);
                return true;
            default:
                if (_itemsOps.PutIn(bay.Id, d.Stack) > 0) _itemsOps.LayDown(d.Stack, d.Cell);
                if (d.Cargo is { } cargo) _itemsOps.LayDown(cargo, d.Cell);
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
            _dist = new int[size];
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
        int tail = 0;
        var spec = Machines.Get(bay.Kind);
        for (int dy = 0; dy < spec.Height; dy++)
            for (int dx = 0; dx < spec.Width; dx++)
            {
                int cell = bay.Cell + dy * _grid.Width + dx;
                if (!Passable(cell)) continue;
                _seen[Local(cell)] = _stamp;
                _queue[tail++] = cell;
            }
        return Spread(tail);
    }

    /// <summary>The cells a drone at <paramref name="cell"/> reaches inside <paramref name="box"/>, as <see cref="Flood(Machine)"/>; false outside it.</summary>
    private bool Flood(int cell, Box box)
    {
        Begin(box);
        if (!InBox(cell) || !Passable(cell)) return Spread(0);
        _seen[Local(cell)] = _stamp;
        _queue[0] = cell;
        return Spread(1);
    }

    /// <summary>A flood on from the <paramref name="tail"/> cells queued; whether there were any.</summary>
    private bool Spread(int tail)
    {
        if (tail == 0)
        {
            _stamp += 1;
            return false;
        }
        int head = 0;
        Span<int> around = stackalloc int[4];
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

    /// <summary>Whether any cell of machine <paramref name="m"/> is in the last flood.</summary>
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
    /// From <paramref name="from"/>, through passable cells inside <paramref name="box"/>: the first step on a shortest
    /// path to any cell of <paramref name="goal"/>, and the cells to any of <paramref name="bay"/>'s, the way home. Breadth
    /// first, neighbours up, left, right, down, so ties go to the lowest cell; -1 for either with no path.
    /// </summary>
    private (int Step, int Home) Search(int from, Machine goal, Machine bay, Box box)
    {
        Begin(box);
        if (!InBox(from)) return (-1, -1);
        int head = 0, tail = 0, step = -1, home = Covers(bay, from) ? 0 : -1;
        Span<int> around = stackalloc int[4];
        _seen[Local(from)] = _stamp;
        _parent[Local(from)] = -1;
        _dist[Local(from)] = 0;
        _queue[tail++] = from;
        while (head < tail && (step < 0 || home < 0))
        {
            int c = _queue[head++];
            Neighbours(c, around);
            foreach (int n in around)
            {
                if (n < 0 || !InBox(n) || _seen[Local(n)] == _stamp || !Passable(n)) continue;
                _seen[Local(n)] = _stamp;
                _parent[Local(n)] = c;
                _dist[Local(n)] = _dist[Local(c)] + 1;
                if (home < 0 && Covers(bay, n)) home = _dist[Local(n)];
                if (step < 0 && Covers(goal, n))
                {
                    step = n;
                    while (_parent[Local(step)] != from) step = _parent[Local(step)];
                }
                _queue[tail++] = n;
            }
        }
        return (step, home);
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
