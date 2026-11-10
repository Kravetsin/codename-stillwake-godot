using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Building;
using Stillwake.Sim.Creatures;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Xunit;
using Spec = Stillwake.Content;
using Tune = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Catchers (tracker-catchers): a hero's hidden wanted meter fills while it acts on a planet, slower under rock; full, a catcher comes for
/// it out of view, warns, hunts it through grown rock, answers another hero's blow, and leaves when its time is out, its quarry
/// gone, or dies. Every end empties the meter.
/// </summary>
public class CatchersTests
{
    private const int Unit = CreaturesModule.Unit;
    private const int W = 480, H = 48, Ground = 24;
    private static readonly Tune.Species Catcher = Tune.Catcher;
    private static readonly Tune.Hunt Hunt = (Tune.Hunt)Catcher.Brain[0];

    /// <summary>
    /// A planet's rules on drawn grown ground, a cell a character: open sky over stone from row <see cref="Ground"/>, bedrock at the
    /// bottom; <paramref name="cave"/> says which cells under ground are a cave, open with a stone back wall.
    /// </summary>
    private static Scenario Field(Func<int, int, bool>? cave = null)
    {
        var rows = new List<string>();
        var looks = new List<string>();
        for (int y = 0; y < H; y++)
        {
            var row = new char[W];
            var look = new char[W];
            for (int x = 0; x < W; x++)
            {
                bool open = y < Ground || cave?.Invoke(x, y) == true;
                row[x] = y < Ground ? ' ' : open ? '.' : '#';
                look[x] = y == H - 1 ? 'b' : y < Ground ? ' ' : 't';
            }
            rows.Add(new string(row));
            looks.Add(new string(look));
        }
        var layout = Layout.Parse(rows).WithMaterials(looks) with { Kind = Spec.Surfaces.TestStrip.Id, Grown = true, Flora = [] };
        return new Scenario(layout);
    }

    private static int Cell(int x, int y) => y * W + x;

    /// <summary>Hero <paramref name="peer"/> on the ground's top at column <paramref name="x"/>, out of creative mode.</summary>
    private static Scenario Arrive(Scenario w, int x, int peer = 0, int y = Ground - 1) =>
        w.Do(new Enter(Cell(x, y)) { From = peer }).Do(new SetCreative(false) { From = peer }).Run();

    /// <summary>Hero <paramref name="peer"/>'s meter set full, its need the least.</summary>
    private static void Full(Scenario w, int peer = 0)
    {
        var hero = w.M.Crew.HeroOf(peer)!;
        (hero.Need, hero.Wanted) = (Spec.Wanted.NeedLeast, Spec.Wanted.NeedLeast);
    }

    /// <summary>Hero <paramref name="peer"/> walking back and forth by a cell at column <paramref name="x"/> for <paramref name="ticks"/>, a step every ten.</summary>
    private static void Walk(Scenario w, int x, int y, int ticks, int peer = 0)
    {
        for (int t = 0; t < ticks; t++)
        {
            if (t % 10 == 0) w.Do(new Enter(Cell(x + t / 10 % 2, y)) { From = peer });
            w.Run();
        }
    }

    private static Creature? CatcherOf(Scenario w, int peer) =>
        w.M.Creatures.Creatures.SingleOrDefault(c => c.Species == Catcher.Id && CreaturesModule.Hunting(c) == peer);

    private static int Column(Creature c) => CreaturesModule.Down(c.X, Unit);

    /// <summary>The worth of what hero <paramref name="peer"/> carries and what lies about.</summary>
    private static long Worth(Scenario w, int peer = 0) =>
        w.M.Items.Inventory(peer).Sum(s => s == null ? 0L : (long)Spec.Market.WorthOf(s.Item) * s.Count)
        + w.M.Items.Floor.Sum(s => (long)Spec.Market.WorthOf(s.Item) * s.Count);

    [Fact]
    public void TheMeterFillsByTheWorthOfWhatTheHeroBreaksNeverByWalking()
    {
        var w = Arrive(Field(), 40);
        Walk(w, 40, Ground - 1, 400);
        Assert.Equal(0, w.M.Crew.HeroOf(0)!.Wanted);
        w.GiveItems(0, (Spec.Weapons.Pickaxe.Item, 1));
        long before = Worth(w);
        int cell = Cell(41, Ground);
        for (int i = 0; i < 100 && w.M.Grid.WallAt(cell) != null; i++) w.Do(HewTesting.Dig(w, cell, Spec.Weapons.Pickaxe.Item)).Run();
        Assert.Null(w.M.Grid.WallAt(cell));
        var hero = w.M.Crew.HeroOf(0)!;
        Assert.True(hero.Wanted > 0);
        Assert.Equal(Worth(w) - before, hero.Wanted);
        Assert.InRange(hero.Need, Spec.Wanted.NeedLeast, Spec.Wanted.NeedMost);
    }

    [Fact]
    public void ABurstFillsItsThrowersMeterAndAMeteorsNoOnes()
    {
        var w = Arrive(Arrive(Field(), 40), 10, peer: 1);
        long before = Worth(w);
        w.M.Building.Burst(1, Cell(60, Ground + 2), 400, 3, Spoils.Drop);
        Assert.True(Worth(w) > before);
        Assert.Equal(Worth(w) - before, w.M.Crew.HeroOf(1)!.Wanted);
        Assert.Equal(0, w.M.Crew.HeroOf(0)!.Wanted);
        w.M.Building.Burst(FloorStack.Nobody, Cell(80, Ground + 2), 400, 3, Spoils.Drop);
        Assert.Equal(0, w.M.Crew.HeroOf(0)!.Wanted);
    }

    [Fact]
    public void NothingFillsInCreativeMode()
    {
        var creative = Arrive(Field(), 40).Do(new SetCreative(true)).Run();
        creative.M.Crew.Loot(0, [("opal", 10)]);
        Assert.Equal(0, creative.M.Crew.HeroOf(0)!.Wanted);
    }

    [Fact]
    public void AFullMeterSoundsTheAlarmThenTheHuntAndItsCatcherComesOnlyThen()
    {
        var w = Arrive(Field(), 40);
        var hero = w.M.Crew.HeroOf(0)!;
        w.M.Crew.Loot(0, [("opal", 1)]);
        Assert.Equal(Signal.Calm, w.M.Crew.SignalOf(0));
        // Two thirds and more stay silent: nothing leaks the meter.
        hero.Wanted = hero.Need - 1;
        Assert.Equal(Signal.Calm, w.M.Crew.SignalOf(0));
        w.M.Crew.Loot(0, [("opal", 1)]);
        Assert.Equal(hero.Need, hero.Wanted);
        Assert.InRange(hero.Coming, Spec.Wanted.ComingLeast, Spec.Wanted.ComingMost);
        Assert.Equal(Signal.Coming, w.M.Crew.SignalOf(0));
        Assert.False(w.M.Crew.Hunted(0));
        int coming = hero.Coming;
        w.Run((coming - 1) * Spec.Crew.BreathEvery);
        Assert.Equal(Signal.Coming, w.M.Crew.SignalOf(0));
        Assert.Null(CatcherOf(w, 0));
        w.Run(Spec.Crew.BreathEvery);
        Assert.Equal(Signal.Hunted, w.M.Crew.SignalOf(0));
        w.Run(Tune.Beat + 1);
        Assert.NotNull(CatcherOf(w, 0));
        // Full, it takes no more: a surplus is lost, and the alarm is not drawn again.
        w.M.Crew.Loot(0, [("opal", 100)]);
        Assert.Equal((hero.Need, 0), (hero.Wanted, hero.Coming));
        Assert.Equal(Signal.Calm, w.M.Crew.SignalOf(7));
    }

    [Fact]
    public void TrackBringsTheHuntThatMuchWorthNearFillsItAtNoneAndIsRefusedWithoutAHero()
    {
        var w = Arrive(Field(), 40);
        w.Do(new Track(100)).Run();
        var hero = w.M.Crew.HeroOf(0)!;
        Assert.InRange(hero.Need, Spec.Wanted.NeedLeast, Spec.Wanted.NeedMost);
        Assert.Equal(hero.Need - 100, hero.Wanted);
        Assert.Equal(Signal.Calm, w.M.Crew.SignalOf(0));
        w.Do(new Track(0)).Run();
        Assert.Equal(Signal.Coming, w.M.Crew.SignalOf(0));
        w.Do(new Track(20) { From = 5 }).Run();
        Assert.Equal("crew.noHero", w.Refusals.Single().Reason);
    }

    [Fact]
    public void AFullMeterBringsOneCatcherForThatHeroOutOfViewThatComesAtOnce()
    {
        var w = Arrive(Arrive(Field(), 40), 10, peer: 1);
        Full(w);
        Creature? c = null;
        for (int t = 0; t <= Tune.Beat && c == null; t++)
        {
            w.Run();
            c = CatcherOf(w, 0);
        }
        Assert.NotNull(c);
        Assert.Null(CatcherOf(w, 1));
        // The left side is off the grid: it comes on the right, past the view.
        Assert.InRange(Column(c!) - 40, Tune.RingFrom, Tune.RingTo + Catcher.Body.Width);
        Assert.False(w.M.Creatures.Overlaps(Catcher.Body, c!.X, c.Y));
        // It hangs nowhere (a playtest): from its first ticks it comes at its hero.
        int x = c.X;
        w.Run(20);
        Assert.True(x - c.X >= 20 * Unit / 2, $"{x - c.X} in 20 ticks");
        Assert.Single(w.M.Creatures.Creatures, k => k.Species == Catcher.Id);
    }

    [Fact]
    public void TwoHuntedHeroesGetACatcherEach()
    {
        var w = Arrive(Arrive(Field(), 40), 440, peer: 1);
        Full(w);
        Full(w, 1);
        w.Run(Tune.Beat + 1);
        Assert.NotNull(CatcherOf(w, 0));
        Assert.NotNull(CatcherOf(w, 1));
    }

    [Fact]
    public void NoPlaceEitherSideAndTheCatcherWaitsTheMeterStayingFull()
    {
        // A hero in the middle of a field too narrow for the ring: nothing comes, and the hunt waits.
        var w = new Scenario(Layout.Parse([.. Enumerable.Repeat(new string(' ', 100), 20), new string('#', 100)]) with { Kind = Spec.Surfaces.TestStrip.Id });
        Arrive(w, 50, y: 19);
        Full(w);
        w.Run(4 * Tune.Beat);
        Assert.Empty(w.M.Creatures.Creatures);
        Assert.True(w.M.Crew.Hunted(0));
    }

    private static int Solids(Scenario w) => Enumerable.Range(0, W * H).Count(w.M.Creatures.Solid);

    private static Hero HeroOf(Scenario w, int peer = 0) => w.M.Crew.HeroOf(peer)!;

    /// <summary>Shut in a cave under rock, its hero is reached by a blink inside it, nothing broken (catcher-combat: no drill).</summary>
    [Fact]
    public void ItBlinksIntoASealedCaveItsHeroIsInBreakingNothing()
    {
        var w = Arrive(Field((x, y) => y >= Ground + 6 && y < Ground + 12 && x >= 140 && x < 170), 154, y: Ground + 9);
        int solid = Solids(w);
        w.Do(new Release(Catcher.Id, Cell(100, Ground - 1))).Run();
        var c = w.M.Creatures.Creatures.Single();
        int far = int.MaxValue;
        for (int t = 0; t < 200 && c.Act == null; t++)
        {
            w.Do(new Enter(Cell(154 + t / 10 % 2, Ground + 9)));
            w.Run();
            far = Math.Min(far, Math.Abs(Column(c) - 154));
        }
        Assert.NotNull(c.Act);
        Assert.True(w.M.Creatures.FightOf(c).Blinked > 0);
        Assert.True(far <= Hunt.Strike, $"{far} columns off at best");
        Assert.Equal(solid, Solids(w));
        Assert.Empty(w.M.Items.Floor);
    }

    /// <summary>Shut in a built hut with no room for it, its hero gets a blasting charge at the wall; the wall's cost falls (catcher-combat).</summary>
    [Fact]
    public void ShutInAHutWithNoRoomForItTheHeroGetsAChargeAtTheWall()
    {
        var w = Arrive(Field(), 40, y: Ground - 3);
        for (int y = Ground - 7; y < Ground; y++)
            foreach (int x in (int[])[37, 43])
                w.Do(new Place(Cell(x, y), Placement.Wall("scrap")));
        for (int x = 37; x <= 43; x++) w.Do(new Place(Cell(x, Ground - 7), Placement.Wall("scrap")));
        w.Run();
        w.Do(new Release(Catcher.Id, Cell(80, Ground - 1))).Run();
        var c = w.M.Creatures.Creatures.Single();
        bool lobbed = false;
        int hurts = HeroOf(w).Hurts;
        for (int t = 0; t < 120 && !w.M.Items.Floor.Any(s => s.Item == "scrapChunk"); t++)
        {
            if (t % 10 == 0) w.Do(new Enter(Cell(40, Ground - 3)));
            w.Run();
            lobbed |= w.M.Creatures.Shots.Any(s => s.Projectile == Hunt.Lob.Projectile);
        }
        Assert.True(lobbed);
        Assert.Equal(0, w.M.Creatures.FightOf(c).Blinked);
        Assert.Contains(w.M.Items.Floor, s => s.Item == "scrapChunk");
        // Its burst opened the hut and took its charge's damage off the hero in it, now in its sight.
        Assert.True(HeroOf(w).Hurts > hurts);
    }

    /// <summary>A playtest (2026-11-09): running at its hero it turned back and stamped; it never turns back while far, nor stops.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void RunningAtItsHeroItNeverTurnsBackOrStops(int hilly)
    {
        var w = Arrive(Field(), 40);
        if (hilly == 1)
            for (int x = 60; x < 135; x += 9)
                w.Do(new Place(Cell(x, Ground - 1), Placement.Wall("scrap"))).Do(new Place(Cell(x + 1, Ground - 1), Placement.Wall("scrap")));
        w.Run();
        w.Do(new Release(Catcher.Id, Cell(140, Ground - 1))).Run();
        var c = w.M.Creatures.Creatures.Single();
        int turns = 0, stops = 0, slow = 0, aloft = 0, way = 0, run = ((Tune.Flyer)Catcher.Body).Run;
        var trace = new System.Text.StringBuilder();
        for (int t = 0; t < 400 && c.Act == null; t++)
        {
            if (t % 10 == 0) w.Do(new Enter(Cell(40 + t / 10 % 2, Ground - 1)));
            w.Run();
            trace.Append($"{c.X / 1000.0:F2},{c.Y / 1000.0:F2}/{c.VX},{c.VY} ");
            if (Math.Abs(Column(c) - 40) <= Hunt.Strike + 2) continue;
            if (c.VX == 0 && c.VY == 0) stops++;
            if (Math.Abs(c.VX) < run / 2) slow++;
            if (!w.M.Creatures.Afoot(c)) aloft++;
            else if (c.VX != 0 && way != 0 && Math.Sign(c.VX) != way) turns++;
            if (c.VX != 0) way = Math.Sign(c.VX);
        }
        Assert.True(turns == 0 && stops == 0 && slow <= 2 && aloft == 0, $"turns {turns}, stops {stops}, slow {slow}, aloft {aloft}: {trace}");
    }

    [Fact]
    public void OnOpenGroundItRunsToItsHeroAndPounces()
    {
        var w = Arrive(Field(), 40);
        w.Do(new Release(Catcher.Id, Cell(140, Ground - 1))).Run();
        var c = w.M.Creatures.Creatures.Single();
        int afoot = 0, ticks = 0;
        for (; ticks < 400 && c.Act == null; ticks++)
        {
            if (ticks % 10 == 0) w.Do(new Enter(Cell(40, Ground - 1)));
            w.Run();
            if (w.M.Creatures.Afoot(c)) afoot++;
        }
        Assert.NotNull(c.Act);
        // About 90 columns at a sprint's 0.76 ticks a cell, a few searches waited; running nearly all the way.
        Assert.True(ticks < 200, $"{ticks} ticks");
        Assert.True(afoot * 10 >= ticks * 8, $"afoot {afoot} of {ticks}");
        Assert.Equal(0, w.M.Creatures.FightOf(c).Blinked);
    }

    [Fact]
    public void AWideRavineItBlinksAcrossRatherThanClimbingDownAndUp()
    {
        var w = Arrive(Field((x, y) => x >= 80 && x < 100 && y < H - 2), 40);
        w.Do(new Release(Catcher.Id, Cell(140, Ground - 1))).Run();
        var c = w.M.Creatures.Creatures.Single();
        int deepest = 0;
        for (int t = 0; t < 400 && c.Act == null; t++)
        {
            if (t % 10 == 0) w.Do(new Enter(Cell(40, Ground - 1)));
            w.Run();
            deepest = Math.Max(deepest, CreaturesModule.Down(c.Y - 1, Unit));
        }
        Assert.NotNull(c.Act);
        Assert.True(w.M.Creatures.FightOf(c).Blinked > 0);
        Assert.True(deepest < Ground + 2, $"down to row {deepest}");
    }

    /// <summary>A playtest (2026-11-09): a sprinting hero always ran past where it landed; outrun, it gathers and is there well ahead.</summary>
    [Fact]
    public void AHeroOutrunningItSeesItBlinkWellAheadOfIt()
    {
        var w = Arrive(Field(), 60, y: Ground - 3);
        w.Do(new Release(Catcher.Id, Cell(20, Ground - 1))).Run();
        var c = w.M.Creatures.Creatures.Single();
        int ahead = int.MinValue, ghost = -1;
        var trace = new System.Text.StringBuilder();
        for (int t = 0; t < 360 && c.Act == null && ahead == int.MinValue; t++)
        {
            int x = Math.Min(W - 20, 60 + (t + 1) * 3 / 2);
            w.Do(new Enter(Cell(x, Ground - 3)));
            w.Run();
            var fight = w.M.Creatures.FightOf(c);
            if (fight.Ghost >= 0) ghost = fight.Ghost;
            if (fight.Blinked == w.M.Creatures.Clock) ahead = Column(c) - x;
            if (t % 5 == 0) trace.Append($"t{t} hero {x} it {Column(c)}; ");
        }
        // Outrun at 30 cells a second, past its sprint, it gathered where it stood and was there, its ghost's spot.
        Assert.True(ahead >= 12, $"landed {ahead} ahead: {trace}");
        Assert.Equal(ghost, CreaturesModule.Down(c.Y - 1, Unit) * W + Column(c));
    }

    /// <summary>However often it lags, its blinks are at least <c>Every</c> apart, gathering still and spent after (catcher-combat).</summary>
    [Fact]
    public void ItBlinksAtMostEveryWhileGatheringStillAndSpentAfter()
    {
        var w = Arrive(Field(), 60, y: Ground - 3);
        w.Do(new Release(Catcher.Id, Cell(20, Ground - 1))).Run();
        var c = w.M.Creatures.Creatures.Single();
        var blinks = new List<int>();
        int x = 60;
        for (int t = 0; t < 900; t++)
        {
            // The hero outruns it, and steps on past it wherever it comes near.
            x += t % 2 == 0 ? 2 : 1;
            if (Math.Abs(Column(c) - x) < 25 || x > W - 20) x = Column(c) + 60 > W - 20 ? 20 : Column(c) + 60;
            w.Do(new Enter(Cell(x, Ground - 3)));
            var before = (c.X, c.Y);
            w.Run();
            var fight = w.M.Creatures.FightOf(c);
            if (fight.Ghost >= 0 && fight.GatherLeft > 0) Assert.Equal(before, (c.X, c.Y));
            if (fight.Blinked == w.M.Creatures.Clock) blinks.Add(fight.Blinked);
            else if (blinks.Count > 0 && w.M.Creatures.Clock - blinks[^1] < Hunt.Blink.Spent) Assert.Equal(before, (c.X, c.Y));
        }
        Assert.True(blinks.Count >= 2, $"{blinks.Count} blinks");
        for (int i = 1; i < blinks.Count; i++) Assert.True(blinks[i] - blinks[i - 1] >= Hunt.Blink.Every, string.Join(",", blinks));
    }

    /// <summary>Up a cliff it has no run up, it blinks onto the top near its hero and pounces (catcher-combat).</summary>
    [Fact]
    public void UpACliffItHasNoRunUpItBlinksOntoIt()
    {
        var w = Arrive(Field(), 40, y: Ground - 11);
        for (int y = Ground - 8; y < Ground; y++)
            for (int x = 30; x < 70; x++)
                w.Do(new Place(Cell(x, y), Placement.Wall("scrap")));
        w.Run();
        w.Do(new Release(Catcher.Id, Cell(130, Ground - 1))).Run();
        var c = w.M.Creatures.Creatures.Single();
        for (int t = 0; t < 300 && c.Act == null; t++)
        {
            if (t % 10 == 0) w.Do(new Enter(Cell(40, Ground - 11)));
            w.Run();
        }
        Assert.NotNull(c.Act);
        Assert.True(w.M.Creatures.FightOf(c).Blinked > 0);
        Assert.True(CreaturesModule.Down(c.Y - 1, Unit) <= Ground - 9, $"row {CreaturesModule.Down(c.Y - 1, Unit)}");
    }

    [Fact]
    public void AWallBuiltAcrossItsWayItBlinksPastAndTheWallStands()
    {
        var w = Arrive(Field(), 40);
        w.Do(new Release(Catcher.Id, Cell(140, Ground - 1))).Run();
        var c = w.M.Creatures.Creatures.Single();
        w.Run(20);
        for (int y = 0; y < Ground; y++) w.Do(new Place(Cell(90, y), Placement.Wall("scrap")));
        for (int t = 0; t < 600 && c.Act == null; t++)
        {
            if (t % 10 == 0) w.Do(new Enter(Cell(40, Ground - 1)));
            w.Run();
        }
        Assert.NotNull(c.Act);
        Assert.All(Enumerable.Range(0, Ground), y => Assert.NotNull(w.M.Grid.WallAt(Cell(90, y))));
    }

    /// <summary>On the run it aims at its hero in sight and strikes an arc at the cell it held a tell before, every while, running on.</summary>
    [Fact]
    public void OnTheRunItStrikesAnArcAtItsHeroInSightAndRunsOn()
    {
        var w = Arrive(Field(), 40, y: Ground - 3);
        w.Do(new Release(Catcher.Id, Cell(140, Ground - 1))).Run();
        var c = w.M.Creatures.Creatures.Single();
        int start = w.M.Creatures.Clock, held = 0;
        Arc? arc = null;
        bool aimed = false;
        for (int t = 0; t < 200 && c.Act == null && arc == null; t++)
        {
            if (t % 10 == 0) w.Do(new Enter(Cell(40, Ground - 3)));
            w.Run();
            var fight = w.M.Creatures.FightOf(c);
            aimed |= fight.Aims;
            if (fight.Held >= 0) held++;
            arc = w.M.Creatures.Arcs.FirstOrDefault(a => a.Left == a.Lingers);
        }
        Assert.NotNull(arc);
        Assert.True(aimed);
        // Its first shot a whole wait from the tick it came, the tick before this loop.
        Assert.True(w.M.Creatures.Clock - start >= Hunt.Gunfire.Every - 1, $"fired at {w.M.Creatures.Clock - start}");
        Assert.Equal(Hunt.Gunfire.Tell, held);
        // A beam, burning: through the cell it held, within half a cell, and on past it.
        Assert.True(arc!.Burns);
        int hx = 40 * Unit + Unit / 2, hy = (Ground - 3) * Unit + Unit / 2;
        Assert.InRange(arc.Y0 + (long)(arc.Y1 - arc.Y0) * (hx - arc.X0) / (arc.X1 - arc.X0), hy - Unit / 2, hy + Unit / 2);
        Assert.True(arc.X1 < hx - 10 * Unit, $"ends at {arc.X1 / Unit}");
        Assert.NotEqual(0, c.VX);
    }

    /// <summary>Within its pounce it never aims nor shoots: played, a laser at a hero beside it ran off anywhere.</summary>
    [Fact]
    public void WithinItsPounceItNeverAimsNorShoots()
    {
        var w = Arrive(Field(), 40);
        w.Do(new Release(Catcher.Id, Cell(40 + Hunt.Strike - 4, Ground - 1))).Run();
        var c = w.M.Creatures.Creatures.Single();
        int near = 0;
        for (int t = 0; t < 200 && w.M.Creatures.Creatures.Count > 0; t++)
        {
            if (t % 10 == 0) w.Do(new Enter(Cell(40, Ground - 1)));
            w.Run();
            if (Math.Abs(Column(c) - 40) >= Hunt.Strike - 1) continue;
            near++;
            Assert.False(w.M.Creatures.FightOf(c).Aims, $"aims {Column(c) - 40} columns off");
            Assert.DoesNotContain(w.M.Creatures.Arcs, a => a.Left == a.Lingers);
        }
        Assert.True(near > 100, $"near {near} ticks");
    }

    /// <summary>Its beam runs on past its hero to the first solid, as far as it reaches; the sentry's arc still stops at its hero.</summary>
    [Fact]
    public void ItsBeamRunsOnPastItsHeroToTheFirstWall()
    {
        var w = Arrive(Field(), 40, y: Ground - 3);
        int wall = 25;
        for (int y = 0; y < Ground; y++) w.M.Grid.Place(Cell(wall, y), Placement.Wall("bedrock"));
        w.Do(new Release(Catcher.Id, Cell(140, Ground - 1))).Run();
        var c = w.M.Creatures.Creatures.Single();
        Arc? arc = null;
        for (int t = 0; t < 200 && c.Act == null && arc == null; t++)
        {
            if (t % 10 == 0) w.Do(new Enter(Cell(40, Ground - 3)));
            w.Run();
            arc = w.M.Creatures.Arcs.FirstOrDefault(a => a.Left == a.Lingers);
        }
        Assert.NotNull(arc);
        Assert.InRange(CreaturesModule.Down(arc!.X1, Unit), wall, wall + 1);
    }

    /// <summary>
    /// Its salvo under open sky: it rises its hover into the sky, lobs its shells over the spread round its hero, each marked on the floor where it
    /// lands; their bursts break a little ground and hurt a hero within reach, not one past it; then it falls and runs on.
    /// </summary>
    [Fact]
    public void ItsSalvoLobsMarkedShellsRoundItsHeroWhoseBurstsBreakAndHurt()
    {
        var salvo = Hunt.Salvo;
        var w = Arrive(Arrive(Field(), 40, y: Ground - 3), 120, peer: 1, y: Ground - 3);
        w.Do(new Release(Catcher.Id, Cell(70, Ground - 1))).Run();
        var c = w.M.Creatures.Creatures.Single();
        int y0 = c.Y, ground = Solids(w), hurts = HeroOf(w).Hurts, other = HeroOf(w, 1).Hurts;
        c.Keep(HuntDrive.SalvoAt, w.M.Creatures.Clock);
        for (int t = 0; t < salvo.Hover + 3 && !w.M.Creatures.MarksOf(c).Any(); t++) w.Run();
        // Its hover, or as high as the grid's top lets it.
        Assert.Equal(Math.Max(Catcher.Body.Height * Unit, y0 - salvo.Hover * Unit), c.Y);
        var marks = w.M.Creatures.MarksOf(c).Select(k => k.Cell).ToList();
        Assert.Equal(salvo.Shells, marks.Count);
        // They land one after another, each Stagger after the one before.
        Assert.Equal(salvo.Stagger, w.M.Creatures.MarksOf(c).ElementAt(1).Left - w.M.Creatures.MarksOf(c).ElementAt(0).Left);
        Assert.Equal(salvo.Shells, w.M.Creatures.Shots.Count(s => s.Projectile == salvo.Projectile));
        Assert.All(marks, m => Assert.True(w.M.Creatures.Solid(m + W) && !w.M.Creatures.Solid(m)));
        Assert.Equal(salvo.Spread, marks.Max(m => m % W) - marks.Min(m => m % W));
        w.Run(salvo.Flight + (salvo.Shells - 1) * salvo.Stagger + 2);
        Assert.DoesNotContain(w.M.Creatures.Shots, s => s.Projectile == salvo.Projectile);
        Assert.Empty(w.M.Creatures.MarksOf(c));
        Assert.True(Solids(w) < ground);
        Assert.True(HeroOf(w).Hurts > hurts);
        Assert.Equal(other, HeroOf(w, 1).Hurts);
        w.Run(salvo.Hover + 2);
        Assert.Equal(0, c.Recall(HuntDrive.Salvo));
        Assert.Equal(y0, c.Y);
    }

    [Fact]
    public void UnderARoofItThrowsNoSalvoTillItIsOutFromUnderIt()
    {
        var w = Arrive(Field(), 40, y: Ground - 3);
        for (int x = 65; x < 95; x++) w.Do(new Place(Cell(x, Ground - 7), Placement.Wall("scrap")));
        w.Run();
        w.Do(new Release(Catcher.Id, Cell(85, Ground - 1))).Run();
        var c = w.M.Creatures.Creatures.Single();
        c.Keep(HuntDrive.SalvoAt, w.M.Creatures.Clock);
        w.Run();
        Assert.Equal(0, c.Recall(HuntDrive.Salvo));
        for (int t = 0; t < 30 && c.Recall(HuntDrive.Salvo) == 0; t++) w.Run();
        Assert.NotEqual(0, c.Recall(HuntDrive.Salvo));
        Assert.True(Column(c) < 65, $"column {Column(c)}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AWorldSavedMidGatherOrMidSalvoLoadsAndPlaysOnAlike(bool salvo)
    {
        // The first planet, saved as its edits; its catcher made to lag, or its salvo due.
        var w = new Scenario("......\n.S....\n######");
        w.Do(new Enter(w.M.Crew.Spawn)).Run();
        w.Do(new OpenSurface(Spec.PlanetTypes.First.Id)).Run();
        w.Do(new Transfer(1)).Run();
        var hero = w.On(1).Crew.HeroOf(0)!;
        (hero.Need, hero.Wanted) = (Spec.Wanted.NeedLeast, Spec.Wanted.NeedLeast);
        w.Run(Tune.Beat + 1);
        var m = w.On(1).Creatures;
        var c = m.Creatures.Single(k => k.Species == Catcher.Id);
        string key = salvo ? HuntDrive.Salvo : HuntDrive.BlinkTo;
        for (int t = 0; t < 600 && c.Recall(key) == 0 && (salvo || c.Act == null); t++)
        {
            if (salvo) c.Keep(HuntDrive.SalvoAt, m.Clock);
            else
            {
                c.Keep(HuntDrive.LagClock, 1);
                c.Keep(HuntDrive.Closest, 1);
            }
            w.Run();
        }
        w.Run(2);
        Assert.NotEqual(0, c.Recall(key));
        var twin = new Scenario("......\n.S....\n######").Reload(w.World.Save());
        w.Run(60);
        twin.Run(60);
        Assert.Equal(w.World.Save(), twin.World.Save());
    }

    [Fact]
    public void TheSearchGivesTheSamePathFromAFreshMemoryAndAUsedOne()
    {
        var w = Arrive(Field((x, y) => y >= Ground + 6 && y < Ground + 12 && x >= 150 && x < 160), 40);
        var m = w.M.Creatures;
        var f = (Tune.Flyer)Catcher.Body;
        var used = new PathFinder();
        var scratch = new List<int>();
        used.Find(m, f, 60, Ground - 1, 154, Ground + 11, scratch);
        used.Find(m, f, 200, Ground - 1, 20, Ground - 1, scratch);
        var a = new List<int>();
        var b = new List<int>();
        used.Find(m, f, 120, Ground - 1, 154, Ground + 11, a);
        new PathFinder().Find(m, f, 120, Ground - 1, 154, Ground + 11, b);
        Assert.NotEmpty(a);
        Assert.Equal(b, a);
    }

    /// <summary>
    /// On drawn ground of rock, built walls and bedrock, every step a path takes is a run the body makes: a column across, at most a step
    /// up or down, standing free on ground; nothing broken on its way (catcher-combat: no drill).
    /// </summary>
    [Property(MaxTest = 40)]
    public Property EveryStepOfAPathIsARunTheBodyCanTake() => Prop.ForAll(
        Gen.Choose(0, 9).ArrayOf(W / 8).ToArbitrary(),
        Gen.Choose(0, 400).Two().ToArbitrary(),
        (columns, ends) =>
        {
            var w = Arrive(Field(), 5);
            for (int i = 0; i < columns.Length; i++)
            {
                string? wall = columns[i] switch { 0 => "bedrock", 1 or 2 => "scrap", _ => null };
                if (wall == null) continue;
                for (int y = Ground - 1 - columns[i] % 4; y < Ground; y++) w.Do(new Place(Cell(i * 8 + 4, y), Placement.Wall(wall)));
            }
            w.Run();
            var m = w.M.Creatures;
            var f = (Tune.Flyer)Catcher.Body;
            var path = new List<int>();
            int sx = 20 + ends.Item1 % 440, gx = 20 + ends.Item2 % 440;
            // A catcher never stands in a wall: it moves only where its body fits.
            if (m.Overlaps(f, m.Middle(sx), Ground * Unit)) return true;
            m.Paths.Find(m, f, sx, Ground - 1, gx, Ground - 1, path);
            int px = sx, py = Ground - 1;
            foreach (int step in path)
            {
                int x = step / 4 % W, y = step / 4 / W;
                if ((StepKind)(step % 4) != StepKind.Run || Math.Abs(x - px) != 1 || Math.Abs(y - py) > Tune.StepUp) return false;
                if (m.Overlaps(f, m.Middle(x), (y + 1) * Unit) || !m.Grounded(f, x, y)) return false;
                (px, py) = (x, y);
            }
            return true;
        });

    [Fact]
    public void WalledInByBedrockTheHeroOutlastsItAndItLeavesTheMeterEmptied()
    {
        var w = Arrive(Field(), 40);
        // A box of bedrock round the hero and under it: nothing breaks it.
        for (int y = Ground - 7; y < Ground; y++)
            foreach (int x in (int[])[37, 43])
                w.Do(new Place(Cell(x, y), Placement.Wall("bedrock")));
        for (int x = 37; x <= 43; x++) w.Do(new Place(Cell(x, Ground - 7), Placement.Wall("bedrock")));
        for (int x = 37; x <= 43; x++)
            for (int y = Ground; y < H - 1; y++)
            {
                w.M.Grid.Clear(Cell(x, y), "structure");
                w.M.Grid.Place(Cell(x, y), Placement.Wall("bedrock"));
            }
        w.Run();
        Full(w);
        w.Run(Tune.Beat + 1);
        var c = CatcherOf(w, 0)!;
        // Never in its sight, its hero draws no arc; its charges burst on bedrock, out of the hero's sight.
        int hurts = HeroOf(w).Hurts;
        for (int t = 0; t < Hunt.Ticks; t++)
        {
            w.Run();
            Assert.DoesNotContain(w.M.Creatures.Arcs, a => a.Left == a.Lingers);
        }
        Assert.Equal(hurts, HeroOf(w).Hurts);
        Assert.All(Enumerable.Range(Ground - 7, 7), y => Assert.NotNull(w.M.Grid.WallAt(Cell(43, y))));
        Assert.Null(CreaturesModule.Hunting(c));
        Assert.Equal((0, 0), (w.M.Crew.HeroOf(0)!.Wanted, w.M.Crew.HeroOf(0)!.Need));
        // It flies off up and is gone once out of view.
        w.Run(200);
        Assert.DoesNotContain(w.M.Creatures.Creatures, k => k.Id == c.Id);
    }

    [Fact]
    public void KilledItGivesItsYieldsAndItsPurseEmptiesItsQuarrysMeterAndFillsItsKillers()
    {
        var w = Arrive(Arrive(Field(), 40), 10, peer: 1);
        Full(w);
        w.Run(Tune.Beat + 1);
        var c = CatcherOf(w, 0)!;
        c.Damage = Catcher.Life.Hp - 1;
        int credits = w.M.Knowledge.Credits;
        long before = Worth(w, peer: 1);
        w.Do(new Strike(c.Id, Spec.Weapons.Axe.Item) { From = 1 }).Run();
        Assert.DoesNotContain(w.M.Creatures.Creatures, k => k.Id == c.Id);
        Assert.Contains(w.M.Items.Floor, s => s.Item == "circuit");
        Assert.InRange(w.M.Knowledge.Credits - credits, Catcher.Purse!.Min, Catcher.Purse.Max);
        Assert.Equal(0, w.M.Crew.HeroOf(0)!.Need);
        Assert.Equal(Worth(w, peer: 1) - before, w.M.Crew.HeroOf(1)!.Wanted);
    }

    [Fact]
    public void ItsQuarryPassedOutOrGoneItLeavesTheMeterAlreadyEmptied()
    {
        var w = Arrive(Field(), 40);
        Full(w);
        w.Run(Tune.Beat + 1);
        var c = CatcherOf(w, 0)!;
        w.M.Crew.Shock(0, 1000, Harm.Blow);
        Assert.Equal(0, w.M.Crew.HeroOf(0)!.Need);
        w.Run();
        Assert.Null(CreaturesModule.Hunting(c));
        // Departing the surface empties it too: launching home is an escape.
        var hero = Arrive(Field(), 40).M.Crew;
        var h = hero.HeroOf(0)!;
        (h.Need, h.Wanted) = (1500, 1500);
        var left = (Hero)hero.Depart(0)!;
        Assert.Equal((0, 0), (left.Wanted, left.Need));
    }

    [Fact]
    public void AnotherHerosBlowIsAnsweredOnceAnAnswerAtItThenItTurnsBack()
    {
        var w = Arrive(Arrive(Field(), 40), 10, peer: 1);
        Full(w);
        w.Run(Tune.Beat + 1);
        var c = CatcherOf(w, 0)!;
        w.Do(new Strike(c.Id, Spec.Weapons.Axe.Item) { From = 0 }).Run();
        Assert.Null(c.Act);
        w.Do(new Strike(c.Id, Spec.Weapons.Axe.Item) { From = 1 }).Run();
        Assert.Equal(1, c.Act?.Move);
        Assert.Equal(Ref.Hero(1), c.Target);
        // Its arc over, it hunts its quarry again; another blow within AnswerEvery goes unanswered.
        w.Run(((Tune.Shoot)Catcher.Moves[1]).WindUp + ((Tune.Shoot)Catcher.Moves[1]).Recover + 1);
        Assert.Null(c.Act);
        Assert.Equal(Ref.Hero(0), c.Target);
        w.Do(new Strike(c.Id, Spec.Weapons.Axe.Item) { From = 1 }).Run();
        Assert.Null(c.Act);
        // Teleporting, it may reach its quarry by now: an answer waits for its pounce to end.
        w.Run(Hunt.AnswerEvery);
        while (c.Act != null) w.Run();
        w.Do(new Strike(c.Id, Spec.Weapons.Axe.Item) { From = 1 }).Run();
        Assert.Equal(1, c.Act?.Move);
    }

    [Fact]
    public void ACatcherLetOutHuntsItsSender()
    {
        var w = Arrive(Arrive(Field(), 40), 10, peer: 1);
        w.Do(new Release(Catcher.Id, Cell(100, Ground - 1)) { From = 1 }).Run();
        Assert.Equal(1, CreaturesModule.Hunting(w.M.Creatures.Creatures.Single()));
    }

    [Fact]
    public void AWorldSavedMidHuntLoadsAndPlaysOnAlike()
    {
        // The first planet: wide enough for the ring, grown, saved as its edits.
        var w = new Scenario("......\n.S....\n######");
        w.Do(new Enter(w.M.Crew.Spawn)).Run();
        w.Do(new OpenSurface(Spec.PlanetTypes.First.Id)).Run();
        w.Do(new Transfer(1)).Run();
        var hero = w.On(1).Crew.HeroOf(0)!;
        (hero.Need, hero.Wanted) = (Spec.Wanted.NeedLeast, Spec.Wanted.NeedLeast);
        w.Run(Tune.Beat + 1);
        Assert.Single(w.On(1).Creatures.Creatures, c => c.Species == Catcher.Id);
        foreach (int ticks in (int[])[100, 300])
        {
            string save = w.World.Save();
            var twin = new Scenario("......\n.S....\n######").Reload(save);
            w.Run(ticks);
            twin.Run(ticks);
            Assert.Equal(w.World.Save(), twin.World.Save());
        }
    }

    /// <summary>
    /// The meter, over a hero walking about and now and then given loot, its need drawn anew as it is called off: never past its need,
    /// never growing but by loot, the alarm only on a full meter, and hunted only when full with the alarm run out.
    /// </summary>
    [Property(MaxTest = 20)]
    public Property TheMeterNeverPassesItsNeedNorGrowsButByLoot() =>
        Prop.ForAll(Gen.Choose(0, int.MaxValue).ToArbitrary(), seed =>
        {
            var w = Arrive(Field((x, y) => y >= Ground + 2 && y < Ground + 8 && x < 80), 40);
            var rng = new Rng(new RngState { S = (uint)seed });
            string[] goods = ["rock", "copperOre", "opal", "crystal"];
            for (int t = 0; t < 1500; t++)
            {
                var hero = w.M.Crew.HeroOf(0)!;
                if (rng.Below(4) == 0) w.Do(new Enter(Cell(30 + (int)rng.Below(20), rng.Below(2) == 0 ? Ground - 1 : Ground + 7)));
                if (rng.Below(300) == 0) w.M.Crew.CallOff(0);
                int before = hero.Wanted;
                w.Run();
                if (hero.Wanted != before) return false.Label($"grew by walking at {t}");
                if (rng.Below(10) == 0) w.M.Crew.Loot(0, [(goods[rng.Below(4)], 1 + (int)rng.Below(40))]);
                bool full = hero.Need > 0 && hero.Wanted == hero.Need;
                if (hero.Wanted > hero.Need) return false.Label($"{hero.Wanted} over {hero.Need} at {t}");
                if (hero.Coming > 0 && !full) return false.Label($"an alarm on a meter not full at {t}");
                if (w.M.Crew.Hunted(0) != (full && hero.Coming == 0)) return false.Label($"hunted wrong at {t}");
            }
            return true.ToProperty();
        });

    // In space (catchers-space).

    private const int SW = 600, SH = 140;

    /// <summary>The ship's rules on open space, a cell a character, <see cref="SW"/> by <see cref="SH"/>; <paramref name="draw"/> says which cells hold another character.</summary>
    private static Scenario Space(Func<int, int, char?>? draw = null) =>
        new(Layout.Parse([.. Enumerable.Range(0, SH).Select(y => new string([.. Enumerable.Range(0, SW).Select(x => draw?.Invoke(x, y) ?? ' ')]))]));

    private static int At(int x, int y) => y * SW + x;

    /// <summary>Hero <paramref name="peer"/> at (<paramref name="x"/>, <paramref name="y"/>) in space, out of creative mode.</summary>
    private static Scenario Afloat(Scenario w, int x, int y, int peer = 0) =>
        w.Do(new Enter(At(x, y)) { From = peer }).Do(new SetCreative(false) { From = peer }).Run();

    /// <summary>A sealed room of hull round (<paramref name="x"/>, <paramref name="y"/>): eleven cells across inside, eight down.</summary>
    private static char? Room(int cx, int cy, int x, int y) =>
        Math.Abs(x - cx) > 6 || y < cy - 7 || y > cy + 2 ? null : Math.Abs(x - cx) == 6 || y == cy - 7 || y == cy + 2 ? '#' : '.';

    [Fact]
    public void InSpaceTheMeterFillsAnywhereAndDrainsInTheStartingBeltTillFull()
    {
        var w = Afloat(Space(), 20, SH / 2);
        var hero = HeroOf(w);
        w.M.Crew.Loot(0, [("opal", 1)]);
        Assert.Equal(Spec.Market.WorthOf("opal"), hero.Wanted);
        w.Run(3 * Spec.Crew.BreathEvery);
        Assert.Equal(Spec.Market.WorthOf("opal"), hero.Wanted);
        // In the belt round the grid's middle it drains a breath at a time, to none.
        w.Do(new Enter(At(SW / 2 + Spec.Wanted.Belt.X, SH / 2))).Run(3 * Spec.Crew.BreathEvery - 1);
        Assert.Equal(Spec.Market.WorthOf("opal") - 3 * Spec.Wanted.Drain, hero.Wanted);
        w.Run(Spec.Crew.BreathEvery * Spec.Market.WorthOf("opal") / Spec.Wanted.Drain);
        Assert.Equal(0, hero.Wanted);
        // Full, it stays full: the catcher is on its way.
        Full(w);
        w.Run(3 * Spec.Crew.BreathEvery);
        Assert.Equal(hero.Need, hero.Wanted);
    }

    [Fact]
    public void TakingTheShipApartFillsNothing()
    {
        var w = Afloat(Space((x, y) => x == 22 && y == SH / 2 ? '#' : null), 20, SH / 2);
        w.Do(new Deconstruct(At(22, SH / 2), "structure")).Run();
        int plan = Assert.Single(w.M.Building.Plans).Id;
        for (int t = 0; t < 2000 && w.M.Grid.Solid(At(22, SH / 2)); t++) w.Do(new Work(plan)).Run();
        Assert.Empty(w.Refusals);
        Assert.False(w.M.Grid.Solid(At(22, SH / 2)));
        Assert.Equal(0, HeroOf(w).Wanted);
    }

    [Fact]
    public void InSpaceItComesOutOfViewInOpenSpaceAndFliesStraightAtItsHero()
    {
        var w = Afloat(Space(), 100, SH / 2);
        Full(w);
        w.Run(Tune.Beat + 1);
        var c = CatcherOf(w, 0)!;
        Assert.NotNull(c);
        var m = w.M.Creatures;
        Assert.InRange(Column(c) - 100, Tune.RingFrom, Tune.RingTo + Catcher.Body.Width);
        Assert.True(m.InSpace(Catcher.Body, c.X, c.Y));
        int far = Column(c) - 100, ticks = 0;
        for (; ticks < 400 && c.Act == null; ticks++)
        {
            w.Run();
            Assert.False(m.Overlaps(Catcher.Body, c.X, c.Y));
        }
        // No path nor blink: straight at it a cell a tick, and its pounce from its strike's reach.
        Assert.NotNull(c.Act);
        Assert.Equal(0, m.FightOf(c).Blinked);
        Assert.InRange(ticks, far - Hunt.Strike - Catcher.Body.Width, far - Hunt.Strike + Catcher.Body.Width);
    }

    [Fact]
    public void AHullPlateInItsWayItBlinksPast()
    {
        var w = Afloat(Space((x, y) => x >= 160 && x < 164 && y >= SH / 2 - 30 && y < SH / 2 + 30 ? '#' : null), 100, SH / 2);
        Full(w);
        w.Run(Tune.Beat + 1);
        var c = CatcherOf(w, 0)!;
        for (int t = 0; t < 600 && Column(c) >= 160; t++) w.Run();
        Assert.True(Column(c) < 160, $"column {Column(c)}");
        Assert.NotEqual(0, w.M.Creatures.FightOf(c).Blinked);
        Assert.True(w.M.Grid.Solid(At(161, SH / 2)));
    }

    [Fact]
    public void ShutInARoomAboardItsHeroGetsAChargeThroughTheHullAndTheRoomVents()
    {
        int x = 100, y = SH / 2;
        var w = Afloat(Space((cx, cy) => Room(x, y, cx, cy)), x, y);
        Assert.True(w.M.Rooms.RoomAt(At(x, y)) >= 0);
        Full(w);
        w.Run(Tune.Beat + 1);
        var c = CatcherOf(w, 0)!;
        bool lobbed = false;
        for (int t = 0; t < 1200 && w.M.Rooms.RoomAt(At(x, y)) >= 0; t++)
        {
            w.Run();
            lobbed |= w.M.Creatures.Shots.Any(s => s.Projectile == Hunt.Lob.Projectile);
        }
        Assert.True(lobbed);
        Assert.Equal(0, w.M.Creatures.FightOf(c).Blinked);
        Assert.Equal(Stillwake.Sim.Rooms.RoomsModule.Space, w.M.Rooms.RoomAt(At(x, y)));
    }

    /// <summary>Whatever its hero does among hull plates, every cell of a hunting catcher's body in space is in open space: never in a plate nor a room.</summary>
    [Property(MaxTest = 20)]
    public Property InSpaceItsBodyIsAlwaysInOpenSpace() => Prop.ForAll(
        Gen.Choose(0, int.MaxValue).ToArbitrary(),
        seed =>
        {
            var rng = new Rng(new RngState { S = (uint)seed });
            var plates = Enumerable.Range(0, 60).Select(_ => (X: 20 + (int)rng.Below(SW - 40), Y: 10 + (int)rng.Below(SH - 20), Long: 4 + (int)rng.Below(30), Up: rng.Below(2) == 0)).ToArray();
            var w = Afloat(Space((x, y) => plates.Any(p => p.Up ? x == p.X && y >= p.Y && y < p.Y + p.Long : y == p.Y && x >= p.X && x < p.X + p.Long) ? '#' : null), SW / 2, SH / 2);
            Full(w);
            var m = w.M.Creatures;
            for (int t = 0; t < 900; t++)
            {
                if (t % 40 == 0) w.Do(new Enter(At(SW / 2 - 150 + (int)rng.Below(300), SH / 2 - 40 + (int)rng.Below(80))));
                w.Run();
                foreach (var c in m.Creatures)
                {
                    if (CreaturesModule.Hunting(c) == null) continue;
                    if (m.Overlaps(Catcher.Body, c.X, c.Y)) return false.Label($"in a wall at {t}");
                    if (!m.InSpace(Catcher.Body, c.X, c.Y)) return false.Label($"out of space at {t}");
                }
            }
            return true.ToProperty();
        });
}
