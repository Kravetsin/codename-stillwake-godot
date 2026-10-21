using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Shared;

/// <summary>An effect on a tool's card: a chip with its name, and a line telling it, in its colours, "rrggbb".</summary>
public sealed record CardEffect(string Name, string About, string Colour, string Ground);

/// <summary>A module's technology (0086), by the id of its icon, and what it does in one short line: "×2 speed · ×2 power".</summary>
public sealed record CardTech(string Tech, string About);

/// <summary>A key cap and what it does: "LMB" and "craft".</summary>
public sealed record CardKey(string Key, string Words);

/// <summary>One of a tool's tiles: its number as written and its label; the first, its accent, in amber.</summary>
public sealed record CardTile(string Value, string Label);

/// <summary>One item of a craft's cost: its name, how many are carried and needed, and whether that is short.</summary>
/// <param name="From">Where a raw item short comes from; null when it is not short, or is crafted itself.</param>
public sealed record CraftRow(string Item, string Name, int Have, int Need, bool Short, string? From = null);

/// <summary>
/// What a station adds at the bottom of an item's card: a recipe's "Craft" header with its seconds and
/// its cost row by row, and the keys a click there takes. An output gives only its keys.
/// </summary>
/// <param name="Seconds">A unit's time at the station, as written; null for no header and no rows.</param>
public sealed record StationBlock(string? Seconds, IReadOnlyList<CraftRow> Rows, IReadOnlyList<CardKey> Keys)
{
    public bool Equals(StationBlock? other) =>
        other is not null && Seconds == other.Seconds && Rows.SequenceEqual(other.Rows) && Keys.SequenceEqual(other.Keys);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Seconds);
        foreach (var row in Rows) hash.Add(row);
        foreach (var key in Keys) hash.Add(key);
        return hash.ToHashCode();
    }
}

/// <summary>
/// What a fed station makes of one item, as the card says it under "Smelts here": its yields, then how long one takes
/// at that station, as written. Equal by what it says.
/// </summary>
public sealed record CardOutput(IReadOnlyList<(string Item, int Count)> Items, string Time)
{
    public bool Equals(CardOutput? other) => other is not null && Time == other.Time && Items.SequenceEqual(other.Items);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Time);
        foreach (var item in Items) hash.Add(item);
        return hash.ToHashCode();
    }
}

/// <summary>
/// What the one tooltip shows over an item (<c>PointerTip</c>), by what the item is (the item card mockup): a tool its
/// picture large and live, its tiles and how it is used; a worn item its picture beside one number; anything else a
/// small label, its name and kind. A violet band while it is not analysed; what the open station does with it
/// (<see cref="Here"/>); a station's block at the bottom where one adds it. Equal by what it says, as <see cref="Tip"/> is.
/// </summary>
/// <param name="Item">The item, whose picture the card shows.</param>
/// <param name="Tiles">A tool's tiles, its key number first; null for an item that is no tool.</param>
/// <param name="Use">How it is used: its key and a hint.</param>
/// <param name="Unknown">Not analysed yet: the band.</param>
public sealed record ItemCard(
    string Item, string Name, string Kind, IReadOnlyList<CardTile>? Tiles, IReadOnlyList<CardEffect> Effects, CardKey? Use, bool Unknown)
{
    /// <summary>A tool: its card edged and its kind written in amber.</summary>
    public bool Tool => Tiles != null;

    /// <summary>Worn in an equipment slot: its card edged and its kind written in green.</summary>
    public bool Worn { get; init; }

    /// <summary>A worn item's one number, large, and what it is: the helmet lamp's reach; a battery shows its <see cref="Charge"/> instead.</summary>
    public CardTile? Headline { get; init; }

    /// <summary>A few words under <see cref="Headline"/>: the lamp's beam.</summary>
    public string? Detail { get; init; }

    /// <summary>What the station the item is shown in adds; null elsewhere.</summary>
    public StationBlock? Station { get; init; }

    /// <summary>What this one holds, a battery's charge; null for an item that holds none.</summary>
    public ChargeMeter? Charge { get; init; }

    /// <summary>What an electric tool draws while it works, in the units machines draw in, as a machine shows its own; null for anything else.</summary>
    public int? Draw { get; init; }

    /// <summary>What the open fed station or fire does with it, a line with its slot's mark: smelts, burns or grows here; <see cref="SlotMark.None"/> elsewhere.</summary>
    public SlotMark Here { get; init; }

    /// <summary>What the open fed station makes of it, on the line of <see cref="Here"/>; null for none.</summary>
    public CardOutput? Output { get; init; }

    /// <summary>
    /// A module's technology and what it does, a strip at its card's bottom (0086); a tool's module fitted, named on the same
    /// strip (0098); null for anything else.
    /// </summary>
    public CardTech? Tech { get; init; }

    /// <summary>The module fitted into this tool (0098), by item: its picture drawn with it; null for none.</summary>
    public string? Fitted { get; init; }

    /// <summary>Its picture: its item's, or the empty battery's for one holding no charge.</summary>
    public string Picture { get; init; } = Item;

    public bool Equals(ItemCard? other) =>
        other is not null && Item == other.Item && Name == other.Name && Kind == other.Kind
        && (Tiles == null ? other.Tiles == null : other.Tiles != null && Tiles.SequenceEqual(other.Tiles))
        && Effects.SequenceEqual(other.Effects) && Use == other.Use && Worn == other.Worn && Headline == other.Headline && Detail == other.Detail
        && Unknown == other.Unknown && Equals(Station, other.Station) && Charge == other.Charge && Picture == other.Picture && Draw == other.Draw
        && Here == other.Here && Equals(Output, other.Output) && Tech == other.Tech && Fitted == other.Fitted;

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Item);
        hash.Add(Name);
        hash.Add(Unknown);
        hash.Add(Station);
        hash.Add(Charge);
        hash.Add(Picture);
        hash.Add(Draw);
        hash.Add(Here);
        hash.Add(Output);
        hash.Add(Headline);
        hash.Add(Tech);
        hash.Add(Fitted);
        foreach (var effect in Effects) hash.Add(effect);
        foreach (var tile in Tiles ?? []) hash.Add(tile);
        return hash.ToHashCode();
    }
}
