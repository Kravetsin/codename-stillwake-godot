using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The numbers light is drawn with (<c>game/Content/logic/Light.cs</c>).</summary>
public class LightTests
{
    [Fact]
    public void LightFadesInAirMoreThroughMachinesAndACellThickWallLetsNextToNoneThrough()
    {
        Assert.True(Light.Margin >= 0);
        Assert.True(Light.Rounds >= 1);
        foreach (float keep in (float[])[Light.StarKeep, Light.StarMachine, Light.WallIn, Light.WallDeep, Light.WallOut])
        {
            Assert.True(keep > 0);
            Assert.True(keep < 1);
        }
        Assert.True(Light.StarMachine < Light.StarKeep);
        // Through a wall a cell thick, into the cell beyond: a tenth at most.
        Assert.True(Light.WallIn * Light.WallOut * Light.StarKeep < 0.1f);
    }

    [Fact]
    public void LightComesInColoursAbove0SourcesAtMostTwiceWhiteNeverLeavingACornerBlack()
    {
        foreach (var (r, g, b) in (List<(float, float, float)>)[Light.Star, Light.Lamp, Light.Fire])
            foreach (float n in (float[])[r, g, b])
            {
                Assert.True(n > 0);
                Assert.True(n <= 2);
            }
        foreach (float n in (float[])[Light.Ambient.R, Light.Ambient.G, Light.Ambient.B])
        {
            Assert.True(n > 0);
            Assert.True(n < 1);
        }
        Assert.True(Light.Particle > 0);
    }
}
