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

    /// <summary>A map character's width and height: two cells (0104), the 16 px every map is drawn at.</summary>
    internal const int Block = 2 * Unit;

    internal static Ship ShipFrom(Layout layout)
    {
        var m = new Registry();
        World.Create(1, layout, m.All);
        return Ship.Of(m.Grid, m.Rooms);
    }

    /// <summary>The bottom-left cell of the block map character (<paramref name="x"/>, <paramref name="y"/>) became: where <see cref="Hero.At"/> stands a hero in its middle.</summary>
    internal static int BlockCell(Ship ship, int x, int y) => (2 * y + 1) * ship.Width + 2 * x;

    /// <summary>The ship a map draws, a character a 16 px block (<see cref="Layout.Doubled"/>).</summary>
    internal static Ship ShipFrom(string map) => ShipFrom(Layout.Parse(map).Doubled());
    internal static Ship ShipFrom(IReadOnlyList<string> rows) => ShipFrom(Layout.Parse(rows).Doubled());

    /// <summary>The ship and a hero standing where the crew makes heroes: its lifepod's hatch, or its S.</summary>
    private static (Ship Ship, Hero Hero) Start(Layout layout)
    {
        var ship = ShipFrom(layout);
        int spawn = new TestWorld(layout).M.Crew.Spawn;
        return (ship, Hero.At(ship, spawn));
    }

    private static (Ship Ship, Hero Hero) Start(string map) => Start(Layout.Parse(map).Doubled());
    private static (Ship Ship, Hero Hero) Start(IReadOnlyList<string> rows) => Start(Layout.Parse(rows).Doubled());

    internal static readonly HeroInput Still = new(0, 0, false);
    internal static readonly HeroInput Left = Still with { X = -1 };
    internal static readonly HeroInput Right = Still with { X = 1 };
    private static readonly HeroInput Up = Still with { Y = -1 };
    private static readonly HeroInput Down = Still with { Y = 1 };
    private static readonly HeroInput Jump = Still with { Jump = true };
    /// <summary>Space held down after a jump's press, so it rises its full height (0089).</summary>
    internal static readonly HeroInput Held = Still with { JumpHeld = true };

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

    private static int Column(Hero h) => h.X / Block;

    private static Hero Run(Ship ship, Hero hero, HeroInput input, int steps)
    {
        for (int i = 0; i < steps; i++) hero = hero.Step(input, ship);
        return hero;
    }

    // Rooms are drawn three characters tall at least: the hero stands 5.25 cells, under their six.
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
        Assert.Equal(6 * Block, landed.Y);
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
        Assert.Equal(hero.X + Spec.WalkAccel, hero.Step(Right, ship).X);
        Assert.Equal(6 * Block - Half, Settle(ship, hero, Right).X);
        Assert.Equal(1 * Block + Half, Settle(ship, hero, Left).X);
    }

    [Fact]
    public void RunsWithShiftHeldFasterThanItWalksButSwimsNoFasterForItInSpace()
    {
        var (ship, hero) = Start(Room);
        Assert.Equal(Spec.Run, Run(ship, hero, Right with { Sprint = true }, 7).Vx);
        Assert.True(Spec.Run > Spec.Walk);
        var opened = Opened();
        var h = Hero.At(opened, 80 * opened.Width + 240);
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
        var h = Hero.At(opened, 80 * opened.Width + 240);
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
        Assert.Equal((4 + depth) * Block, h.Y);
    }

    [Fact]
    public void IsStoppedByAClosedDoorwayAndWalksThroughAnOpenOneSixCellsTall()
    {
        string Doorway(char door, char? top = null) => $"""
            ########
            #...{top ?? door}..#
            #...{door}..#
            #.S.{door}..#
            ########
            """;
        var closed = Start(Doorway('D'));
        Assert.Equal(4 * Block - Half, Settle(closed.Ship, closed.Hero, Right).X);
        var open = Start(Doorway('d'));
        Assert.Equal(7 * Block - Half, Settle(open.Ship, open.Hero, Right).X);
        // Four cells of open door under a wall are too low.
        var low = Start(Doorway('d', '#'));
        Assert.Equal(4 * Block - Half, Settle(low.Ship, low.Hero, Right).X);
    }

    // Was JumpsOntoALedgeThreeCellsHighNotFour, at 16 px: a held jump's feet come 102 short of eight cells, inside the drawn foot's
    // reach at its top, so the ledge assist puts the hero on it (0092). Ten is still out of reach.
    [Fact]
    public void JumpsOntoALedgeEightCellsHighWithTheLedgeAssistNotTen()
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
        h = Until(three.Ship, h, Held with { X = 1 }, x => Column(x) == 3);
        Assert.Equal(6 * Block, Settle(three.Ship, h, Still).Y);

        var four = Ledge(4);
        var f = Run(four.Ship, four.Hero.Step(Jump, four.Ship), Held with { X = 1 }, 200);
        Assert.True(Column(f) >= 3);
        Assert.Equal(5 * Block, f.Y);

        var five = Ledge(5);
        var g = Run(five.Ship, five.Hero.Step(Jump, five.Ship), Held with { X = 1 }, 200);
        Assert.True(Column(g) < 3);
        Assert.Equal(9 * Block, g.Y);
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
        Assert.Equal(1 * Block, top.Y - Spec.Height);
        Assert.Equal(0, top.Vy);
        Assert.True(top.Step(Still, ship).Y > top.Y);
        // Head against the ceiling, the hero still walks into the next column.
        var atEdge = top with { X = 3 * Block - Half };
        Assert.Equal(atEdge.X + Spec.WalkAccel * Spec.AirControl / 1000, atEdge.Step(Right, ship).X);
        Assert.Equal(4 * Block, Settle(ship, top, Still).Y);
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
        var off = Until(ship, hero, Right, h => h.X - Half >= 2 * Block);
        // Pressed into the wall of the ledge, the hero still falls all the way down.
        var fallen = Settle(ship, off with { X = 2 * Block + Half }, Left);
        Assert.Equal(2 * Block + Half, fallen.X);
        Assert.Equal(6 * Block, fallen.Y);
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
        var edge = hero with { X = 2 * Block + Half - 1 };
        Assert.Equal(4 * Block, Settle(ship, edge, Still).Y);
        Assert.Equal(5 * Block, Settle(ship, edge with { X = edge.X + 1 }, Still).Y);

        var mirrored = Start("""
            ######
            #....#
            #....#
            #...S#
            #...##
            ######
            """);
        var other = mirrored.Hero with { X = 4 * Block - Half + 1 };
        Assert.Equal(4 * Block, Settle(mirrored.Ship, other, Still).Y);
        Assert.Equal(5 * Block, Settle(mirrored.Ship, other with { X = other.X - 1 }, Still).Y);
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
        var under = Settle(ship, hero with { X = 3 * Block }, Still);
        Assert.Equal(8 * Block, under.Y);
        var on = Settle(ship, under.Step(Jump, ship), Still);
        Assert.Equal(7 * Block, on.Y);
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
        Assert.Equal(5 * Block - Half, Settle(ship, hero, Right).X);
    }

    // Two decks, the floor between them cut a character, two cells, wide in column 4; room above to jump.
    private const string HoleMap = """
        #########
        #.......#
        #.......#
        #.......#
        #.......#
        #.......#
        #S......#
        ####.####
        #.......#
        #.......#
        #.......#
        #########
        """;

    private const int Deck = 7 * Block, Below = 11 * Block;

    // A step moves the hero further than the 100 thousandths two cells leave beside its body, so walking
    // used to step over a hole that wide or into it by where the hero started (playtest 1).
    [Fact]
    public void FallsIntoAHoleTwoCellsWideWalkedOntoFromEitherSideFromAnywhere()
    {
        var (ship, _) = Start(HoleMap);
        var missed = new List<string>();
        foreach (bool sprint in new[] { false, true })
            for (int offset = 0; offset < Spec.Run; offset++)
                foreach (var (from, keys) in new[] { (3200 + offset, Right), (14800 - offset, Left) })
                {
                    var walked = Until(ship, At(from, Deck), keys with { Sprint = sprint }, h => h.Y > Deck || h.X < 3200 || h.X > 14800);
                    var landed = Settle(ship, walked, Still);
                    if (landed.Y != Below || Column(landed) != 4) missed.Add($"from {from} {(sprint ? "running" : "walking")}");
                }
        Assert.True(missed.Count == 0, $"{missed.Count} of {4 * Spec.Run} missed: {string.Join(", ", missed.Take(8))}");
    }

    [Fact]
    public void StandingOverAHoleTwoCellsWideWithDownSlidesInAndFalls()
    {
        var (ship, _) = Start(HoleMap);
        foreach (int x in new[] { 4 * Block + 10, 4 * Block + Half - 1, 5 * Block - Half + 1, 5 * Block - 10 })
        {
            var over = At(x, Deck);
            // Settled, it has stood: its coyote timer is at the ground's (0089).
            Assert.Equal(over with { Airborne = 0 }, Settle(ship, over, Still));
            var fallen = Settle(ship, over, Down);
            Assert.Equal(Below, fallen.Y);
            Assert.InRange(fallen.X, 4 * Block + Half, 5 * Block - Half);
        }
        // With the middle beside the hole, down does nothing.
        var beside = At(4 * Block - 10, Deck);
        Assert.Equal(beside with { Airborne = 0 }, Settle(ship, beside, Down));
    }

    [Fact]
    public void JumpsOverAHoleTwoCellsWideAndWalksOverALadderTopInOne()
    {
        var (ship, _) = Start(HoleMap);
        var jumped = Settle(ship, At(3200, Deck).Step(Jump with { X = 1 }, ship), Held with { X = 1 });
        Assert.Equal(Deck, jumped.Y);
        Assert.Equal(8 * Block - Half, jumped.X);

        var laddered = ShipFrom(HoleMap.Replace("####.####", "####H####"));
        var walked = Settle(laddered, At(3200, Deck), Right);
        Assert.Equal(Deck, walked.Y);
        Assert.Equal(8 * Block - Half, walked.X);
    }

    // Two floors, one above the other, over the bottom of the room; room above to jump.
    private const string FloorsMap = """
        #######
        #.....#
        #.....#
        #.....#
        #.....#
        #.....#
        #..S..#
        #=====#
        #.....#
        #.....#
        #.....#
        #=====#
        #.....#
        #.....#
        #.....#
        #######
        """;

    [Fact]
    public void DropsThroughAFloorWithDownWalkingOrNot()
    {
        var (ship, hero) = Start(FloorsMap);
        Assert.Equal(7 * Block, Settle(ship, hero, Still).Y);
        var dropped = Until(ship, hero, Down, h => h.Y > 7 * Block);
        Assert.Equal(hero.X, dropped.X);
        Assert.Equal(11 * Block, Settle(ship, dropped, Still).Y);
        Assert.True(Until(ship, hero, Down with { X = 1 }, h => h.Y > 7 * Block || h.X > 5 * Block).Y > 7 * Block);
        // Held, down drops through every floor it lands on.
        Assert.Equal(15 * Block, Settle(ship, hero, Down).Y);
    }

    [Fact]
    public void JumpsRatherThanDropsWithDownHeld()
    {
        var (ship, hero) = Start(FloorsMap);
        Assert.Equal(-Spec.Jump + Spec.Gravity, hero.Step(Down with { Jump = true }, ship).Vy);
    }

    [Fact]
    public void DownDropsThroughNoWallNorALadderTopEvenPartlyUnderTheFeet()
    {
        var (ship, _) = Start("""
            ########
            #......#
            #......#
            #......#
            #==##H=#
            #....H.#
            #....H.#
            #....H.#
            ########
            """);
        // Half on the floor, half on the wall; then half on the floor, half on the ladder's top.
        foreach (int x in new[] { 3 * Block, 6 * Block })
        {
            var standing = At(x, 4 * Block);
            Assert.Equal(4 * Block, Settle(ship, standing, Down with { X = 0 }).Y);
            Assert.Equal(4 * Block, standing.Step(Down with { X = 1 }, ship).Y);
        }
        // Wholly on the floor, it drops.
        Assert.Equal(8 * Block, Settle(ship, At(3400, 4 * Block), Down).Y);
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
        // Eased onto its middle, not snapped (0092).
        Assert.True(Math.Abs(grabbed.X - 7000) < Math.Abs(near.X - 7000) && grabbed.X != 7000, $"{grabbed.X} from {near.X}");
        Assert.Equal(near.Y - Spec.ClimbUp, grabbed.Y);
        // The step the feet reach the top, the hero steps off onto it: no hop past it and back, nor a step longer than a climb's.
        var top = grabbed;
        for (int i = 0; i < 200 && top.Climbing; i++)
        {
            var was = top;
            top = top.Step(Up, ship);
            Assert.True(top.Y >= 4 * Block);
            Assert.InRange(was.Y - top.Y, 0, Spec.ClimbUp);
        }
        Assert.Equal(7000, top.X);
        Assert.False(top.Climbing);
        Assert.Equal(4 * Block, top.Y);
        for (int i = 0; i < 30; i++)
        {
            top = top.Step(Up, ship);
            Assert.Equal(4 * Block, top.Y);
        }
    }

    [Fact]
    public void LaddersAreClimbedDownFromTheTopWithDownToTheFloor()
    {
        var (ship, _) = Start(LadderMap);
        var top = At(7000, 4 * Block);
        var grabbed = top.Step(Down, ship);
        Assert.True(grabbed.Climbing);
        Assert.Equal(8 * Block, Settle(ship, grabbed, Down).Y);
        // A ladder's top holds a hero up, so a jump starts from it.
        Assert.True(top.Step(Jump, ship).Y < top.Y);
    }

    [Fact]
    public void LaddersHoldTheHeroInPlaceWhileNothingIsPressed()
    {
        var (ship, _) = Start(LadderMap);
        var h = At(7000, 13000, climbing: true);
        for (int i = 0; i < 30; i++) h = h.Step(Still, ship);
        Assert.Equal(At(7000, 13000, climbing: true), h);
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
        var grabbed = At(7000, 8 * Block).Step(Up, ship);
        Assert.True(grabbed.Climbing);
        Assert.Equal(8 * Block - Spec.ClimbUp, grabbed.Y);
    }

    [Fact]
    public void LaddersAreCaughtWhileFallingPastThem()
    {
        var (ship, _) = Start(LadderMap);
        var caught = At(7000, 12000, vy: 400).Step(Up, ship);
        Assert.True(caught.Climbing);
        Assert.Equal(12000 - Spec.ClimbUp, caught.Y);
    }

    [Fact]
    public void AJumpOffALadderGoesOnWithUpHeldAndAWalkStepsOff()
    {
        var (ship, _) = Start(LadderMap);
        var on = At(7000, 13000, climbing: true);
        // Jumping with up held: the hero rises off the ladder, not caught again by it for Hero.Regrab steps (0077, 0092).
        var h = on.Step(new HeroInput(0, -1, true), ship);
        Assert.False(h.Climbing);
        for (int i = 0; i < 10; i++)
        {
            h = h.Step(Up, ship);
            Assert.False(h.Climbing);
        }
        Assert.True(h.Y < on.Y - Block, $"{h.Y} from {on.Y}");
        // A jump off it to either side lets go, side and jump pressed together.
        Assert.False(on.Step(new HeroInput(1, 0, true), ship).Climbing);
        Assert.False(on.Step(new HeroInput(-1, -1, true), ship).Climbing);
        // A walk off it at its foot, standing on the floor, lets go.
        Assert.False(At(7000, 8 * Block, climbing: true).Step(Right, ship).Climbing);
    }

    [Fact]
    public void ASideHeldOnALadderInMidAirTurnsTheHeroAndAJumpThenGoesThatWay()
    {
        var (ship, _) = Start(LadderMap);
        var on = At(7000, 13000, climbing: true);
        // Side first: held a while, the hero stays on the ladder, facing that way.
        var held = on;
        for (int i = 0; i < 20; i++) held = held.Step(Right, ship);
        Assert.True(held.Climbing);
        Assert.Equal((7000, 13000, 1), (held.X, held.Y, held.Facing));
        var left = on.Step(new HeroInput(-1, 0, false), ship);
        Assert.Equal((true, -1), (left.Climbing, left.Facing));
        // Then jump, the side still held: off at once, rising and going that way.
        var jumped = held.Step(new HeroInput(1, 0, true), ship);
        Assert.False(jumped.Climbing);
        for (int i = 0; i < 8; i++) jumped = jumped.Step(Right, ship);
        Assert.True(jumped.Y < on.Y && jumped.X > on.X + Block / 2, $"({jumped.X}, {jumped.Y}) from ({on.X}, {on.Y})");
        // Jump first, then the side: off straight up, the side steering it in the air.
        var up = on.Step(Jump, ship);
        Assert.False(up.Climbing);
        for (int i = 0; i < 8; i++) up = up.Step(Right, ship);
        Assert.True(up.Y < on.Y && up.X > on.X, $"({up.X}, {up.Y})");
    }

    // Was LaddersAreNotGrabbedWhileWalkingOrJumpingNorFromBesideThem: up now grabs walking and rising too, by design (0092).
    [Fact]
    public void LaddersAreGrabbedWithUpWalkingFallingOrRisingButNotOnAJumpsPressNorFromBesideThem()
    {
        var (ship, _) = Start(LadderMap);
        var at = At(7000, 8 * Block);
        Assert.True(at.Step(new HeroInput(1, -1, false), ship).Climbing);
        Assert.True((at with { Vx = Spec.Run }).Step(new HeroInput(1, -1, false, Sprint: true), ship).Climbing);
        var falling = at with { Y = 13000, Vy = 400 };
        Assert.True(falling.Step(new HeroInput(1, -1, false), ship).Climbing);
        var rising = at with { Y = 13000, Vy = -600 };
        Assert.True(rising.Step(Up, ship).Climbing);
        // Down still grabs only with no side held.
        Assert.False((at with { Y = 4 * Block }).Step(new HeroInput(1, 1, false), ship).Climbing);
        var midAir = at with { Y = 13000 };
        var jumped = midAir.Step(new HeroInput(0, -1, true), ship);
        Assert.False(jumped.Climbing);
        // No jump: it falls, at a jump's top's gravity with Space held (0089).
        Assert.Equal(ship.ApexGravity, jumped.Vy);
        // The body overlaps the ladder by most of its width, but its middle is not on it.
        var beside = at with { X = 3 * Block - 1 };
        Assert.Equal(beside with { Airborne = 0 }, beside.Step(Up, ship));
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
        var h = At(7000, 4 * Block).Step(Down, ship);
        for (int i = 0; i < 200 && h.Climbing; i++) h = h.Step(Down, ship);
        Assert.False(h.Climbing);
        Assert.True(h.Y < 9 * Block);
        Assert.Equal(9 * Block, Settle(ship, h, Still).Y);
    }

    [Fact]
    public void LaddersAreLetGoByJumpingOrByWalkingAtTheirFoot()
    {
        var (ship, _) = Start(LadderMap);
        var middle = At(7000, 6 * Block, climbing: true);
        // In mid-air a side only turns the hero on the ladder, ready to jump that way (0077); at its foot it walks off.
        Assert.True(middle.Step(Left, ship).Climbing);
        var walked = At(7000, 8 * Block, climbing: true).Step(Left, ship);
        Assert.False(walked.Climbing);
        Assert.Equal(8 * Block, Settle(ship, walked, Still).Y);
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
        var top = At(7000, 4 * Block);
        Assert.Equal(6 * Block, Settle(ship, top.Step(Down, ship), Down).Y);
        Assert.Equal(6 * Block, Settle(ship, top with { Y = 6 * Block }, Down).Y);
    }

    [Fact]
    public void LaddersAreFallenThroughBelowTheirTop()
    {
        var (ship, _) = Start(LadderMap);
        Assert.Equal(8 * Block, Settle(ship, At(7000, 13000), Still).Y);
    }

    [Fact]
    public void LaddersLeadUpAShaftTwoCellsWideIntoTheRoomAbove()
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
        Assert.Equal(4 * Block, above.Y);
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
        Assert.Equal(4 * Block, above.Y);

        var closed = Start(Room('D'));
        var stuck = Settle(closed.Ship, atLadder.Step(Up, closed.Ship), Up);
        Assert.True(stuck.Climbing);
        // Its head under the hatch, two cells thick, its block whole (0104).
        Assert.Equal(5 * Block, stuck.Y - Spec.Height);
    }

    // A hatch three wide in a deck, a ladder under its middle (0067).
    private static string HatchMap(char hatch) => $"""
        #######
        #.....#
        #.....#
        #.....#
        ##{hatch}{hatch}{hatch}##
        #..H..#
        #..H..#
        #..HS.#
        #######
        """;

    [Fact]
    public void DownOnAClosedHatchDoesNothingOverTheLadderOrBesideIt()
    {
        var (ship, _) = Start(HatchMap('D'));
        foreach (int x in new[] { 5000, 7000, 9000 })
        {
            var stood = Settle(ship, At(x, 4 * Block), Down);
            Assert.Equal((4 * Block, false), (stood.Y, stood.Climbing));
        }
    }

    [Fact]
    public void DownOnAnOpenHatchOverALadderClimbsDownAndBesideItTheHeroFalls()
    {
        var (ship, _) = Start(HatchMap('d'));
        var top = At(7000, 4 * Block);
        Assert.Equal(4 * Block, Settle(ship, top, Still).Y);
        var grabbed = top.Step(Down, ship);
        Assert.True(grabbed.Climbing);
        Assert.Equal(8 * Block, Settle(ship, grabbed, Down).Y);
        Assert.Equal(8 * Block, Settle(ship, At(5000, 4 * Block), Still).Y);
    }

    internal static readonly Layout Wreck = Registry.WreckMap;
    internal static readonly int Spawn = new TestWorld(Wreck).M.Crew.Spawn;
    /// <summary>Room B's torn edge, its last cell of hull's left side: a tether out of it is tied where the body's middle last was, a step's walk at most inside.</summary>
    internal const int TornEdge = 36 * Block;

    /// <summary>The wreck with every door open, the airlock's two doorways among them.</summary>
    internal static Ship Opened() => ShipFrom(Layout.Parse(Wreck.Rows.Select(r => r.Replace('D', 'd')).ToArray()));

    [Fact]
    public void CrossesTheWholeWreckOnceItsDoorsAreOpen()
    {
        var ship = ShipFrom(Layout.Parse(Wreck.Rows));
        var opened = Opened();

        // From room A, where the hero wakes, through the doorway into room B, torn open to space, afloat
        // and untied while within the hull; on its tether from the torn edge, as far as it lets the hero.
        Assert.Equal(TornEdge - Spec.Tether, Settle(opened, Hero.At(opened, Spawn), Left).X);

        // With the doorway closed, the hero stops at it, past the generator: at the door, two cells thick, its block whole (0104).
        Assert.Equal(46 * Block + Half, Settle(ship, Hero.At(ship, Spawn), Left).X);
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
        var h = Hero.At(opened, 80 * opened.Width + 240);
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
        var aboard = h;
        while (opened.Space(aboard.BodyCell(width)) || !opened.Space(h.BodyCell(width)))
        {
            aboard = h;
            h = h.Step(Right, opened);
        }
        // Past the outer doorway nothing pulls the hero down, and the tether is tied in the doorway, where the body's middle last was.
        Assert.Equal(23 * Block, h.Y);
        Assert.Equal(aboard.Middle, h.Anchor);
        // Back in: aboard, the tether is let go and gravity takes the hero to the airlock's floor.
        h = Until(opened, h, Left, x => Column(x) == 56);
        Assert.Null(h.Anchor);
        h = Settle(opened, h, Still);
        Assert.Equal(23 * Block, h.Y);
        Assert.Equal(0, h.Vy);
    }

    private static readonly Gen<HeroInput> Inputs =
        from x in Gen.Elements(-1, 0, 1)
        from y in Gen.Elements(-1, 0, 1)
        from jump in Gen.Elements(false, true)
        from held in Gen.Elements(false, true)
        select new HeroInput(x, y, jump, JumpHeld: held);

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
        var adrift = Hero.At(opened, 80 * opened.Width + 240);
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
        Assert.Equal(Spec.Gravity, (woke with { Y = woke.Y - Block }).Step(Still, opened).Vy);
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
            bool Inside(Hero h)
            {
                int l = Floor(h.X - Half), r = Floor(h.X + Half - 1);
                int top = Floor(h.Y - Spec.Height), b = Floor(h.Y - 1);
                for (int y = top; y <= b; y++)
                    for (int x = l; x <= r; x++)
                        if (Blocked(x, y)) return true;
                return false;
            }
            // The hero starts standing in the middle of its character's block, its body reaching up into the cells above.
            var h = Hero.At(ship, (2 * (t.start / t.width) + 1) * ship.Width + 2 * (t.start % t.width));
            if (Inside(h)) return true;
            foreach (var (held, steps) in t.inputs)
                for (int i = 0; i < steps; i++)
                {
                    h = h.Step(i == 0 ? held : held with { Jump = false }, ship);
                    if (Inside(h)) return false;
                }
            return true;
        });
    }

    private static int Floor(int v) => (int)Math.Floor(v / (double)Unit);
}
