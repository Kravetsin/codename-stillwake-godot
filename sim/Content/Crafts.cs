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
        // Free at the pod, made again should one be lost: without it nothing is built (0107), not even the furnace.
        new("screwdriver", [], Ticks: 20),
        // The harpoon of what the jetpack is made of, and less, so it opens first (0068).
        new("harpoon", [("tube", 1), ("coil", 1), ("wire", 4)], Ticks: 100),
        // The first gun (shots-are-the-sims): cheaper than the harpoon, after the first iron; the axe's time.
        new("nailer", [("tube", 1), ("plate", 2), ("wire", 2)], Ticks: 60),
        // Two scrap a nail, so the scrap a hero has too much of goes into nails made ahead: a second a nail at the pod.
        new("nail", [("scrap", 2)], Ticks: 4),
        // Free, a convention: the hero has to go and take them (2026-10-27-suits-are-worn). Five seconds each at the pod.
        new("helmet", [], Ticks: 20),
        new("suit", [], Ticks: 20),
        // Of the ruins' cloth and a grazer's hide, at the Former (2026-10-27-the-cape-is-worn): none before the first planet.
        new("cape", [("cloth", 2), ("hide", 2)], Ticks: 60),
        new("jetpack", [("frame", 1), ("tube", 2), ("coil", 1), ("circuit", 1), ("canister", 1), ("wire", 2)], Ticks: 160),
        new("battery", [("plate", 1), ("coil", 1), ("wire", 2), ("circuit", 1)], Ticks: 60),
        // A plate bent into a band, wire and a coil for its glowing bulb: the workbench's, so it comes before the first cave (0077).
        new("helmetLamp", [("plate", 1), ("wire", 2), ("coil", 1)], Ticks: 60),
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
        // The engine's parts, the last made: a nozzle pressed of the ruins' heat-proof alloy lined with copper, and a fuel pump
        // of a motor at the Former; an igniter of a crystal's spark at the circuit assembler.
        new("nozzle", [("alloyScrap", 4), ("copper", 2)], Ticks: 200),
        new("fuelPump", [("motor", 1), ("tube", 2), ("plate", 1)], Ticks: 160),
        new("igniter", [("crystal", 1), ("circuit", 1), ("wire", 2)], Ticks: 120),
        // At the arc bench (0099): a sky shard wound into a module an electric station or the drill takes (0086, 0096), in ten seconds.
        new("arcModule", [("arcShard", 1), ("coil", 2), ("circuit", 2), ("frame", 1)], Ticks: 200),
        // At the arc bench (0099): a battery wound round a sky shard with a planet's copper, three batteries' charge, in ten seconds.
        new("accumulator", [("arcShard", 1), ("battery", 1), ("copper", 2), ("circuit", 1)], Ticks: 200),
        // At the arc bench (2026-10-27-the-railgun): two rails of copper wound with coils round a sky shard, in ten seconds.
        new("railgun", [("arcShard", 1), ("copper", 4), ("coil", 2), ("tube", 1), ("frame", 1), ("circuit", 2)], Ticks: 200),
    ];

    private static readonly Dictionary<string, Craft> ByItem = All.ToDictionary(c => c.Item, StringComparer.Ordinal);

    /// <summary>The craft making <paramref name="item"/>; throws for an item no craft makes.</summary>
    public static Craft Get(string item) => ByItem[item];

    public static bool TryGet(string item, [MaybeNullWhen(false)] out Craft craft) => ByItem.TryGetValue(item, out craft);
}
