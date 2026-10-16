using System;
using Stillwake.Game.Features.Building;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A plan's sparks fly from its side facing the hero at work on it, toward that hero.</summary>
public class SparksTests
{
    private static readonly (double X, double Y, double W, double H) Wall = (100, 100, 16, 16);

    [Fact]
    public void FlyFromTheSideFacingTheNearestHeroTowardIt()
    {
        var (x, y, heading) = Tools.SparksOf(Wall, [(160, 300), (60, 108)], reach: 128);
        Assert.Equal((101.0, 108.0), (x, y));
        Assert.Equal(Math.PI, Math.Abs(heading), 3);
    }

    [Fact]
    public void RiseFromTheMiddleWithNoHeroNearOrOneInside()
    {
        Assert.Equal((108.0, 108.0, -Math.PI / 2), Tools.SparksOf(Wall, [(400, 400)], reach: 128));
        Assert.Equal((108.0, 108.0, -Math.PI / 2), Tools.SparksOf(Wall, [(105, 110)], reach: 128));
        Assert.Equal((108.0, 108.0, -Math.PI / 2), Tools.SparksOf(Wall, [], reach: 128));
    }
}
