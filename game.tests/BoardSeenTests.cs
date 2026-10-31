using System;
using System.IO;
using System.Linq;
using Stillwake.Game.Features.Research;
using Xunit;

namespace Stillwake.Game.Tests;

public sealed class BoardSeenTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public void Dispose()
    {
        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }

    /// <summary>The notes of a world that has obtained <paramref name="found"/> and knows <paramref name="granted"/>, as the board works them out.</summary>
    private static System.Collections.Generic.HashSet<string> Notes(string[] found, params string[] granted)
    {
        var read = new BoardRead(found, granted, _ => 1, _ => false);
        return BoardView.NotesOf(BoardSlots.Of(found, BoardThings.All, granted), read);
    }

    [Fact]
    public void ABareWorldHasNoDot()
    {
        var seen = new BoardSeen(null);
        seen.Read(Notes([]));
        Assert.False(seen.Lit);
    }

    [Fact]
    public void ANewItemLightsTheDot()
    {
        var seen = new BoardSeen(null);
        seen.Read(Notes([]));
        seen.Read(Notes(["scrapChunk"]));
        Assert.True(seen.Lit);
    }

    [Fact]
    public void ANewGrantWithANoteLightsTheDot()
    {
        var seen = new BoardSeen(null);
        seen.Read(Notes(["scrap"]));
        Assert.False(seen.Lit);
        string shape = BoardSlots.Of(["scrap"], BoardThings.All, []).Select(s => s.Id).First(id => id != "scrap");
        seen.Read(Notes(["scrap"], shape));
        Assert.True(seen.Lit);
        Assert.True(seen.Marked(shape));
    }

    [Fact]
    public void MoreOfAKnownItemLightsNothing()
    {
        var seen = new BoardSeen(["scrap"]);
        seen.Read(Notes(["scrap"]));
        seen.Read(Notes(["scrap"]));
        Assert.False(seen.Lit);
    }

    [Fact]
    public void BuyingABagLightsNothing()
    {
        var seen = new BoardSeen(null);
        seen.Read(Notes(["scrap"]));
        seen.Read(Notes(["scrap"], "bag1"));
        Assert.False(seen.Lit);
        Assert.False(seen.Marked("bag1"));
    }

    [Fact]
    public void OpeningPutsOutTheDotAndTheMarksStayUntilItCloses()
    {
        var seen = new BoardSeen([]);
        seen.Read(["scrap"]);
        seen.Opened();
        Assert.False(seen.Lit);
        Assert.True(seen.Marked("scrap"));
        seen.Closed();
        Assert.False(seen.Lit);
        Assert.False(seen.Marked("scrap"));
    }

    [Fact]
    public void WhatArrivesWhileItIsOpenIsSeenWhenItCloses()
    {
        var seen = new BoardSeen([]);
        seen.Opened();
        seen.Read(["scrap"]);
        Assert.True(seen.Marked("scrap"));
        seen.Closed();
        Assert.False(seen.Lit);
    }

    [Fact]
    public void AJoinedGuestHasNoMarks()
    {
        var seen = new BoardSeen(null);
        seen.Read(["scrap", "iron", "wire"]);
        Assert.False(seen.Lit);
        Assert.False(seen.Marked("iron"));
    }

    [Fact]
    public void WhatWasNewIsStillNewAfterARelaunch()
    {
        var keep = new BoardKeep(_folder);
        keep.Of(7);
        keep.Seen.Read([]);
        Assert.True(keep.Write());
        keep.Seen.Read(["scrap"]);
        var again = new BoardKeep(_folder);
        again.Of(7);
        again.Seen.Read(["scrap"]);
        Assert.True(again.Seen.Lit);
    }

    [Fact]
    public void WhatWasMovedAndSeenComesBackForItsWorldOnly()
    {
        var keep = new BoardKeep(_folder);
        keep.Of(7);
        keep.Move("scrap", new BoardSpot(1, 2));
        keep.Seen.Read(["scrap"]);
        Assert.True(keep.Write());
        var again = new BoardKeep(_folder);
        again.Of(7);
        Assert.Equal(new BoardSpot(1, 2), again.Spots["scrap"]);
        again.Seen.Read(["scrap"]);
        Assert.False(again.Seen.Lit);
        again.Of(8);
        Assert.Empty(again.Spots);
    }

    [Fact]
    public void AWorldWithNoIdKeepsNoFile()
    {
        var keep = new BoardKeep(_folder);
        keep.Move("scrap", new BoardSpot(1, 2));
        keep.Seen.Read(["scrap"]);
        Assert.True(keep.Write());
        Assert.False(Directory.Exists(_folder));
    }
}
