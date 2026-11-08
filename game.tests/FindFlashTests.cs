using Stillwake.Game.Features.Research;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The flash along the tablet's edge as notes start waiting: one for a batch.</summary>
public class FindFlashTests
{
    private static readonly object Board = new();

    /// <summary>A flash that has seen <paramref name="unread"/> waiting already, with the tablet away.</summary>
    private static FindFlash Seen(int unread = 0)
    {
        var flash = new FindFlash();
        flash.Read(Board, unread, open: false);
        return flash;
    }

    [Fact]
    public void WhatWaitedWhenTheWorldOpenedFlashesNothing()
    {
        var flash = Seen(5);
        Assert.Null(flash.Time);
    }

    [Fact]
    public void AFindRunsTheLightInAndTheNumberTakesItAsItLands()
    {
        var flash = Seen();
        flash.Read(Board, 1, open: false);
        Assert.Equal(0, flash.Wave);
        Assert.Equal(1, flash.Holding);
        flash.Step(FindFlash.WaveS);
        Assert.Null(flash.Wave);
        Assert.Equal(0, flash.Holding);
        Assert.Equal(1, flash.Glow);
        flash.Step(FindFlash.GlowS);
        Assert.Equal(0, flash.Glow);
    }

    [Fact]
    public void FindsWhileItRunsJoinItAndFindsSoonAfterFlashNothing()
    {
        var flash = Seen();
        flash.Read(Board, 1, open: false);
        flash.Step(FindFlash.WaveS / 2);
        flash.Read(Board, 4, open: false);
        Assert.Equal(4, flash.Holding);
        flash.Step(FindFlash.WaveS);
        flash.Read(Board, 6, open: false);
        Assert.Null(flash.Wave);
        Assert.Equal(0, flash.Holding);
    }

    [Fact]
    public void AFindOnceItHasBeenQuietFlashesAgain()
    {
        var flash = Seen();
        flash.Read(Board, 1, open: false);
        flash.Step(FindFlash.QuietS);
        Assert.Null(flash.Time);
        flash.Read(Board, 2, open: false);
        Assert.Equal(0, flash.Wave);
    }

    [Fact]
    public void OpeningTheTabletPutsItOutAndAFindWhileOpenFlashesNothing()
    {
        var flash = Seen();
        flash.Read(Board, 1, open: false);
        flash.Read(Board, 1, open: true);
        Assert.Null(flash.Time);
        Assert.Equal(0, flash.Holding);
        flash.Read(Board, 2, open: true);
        Assert.Null(flash.Time);
    }

    [Fact]
    public void AnotherWorldsBoardStartsOverWithNothingNew()
    {
        var flash = Seen();
        flash.Read(new object(), 9, open: false);
        Assert.Null(flash.Time);
    }

    [Fact]
    public void ReadingNotesFlashesNothing()
    {
        var flash = Seen(3);
        flash.Read(Board, 1, open: false);
        Assert.Null(flash.Time);
    }
}
