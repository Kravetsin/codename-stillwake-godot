using System.Diagnostics.CodeAnalysis;

namespace Stillwake.Content;

/// <summary>A part a queued station makes, one unit at a time: what one unit costs and how long it takes.</summary>
/// <param name="Item">The item made, one a unit; the craft is named by it.</param>
/// <param name="Cost">Whole units of items one unit takes: all of a paid craft's taken when it is queued, one fed from a station's inputs a unit's as it begins.</param>
/// <param name="Ticks">Ticks one unit takes at full speed, 20 to a second.</param>
public sealed record Craft(string Item, IReadOnlyList<(string Item, int Count)> Cost, int Ticks);

/// <summary>Every craft, by the item made: what each station of <see cref="Stations"/> makes is in its rows (the crafting module).</summary>
public static class Crafts
{
    public static readonly IReadOnlyList<Craft> All =
    [
        new("wire", [("scrap", 2)], Ticks: 20),
        new("plate", [("iron", 2)], Ticks: 40),
        new("gear", [("iron", 1)], Ticks: 40),
        new("tube", [("iron", 1)], Ticks: 40),
        new("rotor", [("gear", 1), ("wire", 2)], Ticks: 60),
        new("frame", [("plate", 2)], Ticks: 80),
        new("coil", [("wire", 3)], Ticks: 60),
        new("fireAxe", [("iron", 2), ("planks", 1)], Ticks: 60),
        // Made again of scrap should one be lost: without it nothing is built (0107).
        new("screwdriver", [("scrap", 2)], Ticks: 20),
        // The harpoon of what the jetpack is made of, and less, so it opens first (0068).
        new("harpoon", [("tube", 1), ("coil", 1), ("wire", 4)], Ticks: 100),
        new("jetpack", [("frame", 1), ("tube", 2), ("coil", 1), ("circuit", 1), ("canister", 1), ("wire", 2)], Ticks: 160),
        new("battery", [("plate", 1), ("coil", 1), ("wire", 2), ("circuit", 1)], Ticks: 60),
        // A plate bent into a band, wire, and silicon from rock for its glowing bulb: cheap, so it comes before the first cave (0077).
        new("helmetLamp", [("plate", 1), ("wire", 2), ("silicon", 1)], Ticks: 60),
        new("drill", [("frame", 1), ("rotor", 1), ("coil", 1), ("gear", 2), ("tube", 1), ("circuit", 2)], Ticks: 160),
        // Electronics, at the circuit assembler: of silicon and wire, and with glass, a solar cell.
        new("circuit", [("silicon", 1), ("wire", 2)], Ticks: 80),
        new("solarCell", [("silicon", 1), ("glass", 1), ("wire", 1)], Ticks: 100),
        new("processor", [("circuit", 4), ("silicon", 2), ("coil", 1)], Ticks: 240),
        new("display", [("glass", 2), ("circuit", 1), ("wire", 4)], Ticks: 160),
        new("transceiver", [("coil", 3), ("circuit", 2), ("tube", 1), ("glass", 1)], Ticks: 200),
        // Pressed at the Former only: a grow capsule's bed (0060).
        new("soil", [("rock", 1), ("planks", 1)], Ticks: 60),
        // At the Former only: a rotor wound with the planet's copper round magnetite from its deepest band.
        new("motor", [("rotor", 1), ("copper", 2), ("magnetite", 1)], Ticks: 120),
        // At the circuit assembler: a drone, of a rotor, plates, a circuit and the battery it will run on (0062).
        new("drone", [("rotor", 1), ("plate", 2), ("circuit", 1), ("battery", 1)], Ticks: 160),
        new("deckDrive", [("circuit", 2), ("rotor", 1), ("coil", 1)], Ticks: 200),
        // At the arc bench (0099): a sky shard wound into a module an electric station or the drill takes (0086, 0096), in ten seconds.
        new("arcModule", [("arcShard", 1), ("coil", 2), ("circuit", 2), ("frame", 1)], Ticks: 200),
        // At the arc bench (0099): a battery wound round a sky shard with a planet's copper, three batteries' charge, in ten seconds.
        new("accumulator", [("arcShard", 1), ("battery", 1), ("copper", 2), ("circuit", 1)], Ticks: 200),
    ];

    private static readonly Dictionary<string, Craft> ByItem = All.ToDictionary(c => c.Item, StringComparer.Ordinal);

    /// <summary>The craft making <paramref name="item"/>; throws for an item no craft makes.</summary>
    public static Craft Get(string item) => ByItem[item];

    public static bool TryGet(string item, [MaybeNullWhen(false)] out Craft craft) => ByItem.TryGetValue(item, out craft);
}
