using Stillwake.Game.Features.Crew;
using Xunit;
using T = Stillwake.Content.Temperature;

namespace Stillwake.Game.Tests;

/// <summary>
/// The body's temperature as the client keeps it (body-temperature): drawn toward where the degrees felt put it, away from normal
/// no faster than <c>Temperature.Drift</c> a second for each degree felt, back unbounded; dead at 28° or 42°; its speed from it.
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
    public void SpacesColdAtTheFieldsLineCoolsABasicSuitByDriftPerDegreeSoDeathTakes33Seconds()
    {
        // Space's −200° on a basic suit: 90 felt, a target of 19°, far past death; capped at 0.27° a second.
        var body = new BodyHeat();
        body.Step(1, 90, 0);
        Assert.Equal(37 - 90 * T.Drift / 1000f, body.Degrees, 3);
        Assert.InRange(DiesWithin(new BodyHeat(), 60, 90, 0)!.Value, 33.2, 33.5);
    }

    [Fact]
    public void DeeperColdCoolsItFasterSoTheWorldsEdgeKillsInSeconds()
    {
        var (line, deep) = (new BodyHeat(), new BodyHeat());
        line.Step(1, 90, 0);
        deep.Step(1, 900, 0);
        Assert.Equal(10 * (37 - line.Degrees), 37 - deep.Degrees, 2);
        Assert.InRange(DiesWithin(new BodyHeat(), 60, 1890, 0)!.Value, 1.5, 1.7);
    }

    [Fact]
    public void HeatWarmsItTheSameWaySoDeathAtNinetyPastTakes18Seconds()
    {
        Assert.InRange(DiesWithin(new BodyHeat(), 60, 0, 90)!.Value, 18.4, 18.6);
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
        DiesWithin(body, 25, 90, 0);
        Assert.InRange(body.Degrees, 30.2f, 30.3f);
        body.Step(1, 0, 0);
        Assert.True(body.Degrees - 30.25f > BodyHeat.Rate(90, 0), $"{body.Degrees}");
        DiesWithin(body, 30, 0, 0);
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
    [InlineData(36.9f, 991)]
    [InlineData(35f, 822)]
    [InlineData(32f, 555)]
    [InlineData(28f, 200)]
    [InlineData(20f, 200)]
    [InlineData(37.1f, 984)]
    [InlineData(39.5f, 600)]
    [InlineData(42f, 200)]
    public void ItsSpeedFallsStraightFromWholeAtNormalToTheLeastAtEitherDeath(float degrees, int speed) =>
        Assert.Equal(speed, BodyHeat.SpeedOf(degrees));

    [Fact]
    public void EveryTenthOfADegreeSlowsItFurther()
    {
        for (float d = 37; d > 28.05f; d -= 0.1f) Assert.True(BodyHeat.SpeedOf(d - 0.1f) < BodyHeat.SpeedOf(d), $"{d}");
        for (float d = 37; d < 41.95f; d += 0.1f) Assert.True(BodyHeat.SpeedOf(d + 0.1f) < BodyHeat.SpeedOf(d), $"{d}");
    }
}
