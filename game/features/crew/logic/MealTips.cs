using System;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using static Stillwake.Game.I18n.En;
using Diet = Stillwake.Content.Diet;

namespace Stillwake.Game.Features.Crew;

/// <summary>What the HUD's meal slots and hunger sign say and how they move: a meal's food card with its time left, hunger's costs.</summary>
public static class MealTips
{
    /// <summary>A meal pulses in its last minute, when the same food may be eaten again to refresh it.</summary>
    public static bool Pulsing(int left) => left <= Diet.RefreshWithin;

    /// <summary>
    /// What the vitals' last dial is after the meals (hud-vitals): the purge spot while there is a meal to throw up (as the sim's
    /// <c>PurgeProblem</c> has it); with none, hunger's sign, lit while hungry, dim while not yet (creative, where nobody hungers).
    /// </summary>
    public static LastDial LastOf(int meals, bool hungry) => meals > 0 ? LastDial.Purge : hungry ? LastDial.Hungry : LastDial.Unfed;

    /// <summary>
    /// The time a meal has left under its dial, from its <paramref name="ticks"/> (format A1): whole minutes rounded up, "32m";
    /// seconds in its last minute, "45s", a part second counted whole as on its card (<see cref="ItemCards.Clock"/>).
    /// </summary>
    public static string Clock(int ticks)
    {
        int seconds = (Math.Max(0, ticks) + World.TicksPerSecond - 1) / World.TicksPerSecond;
        return seconds > 60 ? T("crew.mealMinutes", ("n", (seconds + 59) / 60)) : T("crew.mealSeconds", ("n", seconds));
    }

    /// <summary>How much more air a hungry hero's running breath costs than a fed one's, in percent.</summary>
    public static int SprintPercent => (Diet.HungrySprint - Diet.Sprint) * 100 / Diet.Sprint;

    /// <summary>How much slower a hungry hero works than a fed one before food's rates, in percent.</summary>
    public static int WorkPercent => (Diet.Work - Diet.HungryWork) * 100 / Diet.Work;

    /// <summary>
    /// A meal's tip: its food card (2026-11-14-food-and-gear-cards) with the time it has left in place of how long it lasts, no key
    /// and no price.
    /// </summary>
    public static Tip Of(string food, int left)
    {
        var card = ItemCards.Meal(food, left, T($"item.{food}"), T($"item.{food}.kind"));
        return new Tip(card.Name) { Card = card };
    }

    /// <summary>The hunger sign's tip, and an empty slot's while hungry: what hunger costs, then what ends it.</summary>
    public static Tip Hunger() => new(T("crew.hungry"),
        new TipLine(T("crew.hungerSprint", ("n", SprintPercent)), Tone.Bad),
        new TipLine(T("crew.hungerWork", ("n", WorkPercent)), Tone.Bad),
        new TipLine(T("crew.hungryWhy"), Tone.Dim));

    /// <summary>Hunger's dial's tip, with no meal eaten: what hunger costs while hungry, otherwise room for a meal.</summary>
    public static Tip Empty(bool hungry) => hungry ? Hunger() : new Tip(T("crew.mealFree"), new TipLine(T("crew.mealHow"), Tone.Dim));
}

/// <summary>The vitals' last dial after the meals (<see cref="MealTips.LastOf"/>).</summary>
public enum LastDial
{
    /// <summary>The purge spot: a meal to throw up.</summary>
    Purge,

    /// <summary>Hunger's sign, lit and breathing ochre: no meal, hungry.</summary>
    Hungry,

    /// <summary>Hunger's sign, dim and still: no meal, not hungry.</summary>
    Unfed,
}
