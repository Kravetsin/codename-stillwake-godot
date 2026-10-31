using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Game.Features.Research;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>Where the board's things stand (docs/plans/board.md).</summary>
public class BoardSlotsTests
{
    private static readonly IReadOnlyList<string> Start = Spec.Research.StartKnown;
    private static readonly string[] AllItems = [.. Spec.Items.All.Select(i => i.Id)];

    private static Arbitrary<int> Seeds => Gen.Choose(1, int.MaxValue).ToArbitrary();

    /// <summary>A found list drawn from every item: <paramref name="seed"/> shuffles them, and some of them stand first.</summary>
    private static string[] Drawn(int seed, int count)
    {
        var random = new Random(seed);
        return [.. AllItems.OrderBy(_ => random.Next()).Take(count)];
    }

    private static IReadOnlyList<BoardSlot> Slots(IReadOnlyList<string> found, IReadOnlyCollection<string>? granted = null) =>
        BoardSlots.Of(found, BoardThings.All, granted ?? Start);

    private static BoardSlot Slot(IReadOnlyList<BoardSlot> slots, string id) => slots.Single(s => s.Id == id);

    [Fact]
    public void NothingStandsWithNothingFound()
    {
        Assert.Empty(Slots([]));
        Assert.Empty(Slots([], [.. BoardThings.All.Select(t => t.Id)]));
    }

    [Fact]
    public void TheFirstNoteStandsAtTheMiddleAndItsUsesHoldSlotsAroundIt()
    {
        var slots = Slots(["scrapChunk"]);
        Assert.Equal((0d, 0d), (Slot(slots, "scrapChunk").X, Slot(slots, "scrapChunk").Y));
        foreach (string use in (string[])["furnace", "workbench", "tradeStation", "wallScrap"])
            Assert.Contains(slots, s => s.Id == use);
        Assert.DoesNotContain(slots, s => s.Id == "plate");
    }

    [Fact]
    public void AThingFoundBeforeItsPartsStandsByTheNoteBeforeIt()
    {
        var slots = Slots(["scrapChunk", "circuit"]);
        var (first, circuit) = (Slot(slots, "scrapChunk"), Slot(slots, "circuit"));
        Assert.True(Math.Sqrt(circuit.X * circuit.X + circuit.Y * circuit.Y) < 400, "far from the note before it");
        Assert.False(circuit.Row);
        Assert.Equal((0d, 0d), (first.X, first.Y));
    }

    [Property(MaxTest = 40)]
    public Property TheSameInputGivesTheSameSlots() => Prop.ForAll(Seeds, seed =>
    {
        var found = Drawn(seed, 30);
        Assert.Equal(Slots(found), Slots(found));
    });

    [Property(MaxTest = 40)]
    public Property NoTwoSlotsStandCloserThanANote() => Prop.ForAll(Seeds, seed =>
    {
        var slots = Slots(Drawn(seed, 60), [.. BoardThings.All.Select(t => t.Id)]);
        for (int a = 0; a < slots.Count; a++)
            for (int b = a + 1; b < slots.Count; b++)
            {
                double apart = Math.Sqrt(Math.Pow(slots[a].X - slots[b].X, 2) + Math.Pow(slots[a].Y - slots[b].Y, 2));
                Assert.True(apart >= BoardSlots.NoteSize - 1e-6, $"{slots[a].Id} and {slots[b].Id} are {apart} apart");
            }
    });

    /// <summary>Leaving out the row, whose things may yet get a part noted and leave it.</summary>
    [Property(MaxTest = 40)]
    public Property AppendingToWhatWasFoundMovesNothingAlreadyPlaced() => Prop.ForAll(Seeds, seed =>
    {
        var found = Drawn(seed, 50);
        var known = BoardThings.All.Where((_, i) => i % 5 == 0).Select(t => t.Id).ToList();
        int cut = 1 + seed % 49;
        var longer = Slots(found, known);
        foreach (var before in Slots(found[..cut], known).Where(s => !s.Row))
            Assert.Equal(before, Slot(longer, before.Id));
    });

    [Fact]
    public void FiveThingsUnderstoodAtOnceKeepTheSlotsTheyHeld()
    {
        string[] found = ["scrapChunk", "hullPlate", "planks", "scrap", "iron"];
        string[] burst = ["wire", "nail", "plate", "gear", "tube"];
        var before = Slots(found);
        var after = Slots(found, [.. Start, .. burst]);
        foreach (string id in burst) Assert.Equal(Slot(before, id), Slot(after, id));
    }

    [Fact]
    public void WhatTheMarketOpensTakesNoSlotTillBought()
    {
        string[] found = ["frame", "solarCell", "wire", "circuit", "photoPanel"];
        Assert.DoesNotContain(Slots(found), s => s.Id == "solarPanel");
        Assert.True(Slot(Slots(found, [.. Start, "solarPanel"]), "solarPanel").Row);
    }

    [Fact]
    public void TheRowHoldsWhatAnOldWorldKnowsWithNoPartNotedByIdNeverWhatTheWreckKnows()
    {
        var slots = Slots(["hullPlate"], [.. Start, "wire", "gear", "motor"]);
        var row = slots.Where(s => s.Row).ToList();
        Assert.Equal(["gear", "motor", "wire"], row.Select(s => s.Id));
        Assert.True(row[0].X < row[1].X && row[1].X < row[2].X);
        Assert.DoesNotContain(row, s => Start.Contains(s.Id));
    }

    [Fact]
    public void EveryThingOnTheBoardHasItsOwnId() =>
        Assert.Equal(BoardThings.All.Count, BoardThings.All.Select(t => t.Id).Distinct(StringComparer.Ordinal).Count());
}
