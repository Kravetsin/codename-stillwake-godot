using Stillwake.Game.Features.Creatures;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A capback's glob as drawn (lurker-attacks): a lob from its mouth to where the arc ended, longer the farther it goes.</summary>
public class GlobFlightTests
{
    [Fact]
    public void ItLeavesTheMouthAndLandsWhereTheArcEnded()
    {
        Assert.Equal((10.0, 50.0), Point(GlobFlight.At(10, 50, 130, 60, 0)));
        Assert.Equal((130.0, 60.0), Point(GlobFlight.At(10, 50, 130, 60, 1)));
        Assert.Equal((130.0, 60.0), Point(GlobFlight.At(10, 50, 130, 60, 3)));
    }

    [Fact]
    public void ItRisesOverItsLineHighestHalfWayAtMostItsMostRise()
    {
        var (_, y, heading) = GlobFlight.At(0, 0, 60, 0, 0.5);
        Assert.Equal(-GlobFlight.Rise * 60, y, 6);
        Assert.Equal(0, heading, 6);
        Assert.Equal(-GlobFlight.MostRise, GlobFlight.At(0, 0, 400, 0, 0.5).Y, 6);
        // Rising as it leaves, falling as it lands.
        Assert.True(GlobFlight.At(0, 0, 60, 0, 0.1).Heading < 0 && GlobFlight.At(0, 0, 60, 0, 0.9).Heading > 0);
    }

    [Fact]
    public void AFartherGlobFliesLongerUpToItsMost()
    {
        Assert.Equal(GlobFlight.LeastS, GlobFlight.Seconds(5, 5, 5, 5), 6);
        Assert.True(GlobFlight.Seconds(0, 0, 160, 0) > GlobFlight.Seconds(0, 0, 64, 0));
        Assert.Equal(GlobFlight.MostS, GlobFlight.Seconds(0, 0, 4000, 0), 6);
    }

    private static (double, double) Point((double X, double Y, double Heading) at) => (Math.Round(at.X, 6), Math.Round(at.Y, 6));
}
