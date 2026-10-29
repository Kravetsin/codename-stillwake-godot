using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Hazards;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>Digging a planet's ground (0077): weapons crack natural walls and break them out at their strength.</summary>
public class DigTests
{
    /// <summary>Open ground over a row of walls: hull at 0, soil at 1 to 3, stone at 4 to 6, bedrock at 7, all back-walled.</summary>
    private const string Map = """
        ........
        ........
        ########
        ########
        """;

    private const string Ground = """
        ........
        ........
        .ssstttb
        .ssstttb
        """;

    private static readonly string Axe = Weapons.Axe.Item, Drill = Weapons.Drill.Item;
    private static readonly int AxeDamage = Weapons.Axe.BlockDamage;
    private static readonly Material Soil = Materials.Get("soil"), Stone = Materials.Get("stone");

    private static Layout Layout(IReadOnlyList<string>? known = null) => Sim.Layout.Parse(Map).WithMaterials(Ground) with { Known = known, Kind = Content.Surfaces.TestStrip.Id };
    private static Scenario World(IReadOnlyList<string>? known = null) => new(Layout(known));

    /// <summary>The same map aboard the ship, where walls are built and taken apart by the plan.</summary>
    private static Scenario Aboard(IReadOnlyList<string>? known = null) => new(Sim.Layout.Parse(Map).WithMaterials(Ground) with { Known = known });
    private static int At(int x, int y) => y * 8 + x;
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static int Count(Scenario w, string item, int peer = 0) => w.M.Items.Totals(peer).Where(t => t.Item == item).Sum(t => t.Count);
    private static int? CrackAt(Scenario w, int cell) => w.M.Building.Cracks.FirstOrDefault(c => c.Cell == cell)?.Damage;

    [Fact]
    public void ReadsTheMaterialsMapIntoWallsAndBackWalls()
    {
        var grid = World().M.Grid;
        Assert.Equal(["scrap", "soil", "soil", "soil", "stone", "stone", "stone", "bedrock"], Enumerable.Range(0, 8).Select(x => grid.WallAt(At(x, 2))!.Id));
        Assert.Equal("stone", grid.BackAt(At(5, 3))?.Id);
        // The open ground over them is back-walled with the default hull, as a map without materials is.
        Assert.Equal("scrap", grid.BackAt(At(2, 0))?.Id);
    }

    [Fact]
    public void TheAxeBreaksSoilAtASwingTheBackWallStayingAndTheSoilGoingToTheDigger()
    {
        var w = World();
        int cell = At(2, 2);
        Assert.Equal(1, Soil.Strength / AxeDamage);
        w.Do(HewTesting.Dig(w, cell, Axe)).Run();
        Assert.Empty(w.Refusals);
        Assert.Null(w.M.Grid.WallAt(cell));
        Assert.Equal("soil", w.M.Grid.BackAt(cell)?.Id);
        Assert.Null(CrackAt(w, cell));
        Assert.Equal(1, Count(w, "soil"));
        Assert.Empty(w.M.Items.Floor);
    }

    [Fact]
    public void StoneKeepsItsCrackBetweenHitsAndThroughASaveAndBreaksIntoRockAtItsStrength()
    {
        var w = World();
        int cell = At(5, 2);
        for (int i = 0; i < 2; i++) w.Do(HewTesting.Dig(w, cell, Axe)).Run();
        Assert.Equal(2 * AxeDamage, CrackAt(w, cell));
        w.Reload();
        Assert.Equal(2 * AxeDamage, CrackAt(w, cell));
        Assert.Equal("stone", w.M.Grid.WallAt(cell)?.Id);
        for (int i = 2; i < Stone.Strength / AxeDamage; i++) w.Do(HewTesting.Dig(w, cell, Axe)).Run();
        Assert.Null(w.M.Grid.WallAt(cell));
        Assert.Equal(1, Count(w, "rock"));
        Assert.Empty(w.M.Building.Cracks);
    }

    [Fact]
    public void KeepsEachWallsCrackApartInCellOrder()
    {
        var w = World();
        w.Do(HewTesting.Dig(w, At(6, 3), Axe)).Do(HewTesting.Dig(w, At(5, 2), Axe)).Do(HewTesting.Dig(w, At(4, 2), Axe)).Run();
        Assert.Equal([At(4, 2), At(5, 2), At(6, 3)], w.M.Building.Cracks.Select(c => c.Cell));
    }

    [Fact]
    public void ADrillDigsOnlyWhileItsHeroRunsItAndRefusesFlatWithNoBattery()
    {
        var w = World().GiveItems(0, (Drill, 1));
        w.Do(HewTesting.Dig(w, At(2, 2), Drill)).Run();
        Assert.Equal(["items.flat"], Reasons(w));
        Assert.Equal("items.flat", HewTesting.DigProblem(w, 0, At(2, 2), Drill));
        w.Do(new SetCreative(true)).Run();
        w.Do(HewTesting.Dig(w, At(2, 2), Drill)).Run();
        Assert.Equal(["items.flat", "items.notRunning"], Reasons(w));
        // What the client asks leaves the run out: its run goes first, in the same frame.
        Assert.Null(HewTesting.DigProblem(w, 0, At(2, 2), Drill));
        w.Do(new Run(Drill, true)).Do(HewTesting.Dig(w, At(2, 2), Drill)).Run();
        Assert.Equal(2, w.Refusals.Count);
        // Soil at one hit of the drill.
        Assert.Null(w.M.Grid.WallAt(At(2, 2)));
    }

    [Fact]
    public void RefusesAHitOnBedrockOpenGroundOrOffTheGridAsStrikingNothingAndBreaksTheHullAsAnyWall()
    {
        var w = World();
        // Bedrock stops every ray unhurt; from open ground aimed up the rays leave the grid.
        w.Do(HewTesting.Dig(w, At(7, 2), Axe)).Do(HewTesting.Dig(w, At(3, 0), Axe)).Do(HewTesting.Dig(w, -1, Axe)).Run();
        Assert.Equal(["building.nothingToDig", "building.nothingToDig", "building.nothingToDig"], Reasons(w));
        Assert.Empty(w.M.Building.Cracks);
        Assert.Equal("bedrock", w.M.Grid.WallAt(At(7, 2))?.Id);
        // On a planet a hull wall is any wall: struck, and its scrap given back.
        w.Do(HewTesting.Dig(w, At(0, 2), Axe)).Run();
        Assert.Null(w.M.Grid.WallAt(At(0, 2)));
        Assert.Equal(Materials.Get("scrap").Cost.Single(), ("scrapChunk", Count(w, "scrapChunk")));
    }

    [Fact]
    public void WhatAFullInventoryCannotTakeFallsWhereTheWallStood()
    {
        var w = World().GiveItems(0, ("scrap", Content.Items.Get("scrap").Stack * Inventory.Slots));
        int cell = At(1, 3);
        w.Do(HewTesting.Dig(w, cell, Axe)).Run();
        Assert.Null(w.M.Grid.WallAt(cell));
        var spilt = Assert.Single(w.M.Items.Floor);
        Assert.Equal(("soil", 1, cell), (spilt.Item, spilt.Count, spilt.Cell));
    }

    [Fact]
    public void OfTwoDiggersOnOneTickTheFirstBreaksTheWallAndTheSecondsRaysGoOnToTheWallBehindIt()
    {
        var w = World();
        int cell = At(5, 3);
        for (int i = 1; i < Stone.Strength / AxeDamage; i++) w.Do(HewTesting.Dig(w, cell, Axe)).Run();
        // Both aimed from inside it, up: the first breaks it, the second's rays go on through the gap to the stone above.
        var (one, two) = (HewTesting.Dig(w, cell, Axe) with { From = 1 }, HewTesting.Dig(w, cell, Axe) with { From = 0 });
        w.Do(one).Do(two).Run();
        Assert.Empty(Reasons(w));
        Assert.Equal(1, Count(w, "rock", peer: 1));
        Assert.Equal(0, Count(w, "rock", peer: 0));
        Assert.Equal(AxeDamage, CrackAt(w, At(5, 2)));
    }

    [Fact]
    public void RefusesEveryMarkOnAPlanetGroundOrHull()
    {
        var w = World();
        w.Do(new Deconstruct(At(2, 2), "structure")).Do(new Deconstruct(At(7, 2), "structure")).Run();
        w.Do(new Deconstruct(At(0, 2), "structure")).Run();
        Assert.Equal(["building.notHere", "building.notHere", "building.notHere"], Reasons(w));
        Assert.Equal("building.notHere", w.M.Building.Problem(At(1, 3), "structure"));
        Assert.Empty(w.M.Building.Plans);
    }

    [Fact]
    public void AboardASoilWallIsPlannedOnceSoilIsAnalysedBuiltOfSoilAndTakenApartForItAgain()
    {
        var w = Aboard(known: []).GiveItems(0, ("soil", 1));
        int cell = At(3, 1);
        var soilWall = Placement.Wall("soil");
        Assert.Equal("building.unknown", w.M.Building.Problem(cell, soilWall));
        w.M.Knowledge.Learn("soil");
        Assert.Null(w.M.Building.Problem(cell, soilWall));
        w.Do(new PlacePlan(cell, soilWall)).Run();
        int plan = w.M.Building.Plans.Single().Id;
        for (int i = 0; i < Soil.BuildTicks; i++) w.Do(new Work(plan)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal("soil", w.M.Grid.WallAt(cell)?.Id);
        Assert.Equal(0, Count(w, "soil"));
        w.Do(new Deconstruct(cell, "structure")).Run();
        int mark = w.M.Building.Plans.Single().Id;
        for (int i = 0; i < 20 && w.M.Building.Plans.Count > 0; i++) w.Do(new Work(mark)).Run();
        Assert.Null(w.M.Grid.WallAt(cell));
        Assert.Equal(1, Count(w, "soil"));
    }

    [Fact]
    public void NoBlueprintBuildsBedrockFrontOrBackNotEvenInCreativeModeAndAPlanetNoBackWallAtAll()
    {
        Assert.Equal("building.notHere", World().M.Building.Problem(At(3, 1), Placement.BackWall("stone")));
        var w = Aboard();
        w.Do(new SetCreative(true)).Run();
        Assert.Equal("building.unknown", w.M.Building.Problem(At(3, 1), Placement.Wall("bedrock")));
        w.Do(new ClearAt(At(3, 1), "back")).Run();
        Assert.Equal("building.unknown", w.M.Building.Problem(At(3, 1), Placement.BackWall("bedrock")));
        Assert.Null(w.M.Building.Problem(At(3, 1), Placement.BackWall("soil")));
        Assert.Null(w.M.Building.Problem(At(3, 1), Placement.BackWall("iron")));
        Assert.Null(w.M.Building.Problem(At(3, 1), Placement.Wall("stone")));
    }

    [Fact]
    public void ACrackGoesWithItsWallWhenAMeteorBreaksIt()
    {
        var w = World();
        w.Do(HewTesting.Dig(w, At(4, 3), Axe)).Run();
        w.Do(new MeteorHit(At(4, 3))).Run();
        Assert.Empty(w.M.Building.Cracks);
    }


    [Fact]
    public void ACrackLeftAloneHealsWholeAndADigBeginsAgain()
    {
        var w = World();
        int cell = At(5, 2);
        for (int i = 0; i < 2; i++) w.Do(HewTesting.Dig(w, cell, Axe)).Run();
        // Hit last on the second tick: it lasts CrackHeals ticks from there, through a save, then heals.
        w.Run(Builds.CrackHeals - 2).Reload();
        Assert.Equal(2 * AxeDamage, CrackAt(w, cell));
        w.Run();
        Assert.Null(CrackAt(w, cell));
        Assert.Equal("stone", w.M.Grid.WallAt(cell)?.Id);
        // A hit keeps a crack fresh: one every few seconds never heals.
        for (int i = 0; i < 2; i++) w.Do(HewTesting.Dig(w, cell, Axe)).Run(Builds.CrackHeals - 1);
        Assert.Equal(2 * AxeDamage, CrackAt(w, cell));
    }

    [Fact]
    public void AWallsLastTickWaitsWhileAHeroStandsInItsCellAndFinishesOnceItStepsOut()
    {
        var w = Aboard();
        int cell = At(3, 1);
        // The hero's body round the cell: its middle reported a cell above, the body under three cells tall.
        w.Do(new Crew.Enter(At(3, 0))).Do(new SetCreative(true)).Run();
        w.Do(new PlacePlan(cell, Placement.Wall("scrap"))).Run();
        int plan = w.M.Building.Plans.Single().Id, ticks = Materials.Get("scrap").BuildTicks;
        for (int i = 0; i < ticks; i++) w.Do(new Work(plan)).Run();
        Assert.Equal(["building.heroInside"], Reasons(w));
        Assert.Equal(ticks - 1, w.M.Building.Plans.Single().Progress);
        Assert.Equal("building.heroInside", w.M.Building.WorkProblem(plan, 0));
        Assert.Null(w.M.Grid.WallAt(cell));
        // A friend standing elsewhere changes nothing; the hero stepping out lets it finish.
        w.Do(new Crew.Enter(At(6, 0))).Do(new Work(plan) { From = 1 }).Run();
        Assert.Equal("scrap", w.M.Grid.WallAt(cell)?.Id);
        Assert.Empty(w.M.Building.Plans);
    }

    /// <summary>
    /// Whatever digs come, in whatever order and with whatever weapon: every crack lies on a wall that breaks, below
    /// its strength, in cell order; and every wall broken gave exactly what it is made of, no more.
    /// </summary>
    [Property(MaxTest = 60)]
    public Property ACrackLiesOnlyOnANaturalWallBelowItsStrengthAndABreakYieldsExactlyItsItem()
    {
        var gen =
            from digs in (from x in Gen.Choose(0, 7)
                          from y in Gen.Choose(0, 3)
                          from tool in Gen.Choose(0, 1)
                          from peer in Gen.Choose(0, 1)
                          select (cell: At(x, y), tool, peer)).ListOf()
            from reload in Gen.Choose(0, 80)
            select (digs: digs.Take(120).ToList(), reload);
        return Prop.ForAll(gen.ToArbitrary(), t =>
        {
            var w = World();
            w.Do(new SetCreative(true)).Run();
            w.Do(new Run(Drill, true)).Do(new Run(Drill, true) { From = 1 }).Run();
            var before = Enumerable.Range(0, 32).Select(c => w.M.Grid.WallAt(c)?.Id).ToArray();
            for (int i = 0; i < t.digs.Count; i++)
            {
                var (cell, tool, peer) = t.digs[i];
                w.Do(HewTesting.Dig(w, cell, tool == 0 ? Axe : Drill) with { From = peer }).Run();
                if (i == t.reload) w.Reload();
                var cracks = w.M.Building.Cracks;
                for (int k = 0; k < cracks.Count; k++)
                {
                    if (k > 0 && cracks[k - 1].Cell >= cracks[k].Cell) return false;
                    if (w.M.Grid.WallAt(cracks[k].Cell) is not { } wall || wall.Strength == Material.Unbreakable || cracks[k].Damage <= 0 || cracks[k].Damage >= wall.Strength) return false;
                }
            }
            int soil = 0, rock = 0, scrap = 0;
            for (int c = 0; c < 32; c++)
                if (before[c] is { } id && w.M.Grid.WallAt(c) == null)
                    foreach (var (item, count) in Materials.Get(id).Cost)
                        (soil, rock, scrap) = item switch { "soil" => (soil + count, rock, scrap), "scrapChunk" => (soil, rock, scrap + count), _ => (soil, rock + count, scrap) };
            int Got(string item) => Count(w, item, 0) + Count(w, item, 1) + w.M.Items.Floor.Where(s => s.Item == item).Sum(s => s.Count);
            return Got("soil") == soil && Got("rock") == rock && Got("scrapChunk") == scrap && before.Count(b => b is "bedrock") == Enumerable.Range(0, 32).Count(c => w.M.Grid.WallAt(c)?.Id is "bedrock");
        });
    }
}
