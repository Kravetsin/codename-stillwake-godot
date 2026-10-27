using Stillwake.Game.Shared;
using Xunit;
using Look = Stillwake.Game.WristLook;

namespace Stillwake.Game.Tests;

/// <summary>A wrist gauge between its four reads a second (0081): smooth, a hit flashing then draining, a refill easing in.</summary>
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
        // Then it drains, never back up, and is gone within a second; the flash long over.
        Run(motion, 1.0, (before, now) => Assert.True(now.Trail <= before.Trail));
        Assert.Equal((0.5, 0.5, 0.0), (motion.Fill, motion.Trail, motion.Flash));
    }

    [Fact]
    public void ADropOfTheThresholdIsAHitAndJustUnderIsNot()
    {
        var hit = At(0.6);
        hit.Read(0.6 - Look.BigDrop - 1e-9);
        Assert.Equal(1, hit.Flash);
        var slide = At(0.6);
        slide.Read(0.6 - Look.BigDrop + 0.001);
        Assert.Equal((0.6, 0.0), (slide.Fill, slide.Flash));
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
    public void ASlowDrainMovesSmoothlyAndLeavesNoTrail()
    {
        // A breath a second out of air: the read falls by a unit every fourth read.
        var motion = At(1);
        double unit = 1.0 / Content.Items.Get(Content.Helmet.Basic).Tank, read = 1, largest = 0;
        for (int r = 1; r <= 40; r++)
        {
            if (r % 4 == 0) read -= unit;
            motion.Read(read);
            Run(motion, Read, (before, now) =>
            {
                Assert.Equal(now.Fill, now.Trail);
                largest = Math.Max(largest, before.Fill - now.Fill);
            });
            Assert.Equal(0, motion.Flash);
        }
        // Never a step: no frame moves it more than a breath spread over its slide.
        Assert.InRange(largest, 1e-6, unit / Look.SlideS * Frame * 1.01);
        Run(motion, Look.SlideS);
        Assert.Equal(read, motion.Fill, 9);
    }

    [Fact]
    public void ASmallChangeSlidesAtAnEvenPaceOverTheSlide()
    {
        var motion = At(0.5);
        motion.Read(0.48);
        Run(motion, Look.SlideS / 2);
        Assert.Equal(0.49, motion.Fill, 3);
        Run(motion, Look.SlideS / 2 + Frame);
        Assert.Equal(0.48, motion.Fill, 9);
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
    public void ARefillOverTheLostPartEndsIt()
    {
        var motion = At(0.8);
        motion.Read(0.5);
        motion.Read(1);
        Run(motion, 2);
        Assert.Equal((1.0, 1.0), (motion.Fill, motion.Trail));
    }
}
