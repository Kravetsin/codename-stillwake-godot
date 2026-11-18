using System;
using System.IO;
using System.Linq;
using Stillwake.Game.Features.Research;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>What is new on the board: batches of finds, each with the cause it came with (docs/plans/board-islands.md), and the market's orders.</summary>
public sealed class BoardSeenTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public void Dispose()
    {
        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }

    /// <summary>A world that has obtained <paramref name="found"/>, knows <paramref name="granted"/> and has <paramref name="standing"/> built.</summary>
    private static BoardRead Read(string[] found, string[] granted, params string[] standing) =>
        new(found, granted, found.Contains, standing.Contains);

    private static readonly BoardRead Nothing = Read([], []);

    /// <summary>What a board kept by a file read first: nothing seen yet, nothing known before.</summary>
    private static BoardSeen Kept() => new([]);

    [Fact]
    public void AFirstReadWithNoFileHasNoBatchAndSeesAllThatIsKnown()
    {
        var seen = new BoardSeen(null);
        seen.Read(Read(["scrapChunk", "scrap"], ["nail"], "workbench"));
        Assert.Empty(seen.Batches);
        Assert.Equal(0, seen.UnreadBatches);
        Assert.False(seen.Lit);
        Assert.Contains("nail", seen.Seen);
    }

    [Fact]
    public void AFindBringsWhatItOpensUnderOneCause()
    {
        var seen = Kept();
        seen.Read(Nothing);
        seen.Read(Read(["scrapChunk", "scrap"], ["nail", "wire"], "workbench"));
        var batch = Assert.Single(seen.Batches);
        Assert.Equal(BoardSeen.CauseFound, batch.Cause);
        Assert.Equal(["scrapChunk", "scrap"], batch.By);
        Assert.Equal(["nail", "scrap", "scrapChunk", "wire"], batch.Things.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void AFindNamesWhatItBroughtCloserAndALaterOneThatAddsNoPartDoesNot()
    {
        var seen = Kept();
        seen.Read(Nothing);
        seen.Read(Read(["scrap"], []));
        Assert.Contains("nail", Assert.Single(seen.Batches).Closer!);
        seen.Read(Read(["scrap", "rock"], []));
        Assert.Equal(2, seen.Batches.Count);
        Assert.DoesNotContain("nail", seen.Batches[^1].Closer!);
    }

    [Fact]
    public void ACraftGrantedOnceItsMachineStandsFormsABuiltBatch()
    {
        var seen = Kept();
        seen.Read(Read(["scrap"], []));
        seen.Read(Read(["scrap"], ["nail"], "workbench"));
        var batch = seen.Batches[^1];
        Assert.Equal(BoardSeen.CauseBuilt, batch.Cause);
        Assert.Equal(["workbench"], batch.By);
        Assert.Equal(["nail"], batch.Things);
    }

    [Fact]
    public void WhatCameWithNeitherAFindNorAMachineWasOpened()
    {
        var seen = Kept();
        seen.Read(Read(["scrap"], []));
        seen.Read(Read(["scrap"], ["solarPanel"]));
        Assert.Equal(BoardSeen.CauseOpened, seen.Batches[^1].Cause);
        Assert.Empty(seen.Batches[^1].By);
    }

    [Fact]
    public void MoreOfWhatIsKnownBringsNoBatch()
    {
        var seen = Kept();
        seen.Read(Read(["scrap"], []));
        int batches = seen.Batches.Count;
        seen.Read(Read(["scrap"], []));
        Assert.Equal(batches, seen.Batches.Count);
    }

    [Fact]
    public void HoveringNeverChangesABatchOnlyWhatIsUnread()
    {
        var seen = Kept();
        seen.Read(Nothing);
        seen.Read(Read(["scrap"], ["nail", "wire"], "workbench"));
        var batch = Assert.Single(seen.Batches);
        string[] things = [.. batch.Things];
        Assert.True(seen.Unread("nail"));
        Assert.True(seen.See("nail"));
        Assert.False(seen.See("nail"));
        seen.Read(Read(["scrap"], ["nail", "wire"], "workbench"));
        var after = Assert.Single(seen.Batches);
        Assert.Equal(batch.Cause, after.Cause);
        Assert.Equal(batch.By, after.By);
        Assert.Equal(things, after.Things);
        Assert.False(seen.Unread("nail"));
        Assert.True(seen.Unread("wire"));
    }

    [Fact]
    public void TheCounterCountsBatchesNotThings()
    {
        var seen = Kept();
        seen.Read(Nothing);
        seen.Read(Read(["scrap"], ["nail", "wire"], "workbench"));
        seen.Read(Read(["scrap", "iron"], ["nail", "wire", "plate"], "workbench"));
        Assert.Equal(2, seen.Batches.Count);
        Assert.Equal(2, seen.UnreadBatches);
        Assert.True(seen.Lit);
        foreach (string id in seen.Batches[0].Things) seen.See(id);
        Assert.Equal(1, seen.UnreadBatches);
        seen.Opened();
        Assert.False(seen.Lit);
        seen.SeeAll();
        Assert.Equal(0, seen.UnreadBatches);
    }

    [Fact]
    public void OnlyTheNewestBatchesAreKeptAndWhatAnOlderOneHeldIsSeen()
    {
        var seen = Kept();
        seen.Read(Nothing);
        var items = BoardThings.Ids.Take(BoardSeen.Kept + 1).ToList();
        for (int i = 1; i <= items.Count; i++) seen.Read(Read([], [.. items.Take(i)]));
        Assert.Equal(BoardSeen.Kept, seen.Batches.Count);
        Assert.False(seen.Unread(items[0]));
        Assert.Contains(items[0], seen.Seen);
    }

    [Fact]
    public void BatchesAndThePinComeBackForTheirWorldAndWhatWasNewIsStillNew()
    {
        var keep = new BoardKeep(_folder);
        keep.Of(7);
        keep.Seen.Read(Nothing);
        keep.Seen.Read(Read(["scrap"], ["nail"], "workbench"));
        keep.Pinned = "plate";
        Assert.True(keep.Write());
        var again = new BoardKeep(_folder);
        again.Of(7);
        again.Seen.Read(Read(["scrap"], ["nail"], "workbench"));
        Assert.Equal(["nail", "scrap"], Assert.Single(again.Seen.Batches).Things.Order(StringComparer.Ordinal));
        Assert.True(again.Seen.Lit);
        Assert.Equal("plate", again.Pinned);
        again.Of(8);
        Assert.Null(again.Pinned);
        Assert.Empty(again.Seen.Batches);
    }

    [Fact]
    public void WhatWasFoundWhileTheFileWasNotWrittenComesAsABatch()
    {
        var keep = new BoardKeep(_folder);
        keep.Of(7);
        keep.Seen.Read(Read(["scrap"], []));
        Assert.True(keep.Write());
        var again = new BoardKeep(_folder);
        again.Of(7);
        again.Seen.Read(Read(["scrap", "iron"], []));
        var batch = Assert.Single(again.Seen.Batches);
        Assert.Equal(BoardSeen.CauseFound, batch.Cause);
        Assert.Equal(["iron"], batch.By);
    }

    [Fact]
    public void AnOldFilesMarkedReadsAsSeenItsDraggedPlacesDropped()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(BoardFile.PathOf(_folder, 9), """{"Moved":{"scrap":{"X":1,"Y":2}},"Seen":["scrap"],"Orders":2,"Marked":["iron"]}""");
        var keep = new BoardKeep(_folder);
        keep.Of(9);
        Assert.Equal(["iron", "scrap"], keep.Seen.Seen.Order(StringComparer.Ordinal));
        Assert.Equal(2, keep.Seen.OrdersSeen);
        keep.Seen.Read(Read(["scrap", "iron"], []));
        Assert.Empty(keep.Seen.Batches);
    }

    [Fact]
    public void AWorldWithNoIdKeepsNoFile()
    {
        var keep = new BoardKeep(_folder);
        keep.Seen.Read(Read(["scrap"], []));
        keep.Pinned = "plate";
        Assert.True(keep.Write());
        Assert.False(Directory.Exists(_folder));
    }

    [Fact]
    public void ANewlyMetOrderPlaysWhenTheMarketIsShownWhateverTheTabletDidAndLightsNoFind()
    {
        var seen = new BoardSeen([], 0);
        seen.ReadOrders(1);
        seen.Opened();
        seen.Closed();
        Assert.False(seen.Lit);
        Assert.Equal(0, seen.SeeOrders(1));
        Assert.Equal(1, seen.SeeOrders(1));
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
        seen.ReadOrders(2);
        Assert.Equal(2, seen.SeeOrders(2));
    }

    [Fact]
    public void WithNothingKeptTheFirstShowingPlaysNothing()
    {
        var seen = new BoardSeen([]);
        Assert.Equal(4, seen.SeeOrders(4));
    }

    [Fact]
    public void AnOlderWorldThanWasSeenPlaysNothing()
    {
        var seen = new BoardSeen([], 3);
        Assert.Equal(3, seen.SeeOrders(1));
        Assert.Equal(3, seen.OrdersSeen);
    }

    [Fact]
    public void AnOrderMetStillPlaysAfterARelaunchAndAFileFromBeforeOrdersStartsAsThingsStand()
    {
        var keep = new BoardKeep(_folder);
        keep.Of(7);
        keep.Seen.Read(Nothing);
        keep.Seen.ReadOrders(1);
        Assert.True(keep.Write());
        var again = new BoardKeep(_folder);
        again.Of(7);
        again.Seen.Read(Nothing);
        again.Seen.ReadOrders(2);
        Assert.Equal(1, again.Seen.SeeOrders(2));
        Assert.True(BoardFile.Write(BoardFile.PathOf(_folder, 8), new BoardKept([])));
        again.Of(8);
        again.Seen.ReadOrders(2);
        Assert.Equal(2, again.Seen.SeeOrders(2));
    }
}
