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
    public void AFireJumpsFromOnePictureToTheNext()
    {
        // Two pictures a hundredth of the roll apart burn visibly apart: nothing fades between them.
        Assert.True(Flames.Blaze(0.5) - Flames.Blaze(0.4) > 0.04f);
    }
}
