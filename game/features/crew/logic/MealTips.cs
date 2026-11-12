using System.Collections.Generic;
using Stillwake.Game.Shared;
using static Stillwake.Game.I18n.En;
using Diet = Stillwake.Content.Diet;
using Foods = Stillwake.Content.Foods;
using World = Stillwake.Sim.World;

namespace Stillwake.Game.Features.Crew;

/// <summary>What the HUD's meal slots say and how they move: a stand-in until the gauges' redesign places them.</summary>
public static class MealTips
{
    /// <summary>A meal pulses in its last minute, when the same food may be eaten again to refresh it.</summary>
    public static bool Pulsing(int left) => left <= Diet.RefreshWithin;

    /// <summary>What a food is called: pod paste is never an item, so it has a name of its own.</summary>
    public static string NameOf(string food) => food == Foods.Paste ? T("crew.podPaste") : T($"item.{food}");

    /// <summary>A meal's tip: its name, what it does for the body, and how long it lasts yet.</summary>
    public static Tip Of(string food, int left)
    {
        var rates = Foods.Of(food)?.Rates ?? new();
        var lines = new List<TipLine>();
        // Thousandths shown as whole percent, rounded down: 50 is 5%.
        if (rates.Calm > 0) lines.Add(new TipLine(T("crew.rateCalm", ("n", rates.Calm / 10)), Tone.Good));
        if (rates.Deep > 0) lines.Add(new TipLine(T("crew.rateDeep", ("n", rates.Deep / 10)), Tone.Good));
        if (rates.Work > 0) lines.Add(new TipLine(T("crew.rateWork", ("n", rates.Work / 10)), Tone.Good));
        if (lines.Count == 0) lines.Add(new TipLine(T("crew.rateNone"), Tone.Dim));
        int seconds = (left + World.TicksPerSecond - 1) / World.TicksPerSecond;
        lines.Add(new TipLine(T("crew.mealLeft", ("t", $"{seconds / 60}:{seconds % 60:00}")), Pulsing(left) ? Tone.Bad : Tone.Dim));
        return new Tip(NameOf(food), lines);
    }

    /// <summary>An empty slot's tip: hungry with all three empty, otherwise room for another meal.</summary>
    public static Tip Empty(bool hungry) =>
        hungry ? new Tip(T("crew.hungry"), new TipLine(T("crew.hungryWhy"), Tone.Bad)) : new Tip(T("crew.mealFree"), new TipLine(T("crew.mealHow"), Tone.Dim));
}
