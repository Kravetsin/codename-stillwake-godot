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
    public void PodPasteIsNamedThoughNoItemAndOnlyStillsHunger()
    {
        var card = MealTips.Of(Foods.Paste, Diet.RefreshWithin).Card!;
        Assert.Equal(("Pod paste", "Food"), (card.Name, card.Kind));
        Assert.Equal([("only stills hunger", "Effect"), ("1:00", "Left")], card.Rows.Select(r => (r.Value, r.Label)));
        Assert.True(card.Rows[0].Off);
        Assert.Equal("of 20:00", card.Rows[^1].Note);
    }

    [Fact]
    public void AMealsLastTickStillShowsASecond()
    {
        Assert.Equal("0:01", MealTips.Of(Foods.Ration, 1).Card!.Rows[^1].Value);
    }

    [Fact]
    public void AnEmptySlotSaysHungerOnlyWhenAllAreEmpty()
    {
        Assert.Equal("Hungry", MealTips.Empty(hungry: true).Name);
        Assert.Equal("Room for a meal", MealTips.Empty(hungry: false).Name);
    }
}
