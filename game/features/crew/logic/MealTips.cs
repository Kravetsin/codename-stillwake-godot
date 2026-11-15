using Stillwake.Game.Shared;
using static Stillwake.Game.I18n.En;
using Diet = Stillwake.Content.Diet;
using Foods = Stillwake.Content.Foods;

namespace Stillwake.Game.Features.Crew;

/// <summary>What the HUD's meal slots say and how they move: a meal's food card with its time left, an empty slot's words.</summary>
public static class MealTips
{
    /// <summary>A meal pulses in its last minute, when the same food may be eaten again to refresh it.</summary>
    public static bool Pulsing(int left) => left <= Diet.RefreshWithin;

    /// <summary>What a food is called: pod paste is never an item, so it has a name of its own.</summary>
    public static string NameOf(string food) => food == Foods.Paste ? T("crew.podPaste") : T($"item.{food}");

    /// <summary>
    /// A meal's tip: its food card (2026-11-14-food-and-gear-cards) with the time it has left in place of how long it lasts, no key
    /// and no price.
    /// </summary>
    public static Tip Of(string food, int left)
    {
        var card = ItemCards.Meal(food, left, NameOf(food), food == Foods.Paste ? T("crew.podPasteKind") : T($"item.{food}.kind"));
        return new Tip(card.Name) { Card = card };
    }

    /// <summary>An empty slot's tip: hungry with all three empty, otherwise room for another meal.</summary>
    public static Tip Empty(bool hungry) =>
        hungry ? new Tip(T("crew.hungry"), new TipLine(T("crew.hungryWhy"), Tone.Bad)) : new Tip(T("crew.mealFree"), new TipLine(T("crew.mealHow"), Tone.Dim));
}
