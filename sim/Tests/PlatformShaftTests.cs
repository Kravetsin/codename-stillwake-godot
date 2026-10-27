using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The shaft under a launch platform (the descent's): what the pod falls through, empty of everything solid down to the grid's
/// edge. A platform is placed only over an empty one, and nothing solid is placed in one, built or planned; back walls may be.
/// </summary>
public class PlatformShaftTests
{
    /// <summary>A room whose floor has a gap over open space, a run built, and a gap over a wall a few rows down.</summary>
    private static readonly string[] Rows =
    [
        "################",
        "#..............#",
        "#..............#",
        "###  #__###  ###",
        "                ",
        "                ",
        "          ##    ",
    ];

    private const int Wide = 32;
    private static int At(int x, int y) => y * Wide + x;
    private static readonly Placement Platform = new("structure", Builds.Platform);
    private static Scenario World() => new(Layout.Parse(Rows), doubled: true);
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();

    [Fact]
    public void APlatformGoesOnlyOverAShaftEmptyToTheEdge()
    {
        var grid = World().M.Grid;
        Assert.Null(grid.PlacementProblem(At(6, 6), Platform));
        // The wall six rows under the second gap blocks it, however far down.
        Assert.Equal("grid.shaftBlocked", grid.PlacementProblem(At(22, 6), Platform));
        Assert.True(grid.ClearBelow(At(7, 6)));
        Assert.False(grid.ClearBelow(At(23, 6)));
    }

    [Fact]
    public void NothingSolidGoesUnderAPlatformBesideItEverything()
    {
        var grid = World().M.Grid;
        // Right under the run, and at the grid's last row.
        Assert.Equal("grid.underPlatform", grid.PlacementProblem(At(12, 8), Placement.Wall("iron")));
        Assert.Equal("grid.underPlatform", grid.PlacementProblem(At(15, 13), Placement.Wall("iron")));
        Assert.Equal("grid.underPlatform", grid.PlacementProblem(At(12, 8), new Placement("structure", "floor")));
        // A crate whose footprint reaches one column under it.
        Assert.Equal("grid.underPlatform", grid.PlacementProblem(At(9, 9), new Placement("machine", "crate")));
        Assert.Null(grid.PlacementProblem(At(16, 8), Placement.Wall("iron")));
        Assert.Null(grid.PlacementProblem(At(8, 9), new Placement("machine", "crate")));
        // A back wall is drawn behind: the pod falls in front of it.
        Assert.Null(grid.PlacementProblem(At(12, 8), Placement.BackWall("scrap")));
    }

    [Fact]
    public void APlanRefusesAPlatformOverAPlannedWallAndAWallUnderAPlannedPlatform()
    {
        var w = World().Do(new SetCreative(true));
        w.Do(new PlacePlan(At(6, 10), Placement.Wall("iron"))).Run();
        w.Do(new PlacePlan(At(6, 6), Platform)).Run();
        Assert.Equal(["grid.shaftBlocked"], Reasons(w));
        w.Do(new CancelPlan(Assert.Single(w.M.Building.Plans).Id)).Do(new PlacePlan(At(6, 6), Platform)).Run();
        w.Do(new PlacePlan(At(7, 12), Placement.Wall("iron"))).Do(new PlacePlan(At(8, 12), Placement.Wall("iron"))).Run();
        Assert.Equal(["grid.shaftBlocked", "grid.underPlatform"], Reasons(w));
        Assert.Equal(2, w.M.Building.Plans.Count);
    }
}
