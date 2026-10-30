using System.Diagnostics.CodeAnalysis;

namespace Stillwake.Content;

/// <summary>A few blueprints and crafts bought together at a tech station, for <paramref name="Price"/> credits.</summary>
/// <param name="Opens">What it opens: blueprints as the build menu names their cards, crafts by the item they make.</param>
public sealed record TechBlock(int Price, params string[] Opens)
{
    /// <summary>The block's id: the first thing it opens.</summary>
    public string Id => Opens[0];
}

/// <summary>A chain of blocks, bought left to right: a block needs the one before it in its branch, and nothing else.</summary>
public sealed record TechBranch(string Id, params TechBlock[] Blocks);

/// <summary>A stage of the tree: open once it and every tier before it have their deposits full, none for the first.</summary>
public sealed record TechTier(string Id, IReadOnlyList<(string Item, int Count)> Deposit, params TechBranch[] Branches);

/// <summary>
/// Knowing blueprints and crafts (2026-10-30-trade-and-tech). Blueprints are named as the build menu names their cards: a wall
/// or a back wall by its kind and material (<c>wallIron</c>, <c>backWallScrap</c>), anything else by the grid's name for it
/// (<c>floor</c>, <c>furnace</c>); a craft by the item it makes (<c>wire</c>). Either is known from the start or bought in a block
/// of <see cref="Tiers"/>. Cables are no blueprint and always known.
/// </summary>
public static class Research
{
    /// <summary>
    /// What the wreck knows from its start: what earns and makes the first parts, what a torn room is closed with, the fire axe
    /// wreckage is broken with. Both stations, so taking the torn room's apart never leaves a world unable to buy.
    /// </summary>
    public static readonly IReadOnlyList<string> StartKnown =
    [
        "furnace", "workbench", "crate", "tradeStation", "techStation", "wallScrap", "backWallScrap",
        "wire", "plate", "gear", "tube", "fireAxe", "screwdriver", "helmet", "suit",
    ];

    /// <summary>The tree, in order. Everything before the pod is had from what space gives; what a planet gives comes after it.</summary>
    public static readonly IReadOnlyList<TechTier> Tiers =
    [
        new("salvage", [],
            new("hull", new(5, "floor", "ladder", "lamp"), new(15, "door", "hatch"), new(25, "wallIron", "backWallIron")),
            new("parts", new TechBlock(10, "coil", "rotor")),
            new("tools", new(10, "nailer", "nail"), new(25, "harpoon"), new(15, "helmetLamp"))),
        new("press", [("wire", 20), ("plate", 15), ("gear", 10)],
            new("power", new TechBlock(60, "burnerGenerator")),
            new("forming", new(120, "former"), new(80, "frame")),
            new("air", new(60, "vent", "tank"), new(150, "generator")),
            new("gear", new TechBlock(200, "jetpack"))),
        new("processing", [("frame", 10), ("gear", 20), ("coil", 20)],
            new("crush", new TechBlock(250, "crusher")),
            new("smelt", new(350, "smelter"), new(100, "wallGlass", "backWallGlass")),
            new("electronics", new(400, "assembler"), new(250, "circuit"), new(450, "solarCell", "solarPanel"))),
        new("automation", [("circuit", 10), ("glass", 20), ("frame", 10)],
            new("farm", new(200, "soil", "wallSoil", "backWallSoil"), new(500, "growCapsule")),
            new("store", new(400, "battery", "charger"), new(400, "capacitorBank"), new(500, "drill")),
            new("drones", new TechBlock(1200, "drone", "droneBay"))),
        new("wayDown", [("circuit", 20), ("glass", 30), ("frame", 20), ("coil", 40)],
            new("pod", new(800, "deckDrive", "launchPlatform"), new(1200, "processor", "display"), new(1800, "dropPod")),
            new("signal", new(600, "transceiver"), new(1000, "computer"), new(1000, "radarAntenna"))),
        new("ground", [("copper", 20), ("mica", 10), ("hide", 5)],
            new("walls", new(100, "wallStone", "backWallStone"), new(150, "wallMica", "backWallMica"), new(150, "wallHide", "backWallHide")),
            new("wear", new(200, "canister"), new(400, "cape")),
            new("ruins", new(300, "arcLamp"), new(600, "wallAlloy", "backWallAlloy"))),
        new("arcAndEngine", [("alloyScrap", 20), ("magnetite", 10), ("crystal", 5), ("biofuel", 20)],
            new("arc", new(800, "motor"), new(1500, "arcBench"), new(1500, "arcModule"), new(1500, "accumulator"), new(2000, "railgun")),
            new("engine", new(2000, "nozzle"), new(2000, "fuelPump", "igniter"), new(25000, "engine"))),
    ];

    /// <summary>The tier named <paramref name="id"/>.</summary>
    public static bool TryTier(string id, [MaybeNullWhen(false)] out TechTier tier)
    {
        foreach (var t in Tiers)
            if (t.Id == id)
            {
                tier = t;
                return true;
            }
        tier = null;
        return false;
    }

    /// <summary>The block named <paramref name="id"/>, with its tier and the block before it in its branch, if any.</summary>
    public static (TechBlock Block, TechTier Tier, TechBlock? Before)? Block(string id)
    {
        foreach (var tier in Tiers)
            foreach (var branch in tier.Branches)
                for (int i = 0; i < branch.Blocks.Length; i++)
                    if (branch.Blocks[i].Id == id) return (branch.Blocks[i], tier, i > 0 ? branch.Blocks[i - 1] : null);
        return null;
    }
}
