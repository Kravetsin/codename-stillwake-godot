using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Creatures;
using Stillwake.Sim.Crew;
using Xunit;
using static Stillwake.Sim.Tests.SpireHall;
using Tune = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Tests;

/// <summary>A spire's hall on a grown planet, for the guards' tests: its cells by the prefab's, and a hero sent about it.</summary>
internal static class SpireHall
{
    private static readonly string Axe = Weapons.Axe.Item;

    public static Registry M(Scenario w) => w.On(1);

    /// <summary>The cell at (<paramref name="x"/>, <paramref name="y"/>) of the spire's prefab, in cells, its left post's own at (0, 52).</summary>
    public static int Cell(Scenario w, int x, int y, int? left = null)
    {
        int post = left ?? Post(w, Ruins.PostLeft), width = M(w).Grid.Width;
        return (post / width - 52 + y) * width + post % width + x;
    }

    public static int Post(Scenario w, string kind) => w.On(1).Building.Plants.First(p => p.Kind == kind).Cell;

    public static Scenario InHall(this Scenario w, int peer = 0) => w.Do(new Enter(Cell(w, 25, 55)) { From = peer }).Run();

    /// <summary>The hero beside the end wall, in the rock outside it, as one digging its own way in.</summary>
    public static Scenario BesideWall(this Scenario w) => w.Do(new Enter(Cell(w, -2, 55))).Run();

    public static Scenario RunTill(this Scenario w, Func<bool> done, int most = 400)
    {
        for (int t = 0; t < most && !done(); t++) w.Run();
        Assert.True(done(), $"not so after {most} ticks");
        return w;
    }

    /// <summary>Cuts the cradle down with the axe, on one tick, its last swing landing in the commands of the tick.</summary>
    public static Scenario FellCradle(this Scenario w, int swings = 4)
    {
        int cradle = Cell(w, 24, 54);
        for (int i = 0; i < swings; i++) w.Do(HewTesting.Dig(w, cradle, Axe, surface: 1)).Run();
        Assert.Empty(w.Refusals);
        return w;
    }
}

/// <summary>
/// The spire's guards (docs/plans/spire-guards.md) on a planet grown as a player's is, so its posts and its relic come back on a load:
/// asleep in their niches, woken by a hero in the arena or a blow, pouncing by turns, stuck on the floor; killed for their circuits, or
/// gone with nothing once the shard is taken; calmed and back whole with nobody left in the hall.
/// </summary>
public class SpireGuardTests
{
    private const int Unit = CreaturesModule.Unit;
    private static readonly string Axe = Weapons.Axe.Item;
    private static readonly Tune.Ward Ward = Tune.Warden.Brain.OfType<Tune.Ward>().Single();
    private static readonly Tune.Pounce Pounce = (Tune.Pounce)Tune.Warden.Moves[0];

    /// <summary>A hero on the small spire planet, reported at its spawn: nowhere near the arena, but in the posts' keep boxes.</summary>
    private static Scenario Down(uint seed = 1, bool breathing = true)
    {
        var w = new Scenario("""
            ......
            .S....
            ######
            """, seed);
        w.Do(new Enter(w.M.Crew.Spawn)).Run();
        w.Do(new OpenSurface(PlanetTypes.SmallSpire.Id)).Run();
        w.Do(new Transfer(1)).Run();
        if (breathing) w.Do(new Items.SetCreative(true)).Run();
        return w;
    }

    /// <summary>The guards there are, in id order.</summary>
    private static List<Creature> Guards(Scenario w) => [.. M(w).Creatures.Creatures.Where(c => c.Species == Tune.Warden.Id)];

    /// <summary>Runs till the beat has brought what the posts bring, and the two guards stand asleep; both their cells' posts known.</summary>
    private static (Scenario W, Creature A, Creature B) Posted(uint seed = 1, bool breathing = true)
    {
        var w = Down(seed, breathing).Run(Tune.Beat + 1);
        var guards = Guards(w);
        Assert.Equal(2, guards.Count);
        return (w, guards[0], guards[1]);
    }

    private static int Suit(Scenario w, int peer = 0) => M(w).Crew.HeroOf(peer)!.Air;

    private static int Count(Scenario w, string item) =>
        M(w).Items.Floor.Where(s => s.Item == item).Sum(s => s.Count) + M(w).Items.Totals(0).Where(t => t.Item == item).Sum(t => t.Count);

    [Fact]
    public void AGuardAppearsAtEachPostOnTheBeatAsleepAndStaysAsleepWithNoHeroInTheArena()
    {
        var (w, a, b) = Posted();
        Assert.Equal((Post(w, Ruins.PostLeft), Post(w, Ruins.PostRight)), (CreaturesModule.PostOf(a), CreaturesModule.PostOf(b)));
        Assert.Equal(M(w).Building.Relics.Single(r => r.Kind == Ruins.Cradle).Cell, CreaturesModule.RelicOf(a));
        Assert.Equal(CreaturesModule.RelicOf(a), CreaturesModule.RelicOf(b));
        // In their niches, under the lintels, on the floor.
        Assert.Equal((Cell(w, 2, 55) % M(w).Grid.Width * Unit + 2 * Unit, (Cell(w, 2, 55) / M(w).Grid.Width + 1) * Unit), (a.X, a.Y));
        Assert.Equal((Cell(w, 44, 55) % M(w).Grid.Width * Unit + 2 * Unit, a.Y), (b.X, b.Y));
        w.Run(4 * Tune.Beat);
        Assert.All(Guards(w), g => Assert.True(CreaturesModule.Asleep(g)));
        Assert.Equal(2, Guards(w).Count);
    }

    [Fact]
    public void AHeroDiggingBesideTheWallDoesNotWakeThemAndEnteringTheArenaDoes()
    {
        var (w, a, b) = Posted();
        w.BesideWall().Run(3 * Tune.Beat);
        Assert.True(CreaturesModule.Asleep(a) && CreaturesModule.Asleep(b));
        w.InHall();
        Assert.False(CreaturesModule.Asleep(a));
        Assert.False(CreaturesModule.Asleep(b));
    }

    [Fact]
    public void ABlowFromOutsideWakesTheStruckGuardAndItsSiblingAndItFliesAtTheShooter()
    {
        var (w, a, b) = Posted();
        w.BesideWall();
        w.Do(new Strike(b.Id, Axe)).Run();
        Assert.False(CreaturesModule.Asleep(b));
        w.Run();
        Assert.False(CreaturesModule.Asleep(a));
        w.RunTill(() => b.Act is { Phase: ActPhase.Active }, Ward.Wake + Pounce.WindUp + 4);
        Assert.True(b.VX < 0, $"flying {b.VX} at a hero on its left");
    }

    [Fact]
    public void ItCrouchesThenFliesStraightAndStopsFlushAtAWall()
    {
        var (w, a, _) = Posted();
        w.Do(new Enter(Cell(w, 25, 55))).RunTill(() => a.Act != null);
        Assert.Equal((Posture.WindUp, Pounce.WindUp), (a.Posture, a.Act!.Left));
        var (x, y) = (a.X, a.Y);
        w.Run(Pounce.WindUp - 1);
        Assert.Equal((x, y), (a.X, a.Y));
        w.Run();
        Assert.Equal(Posture.Charge, a.Posture);
        // Level with a hero level with it, at the pounce's whole speed; rising a share of it, straight, no gravity.
        var (vx, vy) = (a.VX, a.VY);
        Assert.Equal(Pounce.Speed, vx);
        Assert.InRange(vy, -Unit / 10, 0);
        // The hero steps aside: it flies on across the hall to the other end's wall.
        w.Do(new Enter(Cell(w, 25, 40))).Run();
        Assert.Equal((vx, vy), (a.VX, a.VY));
        int flush = (M(w).Building.PostAt(Post(w, Ruins.PostRight))!.Cell % M(w).Grid.Width) * Unit - 2 * Unit;
        w.RunTill(() => a.Act == null, Pounce.Ticks + 1);
        Assert.True(CreaturesModule.Stuck(a));
        Assert.Equal(flush, a.X);
        Assert.Equal((0, 0), (a.VX, a.VY));
    }

    [Fact]
    public void StoppedAtTheRoofItFallsAndCountsItsStuckOnTheFloor()
    {
        var (w, a, _) = Posted();
        // A hero up across the hall: it flies up past its lintel, stops under the step, and falls.
        w.Do(new Enter(Cell(w, 30, 42))).RunTill(() => a.Act is { Phase: ActPhase.Active });
        w.RunTill(() => a.Act == null);
        Assert.True(CreaturesModule.Stuck(a));
        Assert.True(a.Y < (Cell(w, 0, 55) / M(w).Grid.Width + 1) * Unit, "it never left the floor");
        w.RunTill(() => a.Y == (Cell(w, 0, 55) / M(w).Grid.Width + 1) * Unit && a.VY == 0);
        // On the floor, so a tool reaches it, for its whole stuck.
        w.Run(Ward.Stuck - 3);
        Assert.True(CreaturesModule.Stuck(a));
    }

    [Fact]
    public void AHitCostsFiveAndLeavesItStuckAndAStuckGuardHitsNobody()
    {
        var (w, a, _) = Posted(breathing: false);
        w.Do(new Enter(Cell(w, 25, 55))).RunTill(() => a.Act is { Phase: ActPhase.Active });
        int suit = Suit(w);
        Assert.Null(M(w).Creatures.HitByProblem(a.Id));
        w.Do(new HitBy(a.Id)).Run();
        Assert.Equal(suit - Pounce.Damage, Suit(w));
        Assert.True(CreaturesModule.Stuck(a));
        w.Do(new HitBy(a.Id)).Run();
        Assert.Contains(w.Refusals, r => r.Reason == "creatures.notAttacking");
        Assert.Equal(suit - Pounce.Damage, Suit(w));
    }

    [Fact]
    public void TheyGoByTurnsTheStuckOnesWindowTheOthersCrouch()
    {
        var (w, a, b) = Posted();
        w.InHall();
        var order = new List<int>();
        for (int t = 0; t < 500 && order.Count < 4; t++)
        {
            int was = (a.Act == null ? 0 : 1) + (b.Act == null ? 0 : 2);
            w.Run();
            int now = (a.Act == null ? 0 : 1) + (b.Act == null ? 0 : 2);
            Assert.NotEqual(3, now);
            if (now != 0 && was == 0 || now != was && now != 0) order.Add(now);
            // The hero stays up out of a level flight, so each goes on to the far wall.
            w.Do(new Enter(Cell(w, 25, 40)));
        }
        Assert.Equal([1, 2, 1, 2], order);
    }

    [Fact]
    public void ALoneGuardGoesOnPouncing()
    {
        var (w, a, b) = Posted();
        b.Damage = Tune.Warden.Life.Hp - 1;
        w.Do(new Strike(b.Id, Axe)).Run();
        Assert.DoesNotContain(b, Guards(w));
        w.InHall();
        for (int i = 0; i < 3; i++)
        {
            w.RunTill(() => a.Act is { Phase: ActPhase.Active });
            w.Do(new Enter(Cell(w, 25, a.X < 25 * Unit ? 40 : 41)));
            w.RunTill(() => a.Act == null);
        }
    }

    [Fact]
    public void AKillYieldsItsDropAndFellsItsPostForGoodThroughALeavingAndALoad()
    {
        var (w, a, b) = Posted();
        int post = CreaturesModule.PostOf(a)!.Value;
        a.Damage = Tune.Warden.Life.Hp - 1;
        w.Do(new Strike(a.Id, Axe)).Run();
        Assert.InRange(Count(w, "circuit"), 2, 3);
        Assert.InRange(Count(w, "alloyScrap"), 1, 2);
        Assert.Null(M(w).Building.PostAt(post));
        Assert.Single(Guards(w));
        // A second felling, as the wall's break then the kill, does nothing.
        M(w).Building.FellPost(post);
        w.Run(2 * Tune.Beat);
        Assert.Equal([b.Id], Guards(w).Select(g => g.Id));
        w.Reload();
        w.Run(2 * Tune.Beat);
        Assert.Equal([b.Id], Guards(w).Select(g => g.Id));
        // Off the planet and back: the other comes again, the dead one never.
        w.Do(new Transfer(0)).Run(2);
        Assert.Empty(Guards(w));
        w.Do(new Transfer(1)).Run(2 * Tune.Beat);
        var again = Assert.Single(Guards(w));
        Assert.Equal(Post(w, Ruins.PostRight), CreaturesModule.PostOf(again));
    }

    [Fact]
    public void WithNobodyInTheArenaAWokenGuardCalmsAndComesBackWhole()
    {
        var (w, a, b) = Posted();
        w.InHall();
        b.Damage = 6;
        w.BesideWall().Run(Ward.Calm - 3);
        Assert.Equal(2, Guards(w).Count);
        w.Run(4);
        Assert.Empty(Guards(w));
        Assert.Equal(0, Count(w, "circuit"));
        w.Run(Tune.Beat);
        Assert.Equal(2, Guards(w).Count);
        Assert.All(Guards(w), g => Assert.True(CreaturesModule.Asleep(g) && g.Damage == 0));
    }

    [Fact]
    public void ABlowPutsOffItsCalmForBothSiblingsAlike()
    {
        var (w, a, b) = Posted();
        w.InHall().BesideWall().Run(Ward.Calm / 2);
        w.Do(new Strike(a.Id, Axe)).Run(Ward.Calm - 4);
        Assert.Equal(2, Guards(w).Count);
        w.Run(5);
        Assert.Empty(Guards(w));
    }

    [Fact]
    public void ASecondHeroInTheArenaKeepsItAndABodyDoesNot()
    {
        var w = Down(breathing: false);
        w.Do(new Enter(w.On(1).Crew.Spawn) { From = 1 }).Do(new Transfer(1) { From = 1 }).Run(Tune.Beat + 1);
        Assert.Equal(2, Guards(w).Count);
        w.InHall().InHall(peer: 1);
        w.BesideWall().Run(2 * Ward.Calm);
        Assert.Equal(2, Guards(w).Count);
        // Passed out where it stood, it is no one in the hall.
        M(w).Crew.Blow(1, 10 * Scenario.Full);
        Assert.True(M(w).Crew.HeroOf(1)!.Out > 0);
        w.Run(Ward.Calm + 2);
        Assert.Empty(Guards(w));
    }

    [Fact]
    public void TheCradleFelledSendsThemOffThatTickWithNothing()
    {
        var (w, a, b) = Posted();
        w.Do(new Enter(Cell(w, 24, 55))).FellCradle(swings: 3);
        Assert.Equal(2, Guards(w).Count);
        w.Do(HewTesting.Dig(w, Cell(w, 24, 54), Axe, surface: 1)).Run();
        Assert.Empty(Guards(w));
        Assert.Equal(0, Count(w, "circuit"));
        w.Run(2 * Tune.Beat);
        Assert.Empty(Guards(w));
    }

    [Fact]
    public void ACradleDugFromUnderFellsThemToo()
    {
        var (w, _, _) = Posted();
        w.BesideWall();
        for (int i = 0; i < 16 && Guards(w).Count > 0; i++) w.Do(HewTesting.Dig(w, Cell(w, 22, 56), Axe, surface: 1)).Run();
        Assert.True(M(w).Building.Relics.Single(r => r.Kind == Ruins.Cradle).Taken);
        Assert.Empty(Guards(w));
        Assert.Equal(0, Count(w, "circuit"));
    }

    [Fact]
    public void APostsWallDugOutTakesItsGuardThatTickLootlessAndLeavesTheOther()
    {
        var (w, a, b) = Posted();
        w.BesideWall();
        int wall = Post(w, Ruins.PostLeft);
        for (int i = 0; i < 16 && Guards(w).Count > 1; i++) w.Do(HewTesting.Dig(w, wall, Axe, surface: 1)).Run();
        Assert.Equal([b.Id], Guards(w).Select(g => g.Id));
        Assert.Null(M(w).Building.PostAt(wall));
        Assert.Equal(0, Count(w, "circuit"));
    }

    [Fact]
    public void ASleepingGuardWithAHeroOnlyInItsKeepBoxStaysAndNeverFlickers()
    {
        var (w, a, b) = Posted();
        for (int t = 0; t < 3 * Ward.Calm; t++)
        {
            w.Run();
            Assert.Equal([a.Id, b.Id], Guards(w).Select(g => g.Id));
        }
    }

    [Fact]
    public void ABlockedNicheWaitsForALaterBeat()
    {
        var w = Down();
        int niche = Cell(w, 3, 54);
        M(w).Grid.Place(niche, Grid.Placement.Wall());
        w.Run(2 * Tune.Beat);
        Assert.Equal([Post(w, Ruins.PostRight)], Guards(w).Select(g => CreaturesModule.PostOf(g)!.Value));
        M(w).Grid.Clear(niche, "structure");
        w.Run(Tune.Beat);
        Assert.Equal(2, Guards(w).Count);
    }

    [Fact]
    public void SavedInTheTickOfAQuietRemovalItStaysGone()
    {
        var (w, _, _) = Posted();
        w.Do(new Enter(Cell(w, 24, 55))).FellCradle();
        Assert.Empty(Guards(w));
        w.Reload();
        Assert.Empty(Guards(w));
        w.Run(2 * Tune.Beat);
        Assert.Empty(Guards(w));
    }

    [Fact]
    public void AGuardsFlightAndItsMemorySurviveALoad()
    {
        var (w, a, _) = Posted();
        w.Do(new Enter(Cell(w, 25, 55))).RunTill(() => a.Act is { Phase: ActPhase.Active });
        int id = a.Id;
        w.Reload();
        var loaded = Guards(w).Single(g => g.Id == id);
        Assert.Equal(Posture.Charge, loaded.Posture);
        Assert.NotNull(CreaturesModule.PostOf(loaded));
    }

    [Fact]
    public void ReleasingAGuardIsRefused()
    {
        var w = Down();
        Assert.Equal("creatures.notHere", M(w).Creatures.ReleaseProblem(Tune.Warden.Id, Cell(w, 25, 55)));
        w.Do(new Release(Tune.Warden.Id, Cell(w, 25, 55))).Run();
        Assert.Contains(w.Refusals, r => r.Reason == "creatures.notHere");
        Assert.Empty(Guards(w));
    }

    /// <summary>A run: its planet's seed, its ticks and the seed of what the heroes do.</summary>
    public sealed record GuardRun(int Planet, int Ticks, int Seed);

    private static Gen<GuardRun> GuardRuns() =>
        from planet in Gen.Choose(1, 1000)
        from ticks in Gen.Choose(100, 600)
        from seed in Gen.Choose(0, int.MaxValue)
        select new GuardRun(planet, ticks, seed);

    /// <summary>What must hold after a tick; <paramref name="grace"/> names the guards whose post or relic only just went.</summary>
    private static string? Broken(Scenario w, HashSet<int> grace)
    {
        var guards = Guards(w);
        var m = M(w);
        var stale = new HashSet<int>();
        foreach (var g in guards)
        {
            if (m.Building.PostAt(CreaturesModule.PostOf(g)!.Value) == null || m.Building.Relics.Single(r => r.Cell == CreaturesModule.RelicOf(g)).Taken)
            {
                if (!grace.Contains(g.Id)) stale.Add(g.Id);
                else return $"guard {g.Id} outlived its post or relic";
            }
            int half = Tune.Warden.Body.Width * Unit / 2;
            for (int y = (g.Y - Tune.Warden.Body.Height * Unit) / Unit; y <= (g.Y - 1) / Unit; y++)
                for (int x = (g.X - half) / Unit; x <= (g.X + half - 1) / Unit; x++)
                    if (x < 0 || y < 0 || x >= m.Grid.Width || y >= m.Grid.Height || m.Grid.Solid(y * m.Grid.Width + x)) return $"guard {g.Id} in solid at ({x}, {y})";
        }
        grace.Clear();
        grace.UnionWith(stale);
        if (guards.Count(g => g.Act != null) > 1) return "two siblings acting";
        if (guards.Select(g => CreaturesModule.PostOf(g)).Distinct().Count() != guards.Count) return "two guards on a post";
        return null;
    }

    [Property(MaxTest = 12)]
    public Property GuardsKeepTheirPostsTheirTurnsAndTheirBodies() =>
        Prop.ForAll(GuardRuns().ToArbitrary(), run =>
        {
            var w = Down((uint)run.Planet);
            var rng = new Rng(new RngState { S = (uint)run.Seed });
            var grace = new HashSet<int>();
            string? broken = null;
            int left = Post(w, Ruins.PostLeft), right = Post(w, Ruins.PostRight);
            for (int t = 0; t < run.Ticks && broken == null; t++)
            {
                var guards = Guards(w);
                uint pick = rng.Below(40);
                if (pick < 6) w.Do(new Enter(Cell(w, 2 + (int)rng.Below(46), 38 + (int)rng.Below(18), left)));
                else if (pick == 6) w.Do(new Enter(Cell(w, -2, 55, left)));
                else if (pick < 12 && guards.Count > 0) w.Do(new Strike(guards[(int)rng.Below((uint)guards.Count)].Id, Axe));
                else if (pick < 22 && guards.Count > 0) w.Do(new HitBy(guards[(int)rng.Below((uint)guards.Count)].Id));
                else if (pick == 22) w.Do(HewTesting.Dig(w, Cell(w, 24, 54, left), Axe, surface: 1));
                else if (pick == 23) w.Do(HewTesting.Dig(w, rng.Below(2) == 0 ? left : right, Axe, surface: 1));
                w.Run();
                broken = Broken(w, grace);
            }
            return (broken == null).Label(broken ?? "");
        });

    /// <summary>A loper's charge along the ground and into a wall, tick by tick: what the walker's flight must leave as it was.</summary>
    [Fact]
    public void ALungingWalkersRunIsUnchanged()
    {
        var w = CreaturesTesting.Planet("""
            ##############################
            #............................#
            #............................#
            #............................#
            #...........##...............#
            ##############################
            """, CreaturesTesting.Lifeless).HeroAt(20, 4);
        var c = w.Loper(2, 4).Charging(1);
        var trace = new List<string>();
        for (int t = 0; t < 45; t++)
        {
            w.Run();
            if (t % 5 == 0) trace.Add($"{c.X},{c.Y},{c.VX},{c.VY},{c.Posture}");
        }
        Assert.Equal(LungeTrace, string.Join(' ', trace));
    }

    private const string LungeTrace = "7400,10000,1400,0,Charge 14400,10000,1400,0,Charge 21400,10000,1400,0,Charge 28400,8000,1400,0,Charge 35400,10000,1400,0,Charge 42400,10000,1400,0,Charge 49600,10000,1500,0,Escape 56000,10000,1500,0,Escape 48500,10000,-1500,0,Escape";

    /// <summary>A sentry's pounce through open space, tick by tick: a walker's aim and flight are no business of a clinger's.</summary>
    [Fact]
    public void ASentrysPounceIsUnchanged()
    {
        var w = new Scenario(string.Join('\n', Enumerable.Repeat(new string('.', 120), 60)), cells: true).Do(new Enter(30 * 120 + 50)).Run();
        var debris = (Debris.DebrisState)w.M.Debris.Slice;
        debris.Pieces.Add(new Debris.Piece { Id = debris.NextId++, Zone = Content.Debris.Far.Id, Item = "engineBlock", X = 60_000, Y = 30_000 });
        SentryTests.Guarded(w);
        w.Do(new Release(Tune.Sentry.Id, 28 * 120 + 60)).Run(3);
        var c = w.M.Creatures.Creatures.Single();
        w.Do(new Strike(c.Id, Axe)).Run();
        var trace = new List<string>();
        for (int t = 0; t < 40; t++)
        {
            w.Run();
            if (t % 4 == 0) trace.Add($"{c.X},{c.Y},{c.VX},{c.VY},{c.Posture}");
        }
        Assert.Equal(SentryTrace, string.Join(' ', trace));
    }

    private const string SentryTrace = "60500,27000,0,0,WindUp 60500,27000,0,0,WindUp 60500,27000,0,0,WindUp 60500,27000,0,0,WindUp 54500,29700,-2000,900,Charge 46500,33300,-2000,900,Charge 40500,36000,0,0,Idle 46500,33300,2000,-900,Flee 54500,29700,2000,-900,Flee 60500,27000,0,0,Idle";
}
