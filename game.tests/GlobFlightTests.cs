using Stillwake.Game.Features.Creatures;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A capback's glob as drawn (lurker-attacks): a lob from its mouth to where the arc ended, by the share of its flight gone.</summary>
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

    private static (double, double) Point((double X, double Y, double Heading) at) => (Math.Round(at.X, 6), Math.Round(at.Y, 6));
}
