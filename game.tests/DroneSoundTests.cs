using System;
using System.Linq;
using Stillwake.Game.Features.Logistics;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Drones heard (0062): the rotors' loop from the motor, the speed, the wobble and the id; the clamp's click; where each carries.</summary>
public class DroneSoundTests
{
    private const double Frame = 1.0 / 60;

    private static Whir Run(Whir whir, double seconds, bool flying)
    {
        for (double t = 0; t < seconds - 1e-9; t += Frame) whir.Step(flying, Frame);
        return whir;
    }

    [Fact]
    public void ItsMotorSpinsUpOverHalfASecondFromTwoFifthsOfItsPitchAndRunsDownOverSevenTenthsAfterLanding()
    {
        var whir = new Whir();
        Assert.False(whir.Playing);
        whir = Run(whir, 0.25, true);
        Assert.True(whir.Playing);
        double wobble = Whir.Wobble(0.25, 1);
        // Half spun: pitch 0.4 + 0.6 · 0.5, volume full from two thirds of its spin.
        Assert.Equal(Whir.Swarm(1) * 0.7 * Math.Pow(2, 12 * wobble / 1200), whir.Pitch(1, 0), 3);
        Assert.Equal(20 * Math.Log10(0.75) + 0.5 * wobble, whir.GainDb(1, 0), 2);
        whir = Run(whir, 0.25, true);
        whir = Run(whir, 0.65, false);
        Assert.True(whir.Playing);
        whir = Run(whir, 0.1, false);
        Assert.False(whir.Playing);
    }

    [Fact]
    public void AtFullSpeedItIsSixPerCentHigherAndTwoDecibelsLouder()
    {
        var whir = Run(new Whir(), 1, true);
        double wobble = Whir.Wobble(1, 4);
        Assert.Equal(Whir.Swarm(4) * 1.06 * Math.Pow(2, 12 * wobble / 1200), whir.Pitch(4, 1), 3);
        Assert.Equal(2 + 0.5 * wobble, whir.GainDb(4, 1), 2);
        Assert.Equal(whir.GainDb(4, 0) + 2, whir.GainDb(4, 1), 3);
    }

    [Fact]
    public void ItWobblesWithinTwelveCentsAndEachDroneIsTunedApartWithinFourPerCent()
    {
        for (int id = 1; id < 40; id++)
        {
            Assert.InRange(Whir.Swarm(id), 0.96, 1.04);
            for (double t = 0; t < 5; t += 0.05) Assert.InRange(Whir.Wobble(t, id), -1, 1);
        }
        var tunes = Enumerable.Range(1, 4).Select(Whir.Swarm).OrderBy(x => x).ToList();
        for (int i = 1; i < tunes.Count; i++) Assert.True(tunes[i] - tunes[i - 1] > 0.005, $"drones tuned {tunes[i - 1]} and {tunes[i]}");
        Assert.NotEqual(Whir.Wobble(0, 1), Whir.Wobble(0, 2));
    }

    [Fact]
    public void ItsClampClicksTakingAStackAndPuttingSomeOrAllOfItIn()
    {
        Assert.True(Served.Gripped(null, ("chunk", 20)));
        Assert.True(Served.Gripped(("chunk", 20), null));
        Assert.True(Served.Gripped(("chunk", 20), ("chunk", 6)));
        Assert.False(Served.Gripped(("chunk", 20), ("chunk", 20)));
        Assert.False(Served.Gripped(null, null));
    }

    [Fact]
    public void EveryDroneSoundCarriesAsAStationsAndSitsUnderOne()
    {
        foreach (var cue in new[] { Sound.DroneWhir, Sound.DroneDock, Sound.DroneGrab })
        {
            Assert.Equal(Sound.StationReach, cue.Heard);
            Assert.Contains(cue, Sound.All);
        }
        Assert.True(Sound.DroneWhir.Loops);
        Assert.Equal(3, Sound.DroneDock.Files.Count);
        Assert.Equal(3, Sound.DroneGrab.Files.Count);
    }
}
