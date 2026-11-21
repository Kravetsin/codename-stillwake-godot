using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Which drone window the soot frames: one at a time, the newest open (drone-frame).</summary>
public class SootClaimsTests
{
    private static readonly object Bag = new(), Board = new();
    private static readonly (float X, float Y)[] BagOutline = [(0, 0), (380, 0), (380, 200), (0, 200)];
    private static readonly (float X, float Y)[] BoardOutline = [(10, 10), (1810, 10), (1810, 1010), (10, 1010)];

    [Fact]
    public void NewestOpenWindowOwnsTheSoot()
    {
        var soot = new SootClaims();
        soot.Hand(Bag, BagOutline, false);
        Assert.Null(soot.Owner);
        soot.Hand(Bag, BagOutline, true);
        Assert.Same(Bag, soot.Owner);
        Assert.True(soot.Open);
        soot.Hand(Board, BoardOutline, true);
        Assert.Same(Board, soot.Owner);
        Assert.Same(BoardOutline, soot.Outline);
        // The one it took from, still open a frame on, takes nothing back.
        soot.Hand(Bag, BagOutline, true);
        Assert.Same(Board, soot.Owner);
        Assert.Same(BoardOutline, soot.Outline);
    }

    [Fact]
    public void TheOneItTookFromIsTold()
    {
        var soot = new SootClaims();
        var told = new List<object>();
        soot.Taken += told.Add;
        soot.Hand(Bag, BagOutline, true);
        soot.Hand(Board, BoardOutline, true);
        soot.Hand(Bag, BagOutline, false);
        soot.Hand(Board, BoardOutline, true);
        Assert.Equal([Bag], told);
    }

    [Fact]
    public void ClosingReleases()
    {
        var soot = new SootClaims();
        var told = new List<object>();
        soot.Taken += told.Add;
        soot.Hand(Bag, BagOutline, true);
        soot.Hand(Bag, BagOutline, false);
        // Closed, it keeps its outline for the soot flying home.
        Assert.Same(Bag, soot.Owner);
        Assert.False(soot.Open);
        Assert.Same(BagOutline, soot.Outline);
        soot.Hand(Board, BoardOutline, true);
        Assert.Same(Board, soot.Owner);
        Assert.Empty(told);
        // Opened again, the bag is the newest.
        soot.Hand(Board, BoardOutline, true);
        soot.Hand(Bag, BagOutline, true);
        Assert.Same(Bag, soot.Owner);
        Assert.Equal([Board], told);
    }
}
