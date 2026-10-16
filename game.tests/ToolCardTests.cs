using Stillwake.Game.Features.Items;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Xunit;
using static Stillwake.Game.I18n.En;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>A tool's card in the one tooltip: its numbers from content, how they are written, the band of an unknown tool.</summary>
public class ToolCardTests
{
    private static Reading With(bool creative, params string[] analysed) => new([], null, creative, null, null, analysed);

    [Fact]
    public void TakeTheAxesNumbersFromContentAndItsDamageASecondFromThem()
    {
        var card = ToolCards.Of("fireAxe", unknown: false)!;
        double every = (double)Spec.Axe.SwingTicks / World.TicksPerSecond;
        Assert.Equal(ToolCards.Number(Spec.Axe.Damage / every), card.Dps);
        Assert.Equal(Spec.Axe.Damage.ToString(), card.Damage);
        Assert.Equal($"{ToolCards.Number(every)}s", card.Hit);
        // As content stands: 2 damage every 10 ticks of 20 a second.
        Assert.Equal(("4", "2", "0.5s"), (card.Dps, card.Damage, card.Hit));
    }

    [Fact]
    public void WriteNumbersWithOneDecimalAtMostHalfRoundedUp()
    {
        Assert.Equal("4", ToolCards.Number(4));
        Assert.Equal("5.3", ToolCards.Number(16.0 / 3));
        Assert.Equal("0.5", ToolCards.Number(0.5));
        Assert.Equal("0.3", ToolCards.Number(0.25));
        Assert.Equal("6.7", ToolCards.Number(20.0 / 3));
    }

    [Fact]
    public void NameTheAxeItsKindWhatItBreaksAndHowItIsUsed()
    {
        var card = ToolCards.Of("fireAxe", unknown: false)!;
        Assert.Equal(("Fire axe", "Tool · Axe"), (card.Name, card.Kind));
        Assert.Equal(["Hull section", "Cargo container", "Machine block", "Electronics rack", "Oxygen tanks", "Shield slab"], card.Uses);
        Assert.Equal(("LMB", "hold on wreckage in space"), (card.Key, card.Hint));
        Assert.Empty(card.Effects);
        Assert.Equal(Spec.Wreckage.All.Count, card.Uses.Count);
    }

    [Fact]
    public void GiveNoCardToAnItemThatIsNoTool()
    {
        Assert.Null(ToolCards.Of("iron", unknown: false));
        Assert.Null(With(false).TipOf("iron").Card);
    }

    [Fact]
    public void ShowTheBandOnlyWhileTheToolIsNotAnalysedAndNeverInCreativeMode()
    {
        Assert.True(With(false).TipOf("fireAxe").Card!.Unknown);
        Assert.False(With(false, "fireAxe").TipOf("fireAxe").Card!.Unknown);
        Assert.False(With(true).TipOf("fireAxe").Card!.Unknown);
        Assert.Equal("? Not analysed", T("card.notAnalysed"));
    }

    [Fact]
    public void TellTipsWithCardsApartByWhatTheCardsSay()
    {
        Tip Of(bool unknown) => With(!unknown).TipOf("fireAxe");
        Assert.Equal(Of(true), Of(true));
        Assert.Equal(Of(true).GetHashCode(), Of(true).GetHashCode());
        Assert.NotEqual(Of(true), Of(false));
        Assert.NotEqual(Of(false), new Tip("Fire axe"));
        var effect = new CardEffect("Fire", "sets it burning", "ff7a2e", "3a1a0c");
        var card = ToolCards.Of("fireAxe", false)!;
        Assert.Equal(card with { Effects = [effect] }, card with { Effects = [effect with { }] });
        Assert.NotEqual(card with { Effects = [effect] }, card);
    }

    [Fact]
    public void HaveWordsForEveryEffectOfTheAxe()
    {
        Assert.All(Spec.Axe.Effects, e =>
        {
            Assert.True(Table.ContainsKey($"effect.{e.Id}"), e.Id);
            Assert.True(Table.ContainsKey($"effect.{e.Id}.about"), e.Id);
        });
    }
}
