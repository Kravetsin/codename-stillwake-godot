using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Landing;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The drop pod's descent and return (the descent design, slice 1): a launch burns its fuel and opens its run, the pod lands on the
/// planet by the spawn and is its rider's alone to fly back, and it goes wherever its hero goes, docking home empty when its hero
/// leaves, passes out or is brought aboard.
/// </summary>
public class DescentTests
{
    /// <summary>A room with two pods on two runs in its floor, space under them, each run cabled to the panel in space over it.</summary>
    private static readonly string[] Rows =
    [
        "PPP             ",
        "                ",
        "################",
        "#..............#",
        "#..ZZ....ZZ....#",
        "#..ZZ....ZZ....#",
        "#..ZZ....ZZ....#",
        "###__####__#####",
        "                ",
        "                ",
    ];

    private static readonly ((int, int), (int, int))[] Cables = [((0, 0), (3, 7)), ((0, 0), (9, 7))];
    private const int Wide = 32;
    private static int Char(int x, int y) => 2 * y * Wide + 2 * x;
    /// <summary>The cells heroes stand on, by the room's floor, between the pods.</summary>
    private static readonly int Floor = Char(6, 6) + Wide;
    private static readonly int LeftHome = Char(3, 4), RightHome = Char(9, 4);
    private static int Under(int home) => home + 6 * Wide;

    private const int Fall = Content.Landing.FallTicks, Down = Content.Landing.DescentTicks, Up = Content.Landing.AscentTicks;
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();

    /// <summary>
    /// The room, heroes 0 and 1 in it, <paramref name="fuel"/> biofuel in each pod, hero 0 seated in the left one; the small planet
    /// open unless <paramref name="planet"/> is false, so a descent lands there without growing the first.
    /// </summary>
    private static Scenario Ready(int fuel = 7, bool planet = true, ((int, int), (int, int))[]? cables = null, string[]? rows = null)
    {
        var w = new Scenario(Layout.Parse(rows ?? Rows, null, cables ?? Cables), doubled: true);
        w.Do(new Enter(Floor)).Do(new Enter(Floor + 1) { From = 1 }).GiveItems(0, ("biofuel", fuel)).GiveItems(1, ("biofuel", fuel));
        if (planet) w.Do(new OpenSurface(PlanetTypes.Small.Id));
        w.Run();
        foreach (int peer in new[] { 0, 1 })
        {
            int slot = w.M.Items.Inventory(peer).ToList().FindIndex(s => s?.Item == "biofuel");
            w.Do(new Move(new Stillwake.Sim.Items.Place(slot), new Target(null, PodAt(w, peer == 0 ? LeftHome : RightHome)), fuel) { From = peer });
        }
        return w.Do(new Board(PodAt(w, LeftHome))).Run();
    }

    private static int PodAt(Scenario w, int cell) => w.M.Grid.MachineAt(cell)!.Id;
    private static int PodAt(Registry m, int cell) => m.Grid.MachineAt(cell)!.Id;

    /// <summary>The pod standing on surface <paramref name="surface"/> off the ship, by id order.</summary>
    private static Machine Landed(Scenario w, int surface, int nth = 0) => w.On(surface).Grid.MachinesWith(MachineTrait.Seats)[nth];

    private static int Biofuel(Registry m, int pod) => m.Items.HeldFor(pod, "biofuel");

    [Fact]
    public void BurnsFiveOpensTheRunAndShutsItOnceThePodIsThrough()
    {
        var w = Ready();
        int pod = PodAt(w, LeftHome);
        w.Do(new Launch()).Run();
        Assert.Empty(w.Refusals);
        Assert.Null(w.M.Grid.MachineById(pod));
        Assert.Null(w.M.Landing.SeatOf(0));
        var flight = w.M.Landing.FlightOf(0)!;
        Assert.Equal((false, LeftHome, LandingModule.FloorOf(LeftHome, Wide), new Stack("biofuel", 2)), (flight.Up, flight.Home, flight.Floor, flight.Load));
        Assert.Equal(Structure.PlatformOpen, w.M.Grid.StructureAt(Under(LeftHome)));
        w.Run(Fall - 2);
        Assert.Equal(Structure.PlatformOpen, w.M.Grid.StructureAt(Under(LeftHome)));
        w.Run();
        Assert.Equal(Structure.PlatformClosed, w.M.Grid.StructureAt(Under(LeftHome)));
    }

    [Fact]
    public void LandsByThePlanetsSpawnSeatedItsLoadAboardAndItsRiderItsOwner()
    {
        var w = Ready().Do(new Launch()).Run(Down - 1);
        // The launch tick counts: it lands on the last of its ticks.
        Assert.Equal(0, w.World.SurfaceOf(0));
        w.Run();
        Assert.Equal(1, w.World.SurfaceOf(0));
        var m = w.On(1);
        var pod = Landed(w, 1);
        Assert.Equal(pod.Id, m.Landing.SeatOf(0));
        Assert.Equal(0, m.Landing.OwnerOf(pod.Id));
        Assert.Equal(2, Biofuel(m, pod.Id));
        Assert.Empty(m.Landing.Flights);
        Assert.Empty(w.M.Landing.Flights);
        // On level ground a few columns from the spawn, every column under it solid.
        int width = m.Grid.Width, bottom = pod.Cell / width + 6;
        Assert.All(Enumerable.Range(0, 4), dx => Assert.True(m.Grid.Solid(bottom * width + pod.Cell % width + dx)));
        Assert.InRange(pod.Cell % width - m.Crew.Spawn % width, -8, 8);
    }

    [Fact]
    public void FliesBackOnlyInItsOwnPodFreeAndDocksAtHomeSeated()
    {
        var w = Ready().Do(new Launch()).Run(Down + 1);
        int pod = Landed(w, 1).Id;
        // The hero gets out, and its friend, beamed down, may not take its pod.
        w.Do(new Alight()).Do(new Transfer(1) { From = 1 }).Run();
        w.Do(new Board(pod) { From = 1, Surface = 1 }).Run();
        Assert.Equal(["landing.notYours"], Reasons(w));
        w.Do(new Board(pod) { Surface = 1 }).Run().Do(new Launch { Surface = 1 }).Run();
        Assert.Equal(["landing.notYours"], Reasons(w));
        Assert.Empty(w.On(1).Grid.MachinesWith(MachineTrait.Seats));
        Assert.True(w.On(1).Landing.FlightOf(0)!.Up);
        w.Run(Up);
        Assert.Equal(0, w.World.SurfaceOf(0));
        int home = PodAt(w, LeftHome);
        Assert.Equal(home, w.M.Landing.SeatOf(0));
        Assert.Null(w.M.Landing.OwnerOf(home));
        // The return burns nothing: the two left after the descent are still in it.
        Assert.Equal(2, Biofuel(w.M, home));
        Assert.Equal(Structure.PlatformClosed, w.M.Grid.StructureAt(Under(LeftHome)));
    }

    [Fact]
    public void LandsASecondPodBesideTheFirst()
    {
        var w = Ready().Do(new Launch()).Run(Down + 1);
        w.Do(new Board(PodAt(w, RightHome)) { From = 1 }).Run().Do(new Launch { From = 1 }).Run(Down + 1);
        Assert.Equal(1, w.World.SurfaceOf(1));
        var (first, second) = (Landed(w, 1, 0), Landed(w, 1, 1));
        Assert.Equal(second.Id, w.On(1).Landing.SeatOf(1));
        // The first stands centred on the spawn's column, the second flush beside it: to its right, as the ground slopes on its left.
        int width = w.On(1).Grid.Width, spawn = w.On(1).Crew.Spawn % width;
        Assert.Equal(spawn - 1, first.Cell % width);
        Assert.Equal(first.Cell % width + 4, second.Cell % width);
    }

    [Fact]
    public void RefusesNoSeatAShortSlotADarkRunAndAPodOffItsRun()
    {
        var w = Ready(fuel: 4);
        w.Do(new Launch { From = 1 }).Do(new Launch()).Run();
        Assert.Equal(["landing.notSeated", "landing.noFuel"], Reasons(w));
        Assert.Equal(PodAt(w, LeftHome), w.M.Landing.SeatOf(0));
        Assert.Equal("landing.unpowered", Ready(cables: []).M.Landing.LaunchProblem(0));
        // A pod drawn over the floor beside a run: nothing to drop through.
        string[] off = [.. Rows[..7], "#####__##__#####", .. Rows[8..]];
        Assert.Equal("landing.noPlatform", Ready(rows: off).M.Landing.LaunchProblem(0));
        // A wall far under its run, from before shafts were kept clear: it would fall through it.
        string[] walled = [.. Rows[..9], "   ##           "];
        Assert.Equal("landing.shaftBlocked", Ready(rows: walled).M.Landing.LaunchProblem(0));
    }

    /// <summary>A world whose ship cannot open surfaces, as the generic suites build one; or, <c>Here</c> 1, a surface off the ship.</summary>
    private sealed class Fixed(int here, bool canOpen) : ISurfaces
    {
        public int Here => here;
        public Content.Surfaces.Kind Kind => Content.Surfaces.Ship;
        public void Send(int peer, int surface) => throw new NotSupportedException();
        public bool CanOpen => canOpen;
        public bool Landed => false;
        public int OpenPlanet() => throw new NotSupportedException();
    }

    [Fact]
    public void RefusesAWorldThatCannotLeaveTheShipAndOffTheShipAPodNobodyCameDownIn()
    {
        var w = Ready();
        w.M.Landing.Where = new Fixed(0, canOpen: false);
        Assert.Equal("landing.cannotLeave", w.M.Landing.LaunchProblem(0));
        // A pod on the planet nobody came down in, put there as the dev panel would: boarded by anyone, flown by none.
        var down = Ready().Do(new Launch()).Run(Down + 1).Do(new Transfer(1) { From = 1 }).Run();
        var m = down.On(1);
        int beside = Enumerable.Range(4, 40).SelectMany(d => new[] { Landed(down, 1).Cell - d, Landed(down, 1).Cell + d })
            .First(c => m.Grid.PlacementProblem(c, new Placement("machine", "dropPod")) == null);
        m.Grid.Place(beside, new Placement("machine", "dropPod"));
        down.Do(new Board(m.Grid.MachineAt(beside)!.Id) { From = 1, Surface = 1 }).Run().Do(new Launch { From = 1, Surface = 1 }).Run();
        Assert.Equal(["landing.notYours"], Reasons(down));
        Assert.Equal(m.Grid.MachineAt(beside)!.Id, m.Landing.SeatOf(1));
    }

    [Fact]
    public void ALaunchTakesThePodFromUnderAFriendBoardingItOnItsTickAndASecondLaunchFindsNoSeat()
    {
        var w = Ready();
        int pod = PodAt(w, LeftHome);
        w.Do(new Launch()).Do(new Launch()).Do(new Board(pod) { From = 1 }).Run();
        Assert.Equal(["landing.notSeated", "landing.noPod"], Reasons(w));
        Assert.Null(w.M.Landing.SeatOf(1));
    }

    [Fact]
    public void ShutsARunOpenedByHandAsWell()
    {
        var w = Ready().Do(new SetPlatform(Under(LeftHome), true)).Run();
        w.Do(new Launch()).Run(Fall);
        Assert.Equal(Structure.PlatformClosed, w.M.Grid.StructureAt(Under(LeftHome)));
    }

    [Fact]
    public void DocksHomeEmptyWhenItsHeroLeavesMidFallTheRunShut()
    {
        var w = Ready().Do(new Launch()).Run(5).Do(new Leave()).Run();
        Assert.Empty(w.M.Landing.Flights);
        int home = PodAt(w, LeftHome);
        Assert.Null(w.M.Landing.RiderOf(home));
        Assert.Equal(2, Biofuel(w.M, home));
        Assert.Equal(Structure.PlatformClosed, w.M.Grid.StructureAt(Under(LeftHome)));
        Assert.Equal(2, w.World.Surfaces.Count);
    }

    [Fact]
    public void DocksHomeEmptyAndOpensNoPlanetWhenItsHeroIsOutOnItsLastTick()
    {
        var w = Ready(planet: false).Do(new Launch()).Run(Down - 1);
        w.M.Crew.HeroOf(0)!.Out = 50;
        w.Run();
        Assert.Single(w.World.Surfaces);
        Assert.Empty(w.M.Landing.Flights);
        Assert.Null(w.M.Landing.RiderOf(PodAt(w, LeftHome)));
    }

    [Fact]
    public void FollowsItsOwnerAboardEmptyWhenItLeavesThePlanetOrWakesAboard()
    {
        var left = Ready().Do(new Launch()).Run(Down + 1).Do(new Leave() { Surface = 1 }).Run();
        Assert.Empty(left.On(1).Grid.MachinesWith(MachineTrait.Seats));
        Assert.Null(left.M.Landing.RiderOf(PodAt(left, LeftHome)));
        Assert.Equal(2, Biofuel(left.M, PodAt(left, LeftHome)));

        var woke = Ready().Do(new Launch()).Run(Down + 1).Do(new Alight { Surface = 1 }).Run();
        woke.On(1).Crew.HeroOf(0)!.Out = 1;
        woke.Run(2);
        Assert.Equal(0, woke.World.SurfaceOf(0));
        Assert.Empty(woke.On(1).Grid.MachinesWith(MachineTrait.Seats));
        Assert.Null(woke.M.Landing.RiderOf(PodAt(woke, LeftHome)));
    }

    [Fact]
    public void DocksHomeEvenWhenItsHeroLeavesMidReturn()
    {
        var w = Ready().Do(new Launch()).Run(Down + 1).Do(new Launch { Surface = 1 }).Run(10).Do(new Leave { Surface = 1 }).Run();
        Assert.Empty(w.On(1).Landing.Flights);
        Assert.Null(w.M.Landing.RiderOf(PodAt(w, LeftHome)));
        Assert.Equal(2, Biofuel(w.M, PodAt(w, LeftHome)));
        Assert.Empty(w.On(1).Grid.MachinesWith(MachineTrait.Seats));
    }

    [Fact]
    public void LaysItsCostAndLoadAtHomeWhenHomeWasBuiltOver()
    {
        var w = Ready().Do(new Launch()).Run(Down + 1);
        w.M.Grid.Place(LeftHome, new Placement("machine", "crate"));
        w.Do(new Launch { Surface = 1 }).Run(Up + 1);
        Assert.Equal(0, w.World.SurfaceOf(0));
        Assert.Equal("crate", w.M.Grid.MachineAt(LeftHome)!.Kind);
        Assert.Null(w.M.Landing.SeatOf(0));
        var laid = w.M.Items.Floor.GroupBy(s => s.Item).ToDictionary(g => g.Key, g => g.Sum(s => s.Count));
        Assert.Equal(2, laid["biofuel"]);
        Assert.All(Machines.Get("dropPod").Cost, c => Assert.Equal(c.Count, laid[c.Item]));
    }

    /// <summary>
    /// A friend plans a crate where the pod stood while it is down; the pod docks over it first. The plan's last tick waits on what
    /// stands there, as a wall's on a hero, rather than building into it: the world goes on.
    /// </summary>
    [Fact]
    public void APlanOverTheHomeOfAPodBackFirstWaitsOnItsLastTick()
    {
        var w = Ready().Do(new Launch()).Run(Down + 1);
        w.Do(new SetCreative(true) { From = 1 }).Do(new Stillwake.Sim.Building.PlacePlan(LeftHome, new Placement("machine", "crate")) { From = 1 }).Run();
        int plan = Assert.Single(w.M.Building.Plans).Id;
        w.Do(new Launch { Surface = 1 }).Run(Up + 1);
        Assert.Equal(PodAt(w, LeftHome), w.M.Landing.SeatOf(0));
        for (int i = 0; i < 200; i++) w.Do(new Stillwake.Sim.Building.Work(plan) { From = 1 }).Run();
        Assert.Contains("grid.machineInTheWay", Reasons(w));
        Assert.Equal("dropPod", w.M.Grid.MachineAt(LeftHome)!.Kind);
        Assert.Single(w.M.Building.Plans);
    }

    [Fact]
    public void PlaysOnAlikeSavedAndLoadedMidDescentAndMidReturn()
    {
        static Scenario Played(bool reload)
        {
            var w = Ready().Do(new Launch()).Run(Down / 2);
            if (reload) w.Reload();
            w.Run(Down / 2 + 1).Do(new Launch { Surface = 1 }).Run(Up / 2);
            if (reload) w.Reload();
            return w.Run(Up / 2 + 1);
        }
        var (played, loaded) = (Played(false), Played(true));
        Assert.Equal(PodAt(played, LeftHome), played.M.Landing.SeatOf(0));
        Assert.Equal(played.World.Save(), loaded.World.Save());
    }

    /// <summary>
    /// The slow one: two pods launched on one tick with no planet open grow the first planet once, both landing on it, side by side,
    /// and two worlds fed the same hash alike on the tick it opens.
    /// </summary>
    [Fact]
    public void TwoDescentsEndingOnOneTickOpenTheFirstPlanetOnceAndShareIt()
    {
        static Scenario Both()
        {
            var w = Ready(planet: false);
            return w.Do(new Board(PodAt(w, RightHome)) { From = 1 }).Run().Do(new Launch()).Do(new Launch { From = 1 }).Run(Down);
        }
        var (w, twin) = (Both(), Both());
        w.Run();
        twin.Run();
        Assert.Equal([Surface.Ship, Content.Landing.Planet], w.World.Surfaces.Select(s => s.Kind));
        Assert.Equal((1, 1), (w.World.SurfaceOf(0), w.World.SurfaceOf(1)));
        Assert.Equal(2, w.On(1).Grid.MachinesWith(MachineTrait.Seats).Count);
        Assert.Equal(w.World.Hash(), twin.World.Hash());
    }

    [Fact]
    public void RefusesAFlyingHeroAnotherPodAndALaunchFromAHeroGoneOnItsTick()
    {
        var w = Ready().Do(new Launch()).Run();
        w.Do(new Board(PodAt(w, RightHome))).Run();
        Assert.Equal(["landing.seated"], Reasons(w));
        Assert.Null(w.M.Landing.SeatOf(0));
        // A seat outlives a hero that left this tick until derivation: the launch it sent after is refused, its fuel unburnt.
        var gone = Ready().Do(new Leave()).Do(new Launch()).Run();
        Assert.Equal(["landing.notSeated"], Reasons(gone));
        Assert.Empty(gone.M.Landing.Flights);
        Assert.Equal(7, Biofuel(gone.M, PodAt(gone, LeftHome)));
    }

    [Fact]
    public void LaunchesWithJustItsFuelAndLandsAndDocksEmpty()
    {
        var w = Ready(fuel: Content.Landing.DescentFuel).Do(new Launch()).Run();
        Assert.Null(w.M.Landing.FlightOf(0)!.Load);
        w.Run(Down);
        var pod = Landed(w, 1);
        Assert.All(w.On(1).Items.Crate(pod.Id), s => Assert.Null(s));
        w.Do(new Launch { Surface = 1 }).Run(Up + 1);
        Assert.All(w.M.Items.Crate(PodAt(w, LeftHome)), s => Assert.Null(s));
    }

    [Fact]
    public void LaysItsCostAndLoadAtTheSpawnWhenNowhereOffTheShipIsLevelAndTallEnough()
    {
        // The ship's own grid taken for a surface off it: a hull six cells under the sky everywhere, no room for a pod to stand.
        var w = Ready().Do(new Launch()).Run();
        var carried = w.M.Landing.Depart(0);
        w.M.Landing.Where = new Fixed(1, canOpen: true);
        w.M.Landing.Arrive(0, carried, null);
        Assert.Single(w.M.Grid.MachinesWith(MachineTrait.Seats));
        Assert.Empty(((LandingState)w.M.Landing.Slice).Owners);
        var laid = w.M.Items.Floor.Where(s => s.Cell == w.M.Crew.Spawn).GroupBy(s => s.Item).ToDictionary(g => g.Key, g => g.Sum(s => s.Count));
        Assert.Equal(2, laid["biofuel"]);
        Assert.All(Machines.Get("dropPod").Cost, c => Assert.Equal(c.Count, laid[c.Item]));
    }

    [Fact]
    public void LandsAtTheNearestLevelSpotLeftFirstOnATie()
    {
        var carried = Ready().Do(new Launch()).Run().M.Landing.Depart(0);
        // Level ground off the ship, a crate standing where the pod would stand by the spawn: every anchor it covers refused.
        var w = new Scenario(Layout.Parse([.. Enumerable.Repeat(new string(' ', 20), 5), "        S           ", new string('#', 20)]), doubled: true);
        int width = w.M.Grid.Width, from = w.M.Crew.Spawn % width - 1, ground = 12;
        w.M.Grid.Place((ground - 2) * width + from, new Placement("machine", "crate"));
        w.M.Grid.Where = w.M.Landing.Where = new Fixed(1, canOpen: true);
        w.M.Landing.Arrive(0, carried, null);
        var pod = Assert.Single(w.M.Grid.MachinesWith(MachineTrait.Seats));
        Assert.Equal((ground - 6) * width + from - 4, pod.Cell);
    }

    [Fact]
    public void ArrivesUnseatedWhenItsHeroPassesOutOnTheWayBack()
    {
        var w = Ready().Do(new Launch()).Run(Down + 1).Do(new Launch { Surface = 1 }).Run(10);
        w.On(1).Crew.HeroOf(0)!.Out = 200;
        w.Run(Up);
        Assert.Equal(0, w.World.SurfaceOf(0));
        Assert.Null(w.M.Landing.RiderOf(PodAt(w, LeftHome)));
    }

    [Fact]
    public void ADevBeamMidFallLandsItSeatedAndShutsTheRunItOpened()
    {
        var w = Ready().Do(new Launch()).Run(5).Do(new Transfer(1)).Run();
        Assert.Equal(1, w.World.SurfaceOf(0));
        Assert.Equal(Landed(w, 1).Id, w.On(1).Landing.SeatOf(0));
        Assert.Equal(Structure.PlatformClosed, w.M.Grid.StructureAt(Under(LeftHome)));
    }

    [Fact]
    public void TwoPodsLaunchedFromOneRunAFewTicksApartLeaveItShut()
    {
        string[] wide = [.. Rows[..4], "#..ZZ.ZZ.......#", "#..ZZ.ZZ.......#", "#..ZZ.ZZ.......#", "###_____########", .. Rows[8..]];
        var w = new Scenario(Layout.Parse(wide, null, [((0, 0), (3, 7))]), doubled: true);
        int stand = Char(10, 6) + Wide, second = Char(6, 4);
        w.Do(new Enter(stand)).Do(new Enter(stand + 1) { From = 1 }).GiveItems(0, ("biofuel", 5)).GiveItems(1, ("biofuel", 5))
            .Do(new OpenSurface(PlanetTypes.Small.Id)).Run();
        foreach (var (peer, home) in new[] { (0, LeftHome), (1, second) })
        {
            int slot = w.M.Items.Inventory(peer).ToList().FindIndex(s => s?.Item == "biofuel");
            w.Do(new Move(new Stillwake.Sim.Items.Place(slot), new Target(null, PodAt(w, home)), 5) { From = peer })
                .Do(new Board(PodAt(w, home)) { From = peer });
        }
        w.Run().Do(new Launch()).Run(5).Do(new Launch { From = 1 }).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(Structure.PlatformOpen, w.M.Grid.StructureAt(Under(LeftHome)));
        // The first flight shuts it at its count, the second pod through already; the second's shut finds it shut.
        w.Run(Fall);
        Assert.Equal(Structure.PlatformClosed, w.M.Grid.StructureAt(Under(LeftHome)));
        Assert.Empty(w.M.Grid.MachinesWith(MachineTrait.Seats));
        Assert.Equal(2, w.M.Landing.Flights.Count);
    }

    [Fact]
    public void ALandedPodIsNoOnesToTakeApart()
    {
        var w = Ready().Do(new Launch()).Run(Down + 1).Do(new Alight { Surface = 1 }).Run();
        w.Do(new Stillwake.Sim.Building.Deconstruct(Landed(w, 1).Cell, "machine") { Surface = 1 }).Run();
        Assert.Equal(["building.notHere"], Reasons(w));
    }

    /// <summary>
    /// Through random boarding, leaving, launching, quitting and beaming of two heroes: every pod is in one place, standing on one
    /// surface or in one flight; a hero flies one at most and sits in none meanwhile; every pod off the ship stands by its owner, or
    /// its owner flies; biofuel goes only by a descent's five; and a twin fed the same, saved and loaded now and then, hashes alike.
    /// </summary>
    [Property(MaxTest = 20)]
    public Property KeepsEveryPodInOnePlaceByItsOwnerAndBurnsOnlyWhatDescentsDo()
    {
        var gen = from seed in Gen.Choose(1, 100_000)
                  from ticks in Gen.Choose(100, 600)
                  select (seed, ticks);
        return Prop.ForAll(gen.ToArbitrary(), t =>
        {
            var (w, twin) = (Ready(), Ready());
            var rng = new Rng(new RngState { S = (uint)t.seed });
            int descents = 0;
            for (int i = 0; i < t.ticks; i++)
            {
                var sent = Random(w, rng);
                var refused = w.World.Step(sent);
                twin.World.Step(sent);
                if (i % 97 == 96) twin.Reload();
                if (w.World.Hash() != twin.World.Hash()) return false.Label($"seed {t.seed}, tick {i}: the twin differs");
                descents += sent.Count(c => c is Launch && c.Surface == 0 && !refused.Any(r => ReferenceEquals(r.Command, c)));
                if (Problem(w, descents) is { } problem) return false.Label($"seed {t.seed}, tick {i}: {problem}");
            }
            return true.ToProperty();
        });
    }

    /// <summary>Now and then, for each hero, a board, a leave of its pod, a launch, a quit or a return to the game, or a beam across.</summary>
    private static List<Command> Random(Scenario w, Rng rng)
    {
        var sent = new List<Command>();
        foreach (int peer in new[] { 0, 1 })
        {
            if (rng.Below(8) != 0) continue;
            int here = w.World.SurfaceOf(peer);
            var m = w.On(here);
            var pods = m.Grid.MachinesWith(MachineTrait.Seats);
            Command c = rng.Below(7) switch
            {
                0 or 1 when pods.Count > 0 => new Board(pods[(int)rng.Below((uint)pods.Count)].Id),
                2 => new Alight(),
                3 or 4 => new Launch(),
                5 => m.Crew.HeroOf(peer) == null ? new Enter(Floor) : new Leave(),
                _ => new Transfer(here == 0 ? 1 : 0),
            };
            sent.Add(c with { From = peer, Surface = here });
        }
        return sent;
    }

    /// <summary>What breaks the invariants, after <paramref name="descents"/> descents from two pods of 7 biofuel each; null for nothing.</summary>
    private static string? Problem(Scenario w, int descents)
    {
        int pods = 0, fuel = 0;
        var (flying, seated) = (new List<int>(), new List<int>());
        foreach (var s in w.World.Surfaces)
        {
            var m = w.On(s.Id);
            flying.AddRange(m.Landing.Flights.Select(f => f.Peer));
            seated.AddRange(m.Landing.Seats.Select(x => x.Peer));
            var standing = m.Grid.MachinesWith(MachineTrait.Seats);
            pods += standing.Count + m.Landing.Flights.Count;
            fuel += standing.Sum(p => Biofuel(m, p.Id)) + m.Landing.Flights.Sum(f => f.Load?.Item == "biofuel" ? f.Load.Count : 0);
            fuel += m.Items.Floor.Where(f => f.Item == "biofuel").Sum(f => f.Count);
            fuel += new[] { 0, 1 }.Sum(p => m.Items.Inventory(p).Sum(x => x?.Item == "biofuel" ? x.Count : 0));
            foreach (var o in ((LandingState)m.Landing.Slice).Owners)
            {
                if (m.Grid.MachineById(o.Pod) == null) return $"peer {o.Peer}'s pod {o.Pod} gone from surface {s.Id}";
                bool aloft = w.World.Surfaces.Any(x => w.On(x.Id).Landing.FlightOf(o.Peer) != null);
                if (w.World.SurfaceOf(o.Peer) != s.Id && !aloft) return $"peer {o.Peer} on {w.World.SurfaceOf(o.Peer)}, its pod on {s.Id}";
            }
        }
        if (pods != 2) return $"{pods} pods standing and flying";
        if (flying.Count != flying.Distinct().Count()) return $"a hero in two flights: {string.Join(", ", flying)}";
        if (flying.Intersect(seated).Any()) return $"a flying hero seated: {string.Join(", ", flying.Intersect(seated))}";
        return fuel == 14 - Content.Landing.DescentFuel * descents ? null : $"{fuel} biofuel after {descents} descents";
    }
}
