using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Shared;

/// <summary>A module's technology (0086), by the id of its icon, and what it does in one short line: "×2 speed · ×2 power".</summary>
public sealed record CardTech(string Tech, string About);

/// <summary>A key cap and what it does: "LMB" and "craft"; an item's own key alone, the player finding out what it does.</summary>
public sealed record CardKey(string Key, string Words = "");

/// <summary>
/// One of a tool's numbers, a row of its card (2026-11-07-tool-card-rows): its value as written and its label, its sign and the
/// group it is listed in. With modules fitted (0098), how it differs from the plain tool's, <see cref="Boost"/>; null where it does
/// not. A worn item's one number is one too, with no sign.
/// </summary>
public sealed record CardTile(string Value, string Label, CardBoost? Boost = null)
{
    /// <summary>Its icon and hue; none for a worn item's number.</summary>
    public CardSign Sign { get; init; }

    /// <summary>The group it is listed under on its card.</summary>
    public CardGroup Group { get; init; }

    /// <summary>A small word after its label, what it hits: "wrecks"; null for none.</summary>
    public string? Note { get; init; }

    /// <summary>A number the tool does not have, written as words and dimmed, its sign struck: the cutter's block damage.</summary>
    public bool Off { get; init; }
}

/// <summary>What a tool's number is, its icon and hue on the card (2026-11-07-tool-card-rows).</summary>
public enum CardSign { None, Damage, Block, Pace, Pierce, Arc, Reach, Reel, Battery, Loot }

/// <summary>The groups a tool's numbers are listed under, in this order: its hit, its shot or its blast, its reach, what is special.</summary>
public enum CardGroup { Hit, Shot, Blast, Reach, Special }

/// <summary>
/// How a tool's number differs with its modules fitted from the plain tool's (0098): in the colour of the technology of the first
/// module that changes it, <see cref="Tech"/>, up or down, and by how many times, "×2".
/// </summary>
public sealed record CardBoost(string Tech, bool Up, string Times);

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
/// What a fed station makes of one smelt of an item, as the card says it under "Smelts here": how many it takes, its
/// yields, its byproducts by chance, then how long one takes at that station, as written. Equal by what it says.
/// </summary>
/// <param name="Takes">How many of the item one smelt takes; "×N" before the arrow above 1.</param>
/// <param name="Chances">Byproducts, each at its most, and one in how many smelts it comes: 1 for every one.</param>
public sealed record CardOutput(
    IReadOnlyList<(string Item, int Count)> Items, string Time, int Takes = 1,
    IReadOnlyList<(string Item, int Count, int OneIn)>? Chances = null)
{
    public bool Equals(CardOutput? other) =>
        other is not null && Time == other.Time && Takes == other.Takes && Items.SequenceEqual(other.Items)
        && (Chances ?? []).SequenceEqual(other.Chances ?? []);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Time);
        hash.Add(Takes);
        foreach (var item in Items) hash.Add(item);
        foreach (var chance in Chances ?? []) hash.Add(chance);
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
/// <param name="Price">What a unit sells for at a trade station; null for what no one buys.</param>
public sealed record ItemCard(
    string Item, string Name, string Kind, IReadOnlyList<CardTile>? Tiles, CardKey? Use, int? Price)
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

    /// <summary>What an electric tool draws while it works, in the units machines draw in, as a machine shows its own, its modules in it; null for anything else.</summary>
    public int? Draw { get; init; }

    /// <summary>How <see cref="Draw"/> differs from the plain tool's with its modules fitted (0098); null where it does not.</summary>
    public CardBoost? DrawBoost { get; init; }

    /// <summary>What the open fed station or fire does with it, a line with its slot's mark: smelts, burns or grows here; <see cref="SlotMark.None"/> elsewhere.</summary>
    public SlotMark Here { get; init; }

    /// <summary>What the open fed station makes of it, on the line of <see cref="Here"/>; null for none.</summary>
    public CardOutput? Output { get; init; }

    /// <summary>A module's technology and what it does, a strip at its card's bottom (0086); null for anything else.</summary>
    public CardTech? Tech { get; init; }

    /// <summary>
    /// The modules fitted into this tool (0098), by item, as fitted: its picture put together with theirs, each named on a strip of
    /// its tech at the card's bottom; none for most.
    /// </summary>
    public IReadOnlyList<string> Fitted { get; init; } = [];

    /// <summary>A line on what a plain item does, under its kind, where its table has one (<c>item.&lt;id&gt;.about</c>): the salvage drone's; null for most.</summary>
    public string? About { get; init; }

    /// <summary>Its picture: its item's, or the empty battery's for one holding no charge.</summary>
    public string Picture { get; init; } = Item;

    public bool Equals(ItemCard? other) =>
        other is not null && Item == other.Item && Name == other.Name && Kind == other.Kind
        && (Tiles == null ? other.Tiles == null : other.Tiles != null && Tiles.SequenceEqual(other.Tiles))
        && Use == other.Use && Worn == other.Worn && Headline == other.Headline && Detail == other.Detail
        && Price == other.Price && Equals(Station, other.Station) && Charge == other.Charge && Picture == other.Picture && Draw == other.Draw
        && Here == other.Here && Equals(Output, other.Output) && Tech == other.Tech && Fitted.SequenceEqual(other.Fitted)
        && DrawBoost == other.DrawBoost && About == other.About;

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Item);
        hash.Add(Name);
        hash.Add(Price);
        hash.Add(Station);
        hash.Add(Charge);
        hash.Add(Picture);
        hash.Add(Draw);
        hash.Add(Here);
        hash.Add(Output);
        hash.Add(Headline);
        hash.Add(Tech);
        hash.Add(DrawBoost);
        foreach (var module in Fitted) hash.Add(module);
        foreach (var tile in Tiles ?? []) hash.Add(tile);
        return hash.ToHashCode();
    }
}
