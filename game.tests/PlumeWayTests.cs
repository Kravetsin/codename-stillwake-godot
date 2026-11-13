using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

public class PlumeWayTests
{
    [Fact]
    public void BlowsAPlumeOppositeTheThrust()
    {
        var (x, y) = PlumeWay.Of(3, 4);
        Assert.Equal(-0.6f, x, 5);
        Assert.Equal(-0.8f, y, 5);
    }

    [Fact]
    public void BlowsItDownWithNoWay() => Assert.Equal((0f, 1f), PlumeWay.Of(0, 0));

    [Theory]
    [InlineData(1e-25, 0)]
    [InlineData(0, -3e-30)]
    [InlineData(1e-40, 1e-40)]
    public void KeepsAWholeWayWhenTheThrustIsTiny(double tx, double ty)
    {
        // A float's squared length underflows to nothing here, and a plume with no way folds into a point Godot cannot fill.
        var (x, y) = PlumeWay.Of(tx, ty);
        Assert.Equal(1f, x * x + y * y, 4);
    }
}
