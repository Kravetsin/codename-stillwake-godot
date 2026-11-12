using System.Linq;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Shared;
using Xunit;
using Diet = Stillwake.Content.Diet;
using Foods = Stillwake.Content.Foods;

namespace Stillwake.Game.Tests;

/// <summary>The HUD's meal slots (food plan, A2): a meal pulses in its last minute; its tip names it, its rates and its time.</summary>
public class MealTipsTests
{
    [Fact]
    public void AMealPulsesOnlyOnceItMayBeEatenAgain()
    {
        Assert.False(MealTips.Pulsing(Diet.RefreshWithin + 1));
        Assert.True(MealTips.Pulsing(Diet.RefreshWithin));
    }

    [Fact]
    public void ARationSaysItsRatesInPercentAndItsTimeLeft()
    {
        var tip = MealTips.Of(Foods.Ration, 18_000);
        Assert.Equal("Ration", tip.Name);
        Assert.Equal(["5% less air a breath", "30% faster work", "Lasts 15:00 more"], tip.Lines.Select(l => l.Text));
        Assert.Equal(Tone.Dim, tip.Lines[^1].Tone);
    }

    [Fact]
    public void PodPasteIsNamedThoughNoItemAndGivesNoRate()
    {
        var tip = MealTips.Of(Foods.Paste, Diet.RefreshWithin);
        Assert.Equal("Pod paste", tip.Name);
        Assert.Equal(["Keeps you fed, nothing more", "Lasts 1:00 more"], tip.Lines.Select(l => l.Text));
        Assert.Equal(Tone.Bad, tip.Lines[^1].Tone);
    }

    [Fact]
    public void AnEmptySlotSaysHungerOnlyWhenAllAreEmpty()
    {
        Assert.Equal("Hungry", MealTips.Empty(hungry: true).Name);
        Assert.Equal("Room for a meal", MealTips.Empty(hungry: false).Name);
    }
}
