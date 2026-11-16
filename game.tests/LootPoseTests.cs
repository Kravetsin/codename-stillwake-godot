using Stillwake.Game.Features.Items;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>How a stack's picture lies on the floor (2026-11-16-loot-pieces): on a flat side, the one nearest how it landed.</summary>
public class LootPoseTests
{
    /// <summary>A bar ten pixels wide and four high about its middle, with a point inside it.</summary>
    private static readonly (float X, float Y)[] Bar = [(-5, -2), (5, -2), (5, 2), (-5, 2), (0, 0)];

    [Fact]
    public void TheHullDropsWhatLiesInside()
    {
        var hull = LootPose.Hull(Bar);
        Assert.Equal(4, hull.Count);
        Assert.DoesNotContain((0f, 0f), hull);
    }

    [Theory]
    [InlineData(0.3f, 0f)]
    [InlineData(1.4f, 0f)]
    [InlineData(1.8f, 3.14159f)]
    [InlineData(-0.5f, 0f)]
    public void ABarLiesOnALongSideTheOneNearestHowItLanded(float landed, float lies)
    {
        float angle = LootPose.RestAngle(LootPose.Hull(Bar), landed);
        // Never on its short end, however it landed.
        Assert.Equal(lies, LootPose.Wrap(angle), 2);
        Assert.Equal(2, LootPose.Lowest(LootPose.Hull(Bar), angle), 2);
    }

    [Fact]
    public void ItTurnsByTheLeastToGetThere()
    {
        // Landed nearly a whole turn round, it lies flat without spinning back the other way.
        float angle = LootPose.RestAngle(LootPose.Hull(Bar), 6.1f);
        Assert.Equal(2 * MathF.PI, angle, 2);
    }

    [Fact]
    public void ATriangleLiesOnASideNeverOnAPoint()
    {
        var hull = LootPose.Hull([(-6, 3), (6, 3), (0, -6)]);
        for (float landed = -3; landed < 3; landed += 0.25f)
        {
            float angle = LootPose.RestAngle(hull, landed);
            float lowest = LootPose.Lowest(hull, angle);
            // Two corners touch the floor: a side lies flat.
            int touching = hull.Count(p => MathF.Abs(p.X * MathF.Sin(angle) + p.Y * MathF.Cos(angle) - lowest) < 0.01f);
            Assert.Equal(2, touching);
        }
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(9, 2)]
    [InlineData(10, 3)]
    [InlineData(999, 3)]
    public void AStackShowsOneTwoOrThreeObjects(int count, int objects) => Assert.Equal(objects, LootPose.Objects(count));
}
