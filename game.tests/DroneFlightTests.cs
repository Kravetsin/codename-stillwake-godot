using Stillwake.Game.Features.Crew;
using Stillwake.Game.Shared;
using Xunit;
using T = Stillwake.Game.Features.Crew.DroneFlight.Tune;

namespace Stillwake.Game.Tests;

/// <summary>The light drone's body flies by its hero on its own (drone-motion ADR): no jump on a turn, no wall, caught up smoothly.</summary>
public class DroneFlightTests
{
    private static double Apart(Vec a, Vec b) => DroneFlight.Length(DroneFlight.Minus(a, b));

    private static readonly double RunSpeed = DroneFlight.Length(DroneFlight.VelocityOf(Content.Hero.Run, 0));

    private static readonly double JetSpeed = DroneFlight.Length(DroneFlight.VelocityOf(Content.Jetpack.Top, 0));

    private readonly record struct Flown(double Fastest, double Hardest, double Furthest, double Left, double Past);

    /// <summary>Flies it by a hero moving as <paramref name="moving"/> says at each ms, its goal a perch aside.</summary>
    private static Flown Fly(Func<double, Vec, Vec> moving, int fps, double forMs)
    {
        double ms = 1000.0 / fps;
        var flight = new DroneFlight();
        var aside = new Vec(14, 0);
        Vec anchor = default, hero = default, was = default, speed = default;
        flight.Step(anchor, hero, aside, false, false, ms);
        was = flight.At;
        double fastest = 0, hardest = 0, furthest = 0, left = 0, past = 0;
        for (double at = ms; at < forMs; at += ms)
        {
            hero = moving(at, hero);
            anchor = DroneFlight.Plus(anchor, DroneFlight.Scaled(hero, ms));
            var goal = DroneFlight.Plus(anchor, aside);
            var now = flight.Step(anchor, hero, goal, false, false, ms);
            var flew = DroneFlight.Scaled(DroneFlight.Minus(now, was), 1 / ms);
            (fastest, hardest) = (Math.Max(fastest, DroneFlight.Length(flew)), Math.Max(hardest, Apart(flew, speed) / ms));
            (was, speed, left) = (now, flew, Apart(now, goal));
            furthest = Math.Max(furthest, left);
            if (hero.X < 0) past = Math.Max(past, now.X - goal.X);
        }
        return new Flown(fastest, hardest, furthest, left, past);
    }

    /// <summary>A jetpack's velocity a frame on from <paramref name="hero"/> thrusting toward <paramref name="toward"/>, full speed in half a second.</summary>
    private static Vec Thrust(Vec hero, Vec toward, int fps) =>
        DroneFlight.Plus(hero, DroneFlight.Capped(DroneFlight.Minus(toward, hero), JetSpeed / 500 * 1000 / fps));

    /// <summary>In play it hung behind a flight as on a wall: it must outfly the hero until it is by it again, and then keep by it.</summary>
    [Theory]
    [InlineData(30)]
    [InlineData(144)]
    public void LeftBehindAFlightItSpeedsUpPastTheHeroUntilItHasCaughtUp(int fps)
    {
        var flown = Fly((_, hero) => Thrust(hero, new Vec(JetSpeed, 0), fps), fps, 5000);
        Assert.InRange(flown.Furthest, 16, 56);
        Assert.InRange(flown.Fastest, JetSpeed * 1.1, JetSpeed * 1.4);
        Assert.True(flown.Left < 2, $"still {flown.Left:F1} px behind a steady flight");
    }

    /// <summary>In play it changed place in a blink as the hero twisted in fast flight: a turn bends its way, never snaps it.</summary>
    [Theory]
    [InlineData(30)]
    [InlineData(144)]
    public void AFlightTurnedBackOrRoundAndRoundBendsItsWayWithoutASnap(int fps)
    {
        var back = Fly((ms, hero) => Thrust(hero, new Vec(ms < 2500 ? JetSpeed : -JetSpeed, 0), fps), fps, 6000);
        var round = Fly((ms, hero) => Thrust(hero, DroneFlight.Scaled(new Vec(Math.Cos(ms / 350), Math.Sin(ms / 350)), JetSpeed), fps), fps, 6000);
        Assert.True(back.Hardest < 0.0012, $"turned back at {back.Hardest:F4} px/ms²");
        Assert.True(round.Hardest < 0.0012, $"rounded at {round.Hardest:F4} px/ms²");
        Assert.True(back.Past > 16, $"carried only {back.Past:F1} px past the turn");
        Assert.True(back.Left < 2, $"still {back.Left:F1} px off after the turn");
    }

    [Theory]
    [InlineData(30)]
    [InlineData(144)]
    public void ARunReversedLeavesItBehindAndItCatchesUp(int fps)
    {
        var flown = Fly((ms, _) => new Vec(ms < 2000 ? RunSpeed : -RunSpeed, 0), fps, 5000);
        Assert.True(flown.Fastest <= RunSpeed * 1.8, $"flew {flown.Fastest / RunSpeed:F2} runs fast");
        Assert.True(flown.Past > 16, $"trailed only {flown.Past:F1} px after the turn");
        Assert.True(flown.Left < 2, $"still {flown.Left:F1} px off after the run");
    }

    /// <summary>The longest fall is left behind some cells, then made good: no circle stops it dead.</summary>
    [Theory]
    [InlineData(30)]
    [InlineData(144)]
    public void TheLongestFallMeetsNoWall(int fps)
    {
        double fastest = DroneFlight.Length(DroneFlight.VelocityOf(0, Content.Hero.MaxFall));
        var flown = Fly((ms, _) => new Vec(0, ms < 3000 ? Math.Min(fastest, fastest * ms / 1000) : 0), fps, 7000);
        Assert.True(flown.Hardest < 0.016, $"stopped or flung at {flown.Hardest:F4} px/ms²");
        Assert.InRange(flown.Furthest, 60, 160);
        Assert.True(flown.Left < 2, $"still {flown.Left:F1} px off after the landing");
    }

    /// <summary>Seen in play sitting in the helmet: its reticle clears the crown on every perch, and the body turned any way afloat.</summary>
    [Fact]
    public void ItsLowestPerchClearsTheCrown()
    {
        const double reticle = 5;
        var neck = new Vec(100, 50);
        double lowest = DroneFlight.Anchor(neck, 100).Y + T.HomeY + reticle;
        Assert.True(lowest < neck.Y - (Rig.H - Rig.Neck), $"{lowest - neck.Y + Rig.H - Rig.Neck:F1} px into the helmet");
    }

    [Fact]
    public void AFacingFlipAloneMovesNothing()
    {
        // A turn mirrors the head about the feet's column: the anchor stays.
        var right = DroneFlight.Anchor(new Vec(102, 50), feetX: 100);
        var left = DroneFlight.Anchor(new Vec(98, 50), feetX: 100);
        Assert.Equal(right, left);
        var flight = new DroneFlight();
        var at = flight.Step(right, default, right, false, false, 16);
        for (int i = 0; i < 50; i++) Assert.Equal(at, flight.Step(left, default, left, false, false, 16));
    }

    [Fact]
    public void AfloatItsAnchorClearsABodyTurnedAnyWay()
    {
        var middle = new Vec(100, 50);
        var anchor = DroneFlight.AnchorAfloat(middle);
        Assert.Equal(middle.X, anchor.X);
        // A body turned any way reaches half its height from its middle, and the reticle 5 px from the drone.
        Assert.True(anchor.Y + 5 < middle.Y - Rig.H / 2, $"{anchor.Y} is in the body");
    }

    [Fact]
    public void AWindowGoalIsReachedInEveryPose()
    {
        // Before the waist from an anchor over the neck standing, crouching, swimming, and over the body's middle lying.
        foreach (var (neckUp, lying) in ((double, bool)[])[(34, false), (24, false), (20, false), (4, true)])
            foreach (int facing in (int[])[1, -1])
            {
                var feet = new Vec(200, 300);
                var anchor = lying ? DroneFlight.Anchor(default, 0, new Vec(feet.X - facing * 12, feet.Y - neckUp)) : DroneFlight.Anchor(new Vec(feet.X, feet.Y - neckUp), feet.X);
                var goal = new Vec(feet.X + facing * SheetHoloLook.OverAhead, feet.Y - SheetHoloLook.OverUp);
                var flight = new DroneFlight();
                flight.Step(anchor, default, anchor, false, false, 16);
                for (int ms = 0; ms < 1500; ms += 16) flight.Step(anchor, default, goal, window: true, darting: false, 16);
                Assert.True(flight.Off < SheetHoloLook.OverArrived, $"{flight.Off:F2} px short, neck {neckUp} up, facing {facing}");
            }
    }

    [Fact]
    public void TheFirstFrameAReShowAndAJumpPastLostPutItAtItsGoalAtRest()
    {
        var flight = new DroneFlight();
        var goal = new Vec(10, -5);
        Assert.Equal(goal, flight.Step(default, new Vec(0.2, 0), goal, false, false, 16));
        Assert.Equal(default, flight.Velocity);
        var moved = flight.Step(new Vec(30, 0), new Vec(0.2, 0), new Vec(30, -5), false, false, 16);
        Assert.NotEqual(new Vec(30, -5), moved);
        var far = new Vec(30 + T.Lost + 1, 0);
        Assert.Equal(new Vec(far.X, -5), flight.Step(far, default, new Vec(far.X, -5), false, false, 16));
        flight.Step(far, default, new Vec(far.X + 20, -5), false, false, 16);
        flight.Lose();
        Assert.Equal(new Vec(far.X + 20, -5), flight.Step(far, default, new Vec(far.X + 20, -5), false, false, 16));
        Assert.Equal(0, flight.Off);
    }
}
