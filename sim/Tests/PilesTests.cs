using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Research;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>The piles in the hold of the ship a world starts in: what they hold, and that only a tool's hit breaks them (hold-piles).</summary>
public class PilesTests
{
    private static readonly string[] Kinds = ["pileHull", "pileScrap", "pileCargo", "pileBits"];
    private static readonly string Axe = Weapons.Axe.Item;
    private static readonly Layout Wreck = Registry.Wreck with { Meteors = false };

    private static Machine PileOf(Scenario w, string kind) => w.M.Grid.Machines.Single(m => m.Kind == kind);

    private static Hew Hit(Scenario w, string kind, string tool) => HewTesting.Dig(w, PileOf(w, kind).Cell, tool);

    private static int Hits(string kind) => (Machines.Get(kind).Strength!.Value + Weapons.Axe.BlockDamage - 1) / Weapons.Axe.BlockDamage;

    private static Dictionary<string, int> Stock() =>
        Kinds.SelectMany(k => Machines.Get(k).Cost).GroupBy(c => c.Item, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Sum(c => c.Count), StringComparer.Ordinal);

    /// <summary>Breaks the pile of <paramref name="kind"/> with an axe peer 0 is given, for tests that need its cells free.</summary>
    internal static void Break(Scenario w, string kind)
    {
        w.GiveItems(0, (Axe, 1));
        for (int i = 0; i < Hits(kind); i++) w.Do(Hit(w, kind, Axe)).Run();
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

    [Theory]
    [InlineData("pileHull")]
    [InlineData("pileScrap")]
    [InlineData("pileCargo")]
    [InlineData("pileBits")]
    public void AnAxeBreaksOneOnItsLastHitIntoTheBagAboard(string kind)
    {
        var w = new Scenario(Wreck).GiveItems(0, (Axe, 1));
        for (int i = 0; i < Hits(kind) - 1; i++) w.Do(Hit(w, kind, Axe)).Run();
        Assert.Equal(PileOf(w, kind).Cell, Assert.Single(w.M.Building.Cracks).Cell);
        Assert.Single(w.M.Items.Totals(0));

        w.Do(Hit(w, kind, Axe)).Run();

        Assert.Empty(w.Refusals);
        Assert.DoesNotContain(w.M.Grid.Machines, m => m.Kind == kind);
        Assert.Empty(w.M.Building.Cracks);
        foreach (var (item, count) in Machines.Get(kind).Cost) Assert.Equal(count, w.M.Items.Totals(0).Single(t => t.Item == item).Count);
    }

    /// <summary>The way to a pile is by the first axe: a screwdriver's mark is refused, by a reason that says so.</summary>
    [Fact]
    public void AMarkIsRefused()
    {
        var w = new Scenario(Wreck);
        w.Do(new Deconstruct(PileOf(w, "pileBits").Cell, "machine")).Run();
        Assert.Equal("building.needsAxe", Assert.Single(w.Refusals).Reason);
        Assert.Empty(w.M.Building.Plans);
    }

    /// <summary>Aboard a tool still breaks nothing else: the wall, the deck and the back wall it swings at are the plan's.</summary>
    [Fact]
    public void AnAxeAboardStrikesOnlyPiles()
    {
        var w = new Scenario(Wreck);
        var pile = PileOf(w, "pileBits");
        var (x, y) = (pile.Cell % Wreck.Width * 1000 + 500, pile.Cell / Wreck.Width * 1000 + 500);
        for (int heading = 0; heading < Headings.Turn; heading += 8)
            Assert.Equal([pile.Cell], w.M.Building.Struck(Axe, x, y, heading).Cells);
        Assert.Empty(w.M.Building.Struck(Axe, w.M.Crew.Spawn % Wreck.Width * 1000 + 500, w.M.Crew.Spawn / Wreck.Width * 1000 + 500, 0).Cells);
    }

    [Fact]
    public void ACrackInOneHealsIfLeftAndSurvivesAReload()
    {
        var w = new Scenario(Wreck).GiveItems(0, (Axe, 1));
        w.Do(Hit(w, "pileHull", Axe)).Run();
        w.Reload();
        Assert.Single(w.M.Building.Cracks);
        w.Run(Builds.CrackHeals);
        Assert.Empty(w.M.Building.Cracks);
        Assert.Contains(w.M.Grid.Machines, m => m.Kind == "pileHull");
    }

    /// <summary>The bag is large enough: nothing of the whole stock drifts off.</summary>
    [Fact]
    public void AllOfThemFitInOneBag()
    {
        var w = new Scenario(Wreck);
        foreach (var kind in Kinds) Break(w, kind);
        var stock = Stock();
        Assert.All(stock, s => Assert.Equal(s.Value, w.M.Items.Totals(0).Single(t => t.Item == s.Key).Count));
    }

    [Fact]
    public void AnEmergencyLampDrawsNothingAndComesApartByAMarkAloneLeavingTheOther()
    {
        Assert.Null(Machines.Get("emergencyLamp").Draws);
        Assert.Null(Machines.Get("emergencyLamp").Strength);
        var w = new Scenario(Wreck);
        w.Do(new Deconstruct(w.M.Grid.MachinesOf("emergencyLamp")[0].Cell, "machine")).Run();
        int plan = Assert.Single(w.M.Building.Plans).Id;
        for (int i = 0; i < Costs.ApartTicks(Machines.Get("emergencyLamp").BuildTicks); i++) w.Do(new Work(plan) { From = 1 }).Run();
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
