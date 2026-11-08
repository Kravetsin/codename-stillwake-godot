namespace Stillwake.Sim.Water;

public sealed partial class WaterModule
{
    /// <summary>
    /// Pours bottles into machines by id, or one in three draws one to three out: mostly a cistern the grid has, which fills
    /// or empties, then refuses for a full or low cistern, a sender out of bottles or room; otherwise a low id, mostly no
    /// cistern, refused as such.
    /// </summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        var cisterns = new List<int>();
        foreach (var m in _grid.MachinesWith(Content.MachineTrait.HoldsWater)) cisterns.Add(m.Id);
        int machine = cisterns.Count > 0 && rng.Below(4) != 0 ? cisterns[(int)rng.Below((uint)cisterns.Count)] : (int)rng.Below(6);
        return rng.Below(3) == 0 ? new DrawWater(machine, 1 + (int)rng.Below(3)) { From = from } : new PourWater(machine) { From = from };
    }
}
