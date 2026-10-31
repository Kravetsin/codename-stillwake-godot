using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Research;
using Xunit;
using Wreck = Stillwake.Content.Wreck;

namespace Stillwake.Sim.Tests;

/// <summary>The wreck's original plates: scrap in every rule, there only to be dressed; a wall built in their place is clean scrap (the-scoop).</summary>
public class WreckScrapTests
{
    private static readonly Layout World = Registry.Wreck with { Meteors = false };
    private static readonly (int X, int Y) Corner = Layout.OffsetInWorld(Wreck.Width, Wreck.Height);

    /// <summary>The world's cell of a cell of the placed maps.</summary>
    private static int Cell(int x, int y) => (y + Corner.Y) * World.Width + x + Corner.X;

    private static IEnumerable<(int X, int Y)> Dressed() =>
        from y in Enumerable.Range(0, Wreck.Height)
        from x in Enumerable.Range(0, Wreck.Dress[y].Length)
        where Wreck.Dress[y][x] != ' '
        select (x, y);

    private static int ADressedCell() => Dressed().Select(c => Cell(c.X, c.Y)).First();

    [Fact]
    public void EveryDressedCellIsAnOriginalPlateAndEveryOriginalPlateIsDressed()
    {
        var m = new Scenario(World).M;
        var dressed = Dressed().ToHashSet();
        Assert.Equal(17, dressed.Count(c => Wreck.Dress[c.Y][c.X] == 'T'));
        Assert.All(dressed, c => Assert.Equal('#', Wreck.Rows[c.Y][c.X]));
        for (int y = 0; y < Wreck.Height; y++)
            for (int x = 0; x < Wreck.Width; x++)
                Assert.Equal(dressed.Contains((x, y)), m.Grid.WallAt(Cell(x, y))?.Id == "wreckScrap");
    }

    [Fact]
    public void TheDressHoldsOnlyItsFourLooks() =>
        Assert.All(Dressed(), c => Assert.Contains(Wreck.Dress[c.Y][c.X], "RrHT"));

    [Fact]
    public void ItIsScrapInEveryRule()
    {
        var (plate, scrap) = (Materials.Get("wreckScrap"), Materials.Get("scrap"));
        Assert.Equal((scrap.Strength, scrap.Leak, scrap.BuildTicks, scrap.Armour, scrap.Natural, scrap.Clear, scrap.Footing),
            (plate.Strength, plate.Leak, plate.BuildTicks, plate.Armour, plate.Natural, plate.Clear, plate.Footing));
        Assert.Equal(scrap.Cost, plate.Cost);
        Assert.Equal("scrap", plate.As);
    }

    [Fact]
    public void ItHasNoBlueprintAndIsRefusedEvenInCreative()
    {
        Assert.False(Materials.Get("wreckScrap").Built);
        Assert.False(Blueprints.Has(Placement.Wall("wreckScrap")));
        Assert.False(Blueprints.Has(Placement.BackWall("wreckScrap")));
        var w = new Scenario(World);
        w.Do(new SetCreative(true)).Run();
        int free = w.M.Crew.Spawn - 2 * World.Width - 8;
        Assert.Equal("building.unknown", w.M.Building.Problem(free, Placement.Wall("wreckScrap")));
        Assert.Equal("building.unknown", w.M.Building.Problem(free, Placement.BackWall("wreckScrap")));
    }

    [Fact]
    public void BrokenOutItLeavesAScrapBackWallAndAWallBuiltThereIsScrap()
    {
        var w = new Scenario(World);
        w.Do(new SetCreative(true)).Run();
        int cell = ADressedCell();
        Assert.Equal("wreckScrap", w.M.Grid.WallAt(cell)?.Id);
        Assert.Equal("scrap", w.M.Grid.BackAt(cell)?.Id);
        w.Do(new ClearAt(cell, "structure")).Run();
        Assert.Null(w.M.Grid.WallAt(cell));
        Assert.Equal("scrap", w.M.Grid.BackAt(cell)?.Id);
        w.Do(new PlacePlan(cell, Placement.Wall("scrap"))).Run();
        Assert.Empty(w.Refusals);
        int plan = w.M.Building.Plans.Single().Id;
        for (int i = 0; i < Materials.Get("scrap").BuildTicks; i++) w.Do(new Work(plan)).Run();
        Assert.Equal("scrap", w.M.Grid.WallAt(cell)?.Id);
    }

    [Fact]
    public void TakenApartItPaysAsScrap() =>
        Assert.Equal(Materials.Get("scrap").Cost, Costs.BuiltAt(new Scenario(World).M.Grid, ADressedCell(), "structure")!.Cost);
}
