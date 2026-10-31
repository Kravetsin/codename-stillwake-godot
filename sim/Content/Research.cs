namespace Stillwake.Content;

/// <summary>
/// A node of the tech tree: a block opening one blueprint or craft, bought with credits, or the gate of resources deposited for good that
/// opens a ring of blocks (2026-10-30-tech-rings).
/// </summary>
/// <param name="Id">Its name: a block's the one blueprint or craft it opens, named as <see cref="Research"/> says; a gate's own.</param>
/// <param name="Needs">The nodes that must be done first: their ids. A block's is its ring's gate, a gate's the gate before it.</param>
/// <param name="Deposit">What a gate asks for. None for a block.</param>
/// <param name="Times">How many times its worth a block costs.</param>
/// <param name="Tab">The id of the tab it is drawn on.</param>
/// <param name="Ring">Its ring on that tab, counted from the middle; a gate's is the ring it opens.</param>
/// <param name="Branch">The branch it stands on; none for a gate, which stands in the middle.</param>
public sealed record TechNode(string Id, string[] Needs, IReadOnlyList<(string Item, int Count)> Deposit, int Times, string Tab, int Ring, string? Branch)
{
    /// <summary>Whether it is opened by resources rather than bought.</summary>
    public bool Gate => Deposit.Count > 0;
}

/// <summary>What a tab's ring is written as: what its gate asks for, none for a ring open from the start, how many times its worth a block of it costs, and its blocks.</summary>
public sealed record TechRing(string? Gate, IReadOnlyList<(string Item, int Count)> Deposit, int Times, params (string Branch, string Id, int? Times)[] Blocks);

/// <summary>A tab of the tree: its branches, going round from the top, and its rings, from the middle out; its first gate waits on <see cref="After"/>, if any.</summary>
public sealed record TechTab(string Id, string[] Branches, string? After, params TechRing[] Rings);

/// <summary>
/// Knowing blueprints and crafts (2026-10-30-tech-rings). Blueprints are named as the build menu names their cards: a wall
/// or a back wall by its kind and material (<c>wallIron</c>, <c>backWallScrap</c>), anything else by the grid's name for it
/// (<c>floor</c>, <c>furnace</c>); a craft by the item it makes (<c>wire</c>). Either is known from the start or opened by a
/// node of <see cref="Tabs"/>. Cables are no blueprint and always known.
/// </summary>
public static class Research
{
    /// <summary>
    /// What the wreck knows from its start: what earns, what a torn room is closed with, the fire axe wreckage is broken with, what
    /// a castaway's pod makes. The trade station is of raw salvage, so taking one apart never leaves a world unable to trade. The
    /// solar panel too: the wreck's own, taken apart, gives back the photosensitive panel nothing makes, and builds again.
    /// </summary>
    public static readonly IReadOnlyList<string> StartKnown =
    [
        "tradeStation", "wallScrap", "backWallScrap", "fireAxe", "screwdriver", "helmet", "suit", "solarPanel",
    ];

    /// <summary>How many times its worth a craft costs on top of its ring's: a blueprint already costs what it is built of, a craft only a unit.</summary>
    public const int CraftTimes = 3;

    private const string Better = "upgrades", Materials = "materials", Structures = "structures", Machines = "machines", Power = "powerAndAir", Automation = "automation", Gear = "gear", Arc = "arc", Engine = "engine";

    private static (string Branch, string Id, int? Times) Block(string branch, string id) => (branch, id, null);

    /// <summary>An upgrade: its price its own, whatever its ring's.</summary>
    private static (string Branch, string Id, int? Times) Up(string id) => (Better, id, 1);

    private static (string Branch, string Id, int? Times) Dear(int times, string branch, string id) => (branch, id, times);

    /// <summary>The tree, by tab. Everything before the pod is had from what space gives; what a planet gives comes after it.</summary>
    public static readonly IReadOnlyList<TechTab> Tabs =
    [
        new("ship", [Materials, Structures, Machines, Power, Automation, Gear, Better], null,
            new(null, [], 1,
                Block(Machines, "workbench"),
                Block(Machines, "furnace"),
                Up("bag1")),
            new("firstMetal", [("scrap", 20), ("iron", 10)], 1,
                Block(Materials, "wire"),
                Block(Materials, "plate"),
                Block(Materials, "gear"),
                Block(Materials, "tube"),
                Block(Structures, "floor"),
                Block(Structures, "ladder"),
                Block(Structures, "lamp"),
                Block(Gear, "nail"),
                Block(Gear, "nailer"),
                Up("bag2"),
                Up("tradeSlots1")),
            new("press", [("wire", 20), ("plate", 15), ("gear", 10)], 1,
                Block(Materials, "coil"),
                Block(Materials, "rotor"),
                Block(Structures, "door"),
                Block(Structures, "hatch"),
                Block(Structures, "wallIron"),
                Block(Structures, "backWallIron"),
                Block(Automation, "crate"),
                Block(Gear, "harpoon"),
                Block(Gear, "helmetLamp"),
                Up("bag3"),
                Up("tradeSlots2")),
            new("machinery", [("plate", 30), ("coil", 15), ("rotor", 8)], 1,
                Block(Materials, "frame"),
                Block(Machines, "former"),
                Block(Power, "burnerGenerator"),
                Block(Power, "vent"),
                Block(Power, "cistern"),
                Block(Power, "tank"),
                Up("bag4")),
            new("processing", [("frame", 10), ("gear", 20), ("coil", 20)], 1,
                Block(Structures, "wallGlass"),
                Block(Structures, "backWallGlass"),
                Block(Machines, "crusher"),
                Block(Machines, "smelter"),
                Block(Power, "generator"),
                Block(Gear, "jetpack"),
                Up("bag5"),
                Up("tradeSlots3")),
            new("electronics", [("frame", 15), ("glass", 10), ("tube", 20)], 1,
                Block(Materials, "circuit"),
                Block(Materials, "solarCell"),
                Block(Machines, "assembler"),
                Block(Machines, "autoTradeStation"),
                Up("bag6"),
                Up("tradeSlots4")),
            new("automation", [("circuit", 10), ("glass", 20), ("frame", 10)], 1,
                Block(Materials, "soil"),
                Block(Structures, "wallSoil"),
                Block(Structures, "backWallSoil"),
                Block(Machines, "growCapsule"),
                Block(Power, "battery"),
                Block(Power, "charger"),
                Block(Power, "capacitorBank"),
                Block(Automation, "drone"),
                Block(Automation, "droneBay"),
                Block(Gear, "drill"),
                Up("bag7"),
                Up("belt1")),
            new("wayDown", [("circuit", 20), ("glass", 30), ("frame", 20), ("coil", 40)], 1,
                Block(Materials, "processor"),
                Block(Materials, "display"),
                Block(Materials, "transceiver"),
                Block(Structures, "launchPlatform"),
                Block(Machines, "computer"),
                Block(Automation, "deckDrive"),
                Block(Automation, "dropPod"),
                Block(Automation, "radarAntenna"),
                Up("bag8"),
                Up("tradeSlots5")),
            new("ground", [("copper", 20), ("mica", 10), ("hide", 5)], 2,
                Block(Materials, "canister"),
                Block(Structures, "wallStone"),
                Block(Structures, "backWallStone"),
                Block(Structures, "wallMica"),
                Block(Structures, "backWallMica"),
                Block(Structures, "wallHide"),
                Block(Structures, "backWallHide"),
                Block(Structures, "wallAlloy"),
                Block(Structures, "backWallAlloy"),
                Block(Structures, "arcLamp"),
                Block(Gear, "cape"),
                Up("bag9"),
                Up("tradeSlots6"))),
        new("tech", [Arc, Engine, Better], "ground",
            new("arc", [("alloyScrap", 20), ("magnetite", 10), ("crystal", 5), ("biofuel", 20)], 2,
                Block(Arc, "motor"),
                Block(Arc, "arcBench"),
                Block(Arc, "arcModule"),
                Block(Arc, "accumulator"),
                Block(Arc, "railgun"),
                Up("bag10"),
                Up("belt2")),
            new("enginePlans", [("motor", 4), ("copper", 30), ("alloyScrap", 30)], 2,
                Block(Engine, "nozzle"),
                Block(Engine, "fuelPump"),
                Block(Engine, "igniter"),
                Dear(8, Engine, "engine"),
                Up("bag11"))),
    ];

    /// <summary>Every node: tab by tab, ring by ring, a ring's gate before its blocks.</summary>
    public static readonly IReadOnlyList<TechNode> Nodes = [.. Tabs.SelectMany(NodesOf)];

    private static readonly Dictionary<string, TechNode> ById = Nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);

    /// <summary>The node named <paramref name="id"/>; null for none.</summary>
    public static TechNode? Node(string id) => ById.GetValueOrDefault(id);

    private static IEnumerable<TechNode> NodesOf(TechTab tab)
    {
        string? gate = tab.After;
        for (int ring = 0; ring < tab.Rings.Length; ring++)
        {
            var each = tab.Rings[ring];
            if (each.Gate is { } id)
            {
                yield return new TechNode(id, gate == null ? [] : [gate], each.Deposit, 1, tab.Id, ring, null);
                gate = id;
            }
            foreach (var (branch, block, times) in each.Blocks)
                yield return new TechNode(block, gate == null ? [] : [gate], [], times ?? each.Times, tab.Id, ring, branch);
        }
    }
}
