using Stillwake.Game.Features.Crew;
using Xunit;
using T = Stillwake.Content.Temperature;

namespace Stillwake.Game.Tests;

/// <summary>
/// The body's temperature as the client keeps it (body-temperature): drawn toward where the degrees felt put it, no faster than
/// <c>Temperature.Drift</c> away from normal, back unbounded; dead at 28° or 42°; its speed from it.
/// </summary>
public class BodyHeatTests
{
    private const double Frame = 1 / 60.0;

    /// <summary>Steps <paramref name="body"/> a frame at a time for <paramref name="seconds"/>, or until it dies; the seconds it lived, or null.</summary>
    private static double? DiesWithin(BodyHeat body, double seconds, int felt, int heat)
    {
        for (int i = 1; i <= seconds / Frame; i++)
            if (body.Step(Frame, felt, heat)) return i * Frame;
        return null;
    }

    [Fact]
    public void AWellBodyFeelingNothingStaysAtItsNormal()
    {
        var body = new BodyHeat();
        Assert.Null(DiesWithin(body, 60, 0, 0));
        Assert.Equal(37f, body.Degrees);
    }

    [Fact]
    public void TheDeepestColdCoolsItNoFasterThanDriftASecondSoDeathTakes36Seconds()
    {
        // Space's −200° on a basic suit: 90 felt, a target of 19°, far past death; capped at a quarter of a degree a second.
        var body = new BodyHeat();
        body.Step(1, 90, 0);
        Assert.Equal(37 - T.Drift / 100f, body.Degrees, 3);
        var fresh = new BodyHeat();
        Assert.InRange(DiesWithin(fresh, 60, 90, 0)!.Value, 35.9, 36.1);
        Assert.InRange(DiesWithin(new BodyHeat(), 60, 1000, 0)!.Value, 35.9, 36.1);
    }

    [Fact]
    public void TheDeepestHeatWarmsItNoFasterSoDeathTakes20Seconds()
    {
        Assert.InRange(DiesWithin(new BodyHeat(), 60, 0, 90)!.Value, 19.9, 20.1);
    }

    [Fact]
    public void AMildColdSettlesTheBodyShortOfDeathAndItLivesOn()
    {
        // A bare hero on a mild night: 8 felt, settling at 37 − 1.6 = 35.4°.
        var body = new BodyHeat();
        Assert.Null(DiesWithin(body, 120, 8, 0));
        Assert.Equal(35.4f, body.Degrees, 2);
        Assert.Equal(35.4f, BodyHeat.Target(8, 0), 3);
    }

    [Fact]
    public void BackInTheWarmItRecoversFasterThanItCooledUnboundedByDrift()
    {
        var body = new BodyHeat();
        DiesWithin(body, 28, 90, 0);
        Assert.InRange(body.Degrees, 29.9f, 30.1f);
        body.Step(1, 0, 0);
        Assert.True(body.Degrees - 30 > T.Drift / 100f, $"{body.Degrees}");
        DiesWithin(body, 4, 0, 0);
        Assert.Equal(1000, BodyHeat.SpeedOf(body.Degrees));
    }

    [Fact]
    public void FramesOfAnyLengthCoolItAlike()
    {
        var (fine, coarse) = (new BodyHeat(), new BodyHeat());
        DiesWithin(fine, 10, 30, 0);
        for (int i = 0; i < 10; i++) coarse.Step(1, 30, 0);
        Assert.Equal(coarse.Degrees, fine.Degrees, 2);
    }

    [Fact]
    public void ResetMakesItWellAgain()
    {
        var body = new BodyHeat();
        DiesWithin(body, 10, 90, 0);
        body.Reset();
        Assert.Equal(BodyHeat.Normal, body.Degrees);
    }

    [Theory]
    [InlineData(37f, 1000)]
    [InlineData(35f, 1000)]
    [InlineData(39f, 1000)]
    [InlineData(32f, 658)]
    [InlineData(28f, 200)]
    [InlineData(20f, 200)]
    [InlineData(42f, 200)]
    [InlineData(40.5f, 600)]
    public void ItsSpeedIsWholeWithinTheEaseFallingStraightToTheLeastAtEitherDeath(float degrees, int speed) =>
        Assert.Equal(speed, BodyHeat.SpeedOf(degrees));

    [Fact]
    public void ItsSpeedNeverRisesAsItCoolsOrHeatsFurther()
    {
        for (float d = 37; d > 27; d -= 0.05f) Assert.True(BodyHeat.SpeedOf(d - 0.05f) <= BodyHeat.SpeedOf(d), $"{d}");
        for (float d = 37; d < 43; d += 0.05f) Assert.True(BodyHeat.SpeedOf(d + 0.05f) <= BodyHeat.SpeedOf(d), $"{d}");
    }
}
