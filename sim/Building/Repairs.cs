using Cost = System.Collections.Generic.IReadOnlyList<(string Item, int Count)>;

namespace Stillwake.Sim.Building;

/// <summary>
/// One tick of a hero's work repairing broken machine <c>Machine</c> (2026-10-30-magnetic-flare), sent as <see cref="Work"/> is while the
/// screwdriver is held on it. The first tick of a repair pays its whole cost.
/// </summary>
public sealed record Repair(int Machine) : Command;

public sealed partial class BuildingModule
{
    // Queries.

    /// <summary>What repairing machine <paramref name="machine"/> costs, paid by its first tick of work; null for one not broken.</summary>
    public Cost? RepairCost(int machine) =>
        _power.IsBroken(machine) && _grid.MachineById(machine) is { } m ? Costs.RepairOf(m.Kind) : null;

    /// <summary>The ticks of work done repairing machine <paramref name="machine"/>, of all it takes; null for one not broken.</summary>
    public (int Done, int Of)? RepairProgress(int machine) =>
        _power.IsBroken(machine) && _grid.MachineById(machine) is { } m ? (RepairAt(machine) is int at and >= 0 ? State.Repairs[at].Progress : 0, Costs.RepairTicks(m.Kind)) : null;

    /// <summary>Why peer <paramref name="peer"/>'s repair of machine <paramref name="machine"/> would be refused: what the pointer shows.</summary>
    public string? RepairProblem(int machine, int peer) => RepairProblemOf(machine, peer);

    // The command.

    /// <summary>Where machine <paramref name="machine"/>'s repair stands in <c>Repairs</c>, or its complement where it would go.</summary>
    private int RepairAt(int machine)
    {
        int lo = 0, hi = State.Repairs.Count - 1;
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) >> 1);
            int at = State.Repairs[mid].Machine;
            if (at == machine) return mid;
            if (at < machine) lo = mid + 1;
            else hi = mid - 1;
        }
        return ~lo;
    }

    private string? RepairProblemOf(int machine, int peer)
    {
        // Gone, never powered, never broken or repaired already: Power alone says what is broken.
        if (!_power.IsBroken(machine) || _grid.MachineById(machine) is not { } m) return "building.notBroken";
        return RepairAt(machine) >= 0 ? null : _items.ShortOf(peer, Costs.RepairOf(m.Kind));
    }

    /// <summary>A tick of repair: the first takes the whole cost from the sender, the last mends the machine.</summary>
    private void ApplyRepair(Repair c)
    {
        string kind = _grid.MachineById(c.Machine)!.Kind;
        int at = RepairAt(c.Machine);
        if (at < 0)
        {
            _itemsOps.Take(c.From, Costs.RepairOf(kind));
            at = ~at;
            State.Repairs.Insert(at, new Repairing(c.Machine, 0));
        }
        var worked = State.Repairs[at] with { Progress = State.Repairs[at].Progress + 1 };
        if (worked.Progress < Costs.RepairTicks(kind))
        {
            State.Repairs[at] = worked;
            return;
        }
        State.Repairs.RemoveAt(at);
        _powerOps.Mend(c.Machine);
    }

    /// <summary>A repair whose machine is gone, or mended some other way, is dropped with what it was paid: one loss, no refund.</summary>
    private void SweepRepairs()
    {
        if (State.Repairs.Count > 0) State.Repairs.RemoveAll(r => !_power.IsBroken(r.Machine));
    }
}
