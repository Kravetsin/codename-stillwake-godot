using Stillwake.Game.Features.Creatures;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The Istukan, the warden's look (its page's candidate 4): its sprite in every key pose, its motion read off the sim, and why it is gone.</summary>
public class IstukanTests
{
    private const double WakeS = 0.5, Frame = 0.05;

    private static readonly IstukanArt.Pose Standing = new(0, 0, 0, 0, 0, IstukanMotion.Yellow, 1, 1);

    private static readonly IstukanMotion.Seen Asleep = new(true, false, 0, false, false, 0, 0, true, null);
    private static readonly IstukanMotion.Seen Up = Asleep with { Asleep = false, Hero = (40, 0) };

    private static List<(int X, int Y)> Opaque(byte[] rgba)
    {
        var found = new List<(int, int)>();
        for (int i = 0; i < IstukanArt.Width * IstukanArt.Height; i++)
            if (rgba[i * 4 + 3] > 0) found.Add((i % IstukanArt.Width - IstukanArt.OriginX, i / IstukanArt.Width - IstukanArt.OriginY));
        return found;
    }

    private static IstukanMotion Motion() => new(() => 0.5, WakeS);

    public static TheoryData<string, IstukanArt.Pose> Poses() => new()
    {
        { "stand", Standing },
        { "sleep", Standing with { Fold = 1, Lamp = null, Seam = 0.08 } },
        { "crouch", Standing with { Crouch = 1, Lamp = IstukanMotion.Red } },
        { "fly", Standing with { Crouch = 1, Fly = 1, Lamp = IstukanMotion.Red, OnFloor = false } },
        { "stuck", Standing with { Stuck = 1, Lamp = IstukanMotion.Red } },
        { "dead", Standing with { Stuck = 1, Dead = 1, Lamp = null, Seam = 0, Dim = 0.22 } },
    };

    [Theory]
    [MemberData(nameof(Poses))]
    public void EveryPoseFitsItsBufferUntouchedAtTheEdges(string _, IstukanArt.Pose pose)
    {
        var art = new IstukanArt();
        art.Draw(pose);
        var drawn = Opaque(art.Albedo);
        Assert.NotEmpty(drawn);
        Assert.True(drawn.Min(p => p.X) > -IstukanArt.OriginX && drawn.Max(p => p.X) < IstukanArt.Width - IstukanArt.OriginX - 1, "cut off at its sides");
        Assert.True(drawn.Min(p => p.Y) > -IstukanArt.OriginY && drawn.Max(p => p.Y) < IstukanArt.Height - IstukanArt.OriginY - 1, "cut off above or below");
    }

    [Fact]
    public void StandingItIsAboutAsTallAsItsFiveCellsOnItsFeet()
    {
        var art = new IstukanArt();
        art.Draw(Standing);
        var drawn = Opaque(art.Albedo);
        Assert.InRange(drawn.Min(p => p.Y), -40, -32);
        Assert.Equal(-1, drawn.Max(p => p.Y));
    }

    [Fact]
    public void AsleepItIsANarrowerColumnThanStandingItsLampDark()
    {
        var art = new IstukanArt();
        art.Draw(Standing);
        var wide = Opaque(art.Albedo);
        art.Draw(Standing with { Fold = 1, Lamp = null, Seam = 0.08 });
        var column = Opaque(art.Albedo);
        Assert.True(column.Max(p => p.X) - column.Min(p => p.X) < wide.Max(p => p.X) - wide.Min(p => p.X), "it does not fold in");
        // Only the faint core in its chest, the page's, its lamp and seams dark.
        Assert.Single(Opaque(art.Emission));
    }

    [Fact]
    public void ItsLampGlowsItsColourAndItsSilhouetteLeavesItsOutlineOut()
    {
        var art = new IstukanArt();
        var at = art.Draw(Standing with { Lamp = IstukanMotion.Red, Seam = 0 });
        Assert.All(Opaque(art.Emission), p => Assert.True(Math.Abs(p.X - at.Eye.X) <= 5 && Math.Abs(p.Y - at.Eye.Y) <= 2 || (p.X, p.Y) == ((int)Math.Floor(at.Core.X), (int)Math.Floor(at.Core.Y)), $"{p} glows away from its lamp"));
        Assert.NotEmpty(Opaque(art.Emission));
        Assert.True(Opaque(art.Silhouette).Count < Opaque(art.Albedo).Count);
    }

    [Fact]
    public void BelowTheFloorIsHiddenOnlyWhileItStandsOnOne()
    {
        var art = new IstukanArt();
        var lying = Standing with { Stuck = 1, Dead = 1, Lamp = null, Seam = 0 };
        art.Draw(lying);
        Assert.True(Opaque(art.Albedo).Max(p => p.Y) < 0);
    }

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
