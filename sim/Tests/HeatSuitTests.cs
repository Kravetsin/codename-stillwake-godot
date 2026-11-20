using Stillwake.Content;
using Stillwake.Sim.Research;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>The heat suit (2026-11-14-heat-limits): made at the workbench of mica, the planet's alloy, plates and tubes, understood by the board's rule.</summary>
public class HeatSuitTests
{
    private const string Room = """
        ##########
        #........#
        #.OOOII..#
        #.OOOII..#
        #.OOOII..#
        ##########
        """;

    private const int Bench = 2;
    private static readonly string Heat = Content.Suit.Heat;

    [Fact]
    public void TheWorkbenchMakesOneOfFourMicaFourAlloyTwoPlatesAndTwoTubes()
    {
        (string, int)[] recipe = [("mica", 4), ("alloyScrap", 4), ("plate", 2), ("tube", 2)];
        var w = new Scenario(Layout.Parse(Room) with { Known = [] }, doubled: true).GiveItems(0, recipe);
        Assert.Contains(Stations.Get("workbench").Rows, r => r.Contains(Heat));
        Assert.Equal(recipe, Crafts.Get(Heat).Cost);
        w.M.Knowledge.Grant(Heat);
        w.Do(new Crafting.Craft(Bench, Heat, 1)).Run(Crafts.Get(Heat).Ticks * 1000 / Stations.Get("workbench").Speed + 1);
        Assert.Empty(w.Refusals);
        Assert.Equal(new Items.Stack(Heat, 1), w.M.Items.Crate(Bench)[0]);
        Assert.Empty(w.M.Items.Totals(0));
    }

    /// <summary>Not sold: one of each part ever obtained and the workbench standing teach it, all but its last part do not.</summary>
    [Fact]
    public void ItIsUnderstoodOnceOneOfEachPartWasObtainedBesideAWorkbench()
    {
        var w = new Scenario(Layout.Parse(Room) with { Known = Content.Research.StartKnown }, doubled: true);
        Assert.DoesNotContain(Heat, Content.Research.GearSold);
        var cost = Crafts.Get(Heat).Cost;
        foreach (var (item, _) in cost.SkipLast(1)) w.M.Knowledge.Find(0, item);
        Assert.False(w.Run(5).M.Research.Knows(0, Heat));
        w.M.Knowledge.Find(0, cost[^1].Item);
        Assert.True(w.Run(5).M.Research.Knows(0, Heat));
    }

    [Fact]
    public void ItWithstandsTheColdAsTheBasicAndTheHeatTenDegreesMore()
    {
        var (heat, basic) = (Content.Items.Get(Heat), Content.Items.Get(Content.Suit.Basic));
        Assert.Equal((basic.Low, basic.High + 10, Content.Suit.Slot), (heat.Low, heat.High, heat.Equip));
    }
}
