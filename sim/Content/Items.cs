using System.Diagnostics.CodeAnalysis;

namespace Stillwake.Content;

/// <summary>Something a hero carries, by the id saves store: never rename one.</summary>
/// <param name="Stack">The most of it one inventory slot holds.</param>
/// <param name="Yields">
/// A salvage item, caught as debris, is smelted in a furnace into these: materials and canisters, never
/// more salvage.
/// </param>
/// <param name="SmeltTicks">Ticks of a furnace's fire it takes to smelt one; given with <paramref name="Yields"/>.</param>
/// <param name="BurnTicks">Ticks a furnace's fire burns on one: a fuel.</param>
public sealed record ItemSpec(
    string Id,
    int Stack,
    IReadOnlyList<(string Item, int Count)>? Yields = null,
    int? SmeltTicks = null,
    int? BurnTicks = null);

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
        new("planks", 50, BurnTicks: 300),
        new("scrapChunk", 10, Yields: [("scrap", 12)], SmeltTicks: 100),
        new("hullPlate", 5, Yields: [("iron", 6), ("scrap", 4)], SmeltTicks: 200),
        // Containers: caught, they open into their loot (Loot), and are never carried.
        new("supplyPod", 1),
        new("cargoCrate", 1),
        // Wreckage: broken with an axe into its loot (Wreckage), and never carried either.
        new("hullSection", 1),
        new("cargoContainer", 1),
        new("machineBlock", 1),
        new("cableRack", 1),
        new("tankCluster", 1),
        new("shieldSlab", 1),
        // Parts, made by hand (Crafts) of materials and other parts.
        new("wire", 100),
        new("plate", 100),
        new("gear", 50),
        new("rotor", 20),
        // A part no craft makes: salvaged from cable racks alone (Loot).
        new("circuit", 20),
        // Tools, made as parts are.
        new("fireAxe", 1),
    ];

    private static readonly Dictionary<string, ItemSpec> ById = All.ToDictionary(i => i.Id, StringComparer.Ordinal);

    /// <summary>The item by its id; throws for an id that is none.</summary>
    public static ItemSpec Get(string id) => ById[id];

    public static bool TryGet(string id, [MaybeNullWhen(false)] out ItemSpec spec) => ById.TryGetValue(id, out spec);
}

/// <summary>A hero's inventory, which starts empty; its first row is the hotbar.</summary>
public static class Inventory
{
    public const int Slots = 36;
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
}
