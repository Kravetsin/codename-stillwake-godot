using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Research;
using static Stillwake.Game.I18n.En;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Shared;

/// <summary>
/// An item's card, from content: its name and kind; the fire axe's damage, the seconds between its swings
/// and the damage a second they make, its effects and the wreckage it breaks; for anything else, the
/// crafts, blueprints and cables whose cost has it. Its words from the i18n table.
/// </summary>
public static class ItemCards
{
    /// <summary>The most chips naming what an item goes into; past them, one more says how many are left.</summary>
    public const int MostInto = 4;

    /// <summary>Whether <paramref name="item"/> is a tool: the fire axe, the only one yet.</summary>
    public static bool IsTool(string item) => item == Spec.Axe.Item;

    /// <summary>The card of <paramref name="item"/>; <paramref name="unknown"/> while not analysed.</summary>
    public static ItemCard Of(string item, bool unknown)
    {
        string name = T($"item.{item}"), kind = T($"item.{item}.kind");
        if (!IsTool(item)) return new ItemCard(item, name, kind, null, [], GoesInto(item), null, unknown);
        double every = (double)Spec.Axe.SwingTicks / World.TicksPerSecond;
        return new ItemCard(
            item, name, kind,
            new ToolTiles(Number(Spec.Axe.Damage / every), Number(Spec.Axe.Damage), T("card.seconds", ("n", Number(every)))),
            Spec.Axe.Effects.Select(e => new CardEffect(T($"effect.{e.Id}"), T($"effect.{e.Id}.about"), e.Colour, e.Ground)).ToList(),
            Spec.Wreckage.All.Select(w => T($"item.{w.Item}")).ToList(),
            new CardKey(T($"item.{item}.key"), T($"item.{item}.hint")), unknown);
    }

    /// <summary>
    /// What <paramref name="item"/> goes into: the crafts, then the blueprints, then the cable whose cost
    /// has it, in content's order, <see cref="MostInto"/> at most and "+N" for the rest.
    /// </summary>
    public static IReadOnlyList<string> GoesInto(string item)
    {
        var all = Spec.Crafts.All.Where(c => Has(c.Cost, item)).Select(c => NameOf(c.Item, null))
            .Concat(Blueprints.All.Where(b => Has(b.Cost, item)).Select(b => NameOf(b.Id, b.Placement)))
            .Concat(Has(Spec.Power.CableCost, item) ? [T("tool.cable")] : [])
            .Distinct()
            .ToList();
        return all.Count <= MostInto ? all : [.. all.Take(MostInto), T("card.more", ("n", all.Count - MostInto))];
    }

    /// <summary>A craft's name, its item; a blueprint's, its tool and material: "Wall: iron".</summary>
    public static string NameOf(string id, Placement? placement) => placement is { } p
        ? p.Material is { } m ? $"{T($"tool.{p.Kind}")}: {T($"material.{m}")}" : T($"tool.{p.Kind}")
        : T($"item.{id}");

    /// <summary><paramref name="n"/> with one decimal at most, half rounded up: 4, 5.3, 0.5.</summary>
    public static string Number(double n) =>
        Math.Round(n, 1, MidpointRounding.AwayFromZero).ToString("0.#", CultureInfo.InvariantCulture);

    private static bool Has(IReadOnlyList<(string Item, int Count)> cost, string item) => cost.Any(c => c.Item == item);
}
