using System;
using Stillwake.Game.Features.Light;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A fire's light wavering with its flames.</summary>
public class FlickerTests
{
    [Fact]
    public void AFireWaversBetweenMostOfItsLightAndAllOfIt()
    {
        float least = 1, most = 0;
        for (double t = 0; t < 60_000; t += 16)
        {
            float burns = Flicker.FireOf(t, 5);
            least = Math.Min(least, burns);
            most = Math.Max(most, burns);
        }
        Assert.True(least >= 0.6f);
        Assert.True(least < 0.65f);
        Assert.True(most <= 1);
        Assert.True(most > 0.95f);
    }

    [Fact]
    public void AFireFadesFromOneBrightnessToTheNextRatherThanJumping()
    {
        float largest = 0;
        for (double t = 16; t < 60_000; t += 16)
            largest = Math.Max(largest, Math.Abs(Flicker.FireOf(t, 5) - Flicker.FireOf(t - 16, 5)));
        Assert.True(largest > 0);
        Assert.True(largest < 0.1f);
    }

    [Fact]
    public void FiresWaverOutOfStep()
    {
        int apart = 0;
        for (double t = 0; t < 10_000; t += 60)
            if (Flicker.FireOf(t, 1) != Flicker.FireOf(t, 2)) apart++;
        Assert.True(apart > 100);
    }
}
