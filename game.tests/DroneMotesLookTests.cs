using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A drone window projected before the hero is a small cloud of motes over the drone (drone-holo).</summary>
public class DroneMotesLookTests
{
    [Fact]
    public void EveryMoteKeepsInsideTheSmallCloudWhateverItsDrift()
    {
        for (int i = 0; i < DroneMotesLook.Count; i++)
            for (int ms = 0; ms < 6000; ms += 37)
            {
                var (x, y) = DroneMotesLook.At(i, 1, ms, 2.5, (0, 9));
                Assert.InRange(x, -DroneMotesLook.HalfWide - DroneMotesLook.Drift, DroneMotesLook.HalfWide + DroneMotesLook.Drift);
                Assert.InRange(y, -DroneMotesLook.HalfHigh - DroneMotesLook.Drift, DroneMotesLook.HalfHigh + DroneMotesLook.Drift);
                Assert.InRange(DroneMotesLook.Twinkle(i, ms, 2.5), DroneMotesLook.Faintest, 1);
            }
    }

    [Fact]
    public void TheyRiseOutOfTheLensTheMiddleOnesFirst()
    {
        var lens = (0.0, 9.0);
        for (int i = 0; i < DroneMotesLook.Count; i++) Assert.Equal(lens, DroneMotesLook.At(i, 0, 0, 0, lens));
        var first = DroneMotesLook.At(0, 0.25, 0, 0, lens);
        var last = DroneMotesLook.At(DroneMotesLook.Count - 1, 0.25, 0, 0, lens);
        Assert.NotEqual(lens, first);
        Assert.Equal(lens, last);
    }

    [Fact]
    public void WithThePlanOutTheyEaseFromTheCloudIntoTheGrid()
    {
        Assert.Equal(0.5, DroneMotesLook.Reshape(0, true, DroneMotesLook.ReshapeMs / 2), 9);
        Assert.Equal(0, DroneMotesLook.Reshape(0.2, false, DroneMotesLook.ReshapeMs));
        var lens = (0.0, 9.0);
        var cloud = DroneMotesLook.At(5, 1, 0, 0, lens, 0);
        var grid = DroneMotesLook.At(5, 1, 0, 0, lens, 1);
        var midway = DroneMotesLook.At(5, 1, 0, 0, lens, 0.5);
        Assert.InRange(midway.X, System.Math.Min(cloud.X, grid.X) - 0.7, System.Math.Max(cloud.X, grid.X) + 0.7);
        Assert.NotEqual(cloud, midway);
        Assert.NotEqual(grid, midway);
    }
}
