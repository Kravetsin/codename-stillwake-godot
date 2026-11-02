using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FsCheck.Xunit;
using Stillwake.Game.Features.Research;
using Xunit;

namespace Stillwake.Game.Tests;

public sealed class BoardPileTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public void Dispose()
    {
        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }

    private static BoardThing Thing(string id, string[] parts, string[]? stations = null) => new(id, parts.Select(p => (p, 2)).ToList(), stations, false);

    /// <summary>Plate from scrap at the bench, bench from plate; a loop of two; and things with no recipe.</summary>
    private static readonly BoardThing[] Things =
    [
        Thing("plate", ["scrap"], ["bench"]),
        Thing("bench", ["plate"]),
        Thing("egg", ["hen"]),
        Thing("hen", ["egg"]),
    ];

    private static List<BoardSlot> Slots(params string[] ids) => [.. ids.Select((id, i) => new BoardSlot(id, i * 100, 0, false))];

    private static BoardRead ReadOf(Func<string, int>? count = null, Func<string, bool>? stands = null) => new([], [], count ?? (_ => 0), stands ?? (_ => false));

    private static (BoardSeen Seen, BoardPile Pile) Reading(string[] notes, params string[] seen)
    {
        var s = new BoardSeen(seen);
        s.Read(notes);
        var pile = new BoardPile();
        pile.Read(s, Slots(notes), Things);
        return (s, pile);
    }

    [Fact]
    public void PartsAndStationsComeBeforeWhatTheyMake()
    {
        Assert.Equal("scrap", BoardPile.Next(["plate", "scrap"], Slots("plate", "scrap"), Things));
        Assert.Equal("bench", BoardPile.Next(["bench", "plate"], Slots("plate", "bench"), [Thing("plate", [], ["bench"]), Thing("bench", [])]));
    }

    [Fact]
    public void ARecipeCycleDoesNotStallThePile() => Assert.Equal("egg", BoardPile.Next(["hen", "egg"], Slots("egg", "hen"), Things));

    [Fact]
    public void OtherwiseItIsInSlotOrder() => Assert.Equal("iron", BoardPile.Next(["wire", "iron"], Slots("iron", "wire"), Things));

    [Fact]
    public void WhatHasNoSlotYetIsNotChosen()
    {
        Assert.Equal("iron", BoardPile.Next(["wire", "iron"], Slots("iron"), Things));
        Assert.Null(BoardPile.Next(["wire"], Slots("iron"), Things));
    }

    [Fact]
    public void ALandedPartNoLongerHoldsBackWhatItGoesInto()
    {
        var (seen, pile) = Reading(["plate", "scrap"]);
        Assert.Equal("scrap", pile.Top);
        Assert.Equal("scrap", pile.Flip(seen, Slots("plate", "scrap"), Things));
        Assert.Equal("plate", pile.Top);
    }

    [Fact]
    public void ClosingOnTheFourthOfNineResumesThereAndTheFileAgrees()
    {
        string[] notes = ["a", "b", "c", "d", "e", "f", "g", "h", "i"];
        var keep = new BoardKeep(_folder);
        keep.Of(7);
        keep.Seen.Read([]);
        Assert.True(keep.Write());
        keep.Seen.Read(notes);
        var pile = new BoardPile();
        keep.Seen.Opened();
        pile.Read(keep.Seen, Slots(notes), Things);
        for (int i = 0; i < 3; i++) pile.Flip(keep.Seen, Slots(notes), Things);
        Assert.Equal("d", pile.Top);
        keep.Seen.Closed();
        pile.Reset();
        Assert.True(keep.Seen.Lit);
        Assert.Equal(6, keep.Seen.Unread.Count);
        keep.Seen.Opened();
        pile.Read(keep.Seen, Slots(notes), Things);
        Assert.Equal("d", pile.Top);
        Assert.True(keep.Write());

        var again = new BoardKeep(_folder);
        again.Of(7);
        again.Seen.Read(notes);
        var fresh = new BoardPile();
        fresh.Read(again.Seen, Slots(notes), Things);
        Assert.Equal("d", fresh.Top);
        Assert.Equal(6, again.Seen.Unread.Count);
    }

    [Fact]
    public void AnArrivalWaitsUnderTheTop()
    {
        var (seen, pile) = Reading(["iron", "wire"]);
        Assert.Equal("iron", pile.Top);
        string[] more = ["scrap", "iron", "wire"];
        seen.Read(more);
        pile.Read(seen, Slots(more), Things);
        Assert.Equal("iron", pile.Top);
        Assert.Equal(3, seen.Unread.Count);
        Assert.False(seen.Marked("scrap"));
    }

    [Fact]
    public void TakeAllLandsTheRestMarked()
    {
        var (seen, pile) = Reading(["iron", "wire", "scrap"]);
        Assert.True(pile.TakeAll(seen));
        Assert.Null(pile.Top);
        Assert.Empty(seen.Unread);
        Assert.True(seen.Marked("iron") && seen.Marked("wire") && seen.Marked("scrap"));
    }

    [Fact]
    public void TakeAllWithNoPileLandsNothing()
    {
        var (seen, pile) = Reading(["iron"], "iron");
        Assert.False(pile.TakeAll(seen));
        Assert.False(seen.Marked("iron"));
    }

    [Fact]
    public void AFindAfterAShowingEndsStartsAPileAgain()
    {
        var (seen, pile) = Reading(["iron"], "iron");
        pile.Reset();
        seen.Read(["iron", "wire"]);
        pile.Read(seen, Slots("iron", "wire"), Things);
        Assert.Equal("wire", pile.Top);
        Assert.Contains("wire", seen.Unread);
    }

    [Fact]
    public void WithTheBoardWholeAFindLandsAtOnce()
    {
        var (seen, pile) = Reading(["iron"], "iron");
        seen.Read(["iron", "wire"]);
        pile.Read(seen, Slots("iron", "wire"), Things);
        Assert.Empty(seen.Unread);
        Assert.True(seen.Marked("wire"));
        Assert.Null(pile.Top);
    }

    [Fact]
    public void AFindBetweenTheLastFlipAndTheNextReadStartsAPile()
    {
        var (seen, pile) = Reading(["iron"]);
        pile.Flip(seen, Slots("iron"), Things);
        Assert.Null(pile.Top);
        seen.Read(["iron", "wire"]);
        pile.Read(seen, Slots("iron", "wire"), Things);
        Assert.Equal("wire", pile.Top);
    }

    [Fact]
    public void AFlipOfATopNoLongerUnreadLandsNothing()
    {
        var (_, pile) = Reading(["iron", "wire"]);
        var another = new BoardSeen(["iron"]);
        another.Read(["iron", "wire"]);
        Assert.Null(pile.Flip(another, Slots("iron", "wire"), Things));
        Assert.False(pile.TakeAll(another));
        Assert.False(another.Marked("wire"));
    }

    [Fact]
    public void AGuestWithNoFileHasAnEmptyPileAndNoLamp()
    {
        var seen = new BoardSeen(null);
        seen.Read(["iron", "wire"]);
        var pile = new BoardPile();
        pile.Read(seen, Slots("iron", "wire"), Things);
        Assert.Null(pile.Top);
        Assert.Empty(seen.Unread);
        Assert.False(seen.Lit);
    }

    [Fact]
    public void ClosestIsTheHighestPencilTiesInTheOrderGiven()
    {
        var read = ReadOf(item => item == "b" ? 1 : 0);
        BoardThing[] things = [Thing("none", ["a"]), Thing("half", ["b"]), Thing("alsoHalf", ["b"])];
        Assert.Equal("half", BoardPile.Closest(["none", "half", "alsoHalf"], read, things));
        Assert.Null(BoardPile.Closest([], read, things));
        Assert.Equal("none", BoardPile.Closest(["none", "half"], ReadOf(), things));
    }

    [Fact]
    public void LackSaysPartsShortAndAStationNoneStands()
    {
        var thing = Thing("plate", ["scrap", "iron"], ["bench", "press"]);
        var lack = BoardPile.Lack(thing, ReadOf(item => item == "scrap" ? 1 : item == "iron" ? 2 : 0));
        Assert.Equal([("scrap", 1)], lack.Parts);
        Assert.Equal("bench", lack.Station);
        var done = BoardPile.Lack(thing, ReadOf(_ => 2, kind => kind == "press"));
        Assert.Empty(done.Parts);
        Assert.Null(done.Station);
    }

    /// <summary>Whatever the player and the world do, each entry lands once and enough flips land them all.</summary>
    [Property]
    public void EveryEntryLandsExactlyOnce(byte[] script)
    {
        string[] all = ["scrap", "plate", "bench", "egg", "hen", "iron", "wire", "gear"];
        var slots = Slots(all);
        var seen = new BoardSeen([]);
        var pile = new BoardPile();
        int known = 1;
        var landed = new List<string>();
        var sinceClosed = new HashSet<string>();
        void Took(IEnumerable<string> ids)
        {
            foreach (string id in ids)
            {
                landed.Add(id);
                sinceClosed.Add(id);
            }
        }
        void Read()
        {
            var before = seen.Seen.ToHashSet();
            seen.Read(all.Take(known));
            pile.Read(seen, slots, Things);
            Took(seen.Seen.Where(id => !before.Contains(id)));
            // Only what landed since the close is fresh: a second landing of an old note would show here.
            Assert.Equal(sinceClosed.OrderBy(id => id, StringComparer.Ordinal), all.Where(seen.Fresh).OrderBy(id => id, StringComparer.Ordinal));
            Assert.Equal(landed.OrderBy(id => id, StringComparer.Ordinal), all.Where(seen.Marked).OrderBy(id => id, StringComparer.Ordinal));
        }
        void Flip()
        {
            if (pile.Flip(seen, slots, Things) is { } id) Took([id]);
        }
        void Close()
        {
            seen.Closed();
            pile.Reset();
            sinceClosed.Clear();
            seen.Opened();
        }
        seen.Opened();
        Read();
        foreach (byte step in script)
        {
            switch (step % 5)
            {
                case 0: Flip(); break;
                case 1: known = Math.Min(all.Length, known + 1); break;
                case 2: Close(); break;
                case 3:
                    var before = seen.Seen.ToHashSet();
                    if (pile.TakeAll(seen)) Took(seen.Seen.Where(id => !before.Contains(id)));
                    break;
            }
            Read();
        }
        known = all.Length;
        Close();
        Read();
        for (int i = 0; i < all.Length; i++)
        {
            Flip();
            Read();
        }
        Assert.Empty(seen.Unread);
        Assert.Equal(landed.Count, landed.Distinct().Count());
        Assert.Equal(all.OrderBy(id => id, StringComparer.Ordinal), landed.OrderBy(id => id, StringComparer.Ordinal));
    }
}
