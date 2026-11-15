using System;
using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The catcher's motion as its picks draw it (catcher-motion): the curve of its flight, its throw from the belt, its gun as the sim turns it.</summary>
public class CatcherMotionTests
{
    [Fact]
    public void ItsFlightPassesEveryTicksPlaceAndBendsSmoothlyBetween()
    {
        // At the ticks' places themselves.
        Assert.Equal(1000, CatcherMotion.Along(0, 1000, 2100, 0), 9);
        Assert.Equal(2100, CatcherMotion.Along(0, 1000, 2100, 1), 9);
        // Steady, it is a straight line.
        Assert.Equal(1500, CatcherMotion.Along(0, 1000, 2000, 0.5), 9);
        // Speeding up it lags a straight line early in the tick: its way there is the mean of the moves either side.
        Assert.True(CatcherMotion.Along(0, 500, 1500, 0.25) < 750);
        // Speeding up steadily, its speed carries on across a tick's place: the way leaving one tick is the way the next arrives.
        double end = Slope(t => CatcherMotion.Along(0, 500, 1500, t), 1), start = Slope(t => CatcherMotion.Along(500, 1500, 3000, t), 0);
        Assert.InRange(end - start, -1, 1);
    }

    [Fact]
    public void AJumpIsDrawnStraight()
    {
        Assert.Equal(1000 + 4000 * 0.25, CatcherMotion.Along(0, 1000, 5000, 0.25), 9);
        Assert.Equal(9000 + 1000 * 0.25, CatcherMotion.Along(0, 9000, 10000, 0.25), 9);
    }

    [Fact]
    public void ItsPlumeIsLongThrustingAndShortCoasting()
    {
        Assert.Equal(0, CatcherMotion.Thrust(0, 0, 100));
        Assert.Equal(1, CatcherMotion.Thrust(100, 100, 100));
        Assert.Equal(0.5, CatcherMotion.Thrust(50, 0, 100), 9);
        // Braking facing its way it leans forward; speeding up, back.
        Assert.True(CatcherMotion.Leaning(-100, 1, 100) > 0);
        Assert.True(CatcherMotion.Leaning(100, 1, 100) < 0);
        Assert.Equal(CatcherMotion.Leaning(-100, 1, 100), CatcherMotion.Leaning(100, -1, 100), 9);
    }

    [Fact]
    public void TheChargeHangsAtItsBeltTillItsHandUnhooksItAndIsGoneOnceThrown()
    {
        var idle = CatcherMotion.ThrowOf(null);
        Assert.True(idle.OnBelt);
        Assert.False(idle.InHand);
        Assert.Equal(0, idle.Share);
        var reaching = CatcherMotion.ThrowOf(CatcherMotion.GrabMs - 1);
        Assert.True(reaching.OnBelt && !reaching.InHand && reaching.Share > 0);
        var unhooked = CatcherMotion.ThrowOf(CatcherMotion.GrabMs);
        Assert.True(unhooked.InHand && !unhooked.OnBelt);
        Assert.Equal(1, unhooked.Flare, 9);
        Assert.Equal(0, CatcherMotion.ThrowOf(CatcherMotion.GrabMs + CatcherMotion.FlareMs).Flare);
        var thrown = CatcherMotion.ThrowOf(CatcherMotion.ReleaseMs);
        Assert.False(thrown.InHand || thrown.OnBelt);
        // Its hand comes back as it floats, and the next charge hangs at its belt again.
        Assert.Equal(0, CatcherMotion.ThrowOf(CatcherMotion.DoneMs - 1).Share, 2);
        Assert.True(CatcherMotion.ThrowOf(CatcherMotion.DoneMs).OnBelt);
    }

    [Fact]
    public void ItDrawsTheChargeToItsChestThenPushesItStraightOut()
    {
        // Drawn in: the elbow bent hard, the charge along the forearm.
        var drawn = CatcherMotion.ThrowOf(500);
        Assert.True(drawn.Arm.Lower > 1.4);
        Assert.Equal(0, drawn.Hold, 9);
        Assert.True(drawn.Lean < 0);
        // Pushed out: the arm straight ahead, the body leaning into it.
        var pushed = CatcherMotion.ThrowOf(650);
        Assert.True(pushed.Arm.Upper > 1.4 && pushed.Arm.Lower < 0.1);
        Assert.True(pushed.Lean > 0);
        // No step between frames anywhere in it.
        for (double ms = 0; ms < CatcherMotion.DoneMs; ms += 1)
        {
            var (a, b) = (CatcherMotion.ThrowOf(ms), CatcherMotion.ThrowOf(ms + 1));
            Assert.True(Math.Abs(a.Arm.Upper * a.Share - b.Arm.Upper * b.Share) < 0.05, $"the arm jumps at {ms} ms");
            Assert.True(Math.Abs(a.Lean - b.Lean) < 0.005, $"the body jumps at {ms} ms");
        }
    }

    [Fact]
    public void ItsGunIsDrawnTurningTheShortWayBetweenTheSimsHeadings()
    {
        Assert.Equal(0, CatcherMotion.GunAngle(-1, 0, 0.5), 9);
        Assert.Equal(Math.PI / 2, CatcherMotion.GunAngle(Rays.FineTurn / 4, Rays.FineTurn / 4, 0.3), 9);
        // From just short of the full turn to just past it, it turns through none, not back round the long way.
        double half = CatcherMotion.GunAngle(Rays.FineTurn - 10, 10, 0.5);
        Assert.Equal(0, Math.Abs(Poses.Apart(0, half)), 9);
        Assert.True(Math.Abs(CatcherMotion.GunAngle(Rays.FineTurn - 10, 10, 0.25) - half) < 0.01);
    }

    [Fact]
    public void ItsDrawnGunChasesAChangeNoFasterThanItMayAndNeverPastIt()
    {
        Assert.Equal(CatcherMotion.ChaseRate * 0.016, CatcherMotion.Chase(0, 2, 16), 9);
        Assert.Equal(0.3, CatcherMotion.Chase(0.29, 0.3, 1000), 9);
        // The short way round: from just under a full turn to just over none, forward.
        Assert.True(CatcherMotion.Chase(2 * Math.PI - 0.05, 0.05, 16) > 2 * Math.PI - 0.05);
    }

    [Fact]
    public void ItTurnsRoundToItsGunOnlyPastUpright()
    {
        Assert.Equal(1, CatcherMotion.FacingOf(Math.PI / 2 + 0.1, 1));
        Assert.Equal(-1, CatcherMotion.FacingOf(Math.PI * 0.8, 1));
        Assert.Equal(-1, CatcherMotion.FacingOf(Math.PI / 2 - 0.1, -1));
        Assert.Equal(1, CatcherMotion.FacingOf(0.1, -1));
    }

    private static double Slope(Func<double, double> f, double t) => (f(Math.Min(1, t + 1e-4)) - f(Math.Max(0, t - 1e-4))) / (Math.Min(1, t + 1e-4) - Math.Max(0, t - 1e-4));
}
