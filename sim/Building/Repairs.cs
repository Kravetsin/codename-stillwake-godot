namespace Stillwake.Sim.Building;

/// <summary>
/// One tick of a hero's work repairing broken machine <c>Machine</c> (2026-10-30-magnetic-flare), sent as <see cref="Work"/> is while the
/// screwdriver is held on it. It takes nothing but the work (2026-10-30-free-repair).
/// </summary>
public sealed record Repair(int Machine) : Command;

public sealed partial class BuildingModule
{
    // Queries.

    /// <summary>The ticks of work done repairing machine <paramref name="machine"/>, of all it takes; null for one not broken.</summary>
    public (int Done, int Of)? RepairProgress(int machine) =>
        _power.IsBroken(machine) && _grid.MachineById(machine) is { } m ? (RepairAt(machine) is int at and >= 0 ? State.Repairs[at].Progress : 0, Costs.RepairTicks(m.Kind)) : null;

    /// <summary>Why a repair of machine <paramref name="machine"/> would be refused: what the pointer shows.</summary>
    public string? RepairProblem(int machine) =>
        // Gone, never powered, never broken or repaired already: Power alone says what is broken.
        _power.IsBroken(machine) && _grid.MachineById(machine) != null ? null : "building.notBroken";

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

    /// <summary>A tick of repair: the last mends the machine.</summary>
    private void ApplyRepair(Repair c)
    {
        string kind = _grid.MachineById(c.Machine)!.Kind;
        int at = RepairAt(c.Machine);
        if (at < 0)
        {
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

    /// <summary>A repair whose machine is gone, or mended some other way, is dropped.</summary>
    private void SweepRepairs()
    {
        if (State.Repairs.Count > 0) State.Repairs.RemoveAll(r => !_power.IsBroken(r.Machine));
    }
}
