using System.Linq;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Shared;
using Xunit;
using Diet = Stillwake.Content.Diet;
using Foods = Stillwake.Content.Foods;
using World = Stillwake.Sim.World;

namespace Stillwake.Game.Tests;

/// <summary>
/// The HUD's meal slots (food-and-the-body): a meal pulses in its last minute; its tip is its food card with the time left
/// (2026-11-14-food-and-gear-cards).
/// </summary>
public class MealTipsTests
{
    [Fact]
    public void AMealPulsesOnlyOnceItMayBeEatenAgain()
    {
        Assert.False(MealTips.Pulsing(Diet.RefreshWithin + 1));
        Assert.True(MealTips.Pulsing(Diet.RefreshWithin));
    }

    [Fact]
    public void ARationsTipIsItsFoodCardWithTheTimeLeftOutOfItsDurationAndNoKeyOrPrice()
    {
        // 4:13 left of the ration's 15:00.
        var tip = MealTips.Of(Foods.Ration, (4 * 60 + 13) * World.TicksPerSecond);
        var card = tip.Card!;
        Assert.Equal(("Ration", "Ration", "Food"), (tip.Name, card.Name, card.Kind));
        Assert.True(card.Food);
        Assert.Equal([("−5%", "Air used breathing"), ("+30%", "Work speed"), ("4:13", "Left")], card.Rows.Select(r => (r.Value, r.Label)));
        Assert.Equal("of 15:00", card.Rows[^1].Note);
        Assert.Equal(CardSign.Pace, card.Rows[^1].Sign);
        Assert.Null(card.Use);
        Assert.Null(card.Price);
        // The rates are the item card's own; only the duration row differs.
        Assert.Equal(ItemCards.Of(Foods.Ration).Rows.SkipLast(1), card.Rows.SkipLast(1));
    }

    [Fact]
    public void HungersTipSaysWhatItCostsByTheDietsNumbersThenWhatEndsIt()
    {
        var tip = MealTips.Hunger();
        Assert.Equal("Hungry", tip.Name);
        Assert.Equal(
            [("Air use when running +25%", Tone.Bad), ("Work speed −25%", Tone.Bad), ("Eat something", Tone.Dim)],
            tip.Lines.Select(l => (l.Text, l.Tone)));
    }

    [Theory]
    [InlineData(32 * 60, "32m")]
    [InlineData(31 * 60 + 1, "32m")]
    [InlineData(61, "2m")]
    [InlineData(60, "60s")]
    [InlineData(45, "45s")]
    [InlineData(0, "0s")]
    public void UnderItsDialAMealsTimeIsMinutesRoundedUpThenSecondsInItsLastMinute(int seconds, string shown) =>
        Assert.Equal(shown, MealTips.Clock(seconds * World.TicksPerSecond));

    [Fact]
    public void AfterTheMealsThePurgeShowsWhileThereIsOneElseHungerLitOnlyWhenHungry()
    {
        Assert.Equal(LastDial.Purge, MealTips.LastOf(1, hungry: false));
        Assert.Equal(LastDial.Purge, MealTips.LastOf(3, hungry: false));
        Assert.Equal(LastDial.Hungry, MealTips.LastOf(0, hungry: true));
        Assert.Equal(LastDial.Unfed, MealTips.LastOf(0, hungry: false));
    }

    [Fact]
    public void UnderItsDialAMealsLastTickStillShowsASecond() => Assert.Equal("1s", MealTips.Clock(1));

    [Fact]
    public void AMealsLastTickStillShowsASecond()
    {
        Assert.Equal("0:01", MealTips.Of(Foods.Ration, 1).Card!.Rows[^1].Value);
    }

    [Fact]
    public void AnEmptySlotSaysHungerOnlyWhenAllAreEmpty()
    {
        Assert.Equal(MealTips.Hunger(), MealTips.Empty(hungry: true));
        Assert.Equal("Room for a meal", MealTips.Empty(hungry: false).Name);
    }
}
