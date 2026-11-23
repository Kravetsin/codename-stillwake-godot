using Stillwake.Game.Features.Creatures;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>
/// The tick, the sentry's look (its page's candidate 3): its rig in its pose, and its motion read
/// off what the sim says, its moments coming once each.
/// </summary>
public class TickTests
{
    private static readonly TickArt.Pose Standing = new(0, 0, 0, 0, TickMotion.Watching, 1, 0);

    [Fact]
    public void ItsHitBoxReachesTheTopOfItsBodyInInk()
    {
        const int PixelsPerCell = 8;
        var rig = new TickRig();
        rig.Place(Standing);
        var body = rig.Slots.First(s => s.Part == "body");
        foreach (var species in new[] { Stillwake.Content.Creatures.Sentry })
        {
            double top = (CreatureArt.InkTick.Parts["body"].PivotY - body.Y) * CreatureLooks.Ticks[species.Look].Size;
            // Within a pixel: its soft edge.
            Assert.InRange(top, 0, species.Body.Height * PixelsPerCell + 1);
        }
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void APounceStoppedShortStruckAHeroUnlessABlowCutIt(bool blown, bool struck)
    {
        var motion = new TickMotion(() => 0.5);
        var flying = Watching with { Clamped = false, Pouncing = true, FlightLeft = 5, Vx = 100 };
        motion.Step(flying, 0.05);
        var moments = motion.Step(flying with { Pouncing = false, FlightLeft = 0, Blown = blown }, 0.05);
        Assert.Equal(struck, moments.HasFlag(TickMotion.Moment.HeroHit));
    }

    [Fact]
    public void WindingUpAfloatItFacesTheHeroHoweverItDrifts()
    {
        var motion = new TickMotion(() => 0.5);
        motion.Step(Watching with { Clamped = false, WindingUp = true, Vx = 80, Hero = (-40, 0) }, 0.05);
        Assert.Equal(-1, motion.Face);
        Assert.InRange(motion.Angle, -0.1, 0.1);
    }

    [Fact]
    public void WalkingItsFeetStepAndStandingTheyDoNot()
    {
        var rig = new TickRig();
        var still = rig.Place(Standing).Feet.ToList();
        Assert.Equal(still, rig.Place(Standing with { Gait = 2 }).Feet);
        var a = rig.Place(Standing with { Walk = 1, Gait = 0 }).Feet.ToList();
        var b = rig.Place(Standing with { Walk = 1, Gait = 2 }).Feet.ToList();
        Assert.NotEqual(a, b);
    }

    private static TickMotion.Seen Watching => new(true, false, false, 0, false, 0, 0, 0, 0, (40, 0));

    private static TickMotion.Moment Run(TickMotion motion, TickMotion.Seen seen, double seconds)
    {
        var all = TickMotion.Moment.None;
        for (double t = 0; t < seconds; t += 1 / 60.0) all |= motion.Step(seen, 1 / 60.0);
        return all;
    }

    [Fact]
    public void ItsMomentsComeOnceEachAsTheSimMovesIt()
    {
        var motion = new TickMotion(() => 0.5);
        Assert.Equal(TickMotion.Moment.None, Run(motion, Watching, 0.5));
        Assert.Equal(TickMotion.Moment.Notice, Run(motion, Watching with { Alerted = true }, 0.5));
        Assert.Equal(TickMotion.Moment.WindUp, Run(motion, Watching with { WindingUp = true, WindUpS = 0.4 }, 0.5));
        var flight = Watching with { Clamped = false, Pouncing = true, FlightLeft = 9, Vx = 190, Vy = -40 };
        Assert.Equal(TickMotion.Moment.Unclamp, Run(motion, flight, 0.3));
        Assert.Equal(TickMotion.Moment.Brake | TickMotion.Moment.HeroHit, Run(motion, flight with { Pouncing = false, Vx = 0, Vy = 0 }, 0.2));
        Assert.Equal(TickMotion.Moment.Clamp, Run(motion, Watching, 0.5));
        Run(motion, flight with { FlightLeft = 1 }, 0.3);
        Assert.Equal(TickMotion.Moment.Brake, Run(motion, flight with { Pouncing = false, FlightLeft = 0 }, 0.2));
    }

    [Fact]
    public void ItsEyeIsTealWatchingYellowAlertedRedActing()
    {
        var motion = new TickMotion(() => 0.5);
        Run(motion, Watching, 1);
        Assert.Equal(TickMotion.Watching, motion.Pose.Eye, Near);
        Run(motion, Watching with { Alerted = true }, 1);
        Assert.Equal(TickMotion.Alerted, motion.Pose.Eye, Near);
        Run(motion, Watching with { WindingUp = true, WindUpS = 0.6 }, 1);
        Assert.Equal(TickMotion.Attacking, motion.Pose.Eye, Near);
        Assert.True(motion.Hot);
        Assert.InRange(motion.Pose.Crouch, 0.95, 1.05);
    }

    [Fact]
    public void FlyingItFacesAndNosesAlongItsFlightUnderItsBurner()
    {
        var motion = new TickMotion(() => 0.5);
        Run(motion, Watching, 0.2);
        Run(motion, Watching with { Clamped = false, Vx = -100, Vy = 100 }, 0.5);
        Assert.Equal(-1, motion.Face);
        // Its nose (its own +x, mirrored) along its flight, down and left.
        Assert.InRange(motion.Angle, -Math.PI / 4 - 0.05, -Math.PI / 4 + 0.05);
        Assert.InRange(motion.Thrust, 0.7, 0.8);
        Assert.InRange(motion.Pose.Fly, 0.95, 1);
        Assert.Equal(0, motion.Mags);
        Assert.True(motion.Plume.X > 0 && motion.Plume.Y < 0, "its plume does not trail behind it");
    }

    [Fact]
    public void ClampedItsLegsGripAndItsBurnerDies()
    {
        var motion = new TickMotion(() => 0.5);
        Run(motion, Watching with { Clamped = false, Vx = 100 }, 0.5);
        Run(motion, Watching, 0.6);
        Assert.InRange(motion.Pose.Fly, 0, 0.05);
        Assert.InRange(motion.Thrust, 0, 0.05);
        Assert.Equal(0, motion.Angle, 2);
        Assert.Equal(1, motion.Mags);
    }

    [Fact]
    public void CrawlingAlongItsWreckItsLegsWalkAndItFacesItsWay()
    {
        var motion = new TickMotion(() => 0.5);
        Run(motion, Watching with { Hero = null, Crawl = -5 }, 1);
        Assert.Equal(-1, motion.Face);
        Assert.True(motion.Pose.Walk > 0.9, $"walks {motion.Pose.Walk:F2}");
        double gait = motion.Pose.Gait;
        Run(motion, Watching with { Hero = null, Crawl = -5 }, 1);
        Assert.InRange(motion.Pose.Gait - gait, 7.9, 8.2);
        Run(motion, Watching with { Hero = null }, 1);
        Assert.True(motion.Pose.Walk < 0.01);
    }

    [Fact]
    public void StruckItFlashesWhiteAMomentAndKilledItsCracksSpreadTillItBursts()
    {
        var motion = new TickMotion(() => 0.5);
        Run(motion, Watching, 0.2);
        motion.Struck();
        Run(motion, Watching, 1 / 60.0);
        Assert.True(motion.Flash > 0.5);
        Run(motion, Watching, 0.2);
        Assert.Equal(0, motion.Flash);
        motion.Die();
        Run(motion, default, TickMotion.CracksS / 2);
        Assert.InRange(motion.Pose.Cracks, 0.4, 0.6);
        Run(motion, default, TickMotion.CracksS);
        Assert.Equal(1, motion.Pose.Cracks);
        Assert.True(motion.Dying >= TickMotion.CracksS);
    }

    [Fact]
    public void CuttingALineItFacesItRearsAndSawsAtItThenRecoilsAsItSnaps()
    {
        var motion = new TickMotion(() => 0.5);
        Run(motion, Watching, 0.5);
        Assert.Equal(1, motion.Face);
        var cutting = Watching with { Cutting = true, CutDone = 0.5, Line = (-60, -10) };
        Run(motion, cutting, 0.4);
        Assert.Equal(-1, motion.Face);
        Assert.Equal(0.5, motion.Cut);
        Assert.True(motion.Hot);
        Assert.InRange(motion.Pose.Crouch, -0.55, -0.45);
        Assert.True(motion.Pose.Saw > 0.6, $"saws {motion.Pose.Saw:F2}");
        Assert.True(motion.Angle > 0.1, "it does not rear its head up toward the line");
        Assert.Equal(TickMotion.Attacking, motion.Pose.Eye, Near);
        motion.Snipped();
        Run(motion, Watching, 1 / 60.0);
        Assert.Null(motion.Cut);
        Assert.True(motion.Shake > 1, $"no recoil: {motion.Shake:F2}");
        Run(motion, Watching, 0.6);
        Assert.InRange(motion.Pose.Saw, 0, 0.01);
        Assert.Equal(0, motion.Shake, 2);
    }

    [Fact]
    public void ItsClawRisesToTheLineItSaws()
    {
        var rig = new TickRig();
        var standing = rig.Place(Standing);
        var sawing = rig.Place(Standing with { Saw = 1, Jaw = 1 });
        Assert.True(sawing.Cut.Y < standing.Cut.Y, $"its claw at {sawing.Cut}, from {standing.Cut}");
    }

    private static readonly IEqualityComparer<TickArt.Rgb> Near = EqualityComparer<TickArt.Rgb>.Create(
        (a, b) => Math.Abs(a.R - b.R) < 2 && Math.Abs(a.G - b.G) < 2 && Math.Abs(a.B - b.B) < 2, _ => 0);

    /// <summary>A vessel's spider (derelict-enemies): folded asleep, its legs tucked and its burner dark; holding a wall it faces it nose up.</summary>
    [Fact]
    public void ASpiderFoldedTucksItsLegsAndOnAWallTurnsNoseUpFacingIt()
    {
        var motion = new TickMotion(() => 0.5);
        var spider = Watching with { Walker = true };
        Run(motion, spider with { Folded = 1 }, 0.5);
        Assert.True(motion.Pose.Fly > 0.95 && motion.Thrust < 0.01, $"fly {motion.Pose.Fly}, thrust {motion.Thrust}");
        Run(motion, spider with { Wall = -1, Crawl = 120 }, 0.5);
        Assert.Equal(-1, motion.Face);
        Assert.InRange(motion.Angle, Math.PI / 2 - 0.05, Math.PI / 2 + 0.05);
        Assert.True(motion.Pose.Walk > 0.9);
        // Falling it stays upright, its burner dark: a walker, no flyer.
        Run(motion, spider with { Clamped = false, Vy = 120 }, 0.5);
        Assert.True(Math.Abs(motion.Angle) < 0.05 && motion.Thrust < 0.01, $"angle {motion.Angle}, thrust {motion.Thrust}");
    }

    /// <summary>A capsule's waking in order: its iris opens, then the spider is seen folded, unfolds, and only then drops.</summary>
    [Fact]
    public void ASpiderWakesItsIrisOpeningThenItUnfoldsThenDrops()
    {
        Assert.Equal((0.0, 1.0), (DockWake.Progress(30, 30, 0), DockWake.Progress(0, 30, 0.5)));
        Assert.Equal((0.0, false, 1.0, 0.0), (DockWake.Iris(0), DockWake.Shown(0), DockWake.Folded(0), DockWake.Drop(0)));
        Assert.True(DockWake.Iris(DockWake.ShownAt) > 0.5 && DockWake.Folded(DockWake.ShownAt) == 1);
        Assert.True(DockWake.Folded(DockWake.DropFrom) == 0 && DockWake.Drop(DockWake.DropFrom) == 0);
        Assert.Equal((1.0, true, 0.0, 1.0), (DockWake.Iris(1), DockWake.Shown(1), DockWake.Folded(1), DockWake.Drop(1)));
        // Its iris shut, then every step's picture in turn, then open.
        var steps = Enumerable.Range(0, 101).Select(k => DockWake.IrisStep(k / 100.0)).Distinct().ToList();
        Assert.Equal(Enumerable.Range(0, DockWake.IrisSteps + 2), steps);
        Assert.Equal((0, DockWake.IrisSteps + 1), (DockWake.IrisStep(DockWake.IrisFrom), DockWake.IrisStep(DockWake.IrisTo)));
        // A stand: shut and hung asleep; its clamp up before its robot is lowered; lowered before its hooks open; open, risen and its robot up at the end.
        Assert.Equal((0.0, StandWake.Hang, 0.0, 1.0, 0.0), (StandWake.Clamp(0), StandWake.Hung(0), StandWake.Hooks(0), StandWake.Folded(0), StandWake.Lamp(0, 0)));
        Assert.True(StandWake.Clamp(StandWake.LowerFrom) == StandWake.ClampSwing && StandWake.Hung(StandWake.HooksFrom) == 0);
        Assert.Equal((StandWake.ClampSwing, 0.0, 1.0, StandWake.YokeRise, 0.0, 1.0), (StandWake.Clamp(1), StandWake.Hung(1), StandWake.Hooks(1), StandWake.Risen(1), StandWake.Folded(1), StandWake.Lamp(1, 2)));
    }

    [Fact]
    public void ADocksMomentIsHeardOnceAsItsWakingPassesItAndNeverWhenFirstSeenPastIt()
    {
        // Asleep, a moment at its start is heard on the first frame it wakes; the iris once, on the frame that passes it.
        Assert.True(DockWake.Passed(DockWake.Asleep, 0.02, 0));
        Assert.False(DockWake.Passed(0.02, 0.05, 0));
        Assert.Equal(1, new[] { (DockWake.Asleep, 0.02), (0.02, 0.07), (0.07, 0.12), (0.12, 0.3) }.Count(f => DockWake.Passed(f.Item1, f.Item2, DockWake.IrisFrom)));
        // First seen mid-wake (unknown before), nothing is heard for what passed out of sight.
        Assert.False(DockWake.Passed(double.NaN, 0.5, 0));
        Assert.False(DockWake.Passed(double.NaN, 0.5, StandWake.LampFrom));
    }

    [Fact]
    public void AGuardsFootIsHeardEachStepOfItsWayButNeverTwoTooCloseNorStandingNorAsItSetsOff()
    {
        var feet = new Footfalls(10, 0.1);
        Assert.False(feet.Step(25, walks: true, 0.02));
        Assert.False(feet.Step(29, walks: true, 0.05));
        Assert.True(feet.Step(31, walks: true, 0.05));
        // A step's way passed again too soon after the last: unheard, but counted.
        Assert.False(feet.Step(41, walks: true, 0.05));
        Assert.False(feet.Step(45, walks: true, 0.05));
        Assert.True(feet.Step(51, walks: true, 0.1));
        // Stopped, then off again: the first frame walking sets its count, unheard.
        Assert.False(feet.Step(61, walks: false, 0.2));
        Assert.False(feet.Step(75, walks: true, 0.2));
        Assert.True(feet.Step(81, walks: true, 0.2));
    }
}
