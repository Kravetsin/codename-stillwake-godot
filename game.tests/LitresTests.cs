using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Oxygen and water read in litres, a unit a millilitre (industrial-oxygen).</summary>
public class LitresTests
{
    [Fact]
    public void AnAmountIsLitresToTheMillilitre()
    {
        Assert.Equal("1.523", Litres.Of(1523));
        Assert.Equal("0.000", Litres.Of(0));
        Assert.Equal("150.000", Litres.Of(Content.Air.TankCapacity));
    }

    [Fact]
    public void ARateIsLitresAMinuteToATenthOfAMillilitreRoundedDown()
    {
        // Scrap leaks 30 a period of ten minutes, glass 15, a vaporizer makes 12000.
        Assert.Equal("0.003", Litres.PerMinute(30));
        Assert.Equal("0.0015", Litres.PerMinute(15));
        Assert.Equal("1.200", Litres.PerMinute(Content.Air.GeneratorRate));
        Assert.Equal("0.0001", Litres.PerMinute(1));
    }
}
