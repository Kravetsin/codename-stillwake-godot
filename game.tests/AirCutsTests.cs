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
    public void AMoteShedOffTheBladeFliesStraightOutToTheRangeAndIsGone()
    {
        var cuts = new AirCuts();
        Assert.False(cuts.Live);
        // Off the axe's edge 26 px out, 0.3 rad below the line ahead; its range 36.
        double a = 0.3;
        cuts.Shed((26 * Math.Cos(a), 26 * Math.Sin(a)), range: 36, fadeMs: 220);
        var furthest = new List<double>();
        for (int frame = 0; frame < 20; frame++)
        {
            cuts.Step(Frame, null);
            var points = Drawn(cuts);
            foreach (var (x, y) in points)
            {
                Assert.InRange(Math.Atan2(y, x), a - 0.02, a + 0.02);
                Assert.InRange(Math.Sqrt(x * x + y * y), 26 - 1, 36 + 0.5);
            }
            if (points.Count > 0) furthest.Add(points.Max(p => Math.Sqrt(p.X * p.X + p.Y * p.Y)));
        }
        // From the blade outward, never back, stopping near the range; then gone.
        Assert.True(furthest[0] < 30, $"born at {furthest[0]}");
        for (int i = 1; i < furthest.Count; i++) Assert.True(furthest[i] >= furthest[i - 1] - 1e-6, string.Join(' ', furthest));
        Assert.True(furthest[^1] > 33, $"it stopped at {furthest[^1]}");
        Assert.False(cuts.Live);
    }

    [Fact]
    public void ABladeCutsTheAirOnlyMovingWithinItsFan()
    {
        double half = Math.PI / 4;
        (double, double) At(double angle) => (26 * Math.Cos(angle), 26 * Math.Sin(angle));
        Assert.True(AirCuts.Cutting(At(0.1), At(0.0), aim: 0, half));
        // Wound up over the shoulder, past the fan's edge: nothing yet.
        Assert.False(AirCuts.Cutting(At(-1.2), At(-1.4), aim: 0, half));
        // Held on a bite: nothing.
        Assert.False(AirCuts.Cutting(At(0.1), At(0.1), aim: 0, half));
        // Facing the other way round the circle, the fan is found across ±pi.
        Assert.True(AirCuts.Cutting(At(Math.PI - 0.1), At(Math.PI - 0.2), aim: -Math.PI + 0.1, half));
    }

    [Fact]
    public void AThrustsDustIsCarriedOutAlongItToItsRange()
    {
        var cuts = new AirCuts();
        cuts.Thrust(aim: 0, half: Math.PI / 32, range: 48, inner: 40, sweepMs: 65, fadeMs: 220);
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
