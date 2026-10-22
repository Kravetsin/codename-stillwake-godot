using Stillwake.Game.Features.Power;
using Stillwake.Sim;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The capacitor bank's gauge and the world it is recorded in (0101).</summary>
public class StoreGaugeTests
{
    [Fact]
    public void LightsSegmentsFromTheFootAsFarAsTheBankIsFullTheLowestNeverDarkUntilEmpty()
    {
        float[] Lit(int energy) => Enumerable.Range(0, StoreGauge.Segments).Select(s => StoreGauge.Lit(s, energy, 12000)).ToArray();
        Assert.Equal(new float[6], Lit(0));
        Assert.Equal(new[] { StoreGauge.Least, 0, 0, 0, 0, 0 }, Lit(1));
        Assert.Equal(new[] { 1, 0.5f, 0, 0, 0, 0 }, Lit(3000));
        Assert.Equal(new float[] { 1, 1, 1, 1, 1, 1 }, Lit(12000));
    }

    [Fact]
    public void ReadsWhatABankHoldsOfFullGroupedAsTheWristsEnergyWithAnArrowWhilePowerMoves()
    {
        string thin = Shared.Charges.Thin.ToString();
        Assert.Equal($"8{thin}400 / 12{thin}000 {StoreGauge.In}", StoreGauge.Reading(8400, 12000, 4));
        Assert.Equal($"950 / 12{thin}000 {StoreGauge.Out}", StoreGauge.Reading(950, 12000, -5));
        Assert.Equal($"0 / 12{thin}000", StoreGauge.Reading(0, 12000, 0));
    }

    [Fact]
    public void RunsTheSparkUpWhilePowerGoesInDownWhileItComesOutAndNowhereWhileTheBankHolds()
    {
        int Brightest(int flow, double seconds) =>
            Enumerable.Range(0, StoreGauge.Segments).OrderByDescending(s => StoreGauge.SparkAt(s, flow, seconds)).First();
        double early = StoreGauge.SparkS * 0.2, late = StoreGauge.SparkS * 0.8;
        Assert.True(Brightest(1, early) < Brightest(1, late));
        Assert.True(Brightest(-1, early) > Brightest(-1, late));
        Assert.All(Enumerable.Range(0, StoreGauge.Segments), s => Assert.Equal(0, StoreGauge.SparkAt(s, 0, early)));
    }

    [Fact]
    public void TheRecordedWorldFillsItsBankByThreeAndOnceTheSmelterWorksWithTheArcItGivesFive()
    {
        var m = new Registry();
        var world = StoreWorld.Create(m.All);
        int bank = m.Grid.MachinesOf("capacitorBank")[0].Id, smelter = m.Grid.MachinesOf("smelter")[0].Id;
        world.Step([]);
        Assert.Equal((StoreWorld.Holds + 3, 3), (m.Power.Store(bank)!.Energy, m.Power.Store(bank)!.Flow));
        world.Step([new Move(new Place(StoreWorld.RockSlot), new Target(0, smelter), StoreWorld.Rock)]);
        for (int i = 0; i < 20; i++) world.Step([]);
        Assert.Equal(-5, m.Power.Store(bank)!.Flow);
        Assert.Equal(1000, m.Power.Satisfaction(smelter));
        Assert.Equal(8, m.Power.Draw(smelter));
    }
}
