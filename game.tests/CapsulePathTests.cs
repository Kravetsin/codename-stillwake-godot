using Stillwake.Game.Features.Research;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Where a trade station's capsule is drawn (2026-10-30-trade-capsule).</summary>
public class CapsulePathTests
{
    private const int Flight = 600;

    [Fact]
    public void StartsAtItsStationFullSize()
    {
        var pose = CapsulePath.At(Flight, Flight)!.Value;
        Assert.Equal((0.0, 0.0, CapsulePath.Near, 1.0), (pose.X, pose.Y, pose.Scale, pose.Alpha));
    }

    [Fact]
    public void GoesUpAndAsternShrinkingThenIsOutOfSight()
    {
        var early = CapsulePath.At(Flight - 20, Flight)!.Value;
        var late = CapsulePath.At(Flight - 140, Flight)!.Value;
        Assert.True(late.Y < early.Y && early.Y < 0);
        Assert.True(late.X < early.X && early.X < 0);
        Assert.True(late.Scale < early.Scale);
        Assert.True(late.Alpha < 1);
        Assert.Null(CapsulePath.At(Flight / 2, Flight));
    }

    [Fact]
    public void ComesBackTheSameWayReversed()
    {
        Assert.Equal(CapsulePath.At(Flight - 50, Flight), CapsulePath.At(50, Flight));
        Assert.Equal(0, CapsulePath.At(0, Flight)!.Value.Y);
    }
}
