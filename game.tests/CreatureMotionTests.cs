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
        // It strides by the ground covered, its strides longer the faster: a quicker cadence fleeing, and a bound, but not a blur.
        var walk = Run(new CreatureMotion(), 1000, dx: 0.5);
        var flee = Run(new CreatureMotion(), 1000, dx: 4);
        Assert.True(flee.Phase > walk.Phase && flee.Phase < walk.Phase * 4, $"{flee.Phase:F1} strides fleeing, {walk.Phase:F1} walking");
        Assert.Equal(CreatureMotion.LongestStride, CreatureMotion.StrideAt(300));
        Assert.Equal(0, Run(new CreatureMotion(), 500, dx: 0.5).Bound, 3);
        Assert.True(Enumerable.Range(0, 30).Select(_ => Run(flee, Frame, dx: 4).Bound).Max() > 2);
    }

    [Fact]
    public void AJumpCrouchesThenSpringsTallWithItsEarsBackAndAHardLandingSquashesHarder()
    {
        var m = new CreatureMotion();
        m.Jump();
        m.Step(Frame, 0, false, false, null);
        Assert.True(m.Scale(false).Y < 0.95, "no crouch");
        Assert.Equal(1, m.Ears(false));
        Run(m, CreatureMotion.CrouchMs);
        Assert.True(m.Scale(false).Y > 1.1, "no spring");
        double Squash(double hardness)
        {
            var l = new CreatureMotion();
            l.Land(hardness);
            l.Step(Frame, 0, true, true, null);
            return l.Scale(false).Y;
        }
        Assert.True(Squash(1) < Squash(0) - 0.1, $"{Squash(1):F2} hard against {Squash(0):F2} soft");
    }
}
