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
        // A slot a loot entry, in the table's order: all but one of its items come out.
        var loot = Content.Loot.Of("cargoContainer")!;
        string hidden = loot[^2].Item;
        var slots = Finds.SlotsOf("cargoContainer", [.. loot.Select(e => e.Item).Where(i => i != hidden)]);
        Assert.Equal(loot.Select(e => e.Item), slots.Select(s => s.Item));
        Assert.Equal(loot.Select(e => e.Item != hidden), slots.Select(s => s.Seen));
        Assert.Contains(slots, s => s.Seen);
        // Not found at all: every slot a silhouette, its chance shown all the same.
        Assert.All(Finds.SlotsOf("cargoContainer", null), s => Assert.False(s.Seen));
        Assert.Equal(loot.Select(e => e.OneIn), Finds.SlotsOf("cargoContainer", null).Select(s => s.OneIn));
        Assert.Contains(loot, e => e.OneIn != null);
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
        // Its hit points and looks as content gives them; one item of its loot not out yet.
        var loot = Content.Loot.Of("cargoContainer")!;
        Assert.Equal(new Tip("Cargo container", new TipLine($"Hit points {Content.Wreckage.HpOf("cargoContainer")}"), new TipLine("1 not dropped yet", Tone.Unknown)),
            Finds.KindTip("cargoContainer", [.. loot.Skip(1).Select(e => e.Item)]));
        Assert.Equal(new Tip("Cargo crate", new TipLine("Caught by hand"), new TipLine($"Looks 1/{Content.Looks.CountOf("cargoCrate")}")), Finds.KindTip("cargoCrate", ["planks"], [0]));
        Assert.Equal("Caught by hand", Finds.HowTip("supplyPod").Name);
        Assert.Equal($"Hit points {Content.Wreckage.HpOf("asteroidLarge")}", Finds.HowTip("asteroidLarge").Name);
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
    public void ShowsEveryLookOfAKindInItsStripOnlyThoseFoundRevealedAndCountsThem()
    {
        var looks = Finds.LooksOf("supplyPod", [2, 0]);
        Assert.Equal([0, 1, 2, 3], looks.Select(l => l.Look));
        Assert.Equal([true, false, true, false], looks.Select(l => l.Seen));
        Assert.Equal("2/4", Finds.LookCount(looks));
        Assert.Equal("0/8", Finds.LookCount(Finds.LooksOf("cargoCrate", null)));
        // A kind of one look, a wreck drawn once: one, in the strip that is not shown.
        Assert.Single(Finds.LooksOf("cargoContainer", [0]));
        Assert.Equal(new Tip("Look 3 of 4"), looks[2].Tip);
        Assert.Equal(new Tip("???", new TipLine("None like it caught or broken yet", Tone.Dim)), looks[1].Tip);
    }

    [Fact]
    public void ShowsBigTheLookClickedOnceFoundElseTheFirstFound()
    {
        Assert.Equal(2, Finds.ShownLook([2, 0], null));
        Assert.Equal(0, Finds.ShownLook([2, 0], 0));
        // One not found shows nothing new: the first found stays.
        Assert.Equal(2, Finds.ShownLook([2, 0], 3));
        Assert.Equal(0, Finds.ShownLook(null, null));
    }

    [Fact]
    public void CountsTheLooksFoundOfAllThereAreAndSaysBothCountsInItsTip()
    {
        int all = Spawned.Sum(Content.Looks.CountOf);
        Assert.Equal(81, all);
        Assert.Equal((0, all), Finds.LookTally(new Dictionary<string, int[]>()));
        var looks = new Dictionary<string, int[]> { ["cargoCrate"] = [2, 0, 5], ["hullSection"] = [0], ["planks"] = [0] };
        Assert.Equal((4, all), Finds.LookTally(looks));
        Assert.Equal(new Tip("Found in this world", new TipLine("Kinds 2/18"), new TipLine($"Looks 4/{all}")),
            Finds.TallyTip(Finds.Tally(looks.Keys), Finds.LookTally(looks)));
        Assert.Contains(new TipLine("Looks 3/8"), Finds.KindTip("cargoCrate", ["planks"], [2, 0, 5]).Lines);
        Assert.DoesNotContain(Finds.KindTip("hullSection", ["hullPlate"], [0]).Lines, l => l.Text.StartsWith("Looks"));
    }

    [Fact]
    public void ScalesAPictureByAWholeNumberUpToFourAndDownToFitWhenTooBig()
    {
        Assert.Equal(3, Finds.Fit(16, 14, 48, 48));
        Assert.Equal(4, Finds.Fit(16, 14, 356, 116));
        Assert.Equal(1, Finds.Fit(48, 32, 48, 48));
        Assert.Equal(0.5f, Finds.Fit(96, 48, 48, 48));
    }

    private static readonly HandbookScreen Normal = new(Slots.SizeOf(SlotScale.Normal)), Large = new(Slots.SizeOf(SlotScale.Large));

    [Fact]
    public void LaysItsTabletScreenOutFromTheSlotSizeAndFitsItWithItsBezelOnTheSmallestScreen()
    {
        Assert.Equal((838, 376), (Normal.Width, Normal.Height));
        Assert.Equal((950, 440), (Large.Width, Large.Height));
        foreach (var at in (HandbookScreen[])[Normal, Large])
        {
            Assert.Equal(TabletLayout.Pad + TabletLayout.TabHeight + TabletLayout.Pad, HandbookScreen.BodyTop);
            Assert.Equal(at.Width, at.BarLeft + TabletLayout.BarWidth + TabletLayout.Pad);
            Assert.Equal(at.Height, at.FootTop + TabletLayout.FootHeight + TabletLayout.Pad);
            Assert.Equal(at.KindsWidth + Handbook.EntryGap + Handbook.EntryWidth, at.BodyWidth);
            // The bezel round it, the stylus's tip included, still inside 1280×720.
            int around = TabletLayout.ArtAround * TabletLayout.ArtScale;
            Assert.True(at.Width + around <= 1280 && at.Height + around <= 720, $"{at.Width}×{at.Height}");
        }
    }

    [Fact]
    public void ScrollsItsBodyARowOfKindsANotchAndNoFurtherThanItsEnd()
    {
        int content = Normal.BodyHeight + 100;
        Assert.Equal(Normal.Step, Normal.Scrolled(0, 1, content));
        Assert.Equal(100, Normal.Scrolled(0, 5, content));
        Assert.Equal(0, Normal.Scrolled(Normal.Step, -3, content));
        // Content that fits never scrolls, and shows no thumb.
        Assert.Equal(0, Normal.Scrolled(0, 1, Normal.BodyHeight));
        Assert.Null(TabletLayout.Thumb(Normal.BodyHeight, Normal.BodyHeight, 0));
        var (top, length) = TabletLayout.Thumb(300, 600, 300)!.Value;
        Assert.Equal((150f, 150f), (top, length));
        Assert.Equal(TabletLayout.ThumbLeast, TabletLayout.Thumb(300, 60000, 0)!.Value.Length);
    }
}
