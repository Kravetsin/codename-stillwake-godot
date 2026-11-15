using FsCheck;
using FsCheck.Xunit;
using Stillwake.Game.Features.Flora;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A felled tree falling over, and a runner shaking leaves off a bush (plant-stirs).</summary>
public class ToppleTests
{
    private const double Frame = 1 / 60.0;

    [Fact]
    public void ATreeFallsSlowlyThenFastStrikesOnceReboundsAndLiesStill()
    {
        var fall = new Topple(1, FloraLooks.FallPull);
        var angles = new List<float>();
        int strikes = 0, frames = 0;
        while (!fall.Lying && frames++ < 600)
        {
            if (fall.Advance(Frame)) strikes++;
            angles.Add(fall.Angle);
        }
        Assert.True(fall.Lying);
        Assert.Equal(1, strikes);
        Assert.Equal(Topple.Lie, fall.Angle);
        // Slowly at first: its first quarter second leans less than a tenth of the way.
        Assert.True(angles[15] < Topple.Lie / 10, $"{angles[15]} a quarter second in");
        // Under three seconds in all, and it rose off the ground once after striking it.
        Assert.True(frames < 180, $"{frames} frames");
        int struck = angles.IndexOf(Topple.Lie);
        Assert.Contains(angles.Skip(struck), a => a < Topple.Lie - 0.01f);
    }

    [Fact]
    public void ItsRotationIsClockwiseFallingRightAndAgainstFallingLeft()
    {
        var right = new Topple(1, FloraLooks.FallPull);
        var left = new Topple(-1, FloraLooks.FallPull);
        right.Advance(1);
        left.Advance(1);
        Assert.True(right.Rotation > 0);
        Assert.Equal(-right.Rotation, left.Rotation);
    }

    [Fact]
    public void ATreeFallsAwayFromTheNearestHeroNearIt()
    {
        Assert.Equal(-1, Topple.Away(100, [130, 60], 48));
        Assert.Equal(1, Topple.Away(100, [80, 140], 48));
        Assert.Equal(1, Topple.Away(100, [200], 48));
        Assert.Equal(1, Topple.Away(100, [], 48));
    }

    /// <summary>However long the frames, a falling tree never leans past where it lies, nor back past upright.</summary>
    [Property]
    public void NeverPastLyingNorBackPastUpright(PositiveInt frameMs, bool right)
    {
        var fall = new Topple(right ? 1 : -1, FloraLooks.FallPull);
        for (int i = 0; i < 400 && !fall.Lying; i++)
        {
            fall.Advance(Math.Min(frameMs.Get, 500) / 1000.0);
            Assert.InRange(fall.Angle, 0, Topple.Lie);
        }
    }

    [Fact]
    public void ABodySwaysAPlantItBrushesOnceAWhileAndAStillOneNever()
    {
        var brushes = new Brushes(FloraLooks.BrushSpeed, FloraLooks.BrushEvery);
        float walk = 115, still = FloraLooks.BrushSpeed - 1;
        Assert.False(brushes.Answers(1, still, 0));
        Assert.True(brushes.Answers(1, -walk, 0));
        Assert.False(brushes.Answers(1, walk, FloraLooks.BrushEvery - 0.01));
        Assert.True(brushes.Answers(2, walk, 0.1));
        Assert.True(brushes.Answers(1, walk, FloraLooks.BrushEvery));
        brushes.Forget(1);
        Assert.True(brushes.Answers(1, walk, FloraLooks.BrushEvery + 0.1));
    }
}
