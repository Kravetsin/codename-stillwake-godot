using Stillwake.Game.Features.Crew;
using Stillwake.Game.Shared;
using Xunit;
using T = Stillwake.Game.Features.Crew.DroneFlight.Tune;

namespace Stillwake.Game.Tests;

/// <summary>The light drone's body flies by its hero on its own (docs/plans/drone-motion.md): no jump on a turn, never out of reach.</summary>
public class DroneFlightTests
{
    private static double Apart(Vec a, Vec b) => DroneFlight.Length(DroneFlight.Minus(a, b));

    private static readonly double RunSpeed = DroneFlight.Length(DroneFlight.VelocityOf(Content.Hero.Run, 0));

    [Theory]
    [InlineData(30)]
    [InlineData(144)]
    public void ARunReversedCarriesItPastTheHeadButNeverJumpsAFrame(int fps)
    {
        double frameMs = 1000.0 / fps, dt = Math.Min(frameMs, T.MaxFrameMs);
        var flight = new DroneFlight();
        var anchor = new Vec(0, 0);
        double most = 0, past = 0;
        for (double ms = 0; ms < 4000; ms += frameMs)
        {
            int way = ms < 2000 ? 1 : -1;
            var hero = new Vec(way * RunSpeed, 0);
            anchor = DroneFlight.Plus(anchor, DroneFlight.Scaled(hero, frameMs));
            var was = flight.At;
            var at = flight.Step(anchor, hero, anchor, window: false, darting: false, frameMs);
            if (ms > 0) most = Math.Max(most, Apart(at, was) - (RunSpeed + T.Cruise) * dt);
            if (ms >= 2000) past = Math.Max(past, at.X - anchor.X);
        }
        Assert.True(most <= 0.1, $"moved {most:F3} px more than its bound in a frame");
        Assert.True(past > 4, $"carried only {past:F1} px past the head");
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
    public void ItStaysWithinReachAtAnyHeroMotionUpToTheFastestFall()
    {
        var random = new Random(7);
        double fastest = DroneFlight.Length(DroneFlight.VelocityOf(0, Content.Hero.MaxFall));
        var flight = new DroneFlight();
        var anchor = new Vec(0, 0);
        var hero = new Vec(0, 0);
        var goal = anchor;
        for (int frame = 0; frame < 20000; frame++)
        {
            if (frame % 30 == 0)
            {
                double angle = random.NextDouble() * Math.Tau, speed = random.NextDouble() * fastest;
                hero = new Vec(Math.Cos(angle) * speed, Math.Sin(angle) * speed);
                goal = new Vec(random.NextDouble() * 400 - 200, random.NextDouble() * 400 - 200);
            }
            double ms = random.Next(4, 40);
            anchor = DroneFlight.Plus(anchor, DroneFlight.Scaled(hero, ms));
            var at = flight.Step(anchor, hero, DroneFlight.Plus(anchor, goal), false, darting: frame % 60 < 30, ms);
            Assert.InRange(Apart(at, anchor), 0, T.Reach + 1e-9);
        }
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
