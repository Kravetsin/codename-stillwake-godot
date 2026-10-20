using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Game.Features.Crew;
using Stillwake.Sim.Grid;
using Xunit;
using Spec = Stillwake.Content.Hero;

namespace Stillwake.Game.Tests;

/// <summary>The hero on slopes (0069): up and down without a jump or a hop, their closed sides walls, holes and ladders beside them.</summary>
public class SlopeTests
{
    private const int Unit = Hero.Unit;
    private const int Half = Spec.Width / 2;

    private static readonly HeroInput Still = HeroTests.Still, Left = HeroTests.Left, Right = HeroTests.Right;
    private static readonly HeroInput Down = Still with { Y = 1 }, Up = Still with { Y = -1 };

    // A hill: three cells up, a plateau two wide, three down; flat ground either side on row 9's top.
    private const string Hill = """
        ################
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
            5 => 8 * Unit + 999 - fx,
            6 => 7 * Unit + 999 - fx,
            7 => 6 * Unit + 999 - fx,
            8 or 9 => 6 * Unit,
            10 => 6 * Unit + fx,
            11 => 7 * Unit + fx,
            12 => 8 * Unit + fx,
            _ => 9 * Unit,
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
        var ship = HeroTests.ShipFrom(Hill);
        foreach (bool run in new[] { false, true })
        {
            var start = Hero.At(ship, 8 * ship.Width + 1);
            var over = WalkTheHill(ship, start, Right with { Sprint = run });
            Assert.Equal((15 * Unit - Half, 9 * Unit), (over.X, over.Y));
            var back = WalkTheHill(ship, over, Left with { Sprint = run });
            Assert.Equal((1 * Unit + Half, 9 * Unit), (back.X, back.Y));
        }
    }

    /// <summary>
    /// Up and down a 45° slope the hero keeps a steady speed, its share of the flat's (Uphill, Downhill), and the flat's
    /// on the flat, so its pose never flickers between walking and running (playtest of 0069: running uphill lost speed at
    /// every column and froze the legs).
    /// </summary>
    [Fact]
    public void KeepsASteadySpeedAndPoseEveryStepUpAndDownTheHillEitherWay()
    {
        var ship = HeroTests.ShipFrom(Hill);
        foreach (bool run in new[] { false, true })
            foreach (var (from, keys, wall) in new[] { (1 * Unit + Half, Right, 15 * Unit - Half), (15 * Unit - Half, Left, 1 * Unit + Half) })
            {
                var want = run ? Pose.Run : Pose.Walk;
                int speed = run ? Spec.Run : Spec.Walk;
                var h = new Hero(from, 9 * Unit, 0, 0, false, keys.X);
                var seen = new List<string>();
                for (int i = 0; i < 400; i++)
                {
                    var next = h.Step(keys with { Sprint = run }, ship);
                    // The last step, flush against the room's far wall, is short.
                    if (next.X == wall) break;
                    var pose = Poses.PoseOf(next, false, false);
                    // On the flat the flat's speed, on the diagonal its share; a step that reaches a corner, between them.
                    int rise = next.Y - h.Y, across = Math.Abs(next.Vx);
                    int slowed = speed * (rise < 0 ? Spec.Uphill : Spec.Downhill) / 1000;
                    bool steady = rise == 0 ? across == speed : across >= slowed && across <= speed;
                    if (pose != want || !steady) seen.Add($"{pose} at x {next.X} vx {next.Vx} rise {rise}");
                    h = next;
                }
                Assert.Empty(seen);
                Assert.True(h.Step(keys with { Sprint = run }, ship).X == wall, $"never reached the far wall, stopped at {h}");
            }
    }

    [Fact]
    public void WalksAndRunsUpA45DegreeSlopeSlowerThanDownItAndDownItSlowerThanOnTheFlat()
    {
        var ship = HeroTests.ShipFrom(Hill);
        var onRamp = new Hero(6500, 7 * Unit + 499, 0, 0, false, 1);
        foreach (bool run in new[] { false, true })
        {
            int speed = run ? Spec.Run : Spec.Walk;
            Assert.Equal(speed * Spec.Uphill / 1000, onRamp.Step(Right with { Sprint = run }, ship).Vx);
            Assert.Equal(-speed * Spec.Downhill / 1000, onRamp.Step(Left with { Sprint = run }, ship).Vx);
            Assert.Equal(speed, Hero.At(ship, 8 * ship.Width + 2).Step(Right with { Sprint = run }, ship).Vx);
        }
    }

    [Fact]
    public void StandsOnTheHigherFootingButFollowsASlopeOffAPlateauRatherThanHangOnItsCorner()
    {
        var ship = HeroTests.ShipFrom(Hill);
        // The middle on the foot of the ramp, the box's back over the flat: the slope is higher.
        Assert.Equal(8 * Unit + 899, HeroTests.Settle(ship, new Hero(5100, 8 * Unit, 0, 0, false, 1), Still).Y);
        // The middle on the flat, the box's front over the ramp's foot a thousandth higher: the flat holds, the box over the slope.
        Assert.Equal(9 * Unit, HeroTests.Settle(ship, new Hero(4900, 8 * Unit, 0, 0, false, 1), Still).Y);
        // Off the plateau's edge, the box's back still over it: the feet on the slope, not on the corner.
        Assert.Equal(6 * Unit + 100, HeroTests.Settle(ship, new Hero(10_100, 5 * Unit, 0, 0, false, 1), Still).Y);
    }

    [Fact]
    public void DownOnASlopeDoesNothing()
    {
        var ship = HeroTests.ShipFrom(Hill);
        var on = HeroTests.Settle(ship, new Hero(6500, 7 * Unit, 0, 0, false, 1), Still);
        Assert.Equal(7 * Unit + 499, on.Y);
        Assert.Equal(on with { Vx = 0, Vy = 0 }, HeroTests.Settle(ship, on, Down) with { Vx = 0, Vy = 0 });
    }

    [Fact]
    public void JumpsOnASlopeAndLandsBackOnIt()
    {
        var ship = HeroTests.ShipFrom(Hill);
        var on = HeroTests.Settle(ship, new Hero(6500, 7 * Unit, 0, 0, false, 1), Still);
        var up = on.Step(Still with { Jump = true }, ship);
        Assert.True(up.Y < on.Y);
        var landed = HeroTests.Settle(ship, up, Right);
        Assert.Equal(HillGround(landed.X), landed.Y);
    }

    [Theory]
    // A slope's closed side is a wall: ◢ from the right, ◣ from the left.
    [InlineData("#./...S.#", -1, 3 * Unit + Half)]
    [InlineData("#.S...\\.#", 1, 6 * Unit - Half)]
    // A ceiling slope on the floor, the head far above it, is a wall from either side.
    [InlineData("#.r...S.#", -1, 3 * Unit + Half)]
    [InlineData("#.S...7.#", 1, 6 * Unit - Half)]
    public void ASlopesClosedSideAndACeilingSlopeStopTheHeroLikeAWall(string floorRow, int dir, int stopsAt)
    {
        var ship = HeroTests.ShipFrom(string.Join('\n', "#########", "#.......#", "#.......#", "#.......#", floorRow, "#########"));
        var start = Hero.At(ship, 4 * ship.Width + floorRow.IndexOf('S'));
        var stopped = HeroTests.Settle(ship, start, dir < 0 ? Left : Right);
        Assert.Equal((stopsAt, 5 * Unit), (stopped.X, stopped.Y));
    }

    [Fact]
    public void FromItsOpenSideClimbsASlopeAndStepsOffItsTop()
    {
        var ship = HeroTests.ShipFrom(string.Join('\n', "#########", "#.......#", "#.......#", "#.......#", "#S../...#", "#########"));
        var h = Hero.At(ship, 4 * ship.Width + 1);
        h = HeroTests.Until(ship, h, Right, x => x.X >= 4600);
        Assert.Equal(4 * Unit + 999 - (h.X - 4 * Unit), h.Y);
        // Off the top's closed side it falls to the floor.
        Assert.Equal(5 * Unit, HeroTests.Settle(ship, h, Right).Y);
    }

    // A ramp up to a hole one cell wide, the ground going on beyond at the ramp's top.
    private const string HoleBySlope = """
        ##########
        #........#
        #........#
        #........#
        #........#
        #S../.####
        #####.####
        #####.####
        ##########
        """;

    [Fact]
    public void FallsIntoAHoleOneCellWideAtTheTopOfASlopeFromEitherSideAndTheSlopeWallsItIn()
    {
        var ship = HeroTests.ShipFrom(HoleBySlope);
        foreach (var (from, keys) in new[] { (Hero.At(ship, 5 * ship.Width + 1), Right), (new Hero(8 * Unit + 400, 5 * Unit, 0, 0, false, 1), Left) })
            foreach (bool run in new[] { false, true })
            {
                var walked = HeroTests.Until(ship, from, keys with { Sprint = run }, h => h.Y > 6 * Unit || (keys.X < 0 ? h.X < 1600 : h.X > 8400));
                var landed = HeroTests.Settle(ship, walked, Still);
                Assert.Equal((5, 8 * Unit), (landed.X / Unit, landed.Y));
                // In the hole the slope's closed side walls it in like the wall across.
                Assert.Equal(5 * Unit + Half, HeroTests.Settle(ship, landed, Left).X);
                Assert.Equal(6 * Unit - Half, HeroTests.Settle(ship, landed, Right).X);
            }
    }

    // A ladder at the foot of a ramp, up to a plateau.
    private const string LadderBySlope = """
        ##########
        #........#
        #........#
        #........#
        #.H......#
        #.H......#
        #.H../####
        #.H./#####
        #SH/######
        ##########
        """;

    [Fact]
    public void WalksDownASlopePastALadderAtItsFootAndClimbsTheLadder()
    {
        var ship = HeroTests.ShipFrom(LadderBySlope);
        var top = new Hero(7 * Unit, 6 * Unit, 0, 0, false, -1);
        var h = top;
        // Down the ramp and on past the ladder, never in the air.
        while (h.X > 2 * Unit)
        {
            h = h.Step(Left, ship);
            Assert.True(h.Vy == 0, $"in the air at {h}");
        }
        Assert.Equal(9 * Unit, h.Y);
        // Back to the ladder, up it to its top beside the plateau, and down again.
        h = HeroTests.Until(ship, h, Right, x => x.X >= 2 * Unit + 500);
        var climbed = HeroTests.Settle(ship, h, Up);
        Assert.Equal((2 * Unit + 500, 4 * Unit), (climbed.X, climbed.Y));
        Assert.Equal(9 * Unit, HeroTests.Settle(ship, climbed, Down).Y);
    }

    [Fact]
    public void SwimmingTreatsAShapedCellAsWhole()
    {
        // All space but a ◢: swum into from its open side, it stops the hero at its edge.
        var ship = HeroTests.ShipFrom(["          ", "          ", "          ", "      /   ", "          ", "          "]);
        var adrift = new Hero(2 * Unit + 500, 4 * Unit, 0, 0, false, 1);
        Assert.True(ship.Space(adrift.BodyCell(ship.Width)));
        var swum = HeroTests.Settle(ship, adrift, Right);
        Assert.Equal(6 * Unit - Half, swum.X);
    }

    // A room whose ceiling holds a ◤ and a ◥ two rows over the hero's head, the floor on row 6's top.
    private const string Hung = """
        #########
        #.......#
        #..r.7..#
        #.......#
        #.......#
        #.......#
        #########
        """;

    /// <summary>The highest the feet reach, jumping from standing at <paramref name="x"/> on <see cref="Hung"/>'s floor.</summary>
    private static int JumpTop(Ship ship, int x)
    {
        var h = HeroTests.Settle(ship, new Hero(x, 6 * Unit, 0, 0, false, 1), Still);
        int top = h.Y;
        h = h.Step(Still with { Jump = true }, ship);
        for (int i = 0; i < 200; i++, h = h.Step(Still, ship)) top = Math.Min(top, h.Y);
        Assert.Equal(6 * Unit, h.Y);
        return top;
    }

    [Theory]
    // Under ◤'s open half, the box's top-left corner 425 into the cell: the diagonal is 575 down there.
    [InlineData(3 * Unit + 900)]
    // Under ◥'s, the top-right corner 574 in: the diagonal is 575 down there.
    [InlineData(5 * Unit + 100)]
    public void AJumpIntoACeilingSlopeFromItsOpenSideStopsWhereTheHeadMeetsTheDiagonal(int x)
    {
        var ship = HeroTests.ShipFrom(Hung);
        Assert.Equal(2 * Unit + 575 + Spec.Height, JumpTop(ship, x));
        // Under neither, the ceiling above stops it: row 0's bottom.
        Assert.Equal(1 * Unit + Spec.Height, JumpTop(ship, 7 * Unit + 500));
        // Nearer its closed side, ◤'s diagonal hangs lower: 975 down, 25 in.
        Assert.Equal(2 * Unit + 975 + Spec.Height, JumpTop(ship, 3 * Unit + 500));
    }

    // A corridor three cells high, its top corners bevelled: ◤ at its left end, ◥ at its right.
    private const string Corridor = """
        ##########
        #r......7#
        #........#
        #........#
        ##########
        """;

    [Fact]
    public void InACorridorThreeHighTheHeadGoesUnderASlopedCeilingTillItMeetsTheDiagonal()
    {
        var ship = HeroTests.ShipFrom(Corridor);
        // The head is 375 into row 1: the diagonals there are 625 in from the corridor's ends, against a whole wall's 1000.
        var start = Hero.At(ship, 3 * ship.Width + 4);
        Assert.Equal(1 * Unit + 375, start.Y - Spec.Height);
        var left = HeroTests.Settle(ship, start, Left);
        Assert.Equal((1 * Unit + 625 + Half, 4 * Unit), (left.X, left.Y));
        var right = HeroTests.Settle(ship, start, Right);
        Assert.Equal((8 * Unit + 375 - Half, 4 * Unit), (right.X, right.Y));
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
        ###########
        """;

    [Fact]
    public void WalksUnderASlopedCeilingAsFarAsItsOpenHalfGivesTheHeadRoomFromEitherSide()
    {
        var ship = HeroTests.ShipFrom(LowCeiling);
        // From the left a ◤'s closed side is a wall; from the right its open half takes the head 625 in.
        Assert.Equal(4 * Unit - Half, HeroTests.Settle(ship, Hero.At(ship, 3 * ship.Width + 1), Right).X);
        Assert.Equal(6 * Unit + 625 + Half, HeroTests.Settle(ship, Hero.At(ship, 3 * ship.Width + 9), Left).X);
    }

    private static readonly char[] Cells = [' ', '.', '.', '#', '/', '/', '\\', '\\', 'r', '7', '=', 'H', 'D', 'd'];

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
    /// or the edge. Only that lowest band may: the uphill side of a slope, a ledge stepped onto (0069).
    /// </summary>
    [Property(MaxTest = 400)]
    public Property NeverPutsTheFeetInAnythingSolidNorTheBoxAboveItsLowestHalfWidth()
    {
        var keys =
            from x in Gen.Elements(-1, 0, 1)
            from y in Gen.Elements(-1, 0, 0, 1)
            from jump in Gen.Elements(false, false, true)
            from run in Gen.Elements(false, true)
            select new HeroInput(x, y, jump, Sprint: run);
        var gen =
            from width in Gen.Choose(1, 10)
            from height in Gen.Choose(1, 12)
            from cells in Gen.Elements(Cells).ArrayOf(width * height)
            from start in Gen.Choose(0, width * height - 1)
            from inputs in (from k in keys from steps in Gen.Choose(1, 60) select (k, steps)).ListOf()
            select (width, height, cells, start, inputs: inputs.Take(30).ToList());
        return Prop.ForAll(gen.ToArbitrary(), t =>
        {
            var rows = Enumerable.Range(0, t.height).Select(y => new string(t.cells, y * t.width, t.width)).ToArray();
            var ship = HeroTests.ShipFrom(rows);
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
