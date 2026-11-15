using FsCheck;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Landing;
using Stillwake.Sim.Places;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The crew shuttle's trip (shuttle-flight, slice 2): a course set on the radar, its riders set off, fly, arrive together by the
/// vessel, the shuttle standing <c>Shuttle.Gap</c> cells left of its frame; set off again they fly home and dock. The shuttle never
/// dies with the vessel world.
/// </summary>
public class ShuttleTripTests
{
    /// <summary>
    /// A hangar: a run of twelve platform pieces in its floor for the shuttle, a drop pod on a run of its own beside it, space under
    /// both, each run cabled to the panel in space over it.
    /// </summary>
    private static readonly string[] Rows =
    [
        "PPP                     ",
        "                        ",
        "########################",
        "#......................#",
        "#......................#",
        "#......................#",
        "#......................#",
        "#...............ZZ.....#",
        "#...............ZZ.....#",
        "#...............ZZ.....#",
        "##____________##__######",
        "                        ",
        "                        ",
    ];

    private static readonly ((int, int), (int, int))[] Cables = [((0, 0), (2, 10)), ((0, 0), (16, 10))];
    private const int Wide = 48;
    private static int At(int x, int y) => y * Wide + x;
    /// <summary>Where the shuttle stands: its 24 columns over the run, its bottom row just above it.</summary>
    private static readonly int Home = At(4, 10);
    private static readonly int Run = At(4, 20);
    private static readonly int PodHome = At(32, 14);
    /// <summary>Where heroes come in, on the hangar's floor beside the shuttle.</summary>
    private static readonly int[] Spots = [At(2, 19), At(3, 19), At(28, 19), At(29, 19), At(30, 19)];
    private static readonly Placement Craft = new("machine", "crewShuttle");

    private const int Flight = Content.Shuttle.FlightTicks, Fall = Content.Shuttle.FallTicks;
    private const string Kind = "vessel.shuttle";
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();

    /// <summary>
    /// The hangar, the shuttle on its run, <paramref name="heroes"/> heroes in it, peers 0 up, a small vessel found on the radar; the first
    /// <paramref name="seated"/> of them boarded, and the course set to it unless <paramref name="course"/> is false.
    /// </summary>
    private static Scenario Ready(int heroes = 1, int? seated = null, bool course = true, string[]? rows = null, ((int, int), (int, int))[]? cables = null)
    {
        var w = new Scenario(Layout.Parse(rows ?? Rows, null, cables ?? Cables), doubled: true);
        w.M.Grid.Place(Home, Craft);
        for (int peer = 0; peer < heroes; peer++) w.Do(new Enter(Spots[peer]) { From = peer });
        w.Do(new Find("shuttle")).Run();
        for (int peer = 0; peer < (seated ?? heroes); peer++) w.Do(new Board(Shuttle(w.M)) { From = peer });
        if (course) w.Do(new SetCourse(w.M.Places.Contacts[^1].Id));
        w.Run();
        Assert.Empty(w.Refusals);
        return w;
    }

    /// <summary>The one shuttle standing on <paramref name="m"/>'s surface.</summary>
    private static int Shuttle(Registry m) => Assert.Single(m.Grid.MachinesOf(Craft.Kind)).Id;

    /// <summary>Ready, set off and flown: arrived on the vessel world, surface 1.</summary>
    private static Scenario Arrived(int heroes = 1) => Ready(heroes).Do(new SetOff()).Run(Flight);

    /// <summary>The seats in the one shuttle on <paramref name="m"/>'s surface: who sits at which place, by place.</summary>
    private static (int Peer, int Place)[] Riders(Registry m) => [.. m.Landing.RidersOf(Shuttle(m)).Select(s => (s.Peer, s.Place))];

    /// <summary>Every shuttle there is: standing on an open surface, or flying in a voyage from one.</summary>
    private static int Shuttles(Scenario w) =>
        w.World.Surfaces.Where(s => !s.Closed).Sum(s => w.On(s.Id) is var m ? m.Grid.MachinesOf(Craft.Kind).Count + m.Landing.Voyages.Count : 0);

    // The course.

    [Fact]
    public void ACourseIsSetToAContactTheRadarHoldsAndALaterOneReplacesIt()
    {
        var w = Ready(course: false);
        w.Do(new SetCourse(99)).Run();
        Assert.Equal(["places.noContact"], Reasons(w));
        Assert.Null(w.M.Places.Course);
        w.Do(new Find("freighter")).Run();
        var (first, second) = (w.M.Places.Contacts[0], w.M.Places.Contacts[1]);
        w.Do(new SetCourse(first.Id)).Run();
        Assert.Equal(first.Id, w.M.Places.Course!.Id);
        // The last command of a tick wins.
        w.Do(new SetCourse(first.Id)).Do(new SetCourse(second.Id)).Run();
        Assert.Equal(second.Id, w.M.Places.Course!.Id);
    }

    [Fact]
    public void ACourseIsSetOnlyAboard()
    {
        var w = Ready(2, seated: 1).Do(new SetOff()).Run(Flight);
        w.Do(new Find("shuttle") { From = 1 }).Run();
        w.Do(new SetCourse(w.M.Places.Contacts[^1].Id) { Surface = 1 }).Run();
        Assert.Equal(["places.notAboard"], Reasons(w));
        Assert.Null(w.M.Places.Course);
    }

    [Fact]
    public void ACourseClearsWhenItsContactRunsOutOrIsJumpedTo()
    {
        var w = Ready(seated: 0);
        var slice = (PlacesState)w.M.Places.Slice;
        slice.Contacts[^1] = slice.Contacts[^1] with { Left = 2 };
        w.Run();
        Assert.NotNull(w.M.Places.Course);
        w.Run();
        Assert.Empty(w.M.Places.Contacts);
        Assert.Null(w.M.Places.Course);

        var jumped = Ready(seated: 0);
        jumped.Do(new Jump(jumped.M.Places.Course!.Id)).Run();
        Assert.Null(jumped.M.Places.Course);
    }

    [Property]
    public void CostsTheRoundTripEachLegRoundedUpNeverBelowTheExactCost(int offset)
    {
        var contact = new Contact(1, "shuttle", 0, offset % 10_000, 1);
        int km = Math.Abs(contact.Offset) * Contacts.KmPerUnit, cost = PlacesModule.CostOf(contact), leg = cost / 2;
        // Two equal legs, each the least whole litres that cover its distance.
        Assert.Equal(0, cost % 2);
        Assert.True(leg * Content.Shuttle.KmPerLitre >= km);
        Assert.True((leg - 1) * Content.Shuttle.KmPerLitre < km || leg == 0);
    }

    [Fact]
    public void CostsAContactAtTheRadarsEdgeTwoHundredLitres() =>
        Assert.Equal(200, PlacesModule.CostOf(new Contact(1, "shuttle", 0, -Contacts.Reach, 1)));

    // Setting off.

    [Fact]
    public void SettingOffUsesTheContactClearsTheCourseAndOpensTheRunUntilTheShuttleIsThrough()
    {
        var w = Ready(2);
        int contact = w.M.Places.Course!.Id;
        w.Do(new SetOff { From = 1 }).Run();
        Assert.Empty(w.Refusals);
        Assert.Null(w.M.Places.Course);
        Assert.DoesNotContain(w.M.Places.Contacts, k => k.Id == contact);
        Assert.Empty(w.M.Grid.MachinesOf(Craft.Kind));
        Assert.Empty(w.M.Landing.Seats);
        var voyage = Assert.Single(w.M.Landing.Voyages);
        Assert.Equal((Home, Home, Kind, Flight - 1), (voyage.From, voyage.Home, voyage.Out, voyage.TicksLeft));
        Assert.Equal([0, 1], voyage.Riders);
        Assert.True(w.M.Landing.Rides(0));
        Assert.Equal(Structure.PlatformOpen, w.M.Grid.StructureAt(Run));
        w.Run(Fall - 2);
        Assert.Equal(Structure.PlatformOpen, w.M.Grid.StructureAt(Run));
        w.Run();
        Assert.Equal(Structure.PlatformClosed, w.M.Grid.StructureAt(Run));
    }

    [Fact]
    public void RefusesNoCourseNoSeatAndAPassengerNotAwake()
    {
        var w = Ready(2, seated: 1, course: false);
        w.Do(new SetOff()).Run();
        Assert.Equal(["places.noCourse"], Reasons(w));
        w.Do(new SetCourse(w.M.Places.Contacts[^1].Id)).Run();
        w.Refusals.Clear();
        w.Do(new SetOff { From = 1 }).Run();
        Assert.Equal(["landing.notSeated"], Reasons(w));
        Assert.Equal(Shuttle(w.M), w.M.Landing.SeatOf(0));
        Assert.NotNull(w.M.Places.Course);
        Assert.Empty(w.M.Landing.Voyages);
    }

    [Fact]
    public void RefusesADarkRunAShuttleOffItsRunAndABlockedShaft()
    {
        Assert.Equal("landing.unpowered", Ready(cables: []).M.Places.SetOffProblem(0));
        // The last piece of its run taken from under it: half over the floor, nothing to drop through.
        var w = Ready();
        w.M.Grid.Clear(At(27, 20), "structure");
        Assert.Equal("landing.noPlatform", w.M.Places.SetOffProblem(0));
        // A wall far under its run, from before shafts were kept clear.
        string[] walled = [.. Rows[..12], "     ##                 "];
        var blocked = Ready(rows: walled);
        blocked.Do(new SetOff()).Run();
        Assert.Equal(["landing.shaftBlocked"], Reasons(blocked));
        Assert.Equal(Shuttle(blocked.M), blocked.M.Landing.SeatOf(0));
        Assert.NotNull(blocked.M.Places.Course);
    }

    [Fact]
    public void ASecondSetOffInTheTickOfTheFirstFindsNoCourse()
    {
        var w = Ready(2);
        w.Do(new SetOff()).Do(new SetOff { From = 1 }).Run();
        Assert.Equal(["places.noCourse"], Reasons(w));
        Assert.Single(w.M.Landing.Voyages);
    }

    [Fact]
    public void ARiderFlyingBoardsNothingAndJumpsNowhere()
    {
        var w = Ready(2, seated: 1).Do(new SetOff()).Run();
        w.Do(new Find("shuttle") { From = 1 }).Run();
        w.Do(new Jump(w.M.Places.Contacts[^1].Id)).Run();
        Assert.Equal(["places.seated"], Reasons(w));
    }

    // Arrival.

    [Fact]
    public void FourRidersArriveInOneVesselWorldSeatedTheShuttleStoppedAtTheGap()
    {
        var w = Ready(4).Do(new SetOff { From = 2 }).Run(Flight - 1);
        Assert.Single(w.World.Surfaces);
        Assert.All(Enumerable.Range(0, 4), p => Assert.Equal(0, w.World.SurfaceOf(p)));
        w.Run();
        Assert.Equal([Surface.Ship, Kind], w.World.Surfaces.Select(s => s.Kind));
        Assert.All(Enumerable.Range(0, 4), p => Assert.Equal(1, w.World.SurfaceOf(p)));
        Assert.Empty(w.M.Landing.Voyages);
        var m = w.On(1);
        Assert.Empty(m.Landing.Voyages);
        Assert.Equal([(0, 0), (1, 1), (2, 2), (3, 3)], Riders(m));
        // Its right edge the gap left of the frame, its bottom row on the spawn's.
        int width = m.Grid.Width, spawnRow = m.Crew.Spawn / width;
        var spec = Machines.Get(Craft.Kind);
        Assert.Equal((spawnRow - spec.Height + 1) * width + Vessels.Margin - Content.Shuttle.Gap - spec.Width, m.Grid.MachineById(Shuttle(m))!.Cell);
        Assert.Equal(new Parked(Shuttle(m), Home), Assert.Single(m.Landing.Parked));
        Assert.Equal(1, Shuttles(w));
    }

    [Fact]
    public void ARiderOutMidFlightArrivesOutUnseatedAndWakesAboard()
    {
        var w = Ready(2).Do(new SetOff()).Run(10);
        w.M.Crew.HeroOf(0)!.Out = Flight;
        w.Run(Flight - 10);
        Assert.Equal((1, 1), (w.World.SurfaceOf(0), w.World.SurfaceOf(1)));
        var m = w.On(1);
        Assert.True(m.Crew.HeroOf(0)!.Out > 0);
        Assert.Equal([(1, 0)], Riders(m));
        w.Run(20);
        Assert.Equal((0, 1), (w.World.SurfaceOf(0), w.World.SurfaceOf(1)));
    }

    [Fact]
    public void ARiderPassingOutOnTheArrivalTickArrivesOutUnseated()
    {
        var w = Ready(2).Do(new SetOff()).Run(Flight - 1);
        w.M.Crew.HeroOf(1)!.Out = 100;
        w.Run();
        Assert.Equal((1, 1), (w.World.SurfaceOf(0), w.World.SurfaceOf(1)));
        Assert.Equal([(0, 0)], Riders(w.On(1)));
    }

    [Fact]
    public void EveryRiderGoneBeforeArrivingDocksItHomeEmptyAndOpensNothingTheContactUsed()
    {
        var w = Ready(2).Do(new SetOff()).Run(5).Do(new Leave()).Do(new Leave { From = 1 }).Run();
        Assert.Empty(w.M.Landing.Voyages);
        Assert.Equal(Home, w.M.Grid.MachineById(Shuttle(w.M))!.Cell);
        Assert.Empty(w.M.Landing.Seats);
        Assert.Equal(Structure.PlatformClosed, w.M.Grid.StructureAt(Run));
        Assert.Empty(w.M.Places.Contacts);
        w.Run(Flight);
        Assert.Single(w.World.Surfaces);
    }

    [Fact]
    public void ARiderGoneMidFlightIsNotSentTheOthersArrive()
    {
        var w = Ready(2).Do(new SetOff()).Run(50).Do(new Leave()).Run(Flight - 50);
        Assert.Equal((0, 1), (w.World.SurfaceOf(0), w.World.SurfaceOf(1)));
        Assert.Equal([(1, 0)], Riders(w.On(1)));
    }

    // Home.

    [Fact]
    public void SetsOffHomeFreeDocksSeatedAndTheVesselWorldCloses()
    {
        var w = Arrived(2).Do(new SetOff { From = 1, Surface = 1 }).Run();
        Assert.Empty(w.Refusals);
        var m = w.On(1);
        var voyage = Assert.Single(m.Landing.Voyages);
        Assert.Equal((Home, (string?)null, Flight - 1), (voyage.Home, voyage.Out, voyage.TicksLeft));
        Assert.Empty(m.Landing.Parked);
        Assert.Empty(m.Grid.MachinesOf(Craft.Kind));
        w.Run(Flight - 1);
        Assert.Equal((0, 0), (w.World.SurfaceOf(0), w.World.SurfaceOf(1)));
        Assert.True(w.World.Surfaces[1].Closed);
        Assert.Equal([(0, 0), (1, 1)], Riders(w.M));
        Assert.Equal(Home, w.M.Grid.MachineById(Shuttle(w.M))!.Cell);
        Assert.Equal(Structure.PlatformClosed, w.M.Grid.StructureAt(Run));
    }

    [Fact]
    public void FliesHomeWhileAFriendStaysOnTheVesselWhoseWorldStaysOpen()
    {
        var w = Arrived(2).Do(new Alight { From = 1, Surface = 1 }).Run().Do(new SetOff { Surface = 1 }).Run(Flight);
        Assert.Equal((0, 1), (w.World.SurfaceOf(0), w.World.SurfaceOf(1)));
        Assert.False(w.World.Surfaces[1].Closed);
        Assert.Equal([(0, 0)], Riders(w.M));
        Assert.Empty(w.On(1).Grid.MachinesOf(Craft.Kind));
        Assert.Equal(1, Shuttles(w));
    }

    [Fact]
    public void ARiderQuittingOnTheWayHomeLeavesTheOtherArrivingSeated()
    {
        var w = Arrived(2).Do(new SetOff { Surface = 1 }).Run(50).Do(new Leave { From = 1, Surface = 1 }).Run(Flight - 50);
        Assert.Equal(0, w.World.SurfaceOf(0));
        Assert.Equal([(0, 0)], Riders(w.M));
        Assert.True(w.World.Surfaces[1].Closed);
        Assert.Equal(1, Shuttles(w));
    }

    [Fact]
    public void AHomewardRiderWakingAboardOrBeamedHomeBeforeArrivingLeavesExactlyOneShuttle()
    {
        var woke = Arrived(2).Do(new SetOff { Surface = 1 }).Run(10);
        woke.On(1).Crew.HeroOf(1)!.Out = 1;
        woke.Run(Flight);
        Assert.Equal(1, Shuttles(woke));
        Assert.Equal([(0, 0)], Riders(woke.M));

        var beamed = Arrived(2).Do(new SetOff { Surface = 1 }).Run(10).Do(new Transfer(0)).Run(Flight);
        Assert.Equal(1, Shuttles(beamed));
        Assert.Equal([(1, 0)], Riders(beamed.M));
    }

    [Fact]
    public void RidersAllQuittingOnTheWayHomeDockItHomeAsTheWorldCloses()
    {
        var w = Arrived(2).Do(new SetOff { Surface = 1 }).Run(10).Do(new Leave { Surface = 1 }).Do(new Leave { From = 1, Surface = 1 }).Run();
        Assert.True(w.World.Surfaces[1].Closed);
        Assert.Equal(Home, w.M.Grid.MachineById(Shuttle(w.M))!.Cell);
        Assert.Empty(w.M.Landing.Seats);
    }

    [Fact]
    public void ItsHomeBuiltOverOrItsRunGoneItIsLaidThereAsItsCost()
    {
        var w = Arrived();
        w.M.Grid.Place(Home, new Placement("machine", "crate"));
        w.Do(new SetOff { Surface = 1 }).Run(Flight);
        Assert.Equal(0, w.World.SurfaceOf(0));
        Assert.Empty(w.M.Grid.MachinesOf(Craft.Kind));
        Assert.Null(w.M.Landing.SeatOf(0));
        var laid = w.M.Items.Floor.GroupBy(s => s.Item).ToDictionary(g => g.Key, g => g.Sum(s => s.Count));
        Assert.All(Machines.Get(Craft.Kind).Cost, c => Assert.Equal(c.Count, laid[c.Item]));

        var gone = Arrived();
        for (int x = 4; x < 28; x++) gone.M.Grid.Clear(At(x, 20), "structure");
        gone.Do(new SetOff { Surface = 1 }).Run(Flight);
        Assert.Empty(gone.M.Grid.MachinesOf(Craft.Kind));
        Assert.Equal(Machines.Get(Craft.Kind).Cost.Sum(c => c.Count), gone.M.Items.Floor.Sum(s => s.Count));
    }

    // Rule 7: the shuttle never dies with the vessel.

    [Fact]
    public void TheLastHeroOffTheVesselByLeaveDocksItHomeEmpty()
    {
        var w = Arrived(2).Do(new Leave { Surface = 1 }).Run();
        Assert.False(w.World.Surfaces[1].Closed);
        w.Do(new Leave { From = 1, Surface = 1 }).Run();
        Assert.True(w.World.Surfaces[1].Closed);
        Assert.Equal(Home, w.M.Grid.MachineById(Shuttle(w.M))!.Cell);
        Assert.Empty(w.M.Landing.Seats);
        Assert.Equal(1, Shuttles(w));
    }

    [Fact]
    public void TheLastHeroOffTheVesselByDyingDocksItHomeEmpty()
    {
        var w = Arrived();
        w.On(1).Crew.HeroOf(0)!.Out = 1;
        w.Run();
        Assert.Equal(0, w.World.SurfaceOf(0));
        Assert.True(w.World.Surfaces[1].Closed);
        Assert.Equal(Home, w.M.Grid.MachineById(Shuttle(w.M))!.Cell);
        Assert.Null(w.M.Landing.SeatOf(0));
    }

    [Fact]
    public void ALoadReleasingEveryHeroDocksItHomeEmpty()
    {
        // The host lets every hero go after a load (Main.Sessions), each farewell bringing it aboard.
        var w = Arrived(2).Reload().Do(new Leave()).Do(new Leave { From = 1 }).Run();
        Assert.True(w.World.Surfaces[1].Closed);
        Assert.Equal(Home, w.M.Grid.MachineById(Shuttle(w.M))!.Cell);
    }

    // Saves.

    [Fact]
    public void PlaysOnAlikeSavedAndLoadedMidFlightOutAndHome()
    {
        static Scenario Played(bool reload)
        {
            var w = Ready(2).Do(new SetOff()).Run(Flight / 2);
            if (reload) w.Reload();
            w.Run(Flight / 2).Do(new SetOff { Surface = 1 }).Run(Flight / 2);
            if (reload) w.Reload();
            return w.Run(Flight / 2 + 1);
        }
        var (played, loaded) = (Played(false), Played(true));
        Assert.Equal([(0, 0), (1, 1)], Riders(played.M));
        Assert.Equal(played.World.Save(), loaded.World.Save());
    }

    // Core and Grid.

    [Fact]
    public void ADescentAndAVoyageEndingOnOneTickOpenTheirSurfacesInTheOrderAsked()
    {
        var w = Ready(2, seated: 1);
        w.GiveItems(1, ("biofuel", 5)).Run();
        int slot = w.M.Items.Inventory(1).ToList().FindIndex(s => s?.Item == "biofuel");
        int pod = w.M.Grid.MachineAt(PodHome)!.Id;
        w.Do(new Move(new Stillwake.Sim.Items.Place(slot), new Target(null, pod), 5) { From = 1 }).Do(new Board(pod) { From = 1 }).Run();
        w.Do(new SetOff()).Run(Flight - Content.Landing.DescentTicks);
        w.Do(new Launch { From = 1 }).Run(Content.Landing.DescentTicks);
        Assert.Empty(w.Refusals);
        // Flights count before voyages: the planet asked first takes the first id.
        Assert.Equal([Surface.Ship, Content.Landing.Planet, Kind], w.World.Surfaces.Select(s => s.Kind));
        Assert.Equal((2, 1), (w.World.SurfaceOf(0), w.World.SurfaceOf(1)));
        Assert.Equal([(0, 0)], Riders(w.On(2)));
    }

    [Fact]
    public void AShuttleStandsInAVesselWorldsOpenSpaceAndAPodStillNeedsAPlanetsGround()
    {
        var w = Arrived();
        var m = w.On(1);
        Assert.Null(m.Grid.PlacementProblem(Vessels.Margin / 2 * m.Grid.Width + 4, Craft));
        Assert.Null(m.Grid.PlacementProblem(Vessels.Margin / 2 * m.Grid.Width + 4, new Placement("machine", "dropPod")));
        w.Do(new OpenSurface(PlanetTypes.Small.Id)).Run();
        var planet = w.On(2).Grid;
        // Along the build zone's top row, in the sky: nowhere it stands but on the ground.
        var top = Enumerable.Range(planet.Zone.Left, planet.Zone.Right - planet.Zone.Left - 23).Select(x => planet.PlacementProblem(planet.Zone.Top * planet.Width + x, Craft)).ToList();
        Assert.Contains("grid.noGround", top);
        Assert.DoesNotContain(null, top);
    }

    // The invariant.

    /// <summary>
    /// Whatever up to five heroes do — come in, board, alight, set courses, set off, leave, pass out, wait — the one shuttle built stands
    /// on one open surface or flies in one voyage, no machine seats more than its riders, and a voyage's rider sits nowhere. A twin fed
    /// the same commands, one of them saved and loaded on the way, hashes alike throughout.
    /// </summary>
    [Property(MaxTest = 25)]
    public void TheOneShuttleIsAlwaysSomewhereAndItsRidersNowhereElse(PositiveInt seed, PositiveInt heroes)
    {
        int peers = 1 + heroes.Get % 5;
        var (w, twin) = (Ready(peers, seated: 0, course: false), Ready(peers, seated: 0, course: false));
        var rng = new Rng(new RngState { S = (uint)seed.Get });
        int reload = (int)rng.Below(40);
        for (int step = 0; step < 40; step++)
        {
            int peer = (int)rng.Below((uint)peers);
            int at = w.World.SurfaceOf(peer);
            var m = w.On(at);
            var shuttles = m.Grid.MachinesOf(Craft.Kind);
            Command? c = rng.Below(9) switch
            {
                0 => new Enter(Spots[peer]) { From = peer },
                1 or 2 => new Board(shuttles.Count > 0 ? shuttles[0].Id : 1) { From = peer },
                3 => new Alight { From = peer },
                4 => w.M.Places.Contacts.Count > 0 ? new SetCourse(w.M.Places.Contacts[^1].Id) { From = peer } : new Find("shuttle") { From = peer },
                5 => new SetOff { From = peer },
                6 => new Leave { From = peer },
                _ => null,
            };
            int ticks = rng.Below(3) == 0 ? (int)rng.Below(Flight + 20) : 1;
            if (c is Command command)
            {
                w.Do(command);
                twin.Do(command);
            }
            if (rng.Below(12) == 0 && 1 + (int)rng.Below(40) is int faint)
                foreach (var s in (Scenario[])[w, twin])
                    if (s.On(s.World.SurfaceOf(peer)).Crew.HeroOf(peer) is { } hero) hero.Out = faint;
            for (int t = 0; t < ticks; t++)
            {
                w.Run();
                twin.Run();
                Holds(w, peers);
            }
            if (step == reload) w.Reload();
            Assert.Equal(twin.World.Hash(), w.World.Hash());
        }
    }

    private static void Holds(Scenario w, int peers)
    {
        Assert.Equal(1, Shuttles(w));
        foreach (var s in w.World.Surfaces.Where(x => !x.Closed))
        {
            var m = w.On(s.Id);
            foreach (var group in m.Landing.Seats.GroupBy(x => x.Pod))
                Assert.True(group.Count() <= Machines.Get(m.Grid.MachineById(group.Key)!.Kind).Riders);
            for (int peer = 0; peer < peers; peer++)
                if (m.Landing.VoyageOf(peer) != null)
                    Assert.All(w.World.Surfaces.Where(x => !x.Closed), o => Assert.Null(w.On(o.Id).Landing.SeatOf(peer)));
        }
    }
}
