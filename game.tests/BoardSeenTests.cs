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
        Assert.Contains(shape, seen.Unread);
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
    public void OpeningPutsOutTheDotAndALandedNoteIsMarkedUntilItIsTouched()
    {
        var seen = new BoardSeen([]);
        seen.Read(["scrap"]);
        seen.Opened();
        Assert.False(seen.Lit);
        Assert.False(seen.Marked("scrap"));
        seen.Land("scrap");
        Assert.True(seen.Marked("scrap"));
        seen.Closed();
        Assert.False(seen.Lit);
        Assert.True(seen.Marked("scrap"));
        seen.Touch("scrap");
        Assert.False(seen.Marked("scrap"));
    }

    [Fact]
    public void WhatLandedIsFreshOnlyUntilTheTabletCloses()
    {
        var seen = new BoardSeen([]);
        seen.Read(["scrap"]);
        seen.Opened();
        seen.Land("scrap");
        Assert.True(seen.Fresh("scrap"));
        seen.Closed();
        Assert.False(seen.Fresh("scrap"));
    }

    [Fact]
    public void AMarkLastsThroughARestartUntilTouched()
    {
        var keep = new BoardKeep(_folder);
        keep.Of(9);
        keep.Seen.Read([]);
        keep.Seen.Read(["scrap", "iron"]);
        keep.Seen.Land("scrap");
        keep.Seen.Land("iron");
        keep.Seen.Touch("iron");
        Assert.True(keep.Write());
        var again = new BoardKeep(_folder);
        again.Of(9);
        Assert.True(again.Seen.Marked("scrap"));
        Assert.False(again.Seen.Marked("iron"));
        again.Seen.Touch("scrap");
        Assert.True(again.Seen.Unwritten);
    }

    [Fact]
    public void AFileFromBeforeMarksHasNoneMarked()
    {
        Assert.True(BoardFile.Write(BoardFile.PathOf(_folder, 10), new BoardKept(null, ["scrap"])));
        var keep = new BoardKeep(_folder);
        keep.Of(10);
        Assert.False(keep.Seen.Marked("scrap"));
    }

    [Fact]
    public void ClosingUnreadLeavesTheDotLitAndTheNotesUnread()
    {
        var seen = new BoardSeen([]);
        seen.Read(["scrap", "iron"]);
        seen.Opened();
        seen.Land("scrap");
        seen.Closed();
        Assert.True(seen.Lit);
        Assert.Equal(["iron"], seen.Unread);
        Assert.Contains("scrap", seen.Seen);
    }

    [Fact]
    public void ANewlyMetOrderLightsTheMarketsKeyNotTheTablets()
    {
        var seen = new BoardSeen([]);
        seen.ReadOrders(1);
        Assert.False(seen.MarketNew);
        seen.ReadOrders(1);
        Assert.False(seen.MarketNew);
        seen.ReadOrders(2);
        Assert.True(seen.MarketNew);
        Assert.False(seen.Lit);
    }

    [Fact]
    public void AnOrderStaysNewUntilTheMarketIsChosenWhateverTheTabletDoes()
    {
        var seen = new BoardSeen([], 0);
        seen.ReadOrders(1);
        seen.Opened();
        Assert.True(seen.MarketNew);
        seen.Closed();
        Assert.True(seen.MarketNew);
        seen.Opened();
        Assert.Equal(0, seen.SeeOrders(1));
        Assert.False(seen.MarketNew);
        seen.Closed();
        Assert.False(seen.MarketNew);
    }

    [Fact]
    public void TheMarketShownSaysHowManyOrdersWereSeenAndPlaysNoneTwice()
    {
        var seen = new BoardSeen([], 1);
        Assert.Equal(1, seen.SeeOrders(3));
        Assert.Equal(3, seen.OrdersSeen);
        Assert.Equal(3, seen.SeeOrders(3));
        Assert.True(seen.Unwritten);
    }

    [Fact]
    public void TheKeysLagOverAnOrderAlreadySeenLightsNothing()
    {
        var seen = new BoardSeen([], 1);
        seen.SeeOrders(2);
        seen.ReadOrders(1);
        Assert.False(seen.MarketNew);
        seen.ReadOrders(2);
        Assert.False(seen.MarketNew);
    }

    [Fact]
    public void WithNothingKeptTheFirstShowingPlaysNothing()
    {
        var seen = new BoardSeen([]);
        Assert.Equal(4, seen.SeeOrders(4));
        Assert.False(seen.MarketNew);
    }

    [Fact]
    public void AnOlderWorldThanWasSeenPlaysNothing()
    {
        var seen = new BoardSeen([], 3);
        Assert.Equal(3, seen.SeeOrders(1));
        Assert.Equal(3, seen.OrdersSeen);
    }

    [Fact]
    public void AnOrderMetIsStillNewAfterARelaunchAndAFileFromBeforeOrdersStartsAsThingsStand()
    {
        var keep = new BoardKeep(_folder);
        keep.Of(7);
        keep.Seen.Read([]);
        keep.Seen.ReadOrders(1);
        Assert.True(keep.Write());
        var again = new BoardKeep(_folder);
        again.Of(7);
        again.Seen.Read([]);
        again.Seen.ReadOrders(2);
        Assert.True(again.Seen.MarketNew);
        Assert.True(BoardFile.Write(BoardFile.PathOf(_folder, 8), new BoardKept(null, [])));
        again.Of(8);
        again.Seen.ReadOrders(2);
        Assert.False(again.Seen.MarketNew);
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
    public void AThingCarriedPastTheBoardStopsAtItsEdge()
    {
        var keep = new BoardKeep(_folder);
        keep.Move("scrap", new BoardSpot(1e6, -1e6));
        Assert.Equal(new BoardSpot(BoardBox.CarryWide, -BoardBox.CarryTall), keep.Spots["scrap"]);
    }

    [Fact]
    public void AThingCanBeCarriedFarPastWhereSlotsEnd()
    {
        var keep = new BoardKeep(_folder);
        var far = new BoardSpot(BoardBox.ReachWide * 2, -BoardBox.ReachTall * 2);
        keep.Move("scrap", far);
        Assert.Equal(far, keep.Spots["scrap"]);
    }

    [Fact]
    public void AThingKeptPastTheBoardComesBackAtItsEdge()
    {
        BoardFile.Write(BoardFile.PathOf(_folder, 7), new BoardKept(new Dictionary<string, BoardSpot> { ["scrap"] = new(-1e6, 1e6), ["wire"] = new(3, 4) }));
        var keep = new BoardKeep(_folder);
        keep.Of(7);
        Assert.Equal(new BoardSpot(-BoardBox.CarryWide, BoardBox.CarryTall), keep.Spots["scrap"]);
        Assert.Equal(new BoardSpot(3, 4), keep.Spots["wire"]);
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
