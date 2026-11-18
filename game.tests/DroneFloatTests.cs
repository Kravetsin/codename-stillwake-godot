using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The light drone floats by its hero's head (light-drone, a try).</summary>
public class DroneFloatTests
{
    private static double Apart((double X, double Y) a, (double X, double Y) b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

    [Fact]
    public void ItHoversAboveAndBehindTheHeadTheWayTheHeroFaces()
    {
        foreach (int facing in (int[])[1, -1])
        {
            var drone = new DroneFloat();
            var at = drone.Follow((100, 50), facing, 16, 0);
            for (int i = 0; i < 200; i++) at = drone.Follow((100, 50), facing, 16, 0);
            Assert.Equal(100 - facing * DroneFloat.Behind, at.X, 6);
            Assert.Equal(50 - DroneFloat.Above, at.Y, 6);
        }
    }

    [Fact]
    public void ItLagsAfterAMovingHeadButNoFurtherThanItsLeashAndJumpsAfterAFarOne()
    {
        var drone = new DroneFloat();
        drone.Follow((0, 0), 1, 16, 0);
        var place = (X: 40 - DroneFloat.Behind, Y: -DroneFloat.Above);
        var at = drone.Follow((40, 0), 1, 16, 0);
        Assert.InRange(Apart(at, place), 1, DroneFloat.Leash + 1e-9);
        at = drone.Follow((1000, 0), 1, 16, 0);
        Assert.Equal(1000 - DroneFloat.Behind, at.X, 6);
    }
}
