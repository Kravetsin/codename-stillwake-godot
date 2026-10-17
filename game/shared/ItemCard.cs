using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Shared;

/// <summary>An effect on a tool's card: a chip with its name, and a line telling it, in its colours, "rrggbb".</summary>
public sealed record CardEffect(string Name, string About, string Colour, string Ground);

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
/// What the one tooltip shows over an item (<c>PointerTip</c>), the plaque of the card mockup: its icon
/// large, its name and kind. A tool adds its tiles, its effects, what it works on and how it is
/// used; anything else, what it goes into. A battery's charge under the head. A violet band while it is not
/// analysed; a station's block at the bottom where one adds it. Equal by what it says, as <see cref="Tip"/> is.
/// </summary>
/// <param name="Item">The item, whose icon heads the card.</param>
/// <param name="Tiles">A tool's tiles, its key number first; null for an item that is no tool.</param>
/// <param name="Uses">The chips: what a tool works on, or what any other item goes into.</param>
/// <param name="Use">How a tool is used: its key and a hint.</param>
/// <param name="Unknown">Not analysed yet: the band.</param>
public sealed record ItemCard(
    string Item, string Name, string Kind, IReadOnlyList<CardTile>? Tiles, IReadOnlyList<CardEffect> Effects,
    IReadOnlyList<string> Uses, CardKey? Use, bool Unknown)
{
    /// <summary>A tool: its card edged and its kind written in amber.</summary>
    public bool Tool => Tiles != null;

    /// <summary>What the station the item is shown in adds; null elsewhere.</summary>
    public StationBlock? Station { get; init; }

    /// <summary>What this one holds, a battery's charge, under the head; null for an item that holds none.</summary>
    public ChargeMeter? Charge { get; init; }

    /// <summary>What an electric tool draws while it works, in the units machines draw in, as a machine shows its own; null for anything else.</summary>
    public int? Draw { get; init; }

    /// <summary>The picture heading the card: its item's, or the empty battery's for one holding no charge.</summary>
    public string Picture { get; init; } = Item;

    public bool Equals(ItemCard? other) =>
        other is not null && Item == other.Item && Name == other.Name && Kind == other.Kind
        && (Tiles == null ? other.Tiles == null : other.Tiles != null && Tiles.SequenceEqual(other.Tiles))
        && Effects.SequenceEqual(other.Effects) && Uses.SequenceEqual(other.Uses) && Use == other.Use
        && Unknown == other.Unknown && Equals(Station, other.Station) && Charge == other.Charge && Picture == other.Picture && Draw == other.Draw;

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
        foreach (var effect in Effects) hash.Add(effect);
        foreach (var use in Uses) hash.Add(use);
        foreach (var tile in Tiles ?? []) hash.Add(tile);
        return hash.ToHashCode();
    }
}
