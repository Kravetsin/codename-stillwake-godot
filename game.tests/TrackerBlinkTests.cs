using Stillwake.Game.Features.Crew;
using Stillwake.Sim.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The tracker's light by its signal (tracker-catchers): steady when calm, the picked patterns repeating otherwise.</summary>
public class TrackerBlinkTests
{
    [Fact]
    public void ACalmTrackerIsSteadyAndAnAlarmedOrHuntedOneBlinksUrgentlyRepeating()
    {
        Assert.All(new[] { 0.0, 333, 10_000 }, ms => Assert.Equal(1, TrackerBlink.Of(Signal.Calm, ms)));
        foreach (var (signal, steps) in new[] { (Signal.Coming, TrackerBlink.Hunted), (Signal.Hunted, TrackerBlink.Hunted) })
        {
            double period = steps.Sum(s => s.Ms);
            Assert.All(new[] { 0.0, 45, 400, period - 1 }, ms => Assert.Equal(TrackerBlink.Of(signal, ms), TrackerBlink.Of(signal, ms + 3 * period), 9));
            Assert.Contains(steps, s => s.From != 1 || s.To != 1);
        }
    }

    [Fact]
    public void ARampGoesInAStraightLineAndAStepHolds()
    {
        TrackerBlink.Step[] steps = [new(100, 0, 1), new(50, 2, 2)];
        Assert.Equal(0.25, TrackerBlink.LevelOf(steps, 25), 9);
        Assert.Equal(2, TrackerBlink.LevelOf(steps, 120), 9);
        Assert.Equal(0.5, TrackerBlink.LevelOf(steps, 150 + 50), 9);
    }

    [Fact]
    public void BrighterGoesTowardWhiteAndDimmerFadesOut()
    {
        Assert.Equal((1, 0.8, 0.2, 1), TrackerBlink.Lit((0.6, 0.4, 0.1), 2));
        Assert.Equal((0.6, 0.4, 0.1, 0.5), TrackerBlink.Lit((0.6, 0.4, 0.1), 0.5));
        Assert.Equal((0.6, 0.4, 0.1, 0), TrackerBlink.Lit((0.6, 0.4, 0.1), 0));
    }
}
