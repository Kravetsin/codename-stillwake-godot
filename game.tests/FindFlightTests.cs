using Stillwake.Game.Features.Research;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The find sliding down the research key's beam from the burst bulb.</summary>
public class FindFlightTests
{
    private static readonly (double X, double Y) Bulb = (640, 44), Key = (600, 679), KeyTop = (600, 665);

    [Fact]
    public void ItHatchesAtTheBulbAndLandsInTheKey()
    {
        Assert.Equal(Bulb, FindFlight.Find(0, Bulb, Key));
        Assert.Equal(Key, FindFlight.Find(BulbSignal.FlightS, Bulb, Key));
        Assert.Equal(FindFlight.HatchSize, FindFlight.Size(0));
        Assert.Equal(FindFlight.LandSize, FindFlight.Size(BulbSignal.FlightS));
    }

    [Fact]
    public void TheBeamReachesTheBulbBeforeTheFindSetsOffAndIsBackInTheKeyAsItLands()
    {
        Assert.Null(FindFlight.Head(0, Bulb, Key, KeyTop));
        Assert.Equal(Bulb.Y, FindFlight.Head(FindFlight.ReachS - 1e-6, Bulb, Key, KeyTop)!.Value.Y, 1);
        Assert.Null(FindFlight.Head(BulbSignal.FlightS, Bulb, Key, KeyTop));
    }

    [Fact]
    public void ItFadesIntoTheHologramOnTheWayDown()
    {
        Assert.Equal(0, FindFlight.Faded(0.2));
        Assert.Equal(1, FindFlight.Faded(BulbSignal.FlightS));
    }
}
