using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>Standing machines are built on a wall, a floor or a grate (2026-11-07-machines-stand).</summary>
public class MachineFootingTests
{
    private static readonly Placement Tank = new("machine", "tank");
    private static readonly Placement Floor = new("structure", "floor");

    /// <summary>
    /// One character a cell: a deck of floor (x 5-8), grate (9-12), a hatch (13-16) and a launch platform (17-20) on row 7, open
    /// space under it, the hull's wall at the bottom.
    /// </summary>
    private const string Decks = """
        ########################
        #......................#
        #......................#
        #......................#
        #......................#
        #......................#
        #......................#
        #....====%%%%hhhh____..#
        #......................#
        #......................#
        #......................#
        ########################
        """;

    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();

    [Fact]
    public void StandsOnAWallAFloorOrAGrate()
    {
        var w = new Scenario(Decks, cells: true);
        w.Do(new PlacePlan(w.At(5, 3), Tank)).Do(new PlacePlan(w.At(9, 3), Tank)).Do(new PlacePlan(w.At(1, 7), Tank)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(3, w.M.Building.Plans.Count);
    }

    [Fact]
    public void RefusesAStandingMachineInTheAirOnAHatchOnALaunchPlatformOrHalfOverAnEdge()
    {
        var w = new Scenario(Decks, cells: true);
        foreach (var (x, y) in new[] { (1, 3), (13, 3), (17, 3), (3, 3) }) w.Do(new PlacePlan(w.At(x, y), Tank));
        w.Run();
        Assert.Equal(["building.noFloor", "building.noFloor", "building.noFloor", "building.noFloor"], Reasons(w));
        Assert.Empty(w.M.Building.Plans);
    }

    [Fact]
    public void HangsLampsAndVentsAnywhere()
    {
        var w = new Scenario(Decks, cells: true);
        foreach (var kind in new[] { "lamp", "vent" })
            Assert.Null(w.M.Building.Problem(w.At(2, 2), new Placement("machine", kind), 0));
        // Hung by an arc lamp's or the last crew's own; neither has a blueprint here.
        Assert.False(Machines.Stands("arcLamp") || Machines.Stands("emergencyLamp"));
    }

    [Fact]
    public void TakesAFloorPlannedUnderItButFinishesOnlyOnceTheFloorIsLaid()
    {
        var w = new Scenario(Decks, cells: true).Do(new Items.SetCreative(true));
        for (int x = 1; x <= 4; x++) w.Do(new PlacePlan(w.At(x, 7), Floor));
        w.Do(new PlacePlan(w.At(1, 3), Tank)).Run();
        Assert.Empty(w.Refusals);
        for (int i = 0; i < Machines.Get("tank").BuildTicks; i++) w.Do(new Work(5)).Run();
        Assert.Equal(["building.noFloor"], Reasons(w));
        Assert.Empty(w.M.Grid.Machines);

        for (int id = 1; id <= 4; id++)
            for (int i = 0; i < Builds.Get("floor").BuildTicks; i++) w.Do(new Work(id)).Run();
        w.Do(new Work(5)).Run();
        Assert.Equal("tank", w.M.Grid.MachineAt(w.At(1, 3))?.Kind);
    }

    [Fact]
    public void BuildingInstantlyCountsOnlyAFloorBuilt()
    {
        var w = new Scenario(Decks, cells: true);
        for (int x = 1; x <= 4; x++) w.Do(new PlacePlan(w.At(x, 7), Floor));
        w.Do(new SetInstant(true)).Do(new PlacePlan(w.At(1, 3), Tank)).Run();
        Assert.Equal(["building.noFloor"], Reasons(w));
    }

    [Fact]
    public void KeepsTheFloorUnderAStandingMachineMarkedOrWorked()
    {
        var w = new Scenario(Decks, cells: true).Do(new Items.SetCreative(true));
        // Marked while nothing stood on it, then a tank built on it at once: the mark's work is refused.
        w.Do(new Deconstruct(w.At(5, 7), "structure")).Do(new SetInstant(true)).Do(new PlacePlan(w.At(5, 3), Tank)).Run();
        Assert.Empty(w.Refusals);
        w.Do(new SetInstant(false)).Do(new Work(1)).Run();
        // Under the tank's far edge, refused; the hull's wall beside the deck, nothing on it, marked.
        w.Do(new Deconstruct(w.At(8, 7), "structure")).Do(new Deconstruct(w.At(0, 7), "structure")).Run();
        Assert.Equal(["building.holdsMachine", "building.holdsMachine"], Reasons(w));
        Assert.Equal(Structure.Floor, w.M.Grid.StructureAt(w.At(5, 7)));
        Assert.Single(w.M.Building.Plans, p => p.Clear == "structure" && p.Cell == w.At(0, 7));
    }
}
