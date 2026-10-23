using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Xunit;
using Spec = Stillwake.Content.Hero;

namespace Stillwake.Game.Tests;

/// <summary>
/// The hero on slopes (0069): up and down without a jump or a hop, their closed sides walls, holes and ladders beside them. A slope
/// does not double (0104): these maps are drawn a character a cell, an 8 px ramp, with room for a hero 5.25 cells tall.
/// </summary>
public class SlopeTests
{
    private const int Unit = Hero.Unit;
    private const int Half = Spec.Width / 2;

    private static readonly HeroInput Still = HeroTests.Still, Left = HeroTests.Left, Right = HeroTests.Right;
    private static readonly HeroInput Down = Still with { Y = 1 }, Up = Still with { Y = -1 };

    /// <summary>The ship of <paramref name="map"/>, a character a cell.</summary>
    internal static Ship Flat(string map) => HeroTests.ShipFrom(Layout.Parse(map));
    private static Ship Flat(IReadOnlyList<string> rows) => HeroTests.ShipFrom(Layout.Parse(rows));

    // A hill: three cells up, a plateau two wide, three down; flat ground either side on row 12's top.
    internal const string Hill = """
        ################
        #..............#
        #..............#
        #..............#
        #..............#
        #..............#
        #..............#
        #..............#
        #..............#
        #....../##\....#
        #...../####\...#
        #S.../######\..#
        ################
        """;

    /// <summary>The ground of <see cref="Hill"/> under the middle at <paramref name="x"/>, worked out from its picture, not the controller.</summary>
    private static int HillGround(int x)
    {
        int column = x / Unit, fx = x % Unit;
        return column switch
        {
            5 => 11 * Unit + 999 - fx,
            6 => 10 * Unit + 999 - fx,
            7 => 9 * Unit + 999 - fx,
            8 or 9 => 9 * Unit,
            10 => 9 * Unit + fx,
            11 => 10 * Unit + fx,
            12 => 11 * Unit + fx,
            _ => 12 * Unit,
        };
    }

    /// <summary>Steps <paramref name="keys"/> until the hero stops, asserting every step stands on the hill's ground, never in the air.</summary>
    private static Hero WalkTheHill(Ship ship, Hero hero, HeroInput keys)
    {
        var h = hero;
        for (int i = 0; i < 2000; i++)
        {
            var next = h.Step(keys, ship);
            Assert.True(next.Vy == 0, $"in the air at {next}");
            Assert.True(HillGround(next.X) == next.Y, $"off the ground at {next}: it is at {HillGround(next.X)}");
            if (next.X == h.X) return next;
            h = next;
        }
        throw new InvalidOperationException("never stopped");
    }

    [Fact]
    public void WalksUpAndDownAHillOnTheGroundEveryStepEitherWayWalkingOrRunning()
    {
        var ship = Flat(Hill);
        foreach (bool run in new[] { false, true })
        {
            var start = Hero.At(ship, 11 * ship.Width + 1);
            var over = WalkTheHill(ship, start, Right with { Sprint = run });
            Assert.Equal((15 * Unit - Half, 12 * Unit), (over.X, over.Y));
            var back = WalkTheHill(ship, over, Left with { Sprint = run });
            Assert.Equal((1 * Unit + Half, 12 * Unit), (back.X, back.Y));
        }
    }

    /// <summary>
    /// Up and down a 45° slope the hero keeps its speed across, as on the flat, so its pose never flickers between
    /// walking and running (playtest of 0069: running uphill lost speed at every column and froze the legs).
    /// </summary>
    [Fact]
    public void KeepsItsSpeedAndPoseEveryStepUpAndDownTheHillEitherWay()
    {
        var ship = Flat(Hill);
        foreach (bool run in new[] { false, true })
            foreach (var (from, keys, wall) in new[] { (1 * Unit + Half, Right, 15 * Unit - Half), (15 * Unit - Half, Left, 1 * Unit + Half) })
            {
                var want = run ? Pose.Run : Pose.Walk;
                int speed = run ? Spec.Run : Spec.Walk;
                var h = new Hero(from, 12 * Unit, 0, 0, false, keys.X);
                var seen = new List<string>();
                for (int i = 0; i < 400; i++)
                {
                    var next = h.Step(keys with { Sprint = run }, ship);
                    // The last step, flush against the room's far wall, is short.
                    if (next.X == wall) break;
                    var pose = Poses.PoseOf(next, false, false);
                    // From standing it first gains speed (0089), on the flat before the hill.
                    if (Gaining(h, next, run)) Assert.True(Math.Abs(next.X - from) < 3 * Unit, $"still gaining speed at {next}");
                    else if (pose != want || Math.Abs(next.Vx) != speed) seen.Add($"{pose} at x {next.X} vx {next.Vx}");
                    h = next;
                }
                Assert.Empty(seen);
                Assert.True(h.Step(keys with { Sprint = run }, ship).X == wall, $"never reached the far wall, stopped at {h}");
            }
    }

    [Fact]
    public void StandsOnTheHigherFootingButFollowsASlopeOffAPlateauRatherThanHangOnItsCorner()
    {
        var ship = Flat(Hill);
        // The middle on the foot of the ramp, the box's back over the flat: the slope is higher.
        Assert.Equal(11 * Unit + 899, HeroTests.Settle(ship, new Hero(5100, 11 * Unit, 0, 0, false, 1), Still).Y);
        // The middle on the flat, the box's front over the ramp's foot a thousandth higher: the flat holds, the box over the slope.
        Assert.Equal(12 * Unit, HeroTests.Settle(ship, new Hero(4900, 11 * Unit, 0, 0, false, 1), Still).Y);
        // Off the plateau's edge, the box's back still over it: the feet on the slope, not on the corner.
        Assert.Equal(9 * Unit + 100, HeroTests.Settle(ship, new Hero(10_100, 8 * Unit, 0, 0, false, 1), Still).Y);
    }

    [Fact]
    public void DownOnASlopeDoesNothing()
    {
        var ship = Flat(Hill);
        var on = HeroTests.Settle(ship, new Hero(6500, 10 * Unit, 0, 0, false, 1), Still);
        Assert.Equal(10 * Unit + 499, on.Y);
        Assert.Equal(on with { Vx = 0, Vy = 0 }, HeroTests.Settle(ship, on, Down) with { Vx = 0, Vy = 0 });
    }

    [Fact]
    public void JumpsOnASlopeAndLandsBackOnIt()
    {
        var ship = Flat(Hill);
        var on = HeroTests.Settle(ship, new Hero(6500, 10 * Unit, 0, 0, false, 1), Still);
        var up = on.Step(Still with { Jump = true }, ship);
        Assert.True(up.Y < on.Y);
        var landed = HeroTests.Settle(ship, up, Right);
        Assert.Equal(HillGround(landed.X), landed.Y);
    }

    /// <summary>A room six rows tall over <paramref name="floorRow"/>, row 7.</summary>
    private static Ship Floored(string floorRow) =>
        Flat(string.Join('\n', ["#########", .. Enumerable.Repeat("#.......#", 6), floorRow, "#########"]));

    [Theory]
    // A slope's closed side is a wall: ◢ from the right, ◣ from the left.
    [InlineData("#./...S.#", -1, 3 * Unit + Half)]
    [InlineData("#.S...\\.#", 1, 6 * Unit - Half)]
    // A ceiling slope on the floor, the head far above it, is a wall from either side.
    [InlineData("#.r...S.#", -1, 3 * Unit + Half)]
    [InlineData("#.S...7.#", 1, 6 * Unit - Half)]
    public void ASlopesClosedSideAndACeilingSlopeStopTheHeroLikeAWall(string floorRow, int dir, int stopsAt)
    {
        var ship = Floored(floorRow);
        var start = Hero.At(ship, 7 * ship.Width + floorRow.IndexOf('S'));
        var stopped = HeroTests.Settle(ship, start, dir < 0 ? Left : Right);
        Assert.Equal((stopsAt, 8 * Unit), (stopped.X, stopped.Y));
    }

    [Fact]
    public void FromItsOpenSideClimbsASlopeAndStepsOffItsTop()
    {
        var ship = Floored("#S../...#");
        var h = Hero.At(ship, 7 * ship.Width + 1);
        h = HeroTests.Until(ship, h, Right, x => x.X >= 4600);
        Assert.Equal(7 * Unit + 999 - (h.X - 4 * Unit), h.Y);
        // Off the top's closed side it falls to the floor.
        Assert.Equal(8 * Unit, HeroTests.Settle(ship, h, Right).Y);
    }

    // A stair built of ◣ one up and one across, air under every step but the lowest, up to a floor on row 7's top.
    private const string Stairs = """
        #################
        #...............#
        #...............#
        #...............#
        #...............#
        #...............#
        #...............#
        #==\............#
        #...\...........#
        #....\..........#
        #.....\.........#
        #......\........#
        #.......\.......#
        #........\......#
        #################
        """;

    // The same stair meeting a solid ledge at its top, a cliff down to the ground.
    private const string StairsToLedge = """
        #################
        #...............#
        #...............#
        #...............#
        #...............#
        #...............#
        #...............#
        ###\............#
        ###.\...........#
        ###..\..........#
        ###...\.........#
        ###....\........#
        ###.....\.......#
        ###......\......#
        #################
        """;

    /// <summary><paramref name="map"/> in a mirror: ◣ becomes ◢, a stair up to the left one up to the right.</summary>
    private static string Mirrored(string map) =>
        string.Join('\n', map.Split('\n').Select(row => new string(row.TrimEnd('\r').Reverse().Select(c => c switch { '/' => '\\', '\\' => '/', _ => c }).ToArray())));

    /// <summary>The ground under the middle at <paramref name="x"/>, worked out from the picture: the first thing down its column.</summary>
    private static int GroundUnder(Ship ship, int x)
    {
        int column = x / Unit;
        for (int row = 1; ; row++)
        {
            int cell = row * ship.Width + column;
            if (ship.StructureAt(cell) == Structure.Empty) continue;
            return row * Unit + Shape.TopAt(ship.ShapeAt(cell), x % Unit);
        }
    }

    /// <summary>
    /// Steps <paramref name="keys"/> until the hero stops, asserting every step stands on the ground under its middle at the
    /// speed of the flat, never in the air: no hop, no fall, no stall, but the last step flush against a wall.
    /// </summary>
    private static Hero WalkOnTheGround(Ship ship, Hero hero, HeroInput keys)
    {
        int speed = keys.Sprint ? Spec.Run : Spec.Walk;
        var h = hero;
        bool slowed = false;
        for (int i = 0; i < 2000; i++)
        {
            var next = h.Step(keys, ship);
            Assert.True(next.Vy == 0, $"in the air at {next}");
            Assert.True(GroundUnder(ship, next.X) == next.Y, $"off the ground at {next}: it is at {GroundUnder(ship, next.X)}");
            if (next.X == h.X) return next;
            Assert.False(slowed, $"slowed down short of a wall before {next}");
            slowed = Math.Abs(next.Vx) != speed && !Gaining(h, next, keys.Sprint);
            h = next;
        }
        throw new InvalidOperationException("never stopped");
    }

    /// <summary>Whether a step from <paramref name="h"/> to <paramref name="next"/> gained the walk's or the run's whole step of speed toward its top (0089).</summary>
    private static bool Gaining(Hero h, Hero next, bool run) =>
        Math.Abs(next.Vx) == Math.Min(run ? Spec.Run : Spec.Walk, Math.Abs(h.Vx) + (run ? Spec.RunAccel : Spec.WalkAccel)) &&
        Math.Abs(next.Vx) > Math.Abs(h.Vx);

    /// <summary>
    /// Up a stair of slopes with air under its steps, along their one 45° line every step, onto what its top meets and on
    /// to the wall; and back down it to the far wall: with ◣ and ◢, walking and running, to a floor or a ledge (a player's
    /// stair stopped dead on its first step: the feet crossed into a step's column under its diagonal, in the air).
    /// </summary>
    [Theory]
    [InlineData(Stairs, false)]
    [InlineData(Stairs, true)]
    [InlineData(StairsToLedge, false)]
    [InlineData(StairsToLedge, true)]
    public void WalksUpAndDownAStairOfSlopesWithAirUnderItOnItsLineEveryStepEitherWay(string map, bool mirrored)
    {
        var ship = Flat(mirrored ? Mirrored(map) : map);
        var (up, down) = mirrored ? (Right, Left) : (Left, Right);
        int top = mirrored ? 16 * Unit - Half : 1 * Unit + Half, bottom = mirrored ? 1 * Unit + Half : 16 * Unit - Half;
        foreach (bool run in new[] { false, true })
        {
            var start = new Hero(bottom, 14 * Unit, 0, 0, false, up.X);
            var climbed = WalkOnTheGround(ship, start, up with { Sprint = run });
            Assert.Equal((top, 7 * Unit), (climbed.X, climbed.Y));
            var back = WalkOnTheGround(ship, climbed, down with { Sprint = run });
            Assert.Equal((bottom, 14 * Unit), (back.X, back.Y));
        }
    }

    /// <summary>Under a stair with air under it, its steps' closed sides still stop the hero like walls.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnderAStairOfSlopesTheirClosedSidesAreWalls(bool mirrored)
    {
        var ship = Flat(mirrored ? Mirrored(Stairs) : Stairs);
        // From under the floor at its top toward the stair, the head in row 8: ◣'s left side there stops it.
        var start = new Hero(mirrored ? 16 * Unit - Half : 1 * Unit + Half, 14 * Unit, 0, 0, false, 1);
        foreach (bool run in new[] { false, true })
        {
            var stopped = HeroTests.Settle(ship, start, (mirrored ? Left : Right) with { Sprint = run });
            Assert.Equal((mirrored ? 13 * Unit + Half : 4 * Unit - Half, 14 * Unit), (stopped.X, stopped.Y));
        }
    }

    // A ramp up to a hole two cells wide, the hero's body nearly two (0104), the ground going on beyond at the ramp's top.
    private const string HoleBySlope = """
        ###########
        #.........#
        #.........#
        #.........#
        #.........#
        #.........#
        #.........#
        #.........#
        #S../..####
        #####..####
        #####..####
        ###########
        """;

    [Fact]
    public void FallsIntoAHoleTwoCellsWideAtTheTopOfASlopeFromEitherSideAndTheSlopeWallsItIn()
    {
        var ship = Flat(HoleBySlope);
        foreach (var (from, keys) in new[] { (Hero.At(ship, 8 * ship.Width + 1), Right), (new Hero(9 * Unit, 8 * Unit, 0, 0, false, 1), Left) })
            foreach (bool run in new[] { false, true })
            {
                var walked = HeroTests.Until(ship, from, keys with { Sprint = run },
                    h => h.Y > 9 * Unit || (keys.X < 0 ? h.X <= 1 * Unit + Half : h.X >= 10 * Unit - Half));
                var landed = HeroTests.Settle(ship, walked, Still);
                Assert.Equal(11 * Unit, landed.Y);
                Assert.InRange(landed.X, 5 * Unit + Half, 7 * Unit - Half);
                // In the hole the slope's closed side walls it in like the wall across.
                Assert.Equal(5 * Unit + Half, HeroTests.Settle(ship, landed, Left).X);
                Assert.Equal(7 * Unit - Half, HeroTests.Settle(ship, landed, Right).X);
            }
    }

    // A ladder two cells wide at the foot of a ramp, up to a plateau.
    private const string LadderBySlope = """
        ###########
        #.........#
        #.........#
        #.........#
        #.........#
        #.........#
        #.........#
        #.HH......#
        #.HH......#
        #.HH../####
        #.HH./#####
        #SHH/######
        ###########
        """;

    [Fact]
    public void WalksDownASlopePastALadderAtItsFootAndClimbsTheLadder()
    {
        var ship = Flat(LadderBySlope);
        var h = new Hero(8 * Unit, 9 * Unit, 0, 0, false, -1);
        // Down the ramp and on past the ladder, never in the air.
        for (int i = 0; h.X > 2 * Unit; i++)
        {
            Assert.True(i < 300, $"stopped at {h}");
            h = h.Step(Left, ship);
            Assert.True(h.Vy == 0, $"in the air at {h}");
        }
        Assert.Equal(12 * Unit, h.Y);
        // Back to the ladder, up it to its top beside the plateau, and down again.
        h = HeroTests.Until(ship, h, Right, x => x.X >= 3 * Unit);
        var climbed = HeroTests.Settle(ship, h, Up);
        Assert.Equal((3 * Unit, 7 * Unit), (climbed.X, climbed.Y));
        Assert.Equal(12 * Unit, HeroTests.Settle(ship, climbed, Down).Y);
    }

    [Fact]
    public void SwimmingTreatsAShapedCellAsWhole()
    {
        // All space but a ◢: swum into from its open side, it stops the hero at its edge.
        var ship = Flat([.. Enumerable.Repeat("          ", 7), "      /   ", "          ", "          "]);
        var adrift = new Hero(2 * Unit + 500, 8 * Unit, 0, 0, false, 1);
        Assert.True(ship.Space(adrift.BodyCell(ship.Width)));
        var swum = HeroTests.Settle(ship, adrift, Right);
        Assert.Equal(6 * Unit - Half, swum.X);
    }

    // A room whose ceiling holds a ◤ and a ◥ two rows over the hero's head, five apart so the body is under one at a time, the
    // floor on row 9's top.
    private const string Hung = """
        ##############
        #............#
        #...r....7...#
        #............#
        #............#
        #............#
        #............#
        #............#
        #............#
        ##############
        """;

    /// <summary>The highest the feet reach, jumping from standing at <paramref name="x"/> on <see cref="Hung"/>'s floor.</summary>
    private static int JumpTop(Ship ship, int x)
    {
        var h = HeroTests.Settle(ship, new Hero(x, 9 * Unit, 0, 0, false, 1), Still);
        int top = h.Y;
        h = h.Step(Still with { Jump = true }, ship);
        for (int i = 0; i < 200; i++, h = h.Step(HeroTests.Held, ship)) top = Math.Min(top, h.Y);
        Assert.Equal(9 * Unit, h.Y);
        return top;
    }

    [Theory]
    // Under ◤'s open half, the box's top-left corner 425 into the cell: the diagonal is 575 down there.
    [InlineData(4 * Unit + 425 + Half)]
    // Under ◥'s, the top-right corner 574 in: the diagonal is 575 down there.
    [InlineData(9 * Unit + 574 + 1 - Half)]
    public void AJumpIntoACeilingSlopeFromItsOpenSideStopsWhereTheHeadMeetsTheDiagonal(int x)
    {
        var ship = Flat(Hung);
        Assert.Equal(2 * Unit + 575 + Spec.Height, JumpTop(ship, x));
        // Under neither, the ceiling above stops it: row 0's bottom.
        Assert.Equal(1 * Unit + Spec.Height, JumpTop(ship, 7 * Unit));
        // Nearer its closed side, ◤'s diagonal hangs lower: 975 down, 25 in.
        Assert.Equal(2 * Unit + 975 + Spec.Height, JumpTop(ship, 4 * Unit + 25 + Half));
    }

    // A corridor six cells high, its top corners bevelled: ◤ at its left end, ◥ at its right.
    private const string Corridor = """
        ##########
        #r......7#
        #........#
        #........#
        #........#
        #........#
        #........#
        ##########
        """;

    [Fact]
    public void InACorridorSixHighTheHeadGoesUnderASlopedCeilingTillItMeetsTheDiagonal()
    {
        var ship = Flat(Corridor);
        // The head is 750 into row 1: the diagonals there are 250 in from the corridor's ends, against a whole wall's 1000.
        var start = Hero.At(ship, 6 * ship.Width + 4);
        Assert.Equal(1 * Unit + 750, start.Y - Spec.Height);
        var left = HeroTests.Settle(ship, start, Left);
        Assert.Equal((1 * Unit + 250 + Half, 7 * Unit), (left.X, left.Y));
        var right = HeroTests.Settle(ship, start, Right);
        Assert.Equal((8 * Unit + 750 - Half, 7 * Unit), (right.X, right.Y));
        // Walking or running, stopped there it stands under the slope and walks back out.
        Assert.Equal(left.X, HeroTests.Settle(ship, start, Left with { Sprint = true }).X);
        Assert.True(HeroTests.Settle(ship, left, Right).X > left.X);
    }

    // A run of ◤ along a corridor's ceiling: a saw of diagonals over the head.
    private const string LowCeiling = """
        ###########
        #...rrr...#
        #.........#
        #.........#
        #.........#
        #.........#
        #.........#
        ###########
        """;

    [Fact]
    public void WalksUnderASlopedCeilingAsFarAsItsOpenHalfGivesTheHeadRoomFromEitherSide()
    {
        var ship = Flat(LowCeiling);
        // From the left a ◤'s closed side is a wall; from the right its open half takes the head 250 in.
        Assert.Equal(4 * Unit - Half, HeroTests.Settle(ship, Hero.At(ship, 6 * ship.Width + 1), Right).X);
        Assert.Equal(6 * Unit + 250 + Half, HeroTests.Settle(ship, Hero.At(ship, 6 * ship.Width + 8), Left).X);
    }

    private static readonly char[] Cells = [' ', '.', '.', '.', '#', '/', '/', '\\', '\\', 'r', '7', '=', 'H', 'D', 'd'];

    /// <summary>What no part of the hero is ever in: a wall's solid, whole or a slope's half, a closed door, past the edge.</summary>
    private static bool InSolid(Ship ship, int x, int y)
    {
        int cx = (int)Math.Floor(x / (double)Unit), cy = (int)Math.Floor(y / (double)Unit);
        if (cx < 0 || cy < 0 || cx >= ship.Width || cy >= ship.Height) return true;
        int cell = cy * ship.Width + cx, structure = ship.StructureAt(cell), shape = ship.ShapeAt(cell);
        if (structure == Structure.DoorClosed) return true;
        if (structure != Structure.Wall) return false;
        return Shape.Solid(shape, x - cx * Unit, y - cy * Unit);
    }

    /// <summary>
    /// On any ship of walls, slopes, floors, ladders and doors, whatever the keys: the middle of the feet is never in
    /// anything solid, and the box above its lowest half width never overlaps a wall, a slope's solid half, a closed door
    /// or the edge. Only that lowest band may: the uphill side of a slope, a ledge stepped onto (0069). Tall enough maps for
    /// a hero 5.25 cells tall to stand in some (0104).
    /// </summary>
    [Property(MaxTest = 400)]
    public Property NeverPutsTheFeetInAnythingSolidNorTheBoxAboveItsLowestHalfWidth()
    {
        var keys =
            from x in Gen.Elements(-1, 0, 1)
            from y in Gen.Elements(-1, 0, 0, 1)
            from jump in Gen.Elements(false, false, true)
            from run in Gen.Elements(false, true)
            from held in Gen.Elements(false, true)
            select new HeroInput(x, y, jump, Sprint: run, JumpHeld: held);
        var gen =
            from width in Gen.Choose(1, 12)
            from height in Gen.Choose(1, 16)
            from cells in Gen.Elements(Cells).ArrayOf(width * height)
            from start in Gen.Choose(0, width * height - 1)
            from inputs in (from k in keys from steps in Gen.Choose(1, 60) select (k, steps)).ListOf()
            select (width, height, cells, start, inputs: inputs.Take(30).ToList());
        return Prop.ForAll(gen.ToArbitrary(), t =>
        {
            var rows = Enumerable.Range(0, t.height).Select(y => new string(t.cells, y * t.width, t.width)).ToArray();
            var ship = Flat(rows);
            var h = Hero.At(ship, t.start);
            // A hero starts only where it stands clear: its cell and those its body reaches up into open.
            if (Overlaps(ship, h) || InSolid(ship, h.X, h.Y - 1)) return true.ToProperty();
            foreach (var (held, steps) in t.inputs)
                for (int i = 0; i < steps; i++)
                {
                    var was = h;
                    h = h.Step(i == 0 ? held : held with { Jump = false }, ship);
                    if (InSolid(ship, h.X, h.Y - 1)) return false.Label($"feet in at {h} from {was} by {held} in {string.Join("|", rows)}");
                    if (Overlaps(ship, h)) return false.Label($"box in at {h} from {was} by {held} in {string.Join("|", rows)}");
                }
            return true.ToProperty();
        });
    }

    /// <summary>Whether anything solid lies in the box above its lowest half width, read a thousandth apart along its edges and every 25 within.</summary>
    private static bool Overlaps(Ship ship, Hero h)
    {
        for (int y = h.Y - Spec.Height; y < h.Y - Half; y += 25)
            for (int x = h.X - Half; x < h.X + Half; x += 25)
                if (InSolid(ship, x, y)) return true;
        for (int x = h.X - Half; x < h.X + Half; x += 25)
            if (InSolid(ship, x, h.Y - Half - 1)) return true;
        return InSolid(ship, h.X + Half - 1, h.Y - Half - 1) || InSolid(ship, h.X - Half, h.Y - Spec.Height) || InSolid(ship, h.X + Half - 1, h.Y - Spec.Height);
    }
}
