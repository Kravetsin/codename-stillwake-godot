using System.Linq;
using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The drone's flurry at the hero's waist while a drone window is out.</summary>
public class DroneFlurryTests
{
    private static DroneFlurry Run(DroneFlurry flurry, bool on, double seconds)
    {
        for (double t = 0; t < seconds; t += 1 / 60.0) flurry.Step(1 / 60.0, on);
        return flurry;
    }

    [Fact]
    public void WhileAWindowIsOutItsSparksDartAllAboutTheCoreInTheOval()
    {
        var flurry = Run(new DroneFlurry(), true, DroneFlurry.RiseS + 0.05);
        Assert.Equal(1, flurry.Shown);
        for (int i = 0; i < DroneFlurry.Count; i++)
        {
            // Over two seconds each spark goes to many places, each in the oval, and is lit.
            var at = Enumerable.Range(0, 40).Select(k => flurry.At(i, k * 0.05)).ToList();
            Assert.All(at, s => Assert.InRange(s.X * s.X / (DroneFlurry.HalfWide * DroneFlurry.HalfWide) + s.Y * s.Y / (DroneFlurry.HalfHigh * DroneFlurry.HalfHigh), 0, 1.0001f));
            Assert.All(at, s => Assert.True(s.Heat > 0.2f));
            Assert.True(at.Select(s => (int)(s.X * 2)).Distinct().Count() >= 4, $"spark {i} hardly moves");
        }
        // Some before the core, some behind it.
        var now = Enumerable.Range(0, DroneFlurry.Count).Select(i => flurry.At(i, 1.3)).ToList();
        Assert.Contains(now, s => s.Front);
        Assert.Contains(now, s => !s.Front);
    }

    [Fact]
    public void ShutItSinksBackIntoTheCoreAndGoesOut()
    {
        var flurry = Run(new DroneFlurry(), false, 1);
        Assert.Equal(0, flurry.Shown);
        Run(flurry, true, 1);
        Run(flurry, false, DroneFlurry.SinkS + 0.02);
        Assert.Equal(0, flurry.Shown);
        Assert.All(Enumerable.Range(0, DroneFlurry.Count), i =>
        {
            var s = flurry.At(i, 2.5);
            Assert.Equal((0f, 0f, 0f), (s.X, s.Y, s.Heat));
        });
    }
}
