using Stillwake.Game.Features.Debris;
using Stillwake.Game.Shared;
using Xunit;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Tests;

/// <summary>The Handbook's debris (0066): its rows, and what a kind's entry shows of the world's record.</summary>
public class HandbookTests
{
    private static readonly string[] Spawned =
        Content.Debris.Zones.SelectMany(z => z.Tiers).SelectMany(t => t.Kinds).Select(k => k.Item).Distinct().ToArray();

    [Fact]
    public void PutsEveryKindThatSpawnsInExactlyOneRowAndNothingElse()
    {
        var shown = Handbook.DebrisRows.SelectMany(r => r.Kinds).ToList();
        Assert.All(Spawned, kind => Assert.Single(shown, k => k == kind));
        Assert.All(shown, kind => Assert.Contains(kind, Spawned));
        Assert.All(Handbook.DebrisRows, row => Assert.True(Table.ContainsKey($"handbook.row.{row.Id}"), row.Id));
        Assert.Equal("wrecks", Handbook.RowOf("soilClod")?.Id);
        Assert.Null(Handbook.RowOf("planks"));
    }

    [Fact]
    public void RevealsALootSlotOnlyOnceItsItemHasComeOutOfTheKind()
    {
        var slots = Finds.SlotsOf("cargoContainer", ["planks", "scrapChunk", "wire"]);
        Assert.Equal(["planks", "scrapChunk", "gear", "wire"], slots.Select(s => s.Item));
        Assert.Equal([true, true, false, true], slots.Select(s => s.Seen));
        // Not found at all: every slot a silhouette, its chance shown all the same.
        Assert.All(Finds.SlotsOf("cargoContainer", null), s => Assert.False(s.Seen));
        Assert.Equal([null, null, 4, 4], Finds.SlotsOf("cargoContainer", null).Select(s => s.OneIn));
    }

    [Fact]
    public void ShowsAPieceCaughtAsItselfAsOneSlotOfItselfWithNoRangeNorChance()
    {
        var slot = Assert.Single(Finds.SlotsOf("scrapChunk", ["scrapChunk"]));
        Assert.Equal(new LootSlot("scrapChunk", 1, 1, null, true), slot);
        Assert.Equal("", slot.Corner);
        Assert.False(Assert.Single(Finds.SlotsOf("hullPlate", null)).Seen);
    }

    [Fact]
    public void SaysARangeInTheCornerOnceSeenAndAChanceAsOneIn()
    {
        Assert.Equal("8–12", new LootSlot("planks", 8, 12, null, true).Corner);
        Assert.Equal("6", new LootSlot("scrap", 6, 6, null, true).Corner);
        Assert.Equal("", new LootSlot("canister", 1, 1, null, true).Corner);
        Assert.Equal("", new LootSlot("gear", 1, 2, 4, false).Corner);
        Assert.Equal("1–2", new LootSlot("gear", 1, 2, 4, false).Range);
        Assert.Equal("1 in 4", new LootSlot("gear", 1, 2, 4, false).Chance);
        Assert.Equal("Always", new LootSlot("planks", 8, 12, null, true).Chance);
    }

    [Fact]
    public void NamesASeenItemInItsTipAndHidesAnUnseenOneBehindQuestionMarks()
    {
        Assert.Equal(new Tip("Planks", new TipLine("8–12"), new TipLine("Always", Tone.Good)), new LootSlot("planks", 8, 12, null, true).Tip);
        Assert.Equal(new Tip("???", new TipLine("1 in 2"), new TipLine("Not dropped yet", Tone.Dim)), new LootSlot("seed", 1, 2, 2, false).Tip);
    }

    [Fact]
    public void SaysOfAKindNotFoundOnlyWhereToLookAndOfOneFoundHowItIsHadAndWhatIsStillMissing()
    {
        Assert.Equal(new Tip("???", new TipLine("Far out from the ship", Tone.Dim)), Finds.KindTip("engineBlock", null));
        Assert.Equal(new Tip("Cargo container", new TipLine("Hit points 11"), new TipLine("1 not dropped yet", Tone.Unknown)),
            Finds.KindTip("cargoContainer", ["planks", "scrapChunk", "wire"]));
        Assert.Equal(new Tip("Cargo crate", new TipLine("Caught by hand")), Finds.KindTip("cargoCrate", ["planks"]));
        Assert.Equal("Caught by hand", Finds.HowTip("supplyPod").Name);
        Assert.Equal("Hit points 200", Finds.HowTip("asteroidLarge").Name);
    }

    [Fact]
    public void CountsTheKindsFoundAndShowsTheFirstInTheRowsOrderWhenNoneIsChosen()
    {
        Assert.Equal((0, Spawned.Length), Finds.Tally([]));
        Assert.Equal((2, Spawned.Length), Finds.Tally(["asteroid", "cargoCrate", "planks"]));
        Assert.Null(Finds.FirstFound([]));
        // In the rows' order, not the order found.
        Assert.Equal("cargoCrate", Finds.FirstFound(["asteroid", "cargoCrate"]));
    }

    [Fact]
    public void ScalesAPictureByAWholeNumberUpToFourAndDownToFitWhenTooBig()
    {
        Assert.Equal(3, Finds.Fit(16, 14, 48, 48));
        Assert.Equal(4, Finds.Fit(16, 14, 356, 116));
        Assert.Equal(1, Finds.Fit(48, 32, 48, 48));
        Assert.Equal(0.5f, Finds.Fit(96, 48, 48, 48));
    }
}
