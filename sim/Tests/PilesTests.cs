using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
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

    [Fact]
    public void TheShipStartsWithEachPileOnceOnAFloor()
    {
        var w = new Scenario(Wreck);
        foreach (var kind in Kinds)
        {
            var pile = PileOf(w, kind);
            var spec = Machines.Get(kind);
            int under = pile.Cell + spec.Height * Wreck.Width;
            // The row under it holds it up: a deck or the hull, so nothing floats.
            Assert.All(Enumerable.Range(0, spec.Width), x => Assert.True(w.M.Grid.WallAt(under + x) != null || w.M.Grid.StructureAt(under + x) != Structure.Empty, $"{kind} {x}"));
        }
    }

    [Fact]
    public void TheyPayForTheFirstThreeMachines()
    {
        var stock = Stock();
        foreach (var (item, count) in FirstMachines.SelectMany(k => Machines.Get(k).Cost).GroupBy(c => c.Item, StringComparer.Ordinal).Select(g => (g.Key, g.Sum(c => c.Count))))
            Assert.True(stock.GetValueOrDefault(item) >= count, item);
    }

    /// <summary>The first ring still costs 400 credits, so what is left over must not buy it: a player has to catch.</summary>
    [Fact]
    public void WhatTheyLeaveOverIsWorthLessThanTheFirstBlocks()
    {
        var left = Stock();
        foreach (var (item, count) in FirstMachines.SelectMany(k => Machines.Get(k).Cost)) left[item] -= count;
        Assert.InRange(left.Sum(l => l.Value * Market.WorthOf(l.Key)), 1, 399);
    }

    [Theory]
    [InlineData("pileHull")]
    [InlineData("pileScrap")]
    [InlineData("pileCargo")]
    [InlineData("pileBits")]
    public void AMarkTakesOneApartIntoTheBag(string kind)
    {
        var w = new Scenario(Wreck);
        var pile = PileOf(w, kind);
        w.Do(new Deconstruct(pile.Cell, "machine")).Run();
        int plan = Assert.Single(w.M.Building.Plans).Id;

        for (int i = 0; i < Machines.Get(kind).BuildTicks / Builds.DeconstructSpeedup - 1; i++) w.Do(new Work(plan) { From = 1 }).Run();
        Assert.Equal(kind, PileOf(w, kind).Kind);
        w.Do(new Work(plan) { From = 1 }).Run();

        Assert.DoesNotContain(w.M.Grid.Machines, m => m.Kind == kind);
        foreach (var (item, count) in Machines.Get(kind).Cost) Assert.Equal(count, w.M.Items.Totals(1).Single(t => t.Item == item).Count);
    }

    [Fact]
    public void APileIsNeverPlannedNotEvenInCreative()
    {
        Assert.All(Kinds, k => Assert.False(Research.Blueprints.Has(new Placement("machine", k))));
        var w = new Scenario(Wreck);
        w.Do(new Items.SetCreative(true)).Run();
        w.Do(new PlacePlan(w.M.Crew.Spawn - 2 * Wreck.Width - 8, new Placement("machine", "pileBits"))).Run();
        Assert.Equal("building.unknown", Assert.Single(w.Refusals).Reason);
        Assert.Empty(w.M.Building.Plans);
    }
}
