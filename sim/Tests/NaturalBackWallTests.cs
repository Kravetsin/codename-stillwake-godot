using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Rooms;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>Back walls of soil and stone (0084): built of their item, taken apart for it, the ground's own never marked.</summary>
public class NaturalBackWallTests
{
    private const string Ship = """
        ........
        .######.
        .#.S..#.
        .######.
        ........
        """;

    private static readonly Material Soil = Materials.Get("soil");

    /// <summary>A ship with hero 0 beamed down to the small planet, carrying <paramref name="items"/>; every blueprint known unless <paramref name="known"/>.</summary>
    private static Scenario Down(IReadOnlyList<string>? known = null, params (string Item, int Count)[] items)
    {
        var w = new Scenario(Layout.Parse(Ship) with { Known = known });
        w.Do(new Enter(w.M.Crew.Spawn)).Do(new OpenSurface(PlanetTypes.Small.Id)).Run();
        if (items.Length > 0) w.GiveItems(0, items);
        return w.Do(new Transfer(1)).Run();
    }

    private static int Count(Registry m, string item) => m.Items.Totals(0).Where(t => t.Item == item).Sum(t => t.Count);

    /// <summary>Works every plan on <paramref name="m"/> to its end, a tick of E at a time.</summary>
    private static void WorkAll(Scenario w, Registry m)
    {
        for (int i = 0; i < 100 && m.Building.Plans.Count > 0; i++) w.Do(new Work(m.Building.Plans[0].Id)).Run();
        Assert.Empty(m.Building.Plans);
    }

    [Fact]
    public void ASoilBackWallIsBuiltOfSoilUnderThePlanetsSkyAndStaysThroughALoad()
    {
        var w = Down(items: ("soil", 1));
        var m = w.On(1);
        int cell = m.Crew.Spawn - m.Grid.Width;
        Assert.Null(m.Grid.BackAt(cell));
        w.Do(new PlacePlan(cell, Placement.BackWall("soil"))).Run();
        WorkAll(w, m);
        Assert.Empty(w.Refusals);
        Assert.Equal("soil", m.Grid.BackAt(cell)?.Id);
        Assert.False(m.Grid.GrownBack(cell));
        Assert.Equal(0, Count(m, "soil"));

        // Natural ground holds no air (0077): a cell before it stays outside, as before the ground's own back walls.
        Assert.Equal(RoomsModule.Space, m.Rooms.RoomAt(cell));

        m = w.Reload().On(1);
        Assert.Equal("soil", m.Grid.BackAt(cell)?.Id);
        Assert.False(m.Grid.GrownBack(cell));
        // Nothing is marked on a planet, and tools leave back walls alone (the fan of rays): it stays.
        w.Do(new Deconstruct(cell, "back")).Run();
        Assert.Equal(["building.notHere"], w.Refusals.Select(r => r.Reason));
        Assert.Equal("soil", m.Grid.BackAt(cell)?.Id);
    }

    [Fact]
    public void TheGroundsOwnBackWallIsNeverMarkedNorBuiltOverAndASoilOneWaitsForSoilAnalysed()
    {
        var w = Down(known: [], items: ("fireAxe", 1));
        var m = w.On(1);
        int under = m.Crew.Spawn + m.Grid.Width, sky = m.Crew.Spawn - m.Grid.Width;
        for (int i = 0; i < Soil.Strength && m.Grid.WallAt(under) != null; i++) w.Do(HewTesting.Dig(w, under, "fireAxe", surface: 1)).Run();
        Assert.Null(m.Grid.WallAt(under));
        var ground = m.Grid.BackAt(under)!;
        Assert.True(ground.Natural);
        Assert.True(m.Grid.GrownBack(under));
        // Nothing is marked on a planet (the fan of rays), its ground's back wall no more than any.
        w.Do(new Deconstruct(under, "back")).Run();
        Assert.Equal(["building.notHere"], w.Refusals.Select(r => r.Reason));
        Assert.Empty(m.Building.Plans);
        Assert.Equal("grid.occupied", m.Grid.PlacementProblem(under, Placement.BackWall("stone")));

        Assert.Equal("building.unknown", m.Building.Problem(sky, Placement.BackWall("soil")));
        w.M.Knowledge.Learn("soil");
        Assert.Null(m.Building.Problem(sky, Placement.BackWall("soil")));
        Assert.Equal("building.unknown", m.Building.Problem(sky, Placement.BackWall("stone")));
        Assert.Equal("building.unknown", m.Building.Problem(sky, Placement.BackWall("bedrock")));
    }

    [Fact]
    public void AboardASoilBackWallClosesARoomAsAnyBackWallDoes()
    {
        var w = new Scenario(Ship);
        int cell = w.At(4, 2);
        Room RoomOf() => w.M.Rooms.RoomById(w.M.Rooms.RoomAt(cell)!.Value)!;
        w.Do(new ClearAt(cell, "back")).Run();
        Assert.Contains(cell, RoomOf().Openings);
        w.Do(new Place(cell, Placement.BackWall("soil"))).Run();
        Assert.Empty(RoomOf().Openings);
        Assert.False(w.M.Grid.GrownBack(cell));
    }
}
