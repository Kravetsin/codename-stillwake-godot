using System;
using Stillwake.Game.Features.Furnace;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A fire's light jumping with its flames, a coal cracking now and then.</summary>
public class FlamesTests
{
    [Fact]
    public void AFireBurnsBetweenItsLowAndWholeAndFlaresPastWholeOnlyAsACoalCracks()
    {
        float least = 2, most = 0;
        int cracks = 0, rolls = 10_000;
        for (int i = 0; i < rolls; i++)
        {
            float blaze = Flames.Blaze(i / (double)rolls);
            if (Flames.Cracks(blaze))
            {
                cracks++;
                Assert.Equal(Flames.Crack, blaze);
                continue;
            }
            least = Math.Min(least, blaze);
            most = Math.Max(most, blaze);
        }
        Assert.Equal(Flames.Low, least, 3);
        Assert.True(most <= 1);
        Assert.True(most > 0.99f);
        Assert.Equal(Flames.CrackChance, cracks / (double)rolls, 3);
    }

    [Fact]
    public void APictureBurnsVisiblyApartFromTheNext()
    {
        // Two pictures a tenth of the roll apart burn visibly apart: the glow has somewhere to breathe to.
        Assert.True(Flames.Blaze(0.5) - Flames.Blaze(0.4) > 0.04f);
    }

    [Fact]
    public void AFireGlowBreathesTowardItsPictureAlikeAtAnyFrameRate()
    {
        // A sixtieth of a second goes the breath's share of the way; a frame no time, nowhere.
        Assert.Equal(0.5f + 0.5f * (float)Flames.BreathPerFrame, Flames.Breathe(0.5f, 1, 1000.0 / 60), 4);
        Assert.Equal(0.5f, Flames.Breathe(0.5f, 1, 0), 4);
        // Two half frames end where one whole does: the same at 120 Hz as at 60.
        float halves = Flames.Breathe(Flames.Breathe(0.55f, 1.5f, 1000.0 / 120), 1.5f, 1000.0 / 120);
        Assert.Equal(Flames.Breathe(0.55f, 1.5f, 1000.0 / 60), halves, 4);
        // Never past it, and there within a second.
        Assert.True(Flames.Breathe(0.55f, 1, 1000) is > 0.999f and <= 1);
    }
}
