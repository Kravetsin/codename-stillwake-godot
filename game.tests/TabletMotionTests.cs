using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The build tablet sliding up and down and its screen coming on (0073).</summary>
public class TabletMotionTests
{
    private const double Frame = 1.0 / 60;

    private static void Run(TabletMotion motion, bool open, double seconds)
    {
        for (double t = 0; t < seconds - 1e-9; t += Frame) motion.Step(open, Frame);
    }

    [Fact]
    public void SlidesUpEasingOutAndIsDockedInTheSlideTime()
    {
        var motion = new TabletMotion();
        Assert.False(motion.Shown);
        motion.Step(true, TabletMotion.SlideIn / 2);
        // Easing out: half the time is most of the way.
        Assert.Equal(0.875, motion.Up, 3);
        motion.Step(true, TabletMotion.SlideIn / 2);
        Assert.Equal(1, motion.Up);
        Assert.InRange(TabletMotion.SlideIn, 0.12, 0.14);
    }

    [Fact]
    public void SlidesDownEasingInStaysShownTillGoneAndIsFaster()
    {
        var motion = new TabletMotion();
        Run(motion, true, 1);
        motion.Step(false, TabletMotion.SlideOut / 2);
        // Easing in: half the time is the slow start.
        Assert.Equal(0.875, motion.Up, 3);
        Assert.True(motion.Shown);
        motion.Step(false, TabletMotion.SlideOut / 2);
        Assert.False(motion.Shown);
        Assert.True(TabletMotion.SlideOut < TabletMotion.SlideIn);
    }

    [Fact]
    public void TurnedBackHalfwayGoesOnFromWhereItIs()
    {
        var motion = new TabletMotion();
        Run(motion, true, 1);
        Run(motion, false, 0.05);
        double at = motion.Up;
        motion.Step(true, 0.001);
        Assert.InRange(motion.Up, at, at + 0.01);
        Run(motion, true, TabletMotion.SlideIn);
        Assert.Equal(1, motion.Up);
    }

    [Fact]
    public void ItsScreenFadesInGentlyWhenTakenOutCold()
    {
        var motion = new TabletMotion();
        motion.Step(true, Frame);
        Assert.Equal(0, motion.Power);
        double last = 0, steps = 0;
        for (double t = Frame; t < TabletMotion.PowerDelay + TabletMotion.PowerFade + 0.05; t += Frame)
        {
            motion.Step(true, Frame);
            // Never brighter than lit, and no frame darker than the one before: one rise, no flicker.
            Assert.InRange(motion.Power, last, 1);
            if (motion.Power > last) steps++;
            last = motion.Power;
        }
        Assert.Equal(1, motion.Power);
        // Spread over frames, not a flash: at 60 a second, several.
        Assert.True(steps >= 4);
        Assert.InRange(TabletMotion.PowerFade, 0.06, 0.1);
    }

    [Fact]
    public void TakenOutSoonAfterItIsPutAwayItsScreenIsOnAtOnce()
    {
        var motion = new TabletMotion();
        Run(motion, true, 1);
        Run(motion, false, TabletMotion.Warm - 0.2);
        motion.Step(true, Frame);
        Assert.Equal(1, motion.Power);
    }

    [Fact]
    public void TakenOutLongAfterItIsPutAwayItsScreenComesOnAgain()
    {
        var motion = new TabletMotion();
        Run(motion, true, 1);
        Run(motion, false, TabletMotion.Warm + 0.2);
        motion.Step(true, Frame);
        Assert.Equal(0, motion.Power);
    }

    [Fact]
    public void PutAwayItsScreenStaysLitWhileItSlidesDown()
    {
        var motion = new TabletMotion();
        Run(motion, true, 1);
        while (motion.Shown)
        {
            motion.Step(false, Frame);
            Assert.Equal(1, motion.Power);
        }
    }
}
