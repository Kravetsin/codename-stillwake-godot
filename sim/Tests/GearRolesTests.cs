using Stillwake.Content;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// What helmets and suits each give (gear-roles): every worn piece adds its degrees to both limits; the heated helmet, made in
/// space, trades air for cold; the heat helmet, made on the planet, cold for heat.
/// </summary>
public class GearRolesTests
{
    private const string Room = """
        ##########
        #........#
        #.OOOII..#
        #.OOOII..#
        #.OOOII..#
        ##########
        """;

    /// <summary>A pod in a vacuum room, never aired.</summary>
    private const string PodRoom = """
        ###########
        #.........#
        #..OOO...S#
        #..OOO....#
        #..OOO....#
        ###########
        """;

    private const int Bench = 2;

    [Theory]
    // helmet, suit, cape: coldest, hottest withstood
    [InlineData(null, null, false, 0, 30)]
    [InlineData("helmet", null, false, -50, 40)]
    [InlineData(null, "suit", false, -60, 40)]
    [InlineData("helmet", "suit", false, -110, 50)]
    [InlineData("heavyHelmet", "suit", false, -110, 50)]
    [InlineData("heatedHelmet", "suit", false, -150, 40)]
    [InlineData("heatedHelmet", "regenSuit", false, -150, 40)]
    [InlineData("heatHelmet", "suit", false, -90, 70)]
    [InlineData("helmet", "heatSuit", false, -100, 60)]
    [InlineData("heatHelmet", "heatSuit", false, -80, 80)]
    [InlineData("heatedHelmet", "heatSuit", false, -140, 50)]
    [InlineData("heatedHelmet", "suit", true, -155, 35)]
    [InlineData("heatHelmet", "heatSuit", true, -85, 75)]
    public void EveryWornPieceAddsItsDegreesToBothLimits(string? helmet, string? suit, bool caped, int low, int high)
    {
        var w = new Scenario(PodRoom) { Bare = true };
        if (helmet != null) w.Wear(0, helmet);
        if (suit != null) w.Wear(0, suit);
        if (caped) w.Wear(0, Cape.Item);
        Assert.Equal((low, high), (w.M.Crew.LowOf(0), w.M.Crew.HighOf(0)));
        // Every outfit stands a room's warmth.
        Assert.InRange(Temperature.Room, low, high);
    }

    [Fact]
    public void EveryHelmetAndSuitAddsColdBelowZeroAndNoneTakesHeatAway()
    {
        foreach (var item in Content.Items.All)
        {
            if (item.Equip != Helmet.Slot && item.Equip != Suit.Slot) continue;
            Assert.True(item.Low < 0, $"{item.Id} keeps no cold out");
            Assert.True(item.High >= 0, $"{item.Id} takes heat away");
        }
    }

    [Fact]
    public void TheHeatedHelmetTradesTheHeavyOnesAirForColdAndTheHeatHelmetColdForHeat()
    {
        ItemSpec basic = Content.Items.Get(Helmet.Basic), heavy = Content.Items.Get(Helmet.Heavy), heated = Content.Items.Get(Helmet.Heated), heat = Content.Items.Get(Helmet.HeatProof);
        Assert.InRange(heated.Tank, basic.Tank + 1, heavy.Tank - 1);
        Assert.True(heated.Low < heavy.Low && heated.High < heavy.High);
        Assert.True(heat.High > basic.High && heat.Low > basic.Low);
    }

    [Theory]
    [InlineData("heatedHelmet", new[] { "helmet", "coil", "wire", "plate" }, new[] { 1, 2, 2, 2 })]
    [InlineData("heatHelmet", new[] { "helmet", "mica", "alloyScrap", "glass" }, new[] { 1, 2, 2, 1 })]
    public void TheWorkbenchMakesItRoundTheBasicHelmetWithNoPower(string helmet, string[] parts, int[] counts)
    {
        (string, int)[] recipe = [.. parts.Zip(counts)];
        var w = new Scenario(Layout.Parse(Room) with { Known = [] }, doubled: true).GiveItems(0, recipe);
        Assert.Contains(Stations.Get("workbench").Rows, r => r.Contains(helmet));
        Assert.Equal(recipe, Crafts.Get(helmet).Cost);
        w.M.Knowledge.Grant(helmet);
        w.Do(new Crafting.Craft(Bench, helmet, 1)).Run(Crafts.Get(helmet).Ticks * 1000 / Stations.Get("workbench").Speed + 1);
        Assert.Empty(w.Refusals);
        Assert.Equal(new Stack(helmet, 1), w.M.Items.Crate(Bench)[0]);
        Assert.Empty(w.M.Items.Totals(0));
    }
}
