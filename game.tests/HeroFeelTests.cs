using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Xunit;
using Spec = Stillwake.Content.Hero;
using Surfaces = Stillwake.Content.Surfaces;

namespace Stillwake.Game.Tests;

/// <summary>
/// How the hero's jump and walk feel (0089): coyote time and the jump buffer, a jump as high as Space is held, a held
/// jump's eased top and quicker fall, and the walk's start and stop on the ground and in the air.
/// </summary>
public class HeroFeelTests
{
    private const int Unit = Hero.Unit, Block = HeroTests.Block;
    private static readonly HeroInput Still = HeroTests.Still, Right = HeroTests.Right, Left = HeroTests.Left, Held = HeroTests.Held;
    private static readonly HeroInput Jump = Still with { Jump = true };

    // A ledge three characters wide over a pit four deep; head room above it.
    private const string Ledge = """
        ############
        #..........#
        #..........#
        #..........#
        #S.........#
        ####.......#
        #..........#
        #..........#
        #..........#
        ############
        """;

    // A hall twelve characters tall, the floor on row 13's top.
    private const string Hall = """
        ##########
        #........#
        #........#
        #........#
        #........#
        #........#
        #........#
        #........#
        #........#
        #........#
        #........#
        #........#
        #S.......#
        ##########
        """;

    private static Hero Steps(Ship ship, Hero hero, HeroInput input, int steps)
    {
        for (int i = 0; i < steps; i++) hero = hero.Step(input, ship);
        return hero;
    }

    /// <summary>The hero walked off the ledge's edge: right after the step whose start still stood on it.</summary>
    private static Hero OffTheLedge(Ship ship)
    {
        var h = Hero.At(ship, HeroTests.BlockCell(ship, 1, 4));
        for (int i = 0; i < 200; i++)
        {
            h = h.Step(Right, ship);
            if (h.Vy > 0) return h;
        }
        throw new InvalidOperationException("never walked off");
    }

    [Fact]
    public void JumpsOffALedgeUpToSixStepsAfterWalkingOffItAndNotAfter()
    {
        var ship = HeroTests.ShipFrom(Ledge);
        var off = OffTheLedge(ship);
        // The walk that ends where the body drops off the edge keeps its speed into the fall.
        Assert.Equal(Spec.Walk, off.Vx);
        Assert.Equal(6, Spec.Coyote);
        for (int late = 1; late <= Spec.Coyote + 1; late++)
        {
            var jumped = Steps(ship, off, Right, late - 1).Step(Right with { Jump = true }, ship);
            if (late <= Spec.Coyote) Assert.True(jumped.Vy == -Spec.Jump + ship.Gravity, $"no coyote jump {late} steps late: {jumped}");
            else Assert.True(jumped.Vy > 0, $"jumped {late} steps late: {jumped}");
        }
    }

    [Fact]
    public void ACoyoteJumpIsSpentOnceAndNoSecondPressInTheAirJumpsAgain()
    {
        var ship = HeroTests.ShipFrom(Ledge);
        var jumped = OffTheLedge(ship).Step(Right with { Jump = true }, ship);
        Assert.True(jumped.Vy < 0);
        var again = jumped.Step(Right with { Jump = true, JumpHeld = true }, ship);
        Assert.Equal(jumped.Vy + ship.Gravity, again.Vy);
    }

    /// <summary>The steps a hero let fall from high in the hall takes to land, and the hero then.</summary>
    private static (int Steps, Hero Landed) Fallen(Ship ship, Hero from)
    {
        var h = from;
        for (int i = 1; i < 500; i++)
        {
            h = h.Step(Still, ship);
            if (h.Vy == 0) return (i, h);
        }
        throw new InvalidOperationException("never landed");
    }

    [Fact]
    public void ASpacePressedUpToSixStepsBeforeLandingJumpsOnLandingAndOneEarlierDoesNot()
    {
        var ship = HeroTests.ShipFrom(Hall);
        var high = new Hero(7000, 7 * Block, 0, 0, false, 1);
        var (landing, landed) = Fallen(ship, high);
        Assert.Equal(13 * Block, landed.Y);
        Assert.Equal(6, Spec.JumpBuffer);
        for (int early = 1; early <= Spec.JumpBuffer + 1; early++)
        {
            // Pressed `early` steps before the first step that starts on the floor, and let go at once.
            int press = landing + 1 - early;
            var h = high;
            for (int i = 1; i <= landing + 1; i++) h = h.Step(i == press ? Jump : Still, ship);
            if (early <= Spec.JumpBuffer) Assert.True(h.Vy < 0, $"no buffered jump pressed {early} steps early: {h}");
            else Assert.Equal((13 * Block, 0), (h.Y, h.Vy));
        }
        // Held on, a buffered jump rises as high as one from standing.
        var buffered = high;
        for (int i = 1; i <= landing + 1; i++) buffered = buffered.Step(i < landing ? Still : i == landing ? Jump : Held, ship);
        Assert.Equal(Rise(ship, landed, Held), Rise(ship, buffered, Held) + (13 * Block - buffered.Y));
    }

    /// <summary>How far the feet rise from <paramref name="hero"/> stepping <paramref name="after"/> till it comes down.</summary>
    private static int Rise(Ship ship, Hero hero, HeroInput after)
    {
        int from = hero.Y, top = hero.Y;
        var h = hero;
        for (int i = 0; i < 400 && (i == 0 || h.Vy < 0); i++)
        {
            h = h.Step(i == 0 && hero.Vy == 0 ? Jump : after, ship);
            top = Math.Min(top, h.Y);
        }
        return from - top;
    }

    [Fact]
    public void ATapJumpsUnderHalfAsHighAsAJumpHeldToItsTopWhichClearsSevenCellsNotEight()
    {
        var ship = HeroTests.ShipFrom(Hall);
        var standing = Hero.At(ship, HeroTests.BlockCell(ship, 1, 12));
        int held = Rise(ship, standing, Held), tap = Rise(ship, standing, Still);
        Assert.InRange(held, 3 * Block + 1, 4 * Block - 1);
        Assert.True(tap < held / 2, $"tap {tap}, held {held}");
        // Let go halfway up, it rises between the two.
        var h = standing.Step(Jump, ship);
        h = Steps(ship, h, Held, 6);
        int half = standing.Y - h.Y + Rise(ship, h, Still);
        Assert.True(half > tap && half < held, $"tap {tap}, half {half}, held {held}");
    }

    [Fact]
    public void AHeldJumpHangsAtItsTopAndComesDownFasterThanItWentUp()
    {
        var ship = HeroTests.ShipFrom(Hall);
        Assert.Equal((42, 25, 48), (ship.Gravity, ship.ApexGravity, ship.FallGravity));
        // Near the top, Space held, it gains the eased gravity a step.
        var h = Hero.At(ship, HeroTests.BlockCell(ship, 1, 12)).Step(Jump, ship);
        while (h.Vy < -Spec.Apex) h = h.Step(Held, ship);
        Assert.Equal(h.Vy + ship.ApexGravity, h.Step(Held, ship).Vy);
        var (up, down) = Airtime(ship, Hero.At(ship, HeroTests.BlockCell(ship, 1, 12)));
        Assert.Equal((22, 20), (up, down));
        // A fall off an edge, Space not held, gains the fall's gravity from its second step.
        var falling = new Hero(7000, 7 * Block, 0, 0, false, 1).Step(Still, ship);
        Assert.Equal(ship.Gravity, falling.Vy);
        Assert.Equal(ship.Gravity + ship.FallGravity, falling.Step(Still, ship).Vy);
    }

    /// <summary>The steps a jump held throughout rises from <paramref name="standing"/>, and the steps it comes down.</summary>
    private static (int Up, int Down) Airtime(Ship ship, Hero standing)
    {
        var h = standing.Step(Jump, ship);
        int up = 1, down = 0;
        for (; h.Vy < 0; up++) h = h.Step(Held, ship);
        for (; h.Y != standing.Y || h.Vy != 0; down++) h = h.Step(Held, ship);
        return (up, down);
    }

    [Fact]
    public void OnAPlanetAHeldJumpClearsElevenCellsInAboutASecondDownFasterThanUp()
    {
        var world = new TestWorld(Layout.Parse(Hall).Doubled());
        var ship = Ship.Of(world.M.Grid, world.M.Rooms, rules: Surfaces.TestStrip);
        Assert.Equal((29, 17, 43), (ship.Gravity, ship.ApexGravity, ship.FallGravity));
        var standing = Hero.At(ship, world.M.Crew.Spawn);
        // Eleven cells, not the twelve of twice the 16 px cell's six: the planet's gravity rounds to 29, not 28 (0104).
        Assert.InRange(Rise(ship, standing, Held), 11 * Unit + 1, 12 * Unit - 1);
        // 59 steps, about a second: 32 up, 27 down.
        Assert.Equal((32, 27), Airtime(ship, standing));
    }

    [Fact]
    public void AWalkGetsUpToSpeedInFourStepsARunInSevenAndBothStopShortly()
    {
        var ship = HeroTests.ShipFrom(Hall);
        var standing = Hero.At(ship, HeroTests.BlockCell(ship, 1, 12));
        var run = Right with { Sprint = true };
        Assert.Equal(Spec.WalkAccel, standing.Step(Right, ship).Vx);
        Assert.True(Steps(ship, standing, Right, 3).Vx < Spec.Walk);
        var walking = Steps(ship, standing, Right, 4);
        Assert.Equal(Spec.Walk, walking.Vx);
        Assert.True(Steps(ship, standing, run, 6).Vx < Spec.Run);
        var running = Steps(ship, standing, run, 7);
        Assert.Equal(Spec.Run, running.Vx);
        // Let go: a walk stops in four steps, a run in eight.
        Assert.True(Steps(ship, walking, Still, 3).Vx > 0);
        Assert.Equal(0, Steps(ship, walking, Still, 4).Vx);
        Assert.True(Steps(ship, running, Still, 7).Vx > 0);
        Assert.Equal(0, Steps(ship, running, Still, 8).Vx);
        // Turning, it stops first, then gains the other way.
        Assert.Equal(0, Steps(ship, walking, Left, 4).Vx);
        Assert.Equal(-Spec.Walk, Steps(ship, walking, Left, 8).Vx);
        // Shift let go running, it slows to a walk.
        Assert.Equal(Spec.Walk, Steps(ship, running, Right, 4).Vx);
    }

    [Fact]
    public void InTheAirTheKeysSteerAtTwoThirdsOfTheirPullOnTheGround()
    {
        var ship = HeroTests.ShipFrom(Hall);
        int air = Spec.WalkAccel * Spec.AirControl / 1000;
        Assert.Equal(39, air);
        var aloft = new Hero(7000, 5 * Block, 0, 0, false, 1);
        Assert.Equal(air, aloft.Step(Right, ship).Vx);
        Assert.True(Steps(ship, aloft, Right, 6).Vx < Spec.Walk);
        Assert.Equal(Spec.Walk, Steps(ship, aloft, Right, 7).Vx);
        // A jump keeps the speed it was walked at, and let go in the air it slows by the same share.
        var standing = Hero.At(ship, HeroTests.BlockCell(ship, 1, 12));
        var jumped = Steps(ship, standing, Right, 4).Step(Right with { Jump = true }, ship);
        Assert.Equal(Spec.Walk, jumped.Vx);
        Assert.Equal(Spec.Walk - Spec.Stop * Spec.AirControl / 1000, jumped.Step(Held, ship).Vx);
    }
}
