namespace Stillwake.Sim.Atmosphere;

public sealed partial class AtmosphereModule
{
    /// <summary>
    /// Puts canisters into machines by id: mostly a tank the grid has, which fills, then refuses for a
    /// full tank or a sender out of canisters; otherwise a low id, mostly no tank (the wreck's generator
    /// among them), refused as such.
    /// </summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        var tanks = new List<int>();
        foreach (var m in _grid.MachinesWith(Content.MachineTrait.Gas)) tanks.Add(m.Id);
        int tank = tanks.Count > 0 && rng.Below(4) != 0 ? tanks[(int)rng.Below((uint)tanks.Count)] : (int)rng.Below(6);
        return new LoadCanister(tank) { From = from };
    }
}
