namespace Stillwake.Sim.Fuel;

public sealed partial class FuelModule
{
    /// <summary>
    /// Draws one to three cans out of a machine by id: mostly a store the grid has, which empties, then refuses for a low store or a
    /// sender out of room; otherwise a low id, mostly no store, refused as such.
    /// </summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        var stores = new List<int>();
        foreach (var m in _grid.MachinesWith(Content.MachineTrait.HoldsFuel)) stores.Add(m.Id);
        int machine = stores.Count > 0 && rng.Below(4) != 0 ? stores[(int)rng.Below((uint)stores.Count)] : (int)rng.Below(6);
        return new DrawFuel(machine, 1 + (int)rng.Below(3)) { From = from };
    }
}
