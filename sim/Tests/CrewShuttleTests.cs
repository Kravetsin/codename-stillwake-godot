using System.Text.Json.Nodes;
using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Landing;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The crew shuttle: a machine 24 cells by 10 standing only on a launch platform, the whole run under its bottom row, as the drop
/// pod does; four heroes board it, each at a place of their own, the lowest free one. It flies nowhere yet.
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
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();

    /// <summary>The hangar with a shuttle standing on its run and <paramref name="heroes"/> heroes in it, peers 0 up; and the shuttle's id.</summary>
    private static (Scenario W, int Shuttle) Stood(int heroes)
    {
        var w = World(Hangar);
        w.M.Grid.Place(At(4, 4), Shuttle);
        for (int peer = 0; peer < heroes; peer++) w.Do(new Enter(At(2 + peer, 13)) { From = peer });
        return (w.Run(), w.M.Grid.MachineAt(At(4, 4))!.Id);
    }

    /// <summary>The seats taken in <paramref name="shuttle"/>: who sits at which place, by place.</summary>
    private static (int Peer, int Place)[] Riders(Scenario w, int shuttle) => [.. w.M.Landing.RidersOf(shuttle).Select(s => (s.Peer, s.Place))];

    [Fact]
    public void IsTwentyFourCellsByTenOnTheShipTabStandingOnAPlatformSeatingFour()
    {
        var spec = Machines.Get("crewShuttle");
        Assert.Equal((24, 10), (spec.Width, spec.Height));
        Assert.Equal(MachineTrait.Seats | MachineTrait.Lands, spec.Traits);
        Assert.Equal(4, spec.Riders);
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
    public void SeatsFourEachAtTheLowestFreePlaceAndRefusesAFifth()
    {
        var (w, shuttle) = Stood(5);
        for (int peer = 0; peer < 4; peer++) w.Do(new Board(shuttle) { From = peer });
        w.Run();
        Assert.Empty(w.Refusals);
        Assert.Equal([(0, 0), (1, 1), (2, 2), (3, 3)], Riders(w, shuttle));
        Assert.Equal(2, w.M.Landing.PlaceOf(2));
        Assert.Equal(shuttle, w.M.Landing.SeatOf(3));
        w.Do(new Board(shuttle) { From = 4 }).Run();
        Assert.Equal(["landing.taken"], Reasons(w));
        Assert.Null(w.M.Landing.SeatOf(4));
        Assert.Null(w.M.Landing.PlaceOf(4));
    }

    [Fact]
    public void ThePlaceLeftIsTheNextBoardersAndTheSeatsKeepTheirOrder()
    {
        var (w, shuttle) = Stood(4);
        for (int peer = 0; peer < 3; peer++) w.Do(new Board(shuttle) { From = peer });
        w.Run().Do(new Alight { From = 1 }).Run();
        Assert.Equal([(0, 0), (2, 2)], Riders(w, shuttle));
        w.Do(new Board(shuttle) { From = 3 }).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal([(0, 0), (3, 1), (2, 2)], Riders(w, shuttle));
        Assert.Equal(w.M.Landing.RidersOf(shuttle), w.M.Landing.Seats);
    }

    [Fact]
    public void IsNeverLaunchedAsAPodIs()
    {
        var (w, shuttle) = Stood(1);
        w.Do(new Board(shuttle)).Run().Do(new Launch()).Run();
        Assert.Equal(["landing.notSeated"], Reasons(w));
        Assert.Equal(shuttle, w.M.Landing.SeatOf(0));
        Assert.Empty(w.M.Landing.Flights);
        Assert.NotNull(w.M.Grid.MachineById(shuttle));
    }

    [Fact]
    public void FreesTheSeatOfAHeroThatPassesOutAndOnlyItsPlace()
    {
        var (w, shuttle) = Stood(3);
        for (int peer = 0; peer < 3; peer++) w.Do(new Board(shuttle) { From = peer });
        w.Run();
        w.M.Crew.HeroOf(1)!.Out = 100;
        w.Run();
        Assert.Equal([(0, 0), (2, 2)], Riders(w, shuttle));
        Assert.Null(w.M.Landing.SeatOf(1));
    }

    [Fact]
    public void SeatsEachPlaceOnItsBottomRowInTheMiddleOfItsShareThePodsInItsMiddle()
    {
        // Its 24 columns in four shares of six: the middle of each, rounded down, from its left at column 4.
        Assert.Equal([At(6, 13), At(12, 13), At(18, 13), At(24, 13)],
            Enumerable.Range(0, 4).Select(place => LandingModule.FloorOf("crewShuttle", At(4, 4), Wide, place)));
        // A pod four wide and a wheel six wide, a place each: the middle of the bottom row, as before places.
        Assert.Equal(At(11, 9), LandingModule.FloorOf(At(10, 4), Wide));
        Assert.Equal(At(12, 9), LandingModule.FloorOf("treadWheel", At(10, 4), Wide));
    }

    /// <summary>Two drop pods, each on a run of its own.</summary>
    private static readonly string[] TwoPods =
    [
        "##############",
        "#............#",
        "#..ZZ...ZZ...#",
        "#..ZZ...ZZ...#",
        "#..ZZ...ZZ...#",
        "###__###__####",
        "              ",
    ];

    [Fact]
    public void ASaveFromBeforePlacesLoadsInItsSeatOrderAndPlaysOnAlike()
    {
        static Scenario Seated()
        {
            var w = new Scenario(Layout.Parse(TwoPods), doubled: true);
            w.Do(new Enter(w.At(12, 9))).Do(new Enter(w.At(13, 9)) { From = 1 }).Run();
            var pods = w.M.Grid.MachinesWith(MachineTrait.Seats);
            // Peer 0 in the later pod, peer 1 in the first: the seats by pod, not by peer.
            return w.Do(new Board(pods[1].Id)).Do(new Board(pods[0].Id) { From = 1 }).Run();
        }
        var (played, older) = (Seated(), Seated());
        var root = JsonNode.Parse(older.World.Save())!;
        foreach (var seat in root["slices"]!["landing"]!["seats"]!.AsArray()) Assert.True(seat!.AsObject().Remove("place"));
        older.Reload(root.ToJsonString());
        Assert.Equal(played.M.Landing.Seats, older.M.Landing.Seats);
        Assert.Equal([1, 0], older.M.Landing.Seats.Select(s => s.Peer));
        foreach (var w in (Scenario[])[played, older]) w.Do(new Alight()).Run().Do(new Board(w.M.Grid.MachinesWith(MachineTrait.Seats)[1].Id)).Run(5);
        Assert.Equal(played.World.Save(), older.World.Save());
    }
}
