using Stillwake.Game.Features.Items;
using Stillwake.Game.Shared;
using Xunit;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Tests;

/// <summary>The violet "?" of an item not analysed yet, and the one tooltip over its stack.</summary>
public class UnanalysedTests
{
    private static Reading With(bool creative, params string[] analysed) => new([], null, creative, null, null, analysed);

    [Fact]
    public void MarkAnItemUntilItIsAnalysedAndNothingInCreativeMode()
    {
        Assert.True(With(false, "scrap").Unknown("iron"));
        Assert.False(With(false, "scrap", "iron").Unknown("iron"));
        Assert.False(With(true).Unknown("iron"));
    }

    /// <summary>0027 amended: only what goes into some cost waits for an analyzer; the rest is known from the start.</summary>
    [Fact]
    public void MarkNoItemInNoCostAndBandNoCardOfOne()
    {
        Assert.All(["fireAxe", "harpoon", "drill", "jetpack", "drone", "biofuel"], item =>
        {
            Assert.False(With(false).Unknown(item), item);
            Assert.False(With(false).TipOf(item).Card!.Unknown, item);
        });
        // In a cost: the canister in the jetpack's, planks in the harpoon's.
        Assert.True(With(false).Unknown("canister"));
        Assert.True(With(false).Unknown("planks"));
        // Amended again: the furnace's salvage goes into the furnace alone, open from the start.
        Assert.All(["scrapChunk", "hullPlate"], item => Assert.False(With(false).Unknown(item), item));
    }

    [Fact]
    public void TipAStackByItsCardWithTheVioletBandUntilItIsAnalysed()
    {
        Assert.Equal(new Tip("Iron") { Card = ItemCards.Of("iron", unknown: true) }, With(false).TipOf("iron"));
        Assert.Equal(new Tip("Iron") { Card = ItemCards.Of("iron", unknown: false) }, With(false, "iron").TipOf("iron"));
        Assert.Equal(new Tip("Iron") { Card = ItemCards.Of("iron", unknown: false) }, With(true).TipOf("iron"));
    }

    [Fact]
    public void TipAStationTooColdForWhatItHoldsByTheStationHotEnoughForIt()
    {
        // Silicon smelts into glass only at heat 2: the electric smelter, the first fed station that hot (0057).
        Assert.Equal(new Tip("Needs more heat · Electric smelter"), Reading.TooColdTip("silicon"));
        Assert.Equal(T("crafting.tooCold", ("station", T("station.smelter"))), Reading.TooColdTip("silicon").Name);
    }

    [Fact]
    public void TellTipsApartByWhatTheySayNotByTheirLists()
    {
        Tip Of() => new("Gear", [new TipLine("Iron not analysed", Tone.Unknown, "iron")]);
        Assert.Equal(Of(), Of());
        Assert.Equal(Of().GetHashCode(), Of().GetHashCode());
        Assert.NotEqual(Of(), new Tip("Gear", new TipLine("Iron not analysed", Tone.Dim, "iron")));
        Assert.NotEqual(Of(), new Tip(null, Of().Lines));
    }
}
