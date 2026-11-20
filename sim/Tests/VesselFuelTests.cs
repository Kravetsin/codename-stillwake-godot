using Stillwake.Content;
using Xunit;
using Xunit.Abstractions;

namespace Stillwake.Sim.Tests;

/// <summary>The fuel a vessel brings (vessel-wrecks fixes): what its tanks and crates hold and what breaking its tanks gives.</summary>
public class VesselFuelTests(ITestOutputHelper output)
{
    /// <summary>Biofuel the parts of <paramref name="b"/> give: held in their slots, and given as each machine is broken.</summary>
    public static int Fuel(Built b) => b.Parts.Where(p => p.Layer == "machine").Sum(p =>
        (p.Holds?.Where(h => h.Item == Content.Fuel.Item).Sum(h => h.Count) ?? 0) + Machines.Get(p.Kind).Cost.Where(c => c.Item == Content.Fuel.Item).Sum(c => c.Count));

    [Fact]
    public void ATankerBringsOneHundredAndFiftyToThreeHundredLitresOnAverage()
    {
        var all = Enumerable.Range(1, 200).Select(s => Fuel(Shipyard.Build(Vessels.Class("tanker")!, (uint)s))).Order().ToList();
        double mean = all.Average();
        output.WriteLine($"tanker fuel: mean {mean:F0}, p10 {all[19]}, p50 {all[99]}, p90 {all[179]}");
        foreach (var cls in Vessels.Classes)
            output.WriteLine($"{cls.Id}: mean {Enumerable.Range(1, 50).Average(s => Fuel(Shipyard.Build(cls, (uint)s))):F0}");
        Assert.InRange(mean, 150, 300);
    }
}
