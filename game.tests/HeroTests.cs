using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Xunit;
using Spec = Stillwake.Content.Hero;

namespace Stillwake.Game.Tests;

/// <summary>The hero controller: apps/game/src/features/crew/hero.test.ts, one for one.</summary>
public class HeroTests
{
    private const int Unit = Hero.Unit;
    private const int Half = Spec.Width / 2;

    internal static Ship ShipFrom(Layout layout)
    {
        var m = new Registry();
        World.Create(1, layout, m.All);
        return Ship.Of(m.Grid, m.Rooms);
    }

    internal static Ship ShipFrom(string map) => ShipFrom(Layout.Parse(map));
    internal static Ship ShipFrom(IReadOnlyList<string> rows) => ShipFrom(Layout.Parse(rows));

    /// <summary>The ship and a hero standing where the crew makes heroes: its lifepod's hatch, or its S.</summary>
    private static (Ship Ship, Hero Hero) Start(Layout layout)
    {
        var ship = ShipFrom(layout);
        int spawn = new TestWorld(layout).M.Crew.Spawn;
        return (ship, Hero.At(ship, spawn));
    }

    private static (Ship Ship, Hero Hero) Start(string map) => Start(Layout.Parse(map));
    private static (Ship Ship, Hero Hero) Start(IReadOnlyList<string> rows) => Start(Layout.Parse(rows));

    internal static readonly HeroInput Still = new(0, 0, false);
    internal static readonly HeroInput Left = Still with { X = -1 };
    internal static readonly HeroInput Right = Still with { X = 1 };
    private static readonly HeroInput Up = Still with { Y = -1 };
    private static readonly HeroInput Down = Still with { Y = 1 };
    private static readonly HeroInput Jump = Still with { Jump = true };

    /// <summary>Steps with <paramref name="input"/> until <paramref name="done"/> holds; fails if it never does.</summary>
    internal static Hero Until(Ship ship, Hero hero, HeroInput input, Func<Hero, bool> done)
    {
        var h = hero;
        for (int i = 0; i < 2000; i++)
        {
            if (done(h)) return h;
            h = h.Step(input, ship);
        }
        throw new InvalidOperationException($"never got there; stopped at {h}");
    }

    /// <summary>Steps with <paramref name="input"/> until the hero comes to rest, which the top of a jump is not.</summary>
    internal static Hero Settle(Ship ship, Hero hero, HeroInput input)
    {
        var h = hero;
        for (int i = 0; i < 10000; i++)
        {
            var next = h.Step(input, ship);
            if (next.X == h.X && next.Y == h.Y && next.Vy == h.Vy && next.Climbing == h.Climbing) return next;
            h = next;
        }
        throw new InvalidOperationException($"never settled; at {h}");
    }

    private static int Column(Hero h) => h.X / Unit;

    private static Hero Run(Ship ship, Hero hero, HeroInput input, int steps)
    {
        for (int i = 0; i < steps; i++) hero = hero.Step(input, ship);
        return hero;
    }

    // Rooms are drawn three cells tall at least: the hero stands 2.625 cells, under three.
    [Fact]
    public void FallsAndStandsFlushOnTheFloorBelow()
    {
        var (ship, hero) = Start("""
            #####
            #...#
            #...#
            #.S.#
            #...#
            #...#
            #####
            """);
        var landed = Settle(ship, hero, Still);
        Assert.Equal(6 * Unit, landed.Y);
        Assert.Equal(0, landed.Vy);
    }

    private const string Room = """
        #######
        #.....#
        #.....#
        #..S..#
        #######
        """;

    [Fact]
    public void WalksUntilFlushAgainstAWallEitherWay()
    {
        var (ship, hero) = Start(Room);
        Assert.Equal(hero.X + Spec.Walk, hero.Step(Right, ship).X);
        Assert.Equal(6 * Unit - Half, Settle(ship, hero, Right).X);
        Assert.Equal(1 * Unit + Half, Settle(ship, hero, Left).X);
    }

    [Fact]
    public void RunsWithShiftHeldFasterThanItWalksButSwimsNoFasterForItInSpace()
    {
        var (ship, hero) = Start(Room);
        Assert.Equal(hero.X + Spec.Run, hero.Step(Right with { Sprint = true }, ship).X);
        Assert.True(Spec.Run > Spec.Walk);
        var opened = Opened();
        var h = Hero.At(opened, 40 * opened.Width + 120);
        // Shift in space only thrusts, with a jetpack (0047): without one it swims as slowly as ever.
        Assert.Equal(h.X + Spec.Swim, h.Step(Right with { Sprint = true }, opened).X);
    }

    [Fact]
    public void FacesTheWayItLastWalked()
    {
        var (ship, hero) = Start(Room);
        var turned = hero.Step(Left, ship).Step(Still, ship);
        Assert.Equal(-1, turned.Facing);
        Assert.Equal(1, turned.Step(Right, ship).Step(Still, ship).Facing);
    }

    [Fact]
    public void FacesThePointerWhicheverWayItWalksAndStillMovesTheWayItWalks()
    {
        var (ship, hero) = Start(Room);
        var back = hero.Step(Left with { Face = 1 }, ship);
        Assert.Equal(1, back.Facing);
        Assert.True(back.X < hero.X);
        Assert.Equal(-1, back.Step(Still with { Face = -1 }, ship).Facing);
    }

    [Fact]
    public void SwimmingKeepsFacingWhereItsAxeSwingBeganTowardTillTheSwingEnds()
    {
        var opened = Opened();
        var h = Hero.At(opened, 40 * opened.Width + 120);
        Assert.Equal(-1, h.Step(Left with { Face = 1 }, opened).Facing);
        var swinging = h.Step(Left with { Face = 1, Swings = true }, opened);
        Assert.Equal(1, swinging.Facing);
        Assert.True(swinging.X < h.X);
    }

    [Fact]
    public void FallsNoFasterThanItsTopSpeed()
    {
        const int depth = 60;
        var shaft = new[] { "###", "#.#", "#.#", "#S#" }.Concat(Enumerable.Repeat("#.#", depth)).Append("###").ToArray();
        var (ship, hero) = Start(shaft);
        var h = hero;
        int fastest = 0;
        for (int i = 0; i < 400; i++)
        {
            h = h.Step(Still, ship);
            fastest = Math.Max(fastest, h.Vy);
        }
        Assert.Equal(Spec.MaxFall, fastest);
        Assert.Equal((4 + depth) * Unit, h.Y);
    }

    [Fact]
    public void IsStoppedByAClosedDoorwayAndWalksThroughAnOpenOneThreeCellsTall()
    {
        string Doorway(char door, char? top = null) => $"""
            ########
            #...{top ?? door}..#
            #...{door}..#
            #.S.{door}..#
            ########
            """;
        var closed = Start(Doorway('D'));
        Assert.Equal(4 * Unit - Half, Settle(closed.Ship, closed.Hero, Right).X);
        var open = Start(Doorway('d'));
        Assert.Equal(7 * Unit - Half, Settle(open.Ship, open.Hero, Right).X);
        // Two cells of open door under a wall are too low.
        var low = Start(Doorway('d', '#'));
        Assert.Equal(4 * Unit - Half, Settle(low.Ship, low.Hero, Right).X);
    }

    [Fact]
    public void JumpsOntoALedgeThreeCellsHighNotFour()
    {
        (Ship Ship, Hero Hero) Ledge(int high)
        {
            var rows = new[] { "######" }.Concat(Enumerable.Repeat("#....#", 8)).Append("######").ToArray();
            for (int y = 9 - high; y <= 8; y++) rows[y] = "#..###";
            rows[8] = "#S.###";
            return Start(rows);
        }
        var three = Ledge(3);
        var h = three.Hero.Step(Jump, three.Ship);
        Assert.Equal(-Spec.Jump + Spec.Gravity, h.Vy);
        h = Until(three.Ship, h, Right, x => Column(x) == 3);
        Assert.Equal(6 * Unit, Settle(three.Ship, h, Still).Y);

        var four = Ledge(4);
        var g = Run(four.Ship, four.Hero.Step(Jump, four.Ship), Right, 200);
        Assert.True(Column(g) < 3);
        Assert.Equal(9 * Unit, g.Y);
    }

    [Fact]
    public void JumpsOnlyFromSomethingToStandOn()
    {
        var (ship, hero) = Start("""
            ######
            #....#
            #....#
            #....#
            #....#
            #S...#
            ######
            """);
        var up = hero.Step(Jump, ship);
        Assert.Equal(up.Vy + Spec.Gravity, up.Step(Jump, ship).Vy);
    }

    [Fact]
    public void BumpsACeilingAndComesBackDown()
    {
        var (ship, hero) = Start("""
            ######
            #....#
            #....#
            #.S..#
            ######
            """);
        // The room is three cells tall, so the first step of the jump already reaches the ceiling.
        var top = hero.Step(Jump, ship);
        Assert.Equal(1 * Unit, top.Y - Spec.Height);
        Assert.Equal(0, top.Vy);
        Assert.True(top.Step(Still, ship).Y > top.Y);
        // Head against the ceiling, the hero still walks into the next column.
        var atEdge = top with { X = 3 * Unit - Half };
        Assert.Equal(atEdge.X + Spec.Walk, atEdge.Step(Right, ship).X);
        Assert.Equal(4 * Unit, Settle(ship, top, Still).Y);
    }

    [Fact]
    public void WalksOffALedgeAndSlidesDownAWallWithoutCatchingOnIt()
    {
        var (ship, hero) = Start("""
            ######
            #....#
            #....#
            #S...#
            ##...#
            ##...#
            ######
            """);
        var off = Until(ship, hero, Right, h => h.X - Half >= 2 * Unit);
        // Pressed into the wall of the ledge, the hero still falls all the way down.
        var fallen = Settle(ship, off with { X = 2 * Unit + Half }, Left);
        Assert.Equal(2 * Unit + Half, fallen.X);
        Assert.Equal(6 * Unit, fallen.Y);
    }

    [Fact]
    public void StandsOnALedgeItOverlapsByTheLeastAmount()
    {
        var (ship, hero) = Start("""
            ######
            #....#
            #....#
            #S...#
            ##...#
            ######
            """);
        var edge = hero with { X = 2 * Unit + Half - 1 };
        Assert.Equal(4 * Unit, Settle(ship, edge, Still).Y);
        Assert.Equal(5 * Unit, Settle(ship, edge with { X = edge.X + 1 }, Still).Y);

        var mirrored = Start("""
            ######
            #....#
            #....#
            #...S#
            #...##
            ######
            """);
        var other = mirrored.Hero with { X = 4 * Unit - Half + 1 };
        Assert.Equal(4 * Unit, Settle(mirrored.Ship, other, Still).Y);
        Assert.Equal(5 * Unit, Settle(mirrored.Ship, other with { X = other.X - 1 }, Still).Y);
    }

    [Fact]
    public void JumpsUpThroughAFloorAndLandsOnTopOfIt()
    {
        var (ship, hero) = Start("""
            ######
            #....#
            #....#
            #....#
            #....#
            #....#
            #....#
            #S==.#
            ######
            """);
        // Standing under the floor, inside its row: a floor holds a hero up from above only.
        var under = Settle(ship, hero with { X = 3 * Unit }, Still);
        Assert.Equal(8 * Unit, under.Y);
        var on = Settle(ship, under.Step(Jump, ship), Still);
        Assert.Equal(7 * Unit, on.Y);
        Assert.Equal(-Spec.Jump + Spec.Gravity, on.Step(Jump, ship).Vy);
    }

    [Fact]
    public void WalksThroughAFloorFromTheSide()
    {
        var (ship, hero) = Start("""
            ######
            #....#
            #....#
            #S=..#
            ######
            """);
        Assert.Equal(5 * Unit - Half, Settle(ship, hero, Right).X);
    }

    // A ladder four cells tall, its top in row 4, with room for the hero to stand above it.
    private const string LadderMap = """
        #######
        #.....#
        #.....#
        #.....#
        #..H..#
        #..H..#
        #..H..#
        #.SH..#
        #######
        """;

    private static Hero At(int x, int y, bool climbing = false, int vy = 0) => new(x, y, 0, vy, climbing, 1);

    [Fact]
    public void LaddersAreGrabbedWithUpCentringTheHeroAndClimbedToTheTop()
    {
        var (ship, hero) = Start(LadderMap);
        var near = Until(ship, hero, Right, h => Column(h) == 3);
        var grabbed = near.Step(Up, ship);
        Assert.True(grabbed.Climbing);
        Assert.Equal(3 * Unit + Unit / 2, grabbed.X);
        Assert.Equal(near.Y - Spec.Climb, grabbed.Y);
        // The step the feet reach the top, the hero steps off onto it: no hop past it and back.
        var top = grabbed;
        for (int i = 0; i < 200 && top.Climbing; i++)
        {
            top = top.Step(Up, ship);
            Assert.True(top.Y >= 4 * Unit);
        }
        Assert.False(top.Climbing);
        Assert.Equal(4 * Unit, top.Y);
        for (int i = 0; i < 30; i++)
        {
            top = top.Step(Up, ship);
            Assert.Equal(4 * Unit, top.Y);
        }
    }

    [Fact]
    public void LaddersAreClimbedDownFromTheTopWithDownToTheFloor()
    {
        var (ship, _) = Start(LadderMap);
        var top = At(3500, 4 * Unit);
        var grabbed = top.Step(Down, ship);
        Assert.True(grabbed.Climbing);
        Assert.Equal(8 * Unit, Settle(ship, grabbed, Down).Y);
        // A ladder's top holds a hero up, so a jump starts from it.
        Assert.True(top.Step(Jump, ship).Y < top.Y);
    }

    [Fact]
    public void LaddersHoldTheHeroInPlaceWhileNothingIsPressed()
    {
        var (ship, _) = Start(LadderMap);
        var h = At(3500, 6500, climbing: true);
        for (int i = 0; i < 30; i++) h = h.Step(Still, ship);
        Assert.Equal(At(3500, 6500, climbing: true), h);
    }

    [Fact]
    public void LaddersAreGrabbedOverheadWhereTheyEndAboveTheFloor()
    {
        // The ladder stops two cells short of the floor; the hero standing under it reaches it.
        var (ship, _) = Start("""
            #######
            #.....#
            #.....#
            #.....#
            #..H..#
            #..H..#
            #.S...#
            #.....#
            #######
            """);
        var grabbed = At(3500, 8 * Unit).Step(Up, ship);
        Assert.True(grabbed.Climbing);
        Assert.Equal(8 * Unit - Spec.Climb, grabbed.Y);
    }

    [Fact]
    public void LaddersAreCaughtWhileFallingPastThem()
    {
        var (ship, _) = Start(LadderMap);
        var caught = At(3500, 6000, vy: 200).Step(Up, ship);
        Assert.True(caught.Climbing);
        Assert.Equal(6000 - Spec.Climb, caught.Y);
    }

    [Fact]
    public void LaddersAreNotGrabbedWhileWalkingOrJumpingNorFromBesideThem()
    {
        var (ship, _) = Start(LadderMap);
        var at = At(3500, 8 * Unit);
        Assert.False(at.Step(new HeroInput(1, -1, false), ship).Climbing);
        var falling = at with { Y = 6500, Vy = 200 };
        Assert.Equal(200 + Spec.Gravity, falling.Step(new HeroInput(1, -1, false), ship).Vy);
        var midAir = at with { Y = 6500 };
        var jumped = midAir.Step(new HeroInput(0, -1, true), ship);
        Assert.False(jumped.Climbing);
        Assert.Equal(Spec.Gravity, jumped.Vy);
        // The body overlaps the ladder by most of its width, but its middle is not on it.
        var beside = at with { X = 3 * Unit - 1 };
        Assert.Equal(beside, beside.Step(Up, ship));
    }

    [Fact]
    public void LaddersLetGoAtABottomThatOpensIntoAir()
    {
        var (ship, _) = Start("""
            #######
            #.....#
            #.....#
            #.....#
            #..H..#
            #..H..#
            #.S...#
            #.....#
            #.....#
            #######
            """);
        var h = At(3500, 4 * Unit).Step(Down, ship);
        for (int i = 0; i < 200 && h.Climbing; i++) h = h.Step(Down, ship);
        Assert.False(h.Climbing);
        Assert.True(h.Y < 9 * Unit);
        Assert.Equal(9 * Unit, Settle(ship, h, Still).Y);
    }

    [Fact]
    public void LaddersAreLetGoByWalkingOrJumping()
    {
        var (ship, _) = Start(LadderMap);
        var middle = At(3500, 6 * Unit, climbing: true);
        var walked = middle.Step(Left, ship);
        Assert.False(walked.Climbing);
        Assert.Equal(Spec.Gravity, walked.Vy);
        Assert.Equal(8 * Unit, Settle(ship, walked, Still).Y);
        var jumped = middle.Step(Jump, ship);
        Assert.False(jumped.Climbing);
        Assert.Equal(-Spec.Jump + Spec.Gravity, jumped.Vy);
    }

    [Fact]
    public void LaddersEndOnAFloorTheHeroClimbsDownOntoNotThrough()
    {
        var (ship, _) = Start("""
            #######
            #.....#
            #.....#
            #.....#
            #..H..#
            #..H..#
            #.S=..#
            #.....#
            #.....#
            #######
            """);
        var top = At(3500, 4 * Unit);
        Assert.Equal(6 * Unit, Settle(ship, top.Step(Down, ship), Down).Y);
        Assert.Equal(6 * Unit, Settle(ship, top with { Y = 6 * Unit }, Down).Y);
    }

    [Fact]
    public void LaddersAreFallenThroughBelowTheirTop()
    {
        var (ship, _) = Start(LadderMap);
        Assert.Equal(8 * Unit, Settle(ship, At(3500, 6500), Still).Y);
    }

    [Fact]
    public void LaddersLeadUpAShaftOneCellWideIntoTheRoomAbove()
    {
        var (ship, hero) = Start("""
            #####
            #...#
            #...#
            #...#
            ##H##
            ##H##
            #.H.#
            #.H.#
            #SH.#
            #####
            """);
        var atLadder = Until(ship, hero, Right, h => Column(h) == 2);
        var above = Settle(ship, atLadder.Step(Up, ship), Up);
        Assert.False(above.Climbing);
        Assert.Equal(4 * Unit, above.Y);
    }

    [Fact]
    public void LaddersLeadThroughAnOpenHatchIntoTheRoomAboveButNotAClosedOne()
    {
        string Room(char hatch) => $"""
            ######
            #....#
            #....#
            #....#
            ##{hatch}###
            #.H..#
            #.H..#
            #.HS.#
            ######
            """;
        var open = Start(Room('d'));
        var atLadder = Until(open.Ship, open.Hero, Left, h => Column(h) == 2);
        var above = Settle(open.Ship, atLadder.Step(Up, open.Ship), Up);
        Assert.False(above.Climbing);
        Assert.Equal(4 * Unit, above.Y);

        var closed = Start(Room('D'));
        var stuck = Settle(closed.Ship, atLadder.Step(Up, closed.Ship), Up);
        Assert.True(stuck.Climbing);
        Assert.Equal(5 * Unit, stuck.Y - Spec.Height);
    }

    internal static readonly Layout Wreck = Registry.WreckMap;
    internal static readonly int Spawn = new TestWorld(Wreck).M.Crew.Spawn;
    /// <summary>The wreck with every door open, the airlock's two doorways among them.</summary>
    /// <summary>The middle of room B's last cell of hull before space, where a tether out of it is tied.</summary>
    internal const int TornEdge = 36 * Unit + Unit / 2;

    internal static Ship Opened() => ShipFrom(Wreck.Rows.Select(r => r.Replace('D', 'd')).ToArray());

    [Fact]
    public void CrossesTheWholeWreckOnceItsDoorsAreOpen()
    {
        var ship = ShipFrom(Wreck.Rows);
        var opened = Opened();

        // From room A, where the hero wakes, through the doorway into room B, torn open to space, afloat
        // and untied while within the hull; on its tether from the torn edge, as far as it lets the hero.
        Assert.Equal(TornEdge - Spec.Tether, Settle(opened, Hero.At(opened, Spawn), Left).X);

        // With the doorway closed, the hero stops at it, past the generator.
        Assert.Equal(46 * Unit + Half, Settle(ship, Hero.At(ship, Spawn), Left).X);
    }

    /// <summary>How far the body's middle is from where its tether is fixed.</summary>
    private static double ReachOf(Hero h) => h.Anchor is Point a
        ? Math.Sqrt(Math.Pow(h.Middle.X - a.X, 2) + Math.Pow(h.Middle.Y - a.Y, 2))
        : 0;

    [Fact]
    public void InSpaceSwimsFourWaysAtItsSpeedOnNoTetherStoppedByTheGridsEdgeLikeAWall()
    {
        var opened = Opened();
        // A hero in open space far from the hull, put there rather than swum: on no tether.
        var h = Hero.At(opened, 40 * opened.Width + 120);
        Assert.Null(h.Anchor);
        Assert.Equal(h.Y + Spec.Swim, h.Step(Down, opened).Y);
        h = Settle(opened, h, Down);
        Assert.Equal(opened.Height * Unit, h.Y);
        h = Settle(opened, h, Up);
        Assert.Equal(0, h.Y - Spec.Height);
        Assert.Equal(h.X - Spec.Swim, h.Step(Left, opened).X);
        h = Settle(opened, h, Left);
        Assert.Equal(Half, h.X);
        h = Settle(opened, h, Right);
        Assert.Equal(opened.Width * Unit - Half, h.X);
        Assert.Null(h.Anchor);
    }

    [Fact]
    public void TiesATetherLeavingAShipOpenToSpaceAllThroughNeverLettingTheHeroDriftOff()
    {
        // Every door broken out and room B torn: the whole wreck is space, yet still the hull.
        var opened = ShipFrom(Layout.Parse(Wreck.Rows.Select(r => r.Replace('D', '.')).ToArray(), Wreck.Under));
        var h = Hero.At(opened, Spawn);
        Assert.True(opened.Space(h.BodyCell(opened.Width)));
        h = h.Step(Right, opened);
        Assert.Null(h.Anchor);
        h = Settle(opened, h, Right);
        Assert.NotNull(h.Anchor);
        Assert.True(ReachOf(h) <= Spec.Tether);
    }

    [Fact]
    public void InSpaceFloatsOutThroughTheAirlockOnATetherTiedWhereTheBodyLeftItAndFallsOnceBackIn()
    {
        var opened = Opened();
        int width = opened.Width;
        var h = Hero.At(opened, Spawn);
        int aboard = h.BodyCell(width);
        while (opened.Space(aboard) || !opened.Space(h.BodyCell(width)))
        {
            aboard = h.BodyCell(width);
            h = h.Step(Right, opened);
        }
        // Past the outer doorway nothing pulls the hero down, and the tether is tied in the doorway.
        Assert.Equal(23 * Unit, h.Y);
        Assert.Equal(new Point(aboard % width * Unit + Unit / 2, aboard / width * Unit + Unit / 2), h.Anchor);
        // Back in: aboard, the tether is let go and gravity takes the hero to the airlock's floor.
        h = Until(opened, h, Left, x => Column(x) == 56);
        Assert.Null(h.Anchor);
        h = Settle(opened, h, Still);
        Assert.Equal(23 * Unit, h.Y);
        Assert.Equal(0, h.Vy);
    }

    private static readonly Gen<HeroInput> Inputs =
        from x in Gen.Elements(-1, 0, 1)
        from y in Gen.Elements(-1, 0, 1)
        from jump in Gen.Elements(false, true)
        select new HeroInput(x, y, jump);

    [Fact]
    public void InSpaceKeepsTheBodyWithinTheTetherSlidingAlongItsLimitNeverThroughAWall()
    {
        var opened = Opened();
        var outside = Until(opened, Hero.At(opened, Spawn), Right, h => h.Anchor != null);
        // Straight out until taut: stopped at the tether's length, level with where it is tied.
        var taut = Settle(opened, outside, Right);
        Assert.True(ReachOf(taut) <= Spec.Tether);
        Assert.True(ReachOf(taut) > Spec.Tether - Spec.Swim);
        // Pushing on out and up, the hero slides up along the limit rather than stopping.
        var slid = taut.Step(new HeroInput(1, -1, false), opened);
        Assert.True(slid.Y < taut.Y);
        Assert.True(ReachOf(slid) <= Spec.Tether);
    }

    private static readonly Gen<List<(HeroInput, int)>> Moves =
        (from keys in Inputs from steps in Gen.Choose(1, 200) select (keys, steps)).ListOf().Select(l => l.Take(30).ToList());

    [Property(MaxTest = 60)]
    public Property InSpaceNeverLetsTheBodyPastTheTether()
    {
        var opened = Opened();
        var outside = Until(opened, Hero.At(opened, Spawn), Right, h => h.Anchor != null);
        return Prop.ForAll(Moves.ToArbitrary(), list =>
        {
            var h = outside;
            foreach (var (keys, steps) in list)
                for (int i = 0; i < steps; i++)
                {
                    h = h.Step(keys, opened);
                    if (ReachOf(h) > Spec.Tether) return false;
                }
            return true;
        });
    }

    [Fact]
    public void InSpaceStopsWithTheKeysAndStaysPutWithNothingPressedPassedOutToo()
    {
        var opened = Opened();
        var adrift = Hero.At(opened, 40 * opened.Width + 120);
        var moved = adrift.Step(new HeroInput(1, -1, false), opened);
        Assert.Equal((adrift.X + Spec.Swim, adrift.Y - Spec.Swim), (moved.X, moved.Y));
        var away = adrift.Step(new HeroInput(-1, -1, true), opened).Step(Still, opened);
        Assert.Equal((adrift.X - Spec.Swim, adrift.Y - Spec.Swim), (away.X, away.Y));
        Assert.Equal(away with { Vx = 0, Vy = 0 }, away.Step(Still, opened));
    }

    [Fact]
    public void InSpaceWakesInTheShipUnderGravityAgainOnNoTether()
    {
        var opened = Opened();
        var woke = Hero.At(opened, Spawn);
        Assert.Null(woke.Anchor);
        Assert.Equal(woke.Y, woke.Step(Up, opened).Y);
        Assert.Equal(Spec.Gravity, (woke with { Y = woke.Y - Unit }).Step(Still, opened).Vy);
    }

    private static readonly char[] Cells = [' ', '.', '#', '#', '=', 'H', 'H', 'D', 'd'];

    [Property(MaxTest = 300)]
    public Property NeverEndsAStepInsideAWallAClosedDoorOrPastTheEdge()
    {
        var gen =
            from width in Gen.Choose(1, 10)
            from height in Gen.Choose(1, 16)
            from cells in Gen.Elements(Cells).ArrayOf(width * height)
            from start in Gen.Choose(0, width * height - 1)
            from inputs in (from keys in Inputs from steps in Gen.Choose(1, 40) select (keys, steps)).ListOf()
            select (width, height, cells, start, inputs: inputs.Take(30).ToList());
        return Prop.ForAll(gen.ToArbitrary(), t =>
        {
            var rows = Enumerable.Range(0, t.height).Select(y => new string(t.cells, y * t.width, t.width)).ToArray();
            var ship = ShipFrom(rows);
            bool Blocked(int x, int y) =>
                x < 0 || y < 0 || x >= ship.Width || y >= ship.Height ||
                ship.StructureAt(y * ship.Width + x) is Structure.Wall or Structure.DoorClosed;
            // The hero starts standing in its cell, its body reaching up into the cells above.
            int sx = t.start % ship.Width, sy = t.start / ship.Width;
            int head = (int)Math.Floor(((sy + 1) * Unit - Spec.Height) / (double)Unit);
            for (int y = head; y <= sy; y++)
                if (Blocked(sx, y)) return true;

            var h = Hero.At(ship, t.start);
            foreach (var (held, steps) in t.inputs)
                for (int i = 0; i < steps; i++)
                {
                    h = h.Step(i == 0 ? held : held with { Jump = false }, ship);
                    int l = Floor(h.X - Half), r = Floor(h.X + Half - 1);
                    int top = Floor(h.Y - Spec.Height), b = Floor(h.Y - 1);
                    for (int y = top; y <= b; y++)
                        for (int x = l; x <= r; x++)
                            if (Blocked(x, y)) return false;
                }
            return true;
        });
    }

    private static int Floor(int v) => (int)Math.Floor(v / (double)Unit);
}
