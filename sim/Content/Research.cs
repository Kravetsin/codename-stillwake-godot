namespace Stillwake.Content;

/// <summary>
/// A node of the tech tree: a block of blueprints and crafts bought with credits, or a gate of resources deposited for good,
/// open once all it needs are done (2026-10-30-tech-graph).
/// </summary>
/// <param name="Id">Its name: a block's first thing it opens, a gate's own.</param>
/// <param name="Needs">The nodes that must be done first: their ids.</param>
/// <param name="Opens">What a block opens: blueprints as the build menu names their cards, crafts by the item they make. None for a gate.</param>
/// <param name="Deposit">What a gate asks for. None for a block.</param>
/// <param name="Times">How many times its worth a block costs, where its category's will not do.</param>
public sealed record TechNode(string Id, string[] Needs, string[] Opens, IReadOnlyList<(string Item, int Count)> Deposit, int? Times = null)
{
    /// <summary>Whether it is opened by resources rather than bought.</summary>
    public bool Gate => Deposit.Count > 0;
}

/// <summary>A named band of the tree's map: what belongs together, and how many times its worth a block of it costs.</summary>
public sealed record TechCategory(string Id, int Times, params TechNode[] Nodes);

/// <summary>
/// Knowing blueprints and crafts (2026-10-30-tech-graph). Blueprints are named as the build menu names their cards: a wall
/// or a back wall by its kind and material (<c>wallIron</c>, <c>backWallScrap</c>), anything else by the grid's name for it
/// (<c>floor</c>, <c>furnace</c>); a craft by the item it makes (<c>wire</c>). Either is known from the start or opened by a
/// node of <see cref="Categories"/>. Cables are no blueprint and always known.
/// </summary>
public static class Research
{
    /// <summary>
    /// What the wreck knows from its start: what earns, what a torn room is closed with, the fire axe wreckage is broken with, what
    /// a castaway's pod makes. Both stations are of raw salvage, so taking one apart never leaves a world unable to trade.
    /// </summary>
    public static readonly IReadOnlyList<string> StartKnown =
    [
        "tradeStation", "wallScrap", "backWallScrap", "fireAxe", "screwdriver", "helmet", "suit",
    ];

    /// <summary>How many times its worth a craft costs on top of its category's: a blueprint already costs what it is built of, a craft only a unit.</summary>
    public const int CraftTimes = 3;

    private static TechNode Block(string[] needs, params string[] opens) => new(opens[0], needs, opens, []);
    private static TechNode Dear(int times, string[] needs, params string[] opens) => new(opens[0], needs, opens, [], times);
    private static TechNode Gate(string id, string[] needs, params (string Item, int Count)[] deposit) => new(id, needs, [], deposit);

    /// <summary>The tree, by category. Everything before the pod is had from what space gives; what a planet gives comes after it.</summary>
    public static readonly IReadOnlyList<TechCategory> Categories =
    [
        new("basics", 1,
            Block([], "workbench"),
            Block([], "furnace"),
            Block(["workbench", "furnace"], "wire"),
            Block(["workbench", "furnace"], "plate"),
            Block(["plate"], "gear", "tube"),
            Block(["plate"], "crate"),
            Block(["wire"], "floor", "ladder", "lamp"),
            Block(["floor", "plate"], "door", "hatch"),
            Block(["plate", "floor"], "wallIron", "backWallIron"),
            Block(["wire", "gear"], "nail", "nailer"),
            Gate("press", ["wire", "gear"], ("wire", 20), ("plate", 15), ("gear", 10))),
        new("mechanics", 1,
            Block(["press"], "coil", "rotor"),
            Block(["coil"], "former"),
            Block(["former"], "frame"),
            Block(["coil"], "harpoon"),
            Block(["coil"], "helmetLamp")),
        new("powerAndAir", 1,
            Block(["coil"], "burnerGenerator"),
            Block(["burnerGenerator"], "vent", "tank"),
            Block(["vent", "frame"], "generator"),
            Block(["frame", "coil"], "jetpack"),
            Gate("processing", ["frame", "burnerGenerator"], ("frame", 10), ("gear", 20), ("coil", 20))),
        new("processing", 1,
            Block(["processing"], "crusher"),
            Block(["processing"], "smelter"),
            Block(["smelter"], "wallGlass", "backWallGlass")),
        new("electronics", 1,
            Block(["smelter"], "assembler"),
            Block(["assembler"], "circuit"),
            Block(["circuit", "crusher"], "solarCell", "solarPanel"),
            Gate("automation", ["circuit", "crusher"], ("circuit", 10), ("glass", 20), ("frame", 10))),
        new("automation", 1,
            Block(["automation"], "soil", "wallSoil", "backWallSoil"),
            Block(["soil"], "growCapsule"),
            Block(["automation"], "battery", "charger"),
            Block(["automation"], "capacitorBank"),
            Block(["automation"], "drill"),
            Block(["battery"], "drone", "droneBay"),
            Gate("wayDown", ["battery", "solarCell"], ("circuit", 20), ("glass", 30), ("frame", 20), ("coil", 40))),
        new("wayDown", 1,
            Block(["wayDown"], "deckDrive", "launchPlatform"),
            Block(["wayDown"], "processor", "display"),
            Block(["deckDrive", "processor"], "dropPod"),
            Block(["wayDown"], "transceiver"),
            Block(["processor"], "computer"),
            Block(["transceiver"], "radarAntenna")),
        new("planet", 2,
            Gate("ground", ["dropPod"], ("copper", 20), ("mica", 10), ("hide", 5)),
            Block(["ground"], "wallStone", "backWallStone"),
            Block(["ground"], "wallMica", "backWallMica"),
            Block(["ground"], "wallHide", "backWallHide"),
            Block(["ground"], "canister"),
            Block(["ground"], "cape"),
            Block(["ground"], "arcLamp"),
            Block(["ground"], "wallAlloy", "backWallAlloy")),
        new("arc", 2,
            Gate("arcAndEngine", ["ground"], ("alloyScrap", 20), ("magnetite", 10), ("crystal", 5), ("biofuel", 20)),
            Block(["arcAndEngine"], "motor"),
            Block(["motor"], "arcBench"),
            Block(["arcBench"], "arcModule"),
            Block(["arcBench"], "accumulator"),
            Block(["arcBench"], "railgun")),
        new("engine", 2,
            Block(["arcAndEngine"], "nozzle"),
            Block(["motor"], "fuelPump", "igniter"),
            Dear(8, ["nozzle", "fuelPump"], "engine")),
    ];

    /// <summary>Every node, in the order of its category.</summary>
    public static readonly IReadOnlyList<TechNode> Nodes = [.. Categories.SelectMany(c => c.Nodes)];

    private static readonly Dictionary<string, TechNode> ById = Nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);

    /// <summary>The node named <paramref name="id"/>; null for none.</summary>
    public static TechNode? Node(string id) => ById.GetValueOrDefault(id);

    /// <summary>The category <paramref name="node"/> stands in.</summary>
    public static TechCategory CategoryOf(TechNode node) => Categories.First(c => c.Nodes.Contains(node));
}
