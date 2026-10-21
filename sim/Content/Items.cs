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
/// left where it died, a body for anyone to pick up (0061).
/// </param>
/// <param name="Group">Where a sort puts it (0008): its group first, in <see cref="ItemGroup"/>'s order, then its id.</param>
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
    bool Gear = false,
    ItemGroup Group = ItemGroup.Material);

/// <summary>The groups a sort orders a bag or a crate by (0008), in this order: tools first, the rest last.</summary>
public enum ItemGroup
{
    Tool,
    Wearable,
    /// <summary>Materials, parts and what the farm grows: the default.</summary>
    Material,
    /// <summary>What caught debris becomes, smelted into materials.</summary>
    Salvage,
    /// <summary>Never carried but in creative mode: containers, wreckage, derelicts; and the drone, a bay's.</summary>
    Other,
}

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
        new("scrapChunk", 50, Yields: [("scrap", 4)], SmeltTicks: 60, Group: ItemGroup.Salvage),
        new("hullPlate", 20, Yields: [("iron", 2), ("scrap", 1)], SmeltTicks: 100, Group: ItemGroup.Salvage),
        // Broken out of asteroids (0054) and dug out of a planet's stone (0077); smelted into silicon at a furnace or the
        // electric smelter. Players see "Rock".
        new("rock", 50, Yields: [("silicon", 2)], SmeltTicks: 160, Group: ItemGroup.Salvage),
        // Dug out of a planet's caves (0080): copper ore from its veins, an ingot an ore at the furnace's heat, as slow as rock.
        new("copperOre", 50, Yields: [("copper", 1)], SmeltTicks: 160, Group: ItemGroup.Salvage),
        new("copper", 100),
        // From ice veins and crystals; nothing melts it yet: the jump's and the engine's cost to come.
        new("ice", 50),
        // Broken off the deep's crystals and out of geodes: for sensors and machine modules to come.
        new("crystal", 50),
        // Off the violet glowcaps: for the spore bed to come.
        new("spore", 50),
        // Flakes out of the glinting mica in cave walls: for sensors and the radar to come.
        new("mica", 50),
        // Lodestone, a natural magnet, out of the heat's cracks over the bedrock: a motor's magnet.
        new("magnetite", 50),
        // Containers: caught, they open into their loot (Loot), and are never carried.
        new("supplyPod", 1, Group: ItemGroup.Other),
        new("cargoCrate", 1, Group: ItemGroup.Other),
        // Wreckage: broken by a weapon into its loot (Wreckage), and never carried either.
        new("hullSection", 1, Group: ItemGroup.Other),
        new("cargoContainer", 1, Group: ItemGroup.Other),
        new("machineBlock", 1, Group: ItemGroup.Other),
        new("cableRack", 1, Group: ItemGroup.Other),
        new("tankCluster", 1, Group: ItemGroup.Other),
        new("shieldSlab", 1, Group: ItemGroup.Other),
        // A clod of some ship's farm, drifting with a sprout: seeds and soil when broken.
        new("soilClod", 1, Group: ItemGroup.Other),
        new("asteroid", 1, Group: ItemGroup.Other),
        new("asteroidSmall", 1, Group: ItemGroup.Other),
        new("asteroidLarge", 1, Group: ItemGroup.Other),
        new("asteroidRich", 1, Group: ItemGroup.Other),
        // Derelicts, in the far field's outer tiers alone (0056): big, slow to break, rich in what wrecks give.
        new("engineBlock", 1, Group: ItemGroup.Other),
        new("cargoHold", 1, Group: ItemGroup.Other),
        new("podCluster", 1, Group: ItemGroup.Other),
        // Parts, made at the Fabricator (Crafts) of materials; then assemblies, of parts.
        new("wire", 100),
        new("plate", 100),
        new("gear", 50),
        new("tube", 100),
        new("rotor", 20),
        // A rotor wound with copper round a lodestone, at the Former: what radar and the engine will be built of.
        new("motor", 20),
        new("frame", 20),
        new("coil", 50),
        // Smelted from rock, and smelted on into glass only where it is hot enough: the electric smelter (0057).
        new("silicon", 100, Yields: [("glass", 1)], SmeltTicks: 120, Heat: 2),
        new("glass", 50),
        // Salvaged from cable racks and derelicts (Loot), and made at the circuit assembler of silicon and wire (0057).
        new("circuit", 20),
        // Silicon, glass and wire, made at the circuit assembler: what a solar panel is built of.
        new("solarCell", 20),
        // Made at the circuit assembler: docked in a drone bay it charges and flies items about (0062); a stack of one, a battery of its own.
        new("drone", 1, Capacity: Charge.Drone, Group: ItemGroup.Other),
        // The farm (0060): a seed grows in a grow capsule into biomass, giving back none, one or two seeds, 0.8 on
        // average, so the farm slowly eats seeds and supply pods refill it. Biomass smelts into biofuel.
        new("seed", 10, Yields: [("biomass", 4)], SmeltTicks: 9600, Process: "grow", Returns: [35, 50, 15]),
        // Pressed at the Former of rock and planks: what a grow capsule is built on.
        new("soil", 20),
        new("biomass", 20, Yields: [("biofuel", 1)], SmeltTicks: 160),
        // The engine's fuel, to come; nothing burns it yet.
        new("biofuel", 20),
        // Tools, made as parts are.
        new("fireAxe", 1, Gear: true, Group: ItemGroup.Tool),
        new("harpoon", 1, Gear: true, Group: ItemGroup.Tool),
        // Electric: draws from the belt's batteries while it drills (0053, 0054).
        new("drill", 1, Draw: 3, Gear: true, Group: ItemGroup.Tool),
        // The drill with arcs on its coil (0075): found, never made (Weapons.Found).
        new("arcDrill", 1, Draw: 3, Gear: true, Group: ItemGroup.Tool),
        // Equipment, worn in a slot of its kind (Inventory.Equipment).
        new("jetpack", 1, Equip: "back", Gear: true, Group: ItemGroup.Wearable),
        // A soft warm light round the head (0077): free, drawing no power.
        new("helmetLamp", 1, Equip: "head", Gear: true, Group: ItemGroup.Wearable),
        // Holds charge for any electric tool, worn on the belt; filled at a charging station.
        new("battery", 1, Equip: "belt", Capacity: Charge.Battery, Gear: true, Group: ItemGroup.Wearable),
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

    /// <summary>The first row of <see cref="Slots"/>, chosen with keys 1 to 9: a sort leaves it as it is (0008).</summary>
    public const int Hotbar = 9;

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
    /// ticks a second (0061). Long enough to fly back for it; short enough that bodies never pile up.
    /// </summary>
    public const int BodyLasts = 12_000;
}
