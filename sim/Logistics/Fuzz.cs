using Tune = Stillwake.Content.Logistics;

namespace Stillwake.Sim.Logistics;

public sealed partial class LogisticsModule
{
    /// <summary>
    /// The switch mostly at the map's machines, on or off, now and then at a bay (refused as a bay) or at an id that is
    /// none (no machine): every refusal is drawn.
    /// </summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        int Below(int n) => (int)rng.Below((ulong)n);
        var machines = _grid.Machines;
        var bays = _grid.MachinesOf(Tune.Bay);
        int machine = Below(8) switch
        {
            0 => 1000 + Below(60),
            1 when bays.Count > 0 => bays[Below(bays.Count)].Id,
            _ when machines.Count > 0 => machines[Below(machines.Count)].Id,
            _ => Below(60),
        };
        return new SetServed(machine, Below(2) == 0) { From = from };
    }
}
