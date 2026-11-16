using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Landing;
using Xunit;
using Move = Stillwake.Sim.Items.Move;
using Pairs = System.Collections.Generic.IReadOnlyList<((int X, int Y) A, (int X, int Y) B)>;
using Slot = Stillwake.Sim.Items.Place;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The ship's flight between the planets of its system (solar-system): a course set with power, fuel and everyone aboard leaves
/// the planet below for good, refuses descents for its flight, and the ship arrives at its planet, where the next descent lands
/// somewhere new; with a hero off the ship or a pod in the air it is refused, the call to come home.
/// </summary>
public class CourseTests
{
    /// <summary>A room with the engine at its stern's wall and a pod on a run in its floor, both cabled to the panel in space over it.</summary>
    private static readonly string[] Rows =
    [
        "PPP              ",
        "                 ",
        " ############### ",
        " #.............# ",
        " #.....ZZ......# ",
        " #XXX..ZZ......# ",
        " #XXX..ZZ......# ",
        " ######__####### ",
        "                 ",
        "                 ",
    ];

    private static readonly Pairs Cables = [((0, 0), (2, 5)), ((0, 0), (7, 7))];
    private const int Jump = Content.Landing.JumpFuel, Down = Content.Landing.DescentTicks, Up = Content.Landing.AscentTicks;
    private const int Flight = Content.Landing.FlightTicks, Litre = Content.Fuel.Litre;

    private static int Engine(Scenario w) => w.M.Grid.MachineAt(w.At(2, 5))!.Id;
    private static int Pod(Scenario w) => w.M.Grid.MachineAt(w.At(7, 4))!.Id;
    private static int Fuel(Scenario w) => w.M.Fuel.FuelIn(Engine(w));
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static bool Open(Scenario w, int surface) => !w.World.Surfaces[surface].Closed;

    /// <summary>
    /// The room, heroes 0 and 1 in it, <paramref name="fuel"/> millilitres in the engine's store and two descents' in the pod's, the small planet open
    /// below as surface 1, hero 0 seated in the pod.
    /// </summary>
    private static Scenario Ready(int fuel = 2 * Jump + Litre, bool cabled = true, string[]? rows = null)
    {
        var w = new Scenario(Layout.Parse(rows ?? Rows, null, cabled ? Cables : [Cables[1]]), doubled: true);
        int floor = w.At(10, 6) + w.M.Grid.Width;
        w.Do(new Enter(floor)).Do(new Enter(floor + 1) { From = 1 }).Do(new OpenSurface(PlanetTypes.Small.Id)).Run();
        if (w.M.Landing.Engine != null) w.M.Fuel.Fill(Engine(w), fuel);
        w.M.Fuel.Fill(Pod(w), 2 * Content.Fuel.Descent);
        return w.Do(new Board(Pod(w))).Run(2);
    }

    private static Scenario Course(Scenario w, int planet, int from = 0) => w.Do(new PlotCourse(planet) { From = from }).Run();

    [Fact]
    public void SetsOffBurningItsFuelAndLeavingThePlanetBelowForGood()
    {
        var w = Ready();
        Assert.Null(w.M.Landing.CourseProblem(3));
        Course(w, 3);
        Assert.Empty(w.Refusals);
        Assert.False(Open(w, 1));
        Assert.Equal(Jump + Litre, Fuel(w));
        Assert.Equal((0, 3), (w.M.Landing.At, w.M.Landing.Course!.To));
        // What the planet was is all that is kept of it.
        Assert.Equal((1, PlanetTypes.Small.Id), (w.World.Surfaces[1].Id, w.World.Surfaces[1].Kind));
    }

    [Fact]
    public void ArrivesAfterItsFlightAndTheNextDescentLandsOnANewPlanet()
    {
        var w = Course(Ready(), 3).Run(Flight - 2);
        Assert.Equal(1, w.M.Landing.Course!.TicksLeft);
        w.Run();
        Assert.Null(w.M.Landing.Course);
        Assert.Equal(3, w.M.Landing.At);
        w.Do(new Launch()).Run(Down + 1);
        Assert.Empty(w.Refusals);
        Assert.Equal(2, w.World.SurfaceOf(0));
        Assert.Equal(Content.Landing.Planet, w.World.Surfaces[2].Kind);
        Assert.Single(w.On(2).Grid.MachinesWith(MachineTrait.Seats));
    }

    [Fact]
    public void RefusesADescentInFlightTheArrivalTicksToo()
    {
        var w = Course(Ready(), 3).Run(10);
        Assert.Equal("landing.inFlight", w.M.Landing.LaunchProblem(0));
        w.Do(new Launch()).Run(Flight - 12);
        // The arrival comes after the tick's commands: a launch on its tick is still refused, and goes on the next.
        w.Do(new Launch()).Run();
        Assert.Equal(["landing.inFlight", "landing.inFlight"], Reasons(w));
        Assert.Equal(3, w.M.Landing.At);
        w.Do(new Launch()).Run(Down + 1);
        Assert.Equal(2, w.World.SurfaceOf(0));
    }

    [Fact]
    public void FlyingBackToAPlanetVisitedLandsSomewhereNew()
    {
        var w = Course(Ready(), 3).Run(Flight).Do(new Launch()).Run(Down + 1);
        w.Do(new Launch { Surface = 2 }).Run(Up + 1);
        Assert.Equal(0, w.World.SurfaceOf(0));
        Course(w, 0).Run(Flight).Do(new Board(Pod(w))).Run().Do(new Launch()).Run(Down + 1);
        Assert.Empty(w.Refusals);
        Assert.Equal(0, w.M.Landing.At);
        Assert.Equal(3, w.World.SurfaceOf(0));
        Assert.False(Open(w, 2));
        Assert.NotEqual(w.World.Surfaces[1].Seed, w.World.Surfaces[3].Seed);
    }

    /// <summary>Surface <paramref name="surface"/>'s walls, cell by cell: its ground as grown and changed.</summary>
    private static Content.Material?[] Walls(Scenario w, int surface)
    {
        var grid = w.On(surface).Grid;
        return [.. Enumerable.Range(0, grid.Width * grid.Height).Select(grid.WallAt)];
    }

    /// <summary>To system planet 1, a scorched one, and down: the planet opened as surface 2.</summary>
    private static Scenario DownOnTheSecond() => Course(Ready(), 1).Run(Flight).Do(new Launch()).Run(Down + 1);

    [Fact]
    public void ADescentLandsOnItsPlanetsTypeHoldingItsOresAndARevisitOnNewGroundAlike()
    {
        var w = DownOnTheSecond();
        Assert.Empty(w.Refusals);
        var ores = w.M.Landing.OresOf(1);
        Assert.InRange(ores.Count, Content.Landing.OresLeast + 1, Content.Landing.OresMost + 1);
        Assert.Equal(PlanetTypes.Scorched.Id, w.World.Surfaces[2].Kind);
        Assert.Equal(ores, w.World.Surfaces[2].Ores!);
        // Away to the next planet and back without landing there: the second planet's ground grown anew, its ores the same.
        w.Do(new Launch { Surface = 2 }).Run(Up + 1);
        Course(w, 2).Run(Flight).M.Fuel.Fill(Engine(w), Jump);
        Course(w, 1).Run(Flight).Do(new Board(Pod(w))).Run().Do(new Launch()).Run(Down + 1);
        Assert.Empty(w.Refusals);
        Assert.Equal(3, w.World.SurfaceOf(0));
        Assert.Equal(PlanetTypes.Scorched.Id, w.World.Surfaces[3].Kind);
        Assert.Equal(ores, w.World.Surfaces[3].Ores!);
        Assert.NotEqual(w.World.Surfaces[2].Seed, w.World.Surfaces[3].Seed);
    }

    /// <summary>A planet lacking some ores, saved and loaded, grows the same ground again: the ores travel in the save.</summary>
    [Fact]
    public void SavedAndLoadedOnAPlanetItsGroundGrowsWithItsOres()
    {
        var w = DownOnTheSecond();
        var (walls, ores) = (Walls(w, 2), w.World.Surfaces[2].Ores);
        w.Reload();
        Assert.Equal(ores, w.World.Surfaces[2].Ores);
        Assert.Equal(walls, Walls(w, 2));
    }

    /// <summary>A guest joining mid-descent gets the dealt ores in the save and grows the same planet as the host.</summary>
    [Fact]
    public void AGuestJoiningMidDescentGrowsTheSamePlanet()
    {
        var (w, twin) = (Course(Ready(), 1).Run(Flight), Course(Ready(), 1).Run(Flight));
        foreach (var s in new[] { w, twin }) s.Do(new Launch()).Run(Down - 5);
        twin.Reload();
        foreach (var s in new[] { w, twin }) s.Run(6);
        Assert.Equal((2, 2), (w.World.SurfaceOf(0), twin.World.SurfaceOf(0)));
        Assert.Equal(w.World.Hash(), twin.World.Hash());
        Assert.Equal(Walls(w, 2), Walls(twin, 2));
    }

    /// <summary>A save from before ores were dealt, its pod a tick from landing: the ores are dealt first, and it lands on them.</summary>
    [Fact]
    public void AnOldSaveMidDescentDealsBeforeItLands()
    {
        var w = Course(Ready(), 1).Run(Flight).Do(new Launch()).Run(Down - 1);
        var save = System.Text.Json.Nodes.JsonNode.Parse(w.World.Save())!;
        Assert.True(save["slices"]!["landing"]!.AsObject().Remove("ores"));
        w.Reload(save.ToJsonString()).Run(2);
        Assert.Equal(2, w.World.SurfaceOf(0));
        Assert.NotEmpty(w.M.Landing.OresOf(1));
        Assert.Equal(w.M.Landing.OresOf(1), w.World.Surfaces[2].Ores!);
    }

    [Fact]
    public void IsRefusedWhileAHeroIsBelowTheCallToComeHome()
    {
        var w = Ready().Do(new Transfer(1) { From = 1 }).Run();
        Course(w, 3);
        Assert.Equal(["landing.crewAway"], Reasons(w));
        Assert.True(Open(w, 1));
        Assert.Equal(2 * Jump + Litre, Fuel(w));
        Assert.Null(w.M.Landing.Course);
        // Home again, it sets off.
        w.Do(new Transfer(0) { From = 1, Surface = 1 }).Run();
        Course(w, 3);
        Assert.Equal(["landing.crewAway"], Reasons(w));
        Assert.False(Open(w, 1));
    }

    [Fact]
    public void IsRefusedWhileAPodIsInTheAirDownOrUpOrLandingThisTick()
    {
        var down = Ready().Do(new Launch()).Run(5);
        Course(down, 3, from: 1);
        // Its last tick: the pod lands after the commands, so it is in the air still.
        down.Run(Down - 7);
        Assert.Equal(0, down.World.SurfaceOf(0));
        Course(down, 3, from: 1);
        Assert.Equal(1, down.World.SurfaceOf(0));
        down.Do(new Launch { Surface = 1 }).Run(5);
        Course(down, 3, from: 1);
        Assert.Equal(["landing.crewAway", "landing.crewAway", "landing.crewAway"], Reasons(down));
        Assert.True(Open(down, 1));
        // Docked, its rider aboard: it sets off.
        down.Run(Up);
        Course(down, 3, from: 1);
        Assert.False(Open(down, 1));
    }

    [Fact]
    public void ALaunchBeforeItInOneTickRefusesItAndOneAfterItIsRefused()
    {
        var before = Ready();
        before.Do(new Launch()).Do(new PlotCourse(3) { From = 1 }).Run();
        Assert.Equal(["landing.crewAway"], Reasons(before));
        var after = Ready();
        after.Do(new PlotCourse(3) { From = 1 }).Do(new Launch()).Run();
        Assert.Equal(["landing.inFlight"], Reasons(after));
    }

    [Fact]
    public void IsRefusedThereAlreadyOrInFlightOrForNoSuchPlanet()
    {
        var w = Ready();
        Course(w, 0);
        Course(w, -1);
        Course(w, Content.Landing.Planets.Count);
        Course(w, 3);
        Course(w, 4);
        Assert.Equal(["landing.hereAlready", "landing.hereAlready", "landing.hereAlready", "landing.inFlight"], Reasons(w));
        Assert.Equal(Jump + Litre, Fuel(w));
    }

    [Fact]
    public void IsRefusedWithNoEngineNoPowerOrTooLittleFuel()
    {
        string[] bare = [.. Rows.Select(r => r.Replace('X', '.'))];
        Assert.Equal("landing.noEngine", Ready(rows: bare).M.Landing.CourseProblem(3));
        Assert.Equal("landing.engineUnpowered", Ready(cabled: false).M.Landing.CourseProblem(3));
        var dry = Ready(fuel: Jump - 1);
        Course(dry, 3);
        Assert.Equal(["landing.noFuel"], Reasons(dry));
        Assert.Equal(Jump - 1, Fuel(dry));
        Assert.True(Open(dry, 1));
    }

    [Fact]
    public void IsRefusedWithItsNozzleWalledInSinceItWasBuilt()
    {
        var w = Ready();
        // The space its nozzle reached, past the stern's wall, built over.
        foreach (int dy in new[] { 0, 1, 2, 3 })
            foreach (int dx in new[] { 0, 1 })
                w.Do(new Place(w.At(0, 5) + dy * w.M.Grid.Width + dx, Placement.Wall("iron")));
        w.Run();
        Assert.False(w.M.Grid.Exhausts(Engine(w)));
        Course(w, 3);
        Assert.Equal(["landing.noExhaust"], Reasons(w));
        Assert.True(Open(w, 1));
    }

    [Fact]
    public void OffTheShipThereIsNoEngine()
    {
        var w = Ready().Do(new Launch()).Run(Down + 1);
        w.Do(new PlotCourse(3) { Surface = 1 }).Run();
        Assert.Equal(["landing.noEngine"], Reasons(w));
    }

    [Fact]
    public void AfterADeathBelowTheHeroWakesAboardItsPodHomeAndTheCourseTakesThePlanet()
    {
        var w = Ready().Do(new Launch()).Run(Down + 1).Do(new Alight { Surface = 1 }).Run();
        w.On(1).Crew.HeroOf(0)!.Out = 1;
        w.Run(2);
        Assert.Equal(0, w.World.SurfaceOf(0));
        Assert.Single(w.M.Grid.MachinesWith(MachineTrait.Seats));
        Course(w, 3);
        Assert.Empty(w.Refusals);
        Assert.False(Open(w, 1));
        var hash = w.World.Hash();
        Assert.Equal(hash, w.Reload().World.Hash());
        Assert.False(Open(w, 1));
    }

    [Fact]
    public void TheHullsLandedShowerHoldsAfterLeaving()
    {
        var w = Ready();
        var landed = w.M.Hazards.Shower;
        Assert.Equal(landed, Course(w, 3).M.Hazards.Shower);
        Assert.Equal(landed, w.Reload().M.Hazards.Shower);
    }

    [Fact]
    public void SavedAndLoadedInFlightItArrivesOnTheSameTickAndDescendsAlike()
    {
        var (w, twin) = (Course(Ready(), 2).Run(100), Course(Ready(), 2).Run(100));
        twin.Reload();
        Assert.Equal(w.World.Hash(), twin.World.Hash());
        Assert.Equal(w.M.Landing.Course, twin.M.Landing.Course);
        foreach (var s in new[] { w, twin }) s.Run(Flight - 101);
        Assert.Equal((2, 2), (w.M.Landing.At, twin.M.Landing.At));
        foreach (var s in new[] { w, twin }) s.Do(new Launch()).Run(Down + 1);
        Assert.Equal((2, 2), (w.World.SurfaceOf(0), twin.World.SurfaceOf(0)));
        Assert.Equal(w.World.Surfaces[2].Seed, twin.World.Surfaces[2].Seed);
        Assert.Equal(w.World.Hash(), twin.World.Hash());
    }

    /// <summary>
    /// Through random boarding, launching, quitting, beaming and courses of two heroes: whenever a course sets off, every hero and
    /// every pod is aboard and no planet is left open; a twin fed the same, saved and loaded now and then, hashes alike.
    /// </summary>
    [Property(MaxTest = 20)]
    public Property SetsOffOnlyWithEveryHeroAndPodAboard()
    {
        var gen = from seed in Gen.Choose(1, 100_000)
                  from ticks in Gen.Choose(200, 700)
                  select (seed, ticks);
        return Prop.ForAll(gen.ToArbitrary(), t =>
        {
            var (w, twin) = (Ready(fuel: 2 * Jump), Ready(fuel: 2 * Jump));
            var rng = new Rng(new RngState { S = (uint)t.seed });
            int fired = 0;
            for (int i = 0; i < t.ticks; i++)
            {
                var sent = Random(w, rng);
                int fuel = Fuel(w);
                w.World.Step(sent);
                twin.World.Step(sent);
                if (i % 97 == 96) twin.Reload();
                if (w.World.Hash() != twin.World.Hash()) return false.Label($"seed {t.seed}, tick {i}: the twin differs");
                if (Fuel(w) == fuel) continue;
                fired++;
                if (w.World.Surfaces.Any(s => s.Id != 0 && !s.Closed)) return false.Label($"seed {t.seed}, tick {i}: a planet left open");
                if (new[] { 0, 1 }.Any(p => w.World.SurfaceOf(p) != 0)) return false.Label($"seed {t.seed}, tick {i}: set off with a hero away");
                if (w.M.Grid.MachinesWith(MachineTrait.Seats).Count != 1) return false.Label($"seed {t.seed}, tick {i}: set off with the pod not aboard");
                // Fuelled again, the pod too, so the run sets off and descends more than its first stacks allow.
                foreach (var s in new[] { w, twin })
                {
                    s.M.Fuel.Fill(Engine(s), Jump);
                    if (s.M.Grid.MachineAt(s.At(7, 4)) is { } home) s.M.Fuel.Fill(home.Id, Content.Fuel.Descent);
                }
            }
            return true.ToProperty().Classify(fired > 0, "set off");
        });
    }

    /// <summary>Now and then, for each hero: a board, a launch, a quit or a return to the game, a beam across, the planet opened again, a course.</summary>
    private static List<Command> Random(Scenario w, Rng rng)
    {
        var sent = new List<Command>();
        foreach (int peer in new[] { 0, 1 })
        {
            if (rng.Below(6) != 0) continue;
            int here = w.World.SurfaceOf(peer);
            var m = w.On(here);
            var pods = m.Grid.MachinesWith(MachineTrait.Seats);
            int planet = w.World.Surfaces.LastOrDefault(s => s.Id != 0 && !s.Closed)?.Id ?? 0;
            Command c = rng.Below(9) switch
            {
                0 or 1 when pods.Count > 0 => new Board(pods[0].Id),
                2 or 3 => new Launch(),
                4 => m.Crew.HeroOf(peer) == null ? new Enter(w.At(10, 6) + w.M.Grid.Width) : new Leave(),
                5 => new Transfer(here == 0 ? planet : 0),
                6 when planet == 0 => new OpenSurface(PlanetTypes.Small.Id),
                _ => new PlotCourse((int)rng.Below((uint)Content.Landing.Planets.Count)),
            };
            sent.Add(c with { From = peer, Surface = here });
        }
        return sent;
    }
}
