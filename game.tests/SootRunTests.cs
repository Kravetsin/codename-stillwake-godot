using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The drone's sparks framing a window (drone-frame, «Нити»): one clock up open, down shut; straight flights; the screen after.</summary>
public class SootRunTests
{
    private const double Frame = 1 / 60.0;
    private static readonly (float X, float Y)[] Box = [(0, 0), (300, 0), (300, 172), (0, 172)];
    private static readonly (float X, float Y) Origin = (470, 62), Drone = (420, 40);
    private static readonly SootSpots Spots = SootFrame.Spots(Box, 0);

    private static FrameSquare[] All(SootFade fade) => Enumerable.Range(0, Spots.Count).Select(i => SootRun.At(Spots, i, fade, Origin, Drone)).ToArray();

    private static double Run(SootFade fade, bool open, double seconds)
    {
        double t = 0;
        for (; t < seconds - 1e-9; t += Frame) fade.Step(Frame, open);
        return t;
    }

    [Fact]
    public void OpeningTheNearCornerLeavesFirstTheLastLandsByTheFlyAndTheScreenComesAfter()
    {
        var fade = new SootFade();
        fade.Step(0.05, true);
        var early = All(fade);
        Assert.True(early[0].Out, "the near corner's square has left");
        Assert.DoesNotContain(early, s => s.Landed);
        Assert.Contains(early, s => !s.Out);
        Assert.Equal(0, fade.Glass);
        Run(fade, true, DroneInk.Fly - 0.05 + 1e-6);
        Assert.All(All(fade), s => Assert.True(s.Landed));
        Assert.Equal(0, fade.Glass);
        Run(fade, true, DroneInk.InsideFrom - DroneInk.Fly + (DroneInk.InsideTo - DroneInk.InsideFrom) / 2);
        Assert.InRange(fade.Glass, 0.3f, 0.7f);
        Run(fade, true, 1);
        Assert.Equal(1, fade.Glass);
        Assert.Equal(1, fade.Contents);
        Assert.Equal(1, fade.Out);
    }

    [Fact]
    public void ClosingTheScreenGoesFirstThenTheSquaresTheFarthestFirst()
    {
        var fade = new SootFade();
        Run(fade, true, 2);
        Run(fade, false, DroneInk.Close / 2);
        Assert.InRange(fade.Glass, 0.2f, 0.8f);
        Assert.All(All(fade), s => Assert.True(s.Landed));
        Run(fade, false, DroneInk.Close / 2 + 0.05);
        Assert.Equal(0, fade.Glass);
        var leaving = All(fade);
        int far = Enumerable.Range(0, Spots.Count).MaxBy(Spots.Lag), near = 0;
        Assert.False(leaving[far].Landed, "the farthest left first");
        Assert.True(leaving[near].Landed, "the near corner's waits");
        Run(fade, false, DroneInk.Back);
        Assert.All(All(fade), s => Assert.False(s.Out));
        Assert.False(fade.Shown);
    }

    [Fact]
    public void ItTakesClicksAtOnceOpeningAndOnlyTillTheScreenGoesClosing()
    {
        var fade = new SootFade();
        Assert.False(fade.Clicks);
        fade.Step(Frame, true);
        Assert.True(fade.Clicks);
        Run(fade, true, 2);
        Run(fade, false, DroneInk.Close / 2);
        Assert.True(fade.Clicks);
        Run(fade, false, DroneInk.Close);
        Assert.False(fade.Clicks);
    }

    [Fact]
    public void AFlightRunsStraightFromTheDroneToItsPlaceThreadedFromTheDrone()
    {
        var fade = new SootFade();
        Run(fade, true, 0.2);
        var opening = All(fade);
        for (int i = 0; i < Spots.Count; i++)
        {
            var s = opening[i];
            if (!s.Out || s.Landed) continue;
            var (px, py) = Spots.Spot(i);
            (float X, float Y) place = (Origin.X + px, Origin.Y + py);
            // On the line from the drone to its place, heading for its place.
            float cross = (place.X - Drone.X) * (s.Y - Drone.Y) - (place.Y - Drone.Y) * (s.X - Drone.X);
            Assert.True(MathF.Abs(cross) < 1e-2f * MathF.Max(1, MathF.Abs(place.X - Drone.X) + MathF.Abs(place.Y - Drone.Y)), $"square {i} off its line");
            Assert.True((place.X - Drone.X) * s.Dx + (place.Y - Drone.Y) * s.Dy > 0);
            Assert.Equal(Drone, (s.FromX, s.FromY));
        }
        Run(fade, true, 2);
        Run(fade, false, DroneInk.Close + 0.3);
        foreach (var (s, i) in All(fade).Select((s, i) => (s, i)))
        {
            if (!s.Out || s.Landed) continue;
            var (px, py) = Spots.Spot(i);
            // Home it heads for the drone, threaded from its place.
            Assert.Equal((Origin.X + px, Origin.Y + py), (s.FromX, s.FromY));
            Assert.True((Drone.X - Origin.X - px) * s.Dx + (Drone.Y - Origin.Y - py) * s.Dy > 0, $"square {i} heads ({s.Dx}, {s.Dy})");
        }
    }

    [Fact]
    public void EveryFourthTrailsAThread() =>
        Assert.Equal(Enumerable.Range(0, 40).Where(i => i % DroneInk.ThreadEvery == 0), Enumerable.Range(0, 40).Where(SootRun.Threads));

    [Fact]
    public void ReopenedMidFlightEverySquareTurnsBackWhereItIs()
    {
        // The farthest any square moves in a frame of an opening, start to end.
        var opening = new SootFade();
        float stride = 0;
        for (var was = All(opening); opening.U < DroneInk.InsideTo;)
        {
            opening.Step(Frame, true);
            var now = All(opening);
            for (int i = 0; i < Spots.Count; i++)
                if (was[i].Out) stride = MathF.Max(stride, MathF.Abs(now[i].X - was[i].X) + MathF.Abs(now[i].Y - was[i].Y));
            was = now;
        }
        foreach (double shut in (double[])[0.1, 0.4, 0.7])
        {
            var fade = new SootFade();
            Run(fade, true, 0.5);
            Run(fade, false, shut);
            var before = All(fade);
            fade.Step(0, true);
            var after = All(fade);
            for (int i = 0; i < Spots.Count; i++)
            {
                Assert.Equal(before[i].X, after[i].X, 4);
                Assert.Equal(before[i].Y, after[i].Y, 4);
            }
            // A frame on, none has moved further than a frame of an opening moves one.
            fade.Step(Frame, true);
            var next = All(fade);
            for (int i = 0; i < Spots.Count; i++)
                if (after[i].Out)
                    Assert.True(MathF.Abs(next[i].X - after[i].X) + MathF.Abs(next[i].Y - after[i].Y) <= stride + 1e-3f, $"square {i} jumped at a reopen {shut} s in");
        }
    }

    [Fact]
    public void WithNoDroneToFlyFromTheSquaresAppearInPlace()
    {
        var fade = new SootFade();
        fade.Step(0.05, true);
        var square = SootRun.At(Spots, 0, fade, Origin, null);
        Assert.True(square.Landed);
        Assert.Equal((Origin.X + Spots.Spot(0).X, Origin.Y + Spots.Spot(0).Y), (square.X, square.Y));
    }
}
