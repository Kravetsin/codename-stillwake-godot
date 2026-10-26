using Stillwake.Game.Features.Sound;
using Stillwake.Game.Shared;
using Xunit;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Tests;

/// <summary>How thick a planet's fog is (fog-edges): by column, over the screen, flashed whole at a move; and the sound dulled in it.</summary>
public class FogShareTests
{
    /// <summary>A planet 128 wide with bands of 8 and 4 columns past each: its seams at 4 and 124, its zone's sides at 8 and 120.</summary>
    private static readonly FogEdges Edges = new(128, 8, 6, 4, 30, 32);

    [Fact]
    public void TheColumnsFogIsWholeOverABandThinningPastItToTheEdgeAndNoneFurtherIn()
    {
        Assert.Equal(0, FogShare.Column(Edges, 64));
        Assert.Equal(0, FogShare.Column(Edges, 12 + FogLooks.Lead));
        // Whole over the band and at the zone's side: its hollow never shows.
        Assert.Equal(1, FogShare.Column(Edges, 8));
        Assert.Equal(1, FogShare.Column(Edges, 120));
        Assert.InRange(FogShare.Column(Edges, 10), FogLooks.Edge, 1);
        // Where a moved hero falls out of it.
        Assert.Equal(FogLooks.Edge, FogShare.Column(Edges, 12), 9);
        Assert.Equal(FogLooks.Edge, FogShare.Column(Edges, 116), 9);
        Assert.Equal(1, FogShare.Column(Edges, 4));
        Assert.Equal(1, FogShare.Column(Edges, 124));
        Assert.Equal(1, FogShare.Column(Edges, -3));
        Assert.Equal(1, FogShare.Column(Edges, 131));
        double was = 0;
        for (double x = 12 + FogLooks.Lead; x >= 0; x -= 0.25)
        {
            double now = FogShare.Column(Edges, x);
            Assert.True(now >= was, $"thinner at {x}");
            was = now;
        }
    }

    [Fact]
    public void TheScreensFogIsNoneInTheZoneAndWholeAtTheSeam()
    {
        Assert.Equal(0, FogShare.Screen(Edges, 8));
        Assert.Equal(0, FogShare.Screen(Edges, 100));
        Assert.Equal(1, FogShare.Screen(Edges, 4));
        Assert.Equal(1, FogShare.Screen(Edges, 2));
        Assert.InRange(FogShare.Screen(Edges, 6), 0.1, 0.9);
    }

    [Fact]
    public void AMoveAcrossTheFogHoldsTheScreenWholeThenEasesAndNothingElseFlashes()
    {
        var screen = new FogScreen(Edges);
        // The first frame and a jump with the fog on neither side, a beam-down or a wake at the spawn: no flash.
        Assert.Equal(0, screen.Step(40, 16));
        Assert.Equal(0, screen.Step(110, 16));
        Assert.Equal(0, screen.Step(110, 16));
        // A move: from the left seam to the right.
        Assert.Equal(1, screen.Step(4.5, 16), 9);
        Assert.Equal(1, screen.Step(123.5, 16));
        for (double ms = 16; ms < FogLooks.FlashMs; ms += 16) Assert.Equal(1, screen.Step(123.5, 16));
        // Then eased back to the fog where it stands, never thinner.
        double settled = FogShare.Screen(Edges, 123.5), last = 1;
        for (double ms = 0; ms < FogLooks.FadeMs + 100; ms += 16)
        {
            double now = screen.Step(123.5, 16);
            Assert.InRange(now, settled, last);
            last = now;
        }
        Assert.Equal(settled, last, 9);
    }

    [Fact]
    public void TheFogDullsTheWorldBusAsSpaceDoesAndNothingElse()
    {
        var fogged = Hearing.At(0, 1);
        Assert.Equal(S.MuffledHz, fogged.WorldHz, 3);
        Assert.Equal(1, fogged.WorldGain);
        Assert.Equal(S.ClearHz, fogged.SuitHz, 3);
        Assert.Equal((1, 0), (fogged.SuitLows, fogged.SuitDb));
        Assert.Equal(Hearing.At(1).WorldHz, Hearing.At(0.3, 1).WorldHz, 3);
    }
}
