using Stillwake.Game.Features.Creatures;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The Istukan, the warden's look (its page's candidate 4): its motion read off the sim, and why it is gone.</summary>
public class IstukanTests
{
    private const double WakeS = 0.5, Frame = 0.05;

    private static readonly IstukanMotion.Seen Asleep = new(true, false, 0, false, false, 0, 0, true, null);
    private static readonly IstukanMotion.Seen Up = Asleep with { Asleep = false, Hero = (40, 0) };

    private static IstukanMotion Motion() => new(() => 0.5, WakeS);

    [Fact]
    public void AsleepItIsDarkThenWokenTealThenYellowThenRedInItsCrouch()
    {
        var m = Motion();
        m.Step(Asleep, Frame);
        Assert.Null(m.Pose.Lamp);
        Assert.Equal(1, m.Pose.Fold);
        var woke = m.Step(Up, Frame);
        Assert.True(woke.HasFlag(IstukanMotion.Moment.Wake));
        Assert.Equal(IstukanMotion.Teal, m.Pose.Lamp);
        var moments = IstukanMotion.Moment.None;
        for (int i = 0; i < 12; i++) moments |= m.Step(Up, Frame);
        Assert.True(moments.HasFlag(IstukanMotion.Moment.Alert));
        Assert.Equal(0, m.Pose.Fold);
        for (int i = 0; i < 5; i++) m.Step(Up, Frame);
        Assert.Equal(IstukanMotion.Yellow, m.Pose.Lamp);
        var winding = m.Step(Up with { WindingUp = true, WindUpS = 0.05 }, Frame);
        Assert.True(winding.HasFlag(IstukanMotion.Moment.WindUp));
        Assert.Equal(IstukanMotion.Red, m.Pose.Lamp);
    }

    [Fact]
    public void ItsMomentsComeOnceEach()
    {
        var m = Motion();
        m.Step(Up, Frame);
        var flying = Up with { Pouncing = true, Vx = 288, Still = false };
        Assert.True(m.Step(flying, Frame).HasFlag(IstukanMotion.Moment.Pounce));
        Assert.False(m.Step(flying, Frame).HasFlag(IstukanMotion.Moment.Pounce));
        var stuck = Up with { Stuck = true };
        Assert.True(m.Step(stuck, Frame).HasFlag(IstukanMotion.Moment.Land));
        Assert.False(m.Step(stuck, Frame).HasFlag(IstukanMotion.Moment.Land));
    }

    [Fact]
    public void ItRestsOnlyOnceItHasStoppedFallingAfterAWall()
    {
        var m = Motion();
        m.Step(Up with { Pouncing = true, Vx = 288, Still = false }, Frame);
        var falling = Up with { Stuck = true, Still = false, Vy = 100 };
        Assert.False(m.Step(falling, Frame).HasFlag(IstukanMotion.Moment.Land));
        Assert.True(m.Step(falling with { Still = true, Vy = 0 }, Frame).HasFlag(IstukanMotion.Moment.Land));
    }

    [Fact]
    public void FlyingItFacesItsWayAndLodgedItsFlownPoseIsDropped()
    {
        var m = Motion();
        m.Step(Up, Frame);
        var flying = Up with { Pouncing = true, Vx = -288, Still = false };
        for (int i = 0; i < 3; i++) m.Step(flying, Frame);
        Assert.Equal(-1, m.Face);
        Assert.Equal(1, m.Pose.Fly);
        for (int i = 0; i < 3; i++) m.Step(Up with { Stuck = true }, Frame);
        Assert.Equal(1, m.Pose.Stuck);
        Assert.Equal(0, m.Pose.Fly);
        Assert.Equal(0, m.Pose.Crouch);
    }

    [Fact]
    public void FallingItsLampGoesOutAndItEndsDimAsAHeap()
    {
        var m = Motion();
        m.Step(Up, Frame);
        m.Fall();
        for (double t = 0; t < IstukanMotion.FallS + 0.1; t += Frame) m.Step(default, Frame);
        Assert.Equal(1, m.Pose.Dead);
        Assert.Null(m.Pose.Lamp);
        Assert.Equal(0, m.Pose.Seam);
        Assert.Equal(0.22, m.Pose.Dim, 3);
    }

    [Fact]
    public void AHeldPoseIsTheSamePoseSoItIsNotDrawnAgain()
    {
        var m = Motion();
        m.Step(Asleep, Frame);
        var was = m.Pose;
        m.Step(Asleep, Frame);
        Assert.Equal(was, m.Pose);
    }

    [Fact]
    public void APoseHeldAfterAFightIsDrawnOnce()
    {
        var m = Motion();
        m.Step(Up, Frame);
        for (int i = 0; i < 16; i++) m.Step(Up with { WindingUp = true, WindUpS = i * Frame }, Frame);
        for (int i = 0; i < 6; i++) m.Step(Up with { Pouncing = true, Vx = 288, Still = false }, Frame);
        for (int i = 0; i < 20; i++) m.Step(Up with { Stuck = true }, Frame);
        for (int i = 0; i < 40; i++) m.Step(Up, Frame);
        var was = m.Pose;
        m.Step(Up, Frame);
        Assert.Equal(was, m.Pose);
    }

    [Theory]
    [InlineData(true, true, true, IstukanFate.Fate.ShutDown)]
    [InlineData(true, false, true, IstukanFate.Fate.ShutDown)]
    [InlineData(false, false, false, IstukanFate.Fate.ShutDown)]
    [InlineData(false, false, true, IstukanFate.Fate.Killed)]
    [InlineData(false, true, true, IstukanFate.Fate.Calmed)]
    public void WhyItIsGoneIsReadFromWhatItLeaves(bool taken, bool post, bool wall, IstukanFate.Fate fate) =>
        Assert.Equal(fate, IstukanFate.Of(taken, post, wall));
}
