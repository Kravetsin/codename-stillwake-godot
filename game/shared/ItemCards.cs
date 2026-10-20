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
/// An item's card, from content: its name and kind; a tool's tiles and effects — a weapon's (the fire axe's,
/// the drill's) damage a second, damage and seconds between hits; the harpoon's range, reload and reel speed; a
/// worn item's one number, the helmet lamp's reach. Its words from the i18n table.
/// </summary>
public static class ItemCards
{
    /// <summary>Whether <paramref name="item"/> is a tool: a weapon (the fire axe, the drill) or the harpoon.</summary>
    public static bool IsTool(string item) => Spec.Weapons.Of(item) != null || item == Spec.Harpoon.Item;

    /// <summary>
    /// Whether <paramref name="item"/> shows the violet "?" and its card the band: it needs analysis, being in
    /// some cost (<see cref="Analysable"/>), and is not among <paramref name="analysed"/>; never in creative mode.
    /// </summary>
    public static bool Unknown(string item, bool creative, IEnumerable<string> analysed) =>
        !creative && Analysable.NeedsAnalysis(item) && !analysed.Contains(item);

    /// <summary>The card of <paramref name="item"/>; <paramref name="unknown"/> while not analysed.</summary>
    public static ItemCard Of(string item, bool unknown) => Card(item, unknown) with { Draw = Spec.Items.Get(item).Draw };

    private static ItemCard Card(string item, bool unknown)
    {
        string name = T($"item.{item}"), kind = T($"item.{item}.kind");
        // Anything used by a key of its own says so: a canister breathed, a jetpack worn (0047).
        var key = Table.ContainsKey($"item.{item}.key") ? new CardKey(T($"item.{item}.key"), T($"item.{item}.hint")) : null;
        if (!IsTool(item))
        {
            var card = new ItemCard(item, name, kind, null, [], key, unknown);
            return Spec.Items.Get(item).Equip == null ? card : Worn(card);
        }
        if (item == Spec.Harpoon.Item) return new ItemCard(item, name, kind, HarpoonTiles(), [], key, unknown);
        var weapon = Spec.Weapons.Of(item)!;
        double every = (double)weapon.Pace / World.TicksPerSecond;
        return new ItemCard(
            item, name, kind,
            [
                new CardTile(Number(weapon.Damage / every), T("card.dps")),
                new CardTile(Number(weapon.Damage), T("card.damage")),
                new CardTile(T("card.seconds", ("n", Number(every))), T("card.hit")),
            ],
            weapon.Effects.Select(e => new CardEffect(T($"effect.{e.Id}"), T($"effect.{e.Id}.about"), e.Colour, e.Ground)).ToList(),
            key, unknown);
    }

    /// <summary>
    /// A worn item's card: the helmet lamp's reach in cells and its beam across, in degrees, from the light it gives
    /// (<see cref="Light.HeadLampReach"/>); a battery's number is its charge, set per stack; the jetpack has none.
    /// </summary>
    private static ItemCard Worn(ItemCard card)
    {
        if (card.Item != Spec.HelmetLamp.Item) return card with { Worn = true };
        int beam = (int)Math.Round(Light.HeadLampHalfAngle * 2 * 180 / Math.PI);
        return card with
        {
            Worn = true,
            Headline = new CardTile(Light.HeadLampReach.ToString(CultureInfo.InvariantCulture), T("card.reach")),
            Detail = T("card.beam", ("n", beam)),
        };
    }

    /// <summary>
    /// What fed <paramref name="station"/> makes of one <paramref name="item"/>: its yields, and the time one takes there,
    /// <c>SmeltTicks</c> at the station's speed, in seconds or past a minute in minutes, one decimal at most; null for an
    /// item it makes nothing of.
    /// </summary>
    public static CardOutput? OutputAt(string item, Spec.Station station)
    {
        var spec = Spec.Items.Get(item);
        if (spec.Yields is not { Count: > 0 } yields || spec.SmeltTicks is not int ticks) return null;
        // Speed is in thousandths of full speed, as a craft's time at a station is worked out.
        double seconds = (double)ticks * 1000 / station.Speed / World.TicksPerSecond;
        string time = seconds < 60 ? T("card.seconds", ("n", Number(seconds))) : T("card.minutes", ("n", Number(seconds / 60)));
        return new CardOutput(yields, time);
    }

    /// <summary>The harpoon's tiles: how far its rod flies, in cells; the seconds till it fires again; the cells a second it reels.</summary>
    private static List<CardTile> HarpoonTiles() =>
    [
        new(Number(Spec.Harpoon.Range / 1000.0), T("card.range")),
        new(T("card.seconds", ("n", Number((double)Spec.Harpoon.CooldownTicks / World.TicksPerSecond))), T("card.reload")),
        new(T("card.perSecond", ("n", Number(Spec.Harpoon.ReelSpeed * World.TicksPerSecond / 1000.0))), T("card.reel")),
    ];

    /// <summary>A craft's name, its item; a blueprint's, its tool and material: "Wall: iron".</summary>
    public static string NameOf(string id, Placement? placement) => placement is { } p
        ? p.Material is { } m ? $"{T($"tool.{p.Kind}")}: {T($"material.{m}")}" : T($"tool.{p.Kind}")
        : T($"item.{id}");

    /// <summary><paramref name="n"/> with one decimal at most, half rounded up: 4, 5.3, 0.5.</summary>
    public static string Number(double n) =>
        Math.Round(n, 1, MidpointRounding.AwayFromZero).ToString("0.#", CultureInfo.InvariantCulture);
}
