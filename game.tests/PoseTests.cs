using Stillwake.Game.Features.Crew;
using Xunit;
using static Stillwake.Game.Features.Crew.Poses;

namespace Stillwake.Game.Tests;

/// <summary>Poses and the head: apps/game/src/features/crew/pose.test.ts, one for one.</summary>
public class PoseTests
{
    private static readonly Hero Standing = new(5500, 10000, 0, 0, false, 1);
    private static readonly Hero Walking = Standing with { Vx = 120 };

    private static Moment At(double travelled, double timeMs = 0) => new(travelled, timeMs);

    /// <summary>Vitest's toBeCloseTo: within half a unit of the last of <paramref name="digits"/> decimals.</summary>
    internal static void Close(double expected, double actual, int digits = 2) =>
        Assert.True(Math.Abs(expected - actual) < Math.Pow(10, -digits) / 2, $"{actual} is not close to {expected}");

    [Fact]
    public void PoseOfReadsWhatTheHeroDoesFromHowItMoves()
    {
        Assert.Equal(Pose.Stand, PoseOf(Standing, false, false));
        Assert.Equal(Pose.Walk, PoseOf(Standing with { Vx = 120 }, false, false));
        Assert.Equal(Pose.Run, PoseOf(Standing with { Vx = -220 }, false, false));
        Assert.Equal(Pose.Air, PoseOf(Standing with { Vx = 240, Vy = -420 }, false, false));
        Assert.Equal(Pose.Air, PoseOf(Standing with { Vy = 21 }, false, false));
        Assert.Equal(Pose.Climb, PoseOf(Standing with { Climbing = true }, false, false));
        Assert.Equal(Pose.Float, PoseOf(Standing with { Vy = 120 }, true, false));
    }

    [Fact]
    public void PoseOfLiesOnTheFloorPassedOutAndTumblesInSpace()
    {
        Assert.Equal(Pose.Lie, PoseOf(Standing with { Vy = 21 }, false, true));
        Assert.Equal(Pose.Tumble, PoseOf(Standing, true, true));
    }

    [Fact]
    public void SwingsLegsAgainstEachOtherAndArmsAgainstLegsWhileWalking()
    {
        var l = LimbsOf(Pose.Walk, Walking, At(Stride.Walk / 4));
        Assert.True(l.FrontLeg.Upper > 0.3);
        Close(-l.FrontLeg.Upper, l.BackLeg.Upper);
        Assert.Equal(-Math.Sign(l.FrontLeg.Upper), Math.Sign(l.FrontArm.Upper));
    }

    [Fact]
    public void LiftsTheKneeOfTheLegSwingingForwardAndKeepsTheOtherNearStraight()
    {
        // A stride begun, the front leg swings forward past the back one.
        var l = LimbsOf(Pose.Walk, Walking, At(0));
        Assert.True(l.FrontLeg.Lower < -0.8);
        Assert.True(l.BackLeg.Lower > -0.2);
    }

    [Fact]
    public void TakesOneStridePerStrideLengthWalkedWhateverTheTime()
    {
        Bend Leg(double cells, double ms) => LimbsOf(Pose.Walk, Walking, At(cells, ms)).FrontLeg;
        Close(Leg(1.1, 5000).Upper, Leg(1.1 + Stride.Walk, 0).Upper);
        Close(0, Leg(Stride.Walk / 2, 0).Upper);
    }

    [Fact]
    public void KeepsAFootOnTheFloorStandingAndWalkingNeverFloatingNorSunkIn()
    {
        for (int i = 0; i < 24; i++)
            foreach (var pose in new[] { Pose.Stand, Pose.Walk })
            {
                var l = LimbsOf(pose, pose == Pose.Walk ? Walking : Standing, At(i * 0.4, i * 170));
                double lowest = Math.Max(Rig.EndOf(l.FrontLeg, Rig.Leg).Y, Rig.EndOf(l.BackLeg, Rig.Leg).Y);
                Close(Rig.Hip, lowest - l.Lift, 5);
                Assert.True(l.Lift <= 0);
            }
        // Lowest with both feet apart, highest passing over the straight leg.
        double apart = LimbsOf(Pose.Walk, Walking, At(Stride.Walk / 4)).Lift;
        double passing = LimbsOf(Pose.Walk, Walking, At(0)).Lift;
        Assert.True(apart < passing - 0.5);
    }

    [Fact]
    public void StandsOnNearStraightLegsOnlyTheArmsBreathing()
    {
        foreach (var ms in new[] { 0, 700, 1300, 2000 })
        {
            var l = LimbsOf(Pose.Stand, Standing, At(3, ms));
            Assert.True(Math.Abs(l.FrontLeg.Lower) < 0.1);
            Close(Rig.Hip, Rig.EndOf(l.FrontLeg, Rig.Leg).Y, 0);
            Assert.True(Math.Abs(l.FrontArm.Upper) < 0.1);
        }
    }

    [Fact]
    public void SinksIntoALandingWithTheFeetKeptOnTheFloorThenSpringsBack()
    {
        var deep = LimbsOf(Pose.Stand, Standing, At(0) with { Landing = 2.0 / 3 });
        Assert.True(deep.Lift < -Land.Sink * 0.9);
        // The hips come down by the sink; the feet reach just as far.
        Close(Rig.Hip - Land.Sink * 0.99, Rig.EndOf(deep.FrontLeg, Rig.Leg).Y, 0);
        Assert.True(deep.FrontLeg.Lower < -0.3);
        var done = LimbsOf(Pose.Stand, Standing, At(0) with { Landing = 0 });
        Assert.True(done.Lift > -1);
    }

    [Fact]
    public void TakesRungsOverItsHeadOnALadderOneHandUpWhileTheOtherHolds()
    {
        var l = LimbsOf(Pose.Climb, Standing with { Climbing = true }, At(0.3));
        var front = Rig.EndOf(l.FrontArm, Rig.Arm);
        var back = Rig.EndOf(l.BackArm, Rig.Arm);
        Assert.True(Math.Min(front.Y, back.Y) < -Rig.Arm.Upper);
        Assert.False(Math.Abs(front.Y - back.Y) < 0.5);
    }

    [Fact]
    public void LiesTurnedOntoItsBackAndTumblesFurtherTheLongerItDrifts()
    {
        var lying = LimbsOf(Pose.Lie, Standing, At(0));
        Assert.Equal(-Math.PI / 2, lying.Tilt);
        Assert.True(lying.Lying);
        double Tumble(double ms) => LimbsOf(Pose.Tumble, Standing, At(0, ms)).Tilt;
        Assert.True(Tumble(2000) > Tumble(1000));
        Assert.False(LimbsOf(Pose.Tumble, Standing, At(0, 1000)).Lying);
    }

    [Fact]
    public void LeansIntoASwimAlongTheFloorInSpaceUprightOtherwise()
    {
        double across = LimbsOf(Pose.Float, Standing with { Vx = 80 }, At(1)).Tilt;
        double up = LimbsOf(Pose.Float, Standing with { Vy = -80 }, At(1)).Tilt;
        Assert.True(across > up);
    }

    [Fact]
    public void SwimsABreaststrokeInSpaceArmsReachAheadThenSweepBackToTheHips()
    {
        var swimming = Standing with { Vx = 80 };
        Assert.True(LimbsOf(Pose.Float, swimming, At(0, 0)).FrontArm.Upper > 2.5);
        Assert.True(LimbsOf(Pose.Float, swimming, At(0, Adrift.StrokeMs * 0.4)).FrontArm.Upper < 0);
        // Folded to the chest with the knees drawn up before reaching again.
        var folded = LimbsOf(Pose.Float, swimming, At(0, Adrift.StrokeMs * 0.75));
        Assert.True(folded.FrontArm.Lower > 1.5 && folded.FrontLeg.Lower < -1.5);
    }

    [Fact]
    public void SwimsFastHeadFirstFlatAlongTheFloorUprightGoingUp()
    {
        Close(Math.PI / 2, LimbsOf(Pose.Float, Standing with { Vx = 160 }, At(0)).Tilt);
        Close(0, LimbsOf(Pose.Float, Standing with { Vy = -160 }, At(0)).Tilt);
        Assert.True(LimbsOf(Pose.Float, Standing with { Vy = 160 }, At(0)).Tilt > 3);
    }

    [Fact]
    public void CrawlsSwimmingFastOneArmAheadWhileTheOtherPullsFeetFluttering()
    {
        var fast = Standing with { Vx = 160 };
        var l = LimbsOf(Pose.Float, fast, At(0, 0));
        Close(Math.PI, l.FrontArm.Upper);
        Assert.True(l.BackArm.Upper < 1);
        var later = LimbsOf(Pose.Float, fast, At(0, Adrift.CrawlMs / 12));
        Assert.True(Math.Sign(later.FrontLeg.Upper - 0.05) != Math.Sign(later.BackLeg.Upper - 0.05));
    }

    [Fact]
    public void KeepsAFootUnderTheBodyRunningSoItNeverSinksFar()
    {
        for (double at = 0; at < Stride.Run; at += 0.1)
            Assert.True(LimbsOf(Pose.Run, Standing with { Vx = 220 }, At(at)).Lift > -Rig.Hip * 0.18);
    }

    [Fact]
    public void RunsLeaningInElbowsBentKneesHigherThanItWalks()
    {
        var run = LimbsOf(Pose.Run, Standing with { Vx = 220 }, At(Stride.Run / 2));
        var walk = LimbsOf(Pose.Walk, Walking, At(Stride.Walk / 2));
        Assert.True(run.Tilt > walk.Tilt);
        Assert.True(run.FrontArm.Lower > 1.2);
        Assert.True(Math.Min(run.FrontLeg.Lower, run.BackLeg.Lower) < Math.Min(walk.FrontLeg.Lower, walk.BackLeg.Lower));
    }

    [Fact]
    public void KeepsItsStrokeOnTheDiagonalTimedNotByDistance()
    {
        var across = LimbsOf(Pose.Float, Standing with { Vx = 80 }, At(0, 500));
        var diagonal = LimbsOf(Pose.Float, Standing with { Vx = 80, Vy = 80 }, At(7, 500));
        Assert.Equal(across.FrontArm, diagonal.FrontArm);
    }

    [Fact]
    public void BendsItsElbowsForwardOnALadderAsArmsBend()
    {
        for (double at = 0; at < 2; at += 0.1)
        {
            var l = LimbsOf(Pose.Climb, Standing with { Climbing = true }, At(at));
            Assert.True(l.FrontArm.Lower >= 0 && l.BackArm.Lower >= 0);
        }
    }

    [Fact]
    public void TreadsWaterAdriftUntilNowAndThenItPanicsAndWindmills()
    {
        double Reach(double ms) => Math.Max(LimbsOf(Pose.Float, Standing, At(0, ms)).FrontArm.Upper,
            LimbsOf(Pose.Float, Standing, At(0, ms)).BackArm.Upper);
        double calmest = Enumerable.Range(0, 70).Max(k => Reach(k * 100));
        Assert.True(calmest < 1.5);
        Assert.Equal(0, Panic(1000));
        double bout = Adrift.PanicEveryMs - Adrift.PanicMs / 2;
        Assert.Equal(1, Panic(bout));
        // Mid-bout the arms whirl right round, over the head and on behind it.
        Assert.Contains(Enumerable.Range(0, 8), k =>
        {
            var l = LimbsOf(Pose.Float, Standing, At(0, bout + k * Adrift.WhirlMs / 8));
            return Math.Abs(l.FrontArm.Upper) > 2.6 || Math.Abs(l.BackArm.Upper) > 2.6;
        });
        Assert.True(Panic(Adrift.PanicEveryMs - Adrift.PanicMs + Adrift.PanicEaseMs / 2) is > 0 and < 1);
    }

    [Fact]
    public void PutsTheFrontHandWhereTheSheetIsHeldTurnedWithTheHeadTheFarHandFurtherOn()
    {
        foreach (var look in new[] { 0, Head.Down, -Head.Up })
        {
            var l = LimbsOf(Pose.Walk, Walking, new Moment(1, 0, Plan: look));
            var hand = Rig.EndOf(l.FrontArm, Rig.Arm);
            Close(PlanHand(look).X, hand.X, 1);
            Close(PlanHand(look).Y, hand.Y, 1);
            Assert.True(Rig.EndOf(l.BackArm, Rig.Arm).X > hand.X);
        }
        Assert.True(PlanHand(-Head.Up).Y < PlanHand(0).Y);
        Assert.True(PlanHand(Head.Down).Y > PlanHand(0).Y);
    }

    [Fact]
    public void HoldsNothingClimbingLyingOrTumbling()
    {
        Assert.All(new[] { Pose.Stand, Pose.Walk, Pose.Air, Pose.Float }, p => Assert.True(HoldsIn(p)));
        Assert.All(new[] { Pose.Climb, Pose.Lie, Pose.Tumble }, p => Assert.False(HoldsIn(p)));
    }

    [Fact]
    public void HeadTurnLooksStraightAtAPointAheadDownPositive()
    {
        Assert.Equal(0, HeadTurn(5, 0));
        Close(Math.Atan2(1, 4), HeadTurn(4, 1));
        Close(-Math.Atan2(1, 4), HeadTurn(4, -1));
    }

    [Fact]
    public void HeadTurnNeverLooksFurtherUpOrDownThanTheNeckAllows()
    {
        Assert.Equal(-Head.Up, HeadTurn(0.1, -10));
        Assert.Equal(Head.Down, HeadTurn(0.1, 10));
        Assert.Equal(-Head.Up, HeadTurn(0, -3));
    }

    [Fact]
    public void HeadTurnOnlyLooksUpOrDownAtAPointBehindAsAtOneAsFarAhead()
    {
        Close(HeadTurn(4, 1), HeadTurn(-4, 1));
        Assert.Equal(0, HeadTurn(-4, 0));
    }
}
