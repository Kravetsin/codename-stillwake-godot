using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Research;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>The piles in the hold of the ship a world starts in: what they hold, and that the ordinary mark takes them apart (the-scoop).</summary>
public class PilesTests
{
    private static readonly string[] Kinds = ["pileHull", "pileScrap", "pileCargo", "pileBits"];
    private static readonly string[] FirstMachines = ["tradeStation", "furnace", "workbench"];
    private static readonly Layout Wreck = Registry.Wreck with { Meteors = false };

    private static Dictionary<string, int> Stock() =>
        Kinds.SelectMany(k => Machines.Get(k).Cost).GroupBy(c => c.Item, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Sum(c => c.Count), StringComparer.Ordinal);

    private static Machine PileOf(Scenario w, string kind) => w.M.Grid.Machines.Single(m => m.Kind == kind);

    /// <summary>Works <paramref name="machine"/>'s mark to one tick short of done, and returns the mark.</summary>
    private static int WorkAllButTheLastTick(Scenario w, Machine machine)
    {
        w.Do(new Deconstruct(machine.Cell, "machine")).Run();
        int plan = Assert.Single(w.M.Building.Plans).Id;
        for (int i = 0; i < Costs.ApartTicks(Machines.Get(machine.Kind).BuildTicks) - 1; i++) w.Do(new Work(plan) { From = 1 }).Run();
        return plan;
    }

    [Fact]
    public void TheShipStartsWithEachPileOnceOnAFloor()
    {
        var w = new Scenario(Wreck);
        foreach (var kind in Kinds)
        {
            var pile = PileOf(w, kind);
            var spec = Machines.Get(kind);
            int under = pile.Cell + spec.Height * Wreck.Width;
            Assert.All(Enumerable.Range(0, spec.Width), x => Assert.True(w.M.Grid.WallAt(under + x) != null || w.M.Grid.StructureAt(under + x) == Structure.Floor, $"{kind} {x}"));
        }
    }

    [Fact]
    public void TheyHoldWhatTheDesignSays()
    {
        var stock = Stock();
        Assert.Equal((16, 8, 6, 1), (stock["scrapChunk"], stock["hullPlate"], stock["planks"], stock["photoPanel"]));
        Assert.Equal(4, stock.Count);
    }

    [Fact]
    public void TheyPayForTheFirstThreeMachines()
    {
        var stock = Stock();
        foreach (var (item, count) in FirstMachines.SelectMany(k => Machines.Get(k).Cost).GroupBy(c => c.Item, StringComparer.Ordinal).Select(g => (g.Key, g.Sum(c => c.Count))))
            Assert.True(stock.GetValueOrDefault(item) >= count, item);
    }

    /// <summary>The blocks of the workbench and the furnace must still be bought, so what is left over must not buy them: a player has to catch.</summary>
    [Fact]
    public void WhatTheyLeaveOverIsWorthLessThanTheFirstBlocks()
    {
        var left = Stock();
        foreach (var (item, count) in FirstMachines.SelectMany(k => Machines.Get(k).Cost)) left[item] -= count;
        Assert.InRange(left.Sum(l => l.Value * Market.WorthOf(l.Key)), 1, Prices.WorthOf("workbench") + Prices.WorthOf("furnace") - 1);
    }

    [Theory]
    [InlineData("pileHull")]
    [InlineData("pileScrap")]
    [InlineData("pileCargo")]
    [InlineData("pileBits")]
    public void AMarkTakesOneApartIntoTheBagOnItsLastTick(string kind)
    {
        var w = new Scenario(Wreck);
        int plan = WorkAllButTheLastTick(w, PileOf(w, kind));
        Assert.Empty(w.M.Items.Totals(1));
        w.Do(new Work(plan) { From = 1 }).Run();

        Assert.DoesNotContain(w.M.Grid.Machines, m => m.Kind == kind);
        foreach (var (item, count) in Machines.Get(kind).Cost) Assert.Equal(count, w.M.Items.Totals(1).Single(t => t.Item == item).Count);
    }

    /// <summary>The bag is large enough: nothing of the whole stock drifts off.</summary>
    [Fact]
    public void AllOfThemFitInOneBag()
    {
        var w = new Scenario(Wreck);
        foreach (var kind in Kinds) w.Do(new Work(WorkAllButTheLastTick(w, PileOf(w, kind))) { From = 1 }).Run();
        var stock = Stock();
        Assert.Equal(stock.Count, w.M.Items.Totals(1).Count());
        Assert.All(w.M.Items.Totals(1), t => Assert.Equal(stock[t.Item], t.Count));
    }

    [Fact]
    public void AnEmergencyLampDrawsNothingAndComesApartAloneLeavingTheOther()
    {
        Assert.Null(Machines.Get("emergencyLamp").Draws);
        var w = new Scenario(Wreck);
        w.Do(new Work(WorkAllButTheLastTick(w, w.M.Grid.MachinesOf("emergencyLamp")[0])) { From = 1 }).Run();
        Assert.Single(w.M.Grid.MachinesOf("emergencyLamp"));
        Assert.Equal(1, w.M.Items.Totals(1).Single(t => t.Item == "scrapChunk").Count);
    }

    [Theory]
    [InlineData("pileHull")]
    [InlineData("pileScrap")]
    [InlineData("pileCargo")]
    [InlineData("pileBits")]
    [InlineData("emergencyLamp")]
    public void WhatHasNoCardIsNeverPlannedNotEvenInCreative(string kind)
    {
        Assert.False(Blueprints.Has(new Placement("machine", kind)));
        var w = new Scenario(Wreck);
        w.Do(new Items.SetCreative(true)).Run();
        w.Do(new PlacePlan(w.M.Crew.Spawn - 2 * Wreck.Width - 8, new Placement("machine", kind))).Run();
        Assert.Equal("building.unknown", Assert.Single(w.Refusals).Reason);
        Assert.Empty(w.M.Building.Plans);
    }
}
