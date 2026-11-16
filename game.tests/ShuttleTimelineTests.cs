using Stillwake.Game.Features.Landing;
using Stillwake.Sim;
using Xunit;
using T = Stillwake.Game.Features.Landing.ShuttleTimeline;

namespace Stillwake.Game.Tests;

/// <summary>The crew shuttle's trip as «A — Под днищем»: where it is, its skids, flames and its rider's camera at each beat's edges.</summary>
public class ShuttleTimelineTests
{
    private const double Eps = 1e-9;

    [Fact]
    public void AboardItStandsWhileItsRunSlidesOpen()
    {
        var start = T.Leaving(0, true);
        Assert.Equal((0.0, 0.0, 0), (start.X, start.Y, start.Fold));
        Assert.Equal(0, start.Stern0 + start.Stern1 + start.Stern2 + start.Retro);
        Assert.Equal(0, start.Blend);
        Assert.Equal(0, start.Open!.Value);
        Assert.Equal(0, T.Leaving(T.OpenFrom, true).Open!.Value);
        Assert.Equal(1, T.Leaving(T.OpenTo, true).Open!.Value);
        var sinking = T.Leaving(T.SinkFrom, true);
        Assert.Equal((0.0, 0.0, 0), (sinking.X, sinking.Y, sinking.Fold));
        Assert.Equal(1, sinking.Open!.Value);
    }

    [Fact]
    public void ItSinksThroughTheRunFoldingItsSkidsAndTheRunShutsByTheSimsTick()
    {
        var sunk = T.Leaving(T.SinkTo - Eps, true);
        Assert.Equal(T.SinkPx, sunk.Y, 6);
        Assert.Equal(2, sunk.Fold);
        // Its flame kicks in as it clears the hull, the far pod's nozzle cold.
        Assert.Equal((0.0, 0.3, 0.3), (sunk.Stern0, sunk.Stern1, sunk.Stern2));
        Assert.Equal(1, T.Leaving(T.SinkFrom + (T.SinkTo - T.SinkFrom) * 0.35, true).Fold);
        Assert.Equal(1, T.Leaving(T.ShutFrom, true).Open!.Value);
        Assert.Equal(0, T.Leaving(T.ShutTo - Eps, true).Open!.Value, 6);
        Assert.Null(T.Leaving(T.ShutTo, true).Open);
        Assert.Equal(Content.Shuttle.FallTicks, T.ShutTo * World.TicksPerSecond, 9);
        // The run is drawn shut over the sim's only once the craft is wholly below it.
        Assert.True(T.Leaving(T.ShutFrom, true).Y >= T.SinkPx);
    }

    [Fact]
    public void ItFliesOffRightUnderTheBellyTheCameraPullingOutWithNoJump()
    {
        Assert.Equal(T.Leaving(T.SinkTo - Eps, true).Blend, T.Leaving(T.SinkTo, true).Blend, 6);
        var under = T.Leaving(T.SinkTo, true);
        Assert.Equal((0.0, T.SinkPx, 2), (under.X, under.Y, under.Fold));
        var gone = T.Leaving(T.PassTo, true);
        Assert.Equal(T.PassPx, gone.X, 6);
        Assert.Equal(1, gone.Blend);
        Assert.True(gone.Stern0 > 0.8 && gone.Stern2 > 0.9);
        Assert.Equal(1, T.Leaving(T.SinkTo + (T.PassTo - T.SinkTo) / 2, true).Blend);
        // The rider's screen cuts away as it is gone, and the lines run some four seconds of the voyage before it arrives.
        Assert.Equal(T.PassTo, T.DipPeak);
        Assert.Equal(1, T.Dip(T.DipPeak));
        Assert.Equal(0, T.Dip(T.DipFrom));
        Assert.Equal(0, T.Dip(T.DipTo));
        Assert.InRange(Content.Shuttle.FlightTicks / (double)World.TicksPerSecond - T.DipPeak, 4, 5);
    }

    [Fact]
    public void OffTheShipItLiftsOffFoldingItsSkidsAndFliesOffRightAsItDoesAboard()
    {
        foreach (double s in new[] { 0, T.SinkFrom, T.SinkTo, T.PassTo })
        {
            var m = T.Leaving(s, false);
            Assert.Null(m.Open);
            Assert.Equal(T.Leaving(s, true).Blend, m.Blend);
            Assert.Equal(T.Leaving(s, true).X, m.X);
        }
        var standing = T.Leaving(T.SinkFrom, false);
        Assert.Equal((0.0, 0.0, 0), (standing.X, standing.Y, standing.Fold));
        var lifted = T.Leaving(T.SinkTo - Eps, false);
        Assert.Equal(-T.LiftPx, lifted.Y, 6);
        Assert.Equal(2, lifted.Fold);
        Assert.True(lifted.Stern1 > 0 && lifted.Stern2 > 0);
        Assert.Equal(1, T.Leaving(T.SinkFrom + (T.SinkTo - T.SinkFrom) * 0.35, false).Fold);
        var gone = T.Leaving(T.PassTo, false);
        Assert.Equal(T.PassPx, gone.X, 6);
        Assert.True(gone.Stern0 > 0.8 && gone.Stern2 > 0.9);
    }

    [Fact]
    public void ItsOwnLightFadesInAsItSetsOffAndOutAsItSettles()
    {
        foreach (bool aboard in new[] { true, false })
        {
            Assert.Equal(0, T.Leaving(0, aboard).Glow);
            Assert.Equal(1, T.Leaving(T.SinkFrom, aboard).Glow);
            Assert.Equal(1, T.Arriving(0, aboard).Glow);
            Assert.Equal(0, T.Arriving(T.Hold(aboard), aboard).Glow);
        }
        // Home it keeps its light up the run, through a dark hangar, and lets it go as the run shuts under it.
        Assert.Equal(1, T.Arriving(T.RiseTo, true).Glow);
        Assert.Equal(0, T.Arriving(T.HomeShutTo, true).Glow);
    }

    [Fact]
    public void ByTheVesselItComesInFromTheFarLeftOnItsRetrosAndTheCameraClosesIn()
    {
        var first = T.Arriving(0, false);
        Assert.Equal(-T.DriftPx, first.X);
        Assert.Equal((2, 1.0), (first.Fold, first.Blend));
        Assert.True(first.Stern2 > 0);
        // The drift runs into the approach without a jump.
        var drifted = T.Arriving(T.DriftS - Eps, false);
        var braking = T.Arriving(T.DriftS, false);
        Assert.Equal(drifted.X, braking.X, 4);
        Assert.Equal(drifted.Y, braking.Y, 4);
        Assert.Equal(-T.ApproachPx, braking.X);
        var retros = T.Arriving(T.DriftS + T.ApproachS * 0.5, false);
        Assert.True(retros.Retro > 0);
        Assert.Equal(0, retros.Stern0 + retros.Stern1 + retros.Stern2);
        Assert.Equal(2, retros.Fold);
        var stopped = T.Arriving(T.DriftS + T.ApproachS, false);
        Assert.Equal((0.0, 0.0, 0, 0.0), (stopped.X, stopped.Y, stopped.Fold, stopped.Retro));
        Assert.Equal(1, stopped.Blend);
        Assert.Equal(0.5, T.Arriving(T.DriftS + T.ApproachS + T.SettleS / 2, false).Blend, 9);
        Assert.Equal(0, T.Arriving(T.Hold(false), false).Blend);
        Assert.Null(T.Arriving(T.Hold(false), false).Open);
    }

    [Fact]
    public void HomeItComesUnderTheBellyAndRisesThroughItsRunDrawnOpen()
    {
        var first = T.Arriving(0, true);
        Assert.Equal(-T.DriftPx, first.X);
        Assert.Null(first.Open);
        var below = T.Arriving(T.HomeOpenFrom, true);
        // Wholly below the run as its halves begin to part: its 72 px craft under the run's 8.
        Assert.InRange(below.Y, 80, T.SinkPx);
        Assert.Equal(2, below.Fold);
        var rising = T.Arriving(T.RiseFrom, true);
        Assert.Equal((0.0, T.SinkPx, 2), (rising.X, rising.Y, rising.Fold));
        Assert.Equal(1, rising.Open!.Value);
        Assert.Equal(1, rising.Blend);
        var up = T.Arriving(T.RiseTo, true);
        Assert.Equal((0.0, 0.0, 0), (up.X, up.Y, up.Fold));
        Assert.Equal(1, up.Open!.Value);
        var home = T.Arriving(T.Hold(true), true);
        Assert.Equal((0.0, 0.0, 0), (home.X, home.Y, home.Fold));
        Assert.Null(home.Open);
        Assert.Equal(0, home.Blend);
        Assert.Equal(T.HomeSettleTo, T.Hold(true));
        Assert.Null(T.Arriving(T.HomeShutTo, true).Open);
        Assert.True(T.Arriving(T.HomeShutTo, true).Blend > 0);
    }

    [Fact]
    public void EveryMomentShowsADrawnSkidFrameAndABlendInRange()
    {
        for (double s = 0; s < 10; s += 0.01)
            foreach (bool aboard in new[] { true, false })
                foreach (var m in new[] { T.Leaving(s, aboard), T.Arriving(s, aboard) })
                {
                    Assert.InRange(m.Fold, 0, 2);
                    Assert.InRange(m.Blend, 0, 1);
                    if (m.Open is double open) Assert.InRange(open, 0, 1);
                }
    }

    [Fact]
    public void ItsMiddleIsTheCraftsMovedByTheMoment()
    {
        var (x, y) = T.Middle(3 * 100 + 5, 100, T.Leaving(T.PassTo, true));
        Assert.Equal(5 * 8 + 96 + T.PassPx, x, 6);
        Assert.Equal(3 * 8 + 44 + T.Leaving(T.PassTo, true).Y, y, 6);
    }

    [Fact]
    public void AShuttlesRideCutsToItsScreenAsItIsGone()
    {
        var ride = Ride.Of(new Stillwake.Sim.Landing.Voyage(0, [1], Content.Shuttle.FlightTicks, 0, "freighter"));
        Assert.True(ride.Shuttle);
        Assert.Equal(T.DipPeak, ride.ScreenFrom);
        Assert.Equal(0, ride.Fraction(T.DipFrom));
        Assert.Equal(1, ride.Fraction(Content.Shuttle.FlightTicks / (double)World.TicksPerSecond - 1), 9);
        Assert.Equal(T.SinkFrom, ride.BurnsFrom);
    }
}
