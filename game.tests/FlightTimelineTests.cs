using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Game.Features.Landing;
using Stillwake.Sim;
using Stillwake.Sim.Landing;
using Xunit;
using T = Stillwake.Game.Features.Landing.FlightTimeline;

namespace Stillwake.Game.Tests;

/// <summary>When each picture of a drop pod's flight plays: from the flight's ticks, the deck, the fall, the screen, the touchdown.</summary>
public class FlightTimelineTests
{
    private static Flight Down(int ticksLeft) => new(0, false, ticksLeft, 0, 0, null);

    [Fact]
    public void CountsSecondsFromTheLaunchTickAndScalesWithTheFlightsTicks()
    {
        Assert.Equal(0, T.Seconds(Down(Content.Landing.DescentTicks), 0));
        Assert.Equal(1.0, T.Seconds(Down(Content.Landing.DescentTicks - 20), 0), 9);
        Assert.Equal(1.5, T.Seconds(Down(Content.Landing.DescentTicks - 20), 10), 9);
        Assert.Equal(Content.Landing.DescentTicks / 20.0, T.Length(Down(0)));
        Assert.Equal(Content.Landing.AscentTicks / 20.0, T.Length(new Flight(0, true, 0, 0, 0, null)));
        // The screen's windows are fractions of the flight: the same fraction falls later in a longer one.
        Assert.True(T.Fraction(4, 10) < T.Fraction(4, 8));
    }

    [Fact]
    public void OpensTheDeckFallsThePodAndShutsTheDeckByTheSimsShutTick()
    {
        Assert.Equal(T.DropTo * World.TicksPerSecond, Content.Landing.FallTicks);
        Assert.Equal(0, T.Open(0));
        Assert.Equal(1, T.Open(T.OpenS));
        Assert.Equal(1, T.Open(T.ShutFrom));
        Assert.Equal(0, T.Open(T.DropTo));
        // The pod is below the deck, its 56 px clear, when the deck starts to shut: 62 px down.
        Assert.InRange(T.Drop(T.ShutFrom), 56, 64);
        Assert.Equal(T.DropPx, T.Drop(T.DropTo));
        // It falls on three times as long as it takes to clear the deck, gathering speed, far under the ship.
        Assert.Equal(9 * T.DropPx, T.Drop(T.FallShownTo), 6);
        Assert.Equal(9 * T.DropPx, T.Drop(T.FallShownTo + 5), 6);
        Assert.Equal(T.RisePx, T.Lift(T.RiseS), 6);
        Assert.Equal(9 * T.RisePx, T.Lift(T.LiftS), 6);
    }

    [Fact]
    public void DipsToBlackAtTheCutAndFliesDownThroughSpaceAloneUpOutOfTheAir()
    {
        Assert.Equal(0, T.Dip(T.DipFrom));
        Assert.Equal(1, T.Dip(T.DipPeak), 9);
        Assert.Equal(0, T.Dip(T.DipTo), 9);
        Assert.All(new[] { 0.0, 0.3, 0.5, 0.7, 1.0 }, k => Assert.Equal(0, T.Air(k, up: false)));
        Assert.True(T.Air(0, up: true) > 0.99 && T.Air(1, up: true) == 0);
    }

    [Fact]
    public void FallsOnlyDownwardAndTouchesDownWithoutRisingBeforeItsBounce()
    {
        double[] fall = [.. Enumerable.Range(0, 11).Select(i => T.Drop(T.DropFrom + i * (T.DropTo - T.DropFrom) / 10))];
        Assert.True(fall.Zip(fall.Skip(1)).All(p => p.Second >= p.First));
        double[] down = [.. Enumerable.Range(0, 11).Select(i => T.Touchdown(i * T.BounceFrom / 10))];
        Assert.True(down.Zip(down.Skip(1)).All(p => p.Second <= p.First));
        Assert.Equal(T.FromPx, T.Touchdown(0));
        Assert.Equal(0, T.Touchdown(T.Release), 9);
        Assert.True(T.Touchdown((T.BounceFrom + T.BounceTo) / 2) > 0);
    }

    [Fact]
    public void PlaysAMomentOnceWhenPassedAndNeverOnTheFrameFirstSeen()
    {
        Assert.True(T.Crossed(0.29, 0.31, T.DropFrom));
        Assert.True(T.Crossed(0.29, T.DropFrom, T.DropFrom));
        Assert.False(T.Crossed(T.DropFrom, 0.4, T.DropFrom));
        Assert.False(T.Crossed(0.1, 0.2, T.DropFrom));
        Assert.False(T.Crossed(double.NaN, 0.5, T.DropFrom));
    }

    [Fact]
    public void BlastsTheGroundOnlyOnceTheFlamesTipIsNearItHardestAtContact()
    {
        Assert.Equal(0, T.Blast(high: 100, flame: T.Flame(10)));
        Assert.Equal(0, T.Blast(high: 16, flame: T.Flame(0)));
        Assert.Equal(1, T.Blast(high: 0, flame: T.Flame(0)));
        // A lift-off: hardest as it leaves the ground, none once well up.
        Assert.Equal(1, T.Blast(T.Lift(0), T.Flame(18)));
        Assert.Equal(0, T.Blast(T.Lift(T.RiseS), T.Flame(18)));
    }

    [Fact]
    public void FindsThePodsCellFromItsRidersFloor()
    {
        const int width = 40;
        foreach (int cell in new[] { 0, 5 * width + 7, 12 * width + 30 })
            Assert.Equal(cell, T.PodCellOf(LandingModule.FloorOf(cell, width), width));
    }

    [Property(MaxTest = 200)]
    public Property KeepsEveryCurveInItsRange()
    {
        var gen = from s in Gen.Choose(-100, 1200)
                  from up in Gen.Elements(true, false)
                  select (s / 100.0, up);
        return Prop.ForAll(gen.ToArbitrary(), t =>
        {
            var (s, up) = t;
            double k = T.Fraction(s, 8);
            bool unit(double v) => v >= 0 && v <= 1;
            return (unit(T.Open(s)) && unit(T.Dip(s)) && unit(k) && unit(T.Air(k, up))
                && T.Drop(s) >= 0 && T.Drop(s) <= 9 * T.DropPx && T.Touchdown(s) >= 0 && T.Touchdown(s) <= T.FromPx
                && T.Lift(s) >= 0 && T.Lift(s) <= 9 * T.RisePx).Label($"s {s} up {up}");
        });
    }
}
