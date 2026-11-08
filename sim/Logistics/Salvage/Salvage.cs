using Stillwake.Content;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Rooms;
using Tune = Stillwake.Content.Logistics;

namespace Stillwake.Sim.Logistics;

/// <summary>
/// Salvage drones (salvage-drones, salvage-trips): on the air their bay keeps, topped up from a canister in its fuel slot when
/// short, they fly out through doorways into space, net up to <c>SalvageHolds</c> of the pieces a net would catch and the
/// stacks floating there in the bay's zone, and bring them to crates, trip after trip until air or charge runs low; home,
/// their air left goes back to the bay. They serve no machine.
/// </summary>
public sealed partial class LogisticsModule
{
    /// <summary>Whether drone <paramref name="d"/> is a salvage drone: it flies through vacuum on air as well as charge.</summary>
    private static bool Salvages(Drone d) => d.Stack.Item == Tune.SalvageDrone;

    // Launching.

    /// <summary>
    /// Bay <paramref name="bay"/>'s salvage launch, when it launched no ordinary drone: its first salvage drone charged to
    /// <c>LaunchAt</c>, a served crate with an empty slot in its vacuum flood and a target whose trip the drone's charge and
    /// the air the bay keeps cover, with a canister from its fuel slot opened when the air kept alone does not. Then the
    /// drone flies with all the air kept; missing any, nothing is spent (salvage-trips).
    /// </summary>
    private void LookOut(Machine bay)
    {
        var slots = _items.Crate(bay.Id);
        if (Machines.Get(bay.Kind).SlotTakes is not { } fuel) return;
        int slot = -1;
        for (int i = 0; i < slots.Count && slot < 0; i++)
            if (slots[i] is { } s && s.Item == Tune.SalvageDrone && s.Charge >= Tune.LaunchAt) slot = i;
        if (slot < 0) return;
        int start = StartOf(bay, vacuum: true);
        if (start < 0 || !Flood(bay, vacuum: true)) return;
        int charge = slots[slot]!.Charge, kept = KeptBy(bay.Id);
        bool canister = slots[fuel.Slot] != null;
        if (Pick(start, bay, charge, kept + (canister ? Tune.TripAir : 0), out _) is not { } target) return;
        if (!AffordsTrip(charge, kept, start, target.Cell, bay))
        {
            _itemsOps.TakeFrom(bay.Id, fuel.Slot, 1);
            kept += Tune.TripAir;
        }
        var stack = _itemsOps.TakeFrom(bay.Id, slot, 1) ?? throw new InvalidOperationException("logistics.launch: no salvage drone docked");
        Keep(bay.Id, 0);
        var (x, y) = Centre(start);
        var d = new Drone { Id = State.NextId++, Bay = bay.Id, Stack = stack, X = x, Y = y, Cell = start, To = start, Air = kept };
        Aim(d, target);
        State.Drones.Add(d);
    }

    /// <summary>The air bay <paramref name="bay"/> keeps, ticks.</summary>
    private int KeptBy(int bay)
    {
        int at = KeptAt(bay);
        return at >= 0 ? State.Air[at].Air : 0;
    }

    /// <summary>Bay <paramref name="bay"/>'s air kept set to <paramref name="air"/>, its entry gone at nought.</summary>
    private void Keep(int bay, int air)
    {
        int at = KeptAt(bay);
        if (at >= 0 && air > 0) State.Air[at] = new BayAir(bay, air);
        else if (at >= 0) State.Air.RemoveAt(at);
        else if (air > 0) State.Air.Insert(~at, new BayAir(bay, air));
    }

    /// <summary>The index of bay <paramref name="bay"/>'s entry in <c>Air</c>, or the complement of where it would go.</summary>
    private int KeptAt(int bay)
    {
        int lo = 0, hi = State.Air.Count - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) / 2, id = State.Air[mid].Bay;
            if (id == bay) return mid;
            if (id < bay) lo = mid + 1;
            else hi = mid - 1;
        }
        return ~lo;
    }

    /// <summary>The air kept by bays no longer there dropped: a bay taken apart loses it, its canisters dropping as items.</summary>
    private void ForgetGoneBays()
    {
        for (int i = State.Air.Count - 1; i >= 0; i--)
            if (_grid.MachineById(State.Air[i].Bay) is not { } m || !Machines.Is(m.Kind, MachineTrait.Zone)) State.Air.RemoveAt(i);
    }

    /// <summary>Sets drone <paramref name="d"/> on its way to <paramref name="target"/>: a piece, or a floor stack; its net and load stay.</summary>
    private static void Aim(Drone d, (int Piece, int Floor, int Cell) target)
    {
        (d.Leg, d.Low, d.Target) = (Leg.Source, false, 0);
        (d.Piece, d.Floor) = (target.Piece, target.Floor);
    }

    /// <summary>
    /// The target for a salvage drone of <paramref name="bay"/> at <paramref name="from"/>, within the last flood, a vacuum one:
    /// of the pieces a net catches and the stacks afloat, unclaimed, the nearest by Manhattan distance, then pieces before
    /// stacks, then the lowest id, whose trip <paramref name="charge"/> and <paramref name="air"/> cover (<see cref="AffordsTrip"/>),
    /// with its cell. Null with no served crate with an empty slot in the flood, or none; <paramref name="short"/> when one
    /// was passed by for want of air or charge.
    /// </summary>
    private (int Piece, int Floor, int Cell)? Pick(int from, Machine bay, int charge, int air, out bool @short)
    {
        @short = false;
        if (!AnyEmptyCrate()) return null;
        var zone = ZoneBox(bay);
        (int Distance, int Kind, int Id)? best = null;
        (int Piece, int Floor, int Cell) found = default;
        bool shortOf = false;
        void Consider(int cell, int kind, int id)
        {
            var key = (Distance(from, cell), kind, id);
            if (best is { } b && key.CompareTo(b) >= 0) return;
            if (!AffordsTrip(charge, air, from, cell, bay))
            {
                shortOf = true;
                return;
            }
            best = key;
            found = kind == 0 ? (id, 0, cell) : (0, id, cell);
        }
        foreach (var piece in _debris.Pieces)
            if (Catchable(piece, zone) is int cell && InLastFlood(cell) && !Claimed(piece.Id, 0)) Consider(cell, 0, piece.Id);
        foreach (var stack in _items.Floor)
            if (Fetchable(stack, zone, fresh: true) && InLastFlood(stack.Cell) && !Claimed(0, stack.Id)) Consider(stack.Cell, 1, stack.Id);
        @short = shortOf && best == null;
        return best == null ? null : found;
    }

    /// <summary>The cell of piece <paramref name="p"/>'s middle when a salvage drone may take it: a kind a net catches, not reeled, in space inside <paramref name="zone"/>; else null.</summary>
    private int? Catchable(Piece p, Box zone)
    {
        if (p.Reel != null || !Content.Debris.NetCatches.Contains(p.Item, StringComparer.Ordinal)) return null;
        int cell = DebrisModule.CellOf(p.X, p.Y, _grid.Width);
        return _rooms.RoomAt(cell) == RoomsModule.Space && zone.Contains(cell % _grid.Width, cell / _grid.Width) ? cell : null;
    }

    /// <summary>
    /// Whether a salvage drone may fetch floor stack <paramref name="s"/>: no hero's body, afloat inside <paramref name="zone"/>,
    /// and, <paramref name="fresh"/>, lying at least <c>OwnPickupDelay</c>, so a stack just put down or thrown is not snatched.
    /// </summary>
    private bool Fetchable(FloorStack s, Box zone, bool fresh) =>
        s.Body == null && _rooms.Afloat(s.Cell) && zone.Contains(s.Cell % _grid.Width, s.Cell / _grid.Width)
        && (!fresh || _items.Ticks - s.At >= Content.Floor.OwnPickupDelay);

    /// <summary>Whether a drone goes for piece <paramref name="piece"/> or floor stack <paramref name="floor"/> (0 for neither): claims are read off the drones, never stored.</summary>
    private bool Claimed(int piece, int floor)
    {
        foreach (var d in State.Drones)
            if ((piece != 0 && d.Piece == piece) || (floor != 0 && d.Floor == floor)) return true;
        return false;
    }

    /// <summary>Whether cell <paramref name="cell"/> is in the last flood.</summary>
    private bool InLastFlood(int cell) => InBox(cell) && _seen[Local(cell)] == _stamp;

    /// <summary>Whether a served crate in the last flood has an empty slot: a stand-in for room for a load not yet caught.</summary>
    private bool AnyEmptyCrate() => NearestEmptyCrate(0) != null;

    /// <summary>The served crate in the last flood with an empty slot nearest <paramref name="from"/> by Manhattan distance, then the lowest id; null for none.</summary>
    private Machine? NearestEmptyCrate(int from)
    {
        Machine? best = null;
        int near = int.MaxValue;
        foreach (var crate in _grid.MachinesWith(MachineTrait.Container))
        {
            if (!Served(crate.Id) || !InFlood(crate) || !HasEmpty(crate.Id)) continue;
            int distance = Distance(from, crate.Cell);
            if (distance < near) (best, near) = (crate, distance);
        }
        return best;
    }

    private bool HasEmpty(int crate)
    {
        foreach (var s in _items.Crate(crate))
            if (s == null) return true;
        return false;
    }

    /// <summary>
    /// Whether <paramref name="charge"/> and <paramref name="air"/> cover a trip from <paramref name="from"/> to a target at
    /// <paramref name="target"/>, on to the crate with an empty slot nearest it and home: the Manhattan lengths, the crate's
    /// <see cref="Across"/> and <see cref="Spare"/>, as <see cref="Affords"/> counts a job's. Paths round walls may be longer.
    /// </summary>
    private bool AffordsTrip(int charge, int air, int from, int target, Machine bay)
    {
        if (NearestEmptyCrate(target) is not { } crate) return false;
        int cells = Distance(from, target) + Distance(target, crate.Cell) + Across(crate) + Distance(crate.Cell, bay.Cell) + Spare;
        return air >= TicksOver(bay, cells) && charge >= Cost(bay, cells);
    }

    // Flying.

    /// <summary>
    /// The cells salvage drone <paramref name="d"/> flies to on its leg: its target's cell, its crate's footprint, its bay's.
    /// A target gone, reeled, out of space or out of the zone sends it on (<see cref="NextOut"/>); a crate gone or switched off
    /// has the load go to another with room, else laid where it is.
    /// </summary>
    private Box OutGoal(Drone d, Machine bay)
    {
        switch (d.Leg)
        {
            case Leg.Source:
                if (TargetCell(d, ZoneBox(bay)) is int at)
                {
                    int w = _grid.Width;
                    return new Box(at % w, at / w, at % w, at / w);
                }
                // Picked as OutGoal would keep it, so this goes one deep at most.
                NextOut(d, bay);
                return OutGoal(d, bay);
            case Leg.Target:
                if (_grid.MachineById(d.Target) is { } crate && Machines.Is(crate.Kind, MachineTrait.Container) && Served(crate.Id)) return Foot(crate);
                if (Deliver(d, bay, 0)) return Foot(_grid.MachineById(d.Target)!);
                LayCarried(d, d.Cell);
                NextOut(d, bay);
                return OutGoal(d, bay);
            default:
                return Foot(bay);
        }
    }

    /// <summary>The cell of salvage drone <paramref name="d"/>'s target while it may still take it inside <paramref name="zone"/>; null for one gone, reeled, out of space or of the zone.</summary>
    private int? TargetCell(Drone d, Box zone)
    {
        if (d.Piece != 0) return _debris.PieceById(d.Piece) is { } piece ? Catchable(piece, zone) : null;
        foreach (var s in _items.Floor)
            if (s.Id == d.Floor) return Fetchable(s, zone, fresh: false) ? s.Cell : null;
        return null;
    }

    /// <summary>
    /// Salvage drone <paramref name="d"/> in its goal. At its target it catches the piece or lifts the stack into its net, and
    /// goes on (<see cref="NextOut"/>): to the next target while the net has room, else to crates. At a crate it puts in what
    /// fits, stack by stack, the rest on to the next crate with room, or laid there with none, its net emptied, and goes on.
    /// Home, it docks, the air it has left kept by its bay. Whether it still flies.
    /// </summary>
    private bool ArriveOut(Drone d, Machine bay)
    {
        switch (d.Leg)
        {
            case Leg.Source:
                if (d.Piece != 0)
                {
                    string kind = _debris.PieceById(d.Piece)?.Item ?? "";
                    if (_debrisOps.Catch(d.Piece) is { } loot && loot.Count > 0)
                        Net(d, new Netted(kind, d.Piece), loot.Select(l => new Stack(l.Item, l.Count)));
                }
                else if (_itemsOps.Lift(d.Floor) is { } lifted) Net(d, new Netted(lifted.Item, 0), [lifted]);
                NextOut(d, bay);
                return true;
            case Leg.Target:
                int crate = d.Target;
                var left = new List<Stack>();
                foreach (var stack in d.Loaded)
                {
                    int over = _itemsOps.PutIn(crate, stack);
                    if (over > 0) left.Add(stack with { Count = over });
                }
                d.Load = left.Count > 0 ? left : null;
                if (d.Load != null && Deliver(d, bay, crate)) return true;
                LayCarried(d, d.Cell);
                NextOut(d, bay);
                return true;
            default:
                Dock(d, bay);
                return false;
        }
    }

    /// <summary>A catch, <paramref name="netted"/>, in drone <paramref name="d"/>'s net, what it gave, <paramref name="loot"/>, added to its load.</summary>
    private static void Net(Drone d, Netted netted, IEnumerable<Stack> loot)
    {
        (d.Net ??= []).Add(netted);
        (d.Load ??= []).AddRange(loot);
    }

    /// <summary>
    /// Sends salvage drone <paramref name="d"/> with its load to the nearest served crate in its flood, but <paramref name="not"/>,
    /// with room for any stack of it, then the lowest id. False for none.
    /// </summary>
    private bool Deliver(Drone d, Machine bay, int not)
    {
        if (!Flood(d.Cell, ZoneBox(bay), vacuum: true)) return false;
        Machine? best = null;
        int near = int.MaxValue;
        foreach (var crate in _grid.MachinesWith(MachineTrait.Container))
        {
            if (crate.Id == not || !Served(crate.Id) || !InFlood(crate) || !d.Loaded.Any(s => _items.RoomFor(crate.Id, s) > 0)) continue;
            int distance = Distance(d.Cell, crate.Cell);
            if (distance < near) (best, near) = (crate, distance);
        }
        if (best == null) return false;
        (d.Leg, d.Target) = (Leg.Target, best.Id);
        return true;
    }

    /// <summary>
    /// Salvage drone <paramref name="d"/> free in space, its target taken, gone or out of reach, or its load delivered: while its
    /// net holds fewer than <c>SalvageHolds</c>, the next target from where it is whose trip its air and charge cover; else, or
    /// with none, its load to crates (<see cref="Deliver"/>); else home, with the load to lay at its bay, or empty and <c>Low</c>
    /// when a target was passed by for want of air or charge.
    /// </summary>
    private void NextOut(Drone d, Machine bay)
    {
        (d.Leg, d.Low, d.Piece, d.Floor, d.Target) = (Leg.Home, false, 0, 0, 0);
        if (!Flood(d.Cell, ZoneBox(bay), vacuum: true)) return;
        bool @short = false;
        if (d.Caught.Count < Tune.SalvageHolds && Pick(d.Cell, bay, d.Stack.Charge, d.Air, out @short) is { } target) Aim(d, target);
        else if (d.Load != null) Deliver(d, bay, 0);
        else d.Low = @short;
    }
}
