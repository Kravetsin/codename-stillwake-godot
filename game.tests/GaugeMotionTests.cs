using Stillwake.Game.Shared;
using Xunit;
using Look = Stillwake.Game.WristLook;

namespace Stillwake.Game.Tests;

/// <summary>A wrist gauge between its four reads a second (0081): every loss white, held, then drained; a hit flashing; a refill easing in.</summary>
public class GaugeMotionTests
{
    private const double Frame = 1.0 / 60, Read = 0.25;

    /// <summary>Runs <paramref name="seconds"/> of frames, checking each with <paramref name="check"/>, if given, against the frame before.</summary>
    private static void Run(GaugeMotion motion, double seconds, Action<(double Fill, double Trail), GaugeMotion>? check = null)
    {
        for (double t = 0; t < seconds - 1e-9; t += Frame)
        {
            var before = (motion.Fill, motion.Trail);
            motion.Step(Frame);
            check?.Invoke(before, motion);
        }
    }

    private static GaugeMotion At(double share)
    {
        var motion = new GaugeMotion();
        motion.Read(share);
        return motion;
    }

    [Fact]
    public void ShowsTheFirstReadAtOnce()
    {
        var motion = At(0.7);
        Assert.Equal((0.7, 0.7, 0.0, 0.0), (motion.Fill, motion.Trail, motion.Flash, motion.Gain));
    }

    [Fact]
    public void ABigDropFallsAtOnceThenItsLostPartFlashesHoldsAndDrains()
    {
        var motion = At(0.8);
        motion.Read(0.5);
        Assert.Equal((0.5, 0.8, 1.0), (motion.Fill, motion.Trail, motion.Flash));
        // Held whole while it flashes.
        Run(motion, 0.1);
        Assert.Equal(0.8, motion.Trail);
        Assert.InRange(motion.Flash, 0.6, 0.7);
        // Whole for its hold, then it drains, never back up, and is gone within a second more; the flash long over.
        Run(motion, Look.TrailHoldS - 0.1 - Frame);
        Assert.Equal(0.8, motion.Trail);
        Run(motion, 1.0, (before, now) => Assert.True(now.Trail <= before.Trail));
        Assert.Equal((0.5, 0.5, 0.0), (motion.Fill, motion.Trail, motion.Flash));
    }

    [Fact]
    public void ADropOfTheThresholdIsAHitAndJustUnderIsNot()
    {
        var hit = At(0.6);
        hit.Read(0.6 - Look.BigDrop - 1e-9);
        Assert.Equal(1, hit.Flash);
        var small = At(0.6);
        small.Read(0.6 - Look.BigDrop + 0.001);
        Assert.Equal((0.6 - Look.BigDrop + 0.001, 0.6, 0.0), (small.Fill, small.Trail, small.Flash));
    }

    [Fact]
    public void ARunningBreathIsNoHit()
    {
        // Out of room air a running hero breathes 2 of the suit's 45 units a breath (Content.Crew.Exerted).
        var motion = At(1);
        motion.Read(1 - 2.0 / Content.Items.Get(Content.Helmet.Basic).Tank);
        Assert.Equal(0, motion.Flash);
    }

    [Fact]
    public void EveryBreathLostStaysWhiteForItsHoldThenDrainsWithoutAFlash()
    {
        // A breath a second out of air: the read falls by a unit every fourth read.
        var motion = At(1);
        double unit = 1.0 / Content.Items.Get(Content.Helmet.Basic).Tank, read = 1;
        for (int r = 1; r <= 40; r++)
        {
            bool breath = r % 4 == 0;
            double edge = motion.Trail;
            if (breath) read -= unit;
            motion.Read(read);
            Assert.Equal(read, motion.Fill, 9);
            double ran = 0;
            Run(motion, Read, (before, now) =>
            {
                ran += Frame;
                Assert.True(now.Trail <= before.Trail && now.Trail >= now.Fill);
                // Held whole as long as its hold, then gone well before the next breath.
                if (breath && ran < Look.TrailHoldS - Frame) Assert.Equal(edge, now.Trail);
            });
            Assert.Equal(0, motion.Flash);
            if (r % 4 == 3) Assert.Equal(read, motion.Trail, 9);
        }
    }

    [Fact]
    public void ASteadyDrainTrailsAWhiteBandAsWideAsItsHold()
    {
        // A battery spent every read, as the energy tab reads it.
        var motion = At(1);
        double read = 1, step = 0.01;
        for (int r = 0; r < 20; r++)
        {
            motion.Read(read -= step);
            Run(motion, Read);
        }
        double band = motion.Trail - motion.Fill;
        Assert.InRange(band, step * (Look.TrailHoldS / Read - 1), step * (Look.TrailHoldS / Read + 2));
    }

    [Fact]
    public void ASmallRiseSlidesAtAnEvenPaceOverTheSlide()
    {
        var motion = At(0.48);
        motion.Read(0.5);
        Run(motion, Look.SlideS / 2);
        Assert.Equal(0.49, motion.Fill, 3);
        Run(motion, Look.SlideS / 2 + Frame);
        Assert.Equal(0.5, motion.Fill, 9);
    }

    [Fact]
    public void ADropWhileARiseIsUnderWayStopsItLowerAndLosesNothingShown()
    {
        var motion = At(0.2);
        motion.Read(0.6);
        Run(motion, 0.1);
        double shown = motion.Fill;
        motion.Read(0.45);
        Assert.Equal((shown, shown), (motion.Fill, motion.Trail));
        Run(motion, 2);
        Assert.Equal((0.45, 0.45), (motion.Fill, motion.Trail));
    }

    [Fact]
    public void ARefillRisesSmoothlyEasingInWithItsLightAndNoTrail()
    {
        // A battery put on the belt: a third of full at once.
        var motion = At(0.2);
        motion.Read(0.2 + 1.0 / 3);
        Assert.Equal(0.2, motion.Fill);
        double target = 0.2 + 1.0 / 3, firstStep = 0;
        Run(motion, 0.1, (before, now) =>
        {
            Assert.True(now.Fill > before.Fill && now.Fill < target);
            if (firstStep == 0) firstStep = now.Fill - before.Fill;
            Assert.Equal(now.Fill, now.Trail);
        });
        Assert.InRange(firstStep, 0.001, 0.05);
        Assert.Equal(1, motion.Gain);
        // Full within a second; the light gone a moment after it stopped rising.
        Run(motion, 1);
        Assert.Equal(target, motion.Fill, 9);
        Run(motion, Look.GainS + Frame);
        Assert.Equal(0, motion.Gain);
    }

    [Fact]
    public void AHitWhileTheLastOneStillDrainsKeepsTheFurthestEdge()
    {
        var motion = At(0.9);
        motion.Read(0.6);
        Run(motion, 0.05);
        motion.Read(0.3);
        Assert.Equal((0.3, 0.9, 1.0), (motion.Fill, motion.Trail, motion.Flash));
        Run(motion, 2);
        Assert.Equal(0.3, motion.Trail);
    }

    [Fact]
    public void AShareShownForANewCapacityLosesNothing()
    {
        // The helmet put on: 20 of the lungs' 20 become 20 of its 45.
        var motion = At(1);
        motion.Read(0.7);
        motion.Show(20.0 / 45);
        Assert.Equal((20.0 / 45, 20.0 / 45, 0.0), (motion.Fill, motion.Trail, motion.Flash));
        Run(motion, Look.TrailHoldS);
        Assert.Equal(20.0 / 45, motion.Trail);
    }

    [Fact]
    public void ARefillOverTheLostPartEndsIt()
    {
        var motion = At(0.8);
        motion.Read(0.5);
        motion.Read(1);
        Run(motion, 2);
        Assert.Equal((1.0, 1.0), (motion.Fill, motion.Trail));
    }
}
