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
/// The engine's jump (planet-jump): fired with power, fuel and everyone aboard, it closes the planet below for good and the next
/// descent opens another; with a hero off the ship or a pod in the air it is refused, the call to come home.
/// </summary>
public class JumpTests
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

    private static int Engine(Scenario w) => w.M.Grid.MachineAt(w.At(2, 5))!.Id;
    private static int Pod(Scenario w) => w.M.Grid.MachineAt(w.At(7, 4))!.Id;
    private static int Fuel(Scenario w) => w.M.Items.HeldFor(Engine(w), "biofuel");
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static bool Open(Scenario w, int surface) => !w.World.Surfaces[surface].Closed;

    /// <summary>
    /// The room, heroes 0 and 1 in it, <paramref name="fuel"/> biofuel in the engine and 12 in the pod, the small planet open
    /// below as surface 1, hero 0 seated in the pod.
    /// </summary>
    private static Scenario Ready(int fuel = 2 * Jump + 5, bool cabled = true)
    {
        var w = new Scenario(Layout.Parse(Rows, null, cabled ? Cables : [Cables[1]]), doubled: true);
        int floor = w.At(10, 6) + w.M.Grid.Width;
        w.Do(new Enter(floor)).Do(new Enter(floor + 1) { From = 1 }).Do(new OpenSurface(PlanetTypes.Small.Id)).Run();
        w.GiveItems(0, ("biofuel", fuel + 12));
        int slot = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == "biofuel");
        w.Do(new Move(new Slot(slot), new Target(0, Engine(w)), fuel)).Run();
        w.Do(new Move(new Slot(slot), new Target(0, Pod(w)), 12)).Run();
        return w.Do(new Board(Pod(w))).Run(2);
    }

    private static Scenario Fire(Scenario w, int from = 0) => w.Do(new FireEngine(Engine(w)) { From = from }).Run();

    [Fact]
    public void ClosesThePlanetBelowAndBurnsItsFuel()
    {
        var w = Ready();
        Assert.Null(w.M.Landing.FireProblem(Engine(w)));
        Fire(w);
        Assert.Empty(w.Refusals);
        Assert.False(Open(w, 1));
        Assert.Equal(Jump + 5, Fuel(w));
        // What it was is all that is kept of it.
        Assert.Equal((1, PlanetTypes.Small.Id), (w.World.Surfaces[1].Id, w.World.Surfaces[1].Kind));
    }

    [Fact]
    public void IsRefusedTwiceWithNoDescentBetween()
    {
        var w = Fire(Ready());
        Fire(w);
        Assert.Equal(["landing.nothingBelow"], Reasons(w));
        Assert.Equal(Jump + 5, Fuel(w));
    }

    [Fact]
    public void TheNextDescentOpensAnotherPlanetOfTheFirstKind()
    {
        var w = Fire(Ready());
        w.Do(new Launch()).Run(Down + 1);
        Assert.Empty(w.Refusals);
        Assert.Equal(2, w.World.SurfaceOf(0));
        Assert.Equal(Content.Landing.Planet, w.World.Surfaces[2].Kind);
        Assert.False(Open(w, 1));
        Assert.True(Open(w, 2));
        Assert.Single(w.On(2).Grid.MachinesWith(MachineTrait.Seats));
    }

    [Fact]
    public void IsRefusedWhileAHeroIsBelowTheCallToComeHome()
    {
        var w = Ready().Do(new Transfer(1) { From = 1 }).Run();
        Fire(w);
        Assert.Equal(["landing.crewAway"], Reasons(w));
        Assert.True(Open(w, 1));
        Assert.Equal(2 * Jump + 5, Fuel(w));
        // Home again, it fires.
        w.Do(new Transfer(0) { From = 1, Surface = 1 }).Run();
        Fire(w);
        Assert.Equal(["landing.crewAway"], Reasons(w));
        Assert.False(Open(w, 1));
    }

    [Fact]
    public void IsRefusedWhileAPodIsInTheAirDownOrUp()
    {
        var down = Ready().Do(new Launch()).Run(5);
        Fire(down, from: 1);
        Assert.Equal(["landing.crewAway"], Reasons(down));
        down.Run(Down);
        Assert.Equal(1, down.World.SurfaceOf(0));
        down.Do(new Launch { Surface = 1 }).Run(5);
        Fire(down, from: 1);
        Assert.Equal(["landing.crewAway", "landing.crewAway"], Reasons(down));
        Assert.True(Open(down, 1));
        // Docked, its rider aboard: it fires.
        down.Run(Up);
        Fire(down, from: 1);
        Assert.False(Open(down, 1));
    }

    [Fact]
    public void ALaunchAppliedBeforeItRefusesItAndOneAfterItDescendsToTheNewPlanet()
    {
        var before = Ready();
        before.Do(new Launch()).Do(new FireEngine(Engine(before)) { From = 1 }).Run();
        Assert.Equal(["landing.crewAway"], Reasons(before));
        var after = Ready();
        after.Do(new FireEngine(Engine(after)) { From = 1 }).Do(new Launch()).Run(Down + 1);
        Assert.Empty(after.Refusals);
        Assert.Equal(2, after.World.SurfaceOf(0));
    }

    [Fact]
    public void IsRefusedWithNoEngineNoPowerOrTooLittleFuel()
    {
        var w = Ready();
        w.Do(new FireEngine(Pod(w))).Do(new FireEngine(9999)).Run();
        Assert.Equal(["landing.noEngine", "landing.noEngine"], Reasons(w));
        Assert.Equal("landing.engineUnpowered", Ready(cabled: false).M.Landing.FireProblem(Engine(w)));
        var dry = Ready(fuel: Jump - 1);
        Fire(dry);
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
        Fire(w);
        Assert.Equal(["landing.noExhaust"], Reasons(w));
        Assert.True(Open(w, 1));
    }

    [Fact]
    public void OffTheShipThereIsNoEngineToFire()
    {
        var w = Ready().Do(new Launch()).Run(Down + 1);
        w.Do(new FireEngine(Engine(w)) { Surface = 1 }).Run();
        Assert.Equal(["landing.noEngine"], Reasons(w));
    }

    [Fact]
    public void AfterADeathBelowTheHeroWakesAboardItsPodHomeAndTheJumpTakesThePlanet()
    {
        var w = Ready().Do(new Launch()).Run(Down + 1).Do(new Alight { Surface = 1 }).Run();
        w.On(1).Crew.HeroOf(0)!.Out = 1;
        w.Run(2);
        Assert.Equal(0, w.World.SurfaceOf(0));
        Assert.Single(w.M.Grid.MachinesWith(MachineTrait.Seats));
        Fire(w);
        Assert.Empty(w.Refusals);
        Assert.False(Open(w, 1));
        var hash = w.World.Hash();
        Assert.Equal(hash, w.Reload().World.Hash());
        Assert.False(Open(w, 1));
    }

    [Fact]
    public void TheHullsLandedShowerHoldsAfterTheJump()
    {
        var w = Ready();
        var landed = w.M.Hazards.Shower;
        Assert.Equal(landed, Fire(w).M.Hazards.Shower);
        Assert.Equal(landed, w.Reload().M.Hazards.Shower);
    }

    [Fact]
    public void SavedAndLoadedAfterAJumpTheNextDescentIsTheSameOnBoth()
    {
        var (w, twin) = (Fire(Ready()), Fire(Ready()));
        twin.Reload();
        Assert.Equal(w.World.Hash(), twin.World.Hash());
        foreach (var s in new[] { w, twin }) s.Do(new OpenSurface(PlanetTypes.Small.Id)).Do(new Launch()).Run(Down + 1);
        Assert.Equal((2, 2), (w.World.SurfaceOf(0), twin.World.SurfaceOf(0)));
        Assert.Equal(w.World.Surfaces[2].Seed, twin.World.Surfaces[2].Seed);
        Assert.Equal(w.World.Hash(), twin.World.Hash());
    }

    /// <summary>
    /// Through random boarding, launching, quitting, beaming and firing of two heroes: whenever the engine fires, every hero and
    /// every pod is aboard and no planet is left open; a twin fed the same, saved and loaded now and then, hashes alike.
    /// </summary>
    [Property(MaxTest = 20)]
    public Property FiresOnlyWithEveryHeroAndPodAboard()
    {
        var gen = from seed in Gen.Choose(1, 100_000)
                  from ticks in Gen.Choose(200, 700)
                  select (seed, ticks);
        return Prop.ForAll(gen.ToArbitrary(), t =>
        {
            var (w, twin) = (Ready(fuel: 99), Ready(fuel: 99));
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
                if (new[] { 0, 1 }.Any(p => w.World.SurfaceOf(p) != 0)) return false.Label($"seed {t.seed}, tick {i}: fired with a hero away");
                // A launch applied after the fire in one tick has the pod in the air already, off to the new planet.
                if (w.M.Grid.MachinesWith(MachineTrait.Seats).Count + w.M.Landing.Flights.Count != 1) return false.Label($"seed {t.seed}, tick {i}: fired with the pod not aboard");
                // Fuelled again, the pod too, so the run fires and descends more than its first stacks allow.
                foreach (var s in new[] { w, twin })
                {
                    s.M.Items.PutIn(Engine(s), new Stack("biofuel", Jump));
                    if (s.M.Grid.MachineAt(s.At(7, 4)) is { } home) s.M.Items.PutIn(home.Id, new Stack("biofuel", Content.Landing.DescentFuel));
                }
            }
            return true.ToProperty().Classify(fired > 0, "fired");
        });
    }

    /// <summary>Now and then, for each hero: a board, a launch, a quit or a return to the game, a beam across, the planet opened again, the engine fired.</summary>
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
                _ => new FireEngine(Engine(w)),
            };
            sent.Add(c with { From = peer, Surface = here });
        }
        return sent;
    }
}
