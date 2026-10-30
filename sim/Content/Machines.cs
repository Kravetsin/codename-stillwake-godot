using System.Diagnostics.CodeAnalysis;

namespace Stillwake.Content;

/// <summary>A machine: what it costs, its footprint, and what it does with power and items.</summary>
/// <param name="Width">Footprint in cells, from the machine's top-left cell: twice what a map draws it, a character a 16 px cell (0104).</param>
/// <param name="Ports">Where pipes connect, as offsets inside the footprint; none for a crate.</param>
/// <param name="Cost">Whole units of items it takes from its builder, and gives back when taken apart.</param>
/// <param name="BuildTicks">Ticks of a hero's work to build it.</param>
/// <param name="Makes">
/// Power it makes a tick, cabled to a network: one with a <paramref name="FuelSlot"/> while its fire
/// burns, any other only under the stars, every cell of it in space with no back wall behind it (0043).
/// </param>
/// <param name="Draws">Power it draws a tick from its network, cabled to one.</param>
/// <param name="Plug">
/// Where a cable plugs into it, in pixels from its footprint's top-left, eight to a cell (0104): given
/// with <paramref name="Makes"/> or <paramref name="Draws"/>, one socket a machine. Only drawn: the sim
/// knows the machine as one node.
/// </param>
/// <param name="Slots">Item slots it holds, opened with E as a crate is.</param>
/// <param name="Inputs">
/// How many of its first slots items are put into; the rest are only taken from. All of them when not
/// given; none for a machine that only gives out (the lifepod). A move into it naming no slot goes to the first.
/// </param>
/// <param name="FuelSlot">The slot a fire burns fuel from (0023): a furnace's, a burner generator's.</param>
/// <param name="Fixed">
/// Built into the ship from its start: never planned, never taken apart, and no blueprint (the lifepod).
/// Meteors hit no machine at all.
/// </param>
/// <param name="Charges">
/// A charging station: what it draws, scaled by its satisfaction, goes into what its slots hold short of full, shared
/// between them (<see cref="Charge"/>): the charger's one, a drone bay's four. Its slots take only items with a
/// <c>Capacity</c>, and it draws only while one is short of full.
/// </param>
/// <param name="Store">
/// A store of its network's energy (0101): it takes in what the network makes beyond what it draws and gives it back
/// when the network draws more than it makes, within its <see cref="StoreSpec"/>. It neither makes nor draws.
/// </param>
/// <param name="Tunes">
/// A bench a tool is fitted at (0099): its first slot holds one tool a module fits (<c>Modules</c>' hosts) and nothing else,
/// and a module fitted with it as the target goes onto that tool's stack. Its other slots are what it crafts; its crafts are
/// paid by hand, and drones never touch it.
/// </param>
/// <param name="Takes">The one item its slots hold, put in by hand or by drones: the engine's biofuel. Any when not given.</param>
/// <param name="Exhaust">
/// Cells its nozzle reaches out of its left side, the stern's (2026-10-25-the-engine-through-the-hull): in every row of it, a
/// wall or more from its side, then space, the nozzle's tip in space. Its picture is as much wider, the nozzle drawn over the
/// wall. None for a machine standing wholly in its footprint.
/// </param>
/// <param name="Has">
/// What it is to the systems that ask (0113), a flag a trait; never <see cref="MachineTrait.Fire"/>, which its
/// <paramref name="FuelSlot"/> implies.
/// </param>
/// <param name="Menu">Its card in the build menu (0114); none for one never built (the lifepod).</param>
/// <param name="Char">Its character in an ASCII map (<see cref="Legend"/>): drawn over its whole footprint, a character a 16 px cell; null for none.</param>
/// <param name="Means">What the map character means, for the legend; given with <paramref name="Char"/>.</param>
public sealed record MachineSpec(
    string Kind,
    int Width,
    int Height,
    IReadOnlyList<(int X, int Y)> Ports,
    IReadOnlyList<(string Item, int Count)> Cost,
    int BuildTicks,
    int? Makes = null,
    int? Draws = null,
    (int X, int Y)? Plug = null,
    int? Slots = null,
    int? Inputs = null,
    int? FuelSlot = null,
    bool Fixed = false,
    bool Charges = false,
    StoreSpec? Store = null,
    bool Tunes = false,
    string? Takes = null,
    int? Exhaust = null,
    MachineTrait Has = MachineTrait.None,
    MenuSlot? Menu = null,
    char? Char = null,
    string? Means = null)
{
    /// <summary>Whether it is a node cables end on: it makes power, draws it or stores it.</summary>
    public bool Powered => Makes != null || Draws != null || Store != null;

    /// <summary>Its traits: those it <see cref="Has"/>, and <see cref="MachineTrait.Fire"/> with a <see cref="FuelSlot"/>.</summary>
    public MachineTrait Traits => Has | (FuelSlot != null ? MachineTrait.Fire : MachineTrait.None);

    /// <summary>Whether it has any of <paramref name="trait"/>.</summary>
    public bool Is(MachineTrait trait) => (Traits & trait) != 0;
}

/// <summary>
/// What a machine is to the systems that ask (0113): they read these, never its kind. A new machine whose behaviours
/// exist is a row of <see cref="Machines.All"/> with its <see cref="MachineSpec.Menu"/> card, its art and its name.
/// </summary>
[Flags]
public enum MachineTrait
{
    None = 0,
    /// <summary>Takes any item into any slot and serves it back: a crate. Drones store in it and take from it.</summary>
    Container = 1,
    /// <summary>Stores its network's oxygen, filled by a canister loaded into it: a tank.</summary>
    Gas = 2,
    /// <summary>
    /// Feeds the pipes it stands on: a vent piped to it is fed, and blows, airs its room and hums. One that stores no
    /// <see cref="Gas"/> makes its air on power: the generator.
    /// </summary>
    AirSource = 4,
    /// <summary>Lets its network's air into the room it stands in.</summary>
    Vents = 8,
    /// <summary>Keeps the machines in its zone going with its drones: the drone bay (0062).</summary>
    Zone = 16,
    /// <summary>Gives light as it gets power, its colour and reach the client's lamp looks: lamps.</summary>
    Glow = 32,
    /// <summary>A fire burns in it, fuel from its <see cref="MachineSpec.FuelSlot"/> (0023): implied by that slot, never declared.</summary>
    Fire = 64,
    /// <summary>Opens the ship's programs on E, each brought by a module on its network (<see cref="Programs"/>): the ship's computer.</summary>
    Computer = 128,
    /// <summary>A hero boards it with E and sits in it, one hero at a time (Landing): the drop pod.</summary>
    Seats = 256,
    /// <summary>Stands only on a launch platform, the row under it wholly a run of one (<c>grid.noPlatform</c>): the drop pod, its landers to come.</summary>
    Lands = 512,
    /// <summary>Sells what its slots hold for the ship's credits (Trade): the trade station.</summary>
    Sells = 1024,
    /// <summary>Opens the tech tree on E (Research): the tech station.</summary>
    Tech = 2048,
}

/// <summary>
/// What a network's store holds and moves, in power-ticks (0101): <paramref name="Capacity"/> full, at most
/// <paramref name="In"/> taken in a tick and <paramref name="Out"/> given out.
/// </summary>
public sealed record StoreSpec(int Capacity, int In, int Out);

/// <summary>The machines, by the kind saves and the grid name them.</summary>
public static class Machines
{
    public static readonly IReadOnlyList<MachineSpec> All =
    [
        new("tank", 4, 4, [(0, 2)], [("plate", 8), ("tube", 2), ("gear", 2)], BuildTicks: 160, Has: MachineTrait.Gas | MachineTrait.AirSource, Menu: new(MenuTabs.Air, 20), Char: 'T', Means: "oxygen tank: draw its whole footprint"),
        new("generator", 4, 4, [(0, 2)], [("frame", 2), ("rotor", 2), ("tube", 2), ("wire", 4)], BuildTicks: 200, Draws: 4, Plug: (16, 5), Has: MachineTrait.AirSource, Menu: new(MenuTabs.Air, 30), Char: 'G', Means: "oxygen generator: draw its whole footprint"),
        new("solarPanel", 6, 2, [], [("frame", 2), ("solarCell", 6), ("wire", 4), ("circuit", 1)], BuildTicks: 160, Makes: 5, Plug: (24, 14), Menu: new(MenuTabs.Power, 10), Char: 'P', Means: "solar panel: draw its whole footprint"),
        new("vent", 2, 2, [(0, 0)], [("plate", 2), ("tube", 2)], BuildTicks: 60, Has: MachineTrait.Vents, Menu: new(MenuTabs.Air, 10), Char: 'V', Means: "vent"),
        new("lamp", 2, 2, [], [("wire", 2), ("scrap", 2)], BuildTicks: 60, Draws: 1, Plug: (11, 1), Has: MachineTrait.Glow, Menu: new(MenuTabs.Power, 30), Char: 'L', Means: "lamp"),
        // A lamp of the arc makers' homes hung aboard (0091): its blueprint opens once an arc lamp is analysed; brighter and
        // colder than the starter lamp, for the same power.
        new("arcLamp", 2, 2, [], [("arcLamp", 1)], BuildTicks: 40, Draws: 1, Plug: (11, 1), Has: MachineTrait.Glow, Menu: new(MenuTabs.Power, 40), Char: 'J', Means: "arc lamp: one of the arc makers' hung aboard (0091)"),
        // Analyses what is put in its slot, opening what is made of it (ADRs 0024, 0027).
        // Where the tree is walked: resources open its tiers, credits buy its blueprints (2026-10-30-trade-and-tech). No power, no slots.
        new("techStation", 4, 4, [], [("scrapChunk", 4), ("hullPlate", 2)], BuildTicks: 200, Has: MachineTrait.Tech, Menu: new(MenuTabs.Production, 20), Char: 'A', Means: "tech station: draw its whole footprint"),
        // Sells what is put in for the ship's credits, a unit at a time; of raw salvage, so the first credits need nothing made.
        new("tradeStation", 4, 4, [], [("scrapChunk", 4), ("hullPlate", 2)], BuildTicks: 120, Slots: 4, Has: MachineTrait.Sells, Menu: new(MenuTabs.Production, 17), Char: '$', Means: "trade station: draw its whole footprint"),
        new("crate", 4, 2, [], [("planks", 8), ("plate", 2)], BuildTicks: 40, Slots: 27, Has: MachineTrait.Container, Menu: new(MenuTabs.Storage, 10), Char: 'K', Means: "crate: draw its whole footprint"),
        // The first bench (Stations), of raw salvage as the furnace is: no power, paid by hand, its three slots what it made.
        new("workbench", 4, 2, [], [("scrapChunk", 4), ("hullPlate", 2), ("planks", 4)], BuildTicks: 120, Slots: 3, Inputs: 0, Menu: new(MenuTabs.Production, 15), Char: 'I', Means: "workbench: draw its whole footprint"),
        // Its input, its fuel, then three outputs.
        new("furnace", 4, 4, [], [("scrapChunk", 4), ("hullPlate", 2)], BuildTicks: 200, Slots: 5, Inputs: 2, FuelSlot: 1, Menu: new(MenuTabs.Production, 10), Char: 'F', Means: "furnace: draw its whole footprint"),
        // The furnace's fire turning a dynamo: power while it burns, from its one slot of fuel. Of the workbench's parts, so
        // power comes before the Former.
        new("burnerGenerator", 4, 4, [], [("plate", 4), ("rotor", 2), ("coil", 2), ("scrap", 10)], BuildTicks: 200, Makes: 6, Plug: (23, 25), Slots: 1, FuelSlot: 0, Menu: new(MenuTabs.Power, 20), Char: 'B', Means: "burner generator: draw its whole footprint"),
        // Fills a battery with what it draws: 3 a tick fills 1800 in 600 ticks at full power.
        new("charger", 2, 4, [], [("frame", 1), ("coil", 2), ("wire", 6), ("circuit", 2)], BuildTicks: 120, Draws: 3, Plug: (8, 10), Slots: 1, Charges: true, Menu: new(MenuTabs.Power, 50), Char: 'C', Means: "charging station: draw its whole footprint"),
        // Keeps what its network makes and nothing draws, and gives it back when the network draws more than it makes (0101):
        // a minute at its most taken in fills it, two of a spare panel's. Basic infrastructure, not arc-gated; a bigger arc store may come later.
        new("capacitorBank", 4, 4, [], [("frame", 2), ("coil", 6), ("plate", 6), ("circuit", 2)], BuildTicks: 200, Plug: (16, 4), Store: new(12000, 10, 10), Menu: new(MenuTabs.Power, 60), Char: 'Q', Means: "capacitor bank: draw its whole footprint (0101)"),
        // Presses and turns parts at full speed while it draws (Stations): three inputs, a motor's, then three outputs. Rests idle.
        // Of the workbench's parts and no circuit, never a frame, which only it makes: a machine with no electronics in it.
        new("former", 4, 4, [], [("plate", 6), ("rotor", 2), ("gear", 6), ("tube", 2), ("coil", 2)], BuildTicks: 200, Draws: 3, Plug: (4, 21), Slots: 6, Inputs: 3, Menu: new(MenuTabs.Production, 30), Char: 'M', Means: "former: draw its whole footprint"),
        // Crushes rock and asteroid ore on power (Stations): its input, then three outputs, room for gravel, iron ore and quartz.
        // Rests idle. Mechanical, no circuit in it.
        new("crusher", 4, 4, [], [("frame", 2), ("gear", 8), ("plate", 6), ("rotor", 2), ("tube", 2)], BuildTicks: 200, Draws: 4, Plug: (27, 6), Slots: 4, Inputs: 1, Menu: new(MenuTabs.Production, 35), Char: 'k', Means: "crusher: draw its whole footprint"),
        // Smelts on power, twice the furnace's speed and hotter (Stations): its input, then three outputs. Rests idle.
        new("smelter", 4, 4, [], [("frame", 3), ("coil", 4), ("plate", 8), ("tube", 4), ("circuit", 1)], BuildTicks: 200, Draws: 4, Plug: (4, 17), Slots: 4, Inputs: 1, Menu: new(MenuTabs.Production, 40), Char: 'E', Means: "electric smelter: draw its whole footprint"),
        // Assembles electronics at full speed while it draws (Stations): four inputs, a drone's, then three outputs. Rests idle.
        new("assembler", 4, 4, [], [("frame", 3), ("rotor", 2), ("glass", 4), ("coil", 2), ("wire", 10), ("circuit", 1)], BuildTicks: 200, Draws: 2, Plug: (4, 25), Slots: 7, Inputs: 4, Menu: new(MenuTabs.Production, 50), Char: 'Y', Means: "circuit assembler: draw its whole footprint"),
        // Grows what is planted in it on power (Stations), sealed under glass, needing no air: a seed in, biomass and seeds out. Rests idle.
        new("growCapsule", 4, 6, [], [("frame", 2), ("glass", 6), ("soil", 8), ("circuit", 2), ("wire", 4)], BuildTicks: 200, Draws: 2, Plug: (4, 42), Slots: 3, Inputs: 1, Menu: new(MenuTabs.Production, 60), Char: 'g', Means: "grow capsule: draw its whole footprint"),
        // Where heroes are made and where parts are crafted (Stations); its slots are the crafts made: paid by hand, no inputs (0057).
        // Launches the drones docked in its four slots to keep the machines in its zone going (0062), and charges them as a charging station does.
        new("droneBay", 4, 4, [], [("frame", 4), ("plate", 8), ("rotor", 4), ("circuit", 4), ("wire", 10)], BuildTicks: 200, Draws: 4, Plug: (29, 20), Slots: 4, Charges: true, Has: MachineTrait.Zone, Menu: new(MenuTabs.Storage, 20), Char: 'R', Means: "drone bay: draw its whole footprint"),
        new("lifepod", 6, 6, [], [], BuildTicks: 0, Slots: 3, Inputs: 0, Fixed: true, Char: 'O', Means: "lifepod: draw its whole footprint; heroes are made at its hatch"),
        // The arc bench (0099): a ship's bench with a vise, a sky shard set in its jaw. Its tool slot, then two outputs of its arc
        // crafts; unpowered, its shard is its power. A shard in its cost, so analysing one opens it with the arc module (0027).
        new("arcBench", 4, 2, [], [("arcShard", 1), ("frame", 2), ("plate", 4), ("motor", 1)], BuildTicks: 160, Slots: 3, Inputs: 1, Tunes: true, Menu: new(MenuTabs.Production, 70), Char: 'W', Means: "arc bench: draw its whole footprint (0099)"),
        // The ship's computer and the radar antenna on its roof (the radar its program): of what space gives, the last built before a planet.
        new("computer", 6, 4, [], [("frame", 4), ("plate", 12), ("processor", 3), ("display", 2), ("coil", 6), ("wire", 20)], BuildTicks: 240, Draws: 2, Plug: (43, 11), Has: MachineTrait.Computer, Menu: new(MenuTabs.Ship, 10), Char: 'U', Means: "ship's computer: draw its whole footprint"),
        new("radarAntenna", 4, 4, [], [("frame", 3), ("plate", 8), ("tube", 4), ("rotor", 2), ("gear", 6), ("transceiver", 2), ("solarCell", 2)], BuildTicks: 200, Draws: 1, Plug: (13, 26), Menu: new(MenuTabs.Ship, 20), Char: 'N', Means: "radar antenna: draw its whole footprint"),
        // The engine (2026-10-25-the-engine-through-the-hull): the last thing built, of a planet's parts; its nozzle out through
        // the stern's wall, its tank of biofuel, its pumps and igniter kept ready on power. It does not run yet: the jump is to come.
        new("engine", 6, 4, [], [("nozzle", 1), ("fuelPump", 1), ("igniter", 1), ("frame", 4), ("plate", 12), ("motor", 1)], BuildTicks: 240, Draws: 2, Plug: (39, 12), Slots: 1, Takes: "biofuel", Exhaust: 3, Menu: new(MenuTabs.Ship, 110), Char: 'X', Means: "engine: draw its whole footprint, a hull wall on its left with space past it"),
        // The drop pod, the bell (the capsule concepts' 2C): a dome with a round porthole on a wide skirt, standing on a launch platform
        // two pieces wide. A hero sits in it; its one slot holds the biofuel it will burn going down. Guided by a processor and a display.
        new("dropPod", 4, 6, [], [("frame", 6), ("plate", 16), ("glass", 8), ("tube", 6), ("processor", 1), ("display", 1)], BuildTicks: 240, Slots: 1, Takes: "biofuel", Has: MachineTrait.Seats | MachineTrait.Lands, Menu: new(MenuTabs.Ship, 105), Char: 'Z', Means: "drop pod: draw its whole footprint, on a launch platform"),
    ];

    private static readonly Dictionary<string, MachineSpec> ByKind = All.ToDictionary(m => m.Kind, StringComparer.Ordinal);

    /// <summary>The machine by its kind; throws for a kind that is none.</summary>
    public static MachineSpec Get(string kind) => ByKind[kind];

    public static bool TryGet(string kind, [MaybeNullWhen(false)] out MachineSpec spec) => ByKind.TryGetValue(kind, out spec);

    /// <summary>Whether a machine of <paramref name="kind"/> has any of <paramref name="trait"/>; false for a kind that is none.</summary>
    public static bool Is(string kind, MachineTrait trait) => ByKind.TryGetValue(kind, out var spec) && spec.Is(trait);
}
