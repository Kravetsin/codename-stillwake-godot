using Stillwake.Game.Features.Building;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The build glow's quads too thin to draw: Godot cannot triangulate a quad whose corners meet.</summary>
public class SliverTests
{
    [Fact]
    public void ABandClippedToAHairFarOutCollapsesToALineAndIsNotDrawn()
    {
        // A sweep's band clipped to the plan's edge leaves a positive width that the far coordinate swallows.
        float x = 4096f, width = 1e-4f;
        Assert.True(width > 0);
        Assert.Equal(x, x + width);
        Assert.True(Sliver.Is(x, 100, x + width, 140));
    }

    [Fact]
    public void AQuadAHundredthOfAPixelAcrossOrMoreIsDrawn()
    {
        Assert.False(Sliver.Is(10, 20, 10 + Sliver.Least, 20 + Sliver.Least));
        Assert.True(Sliver.Is(10, 20, 30, 20));
        Assert.True(Sliver.Is(10, 20, 10, 40));
    }
}
