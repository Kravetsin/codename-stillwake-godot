using Stillwake.Game.Features.Fauna;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>How a creature's body reacts to a blow and moves (0102): flash, squash, flinch, stop, wide eye, facing, stride.</summary>
public class CreatureMotionTests
{
    private const double Frame = 1000.0 / 60;

    private static CreatureMotion Run(CreatureMotion m, double ms, double dx = 0, int? watch = null)
    {
        for (double t = 0; t < ms; t += Frame) m.Step(Frame, dx, grounded: true, landed: false, watch);
        return m;
    }

    [Fact]
    public void ABlowFlashesItWhiteSquashesItFlatAndShakesItThenAllSettles()
    {
        var m = new CreatureMotion();
        Assert.Equal(0, m.Flash);
        m.Hit(away: 1, share: 1);
        Assert.Equal(1, m.Flash, 3);
        Assert.True(m.Shaking);
        var (x, y) = m.Scale(fleeing: false);
        Assert.True(x > 1.15 && y < 0.85, $"squashed to ({x:F2}, {y:F2})");
        Assert.True(m.Eye > 1.3, "its eye goes wide");
        Run(m, CreatureMotion.ShakeMs + Frame);
        Assert.False(m.Shaking);
        Run(m, CreatureMotion.FlashMs);
        Assert.Equal(0, m.Flash);
        Run(m, CreatureMotion.StartleMs);
        var (sx, sy) = m.Scale(fleeing: false);
        Assert.InRange(sx, 0.97, 1.03);
        Assert.InRange(sy, 0.97, 1.03);
        Assert.Equal(0, m.Tilt(fleeing: false), 3);
    }

    [Fact]
    public void ASquashSpringsBackPastRoundBeforeItSettles()
    {
        var m = new CreatureMotion();
        m.Hit(away: -1, share: 1);
        double tallest = 0;
        for (double t = 0; t < CreatureMotion.SquashMs; t += Frame)
        {
            m.Step(Frame, 0, true, false, null);
            tallest = System.Math.Max(tallest, m.Scale(false).Y);
        }
        Assert.True(tallest > 1.02, $"never past round: {tallest:F3}");
    }

    [Fact]
    public void ADrillsShareOfABlowFlashesLessThanTheAxesWhole()
    {
        var axe = new CreatureMotion();
        var drill = new CreatureMotion();
        axe.Hit(1, 1);
        drill.Hit(1, 0.2);
        Assert.True(drill.Flash < axe.Flash && drill.Flash > 0.1);
    }

    [Fact]
    public void ItFlinchesTheWayTheBlowKnocksItAndFacesTheWayItGoesOrAHeroItWatches()
    {
        var m = new CreatureMotion();
        m.Hit(away: 1, share: 1);
        m.Step(Frame, 0, true, false, null);
        Assert.True(m.Tilt(false) > 0, "thrown right, it tilts clockwise");
        Run(m, 200, dx: -2);
        Assert.Equal(-1, m.Facing);
        Run(new CreatureMotion(), 10);
        var still = Run(new CreatureMotion(), 50, watch: -1);
        Assert.Equal(-1, still.Facing);
        // It strides as it walks, by the ground covered: faster fleeing, as it covers more.
        var walk = Run(new CreatureMotion(), 1000, dx: 1);
        var flee = Run(new CreatureMotion(), 1000, dx: 3);
        Assert.True(flee.Phase > walk.Phase * 2.5);
    }
}
