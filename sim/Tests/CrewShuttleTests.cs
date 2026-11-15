using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Landing;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The crew shuttle's first slice: a machine 24 cells by 10 standing only on a launch platform, the whole run under its bottom row,
/// as the drop pod does. Only built: nobody boards it yet.
/// </summary>
public class CrewShuttleTests
{
    /// <summary>A hangar six characters tall over a run of twelve platform pieces in its floor, space under it.</summary>
    private static readonly string[] Hangar =
    [
        "################",
        "#..............#",
        "#..............#",
        "#..............#",
        "#..............#",
        "#..............#",
        "#..............#",
        "##____________##",
        "                ",
    ];

    private const int Wide = 32;
    private static int At(int x, int y) => y * Wide + x;
    private static readonly Placement Shuttle = new("machine", "crewShuttle");
    private static Scenario World(string[] rows) => new(Layout.Parse(rows), doubled: true);

    [Fact]
    public void IsTwentyFourCellsByTenOnTheShipTabStandingOnAPlatform()
    {
        var spec = Machines.Get("crewShuttle");
        Assert.Equal((24, 10), (spec.Width, spec.Height));
        Assert.Equal(MachineTrait.Lands | MachineTrait.HoldsFuel, spec.Traits);
        // Its 200 l tank, filled by cans in its hopper or by a pipe into its stern (fuel-liquid).
        Assert.Equal((200, (0, Content.Fuel.Item)), (spec.FuelHolds, spec.SlotTakes));
        Assert.Equal(MenuTabs.Ship, spec.Menu!.Tab);
    }

    [Fact]
    public void IsPlannedOnlyWithTheRunUnderAllOfItsBottomRow()
    {
        var w = World(Hangar);
        // The run is cells 4 to 27 of row 14: the shuttle's 24 columns over it, its bottom row just above.
        Assert.Null(w.M.Grid.PlacementProblem(At(4, 4), Shuttle));
        // A cell off the run either way, and a row too high.
        Assert.Equal("grid.noPlatform", w.M.Grid.PlacementProblem(At(3, 4), Shuttle));
        Assert.Equal("grid.noPlatform", w.M.Grid.PlacementProblem(At(5, 4), Shuttle));
        Assert.Equal("grid.noPlatform", w.M.Grid.PlacementProblem(At(4, 3), Shuttle));
        w.Do(new PlacePlan(At(3, 4), Shuttle)).Do(new PlacePlan(At(4, 4), Shuttle)).Run();
        Assert.Equal(["grid.noPlatform"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(At(4, 4), Assert.Single(w.M.Building.Plans).Cell);
    }

    [Fact]
    public void NeedsARunAsLongAsItIs()
    {
        // Eleven pieces, a piece short: no cell between the hangar's walls (cells 2 to 29) takes it.
        var w = World([.. Hangar[..7], "##___________###", Hangar[8]]);
        for (int x = 2; x + 24 <= 30; x++)
            Assert.Equal("grid.noPlatform", w.M.Grid.PlacementProblem(At(x, 4), Shuttle));
    }

    [Fact]
    public void IsNoSeatYet()
    {
        var w = World(Hangar);
        w.M.Grid.Place(At(4, 4), Shuttle);
        int shuttle = w.M.Grid.MachineAt(At(4, 4))!.Id;
        Assert.Equal("landing.noPod", w.M.Landing.BoardProblem(1, shuttle));
    }
}
