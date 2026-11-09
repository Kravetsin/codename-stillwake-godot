using Stillwake.Game.Features.Crew;
using Stillwake.Game.Render;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The air a tool works shows its reach while it is used (tool-reach): inside its fan or zone, only then, gone soon after.</summary>
public class AirCutsTests
{
    private const double Frame = 1000.0 / 60;

    /// <summary>Every point drawn now, the shoulder at the origin.</summary>
    private static List<(float X, float Y)> Drawn(AirCuts cuts)
    {
        var ink = new Ink { Feather = 0.05f };
        cuts.Draw(ink);
        return [.. ink.Points];
    }

    [Fact]
    public void ASwingsDustStaysInItsFanAndRangeAndMovesTheWayTheBladeGoes()
    {
        var cuts = new AirCuts();
        Assert.False(cuts.Live);
        // The axe's quarter turn, aimed straight ahead, 36 px long, the blade reaching 26.
        double half = Math.PI / 4;
        cuts.Strike(thrust: false, aim: 0, half, range: 36, inner: 26, sweepMs: 40, fadeMs: 220);
        double? early = null, late = null;
        for (int frame = 0; frame < 30; frame++)
        {
            cuts.Step(Frame, null);
            var points = Drawn(cuts);
            foreach (var (x, y) in points)
            {
                Assert.InRange(Math.Sqrt(x * x + y * y), 26 * 0.25, 36 + 1);
                Assert.InRange(Math.Atan2(y, x), -half - 0.1, half + 0.1);
            }
            if (points.Count == 0) continue;
            double mean = points.Average(p => Math.Atan2(p.Y, p.X));
            if (frame == 2) early = mean;
            if (frame == 10) late = mean;
        }
        // Carried round the way the blade comes down: from the fan's top edge toward its bottom.
        Assert.True(late > early, $"dust went from {early} to {late}");
        Assert.False(cuts.Live);
    }

    [Fact]
    public void ARisingSwingsDustIsCarriedUpTheFan()
    {
        var cuts = new AirCuts();
        double half = Math.PI / 4;
        cuts.Strike(thrust: false, aim: 0, half, range: 36, inner: 26, sweepMs: 40, fadeMs: 220, rising: true);
        var means = new List<double>();
        for (int frame = 0; frame < 12; frame++)
        {
            cuts.Step(Frame, null);
            var points = Drawn(cuts);
            foreach (var (x, y) in points) Assert.InRange(Math.Atan2(y, x), -half - 0.1, half + 0.1);
            if (points.Count > 0) means.Add(points.Average(p => Math.Atan2(p.Y, p.X)));
        }
        // From the fan's bottom edge toward its top, as a backhand comes up.
        Assert.True(means[^1] < means[1], $"dust went from {means[1]} to {means[^1]}");
    }

    [Fact]
    public void AThrustsDustIsCarriedOutAlongItToItsRange()
    {
        var cuts = new AirCuts();
        cuts.Strike(thrust: true, aim: 0, half: Math.PI / 32, range: 48, inner: 40, sweepMs: 65, fadeMs: 220);
        for (int frame = 0; frame < 20; frame++)
        {
            cuts.Step(Frame, null);
            foreach (var (x, y) in Drawn(cuts)) Assert.InRange(x, 40 - 16, 48 + 1);
        }
    }

    [Fact]
    public void ARunningToolsZoneShowsOnlyWhileItRunsAndStaysInsideIt()
    {
        var cuts = new AirCuts();
        var disc = new AirCuts.Zone(true, (28, 0), (1, 0), 24);
        for (int frame = 0; frame < 40; frame++)
        {
            cuts.Step(Frame, disc);
            foreach (var (x, y) in Drawn(cuts))
            {
                // The forward half of the disc round its hub.
                Assert.InRange(Math.Sqrt((x - 28) * (x - 28) + y * y), 0, 24 + 1);
                Assert.True(x > 28 - 1);
            }
        }
        Assert.NotEmpty(Drawn(cuts));
        // Stopped, it fades out and is gone.
        for (int frame = 0; frame < 20; frame++) cuts.Step(Frame, null);
        Assert.False(cuts.Live);
        Assert.Empty(Drawn(cuts));
        // The drill's cone from its bit along the aim, no wider than its base nor longer than it.
        var cone = new AirCuts.Zone(false, (40, 0), (1, 0), 24);
        for (int frame = 0; frame < 40; frame++)
        {
            cuts.Step(Frame, cone);
            foreach (var (x, y) in Drawn(cuts))
            {
                Assert.InRange(x, 40 - 1, 64 + 1);
                Assert.InRange(Math.Abs(y), 0, WorkLooks.ConeBase + 1);
            }
        }
    }
}
