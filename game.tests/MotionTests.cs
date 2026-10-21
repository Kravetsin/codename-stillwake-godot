using Stillwake.Game.Features.Crew;
using Xunit;
using static Stillwake.Game.Features.Crew.Poses;
using Look = Stillwake.Game.HeroMotion;
using Spec = Stillwake.Content.Hero;

namespace Stillwake.Game.Tests;

/// <summary>The figure's lean, skid, landing and air poses read from the motion (0090), the hero moved by its own controller.</summary>
public class MotionTests
{
    private const int Unit = Hero.Unit;

    /// <summary>A hall forty cells wide and twenty-four tall, its floor on row 23's top.</summary>
    private static readonly Ship Hall = HeroTests.ShipFrom(
        Enumerable.Range(0, 24).Select(r => r is 0 or 23 ? new string('#', 40) : "#" + new string('.', 38) + "#").ToArray());

    private const int Floor = 23 * Unit;

    private static readonly HeroInput Still = HeroTests.Still, Right = HeroTests.Right, Left = HeroTests.Left;
    private static readonly HeroInput RunRight = Right with { Sprint = true }, RunLeft = Left with { Sprint = true };

    private sealed record Frame(Hero Hero, Pose Pose, Momentum Now, Motion.Cues Cues, double Landing);

    /// <summary>
    /// Steps the controller through <paramref name="keys"/> from <paramref name="hero"/>, a frame a step, as this machine draws
    /// its own hero; with <paramref name="sendEvery"/> steps, as a friend's hero is drawn: the frames go on at 60 a second, the
    /// hero as last sent.
    /// </summary>
    private static List<Frame> Drive(Hero hero, IEnumerable<HeroInput> keys, int sendEvery = 1, Motion? motion = null)
    {
        motion ??= new Motion();
        var frames = new List<Frame>();
        var seen = hero;
        int step = 0;
        foreach (var key in keys)
        {
            hero = hero.Step(key, Hall);
            if (++step % sendEvery == 0) seen = hero;
            var pose = PoseOf(seen, false, false);
            var cues = motion.Step(seen, pose, Hero.StepMs);
            frames.Add(new Frame(seen, pose, motion.Now, cues, motion.Landing));
        }
        return frames;
    }

    private static IEnumerable<HeroInput> Hold(HeroInput keys, int steps) => Enumerable.Repeat(keys, steps);

    private static readonly Hero Standing = new(5 * Unit, Floor, 0, 0, false, 1);

    [Fact]
    public void LeansFromAccelerationAtMostAQuarterRadianForwardAndAFifthBackOnly()
    {
        Assert.Equal(Look.Lean.Forward, Motion.LeanOf(1000, 1));
        Assert.Equal(-Look.Lean.Back, Motion.LeanOf(-1000, 1));
        // Facing left, gaining speed leftward leans it forward.
        Assert.Equal(Look.Lean.Forward, Motion.LeanOf(-1000, -1));
        PoseTests.Close(Spec.RunAccel * Look.Lean.Gain, Motion.LeanOf(Spec.RunAccel, 1), 6);
    }

    [Fact]
    public void LeansIntoARunStartedAndUprightOnceAtSpeedAndBackAsItStops()
    {
        var frames = Drive(Standing, [.. Hold(RunRight, 60), .. Hold(Still, 40)]);
        var starting = frames.Take(10).Max(f => f.Now.Lean);
        Assert.InRange(starting, 0.12, Look.Lean.Forward);
        // At full speed nothing is gained: upright again, but for the run's own lean in its pose.
        Assert.True(Math.Abs(frames[59].Now.Lean) < 0.005, $"{frames[59].Now.Lean}");
        var stopping = frames.Skip(60).Min(f => f.Now.Lean);
        Assert.InRange(stopping, -Look.Lean.Back, -0.1);
        Assert.True(Math.Abs(frames[^1].Now.Lean) < 0.005);
        // Nothing gained or lost: none at all.
        Assert.All(Drive(Standing, Hold(Still, 20)), f => Assert.Equal(0, f.Now.Lean));
    }

    [Fact]
    public void AFriendsHeroSeenASendAtATimeLeansAboutAsFarAsItsOwnPeerDrawsIt()
    {
        var keys = Hold(RunRight, 60).Concat(Hold(Still, 40)).ToList();
        var own = Drive(Standing, keys);
        var sent = Drive(Standing, keys, sendEvery: 3);
        double ownMost = own.Max(f => f.Now.Lean), sentMost = sent.Max(f => f.Now.Lean);
        Assert.InRange(sentMost, ownMost * 0.8, Math.Min(Look.Lean.Forward, ownMost * 1.5));
        Assert.InRange(sent.Min(f => f.Now.Lean), -Look.Lean.Back, own.Min(f => f.Now.Lean) * 0.8);
    }

    [Fact]
    public void BrakingAndTurningOnlyLeanTheBodyTheLegsKeepTheirStride()
    {
        // Tapping left and right once made a skid's legs dance on the spot: a stop or a turn now only leans.
        var hero = new Hero(5 * Unit, 9 * Unit, 0, 0, false, 1);
        foreach (var keys in new[] { Hold(RunRight with { Face = 1 }, 40).Concat(Hold(RunLeft with { Face = 1 }, 30)), Hold(RunRight, 40).Concat(Hold(Still, 30)) })
            foreach (var f in Drive(Standing, [.. keys]))
            {
                var still = LimbsOf(Pose.Run, hero, new Moment(0, 0, Momentum: Momentum.AtRest));
                var moved = LimbsOf(Pose.Run, hero, new Moment(0, 0, Momentum: f.Now));
                Assert.Equal(still with { Tilt = moved.Tilt }, moved);
            }
    }

    [Fact]
    public void AWalkKeptOnAHillNeverLeans()
    {
        var ship = HeroTests.ShipFrom(SlopeTests.Hill);
        foreach (bool run in new[] { false, true })
        {
            var motion = new Motion();
            var h = new Hero(1 * Unit + 475, 9 * Unit, 0, 0, false, 1);
            for (int i = 0; i < 300; i++)
            {
                var next = h.Step(Right with { Sprint = run }, ship);
                // Short of the room's far wall, which stops it dead and leans it back.
                if (next.X > 14 * Unit) break;
                h = next;
                motion.Step(h, PoseOf(h, false, false), Hero.StepMs);
                // Past setting off, at its speed up the slope, over the top and down: as on the flat.
                if (i < 30) continue;
                Assert.True(Math.Abs(motion.Now.Lean) < 0.01, $"leans {motion.Now.Lean} at {h}");
            }
        }
    }

    [Fact]
    public void ALandingSinksByTheFallsSpeedAndTakesLongerFromHigher()
    {
        Assert.Equal(Look.Landing.Least, Motion.DepthOf(100, 0));
        Assert.Equal(1, Motion.DepthOf(Look.Landing.PerDepth, 0));
        Assert.Equal(Look.Landing.Most, Motion.DepthOf(Spec.MaxFall, 0));
        // Landing at a run it sinks less.
        Assert.Equal(Look.Landing.Running, Motion.DepthOf(Look.Landing.PerDepth, Spec.Run));
        Assert.Equal(1, Motion.DepthOf(Look.Landing.PerDepth, Look.Landing.RunningSpeed));
        Assert.Equal(150, Motion.LandingMs(0));
        Assert.Equal(205, Motion.LandingMs(Look.Landing.LongFall / 2));
        Assert.Equal(260, Motion.LandingMs(Look.Landing.LongFall));
        Assert.Equal(260, Motion.LandingMs(Spec.MaxFall));
        Assert.True(Motion.Heavy(Look.Landing.Heavy));
        Assert.False(Motion.Heavy(Look.Landing.Heavy - 1));
    }

    /// <summary>The fall a hero lands from, and its landing's frames from touching down, jumping or dropped from <paramref name="cells"/> up.</summary>
    private static (int Fall, double Depth, List<Frame> After) Landed(IEnumerable<HeroInput> keys, int cells = 0)
    {
        var frames = Drive(Standing with { Y = Floor - cells * Unit }, keys);
        int at = frames.FindIndex(f => f.Cues.Landed != null);
        Assert.True(at >= 0, "never landed");
        Assert.Single(frames, f => f.Cues.Landed != null);
        return (frames[at].Cues.Landed!.Value, frames[at].Now.Depth, frames.Skip(at).ToList());
    }

    [Fact]
    public void AHopLandsLightAHeldJumpFullyAndAFallOfSevenCellsHeavy()
    {
        var hop = Landed([Still with { Jump = true }, .. Hold(Still, 80)]);
        var held = Landed([Still with { Jump = true }, .. Hold(HeroTests.Held, 80)]);
        var fall = Landed(Hold(Still, 80), cells: 7);
        Assert.True(hop.Depth < 0.7 && hop.Depth >= Look.Landing.Least, $"{hop}");
        Assert.InRange(held.Depth, 0.9, 1.15);
        Assert.True(fall.Depth > 1.3, $"{fall.Depth}");
        Assert.False(Motion.Heavy(hop.Fall) || Motion.Heavy(held.Fall));
        Assert.True(Motion.Heavy(fall.Fall));
        // Over its own length, then done: longer the harder it lands.
        int Lasts(List<Frame> after) => after.TakeWhile(f => f.Landing > 0).Count();
        Assert.Equal((int)Math.Ceiling(Motion.LandingMs(hop.Fall) / Hero.StepMs) - 1, Lasts(hop.After));
        Assert.True(Lasts(hop.After) < Lasts(held.After) && Lasts(held.After) < Lasts(fall.After));
    }

    [Fact]
    public void ADeeperLandingSinksTheHipsFurtherWithTheFeetStillOnTheFloor()
    {
        var hero = Standing;
        double Lift(double depth) => LimbsOf(Pose.Stand, hero, new Moment(0, 0, 2.0 / 3, Momentum: new Momentum(Depth: depth))).Lift;
        Assert.True(Lift(1.8) < Lift(1) - 2);
        Assert.True(Lift(1) < Lift(0.35) - 1);
        var deep = LimbsOf(Pose.Stand, hero, new Moment(0, 0, 2.0 / 3, Momentum: new Momentum(Depth: 1.8)));
        PoseTests.Close(Rig.Hip, Math.Max(Rig.EndOf(deep.FrontLeg, Rig.Leg).Y, Rig.EndOf(deep.BackLeg, Rig.Leg).Y) - deep.Lift, 5);
    }

    [Fact]
    public void TheAirPoseRunsFromRisingToFallingWithTheSpeedNotAtASwitch()
    {
        var air = Standing with { Vy = 1 };
        Limbs At(int vy) => LimbsOf(Pose.Air, air with { Vy = vy }, new Moment(0, 0));
        Assert.Equal(Airborne.Rise, At(-Spec.Jump));
        var fall = At(Look.Air.RiseBelow + Look.Air.Blend);
        PoseTests.Close(Airborne.Fall.FrontLeg.Upper, fall.FrontLeg.Upper, 6);
        PoseTests.Close(Airborne.Fall.BackArm.Lower, fall.BackArm.Lower, 6);
        // No step of the speed moves a limb far: the old poses swapped whole across 0.
        for (int vy = -Spec.Jump; vy < 600; vy += 6)
        {
            var (a, b) = (At(vy), At(vy + 6));
            foreach (var (x, y) in new[] { (a.FrontArm, b.FrontArm), (a.BackArm, b.BackArm), (a.FrontLeg, b.FrontLeg), (a.BackLeg, b.BackLeg) })
                Assert.True(Math.Abs(x.Upper - y.Upper) < 0.12 && Math.Abs(x.Lower - y.Lower) < 0.12, $"at {vy}: {x} to {y}");
        }
        // Going fast across it tips into its way.
        Assert.Equal(Look.Air.Drift, LimbsOf(Pose.Air, air with { Vx = Spec.Run }, new Moment(0, 0)).Tilt);
        Assert.Equal(-Look.Air.Drift, LimbsOf(Pose.Air, air with { Vx = -Spec.Run }, new Moment(0, 0)).Tilt);
        Assert.Equal(0, LimbsOf(Pose.Air, air with { Vx = Spec.Walk / 2 }, new Moment(0, 0)).Tilt);
    }

    [Fact]
    public void TheKneesTuckAtTheTopTheLegsPushJustAfterAJumpAndTheArmsFlailOnlyALongFall()
    {
        Assert.Equal(Look.Air.Tuck, TopOf(0));
        Assert.Equal(Look.Air.Tuck / 2, TopOf(-Look.Air.Apex / 2));
        Assert.Equal(0, TopOf(Look.Air.Apex));
        Assert.Equal(1, PushOf(0));
        Assert.Equal(0.5, PushOf(Look.Air.PushMs / 2));
        Assert.Equal(0, PushOf(Look.Air.PushMs));
        Assert.Equal(0, PushOf(double.PositiveInfinity));
        Assert.Equal(0, FlailOf(Look.Air.FlailSpeed, 2000));
        Assert.Equal(0, FlailOf(Spec.MaxFall, Look.Air.FlailAfterMs));
        Assert.Equal(0.5, FlailOf(Spec.MaxFall, Look.Air.FlailAfterMs + Look.Air.FlailMs / 2));
        Assert.Equal(1, FlailOf(Spec.MaxFall, 5000));

        // A held jump: the push for its first 110 ms, the knees highest at its top, and no flailing on the way down.
        var frames = Drive(Standing, [Still, Still with { Jump = true }, .. Hold(HeroTests.Held, 80)]);
        var aloft = frames.Skip(1).TakeWhile(f => f.Pose == Pose.Air).ToList();
        Assert.Equal(PushOf(0), PushOf(aloft[0].Now.SinceJumpMs));
        // At 0 to 100 ms: seven 60 Hz frames.
        Assert.Equal(7, aloft.Count(f => PushOf(f.Now.SinceJumpMs) > 0));
        var top = aloft.OrderBy(f => Math.Abs(f.Hero.Vy)).First();
        Assert.True(TopOf(top.Hero.Vy) > 0.3, $"{top.Hero.Vy}");
        Assert.All(aloft, f => Assert.Equal(0, FlailOf(f.Hero.Vy, f.Now.FallingMs)));
        // Off nineteen cells it flails, most of the way to its full by the time it lands.
        var fall = Drive(Standing with { Y = Floor - 19 * Unit }, Hold(Still, 120)).TakeWhile(f => f.Pose == Pose.Air).ToList();
        Assert.True(FlailOf(fall[^1].Hero.Vy, fall[^1].Now.FallingMs) > 0.6, $"{fall[^1]}");
        // Walked off, not jumped: no push.
        Assert.All(fall, f => Assert.Equal(0, PushOf(f.Now.SinceJumpMs)));
    }

    [Fact]
    public void FlailingTheFrontArmHoldingAnItemKeepsCalm()
    {
        var falling = Standing with { Vy = Spec.MaxFall };
        var motion = new Momentum(FallingMs: 2000);
        Bend Front(double ms, bool wields) => LimbsOf(Pose.Air, falling, new Moment(0, ms, Wields: wields, Momentum: motion)).FrontArm;
        Assert.True(Math.Abs(Front(0, false).Upper - Front(100, false).Upper) > 0.1);
        Assert.Equal(Front(0, true), Front(100, true));
        PoseTests.Close(Airborne.Fall.FrontArm.Upper, Front(100, true).Upper, 6);
    }

    [Fact]
    public void AFriendsJumpAndLandingAreSeenFromWhatItSends()
    {
        var frames = Drive(Standing, [Still with { Jump = true }, .. Hold(HeroTests.Held, 40), .. Hold(Still, 60)], sendEvery: 3);
        Assert.Single(frames, f => f.Cues.Jumped);
        var landed = Assert.Single(frames, f => f.Cues.Landed != null);
        Assert.InRange(landed.Now.Depth, 0.8, 1.2);
    }
}
