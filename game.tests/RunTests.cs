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
    public void ARunLeansWellInAndLeavesTheFloorBetweenSteps()
    {
        Assert.True(LimbsOf(Pose.Run, Running, At(0)).Tilt >= 0.28);
        // Mid-stance a foot is down; between the steps both are up and the body rises.
        Assert.Equal(0, Bounce(Stride.Run * 0.5));
        double flight = Bounce(Stride.Run * 0.26);
        Assert.InRange(flight, HeroMotion.Run.Bounce * 0.8, HeroMotion.Run.Bounce);
        Assert.True(LimbsOf(Pose.Run, Running, At(Stride.Run * 0.26)).Lift > LimbsOf(Pose.Run, Running, At(Stride.Run * 0.5)).Lift);
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
