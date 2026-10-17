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

    [Fact]
    public void TipAStackByItsCardWithTheVioletBandUntilItIsAnalysed()
    {
        Assert.Equal(new Tip("Iron") { Card = ItemCards.Of("iron", unknown: true) }, With(false).TipOf("iron"));
        Assert.Equal(new Tip("Iron") { Card = ItemCards.Of("iron", unknown: false) }, With(false, "iron").TipOf("iron"));
        Assert.Equal(new Tip("Iron") { Card = ItemCards.Of("iron", unknown: false) }, With(true).TipOf("iron"));
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
