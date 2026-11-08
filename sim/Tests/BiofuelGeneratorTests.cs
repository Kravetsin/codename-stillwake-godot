using Stillwake.Sim.Furnace;
using Stillwake.Sim.Items;
using Xunit;
using FurnaceContent = Stillwake.Content.Furnace;
using Machines = Stillwake.Content.Machines;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The biofuel generator (the power-chain ADR): a fuel burner of biofuel alone, making 16 a tick while it burns,
/// breathing its room's air and lighting only while its network wants a fire, as the burner generator does.
/// </summary>
public class BiofuelGeneratorTests
{
    /// <summary>
    /// Twelve wide: a sealed room aired by a vaporizer piped to a vent, its tank and panel under the hull (liquid-oxygen);
    /// a biofuel generator, with <paramref name="cabled"/> cabled to a lamp.
    /// </summary>
    private static Layout Map(bool cabled = true) => AirTesting.Vaporized(
        ["############", "#qqq.TT.L..#", "#qqq.TTV...#", "############"],
        ["", "     o", "     ---"],
        cabled ? [((1, 1), (8, 1))] : []);

    /// <summary>The cell of the map's character at (<paramref name="x"/>, <paramref name="y"/>): its block's top-left, doubled (0104).</summary>
    private static int At(int x, int y) => 2 * y * 24 + 2 * x;
    private static readonly int Generator = At(1, 1), Lamp = At(8, 1);
    private static readonly int Makes = Machines.Get("biofuelGenerator").Makes!.Value;
    private static readonly int Fuel = Machines.Get("biofuelGenerator").FuelSlot!.Value;
    private static readonly int BiofuelTicks = Content.Items.Get("biofuel").BurnTicks!.Value;

    /// <summary>The world, its room aired, a peer 0 carrying biofuel, planks and fat.</summary>
    private static Scenario SetUp(bool cabled = true)
    {
        var w = new Scenario(Map(cabled), doubled: true).GiveItems(0, ("biofuel", 3), ("planks", 2), ("fat", 2));
        for (int i = 0; i < 2000 && !w.M.Atmosphere.AiredAt(Generator); i++) w.Run();
        Assert.True(w.M.Atmosphere.AiredAt(Generator));
        return w;
    }

    private static int Id(Scenario w) => w.M.Grid.MachineAt(Generator)!.Id;

    /// <summary>Moves <paramref name="count"/> of <paramref name="item"/> from peer 0 into the generator's fuel slot, and runs the tick.</summary>
    private static Scenario Feed(Scenario w, string item, int count = 1)
    {
        int from = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == item);
        return w.Do(new Move(new Items.Place(from), new Target(Fuel, Id(w)), count)).Run();
    }

    private static int FuelIn(Scenario w) => w.M.Items.Crate(Id(w))[Fuel]?.Count ?? 0;
    private static int Supply(Scenario w) => w.M.Power.NetworkOf(Generator)!.Supply;

    [Fact]
    public void MakesSixteenATickForTheSixteenHundredTicksABiofuelBurnsFromTheTickAfterItIsLit()
    {
        Assert.Equal((16, 1600), (Makes, BiofuelTicks));
        var w = SetUp();
        Feed(w, "biofuel");
        Assert.True(w.M.Furnace.At(Id(w)).Burning);
        Assert.Equal(0, FuelIn(w));
        w.Run();
        Assert.Equal(Makes, Supply(w));
        Assert.Equal(1000, w.M.Power.Satisfaction(w.M.Grid.MachineAt(Lamp)!.Id));
        // The move's tick was the first of its fire.
        w.Run(BiofuelTicks - 2);
        Assert.Equal(Makes, Supply(w));
        w.Run();
        Assert.False(w.M.Furnace.At(Id(w)).Burning);
        w.Run();
        Assert.Equal(0, Supply(w));
        Assert.Empty(w.Refusals);
    }

    /// <summary>Its slot takes biofuel alone: a plank or fat, both fuels elsewhere, is refused and stays with the hero.</summary>
    [Theory]
    [InlineData("planks")]
    [InlineData("fat")]
    public void TakesBiofuelAloneRefusingOtherFuels(string item)
    {
        Assert.Equal("biofuel", Machines.Get("biofuelGenerator").Takes);
        var w = SetUp();
        Feed(w, item);
        Assert.Equal(["items.notTaken"], w.Refusals.Select(r => r.Reason));
        Assert.Null(w.M.Items.Crate(Id(w))[Fuel]);
        Assert.False(w.M.Furnace.At(Id(w)).Burning);
    }

    /// <summary>On no network nothing wants its fire (0101), so its biofuel waits in its slot.</summary>
    [Fact]
    public void LightsOnlyWhileItsNetworkWantsAFire()
    {
        var w = SetUp(cabled: false);
        Feed(w, "biofuel", 2).Run(100);
        Assert.False(w.M.Furnace.At(Id(w)).Burning);
        Assert.Equal(2, FuelIn(w));
    }

    [Fact]
    public void BreathesItsRoomsAirAsTheBurnerDoes()
    {
        var w = SetUp();
        var cold = SetUp();
        foreach (var x in new[] { w, cold }) x.Run(200);
        Assert.Equal(ChainOxygen(cold), ChainOxygen(w));
        Feed(w, "biofuel");
        cold.Run();
        w.Run(4 * FurnaceContent.BreathEvery - 1);
        cold.Run(4 * FurnaceContent.BreathEvery - 1);
        Assert.Equal(4 * FurnaceContent.Breath, ChainOxygen(cold) - ChainOxygen(w));
    }

    /// <summary>The oxygen the chain holds: the tank's liquid and the vaporizer's gas (liquid-oxygen).</summary>
    private static int ChainOxygen(Scenario w) => w.M.Grid.Machines.Sum(m => w.M.Atmosphere.OxygenIn(m.Id));

    [Fact]
    public void GoesOutWithoutAirItsBiofuelLeftInItsSlot()
    {
        var w = SetUp();
        Feed(w, "biofuel", 2).Run();
        Assert.Equal(Makes, Supply(w));
        // A hole through both cells of the hull's right wall: the room is space, and the fire's next breath finds none.
        w.Do(new ClearAt(At(11, 1), "structure")).Do(new ClearAt(At(11, 1) + 1, "structure")).Run();
        w.Run(FurnaceContent.BreathEvery);
        Assert.Equal(new Smelting(false, Problem: "furnace.noAir"), w.M.Furnace.At(Id(w)));
        w.Run(100);
        Assert.Equal(0, Supply(w));
        Assert.Equal(1, FuelIn(w));
    }
}
