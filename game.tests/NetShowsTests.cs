using Stillwake.Game.Features.Debris;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>A debris net drawn by how many it holds (docs/decisions/2026-11-03-debris-net.md), and its count by the pointer.</summary>
public class NetShowsTests
{
    /// <summary>The footprint in the world's pixels: 2 cells by 6.</summary>
    private const float Wide = 16, Tall = 48;

    [Fact]
    public void HasAPlaceForEveryPieceAFullNetHoldsAndAFrontForEveryCount()
    {
        Assert.Equal(Spec.Debris.NetHolds + 1, NetInk.Places.Length);
        for (int held = 0; held <= Spec.Debris.NetHolds; held++)
        {
            Assert.Equal(held, NetInk.Places[held].Length);
            Assert.True(Png.Has(NetShows.FrontOf(held)), NetShows.FrontOf(held));
        }
        Assert.True(Png.Has("net_mount"));
    }

    [Fact]
    public void LaysEveryPieceInsideTheFootprint()
    {
        foreach (var row in NetInk.Places)
            foreach (var (x, y, _) in row)
                Assert.True(x > 0 && x < Wide && y > 0 && y < Tall, $"({x}, {y})");
    }

    [Fact]
    public void SagsLowerAndInTowardTheHullAsItFills()
    {
        for (int held = 1; held < Spec.Debris.NetHolds; held++)
            for (int k = 0; k < held; k++)
            {
                var (x, y, _) = NetShows.Place(held, k);
                var (fullerX, fullerY, _) = NetShows.Place(held + 1, k);
                Assert.True(fullerY >= y && fullerX <= x, $"piece {k} from {held} to {held + 1}");
            }
        Assert.NotEqual(NetShows.Place(1, 0), NetShows.Place(Spec.Debris.NetHolds, 0));
    }

    [Fact]
    public void AFullerNetThanItHoldsIsDrawnFull() =>
        Assert.Equal(NetShows.FrontOf(Spec.Debris.NetHolds), NetShows.FrontOf(Spec.Debris.NetHolds + 3));

    [Fact]
    public void TheNetTellsItsCountOfWhatItHolds() =>
        Assert.Equal(["7/10"], NetShows.Says(Spec.Machines.Get("net"), 7)!);

    [Fact]
    public void SaysNothingOfAnyOtherMachine() => Assert.Null(NetShows.Says(Spec.Machines.Get("crate"), 7));
}
