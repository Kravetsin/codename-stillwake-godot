using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The air a running tool works (tool-reach): its zone shows only while it runs, eased in, gone soon after it stops.</summary>
public class AirCutsTests
{
    private const double Frame = 1000.0 / 60;

    [Fact]
    public void ARunningToolsZoneShowsOnlyWhileItRunsAndFadesAfter()
    {
        var cuts = new AirCuts();
        var disc = new AirCuts.Zone(true, (28, 0), (1, 0), 24);
        Assert.Null(cuts.Working);
        cuts.Step(Frame, disc);
        double first = cuts.Working!.Value.On;
        for (int frame = 0; frame < 40; frame++) cuts.Step(Frame, disc);
        Assert.Equal(disc, cuts.Working!.Value.Zone);
        Assert.True(cuts.Working.Value.On > first);
        Assert.Equal(1, cuts.Working.Value.On);
        // Stopped, it fades out and is gone.
        cuts.Step(Frame, null);
        Assert.InRange(cuts.Working!.Value.On, 0.01, 0.99);
        for (int frame = 0; frame < 20; frame++) cuts.Step(Frame, null);
        Assert.Null(cuts.Working);
    }
}
