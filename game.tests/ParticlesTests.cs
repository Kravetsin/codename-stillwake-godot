using Stillwake.Game.Render;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>render/particles.test.ts, ported.</summary>
public class ParticlesTests
{
    /// <summary>Halfway through every range: the same particle each time.</summary>
    private static Particles Middle(int limit) => new(limit, () => 0.5);

    [Fact]
    public void FlyFromWhereTheyAreEmittedAlongTheirHeadingAndGoAtTheEndOfTheirLife()
    {
        var p = Middle(10);
        p.Emit(Effect.Air, 5, 5, 3, 0, inSpace: true);
        Assert.Equal(3, p.Live.Count);
        p.Step(100);
        Assert.True(p.Live[0].X > 5);
        Assert.Equal(5, p.Live[0].Y, 2);
        // Air lives 500 to 1000 ms: 750 at the middle.
        p.Step(649);
        Assert.Equal(3, p.Live.Count);
        p.Step(2);
        Assert.Empty(p.Live);
    }

    [Fact]
    public void NeverFlyMoreThanTheLimitAtOnce()
    {
        var p = Middle(5);
        p.Emit(Effect.Dust, 0, 0, 4);
        p.Emit(Effect.Dust, 0, 0, 4);
        Assert.Equal(5, p.Live.Count);
    }

    [Fact]
    public void FallAboardAndDriftStraightInSpace()
    {
        var p = Middle(10);
        var spark = new Effect(0xfff3c4, 1, (1, 1), (550, 550), (75, 75), 0, 260, 1);
        p.Emit(spark, 0, 0, 1, 0);
        p.Emit(spark, 0, 0, 1, 0, inSpace: true);
        p.Step(200);
        Assert.True(p.Live[0].Y > 1);
        Assert.Equal(0, p.Live[1].Y, 2);
    }

    [Fact]
    public void LetEmbersRiseAsGravityBelowZero()
    {
        var p = Middle(10);
        p.Emit(Effect.Dust with { Gravity = -8, Drag = 0.4 }, 0, 0, 1, 0);
        p.Step(500);
        Assert.True(p.Live[0].Vy < 0);
    }

    [Fact]
    public void FadeOutOverTheirLife()
    {
        var p = Middle(10);
        p.Emit(Effect.Dust, 0, 0, 1);
        double fresh = p.Live[0].Alpha;
        p.Step(400);
        double late = p.Live[0].Alpha;
        Assert.Equal(Effect.Dust.Alpha, fresh, 2);
        Assert.True(late < fresh / 2);
    }

    [Fact]
    public void LoseSpeedToDrag()
    {
        var slow = Effect.Air with { Drag = 2 };
        var p = Middle(10);
        p.Emit(slow, 0, 0, 1, 0, inSpace: true);
        double first = p.Live[0].Vx;
        p.Step(100);
        Assert.Equal(first * 0.8, p.Live[0].Vx, 2);
    }
}
