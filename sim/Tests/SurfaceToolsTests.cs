using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Each surface's own way of taking things apart (the fan of rays): on a planet tools break all that stands and nothing is
/// marked; on the ship the plan alone takes things apart and tools break nothing of its grid.
/// </summary>
public class SurfaceToolsTests
{
    private static readonly string Axe = Weapons.Axe.Item;
    private const int Left = 128;

    /// <summary>A floor of the hull's scrap and stone, open over it: a character a cell.</summary>
    private static Layout Ground => Layout.Parse("""
        ........
        ........
        ########
        """).WithMaterials("""
        ........
        ........
        ..tttt..
        """);

    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static int Count(Scenario w, string item) => w.M.Items.Totals(0).Where(t => t.Item == item).Sum(t => t.Count);
    private static int At(int x, int y) => y * 8 + x;

    [Fact]
    public void OnTheShipToolsBreakNothingOfItsGridAndAreRefusedAsNotHere()
    {
        var w = new Scenario(Ground);
        Assert.True(w.M.Building.Struck(Axe, 3500, 1500, 64).IsEmpty);
        w.Do(HewTesting.Dig(w, At(3, 2), Axe)).Do(HewTesting.Dig(w, At(0, 2), Axe)).Run();
        Assert.Equal(["building.notHere", "building.notHere"], Reasons(w));
        Assert.Empty(w.M.Building.Cracks);
        Assert.Equal("stone", w.M.Grid.WallAt(At(3, 2))?.Id);
    }

    [Fact]
    public void OnTheShipAStoneWallIsTakenApartByAMarkAsAnyOther()
    {
        var w = new Scenario(Ground);
        w.Do(new Deconstruct(At(3, 2), "structure")).Run();
        Assert.Empty(w.Refusals);
        int plan = w.M.Building.Plans.Single().Id;
        for (int i = 0; i < w.M.Building.TicksFor(plan); i++) w.Do(new Work(plan)).Run();
        Assert.Null(w.M.Grid.WallAt(At(3, 2)));
        Assert.Equal(1, Count(w, "rock"));
    }

    [Fact]
    public void OnAPlanetNothingIsMarkedAndAMarkMadeBeforeIsNotWorked()
    {
        var w = new Scenario(Ground.OnPlanet());
        w.Do(new Deconstruct(At(3, 2), "structure")).Do(new Deconstruct(At(0, 2), "structure")).Run();
        Assert.Equal(["building.notHere", "building.notHere"], Reasons(w));
        Assert.Empty(w.M.Building.Plans);
    }

    [Fact]
    public void OnAPlanetToolsBreakAWallAHeroBuiltOfTheHullsScrapGivingItBack()
    {
        var w = new Scenario(Ground.OnPlanet());
        // The scrap at (0, 2): two of strength, one swing.
        w.Do(HewTesting.Dig(w, At(0, 2), Axe)).Run();
        Assert.Empty(w.Refusals);
        Assert.Null(w.M.Grid.WallAt(At(0, 2)));
        Assert.Equal(1, Count(w, "scrap"));
    }

    [Fact]
    public void OnAPlanetToolsBreakADoorWholeAtItsBuildsStrengthGivingWhatItCost()
    {
        // A closed door standing on the ground, two cells by two once doubled.
        var w = new Scenario(Layout.Parse("""
            ....
            ..D.
            ####
            """).OnPlanet(), doubled: true);
        int door = w.At(2, 1);
        var build = Builds.Get("door");
        Assert.Equal(4, w.M.Grid.PieceOf(door).Count);
        // From its right, aimed at it.
        var hit = new Hew(Axe, w.At(3, 1) % w.M.Grid.Width * 1000 + 1500, w.At(3, 1) / w.M.Grid.Width * 1000 + 1000, Left);
        Assert.Contains(door, w.M.Building.Struck(Axe, hit.X, hit.Y, hit.Heading).Cells);
        for (int i = 0; i < build.Strength / Weapons.Axe.BlockDamage; i++) w.Do(hit).Run();
        Assert.Empty(w.Refusals);
        Assert.All(w.M.Grid.PieceOf(door), c => Assert.Equal(Structure.Empty, w.M.Grid.StructureAt(c)));
        Assert.Equal(build.Cost.Sum(c => c.Count), w.M.Items.Totals(0).Where(t => build.Cost.Any(c => c.Item == t.Item)).Sum(t => t.Count));
    }
}
