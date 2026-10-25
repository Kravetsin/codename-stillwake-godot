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
    public void NoBackWallIsPlannedOnAPlanet()
    {
        // A planet has no back layer to build in (the fan of rays).
        var w = Down(items: ("soil", 1));
        var m = w.On(1);
        int cell = m.Crew.Spawn - m.Grid.Width;
        Assert.Equal("building.notHere", m.Building.Problem(cell, Placement.BackWall("soil")));
        w.Do(new PlacePlan(cell, Placement.BackWall("soil"))).Run();
        Assert.Equal(["building.notHere"], w.Refusals.Select(r => r.Reason));
        Assert.Null(m.Grid.BackAt(cell));
    }

    [Fact]
    public void TheGroundsOwnBackWallIsNeverMarkedNorBuiltOver()
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

        // Nor is any back wall built over the sky there.
        Assert.Equal("building.notHere", m.Building.Problem(sky, Placement.BackWall("soil")));
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
