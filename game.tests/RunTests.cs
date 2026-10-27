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
        Assert.Equal(0, Bounce(Stride.Run * 0.2));
        double flight = Bounce(Stride.Run * 0.46);
        Assert.InRange(flight, HeroMotion.Run.Bounce * 0.8, HeroMotion.Run.Bounce);
        Assert.True(LimbsOf(Pose.Run, Running, At(Stride.Run * 0.46)).Lift > LimbsOf(Pose.Run, Running, At(Stride.Run * 0.2)).Lift);
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
            Assert.True(l.Lift <= 0, $"off the floor at {at}");
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
