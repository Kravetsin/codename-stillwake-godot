using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Oxygen and water read in litres, a unit a millilitre (industrial-oxygen), to the hundredth (liquid-volumes).</summary>
public class LitresTests
{
    [Fact]
    public void AnAmountIsLitresToTheHundredthRoundedDown()
    {
        Assert.Equal("1.52", Litres.Of(1523));
        Assert.Equal("1.52", Litres.Of(1529));
        Assert.Equal("0.00", Litres.Of(0));
        Assert.Equal("0.00", Litres.Of(9));
        Assert.Equal("149.99", Litres.Of(Content.Air.TankCapacity - 1));
        Assert.Equal("150.00", Litres.Of(Content.Air.TankCapacity));
    }

    [Fact]
    public void ACanisterAndABottleEachHoldThreeLitresAndNothingElseHoldsAny()
    {
        Assert.Equal(3000, Litres.In(Content.Crew.Canister));
        Assert.Equal(3000, Litres.In(Content.Water.Item));
        Assert.Null(Litres.In("biofuel"));
        Assert.Null(Litres.In("iron"));
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
