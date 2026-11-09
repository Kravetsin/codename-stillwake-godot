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
    public void ASwingsWaveRunsOutFromTheBladeInItsFanAndIsGoneAtTheRange()
    {
        var cuts = new AirCuts();
        Assert.False(cuts.Live);
        // The axe's quarter turn, aimed straight ahead, 36 px long, born at 26.
        double half = Math.PI / 4;
        cuts.Strike(thrust: false, aim: 0, half, range: 36, inner: 26, sweepMs: WorkLooks.WaveMs, fadeMs: 220);
        var middles = new List<double>();
        for (double ms = Frame; ms <= WorkLooks.WaveMs + 3 * Frame; ms += Frame)
        {
            cuts.Step(Frame, null);
            var points = Drawn(cuts);
            foreach (var (x, y) in points)
            {
                Assert.InRange(Math.Sqrt(x * x + y * y), 26 - 3.5, 36 + 0.5);
                Assert.InRange(Math.Atan2(y, x), -half - 0.01, half + 0.01);
            }
            if (ms <= WorkLooks.WaveMs - Frame) Assert.NotEmpty(points);
            if (ms > WorkLooks.WaveMs) Assert.Empty(points);
            if (points.Count > 0) middles.Add(points.Max(p => Math.Sqrt(p.X * p.X + p.Y * p.Y)));
        }
        // Out from the blade, never back, to the range.
        for (int i = 1; i < middles.Count; i++) Assert.True(middles[i] >= middles[i - 1], string.Join(' ', middles));
        Assert.True(middles[^1] > 35, $"it went out to {middles[^1]}");
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
