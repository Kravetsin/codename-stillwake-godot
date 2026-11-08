using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A number counting to what it reads and flaring as it goes (credit-counter).</summary>
public class CountFlareTests
{
    [Fact]
    public void TheFirstReadShowsAtOnceAndDoesNotFlare()
    {
        var flare = new CountFlare();
        flare.Read(1300);
        Assert.Equal(1300, flare.Shown);
        Assert.Equal(0, flare.Heat);
    }

    [Fact]
    public void ANewReadIsCountedToWithinTheCount()
    {
        var flare = Read(1300, 4200);
        Assert.Equal(1300, flare.Shown);
        flare.Step(CountFlare.CountS / 2);
        Assert.InRange(flare.Shown, 1301, 4199);
        flare.Step(CountFlare.CountS / 2);
        Assert.Equal(4200, flare.Shown);
        Assert.True(flare.Up);
    }

    [Fact]
    public void TheFlareRisesThenFadesAfterTheCount()
    {
        var flare = Read(1300, 4200);
        flare.Step(CountFlare.RiseS);
        Assert.Equal(1, flare.Heat, 3);
        flare.Step(CountFlare.CountS);
        Assert.InRange(flare.Heat, 0.01, 0.99);
        flare.Step(CountFlare.GlowS);
        Assert.Equal(0, flare.Heat, 6);
    }

    [Fact]
    public void ASmallChangeFlaresFaintly()
    {
        var flare = Read(10000, 9990);
        flare.Step(CountFlare.RiseS);
        Assert.Equal(CountFlare.Faint, flare.Heat, 3);
        Assert.False(flare.Up);
    }

    [Fact]
    public void OnlyTheDigitsThatChangedGlowOnAfterTheCount()
    {
        var flare = Read(1300, 1350);
        flare.Step(CountFlare.RiseS);
        Assert.True(flare.HeatOf(0) > 0 && flare.HeatOf(3) > 0, "every digit glows while counting");
        flare.Step(CountFlare.CountS);
        Assert.True(flare.HeatOf(1) > 0, "the tens changed");
        Assert.Equal(0, flare.HeatOf(0));
        Assert.Equal(0, flare.HeatOf(3));
    }

    [Fact]
    public void ReadsQuickerThanTheCountShortenItSoADrainKeepsUp()
    {
        var flare = Read(4300, 4260);
        flare.Step(0.25);
        flare.Read(4220);
        flare.Step(0.25 * 0.9);
        Assert.Equal(4220, flare.Shown);
    }

    [Fact]
    public void AReadMidCountCountsOnFromWhatIsShown()
    {
        var flare = Read(0, 1000);
        flare.Step(CountFlare.CountS / 4);
        int shown = flare.Shown;
        flare.Read(2000);
        Assert.Equal(shown, flare.Shown);
    }

    [Fact]
    public void ASmallChangePopsAsFullAsABigOneThoughItFlaresLess()
    {
        var small = Read(20000, 20001);
        var big = Read(0, 20000);
        small.Step(CountFlare.RiseS);
        big.Step(CountFlare.RiseS);
        Assert.Equal(1, small.Pop, 6);
        Assert.Equal(big.Pop, small.Pop, 6);
        Assert.True(small.Heat < big.Heat);
        small.Step(CountFlare.CountS + CountFlare.GlowS);
        Assert.Equal(0, small.Pop, 6);
    }

    private static CountFlare Read(int first, int then)
    {
        var flare = new CountFlare();
        flare.Read(first);
        flare.Read(then);
        return flare;
    }
}
