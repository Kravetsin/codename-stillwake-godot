using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The air bar's spring: it settles without overshoot, alike at any frame rate.</summary>
public class CriticalSpringTests
{
    [Fact]
    public void ItSettlesOnItsAimWithoutOvershoot()
    {
        var spring = new CriticalSpring(0);
        double was = 0;
        for (int f = 0; f < 90; f++)
        {
            spring.Step(10, 9, 1.0 / 60);
            Assert.True(spring.X >= was && spring.X <= 10, $"{was} -> {spring.X}");
            was = spring.X;
        }
        Assert.Equal(10, spring.X, 1);
    }

    [Fact]
    public void ItLandsAlikeAt30And144FramesASecond()
    {
        var slow = new CriticalSpring(0);
        var fast = new CriticalSpring(0);
        for (int f = 0; f < 15; f++) slow.Step(10, 9, 1.0 / 30);
        for (int f = 0; f < 72; f++) fast.Step(10, 9, 1.0 / 144);
        Assert.Equal(fast.X, slow.X, 9);
        Assert.Equal(fast.V, slow.V, 9);
        // Half a second in, part of the way: never rounded to a whole unit.
        Assert.InRange(slow.X, 9, 9.99);
    }
}
