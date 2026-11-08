using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Items;
using Stillwake.Sim.Pipes;
using Stillwake.Sim.Water;
using Xunit;
using WaterContent = Stillwake.Content.Water;

namespace Stillwake.Sim.Tests;

/// <summary>Water (water): bottles poured into a cistern, split by the electrolyser into a tank's liquid oxygen.</summary>
public class WaterTests
{
    /// <summary>
    /// A cell a character: a cistern at (0, 0), its output (3, 2); the electrolyser at (6, 0), its water in at (6, 5) and its
    /// oxygen out at (9, 5); a tank at (12, 0), its input (12, 2) and output (15, 2); two panels out in space cabled to the electrolyser.
    /// </summary>
    private static readonly string[] Rows =
    [
        "cccc..yyyy..TTTT",
        "cccc..yyyy..TTTT",
        "cccc..yyyy..TTTT",
        "cccc..yyyy..TTTT",
        "......yyyy......",
        "......yyyy......",
        "                ",
        "PPPPPPPPPPPP    ",
        "PPPPPPPPPPPP    ",
    ];

    private static readonly ((int, int), (int, int))[] Cables = [((0, 7), (6, 0)), ((6, 7), (6, 0))];
    private const int W = 16;
    private static int At(int x, int y) => y * W + x;

    private static Scenario World() =>
        new Scenario(Layout.Parse(Rows, null, Cables), doubled: false).GiveItems(0, (WaterContent.Item, WaterContent.CisternBottles + 1), ("tube", 20)).GiveItems(1, (WaterContent.Item, 1));

    private static Scenario Piped() => World().Do(new LayPipe(At(3, 2), At(6, 5))).Do(new LayPipe(At(9, 5), At(12, 2))).Run();

    private static int Id(Scenario w, string kind) => w.M.Grid.MachinesOf(kind)[0].Id;
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static int Bottles(Scenario w, int peer) => w.M.Items.Totals(peer).Where(t => t.Item == WaterContent.Item).Sum(t => t.Count);

    [Fact]
    public void MeltsIceIntoABottleOfWaterAtTheFurnace()
    {
        var ice = Content.Items.Get("ice");
        Assert.Equal([(WaterContent.Item, 1)], ice.Yields);
        Assert.Equal(40, ice.SmeltTicks);
        Assert.Equal("smelt", ice.Process);
    }

    /// <summary>A cistern's hopper pours a bottle put into it when it fits, as one poured by hand, and waits while full (industrial-oxygen).</summary>
    [Fact]
    public void ItsHopperPoursABottleWhenItFitsAndWaitsWhileFull()
    {
        var w = World();
        int cistern = Id(w, "cistern"), slot = Content.Machines.Get("cistern").SlotTakes!.Value.Slot;
        Hop(w, cistern, slot).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(WaterContent.Bottle, w.M.Water.WaterIn(cistern));
        Assert.Null(w.M.Items.Crate(cistern)[slot]);
        for (int i = 1; i < WaterContent.CisternBottles; i++) w.Do(new PourWater(cistern));
        Hop(w.Run(), cistern, slot).Run(5);
        Assert.Equal(WaterContent.CisternCapacity, w.M.Water.WaterIn(cistern));
        Assert.Equal(1, w.M.Items.Crate(cistern)[slot]?.Count);
    }

    /// <summary>Peer 0 moves a bottle into <paramref name="slot"/> of <paramref name="cistern"/>.</summary>
    private static Scenario Hop(Scenario w, int cistern, int slot)
    {
        int from = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == WaterContent.Item);
        return w.Do(new Items.Move(new Items.Place(from), new Items.Target(slot, cistern), 1));
    }

    [Fact]
    public void PoursBottlesIntoACisternTillFullAndOnlyIntoACistern()
    {
        var w = World();
        int cistern = Id(w, "cistern");
        for (int i = 0; i < WaterContent.CisternBottles; i++) w.Do(new PourWater(cistern));
        w.Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(WaterContent.CisternCapacity, w.M.Water.WaterIn(cistern));
        Assert.Equal(1, Bottles(w, 0));
        w.Do(new PourWater(cistern)).Do(new PourWater(Id(w, "tank"))).Do(new PourWater(Id(w, "electrolyser"))).Run();
        Assert.Equal(["water.full", "water.noCistern", "water.noCistern"], Reasons(w));
        Assert.Equal(1, Bottles(w, 0));
    }

    /// <summary>Bottles drawn out of a cistern, several at once, as a tank's canisters are (industrial-oxygen).</summary>
    [Fact]
    public void DrawsBottlesOutOfACisternOrSaysWhyNot()
    {
        var w = World();
        int cistern = Id(w, "cistern"), carried = Bottles(w, 0);
        for (int i = 0; i < 3; i++) w.Do(new PourWater(cistern));
        w.Run().Do(new DrawWater(cistern, 2)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(WaterContent.Bottle, w.M.Water.WaterIn(cistern));
        Assert.Equal(carried - 1, Bottles(w, 0));
        w.Do(new DrawWater(Id(w, "tank"))).Do(new DrawWater(cistern, 2)).Run();
        w.GiveItems(2, ("planks", Content.Inventory.Slots * Content.Items.Get("planks").Stack));
        w.Do(new DrawWater(cistern) { From = 2 }).Run();
        Assert.Equal(["water.noCistern", "water.low", "items.full"], Reasons(w));
        Assert.Equal(WaterContent.Bottle, w.M.Water.WaterIn(cistern));
        w.Do(new DrawWater(cistern)).Run();
        Assert.Equal(0, w.M.Water.WaterIn(cistern));
    }

    [Fact]
    public void RefusesAPourWithNoBottleAndTheSecondOfTwoThatWouldSpillOnOneTick()
    {
        var w = World();
        int cistern = Id(w, "cistern");
        w.Do(new PourWater(cistern) { From = 2 }).Run();
        Assert.Equal(["items.notEnough"], Reasons(w));
        for (int i = 1; i < WaterContent.CisternBottles; i++) w.Do(new PourWater(cistern));
        w.Run();
        // Room for one more: both heroes pour on the same tick, the first in stamped order fills it.
        w.Do(new PourWater(cistern)).Do(new PourWater(cistern) { From = 1 }).Run();
        Assert.Equal(["items.notEnough", "water.full"], Reasons(w));
        Assert.Equal(WaterContent.CisternCapacity, w.M.Water.WaterIn(cistern));
        Assert.Equal(1, Bottles(w, 1));
    }

    [Fact]
    public void SplitsACisternsWaterIntoTheTanksLiquidOxygenAUnitATickAtFullPower()
    {
        var w = Piped();
        int cistern = Id(w, "cistern"), electrolyser = Id(w, "electrolyser"), tank = Id(w, "tank");
        Assert.Empty(w.Refusals);
        Assert.Equal("water.noWater", w.M.Water.Stalled(electrolyser));
        w.Do(new PourWater(cistern)).Run(30);
        Assert.Equal(1000, w.M.Power.Satisfaction(electrolyser));
        Assert.Null(w.M.Water.Stalled(electrolyser));
        Assert.Equal(WaterContent.Bottle - 30, w.M.Water.WaterIn(cistern));
        Assert.Equal(30, w.M.Atmosphere.OxygenIn(tank));
    }

    /// <summary>Switched off, an electrolyser splits nothing and says so; on again, it splits (industrial-oxygen).</summary>
    [Fact]
    public void SwitchedOffSplitsNothingAndSaysSo()
    {
        var w = Piped();
        int cistern = Id(w, "cistern"), electrolyser = Id(w, "electrolyser"), tank = Id(w, "tank");
        w.Do(new Power.SetSwitch(electrolyser, false)).Do(new PourWater(cistern)).Run(30);
        Assert.Equal("water.off", w.M.Water.Stalled(electrolyser));
        Assert.Equal(0, w.M.Atmosphere.OxygenIn(tank));
        w.Do(new Power.SetSwitch(electrolyser, true)).Run(30);
        Assert.Null(w.M.Water.Stalled(electrolyser));
        Assert.True(w.M.Atmosphere.OxygenIn(tank) > 0);
    }

    [Fact]
    public void StallsWithItsWorkKeptWhenTheTanksAreFullOrThePowerGoes()
    {
        int full = Content.Air.TankCapacity / Content.Air.Canister;
        var w = Piped().GiveItems(0, ("canister", full));
        int cistern = Id(w, "cistern"), electrolyser = Id(w, "electrolyser"), tank = Id(w, "tank");
        for (int i = 0; i < full; i++) w.Do(new LoadCanister(tank));
        w.Do(new PourWater(cistern)).Run(20);
        Assert.Equal(Content.Air.TankCapacity, w.M.Atmosphere.OxygenIn(tank));
        Assert.Equal(WaterContent.Bottle, w.M.Water.WaterIn(cistern));
        Assert.Equal("water.tanksFull", w.M.Water.Stalled(electrolyser));

        var v = Piped();
        foreach (var panel in v.M.Grid.MachinesOf("solarPanel")) v.Do(new ClearAt(panel.Cell, "machine"));
        v.Do(new PourWater(Id(v, "cistern"))).Run(20);
        Assert.Equal("water.noPower", v.M.Water.Stalled(Id(v, "electrolyser")));
        Assert.Equal(WaterContent.Bottle, v.M.Water.WaterIn(Id(v, "cistern")));
        Assert.Equal(0, v.M.Atmosphere.OxygenIn(Id(v, "tank")));
    }

    [Fact]
    public void PipesWaterOnlyToWaterAndATankTakesTheElectrolysersOxygenThroughItsInput()
    {
        var w = World();
        // A cistern's water to a tank's input: refused; the electrolyser's oxygen to the tank's output: two outputs.
        w.Do(new LayPipe(At(3, 2), At(12, 2))).Do(new LayPipe(At(9, 5), At(15, 2))).Run();
        Assert.Equal(["pipes.mixedFluid", "pipes.bothGive"], Reasons(w));
    }

    [Fact]
    public void ACisternTakenApartTakesItsWaterWithIt()
    {
        var w = World();
        int cistern = Id(w, "cistern");
        w.Do(new PourWater(cistern)).Run();
        w.Do(new ClearAt(At(0, 0), "machine")).Run();
        Assert.Equal(0, w.M.Water.WaterIn(cistern));
        Assert.Empty(((WaterState)w.M.Water.Slice).Cisterns);
    }
}
