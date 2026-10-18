using Stillwake.Content;

namespace Stillwake.Sim.Logistics;

public sealed partial class LogisticsModule
{
    /// <summary>
    /// Roles mostly of the map's crates, now and then of another machine or an id that is none (not a crate); requests
    /// mostly at machines that may hold them, as many as they may or one too many, of any item, now and then none to
    /// clear them, and now and then at a machine that may not (cannot request): every refusal is drawn.
    /// </summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        int Below(int n) => (int)rng.Below((ulong)n);
        var machines = _grid.Machines;
        int AnyId() => machines.Count > 0 && Below(2) == 0 ? machines[Below(machines.Count)].Id : Below(60);
        if (Below(2) == 0)
        {
            var crates = _grid.MachinesOf("crate");
            int crate = crates.Count > 0 && Below(4) != 0 ? crates[Below(crates.Count)].Id : AnyId();
            return new SetRole(crate, (Role)Below(3)) { From = from };
        }
        var requesting = machines.Where(m => Most(m) > 0).ToList();
        int machine = requesting.Count > 0 && Below(5) != 0 ? requesting[Below(requesting.Count)].Id : AnyId();
        int most = RequestsMost(machine);
        int count = Below(6) == 0 ? 0 : 1 + Below(Math.Max(1, most) + 1);
        var all = Content.Items.All;
        var requests = Enumerable.Range(0, count).Select(_ => new Request(all[Below(all.Count)].Id, 1 + Below(60))).ToList();
        return new SetRequests(machine, requests) { From = from };
    }
}
