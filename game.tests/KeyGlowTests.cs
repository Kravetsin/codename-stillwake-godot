using Stillwake.Game.Features.Research;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The lens on the tablet's key coming on and going out.</summary>
public class KeyGlowTests
{
    [Fact]
    public void ItStartsDarkAndStaysSoWithNothingNew()
    {
        var glow = new KeyGlow();
        Assert.False(glow.Step(1, false));
        Assert.Equal(0, glow.Lit);
    }

    [Fact]
    public void ItComesOnOverItsTimeAndHolds()
    {
        var glow = new KeyGlow();
        Assert.True(glow.Step(KeyGlow.ComesOn / 2, true));
        Assert.Equal(0.5, glow.Lit, 6);
        Assert.True(glow.Step(KeyGlow.ComesOn / 2, true));
        Assert.Equal(1, glow.Lit, 6);
        Assert.False(glow.Step(1, true));
    }

    [Fact]
    public void ItGoesOutFasterThanItComesOnFromWhereverItWas()
    {
        var glow = new KeyGlow();
        glow.Step(KeyGlow.ComesOn / 2, true);
        glow.Step(KeyGlow.GoesOut / 2, false);
        Assert.Equal(0, glow.Lit, 6);
        glow.Step(KeyGlow.ComesOn, true);
        glow.Step(KeyGlow.GoesOut, false);
        Assert.Equal(0, glow.Lit, 6);
    }

    [Fact]
    public void ItTakesTheTimesItIsGiven()
    {
        var glow = new KeyGlow(comesOn: 0.12, goesOut: 0.5);
        glow.Step(0.12, true);
        Assert.Equal(1, glow.Lit, 6);
        glow.Step(0.25, false);
        Assert.Equal(0.5, glow.Lit, 6);
    }
}
