using Stillwake.Content;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Xunit;
using Move = Stillwake.Sim.Items.Move;
using Pairs = System.Collections.Generic.IReadOnlyList<((int X, int Y) A, (int X, int Y) B)>;
using Slot = Stillwake.Sim.Items.Place;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The engine (2026-10-25-the-engine-through-the-hull): built only with its nozzle out through the stern's wall into space,
/// its one slot taking biofuel alone, drawing power once cabled, and never running yet.
/// </summary>
public class EngineTests
{
    private static readonly Placement Engine = new("machine", "engine");

    /// <summary>A room with a hull wall a character thick, two cells, on its left, space past it, and space past its right wall.</summary>
    private static readonly string[] Room =
    [
        "                ",
        " ########       ",
        " #......#       ",
        " #......#       ",
        " ########       ",
    ];

    private static string? Problem(Scenario w, int cell) => w.M.Grid.PlacementProblem(cell, Engine);

    [Fact]
    public void StandsAgainstTheSternsWallItsNozzleThroughItIntoSpace()
    {
        var w = new Scenario(Layout.Parse(Room), doubled: true);
        Assert.Null(Problem(w, w.At(2, 2)));
        Assert.Null(w.M.Building.Problem(w.At(2, 2), Engine));
    }

    [Fact]
    public void IsRefusedAwayFromTheWallOrWithARoomPastItRatherThanSpace()
    {
        var w = new Scenario(Layout.Parse(Room), doubled: true);
        // A cell off the wall, its nozzle in the room.
        Assert.Equal("grid.noExhaust", Problem(w, w.At(2, 2) + 1));
        Assert.Equal("grid.noExhaust", w.M.Building.Problem(w.At(2, 2) + 1, Engine));
        // Outside against the right wall: the room past it, not space.
        Assert.Equal("grid.noExhaust", Problem(w, w.At(9, 2)));
        // Out in space, no wall at all.
        Assert.Equal("grid.noExhaust", Problem(w, w.At(12, 2)));
    }

    [Fact]
    public void ReachesThroughAWallOfOneCellOrTwoButNotOneThickerThanItsNozzle()
    {
        string[] thin = ["            ", "   ########", "   #......#", "   #......#", "   #......#", "   #......#", "   ########"];
        var one = new Scenario(Layout.Parse(thin));
        Assert.Null(one.M.Grid.PlacementProblem(one.At(4, 2), Engine));
        string[] thick = ["            ", " #########  ", " ##......#  ", " ##......#  ", " #########  "];
        var two = new Scenario(Layout.Parse(thick), doubled: true);
        Assert.Equal(Machines.Get("engine").Exhaust, 3);
        Assert.Equal("grid.noExhaust", Problem(two, two.At(3, 2)));
    }

    [Fact]
    public void IsRefusedWhereAMachineStandsInTheSpaceItsNozzleReaches()
    {
        string[] rows = ["           ", "   ########", "PPP#......#", "   #......#", "   ########"];
        var w = new Scenario(Layout.Parse(rows), doubled: true);
        Assert.Equal("grid.noExhaust", Problem(w, w.At(4, 2)));
    }

    /// <summary>The engine built against the stern's wall, a panel out in space cabled to it.</summary>
    private static readonly string[] Built =
    [
        "PPP         ",
        " ########   ",
        " #XXX...#   ",
        " #XXX...#   ",
        " ########   ",
    ];

    private static readonly Pairs Cable = [((0, 0), (2, 2))];

    private static Scenario World(bool cabled = true) => new(Layout.Parse(Built, null, cabled ? Cable : null), doubled: true);

    private static int Id(Scenario w) => w.M.Grid.MachineAt(w.At(2, 2))!.Id;

    private static Scenario Move(Scenario w, Slot src, Target dst, int count) => w.Do(new Move(src, dst, count) { From = 0 }).Run();

    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();

    [Fact]
    public void ItsSlotTakesBiofuelAlonePutInSwappedInOrDelivered()
    {
        var w = World(cabled: false).GiveItems(0, ("biofuel", 5), ("planks", 5));
        int engine = Id(w);
        Move(w, new Slot(1), new Target(0, engine), 5);
        Move(w, new Slot(1), new Target(Crate: engine), 5);
        Assert.Equal(["items.notTaken", "items.notTaken"], Reasons(w));
        Move(w, new Slot(0), new Target(0, engine), 5);
        Assert.Equal(new Stack("biofuel", 5), w.M.Items.Crate(engine)[0]);
        // Planks onto the biofuel there would swap them in.
        Move(w, new Slot(1), new Target(0, engine), 5);
        Assert.Equal(["items.notTaken", "items.notTaken", "items.notTaken"], Reasons(w));
        Assert.Equal(new Stack("biofuel", 5), w.M.Items.Crate(engine)[0]);
        // A drone brings it biofuel to a full stack, and nothing else.
        int stack = Content.Items.Get("biofuel").Stack;
        Assert.Equal(stack - 5, w.M.Items.RoomFor(engine, new Stack("biofuel", stack)));
        Assert.Equal(0, w.M.Items.RoomFor(engine, new Stack("planks", 5)));
        Assert.Equal(0, w.M.Items.HeldFor(engine, "planks"));
    }

    /// <summary>Its pumps and igniter kept ready while its tank holds fuel, idle while empty.</summary>
    [Fact]
    public void DrawsItsPowerCabledWhileItHoldsFuelAndNothingWithout()
    {
        var w = World().Run(2);
        Assert.Equal(0, w.M.Power.NetworkOf(w.At(2, 2))!.Demand);
        Move(w.GiveItems(0, ("biofuel", 1)), new Slot(0), new Target(0, Id(w)), 1).Run();
        Assert.Equal(1000, w.M.Power.Satisfaction(Id(w)));
        Assert.Equal(Machines.Get("engine").Draws, w.M.Power.NetworkOf(w.At(2, 2))!.Demand);
        var cut = World(cabled: false).GiveItems(0, ("biofuel", 1));
        Move(cut, new Slot(0), new Target(0, Id(cut)), 1).Run();
        Assert.Equal(0, cut.M.Power.Satisfaction(Id(cut)));
    }

    [Fact]
    public void NeverBurnsTheBiofuelItHoldsForItDoesNotRunYet()
    {
        var w = World().GiveItems(0, ("biofuel", 5));
        Move(w, new Slot(0), new Target(0, Id(w)), 5);
        w.Run(4 * Content.Items.Get("biofuel").BurnTicks!.Value);
        Assert.Equal(new Stack("biofuel", 5), w.M.Items.Crate(Id(w))[0]);
        Assert.False(Machines.Get("engine").Is(MachineTrait.Fire));
    }
}
