using Stillwake.Game.Features.Research;
using Xunit;

namespace Stillwake.Game.Tests;

public class BulbSignalTests
{
    private readonly object _board = new();

    /// <summary>A signal with its first read taken: nothing new yet.</summary>
    private BulbSignal Started()
    {
        var bulb = new BulbSignal();
        bulb.Read(_board, 0, open: false);
        return bulb;
    }

    [Fact]
    public void WhatWaitedAtTheFirstReadLightsNothing()
    {
        var bulb = new BulbSignal();
        bulb.Read(_board, 3, open: false);
        Assert.Null(bulb.Time);
    }

    [Fact]
    public void ANewNoteLightsIt()
    {
        var bulb = Started();
        bulb.Read(_board, 1, open: false);
        Assert.Equal(0, bulb.Time);
    }

    [Fact]
    public void FewerNotesOrNoChangeLightNothing()
    {
        var bulb = new BulbSignal();
        bulb.Read(_board, 2, open: false);
        bulb.Read(_board, 2, open: false);
        bulb.Read(_board, 1, open: false);
        Assert.Null(bulb.Time);
    }

    [Fact]
    public void AnotherWorldsBoardStartsOverWithNothingNew()
    {
        var bulb = Started();
        bulb.Read(new object(), 5, open: false);
        Assert.Null(bulb.Time);
    }

    [Fact]
    public void ANoteFoundWithTheTabletOpenLightsNothing()
    {
        var bulb = Started();
        bulb.Read(_board, 1, open: true);
        bulb.Read(_board, 1, open: false);
        Assert.Null(bulb.Time);
    }

    [Fact]
    public void OpeningTheTabletPutsItOut()
    {
        var bulb = Started();
        bulb.Read(_board, 1, open: false);
        bulb.Step(0.5);
        bulb.Read(_board, 1, open: true);
        Assert.Null(bulb.Time);
    }

    [Fact]
    public void ItBurstsOnceAndIsGone()
    {
        var bulb = Started();
        bulb.Read(_board, 1, open: false);
        bulb.Step(BulbSignal.Burns + BulbSignal.Gone - 0.01);
        Assert.NotNull(bulb.Time);
        bulb.Step(0.02);
        Assert.Null(bulb.Time);
    }

    [Fact]
    public void AFindWhileItBurnsKeepsTheOneBurningLonger()
    {
        var bulb = Started();
        bulb.Read(_board, 1, open: false);
        bulb.Step(0.9);
        bulb.Read(_board, 2, open: false);
        Assert.Equal(0.9, bulb.Time);
        Assert.Equal(0.9 + BulbSignal.BurnsOn, bulb.BurstsAt);
    }

    [Fact]
    public void AnEarlyFindDoesNotShortenIt()
    {
        var bulb = Started();
        bulb.Read(_board, 1, open: false);
        bulb.Step(0.1);
        bulb.Read(_board, 2, open: false);
        Assert.Equal(BulbSignal.Burns, bulb.BurstsAt);
    }

    [Fact]
    public void AFindAfterTheBurstAddsNothing()
    {
        var bulb = Started();
        bulb.Read(_board, 1, open: false);
        bulb.Step(BulbSignal.Burns + 0.1);
        bulb.Read(_board, 2, open: false);
        Assert.Equal(BulbSignal.Burns, bulb.BurstsAt);
        Assert.Equal(BulbSignal.Burns + 0.1, bulb.Time);
    }

    [Fact]
    public void AFindAfterItHasGoneLightsItAgain()
    {
        var bulb = Started();
        bulb.Read(_board, 1, open: false);
        bulb.Step(BulbSignal.Burns + BulbSignal.Gone + 0.1);
        bulb.Read(_board, 2, open: false);
        Assert.Equal(0, bulb.Time);
        Assert.Null(bulb.Later);
    }

    [Theory]
    [InlineData(0.1, 0)]
    [InlineData(HalfWarm, 0.5)]
    [InlineData(1.0, 1)]
    [InlineData(BulbSignal.Burns, 0)]
    public void HeatRisesAndEndsAtTheBurst(double t, double heat) => Assert.Equal(heat, new BulbSignal().Heat(t), 3);

    private const double HalfWarm = (BulbSignal.DrawnBy + BulbSignal.HotBy) / 2;

    [Fact]
    public void ItStuttersJustBeforeTheBurst()
    {
        var bulb = new BulbSignal();
        var heats = Enumerable.Range(0, 20).Select(i => bulb.Heat(BulbSignal.Burns - 0.2 + i * 0.01)).ToList();
        Assert.Contains(heats, h => h < 1);
        Assert.Contains(heats, h => h == 1);
        Assert.All(Enumerable.Range(0, 20), i => Assert.Equal(1, bulb.Heat(BulbSignal.Burns - 0.5 + i * 0.01)));
    }

    [Fact]
    public void ALaterFindDipsTheFilamentAndItCatchesAgain()
    {
        var bulb = Started();
        bulb.Read(_board, 1, open: false);
        bulb.Step(0.8);
        bulb.Read(_board, 2, open: false);
        Assert.True(bulb.Heat(0.85) < 0.5);
        Assert.Equal(1, bulb.Heat(1.0));
    }

    [Theory]
    [InlineData(0.0, null)]
    [InlineData(0.1, 0)]
    [InlineData(0.3, 1)]
    [InlineData(0.5, 2)]
    [InlineData(0.8, 3)]
    [InlineData(1.0, 4)]
    public void TheFilamentGoesInHardSteps(double heat, int? picture) => Assert.Equal(picture, BulbSignal.FireOf(heat));

    [Fact]
    public void ShardsFallToTheFloorAndGo()
    {
        var late = BulbSignal.Shard(0, 0.4, afloat: false, floor: 5);
        var never = Enumerable.Range(0, BulbSignal.Shards).Select(i => BulbSignal.Shard(i, 0.9, afloat: false, floor: 5));
        Assert.NotNull(late);
        Assert.True(late!.Value.Y <= 5);
        Assert.All(never, Assert.Null);
    }

    [Fact]
    public void AfloatShardsDoNotFall()
    {
        var flying = BulbSignal.Shard(1, 0.4, afloat: true, floor: 0)!.Value;
        var falling = BulbSignal.Shard(1, 0.4, afloat: false, floor: 1000)!.Value;
        Assert.True(falling.Y > flying.Y);
    }

    [Fact]
    public void SmokeShowsOnlyForItsMoment()
    {
        Assert.Equal(0, BulbSignal.SmokeAlpha(0.05));
        Assert.Equal(1, BulbSignal.SmokeAlpha(0.2));
        Assert.Equal(0, BulbSignal.SmokeAlpha(0.7));
    }

    [Fact]
    public void TheFindsGatheredWhileItBurnsFlyUntilTheOneThatHatchedLands()
    {
        var bulb = Started();
        bulb.Read(_board, 1, open: false);
        bulb.Step(0.9);
        bulb.Read(_board, 3, open: false);
        Assert.Equal(3, bulb.Carried);
        Assert.Equal(3, bulb.Flying);
        Assert.Null(bulb.Flight);
        bulb.Step(bulb.BurstsAt - 0.9 + BulbSignal.FlightS / 2);
        Assert.Equal(0.5, bulb.Flight!.Value, 6);
        Assert.Equal(3, bulb.Flying);
        bulb.Step(BulbSignal.FlightS / 2 + 0.01);
        Assert.Null(bulb.Flight);
        Assert.Equal(0, bulb.Flying);
    }

    [Fact]
    public void AFindAfterTheBurstIsNotCarriedSoTheNumberTakesItAtOnce()
    {
        var bulb = Started();
        bulb.Read(_board, 1, open: false);
        bulb.Step(BulbSignal.Burns + 0.1);
        bulb.Read(_board, 2, open: false);
        Assert.Equal(1, bulb.Flying);
    }

    [Fact]
    public void OpeningTheTabletMidFlightLandsNothingAndShowsTheRealCount()
    {
        var bulb = Started();
        bulb.Read(_board, 2, open: false);
        bulb.Step(BulbSignal.Burns + 0.2);
        bulb.Read(_board, 2, open: true);
        Assert.Equal(0, bulb.Flying);
        Assert.Null(bulb.Flight);
    }
}
