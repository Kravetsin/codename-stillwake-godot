using System.Diagnostics.CodeAnalysis;

namespace Stillwake.Content;

/// <summary>Something a hero carries, by the id saves store: never rename one.</summary>
/// <param name="Stack">The most of it one inventory slot holds.</param>
/// <param name="Yields">
/// Smelted at a fed station into these: salvage, caught as debris, into materials and canisters, never more
/// salvage; silicon on into glass, where it is hotter.
/// </param>
/// <param name="SmeltTicks">Ticks at full speed a fed station takes to smelt one; given with <paramref name="Yields"/>.</param>
/// <param name="Heat">How hot a fed station must be to smelt it (<see cref="Station.Heat"/>): the furnace's 1, or the electric smelter's 2.</param>
/// <param name="Process">
/// What a fed station does to it, matched to the station's <see cref="Station.Process"/>: "smelt" (a furnace, the
/// electric smelter) or "grow" (a grow capsule). At a station of another process it sits in the input, never worked.
/// </param>
/// <param name="Returns">
/// Weights of how many of itself one gives back besides its yields when worked, entry n the weight of n: a crop's
/// seeds. Drawn from the world's Rng as the work completes; its outputs must take the most. None for anything else.
/// </param>
/// <param name="BurnTicks">Ticks a fire, a furnace's or a burner generator's, burns on one: a fuel.</param>
/// <param name="Equip">Equipment: the kind of a hero's equipment slot it is worn in, of <c>Inventory.Equipment</c>; none for anything else.</param>
/// <param name="Capacity">
/// The most charge one holds, stored as <see cref="Charge"/> says: a battery, a general store any electric
/// tool runs on. Only for a stack of one, and never in a cost, so paying with one never destroys its charge.
/// </param>
/// <param name="Draw">
/// An electric tool: the power it draws every tick it runs, hitting or not, as a machine draws (<see cref="Charge"/>), from the
/// belt's batteries. Held, the energy bar shows; its card shows the draw.
/// </param>
/// <param name="Gear">
/// What a hero keeps when it dies: its tools, what it wears and the batteries. Everything else it carries is
/// left where it died, a body for anyone to pick up (0060).
/// </param>
public sealed record ItemSpec(
    string Id,
    int Stack,
    IReadOnlyList<(string Item, int Count)>? Yields = null,
    int? SmeltTicks = null,
    int Heat = 1,
    string Process = "smelt",
    IReadOnlyList<int>? Returns = null,
    int? BurnTicks = null,
    string? Equip = null,
    int? Capacity = null,
    int? Draw = null,
    bool Gear = false);

/// <summary>
/// Everything a hero carries. Every material is an item by the same id; the salvage items are what
/// debris becomes when caught; a container caught opens, a wreck broken too; parts are crafted, but the
/// circuit, salvaged.
/// </summary>
public static class Items
{
    public static readonly IReadOnlyList<ItemSpec> All =
    [
        new("scrap", 500),
        new("iron", 500),
        new("canister", 5),
        new("planks", 50, BurnTicks: 400),
        new("scrapChunk", 10, Yields: [("scrap", 4)], SmeltTicks: 60),
        new("hullPlate", 5, Yields: [("iron", 2), ("scrap", 1)], SmeltTicks: 100),
        // Broken out of asteroids (0054); smelted into silicon at a furnace or the electric smelter.
        new("rock", 10, Yields: [("silicon", 2)], SmeltTicks: 160),
        // Containers: caught, they open into their loot (Loot), and are never carried.
        new("supplyPod", 1),
        new("cargoCrate", 1),
        // Wreckage: broken by a weapon into its loot (Wreckage), and never carried either.
        new("hullSection", 1),
        new("cargoContainer", 1),
        new("machineBlock", 1),
        new("cableRack", 1),
        new("tankCluster", 1),
        new("shieldSlab", 1),
        new("asteroid", 1),
        new("asteroidSmall", 1),
        new("asteroidLarge", 1),
        new("asteroidRich", 1),
        // Derelicts, in the far field's outer tiers alone (0056): big, slow to break, rich in what wrecks give.
        new("engineBlock", 1),
        new("cargoHold", 1),
        new("podCluster", 1),
        // Parts, made at the Fabricator (Crafts) of materials; then assemblies, of parts.
        new("wire", 100),
        new("plate", 100),
        new("gear", 50),
        new("tube", 100),
        new("rotor", 20),
        new("frame", 20),
        new("coil", 50),
        // Smelted from rock, and smelted on into glass only where it is hot enough: the electric smelter (0057).
        new("silicon", 100, Yields: [("glass", 1)], SmeltTicks: 120, Heat: 2),
        new("glass", 50),
        // Salvaged from cable racks and derelicts (Loot), and made at the circuit assembler of silicon and wire (0057).
        new("circuit", 20),
        // Silicon, glass and wire, made at the circuit assembler: what a solar panel is built of.
        new("solarCell", 20),
        // The farm (0060): a seed grows in a grow capsule into biomass, giving back none, one or two seeds, 0.8 on
        // average, so the farm slowly eats seeds and supply pods refill it. Biomass smelts into biofuel.
        new("seed", 10, Yields: [("biomass", 4)], SmeltTicks: 9600, Process: "grow", Returns: [35, 50, 15]),
        // Pressed at the Former of rock and planks: what a grow capsule is built on.
        new("soil", 20),
        new("biomass", 20, Yields: [("biofuel", 1)], SmeltTicks: 160),
        // The engine's fuel, to come; nothing burns it yet.
        new("biofuel", 20),
        // Tools, made as parts are.
        new("fireAxe", 1, Gear: true),
        new("harpoon", 1, Gear: true),
        // Electric: draws from the belt's batteries while it drills (0053, 0054).
        new("drill", 1, Draw: 3, Gear: true),
        // Equipment, worn in a slot of its kind (Inventory.Equipment).
        new("jetpack", 1, Equip: "back", Gear: true),
        // Holds charge for any electric tool, worn on the belt; filled at a charging station.
        new("battery", 1, Equip: "belt", Capacity: Charge.Battery, Gear: true),
    ];

    private static readonly Dictionary<string, ItemSpec> ById = All.ToDictionary(i => i.Id, StringComparer.Ordinal);

    /// <summary>The item by its id; throws for an id that is none.</summary>
    public static ItemSpec Get(string id) => ById[id];

    public static bool TryGet(string id, [MaybeNullWhen(false)] out ItemSpec spec) => ById.TryGetValue(id, out spec);
}

/// <summary>
/// A hero's inventory, which starts empty: <see cref="Slots"/> slots of its own, the first row the hotbar,
/// then its equipment, a slot for each kind in <see cref="Equipment"/>, by the body: the head, the back, three
/// on the belt. Giving, taking and picking up use only its own slots; a slot of equipment holds only an item
/// whose <c>Equip</c> is its kind.
/// </summary>
public static class Inventory
{
    public const int Slots = 36;

    /// <summary>The kinds of the equipment slots, in slot order after <see cref="Slots"/>.</summary>
    public static readonly IReadOnlyList<string> Equipment = ["head", "back", "belt", "belt", "belt"];

    /// <summary>Every slot of an inventory: its own, then its equipment.</summary>
    public static readonly int Total = Slots + Equipment.Count;
}

/// <summary>Stacks on the floor.</summary>
public static class Floor
{
    /// <summary>
    /// Ticks before a peer may pick up a stack it dropped itself: walking on, it would take it straight
    /// back. Anyone else may at once.
    /// </summary>
    public const int OwnPickupDelay = 40;

    /// <summary>How fast a stack is thrown forward, in thousandths of a cell a tick: about three cells.</summary>
    public const int ThrowSpeed = 200;

    /// <summary>How fast a stack is thrown up, in thousandths of a cell a tick.</summary>
    public const int ThrowLift = 150;

    /// <summary>Inside the ship: what a stack gains falling each tick.</summary>
    public const int Gravity = 30;

    /// <summary>Inside the ship: a stack's fastest fall.</summary>
    public const int MaxFall = 500;

    /// <summary>Inside the ship: what landing takes off a stack's speed.</summary>
    public const int Friction = 40;

    /// <summary>
    /// Out in space, where nothing falls or slows it, how fast a stack drifts back past the moving ship
    /// as debris does: past the grid's edge it is gone, thrown away.
    /// </summary>
    public const int SpaceDrift = 30;

    /// <summary>
    /// How many steps through what is built a stack inside it is pushed out to the nearest room cell:
    /// past a wall's thickness and out of the middle of the largest machine.
    /// </summary>
    public const int PushReach = 3;

    /// <summary>
    /// How long what a hero left where it died lies before it is gone, wherever it is: ten minutes at 20
    /// ticks a second (0060). Long enough to fly back for it; short enough that bodies never pile up.
    /// </summary>
    public const int BodyLasts = 12_000;
}
