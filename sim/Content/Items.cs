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
/// <param name="Byproducts">
/// What one gives besides its yields only by chance, each entry a loot roll (<see cref="LootEntry"/>, its <c>OneIn</c> the
/// chance): ore's quartz. Drawn after <paramref name="Returns"/> as the work completes; its outputs must take every entry's most.
/// </param>
/// <param name="SmeltTakes">How many of it one smelt works, all taken as it completes: a fed station waits for them in its input.</param>
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
/// <param name="Tank">
/// A helmet: the air it holds, in units, the hero's whole pool while worn (2026-10-27-suits-are-worn); 0 for anything else.
/// </param>
public sealed record ItemSpec(
    string Id,
    int Stack,
    IReadOnlyList<(string Item, int Count)>? Yields = null,
    int? SmeltTicks = null,
    int Heat = 1,
    string Process = "smelt",
    IReadOnlyList<int>? Returns = null,
    IReadOnlyList<LootEntry>? Byproducts = null,
    int SmeltTakes = 1,
    int? BurnTicks = null,
    string? Equip = null,
    int? Capacity = null,
    int? Draw = null,
    bool Gear = false,
    ItemGroup Group = ItemGroup.Material,
    int Tank = 0);

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
        new("scrap", 999),
        // What the nailer fires (Content.Shots), made of scrap at the pod.
        new("nail", 250),
        new("iron", 999),
        new("canister", 20),
        new("planks", 200, BurnTicks: 400),
        new("scrapChunk", 250, Yields: [("scrap", 4)], SmeltTicks: 60, Group: ItemGroup.Salvage),
        new("hullPlate", 250, Yields: [("iron", 2), ("scrap", 1)], SmeltTicks: 100, Group: ItemGroup.Salvage),
        // Dug out of a planet's stone (0077): bulk, crushed into gravel with now and then a quartz, or built back as stone.
        // Players see "Rock".
        new("rock", 999, Yields: [("gravel", 2)], SmeltTicks: 160, Process: "crush", Byproducts: [new("quartz", 1, 1, OneIn: 12)], Group: ItemGroup.Salvage),
        // Broken out of asteroids (0054): crushed into iron ore and gravel, a quartz one time in two, the only silicon space gives.
        new("asteroidOre", 250, Yields: [("ironOre", 1), ("gravel", 2)], SmeltTicks: 120, Process: "crush", Byproducts: [new("quartz", 1, 1, OneIn: 2)], Group: ItemGroup.Salvage),
        // The crusher's: iron ore smelted at the furnace; quartz into silicon where it is hot, at the electric smelter; gravel
        // four to a glass there too, and into soil and stone's own uses.
        new("ironOre", 250, Yields: [("iron", 1)], SmeltTicks: 80),
        new("quartz", 250, Yields: [("silicon", 1)], SmeltTicks: 120, Heat: 2),
        new("gravel", 999, Yields: [("glass", 1)], SmeltTicks: 160, Heat: 2, SmeltTakes: 4),
        // Dug out of a planet's caves (0080): copper ore from its veins, an ingot an ore at the furnace's heat, as slow as rock.
        new("copperOre", 250, Yields: [("copper", 1)], SmeltTicks: 160, Group: ItemGroup.Salvage),
        new("copper", 250),
        // From ice veins and crystals; nothing melts it yet: the jump's and the engine's cost to come.
        new("ice", 250),
        // Broken off the deep's crystals and out of geodes: for sensors and machine modules to come.
        new("crystal", 100),
        // Off the violet glowcaps: the farm's second crop, grown as a seed is into less biomass but giving back 1.1 spores on
        // average, so a farm of them keeps itself where seeds slowly run out (0060). Had only on a planet.
        new("spore", 250, Yields: [("biomass", 3)], SmeltTicks: 9600, Process: "grow", Returns: [20, 50, 30]),
        // Flakes out of the glinting mica in cave walls: for sensors and the radar to come.
        new("mica", 250),
        // Lodestone, a natural magnet, out of the heat's cracks over the bedrock: a motor's magnet.
        new("magnetite", 250),
        // Out of the ruins (0085): scrap of their alloy, a cell of wall dug or a spire cache opened; walls of it built.
        new("alloyScrap", 250),
        // What the arc makers' homes give (0091): the cloth of their rugs and banners, which burns till the ship makes things of
        // it; a whole lamp of theirs, kept till its blueprint comes.
        new("cloth", 200, BurnTicks: 200),
        new("arcLamp", 10),
        // A shard of the sky the arc makers built round (0096): four from a spire's cradle, one from a home's niche; each is an
        // arc module (0086). Kept, as tools are, when a hero dies.
        new("arcShard", 8, Gear: true, Group: ItemGroup.Tool),
        // Made of that relic at the circuit assembler (0086): fitted into an electric station, twice its speed for twice its
        // power (Modules). Gear, kept when a hero dies.
        new("arcModule", 1, Gear: true, Group: ItemGroup.Tool),
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
        new("wire", 250),
        new("plate", 250),
        new("gear", 250),
        new("tube", 250),
        new("rotor", 100),
        // A rotor wound with copper round a lodestone, at the Former: what radar and the engine will be built of.
        new("motor", 50),
        new("frame", 100),
        new("coil", 200),
        // Smelted from quartz at the electric smelter (0057): circuits and solar cells.
        new("silicon", 250),
        new("glass", 250),
        // Salvaged from cable racks and derelicts (Loot), and made at the circuit assembler of silicon and wire (0057).
        new("circuit", 100),
        // Silicon, glass and wire, made at the circuit assembler: what a solar panel is built of.
        new("solarCell", 100),
        // The light-sensitive sheet of a solar panel: no craft makes one and nothing drops one, only a panel taken apart
        // gives it back, so the wreck's own panel can always be built again, and no second one.
        new("photoPanel", 20),
        // Two circuits driving a rotor through a coil, made at the circuit assembler: what slides a launch platform's deck
        // open, the last part built of what space gives (the drop pod's, to come).
        new("deckDrive", 50),
        // The ship computer's and the radar's parts, at the circuit assembler of what space gives: the last made before a planet.
        new("processor", 50),
        new("display", 50),
        new("transceiver", 50),
        // The engine's parts, of what a planet gives (ruins' alloy, the deepest band's magnetite in a motor, the deep's crystal):
        // the last made. A nozzle and a fuel pump at the Former, an igniter at the circuit assembler.
        new("nozzle", 20),
        new("fuelPump", 20),
        new("igniter", 20),
        // Made at the circuit assembler: docked in a drone bay it charges and flies items about (0062); a stack of one, a battery of its own.
        new("drone", 1, Capacity: Charge.Drone, Group: ItemGroup.Other),
        // The farm (0060): a seed grows in a grow capsule into biomass, giving back none, one or two seeds, 0.8 on
        // average, so the farm slowly eats seeds and supply pods refill it. Biomass smelts into biofuel.
        new("seed", 100, Yields: [("biomass", 4)], SmeltTicks: 9600, Process: "grow", Returns: [35, 50, 15]),
        // Pressed at the Former of rock and planks: what a grow capsule is built on.
        new("soil", 999),
        new("biomass", 250, Yields: [("biofuel", 1)], SmeltTicks: 160),
        // The engine's fuel, the one item its tank takes; till the engine runs, the furnace and the burner generator burn it four times
        // as long as a plank.
        new("biofuel", 100, BurnTicks: 1600),
        // What a planet's grazers give besides biomass (Creatures): hide for the cape; fat, a fuel burning twice a plank, or
        // smelted at the furnace into biofuel as biomass is, so hunting fuels the ship. Fed or shift-clicked in it is fuel.
        new("hide", 100),
        new("fat", 100, Yields: [("biofuel", 1)], SmeltTicks: 160, BurnTicks: 800),
        // Tools, made as parts are.
        // Every hero comes aboard with one (Inventory.Kit, 0107): it builds plans and takes marks apart.
        new("screwdriver", 1, Gear: true, Group: ItemGroup.Tool),
        new("fireAxe", 1, Gear: true, Group: ItemGroup.Tool),
        new("harpoon", 1, Gear: true, Group: ItemGroup.Tool),
        // A gun (Content.Shots): one nail a shot.
        new("nailer", 1, Gear: true, Group: ItemGroup.Tool),
        // A gun of a sky shard (2026-10-27-the-railgun): a nail a shot and the belt's charge for it.
        new("railgun", 1, Gear: true, Group: ItemGroup.Tool),
        // Electric: draws from the belt's batteries while it drills (0053, 0054).
        new("drill", 1, Draw: 3, Gear: true, Group: ItemGroup.Tool),
        // Equipment, worn in a slot of its kind (Inventory.Equipment).
        new("jetpack", 1, Equip: "back", Gear: true, Group: ItemGroup.Wearable),
        // A soft warm light round the head (0077): free, drawing no power.
        new("helmetLamp", 1, Equip: Helmet.Mount, Gear: true, Group: ItemGroup.Wearable),
        // Holds charge for any electric tool, worn on the belt; filled at a charging station.
        new("battery", 1, Equip: "belt", Capacity: Charge.Battery, Gear: true, Group: ItemGroup.Wearable),
        // A battery wound round a sky shard at the arc bench (0099): three batteries' charge, worn and filled as one is.
        new("accumulator", 1, Equip: "belt", Capacity: Charge.Accumulator, Gear: true, Group: ItemGroup.Wearable),
        // The basic suit (2026-10-27-suits-are-worn): the helmet's tank the hero's air while worn, the body keeping the cold
        // out; both free at the pod.
        new(Helmet.Basic, 1, Equip: Helmet.Slot, Gear: true, Group: ItemGroup.Wearable, Tank: 45),
        new(Suit.Basic, 1, Equip: Suit.Slot, Gear: true, Group: ItemGroup.Wearable),
        // Softens enemies' blows and the cold a fifth (2026-10-27-the-cape-is-worn).
        new(Cape.Item, 1, Equip: Cape.Slot, Gear: true, Group: ItemGroup.Wearable),
    ];

    private static readonly Dictionary<string, ItemSpec> ById = All.ToDictionary(i => i.Id, StringComparer.Ordinal);

    /// <summary>The item by its id; throws for an id that is none.</summary>
    public static ItemSpec Get(string id) => ById[id];

    public static bool TryGet(string id, [MaybeNullWhen(false)] out ItemSpec spec) => ById.TryGetValue(id, out spec);
}

/// <summary>
/// A hero's inventory, which starts empty: <see cref="Slots"/> slots of its own, the first row the hotbar,
/// then its equipment, a slot for each kind in <see cref="Equipment"/>: the head (a helmet), the back, three on the belt,
/// then the body, the helmet's mount (its lamp) and the cape, last so older slots keep their places. Giving, taking and picking up use only its own slots; a slot of equipment holds only an item
/// whose <c>Equip</c> is its kind.
/// </summary>
public static class Inventory
{
    public const int Slots = 36;

    /// <summary>The first row of <see cref="Slots"/>, chosen with keys 1 to 9: a sort leaves it as it is (0008).</summary>
    public const int Hotbar = 9;

    /// <summary>What every hero comes aboard with, into its first slots (0107): the screwdriver that builds. Cables and pipes are laid from the plan (plan modes).</summary>
    public static readonly IReadOnlyList<(string Item, int Count)> Kit = [(Screwdrivers.Basic.Item, 1)];

    /// <summary>The kinds of the equipment slots, in slot order after <see cref="Slots"/>.</summary>
    public static readonly IReadOnlyList<string> Equipment = [Helmet.Slot, "back", "belt", "belt", "belt", Suit.Slot, Helmet.Mount, Cape.Slot];

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

    /// <summary>How fast a stack is thrown forward, in thousandths of a cell a tick: about six cells.</summary>
    public const int ThrowSpeed = 400;

    /// <summary>How fast a stack is thrown up, in thousandths of a cell a tick.</summary>
    public const int ThrowLift = 300;

    /// <summary>
    /// A creature's loot sprays out of it (2026-10-29-kill-feel): dealt into at most this many pieces, a unit each, the rest
    /// riding on each item's last; every item at least one piece.
    /// </summary>
    public const int SprayMost = 6;

    /// <summary>A sprayed piece's speed across, away from the striker, and up, in thousandths of a cell a tick: a draw between the two.</summary>
    public const int SprayLeast = 60, SprayFastest = 200, SprayLiftLeast = 200, SprayLiftMost = 320;

    /// <summary>
    /// Afloat, a sprayed piece flies this share of its speed in thousandths, up or down by a draw, rounded toward none: nothing
    /// slows it there, and past the grid's edge it is gone.
    /// </summary>
    public const int SpraySpaceShare = 300;

    /// <summary>
    /// How near a hero, in cells box distance from its middle, a stack nobody dropped flies to it and is picked up (the
    /// client's magnet, 2026-10-29-kill-feel); one it overlaps it picks up as ever.
    /// </summary>
    public const int PickUpReach = 2;

    /// <summary>
    /// How many whole cells round a hero's body (its stacks' cell) a hero touching it takes it all, as far as it fits: the body lying
    /// some five cells long holds them inside (the client's, 2026-10-29-body-bag).
    /// </summary>
    public const int BodyReach = 2;

    /// <summary>Inside the ship: what a stack gains falling each tick.</summary>
    public const int Gravity = 60;

    /// <summary>Inside the ship: a stack's fastest fall.</summary>
    public const int MaxFall = 1000;

    /// <summary>Inside the ship: what landing takes off a stack's speed.</summary>
    public const int Friction = 80;

    /// <summary>
    /// Out in space, where nothing falls or slows it, how fast a stack drifts back past the moving ship
    /// as debris does: past the grid's edge it is gone, thrown away.
    /// </summary>
    public const int SpaceDrift = 60;

    /// <summary>
    /// How many steps through what is built a stack inside it is pushed out to the nearest room cell:
    /// past a wall's thickness and out of the middle of the largest machine.
    /// </summary>
    public const int PushReach = 6;

    /// <summary>
    /// How long what a hero left where it died lies before it is gone, wherever it is: ten minutes at 20
    /// ticks a second (0061). Long enough to fly back for it; short enough that bodies never pile up.
    /// </summary>
    public const int BodyLasts = 12_000;

    /// <summary>
    /// How long any other stack lies on the floor after it was dropped before it is gone: ten minutes. The floor is
    /// worked every tick on every peer, so what is thrown away must not pile up.
    /// </summary>
    public const int Lasts = 12_000;
}
