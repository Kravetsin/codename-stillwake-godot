using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Features.Research;

/// <summary>A district of the board: its id (its caption is <c>research.district.&lt;id&gt;</c>), the lane of blocks it stands in, and its things in order.</summary>
public sealed record BoardDistrict(string Id, int Lane, IReadOnlyList<string> Things);

/// <summary>
/// The board's districts (board-islands), in flow order, as ids only: client presentation, so a table edit never touches the sim. Whoever adds a
/// thing to Content adds it here; <c>BoardDistrictsTests</c> names anything left out or listed twice. Contraband and loot no recipe uses go to raw.
/// </summary>
public static class BoardDistricts
{
    public static readonly IReadOnlyList<BoardDistrict> All =
    [
        new("raw", 0,
        [
            "rock", "scrapChunk", "hullPlate", "copperOre", "asteroidOre", "planks", "ironOre", "quartz", "canister", "contraband", "crystal", "ice",
            "alloyScrap", "magnetite", "opal", "photoPanel", "sulfur", "mica", "cloth", "hide", "arcShard",
        ]),
        new("smelt", 0, ["water", "iron", "silicon", "scrap", "copper", "glass", "gravel"]),
        new("parts", 0,
        [
            "display", "igniter", "coil", "motor", "plate", "deckDrive", "transceiver", "wire", "processor", "frame", "rotor", "solarCell", "nozzle",
            "circuit", "fuelPump", "tube", "gear", "arcModule",
        ]),
        new("machines", 0,
        [
            "drone", "salvageDrone", "vent", "generator", "electrolyser", "solarPanel", "treadWheel", "burnerGenerator", "biofuelGenerator", "lamp",
            "arcLamp", "charger", "capacitorBank", "furnace", "workbench", "tradeStation", "autoTradeStation", "former", "crusher", "smelter", "assembler",
            "growCapsule", "galley", "fermenter", "arcBench", "crate", "net", "droneBay", "tank", "cistern", "fuelTank", "dropPod", "computer",
            "radarAntenna", "crewShuttle", "engine",
        ]),
        new("farm", 1,
        [
            "biomass", "biofuel", "soil", "roast", "fat", "porridge", "hash", "stew", "ration", "glowberry", "broth", "seed", "spore", "stim",
            "cannedMeat", "fruit",
        ]),
        new("walls", 1,
        [
            "wallScrap", "wallCastScrap", "wallIron", "wallGlass", "wallMica", "wallHide", "wallSoil", "wallStone", "wallAlloy", "backWallScrap",
            "backWallCastScrap", "backWallIron", "backWallGlass", "backWallMica", "backWallHide", "backWallSoil", "backWallStone", "backWallAlloy", "floor",
            "ladder", "door", "hatch", "launchPlatform",
        ]),
        new("gear", 1,
        [
            "nail", "blastCharge", "grinder", "drill", "harpoon", "nailer", "pickaxe", "spear", "grapple", "sledgehammer", "screwdriver", "plasmaCutter",
            "fireAxe", "railgun", "powerDriver", "accumulator", "battery", "heatSuit", "jetpack", "cape", "regenSuit", "suit",
            "heatedHelmet", "heatHelmet", "heavyHelmet", "lightDrone", "helmet",
        ]),
        // The drone's forms (drone-lessons), «Спутник»: known ones show, the rest a «?» with no hint.
        new("companion", 1, Stillwake.Content.Lessons.Forms),
    ];

    private static readonly Dictionary<string, string> ById = All.SelectMany(d => d.Things.Select(t => (t, d.Id))).GroupBy(p => p.t, StringComparer.Ordinal)
        .ToDictionary(g => g.Key, g => g.First().Item2, StringComparer.Ordinal);

    /// <summary>The district <paramref name="id"/> stands in; null for a thing the board does not hold.</summary>
    public static string? Of(string id) => ById.GetValueOrDefault(id);
}
