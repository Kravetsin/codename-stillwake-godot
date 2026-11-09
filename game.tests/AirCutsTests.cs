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
    public void ASwingsBlurTrailsTheBladeOverWhatItSweptOutToTheRangeAndFadesWhenItStops()
    {
        var cuts = new AirCuts();
        Assert.False(cuts.Live);
        // The blade entered the fan at -0.6 rad and comes down 0.1 a frame; the axe's range 36.
        double from = -0.6, blade = from;
        for (int frame = 0; frame < 10; frame++)
        {
            blade += 0.1;
            cuts.Swing(from, blade, range: 36);
            cuts.Step(Frame, null);
            var points = Drawn(cuts);
            Assert.NotEmpty(points);
            foreach (var (x, y) in points)
            {
                // Never ahead of the blade, never before where it came in, between the blade and the range.
                Assert.InRange(Math.Atan2(y, x), from - 0.02, blade + 0.02);
                Assert.InRange(Math.Sqrt(x * x + y * y), 36 - 13 - 1, 36 + 0.5);
            }
            if (frame == 9) Assert.True(points.Max(p => Math.Atan2(p.Y, p.X)) > blade - 0.1, $"its front lags the blade at {blade}");
        }
        // Let go, it fades out and is gone.
        for (double ms = 0; ms <= WorkLooks.ZoneOut + Frame; ms += Frame) cuts.Step(Frame, null);
        Assert.False(cuts.Live);
        Assert.Empty(Drawn(cuts));
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
