using System;
using System.Globalization;
using System.Linq;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using static Stillwake.Game.I18n.En;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// A tool's card, from content: the fire axe's damage, the seconds between its swings and the damage a
/// second they make, its effects, and the wreckage it breaks; its words from the i18n table.
/// </summary>
public static class ToolCards
{
    /// <summary>The card of <paramref name="item"/>, or null for an item that is no tool; <paramref name="unknown"/> while not analysed.</summary>
    public static ToolCard? Of(string item, bool unknown)
    {
        if (item != Spec.Axe.Item) return null;
        double every = (double)Spec.Axe.SwingTicks / World.TicksPerSecond;
        return new ToolCard(
            item, T($"item.{item}"), T($"item.{item}.kind"),
            Dps: Number(Spec.Axe.Damage / every),
            Damage: Number(Spec.Axe.Damage),
            Hit: T("card.seconds", ("n", Number(every))),
            Effects: Spec.Axe.Effects.Select(e => new CardEffect(T($"effect.{e.Id}"), T($"effect.{e.Id}.about"), e.Colour, e.Ground)).ToList(),
            Uses: Spec.Wreckage.All.Select(w => T($"item.{w.Item}")).ToList(),
            Key: T($"item.{item}.key"), Hint: T($"item.{item}.hint"), Unknown: unknown);
    }

    /// <summary><paramref name="n"/> with one decimal at most, half rounded up: 4, 5.3, 0.5.</summary>
    public static string Number(double n) =>
        Math.Round(n, 1, MidpointRounding.AwayFromZero).ToString("0.#", CultureInfo.InvariantCulture);
}
