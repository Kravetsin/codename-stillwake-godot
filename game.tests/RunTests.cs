using System;
using Stillwake.Game.Features.Crew;
using Xunit;
using static Stillwake.Game.Features.Crew.Poses;

namespace Stillwake.Game.Tests;

/// <summary>The run's look (<see cref="HeroMotion.Run"/>, <see cref="HeroMotion.Carry"/>): its lean and flight, braking, and an item carried trailing.</summary>
public class RunTests
{
    private static readonly Hero Running = new(11000, 20000, 440, 0, false, 1);
    private static Moment At(double travelled) => new(travelled, 0);

    [Fact]
    public void ARunLeansWellInAndRidesHigherBetweenSteps()
    {
        Assert.True(LimbsOf(Pose.Run, Running, At(0)).Tilt >= HeroMotion.Run.Lean - HeroMotion.Run.Pitch);
        // Mid-step a foot is down; between the steps the body rides higher, both feet off the floor.
        var seen = Enumerable.Range(0, 100).Select(k => LimbsOf(Pose.Run, Running, At(Stride.Run * k / 100))).ToList();
        var mid = seen.MinBy(l => l.Lift);
        var between = seen.MaxBy(l => l.Lift);
        Assert.True(Math.Abs(PoseTests.Soles(mid).Max(p => p.Y)) < 0.05, $"mid-step the lowest sole is at {PoseTests.Soles(mid).Max(p => p.Y)}");
        Assert.True(PoseTests.Soles(between).Max(p => p.Y) < -0.2, $"between steps the lowest sole is at {PoseTests.Soles(between).Max(p => p.Y)}");
        Assert.InRange(between.Lift - mid.Lift, HeroMotion.Run.Bounce * 0.9, HeroMotion.Run.Bounce * 1.01);
    }

    [Fact]
    public void ARunSinksQuicklyOntoTheLegComingDownAndRisesSlowlyPushingOff()
    {
        // Rising and falling alike, the body bobbed like a ball; a run's weight comes down fast and is pushed up slowly.
        var lifts = Enumerable.Range(0, 201).Select(k => LimbsOf(Pose.Run, Running, At(Stride.Run * k / 200)).Lift).ToList();
        int rising = Enumerable.Range(0, 200).Count(k => lifts[k + 1] > lifts[k]);
        Assert.InRange(rising / 200.0, HeroMotion.Run.Rise - 0.05, HeroMotion.Run.Rise + 0.05);
    }

    [Fact]
    public void ARunNeverCrouchesTheLegOnTheFloorNearlyStraight()
    {
        // Held to a smooth ride, the knee on the floor once folded over a radian, a run in a crouch.
        for (int k = 0; k < 100; k++)
        {
            var l = LimbsOf(Pose.Run, Running, At(Stride.Run * k / 100));
            var soles = PoseTests.Soles(l).ToList();
            foreach (var (leg, at) in new[] { (l.FrontLeg, 0), (l.BackLeg, 2) })
                if (Math.Max(soles[at].Y, soles[at + 1].Y) > -0.05)
                    Assert.True(leg.Lower > -0.55, $"the knee on the floor folded {leg.Lower} at {k / 100.0}");
        }
    }

    [Fact]
    public void ARunningFootStaysWhereItCameDownTillItPushesOff()
    {
        // The leg swept back at half the body's speed: a foot on the floor slid twenty pixels on, the hero skating.
        int n = 400;
        foreach (int leg in new[] { 0, 2 })
        {
            double? down = null;
            for (int k = 0; k <= 2 * n; k++)
            {
                double travelled = Stride.Run * k / n;
                var soles = PoseTests.Soles(LimbsOf(Pose.Run, Running, At(travelled))).ToList();
                double x = travelled * Rig.Cell + (soles[leg].X + soles[leg + 1].X) / 2;
                if (Math.Max(soles[leg].Y, soles[leg + 1].Y) < -0.3) down = null;
                else if (down is double at) Assert.True(Math.Abs(x - at) < 4, $"a foot on the floor slid {x - at} px at {k / (double)n}");
                else down = x;
            }
        }
    }

    [Fact]
    public void ARunPitchesFurthestInAsAFootPushesOffAndRisesInTheFlight()
    {
        // Leaning alike all along, the body rode its legs like a plank.
        var seen = Enumerable.Range(0, 200).Select(k => LimbsOf(Pose.Run, Running, At(Stride.Run * k / 200))).ToList();
        var most = seen.MaxBy(l => l.Tilt);
        var least = seen.MinBy(l => l.Tilt);
        Assert.InRange(most.Tilt - least.Tilt, HeroMotion.Run.Pitch * 1.9, HeroMotion.Run.Pitch * 2.01);
        // Pitched furthest in, a foot is on the floor; least, both are off it.
        Assert.True(PoseTests.Soles(most).Max(p => p.Y) > -0.3, $"pitched in with the lowest sole at {PoseTests.Soles(most).Max(p => p.Y)}");
        Assert.True(PoseTests.Soles(least).Max(p => p.Y) < -0.3, $"pitched up with a sole at {PoseTests.Soles(least).Max(p => p.Y)}");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(HeroMotion.Lean.Forward)]
    public void ARunRidesSmoothlyAlikeOnBothStepsNeverThroughTheFloor(double lean)
    {
        // A frame at full run is 440 thousandths of a cell a step at 60 steps a second: the body moved a pixel or two a frame
        // when its height was read from the feet alone, and the step on the near leg sat lower than the far one's.
        double frame = 0.44 / Stride.Run;
        var moment = At(0) with { Momentum = new Momentum(Lean: lean) };
        Limbs Ran(double cycle) => LimbsOf(Pose.Run, Running, moment with { Cycle = cycle, Gait = 1, Moving = 1 });
        for (double cycle = 0; cycle < 1; cycle += frame / 4)
        {
            Assert.True(Math.Abs(Ran(cycle + frame).Lift - Ran(cycle).Lift) < 0.35, $"the body jumped {Ran(cycle + frame).Lift - Ran(cycle).Lift} px at {cycle}");
            Assert.True(PoseTests.Soles(Ran(cycle)).Max(p => p.Y) < 0.05, $"a sole {PoseTests.Soles(Ran(cycle)).Max(p => p.Y)} under the floor at {cycle}");
            Assert.True(Math.Abs(Ran(cycle + 0.5).Lift - Ran(cycle).Lift) < 0.05, $"one step at {Ran(cycle).Lift}, the other at {Ran(cycle + 0.5).Lift}");
        }
    }

    [Theory]
    [InlineData(Pose.Walk, 240)]
    [InlineData(Pose.Run, 440)]
    public void EachArmSwingsAgainstTheLegOnItsOwnSide(Pose pose, int vx)
    {
        // The near arm forward while the near leg is back, as people walk and run: the two move against each other.
        double stride = pose == Pose.Run ? Stride.Run : Stride.Walk;
        var seen = Enumerable.Range(0, 40).Select(k => LimbsOf(pose, Running with { Vx = vx }, At(stride * k / 40))).ToList();
        double Against(Func<Limbs, double> arm, Func<Limbs, double> leg)
        {
            double ma = seen.Average(arm), ml = seen.Average(leg);
            double cov = seen.Sum(l => (arm(l) - ma) * (leg(l) - ml));
            return cov / Math.Sqrt(seen.Sum(l => Math.Pow(arm(l) - ma, 2)) * seen.Sum(l => Math.Pow(leg(l) - ml, 2)));
        }
        Assert.True(Against(l => l.FrontArm.Upper, l => l.FrontLeg.Upper) < -0.6, $"near arm with near leg: {Against(l => l.FrontArm.Upper, l => l.FrontLeg.Upper)}");
        Assert.True(Against(l => l.BackArm.Upper, l => l.BackLeg.Upper) < -0.6, $"far arm with far leg: {Against(l => l.BackArm.Upper, l => l.BackLeg.Upper)}");
    }

    [Fact]
    public void AWalkTurnsIntoARunAndAStandIntoAStrideWithoutAJump()
    {
        // The figure eases its gait (2026-11-04-hero-gait): at its ends it is the pose itself, between them no limb jumps, nor the
        // body by a pixel.
        static double Apart(Limbs a, Limbs b) => new[]
        {
            a.FrontArm.Upper - b.FrontArm.Upper, a.BackArm.Upper - b.BackArm.Upper, a.FrontLeg.Upper - b.FrontLeg.Upper,
            a.BackLeg.Upper - b.BackLeg.Upper, a.FrontLeg.Lower - b.FrontLeg.Lower, a.Tilt - b.Tilt, (a.Lift - b.Lift) / 4,
        }.Max(Math.Abs);
        foreach (double cycle in new[] { 0.1, 0.4, 0.75 })
        {
            Limbs Eased(Pose pose, double gait, double moving) =>
                LimbsOf(pose, Running, At(0) with { Cycle = cycle, Gait = gait, Moving = moving });
            Assert.True(Apart(Eased(Pose.Run, 0, 1), LimbsOf(Pose.Walk, Running with { Vx = 240 }, At(cycle * Stride.Walk))) < 1e-9);
            Assert.True(Apart(Eased(Pose.Run, 1, 1), LimbsOf(Pose.Run, Running, At(cycle * Stride.Run))) < 1e-9);
            Assert.True(Apart(Eased(Pose.Stand, 1, 0), LimbsOf(Pose.Stand, Running with { Vx = 0 }, At(0))) < 1e-9);
            for (int k = 0; k < 20; k++)
            {
                Assert.True(Apart(Eased(Pose.Run, k / 20.0, 1), Eased(Pose.Run, (k + 1) / 20.0, 1)) < 0.12, $"walk into run at {cycle}, {k}: {Eased(Pose.Run, k / 20.0, 1)} then {Eased(Pose.Run, (k + 1) / 20.0, 1)}");
                Assert.True(Apart(Eased(Pose.Walk, 0, k / 20.0), Eased(Pose.Walk, 0, (k + 1) / 20.0)) < 0.12, $"stand into walk at {cycle}, {k}");
            }
        }
    }

    [Fact]
    public void RunningBackwardItLeansBackAndKeepsAFootDown()
    {
        var back = Running with { Vx = -440 };
        Assert.True(Backward(back));
        for (double at = 0; at < Stride.Back; at += 0.4)
        {
            var l = LimbsOf(Pose.Run, back, At(-at));
            Assert.True(l.Tilt < 0, $"leaning {l.Tilt} at {at}");
            Assert.True(Math.Abs(PoseTests.Soles(l).Max(p => p.Y)) < 0.05, $"off the floor at {at}");
        }
    }

    [Fact]
    public void RunningBackwardAFootOnTheFloorSlidesForwardAndACockedLegSwingsBack()
    {
        var back = Running with { Vx = -440 };
        double step = Stride.Back / 400;
        int stanceForward = 0, stanceBack = 0, swingForward = 0, swingBack = 0;
        for (double at = 0; at < Stride.Back; at += step)
        {
            var now = LimbsOf(Pose.Run, back, At(-at)).FrontLeg;
            var next = LimbsOf(Pose.Run, back, At(-at - step)).FrontLeg;
            double change = next.Upper - now.Upper;
            // A leg bent at the knee is off the floor, a straight one on it.
            bool straight = now.Lower > -0.16 && next.Lower > -0.16, bent = now.Lower < -0.5 && next.Lower < -0.5;
            if (straight) (change > 0 ? ref stanceForward : ref stanceBack)++;
            if (bent) (change > 0 ? ref swingForward : ref swingBack)++;
        }
        Assert.True(stanceForward > 0 && stanceBack == 0, $"on the floor the thigh went forward {stanceForward} times, back {stanceBack}");
        Assert.True(swingBack > 0 && swingForward == 0, $"off it the thigh went back {swingBack} times, forward {swingForward}");
    }

    [Fact]
    public void StoppingFromARunTheFrontLegPlantsAheadAndTheArmsGoForward()
    {
        var still = Running with { Vx = 0 };
        var calm = LimbsOf(Pose.Stand, still, At(0));
        var braking = LimbsOf(Pose.Stand, still, At(0) with { Momentum = new Momentum(Lean: -HeroMotion.Lean.Back) });
        Assert.True(braking.FrontLeg.Upper > calm.FrontLeg.Upper + 0.4);
        Assert.True(braking.FrontArm.Upper > calm.FrontArm.Upper + 0.4);
        Assert.True(braking.Tilt < 0);
    }

    [Fact]
    public void AnItemInOneHandIsCarriedPointingDownBehindTheHero()
    {
        // Carried, the hand turns any item along itself, the axe held square to it too: its hold taken back.
        foreach (var look in new[] { HeldLook.Harpoon, HeldLook.Nailer, HeldLook.FireAxe })
        {
            double turn = Rig.HeldTurn(Rig.Carried(0), look) + look.Hold;
            Assert.True(Math.Cos(turn) < -0.4 && Math.Sin(turn) > 0.4, $"{look.Picture} turned {turn}");
        }
    }

    [Fact]
    public void ASpringLagsAChangeAndSwingsOnPastIt()
    {
        var spring = new Spring(HeroMotion.Carry.SpringHz, HeroMotion.Carry.SpringDamping);
        spring.Hold(0);
        double first = spring.Follow(1, 50), most = first;
        for (int i = 0; i < 60; i++) most = Math.Max(most, spring.Follow(1, 1000.0 / 60));
        Assert.InRange(first, 0.01, 0.5);
        Assert.True(most > 1.1, $"it only reached {most}");
        for (int i = 0; i < 300; i++) spring.Follow(1, 1000.0 / 60);
        Assert.InRange(spring.At, 0.99, 1.01);
    }
}
