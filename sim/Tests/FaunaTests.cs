using Stillwake.Sim.Fauna;
using Stillwake.Sim.Items;
using Xunit;
using Spec = Stillwake.Content;
using Tune = Stillwake.Content.Fauna;

namespace Stillwake.Sim.Tests;

/// <summary>Creatures (0102): how a loper walks, takes hits and dies, and where and when creatures spawn and go.</summary>
public class FaunaTests
{
    /// <summary>A cell, and a character of a doubled map: two cells across and down (0104).</summary>
    private const int Unit = FaunaModule.Unit, Cell = 2 * Unit;
    private static readonly string Axe = Spec.Weapons.Axe.Item, Drill = Spec.Weapons.Drill.Item;
    private static readonly Tune.Species Loper = Tune.Loper;
    private static readonly int Belt = Spec.Inventory.Slots + 2;
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();

    /// <summary>A room 12 wide, its floor row 4: the side walls taller than a step.</summary>
    private const string Room = """
        ############
        #..........#
        #..........#
        #..........#
        ############
        """;

    /// <summary>A room whose floor is a ledge three rows over the floor at its right: too deep a drop to walk off.</summary>
    private const string Ledge = """
        ##############
        #............#
        #............#
        #######......#
        #######......#
        #######......#
        ##############
        """;

    /// <summary>The same ledge two rows over the floor: walked off.</summary>
    private const string Low = """
        ##############
        #............#
        #............#
        #######......#
        #######......#
        ##############
        """;

    /// <summary>A floor with a step one cell high at column 7, then the room's side wall.</summary>
    private const string Step = """
        ##############
        #............#
        #............#
        #............#
        #......#######
        ##############
        """;

    private static Creature Only(Scenario w) => Assert.Single(w.M.Fauna.Creatures);

    /// <summary>Whether <paramref name="c"/>'s body is clear of every wall, in the grid.</summary>
    private static bool Clear(Scenario w, Creature c) => !w.M.Fauna.Overlaps(Tune.Get(c.Species), c.X, c.Y);

    // Walking.

    [Fact]
    public void ALoperLetOutFallsToTheFloorAndStandsOnIt()
    {
        var w = FaunaTesting.Planet(Room).HeroAt(1, 3);
        var c = w.Loper(3, 2).Still();
        w.Run(30);
        Assert.Equal((4 * Cell, 0), (c.Y, c.VY));
        Assert.Equal(3 * Cell + Cell, c.X);
    }

    [Theory]
    [InlineData(Mode.Wander)]
    [InlineData(Mode.Flee)]
    public void AWalkerTurnsBackAtAWallItCannotStepUpNeverEnteringIt(Mode mode)
    {
        var w = FaunaTesting.Planet(Room).HeroAt(1, 3);
        var c = w.Loper(2, 3).Walking(+1, mode);
        int most = 0;
        bool turned = false;
        for (int t = 0; t < 200; t++)
        {
            w.Run();
            most = Math.Max(most, c.X);
            turned |= c.VX < 0;
            Assert.True(Clear(w, c));
        }
        Assert.True(turned);
        // Flush with the wall at column 11: its right edge at its left.
        Assert.Equal(11 * Cell - Cell, most);
        Assert.Equal(4 * Cell, c.Y);
    }

    [Theory]
    [InlineData(Mode.Wander)]
    [InlineData(Mode.Flee)]
    public void AWalkerTurnsBackFromADropOfThreeRowsAtTheLedgesEdge(Mode mode)
    {
        var w = FaunaTesting.Planet(Ledge).HeroAt(8, 5);
        var c = w.Loper(2, 2).Walking(+1, mode);
        int most = 0;
        for (int t = 0; t < 200; t++)
        {
            w.Run();
            most = Math.Max(most, c.X + Loper.Width * Unit / 2);
            Assert.Equal(3 * Cell, c.Y);
        }
        // Its leading edge reaches the ledge's edge at column 7 and goes no farther.
        Assert.True(most <= 7 * Cell, $"its edge reached {most}");
        int speed = mode == Mode.Flee ? Loper.Flee : Loper.Wander;
        Assert.True(most > 7 * Cell - speed - 1, $"it turned early, at {most}");
    }

    [Fact]
    public void AWalkerWalksOffADropOfTwoRowsAndLandsBelow()
    {
        var w = FaunaTesting.Planet(Low).HeroAt(8, 4);
        var c = w.Loper(2, 2).Walking(+1);
        w.Run(120);
        Assert.Equal(5 * Cell, c.Y);
        Assert.True(c.X > 7 * Cell);
        Assert.True(Clear(w, c));
    }

    [Theory]
    [InlineData(Mode.Wander)]
    [InlineData(Mode.Flee)]
    public void AWalkerStepsUpAStepOneCellHighOnItsWay(Mode mode)
    {
        var w = FaunaTesting.Planet(Step).HeroAt(1, 4);
        var c = w.Loper(2, 4).Walking(+1, mode);
        bool up = false;
        for (int t = 0; t < 120; t++)
        {
            w.Run();
            up |= c.Y == 4 * Cell && c.X > 7 * Cell + Cell;
            Assert.True(Clear(w, c));
        }
        Assert.True(up, "it never stood on the step");
    }

    [Theory]
    [InlineData(Mode.Wander)]
    [InlineData(Mode.Flee)]
    public void AWalkerStepsDownAStepOneCellDeepWithoutFalling(Mode mode)
    {
        var w = FaunaTesting.Planet("""
            ##############
            #............#
            #............#
            #............#
            #######......#
            ##############
            """).HeroAt(8, 4);
        var c = w.Loper(2, 3).Walking(+1, mode);
        bool down = false;
        for (int t = 0; t < 120; t++)
        {
            w.Run();
            Assert.Equal(0, c.VY);
            down |= c.Y == 5 * Cell;
        }
        Assert.True(down, "it never stood below the step");
    }

    /// <summary>A room a cell to a character, 30 wide, its floor row 10, a step <paramref name="high"/> cells high from column 15 to its side wall.</summary>
    private static Layout StepOf(int high) => Layout.Parse([.. Enumerable.Range(0, 11).Select(y => new string([.. Enumerable.Range(0, 30)
        .Select(x => y == 0 || y == 10 || x == 0 || x == 29 || x >= 15 && y >= 10 - high ? '#' : '.')]))]);

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public void AWalkerStepsUpTwoCellsButNotThreeWithoutAJump(int high, bool steps)
    {
        var w = FaunaTesting.Planet(StepOf(high), cells: true).HeroAt(1, 9);
        var c = w.Loper(5, 9).Walking(+1);
        bool up = false, jumped = false;
        for (int t = 0; t < 200 && !up; t++)
        {
            w.Run();
            jumped |= c.VY < 0;
            up |= c.Y == (10 - high) * Unit && c.X - Loper.Width * Unit / 2 >= 15 * Unit;
            Assert.True(Clear(w, c));
        }
        // Up a step of three it leaps, as up a ledge (WanderLedge), never walks.
        Assert.Equal(steps, up && !jumped);
    }

    [Fact]
    public void FasterThanACellATickACreatureNeverPassesAWallOrAFloorOneCellThick()
    {
        // Fleeing at 1500 a tick and falling at most 1200, it moves in parts of under a cell (0104).
        Assert.True(Loper.Flee > Unit && Tune.MaxFall > Unit);
        var w = FaunaTesting.Planet(Layout.Parse([.. Enumerable.Range(0, 8).Select(y => new string([.. Enumerable.Range(0, 30)
            .Select(x => y == 0 || y == 7 || x == 0 || x == 29 || x == 15 ? '#' : '.')]))]), cells: true).HeroAt(1, 6);
        var c = w.Loper(5, 6).Walking(+1, Mode.Flee);
        int most = 0;
        for (int t = 0; t < 60; t++)
        {
            w.Run();
            most = Math.Max(most, c.X + Loper.Width * Unit / 2);
            Assert.True(Clear(w, c));
        }
        Assert.Equal(15 * Unit, most);

        var shaft = FaunaTesting.Planet(Layout.Parse([.. Enumerable.Range(0, 40).Select(y => new string([.. Enumerable.Range(0, 12)
            .Select(x => y == 0 || y == 39 || x == 0 || x == 11 || y == 30 ? '#' : '.')]))]), cells: true).HeroAt(1, 29);
        var falling = shaft.Loper(4, 4).Still();
        int fastest = 0;
        for (int t = 0; t < 60; t++)
        {
            fastest = Math.Max(fastest, falling.VY);
            shaft.Run();
            Assert.True(Clear(shaft, falling));
        }
        Assert.Equal(Tune.MaxFall, fastest);
        Assert.Equal((30 * Unit, 0), (falling.Y, falling.VY));
    }

    // Jumps.

    /// <summary>
    /// Ground 80 wide and 16 tall, its top at row 9 under eight open rows and a ceiling, with a wall <paramref name="wall"/> cells
    /// high standing on it at column 40, or a pit <paramref name="gap"/> wide from column 40 down to the bottom row.
    /// </summary>
    private static Layout Course(int wall = 0, int gap = 0) =>
        Layout.Parse([.. Enumerable.Range(0, 16).Select(y => new string([.. Enumerable.Range(0, 80).Select(x =>
            y == 0 || x == 0 || x == 79 || y == 15 ? '#'
            : y >= 9 ? (gap > 0 && x >= 40 && x < 40 + gap ? '.' : '#')
            : wall > 0 && x == 40 && y >= 9 - wall ? '#' : '.')]))]);

    /// <summary>A loper fleeing right from column 30 on the course, its run till it calms, and whether it ever stood past column 40 plus <paramref name="past"/>.</summary>
    private static (Creature C, bool Over, bool Fell) Flee(Layout course, int past)
    {
        var w = FaunaTesting.Planet(course, FaunaTesting.Lifeless).Breathing().HeroAt(20, 8);
        var c = w.Loper(30, 8).Walking(+1, Mode.Flee);
        c.ModeLeft = 200;
        bool over = false, fell = false;
        for (int t = 0; t < 200; t++)
        {
            w.Run();
            Assert.True(Clear(w, c), $"in a wall at ({c.X}, {c.Y})");
            over |= c.X - Loper.Width * Unit / 2 >= (40 + past) * Cell && c.VY == 0;
            fell |= c.Y > 9 * Cell;
        }
        return (c, over, fell);
    }

    [Fact]
    public void ALopersJumpIsAboutNineCellsHighAndThirtyFourLongOnTheFirstPlanet()
    {
        int g = Content.PlanetTypes.First.Fall(Tune.Gravity);
        Assert.InRange(Leaps.Height(Loper.Jump, g), 8_000, 9_200);
        Assert.InRange(Leaps.Length(Loper.Jump, Loper.JumpAcross, g), 30_000, 36_000);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void AFleeingLoperJumpsAWallItClears(int high)
    {
        var (_, over, fell) = Flee(Course(wall: high), 1);
        Assert.True(over, $"it never got over a wall {high} high");
        Assert.False(fell);
    }

    [Fact]
    public void AFleeingLoperTurnsAtAWallTooHighToClear()
    {
        var (c, over, _) = Flee(Course(wall: 6), 1);
        Assert.False(over);
        Assert.True(c.X < 40 * Cell);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(12)]
    public void AFleeingLoperLeapsAGapItClearsAndNeverFallsIn(int wide)
    {
        var (_, over, fell) = Flee(Course(gap: wide), wide);
        Assert.True(over, $"it never got across a gap {wide} wide");
        Assert.False(fell, "it fell in");
    }

    [Fact]
    public void AFleeingLoperTurnsAtAGapTooWideToLeap()
    {
        var (c, over, fell) = Flee(Course(gap: 22), 22);
        Assert.False(over);
        Assert.False(fell, "it leapt into the void");
        Assert.True(c.X < 40 * Cell);
    }

    [Fact]
    public void AWanderingLoperJumpsUpALedgeOfTwoButNotAWallOfThreeNorAGap()
    {
        foreach (var (course, gets) in new[] { (Course(wall: 2), true), (Course(wall: 3), false), (Course(gap: 3), false) })
        {
            var w = FaunaTesting.Planet(course, FaunaTesting.Lifeless).Breathing().HeroAt(20, 8);
            var c = w.Loper(36, 8).Walking(+1);
            bool over = false, fell = false;
            for (int t = 0; t < 300; t++)
            {
                w.Run();
                Assert.True(Clear(w, c));
                over |= c.X - Loper.Width * Unit / 2 >= 41 * Cell;
                fell |= c.Y > 9 * Cell;
            }
            Assert.Equal(gets, over);
            Assert.False(fell);
        }
    }

    [Fact]
    public void AnIdleLoperTakesToWanderingAndBackOnItsOwnDrawingItsWay()
    {
        var w = FaunaTesting.Planet(Room).HeroAt(1, 3);
        var c = w.Loper(4, 3);
        var modes = new HashSet<Mode>();
        for (int t = 0; t < 600; t++)
        {
            w.Run();
            modes.Add(c.Mode);
            Assert.True(c.Mode != Mode.Wander || Math.Abs(c.VX) == Loper.Wander || c.VY != 0);
        }
        Assert.Equal([Mode.Idle, Mode.Wander], modes.Order());
    }

    // Hits.

    /// <summary>A room 48 wide, its floor row 4: room to run.</summary>
    private static readonly string Long = string.Join('\n',
        [new string('#', 48), .. Enumerable.Repeat("#" + new string('.', 46) + "#", 3), new string('#', 48)]);

    [Fact]
    public void ALoperHasNoHitStunAHitKnocksItAwayWithAHopAndItFleesAtOnceThenCalmsDown()
    {
        Assert.Equal(0, Loper.Stun);
        // Struck from past RamBackReach, on the hero's side: it flees, never turning back to ram (0103).
        foreach (var (hero, away, striker) in new[] { (20, +1, 2), (27, -1, 45) })
        {
            var w = FaunaTesting.Planet(Long).HeroAt(hero, 3).HeroAt(striker, 3, peer: 1);
            var c = w.Loper(23, 3).Still();
            w.Do(new Strike(c.Id, Axe) { From = 1 }).Run();
            Assert.Equal(Mode.Flee, c.Mode);
            // Knocked at its flight's speed or the knock's, the more: it bolts from the blow.
            Assert.Equal(away * Math.Max(Loper.Knock, Loper.Flee), c.VX);
            Assert.True(c.VY < 0, "no hop");
            while (c.VY != 0) w.Run();
            w.Run();
            Assert.Equal(away * Loper.Flee, c.VX);
            w.Run(Loper.FleeTicks);
            Assert.Equal(Mode.Idle, c.Mode);
            Assert.Equal(0, c.VX);
            Assert.Equal(Spec.Weapons.Axe.Damage, c.Damage);
        }
    }

    [Fact]
    public void ALoperBeingDrilledGetsAwayFromAHeroStandingStill()
    {
        // The hero at column 4 holds its running drill level to the right, its bit reaching three cells from its middle: every
        // two ticks a strike on the loper while any of its body is within that, as the client sends one.
        var w = FaunaTesting.Planet(Long).HeroAt(4, 3).Do(new SetCreative(true)).Do(new Run(Drill, true)).GiveItems(0, (Drill, 1));
        var c = w.Loper(5, 3).Still();
        int reach = w.HeroMiddle(4) + 3 * Cell, half = Loper.Width * Unit / 2, hits = 0, free = -1;
        for (int t = 0; t < 80; t++)
        {
            if (t % Spec.Weapons.Drill.Pace == 0 && c.X - half <= reach && w.M.Fauna.Creatures.Count > 0)
            {
                w.Do(new Strike(c.Id, Drill));
                hits++;
            }
            w.Run();
            if (free < 0 && c.X - half > reach) free = t;
        }
        Assert.Same(c, Only(w));
        Assert.True(c.X - half > reach, $"still under the bit at {c.X}");
        // Out of reach within half a second, a few of the drill's twelve hits taken.
        Assert.InRange(free, 1, 10);
        Assert.True(c.Damage < Loper.Hp / 2, $"{hits} hits, {c.Damage} damage before it got away");
    }

    [Fact]
    public void ASprintingHeroCannotCloseTheGapDuringAFleeThenItCalmsAndCanBeFoundAgain()
    {
        // The hero's sprint in thousandths a tick: its controller steps 60 times a second, the sim 20.
        int sprint = Spec.Hero.Run * 60 / World.TicksPerSecond;
        Assert.True(Loper.Flee > sprint, $"a flee of {Loper.Flee} a tick against a sprint of {sprint}");
        var wide = Wide(400, 8);
        // Struck by a friend past RamBackReach behind the hero: it flees, never turning back to ram (0103).
        var w = FaunaTesting.Planet(wide, FaunaTesting.Lifeless).Breathing().HeroAt(100, 6).HeroAt(80, 6, peer: 1);
        var c = w.Loper(102, 6).Still();
        w.Do(new Strike(c.Id, Axe) { From = 1 }).Run();
        // A hero sprinting after it from where it struck, its body's middle level with the loper's back as the blow lands.
        long hero = w.HeroMiddle(100), gap = c.X - Loper.Width * Unit / 2 - hero;
        int ticks = 1;
        while (c.Mode == Mode.Flee)
        {
            w.Run();
            hero += sprint;
            ticks++;
            long now = c.X - Loper.Width * Unit / 2 - hero;
            Assert.True(now >= gap - 2 * sprint, $"the gap closed to {now} from {gap} on tick {ticks}");
        }
        // Calmed within its flight's three seconds, never caught; then it idles and wanders, where a follower finds it.
        Assert.InRange(ticks, Loper.FleeTicks, Loper.FleeTicks + 1);
        Assert.True(c.X - Loper.Width * Unit / 2 - hero > gap, "a sprint gained on it");
        Assert.Equal(Mode.Idle, c.Mode);
        Assert.Same(c, Only(w));
    }

    [Fact]
    public void ABlowNeverSlowsALoperRunningTheKnocksWay()
    {
        var w = FaunaTesting.Planet(Long).HeroAt(4, 3).Do(new SetCreative(true)).Do(new Run(Drill, true)).GiveItems(0, (Drill, 1));
        var c = w.Loper(10, 3).Walking(+1, Mode.Flee);
        w.Run();
        // Sent flying faster than it runs, struck again: not slowed to the blow's speed.
        c.VX = 2 * Loper.Flee;
        w.Do(new Strike(c.Id, Drill)).Run();
        Assert.Equal(2 * Loper.Flee, c.VX);
        // Running at the hero, it is turned back, bolting.
        c.Walking(-1, Mode.Flee);
        w.Do(new Strike(c.Id, Drill)).Run();
        Assert.Equal(Loper.Flee, c.VX);
    }

    [Fact]
    public void TheDrillsQuickHitsEachHopItsPacesShareOfTheAxesAndNoHopWhileRising()
    {
        var w = FaunaTesting.Planet(Room).HeroAt(1, 3).Do(new SetCreative(true)).Do(new Run(Drill, true)).GiveItems(0, (Drill, 1));
        var c = w.Loper(5, 3).Still();
        w.Do(new Strike(c.Id, Drill)).Run();
        int share = Spec.Weapons.Drill.Pace * 1000 / Tune.KnockPace;
        // Its knock a fifth of the axe's, under its flight: it bolts at its flight's speed, hopping a fifth as high.
        Assert.Equal(Loper.Flee, c.VX);
        Assert.Equal(-Loper.Hop * share / 1000 + Content.PlanetTypes.First.Fall(Tune.Gravity), c.VY);
        int rising = c.VY;
        Assert.True(rising < 0);
        // Struck again while it rises: knocked as before, its rise not renewed.
        w.Do(new Strike(c.Id, Drill)).Run();
        Assert.Equal(rising + Content.PlanetTypes.First.Fall(Tune.Gravity), c.VY);
    }

    [Fact]
    public void AStunnedCreatureFleesAwayFromTheNearestHeroAsItsStunEnds()
    {
        // No species has hit-stun yet (a hostile's to come): one held stunned, knocked left by hero 1 on its right, bolts from
        // hero 0 beside it on the left, the nearer, once its stun is out.
        var w = FaunaTesting.Planet(Room).HeroAt(10, 3, peer: 1).HeroAt(4, 3);
        var c = w.Loper(5, 3).Still();
        (c.Mode, c.ModeLeft, c.VX) = (Mode.Hurt, 3, -Loper.Knock);
        w.Run(2);
        Assert.Equal((Mode.Hurt, -Loper.Knock), (c.Mode, c.VX));
        w.Run();
        Assert.Equal((Mode.Flee, Loper.Flee), (c.Mode, c.VX));
    }

    [Fact]
    public void SixHitsOfTheAxeKillALoperAndItsYieldsFallWhereItStood()
    {
        int hits = (Loper.Hp + Spec.Weapons.Axe.Damage - 1) / Spec.Weapons.Axe.Damage;
        Assert.Equal(6, hits);
        var w = FaunaTesting.Planet(Room).HeroAt(1, 3);
        var c = w.Loper(5, 3).Still();
        for (int i = 1; i < hits; i++)
        {
            w.Do(new Strike(c.Id, Axe)).Run(3);
            Assert.Equal(i * Spec.Weapons.Axe.Damage, c.Damage);
            c.Still();
            (c.X, c.Y, c.VY) = (6 * Cell, 4 * Cell, 0);
        }
        Assert.Equal(c, Only(w));
        w.Do(new Strike(c.Id, Axe)).Run();
        Assert.Empty(w.M.Fauna.Creatures);
        Assert.Empty(w.Refusals);
        w.Run(30);
        var biomass = w.M.Items.Floor.Where(s => s.Item == "biomass").ToList();
        Assert.InRange(biomass.Sum(s => s.Count), 2, 3);
        Assert.All(w.M.Items.Floor, s =>
        {
            Assert.Equal(FloorStack.Nobody, s.By);
            Assert.Equal(4 * Cell, s.Y);
            Assert.Equal(6 * Cell + Unit / 2, s.X);
        });
    }

    [Fact]
    public void ALoperGivesASeedNowAndThen()
    {
        const int kills = 40;
        var w = FaunaTesting.Planet(Room).HeroAt(1, 3);
        for (int k = 0; k < kills; k++)
        {
            var c = w.Loper(5, 3).Still();
            c.Damage = Loper.Hp - 1;
            w.Do(new Strike(c.Id, Axe)).Run();
        }
        int seeds = w.M.Items.Floor.Where(s => s.Item == "seed").Sum(s => s.Count);
        // One in four on average: never all, never none in forty.
        Assert.InRange(seeds, 1, kills - 1);
    }

    [Fact]
    public void TheDrillHitsAsItsToolStatsSayPlainOrFittedWhileItRuns()
    {
        foreach (var (drill, damage) in new[] { (new Stack(Drill, 1), 1), (new Stack(Drill, 1, Module: "arcModule"), 2) })
        {
            var w = FaunaTesting.Planet(Room).HeroAt(1, 3).Put(0, drill).Do(new SetCreative(true)).Do(new Run(Drill, true));
            var c = w.Loper(5, 3).Still();
            Assert.Equal(damage, w.M.Items.Damage(0, Drill));
            w.Do(new Strike(c.Id, Drill)).Run();
            Assert.Equal(damage, c.Damage);
            int hits = 1;
            while (w.M.Fauna.Creatures.Count > 0)
            {
                w.Do(new Strike(c.Id, Drill)).Run();
                hits++;
            }
            Assert.Equal((Loper.Hp + damage - 1) / damage, hits);
            Assert.Empty(w.Refusals);
        }
    }

    [Fact]
    public void TheSecondOfTwoKillingBlowsOnOneTickIsRefusedGone()
    {
        var w = FaunaTesting.Planet(Room).HeroAt(1, 3).HeroAt(10, 3, peer: 1);
        var c = w.Loper(5, 3).Still();
        c.Damage = Loper.Hp - 2;
        w.Do(new Strike(c.Id, Axe)).Do(new Strike(c.Id, Axe) { From = 1 }).Run();
        Assert.Empty(w.M.Fauna.Creatures);
        Assert.Equal(["fauna.gone"], Reasons(w));
    }

    [Fact]
    public void AStrikeIsRefusedForACreatureGoneAndAnElectricToolFlatOrNotRunning()
    {
        var w = FaunaTesting.Planet(Room).HeroAt(1, 3).GiveItems(0, (Drill, 1));
        var c = w.Loper(5, 3).Still();
        Assert.Equal("fauna.gone", w.M.Fauna.StrikeProblem(0, c.Id + 1, Axe));
        Assert.Equal("items.flat", w.M.Fauna.StrikeProblem(0, c.Id, Drill));
        w.Do(new Strike(c.Id + 1, Axe)).Do(new Strike(c.Id, Drill)).Run();
        Assert.Equal(["fauna.gone", "items.flat"], Reasons(w));
        // Charged but not running: the client sends its run first, so a hit without one is refused as it applies.
        w.Put(Belt, new Stack("battery", 1, Spec.Charge.Battery));
        Assert.Null(w.M.Fauna.StrikeProblem(0, c.Id, Drill));
        w.Refusals.Clear();
        w.Do(new Strike(c.Id, Drill)).Run();
        Assert.Equal(["items.notRunning"], Reasons(w));
        Assert.Equal(0, c.Damage);
        Assert.Throws<InvalidOperationException>(() => w.M.Fauna.StrikeProblem(0, c.Id, "iron"));
    }

    // Releases.

    [Fact]
    public void AReleaseIsRefusedAboardIntoAWallOffTheGridAndAtTheCap()
    {
        var ship = new Scenario(Room).HeroAt(1, 3);
        ship.Do(new Release(Loper.Id, ship.Feet(3, 3))).Run();
        Assert.Equal(["fauna.notHere"], Reasons(ship));
        Assert.Empty(ship.M.Fauna.Creatures);

        var w = FaunaTesting.Planet(Room).HeroAt(1, 3);
        // In the side wall, its body a character into the wall on the right, its head in the ceiling, off the grid.
        foreach (int cell in new[] { w.Feet(0, 3), w.Feet(10, 3), w.Feet(4, 1), -1, w.Layout.Width * w.Layout.Height })
            w.Do(new Release(Loper.Id, cell));
        w.Run();
        Assert.Equal(Enumerable.Repeat("fauna.blocked", 5), Reasons(w));
        w.Refusals.Clear();
        for (int i = 0; i < Tune.Max + 1; i++) w.Do(new Release(Loper.Id, w.Feet(1 + i % 9, 3)));
        w.Run();
        Assert.Equal(Tune.Max, w.M.Fauna.Creatures.Count);
        Assert.Equal(["fauna.full"], Reasons(w));
    }

    // Going.

    [Fact]
    public void CreaturesGoWithTheLastHeroAndAtOnceOutOfEveryHardBox()
    {
        var w = FaunaTesting.Planet(Wide(260, 8), FaunaTesting.Lifeless).HeroAt(10, 6);
        w.Loper(12, 6).Still();
        w.Do(new Stillwake.Sim.Crew.Leave()).Run();
        Assert.Empty(w.M.Fauna.Creatures);

        w.HeroAt(10, 6).Run();
        w.Loper(12, 6).Still();
        // Past the hard box across from the cell under its middle, the left one of the character at column 13.
        w.HeroAt(13 + Tune.Hard.X / 2 + 1, 6).Run();
        Assert.Empty(w.M.Fauna.Creatures);
    }

    [Fact]
    public void ACreatureOutOfEveryKeepBoxButInAHardOneLingersThenGoes()
    {
        var w = FaunaTesting.Planet(Wide(260, 8), FaunaTesting.Lifeless).HeroAt(10, 6);
        var c = w.Loper(12, 6).Still();
        w.HeroAt(13 + Tune.Keep.X / 2 + 1, 6).Run();
        w.Run(Tune.Linger - 2);
        Assert.Same(c, Only(w));
        w.Run();
        Assert.Empty(w.M.Fauna.Creatures);
    }

    // Spawning.

    /// <summary>Open ground <paramref name="width"/> wide, its floor the bottom row: under the sky, or with <paramref name="open"/> a dot, back walls.</summary>
    private static Layout Wide(int width, int height, char open = ' ') =>
        Layout.Parse([.. Enumerable.Range(0, height).Select(y => new string(y == height - 1 ? '#' : open, width))]);

    [Fact]
    public void LopersSpawnOnTheBeatOnOpenGroundOutOfEveryHerosViewAndInTheKeepBox()
    {
        var w = FaunaTesting.Planet(Wide(400, 12)).Breathing().HeroAt(200, 10).HeroAt(150, 10, peer: 1);
        int seen = 0;
        for (int t = 0; t < 40 * Tune.Beat; t++)
        {
            w.Run();
            foreach (var c in w.M.Fauna.Creatures.Where(c => c.Id > seen))
            {
                Assert.Equal(0, w.World.Tick % Tune.Beat);
                var spec = Tune.Get(c.Species);
                int left = c.X / Unit - spec.Width / 2, top = c.Y / Unit - spec.Height;
                foreach (var hero in new[] { 2 * 200, 2 * 150 })
                    Assert.False(left + spec.Width - 1 >= hero - Tune.View.X && left <= hero + Tune.View.X, $"creature {c.Id} spawned at {left} in view of {hero}");
                Assert.Equal((11 * Cell, Mode.Idle), (c.Y, c.Mode));
                Assert.True(top >= 0);
                seen = c.Id;
            }
        }
        Assert.True(seen > 0, "nothing spawned");
    }

    [Fact]
    public void NoHerosKeepBoxGetsMoreThanItsShareFromSpawningNorTheSurfaceMoreThanItsCap()
    {
        // Three heroes far apart, their keep boxes apart: each fills its share and no more.
        var w = FaunaTesting.Planet(Wide(1200, 12)).Breathing();
        for (int h = 0; h < 3; h++) w.HeroAt(100 + 200 * h, 10, peer: h);
        void Settle()
        {
            for (int t = 0; t < 100 * Tune.Beat; t++)
            {
                w.Run();
                foreach (var c in w.M.Fauna.Creatures) c.Still();
            }
        }
        int InKeep(int h) => w.M.Fauna.Creatures.Count(c => Math.Abs(c.X / Unit - 2 * (100 + 200 * h)) <= Tune.Keep.X);
        Settle();
        Assert.Equal([Tune.PerKeep, Tune.PerKeep, Tune.PerKeep], Enumerable.Range(0, 3).Select(InKeep));
        // Three more: six shares of ten would pass the cap of forty.
        for (int h = 3; h < 6; h++) w.HeroAt(100 + 200 * h, 10, peer: h);
        Settle();
        Assert.Equal(Tune.Max, w.M.Fauna.Creatures.Count);
        Assert.All(Enumerable.Range(0, 6), h => Assert.True(InKeep(h) <= Tune.PerKeep));
    }

    [Fact]
    public void NothingOfTheSurfaceSpawnsUnderBackWallsNorAnythingOnALifelessTypeOrAboard()
    {
        var backed = Wide(400, 12, '.');
        var sky = Spec.PlanetTypes.First with { Fauna = [.. Spec.PlanetTypes.First.Fauna.Where(h => h.Place == Tune.Place.Surface)] };
        var caves = FaunaTesting.Planet(backed, sky).HeroAt(200, 10);
        var lifeless = FaunaTesting.Planet(Wide(400, 12), FaunaTesting.Lifeless).HeroAt(200, 10);
        var ship = new Scenario(Wide(400, 12)).HeroAt(200, 10);
        foreach (var w in new[] { caves, lifeless, ship }) w.Run(20 * Tune.Beat);
        Assert.Empty(caves.M.Fauna.Creatures);
        Assert.Empty(lifeless.M.Fauna.Creatures);
        Assert.Empty(ship.M.Fauna.Creatures);
    }

    [Fact]
    public void ACaveHabitatSpawnsOnlyOnBackWalledFloorsInItsBand()
    {
        // Rows this near the top are the first planet's band 0, the cold; band 1 is far under them.
        var cold = Spec.PlanetTypes.First with { Fauna = [new(Loper.Id, Tune.Place.Band, 1, Band: 0)] };
        var garden = Spec.PlanetTypes.First with { Fauna = [new(Loper.Id, Tune.Place.Band, 1, Band: 1)] };
        Assert.Equal(0, Terrain.BandOf(cold, 2 * 12));
        var caves = FaunaTesting.Planet(Wide(400, 12, '.'), cold).Breathing().HeroAt(200, 10);
        var sky = FaunaTesting.Planet(Wide(400, 12), cold).Breathing().HeroAt(200, 10);
        var deeper = FaunaTesting.Planet(Wide(400, 12, '.'), garden).Breathing().HeroAt(200, 10);
        foreach (var w in new[] { caves, sky, deeper }) w.Run(10 * Tune.Beat);
        Assert.NotEmpty(caves.M.Fauna.Creatures);
        Assert.Empty(sky.M.Fauna.Creatures);
        Assert.Empty(deeper.M.Fauna.Creatures);
    }

    [Fact]
    public void TheFirstPlanetsKindsSpawnByHabitatTheWalkersUnderTheSkyTheFluffOnCaveFloors()
    {
        // The user's rule: by habitat. Under the sky the loper, the tapir and the sixleg, each drawn; in the cold band's caves the fluff alone.
        string[] Spawned(Layout map)
        {
            var w = FaunaTesting.Planet(map).Breathing();
            for (int h = 0; h < 3; h++) w.HeroAt(100 + 200 * h, 10, peer: h);
            for (int t = 0; t < 60 * Tune.Beat; t++)
            {
                w.Run();
                foreach (var c in w.M.Fauna.Creatures) c.Still();
            }
            return [.. w.M.Fauna.Creatures.Select(c => c.Species).Distinct().Order(StringComparer.Ordinal)];
        }
        Assert.Equal([Tune.Loper.Id, Tune.Sixleg.Id, Tune.Tapir.Id], Spawned(Wide(700, 12)));
        Assert.Equal([Tune.Fluff.Id], Spawned(Wide(700, 12, '.')));
        var first = Spec.PlanetTypes.First.Fauna;
        Assert.All(first.Where(h => h.Species == Tune.Fluff.Id), h => Assert.Equal((Tune.Place.Band, true), (h.Place, h.Band is 0 or 1)));
        Assert.All(first.Where(h => h.Species != Tune.Fluff.Id), h => Assert.Equal(Tune.Place.Surface, h.Place));
    }

    [Fact]
    public void TheShipsFaunaNeverChangesNorDrawsAboardWhateverTheHeroesDo()
    {
        var w = new Scenario(Wide(400, 12)).HeroAt(200, 10).HeroAt(30, 10, peer: 1);
        string before = FaunaSlice(w);
        var rng = new Rng(new RngState { S = 7 });
        for (int t = 0; t < 400; t++)
        {
            if (w.M.Fauna.Fuzz(rng, (int)rng.Below(2)) is { } c) w.Do(c);
            w.Run();
        }
        Assert.Equal(before, FaunaSlice(w));
        Assert.All(w.Refusals.Where(r => r.Command is Release), r => Assert.Equal("fauna.notHere", r.Reason));
    }

    private static string FaunaSlice(Scenario w) => System.Text.Json.JsonSerializer.Serialize(w.M.Fauna.Slice, w.World.Json);

    [Fact]
    public void TheSpeciesAndTheirHabitatsHoldTogether()
    {
        Assert.Equal(Tune.All.Count, Tune.All.Select(s => s.Id).Distinct().Count());
        Assert.All(Tune.All, s =>
        {
            // Faster than a cell a tick is moved in parts (0104): FasterThanACellATickACreatureNeverPassesAWallOrAFloorOneCellThick.
            Assert.True(s.Width > 0 && s.Height > 0 && s.Hp > 0 && s.Stun >= 0 && s.FleeTicks > 0);
            Assert.All(s.Yields, y => Assert.NotNull(Spec.Items.Get(y.Item)));
        });
        foreach (var type in Spec.PlanetTypes.All)
            Assert.All(type.Fauna, h =>
            {
                Assert.NotNull(Tune.Get(h.Species));
                Assert.True(h.Weight > 0);
                Assert.InRange(h.Band, 0, type.Bands.Count);
            });
        // Spawned past every view, inside the keep box, which lies inside the hard box.
        Assert.True(Tune.RingFrom > Tune.View.X && Tune.RingTo + Tune.All.Max(s => s.Width) <= Tune.Keep.X);
        Assert.True(Tune.Keep.X < Tune.Hard.X && Tune.Keep.Y < Tune.Hard.Y && Tune.View.Y < Tune.Keep.Y);
        Assert.True(Tune.PerKeep <= Tune.Max);
    }

    [Fact]
    public void ACreatureABuildingWallsInRisesOutOfItACellATick()
    {
        var w = FaunaTesting.Planet(Room).HeroAt(1, 3);
        var c = w.Loper(5, 3).Still();
        // A wall a cell high under each half of it.
        w.Do(new Place(w.Feet(5, 3), Grid.Placement.Wall())).Do(new Place(w.Feet(6, 3), Grid.Placement.Wall())).Run();
        Assert.Equal(4 * Cell - Unit, c.Y);
        Assert.True(Clear(w, c));
    }
}
