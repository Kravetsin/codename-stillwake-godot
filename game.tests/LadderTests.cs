using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Xunit;
using static Stillwake.Game.Features.Crew.Poses;
using Look = Stillwake.Game.HeroMotion;
using Spec = Stillwake.Content.Hero;
using Surfaces = Stillwake.Content.Surfaces;

namespace Stillwake.Game.Tests;

/// <summary>The whole ladder (0092): grabbed on the move, eased onto, climbed, slid, leapt off, pulled up out of, its hands on its rungs.</summary>
public class LadderTests
{
    private const int Unit = Hero.Unit, Block = HeroTests.Block;
    private static readonly HeroInput Still = HeroTests.Still, Up = Still with { Y = -1 }, Down = Still with { Y = 1 };

    /// <summary>A ladder up a shaft a character (two cells) wide in column 3 into the room above, whose floor is row 5's top; a hall below.</summary>
    private static readonly string[] Shaft =
    [
        "#######",
        "#.....#",
        "#.....#",
        "#.....#",
        "#.....#",
        "###H###",
        "#..H..#",
        "#..H..#",
        "#..H..#",
        "#..H..#",
        "#######",
    ];

    private static Ship ShipOf(string[] rows, Surfaces.Kind? rules = null)
    {
        var world = new TestWorld(Layout.Parse(rows).Doubled());
        return Ship.Of(world.M.Grid, world.M.Rooms, rules: rules);
    }

    private static readonly Ship Aboard = ShipOf(Shaft);
    private const int Deck = 10 * Block, Top = 5 * Block, Middle = 3 * Block + Block / 2;

    private static Hero At(int x, int y, bool climbing = false, int vx = 0, int vy = 0) => new(x, y, vx, vy, climbing, 1);

    private static Hero Run(Ship ship, Hero hero, HeroInput keys, int steps)
    {
        for (int i = 0; i < steps; i++) hero = hero.Step(keys, ship);
        return hero;
    }

    [Fact]
    public void ClimbsUpAtThirteenPointTwoCellsASecondDownAtEighteenAndSlidesWithShiftAtThirtySix()
    {
        var on = At(Middle, 8 * Block + 1000, climbing: true);
        Assert.Equal(on.Y - Spec.ClimbUp, on.Step(Up, Aboard).Y);
        Assert.Equal(on.Y + Spec.ClimbDown, on.Step(Down, Aboard).Y);
        var slid = on.Step(Down with { Sprint = true }, Aboard);
        Assert.Equal((on.Y + Spec.Slide, Spec.Slide, true), (slid.Y, slid.Vy, slid.Climbing));
        Assert.Equal((13.2, 18.0, 36.0), (Spec.ClimbUp * 60 / 1000.0, Spec.ClimbDown * 60 / 1000.0, Spec.Slide * 60 / 1000.0));
        // Slid to the floor, it stands on it at the ladder's foot.
        var down = HeroTests.Settle(Aboard, on, Down with { Sprint = true });
        Assert.Equal(Deck, down.Y);
    }

    [Fact]
    public void UpGrabsTheLadderFromARunAndClimbsOnWithTheSideStillHeld()
    {
        var running = At(Middle - 900, Deck, vx: Spec.Run);
        var keys = new HeroInput(1, -1, false, Sprint: true);
        var grabbed = running.Step(keys, Aboard);
        Assert.True(grabbed.Climbing);
        Assert.Equal((0, Deck - Spec.ClimbUp), (grabbed.Vx, grabbed.Y));
        var on = Run(Aboard, grabbed, keys, 10);
        Assert.True(on.Climbing);
        Assert.Equal((Middle, Deck - 11 * Spec.ClimbUp), (on.X, on.Y));
        // Up let go with the side still held at the foot: it walks off.
        var foot = At(Middle, Deck, climbing: true);
        Assert.False(foot.Step(new HeroInput(1, 0, false), Aboard).Climbing);
    }

    [Fact]
    public void UpGrabsTheLadderFromAJumpRising()
    {
        var jumped = At(Middle - 600, Deck).Step(new HeroInput(0, -1, true), Aboard);
        Assert.False(jumped.Climbing);
        Assert.True(jumped.Vy < 0);
        var caught = jumped.Step(Up, Aboard);
        Assert.True(caught.Climbing);
        Assert.Equal(jumped.Y - Spec.ClimbUp, caught.Y);
    }

    [Fact]
    public void EasesOntoTheLaddersMiddleOverFiveStepsNotAtOnce()
    {
        var h = At(Middle - 900, Deck);
        var xs = new List<int>();
        for (int i = 0; i < 8; i++)
        {
            h = h.Step(Up, Aboard);
            xs.Add(h.X);
        }
        Assert.Equal(Middle - 495, xs[0]);
        Assert.Equal(4, xs.FindIndex(x => x == Middle));
        for (int i = 1; i < xs.Count; i++) Assert.True(xs[i] >= xs[i - 1]);
        // From the far edge of its column it takes a step more.
        var edge = Run(Aboard, At(3 * Block, Deck), Up, 6);
        Assert.Equal(Middle, edge.X);
    }

    [Fact]
    public void LeapsOffToASideAcrossAndLessHighButStraightUpAsFromTheFloor()
    {
        var on = At(Middle, Deck - 1000, climbing: true);
        var leapt = on.Step(new HeroInput(1, 0, true), Aboard);
        Assert.False(leapt.Climbing);
        Assert.Equal(Spec.Run * Spec.LadderJumpAcross / 1000, leapt.Vx);
        Assert.Equal(-(Spec.Jump * Spec.LadderJumpRise / 1000) + Aboard.Gravity, leapt.Vy);
        Assert.True(leapt.Rising);
        var flown = Run(Aboard, leapt, new HeroInput(1, 0, false, JumpHeld: true), 12);
        Assert.True(flown.X > on.X + 3 * Block / 2, $"{flown.X}");
        var up = on.Step(new HeroInput(0, 0, true), Aboard);
        Assert.Equal((0, -Spec.Jump + Aboard.Gravity), (up.Vx, up.Vy));
    }

    [Fact]
    public void UpGrabsNoLadderForAMomentAfterAJumpOffItThenCatchesItAgain()
    {
        var on = At(Middle, 9 * Block, climbing: true);
        var h = on.Step(new HeroInput(0, -1, true), Aboard);
        int steps = 1;
        while (!h.Climbing && steps < 40)
        {
            h = h.Step(Up with { JumpHeld = true }, Aboard);
            steps++;
        }
        Assert.True(h.Climbing);
        // The jump's step and Hero.Regrab more go by before up takes it again.
        Assert.Equal(Spec.Regrab + 2, steps);
        Assert.True(h.Y < on.Y - 2 * Block, $"{h.Y}");
    }

    [Fact]
    public void PullsUpOutOfTheShaftOntoTheFloorAboveNeverFasterThanItClimbs()
    {
        var h = At(Middle, Deck, climbing: true);
        var ledges = new List<int>();
        while (h.Climbing)
        {
            var was = h;
            if (Hero.LedgeAbove(Aboard, h) is int ledge) ledges.Add(ledge);
            h = h.Step(Up, Aboard);
            Assert.InRange(was.Y - h.Y, 0, Spec.ClimbUp);
            Assert.Equal(h.Y, was.Y - Math.Min(Spec.ClimbUp, was.Y - Top));
        }
        Assert.Equal((Top, 0, 0, Middle), (h.Y, h.Vy, h.Vx, h.X));
        Assert.Equal(Top, HeroTests.Settle(Aboard, h, Up).Y);
        // The ledge read on the way, its top three rows over the feet and less each step: what the figure pulls up onto.
        Assert.True(ledges.Count > 20);
        Assert.True(ledges[0] <= 3 * Block + Unit && ledges[^1] < Spec.ClimbUp);
        for (int i = 1; i < ledges.Count; i++) Assert.Equal(ledges[i - 1] - Spec.ClimbUp, ledges[i]);
    }

    [Fact]
    public void ReadsNoLedgeUnderAClosedHatchNorOffALadder()
    {
        var hatched = ShipOf(Shaft.Select((r, y) => y == 4 ? "###D###" : r).ToArray());
        Assert.Null(Hero.LedgeAbove(hatched, At(Middle, 7 * Block + 1000, climbing: true)));
        var opened = ShipOf(Shaft.Select((r, y) => y == 4 ? "###d###" : r).ToArray());
        Assert.Equal(3000 + Block, Hero.LedgeAbove(opened, At(Middle, 6 * Block + 1000, climbing: true)));
        Assert.Null(Hero.LedgeAbove(Aboard, At(Middle, 6 * Block + 1000)));
    }

    [Fact]
    public void DownAtTheTopOfAShaftEasesOntoItsMiddleOnTheLadderAndThenClimbsDown()
    {
        var h = At(Middle - 600, Top);
        var seen = new List<Hero>();
        for (int i = 0; i < 12; i++)
        {
            h = h.Step(Down, Aboard);
            seen.Add(h);
        }
        Assert.All(seen, s => Assert.True(s.Climbing));
        Assert.True(h.Y > Top + Block && h.X == Middle, $"{h}");
    }

    [Fact]
    public void ClimbsAndLeapsOnAPlanetAsAboardItsJumpUnderItsOwnGravity()
    {
        var planet = ShipOf(Shaft, Surfaces.TestStrip);
        var on = At(Middle, 8 * Block, climbing: true);
        Assert.Equal(on.Y - Spec.ClimbUp, on.Step(Up, planet).Y);
        Assert.Equal(on.Y + Spec.Slide, on.Step(Down with { Sprint = true }, planet).Y);
        var leapt = on.Step(new HeroInput(-1, 0, true), planet);
        Assert.Equal(-(Spec.Jump * Spec.LadderJumpRise / 1000) + planet.Gravity, leapt.Vy);
        Assert.Equal(Top, HeroTests.Settle(planet, on, Up).Y);
    }

    /// <summary>Where a hand of <paramref name="limbs"/> is, in pixels down the world, for a hero's feet at <paramref name="y"/>.</summary>
    private static double HandAt(Bend arm, int y) => (double)y / Unit * Rig.Cell - Rig.Shoulder + Rig.EndOf(arm, Rig.Arm).Y;

    private static Limbs Climbing(int y, int vy = 0, double? ledge = null) =>
        LimbsOf(Pose.Climb, At(Middle, y, climbing: true, vy: vy), new Moment(0, 0, Ledge: ledge));

    [Fact]
    public void HandsReachOverEachOtherEveryFourCellsClimbedReadFromTheHeight()
    {
        // The cycle is Ladder.Stride cells climbed, whatever pose came before; a hand reaches over the other every half of it.
        int stride = (int)(Look.Ladder.Stride * Unit), half = stride / 2;
        Assert.Equal(2 * Block, half);
        foreach (int y in new[] { 9 * Unit, 8 * Unit + 370, 7 * Unit + 910 })
        {
            PoseTests.Close(Climbing(y).FrontArm.Upper, Climbing(y - stride).FrontArm.Upper, 6);
            var a = Climbing(y);
            var b = Climbing(y - half);
            PoseTests.Close(a.FrontArm.Upper, b.BackArm.Upper, 6);
            PoseTests.Close(a.BackLeg.Upper, b.FrontLeg.Upper, 6);
        }
        // Climbing up, a hand reaches over the other about three times a second: calm, not by the rungs.
        double reaches = Spec.ClimbUp * 60.0 / Unit / (Look.Ladder.Stride / 2);
        Assert.InRange(reaches, 3, 3.5);
        // Each hand on its own hold, not together.
        var l = Climbing(8 * Unit + 300);
        Assert.False(Math.Abs(HandAt(l.FrontArm, 8300) - HandAt(l.BackArm, 8300)) < 0.5);
    }

    [Fact]
    public void SlidesWithHandsHighOnTheRailsBySpeedAndThrowsSparksOnlySliding()
    {
        Assert.Equal(0, SlideOf(Spec.ClimbDown));
        Assert.Equal(1, SlideOf(Spec.Slide));
        var slid = Climbing(8 * Unit, Spec.Slide);
        PoseTests.Close(OnLadder.Slide.FrontArm.Upper, slid.FrontArm.Upper, 6);
        PoseTests.Close(OnLadder.Slide.BackLeg.Lower, slid.BackLeg.Lower, 6);
        int Sparks(int vy)
        {
            var motion = new Motion();
            int sparks = 0;
            for (int i = 0; i < 30; i++) sparks += motion.Step(At(Middle, 8 * Unit + i * vy, climbing: true, vy: vy), Pose.Climb, Hero.StepMs).Sparks;
            return sparks;
        }
        Assert.InRange(Sparks(Spec.Slide), (int)(30 * Look.Ladder.SparksPerStep) - 1, (int)(30 * Look.Ladder.SparksPerStep));
        Assert.Equal(0, Sparks(Spec.ClimbDown));
        Assert.Equal(0, Sparks(-Spec.ClimbUp));
    }

    [Fact]
    public void PullsUpWithTheHandsOnTheLedgeAndEndsAllButStandingOnIt()
    {
        double reachTop = Rig.Shoulder - Look.Ladder.HandTop;
        Assert.Equal(0, PullOf(reachTop + 1));
        Assert.Equal(1, PullOf(reachTop - Look.Ladder.PullBlend));
        // Hands on the ledge, where it is, as the body rises past it.
        foreach (double ledge in new[] { 28.0, 24.0, 20.0 })
        {
            int y = 8 * Unit;
            var l = Climbing(y, ledge: ledge);
            double ledgeAt = (double)y / Unit * Rig.Cell - ledge;
            PoseTests.Close(ledgeAt, HandAt(l.FrontArm, y), 1);
            PoseTests.Close(ledgeAt, HandAt(l.BackArm, y), 1);
            Assert.True(l.Tilt > 0);
        }
        // At the ledge: arms straight down, the feet under the body, upright, as the standing pose takes over.
        var done = Climbing(8 * Unit, ledge: 0);
        var stand = LimbsOf(Pose.Stand, At(Middle, 8 * Unit), new Moment(0, 0));
        Assert.Equal(0, done.Tilt);
        Assert.True(Math.Abs(done.FrontArm.Upper - stand.FrontArm.Upper) < 0.4);
        Assert.True(Rig.EndOf(done.FrontLeg, Rig.Leg).Y > Rig.Hip - 1 && Rig.EndOf(done.BackLeg, Rig.Leg).Y > Rig.Hip - 1);
    }

    [Fact]
    public void AFriendsClimbSlideAndHoldsAreReadFromWhatItsHeroSends()
    {
        var sliding = At(Middle, 8 * Unit + 340, climbing: true, vy: Spec.Slide);
        var heard = SentHero.Decode(new SentHero(new Seen(sliding), 100).Encode())!.Value.Seen.Hero;
        Assert.Equal(Climbing(sliding.Y, Spec.Slide), LimbsOf(Pose.Climb, heard, new Moment(0, 0)));
        Assert.Equal(PoseOf(sliding, false, false), PoseOf(heard, false, false));
        // Drawn between two sends of a climb, the hands are where its own peer draws them at that height: on the same rungs.
        var remote = new RemoteHeroes();
        var a = At(Middle, 8 * Unit, climbing: true, vy: -Spec.ClimbUp);
        var b = a with { Y = a.Y - 3 * Spec.ClimbUp };
        remote.Add(1, new SentHero(new Seen(a), 0), 0);
        remote.Add(1, new SentHero(new Seen(b), RemoteHeroes.SendMs), RemoteHeroes.SendMs);
        var shown = remote.At(RemoteHeroes.SendMs / 2 + RemoteHeroes.BehindMs)[0].Seen.Hero;
        Assert.True(shown.Climbing && shown.Y < a.Y && shown.Y > b.Y, $"{shown}");
        var own = a with { Y = shown.Y };
        Assert.Equal(Climbing(own.Y), LimbsOf(Pose.Climb, shown, new Moment(0, 0)));
        // And the ledge it pulls up onto, read from where it is.
        Assert.Equal(Hero.LedgeAbove(Aboard, own), Hero.LedgeAbove(Aboard, shown));
    }
}
