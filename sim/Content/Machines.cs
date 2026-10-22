using System.Diagnostics.CodeAnalysis;

namespace Stillwake.Content;

/// <summary>A machine: what it costs, its footprint, and what it does with power and items.</summary>
/// <param name="Width">Footprint in cells, from the machine's top-left cell.</param>
/// <param name="Ports">Where conduits connect, as offsets inside the footprint; none for a crate.</param>
/// <param name="Cost">Whole units of items it takes from its builder, and gives back when taken apart.</param>
/// <param name="BuildTicks">Ticks of a hero's work to build it.</param>
/// <param name="Makes">
/// Power it makes a tick, cabled to a network: one with a <paramref name="FuelSlot"/> while its fire
/// burns, any other only under the stars, every cell of it in space with no back wall behind it (0043).
/// </param>
/// <param name="Draws">Power it draws a tick from its network, cabled to one.</param>
/// <param name="Plug">
/// Where a cable plugs into it, in pixels of its picture from its top-left, sixteen to a cell: given
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
    StoreSpec? Store = null)
{
    /// <summary>Whether it is a node cables end on: it makes power, draws it or stores it.</summary>
    public bool Powered => Makes != null || Draws != null || Store != null;
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
        new("tank", 2, 2, [(0, 1)], [("plate", 4), ("tube", 1)], BuildTicks: 160),
        new("generator", 2, 2, [(0, 1)], [("frame", 1), ("rotor", 1), ("tube", 1), ("wire", 2)], BuildTicks: 200, Draws: 4, Plug: (16, 5)),
        new("solarPanel", 3, 1, [], [("frame", 1), ("solarCell", 4), ("wire", 2), ("circuit", 1)], BuildTicks: 160, Makes: 5, Plug: (24, 14)),
        new("vent", 1, 1, [(0, 0)], [("plate", 1), ("tube", 1)], BuildTicks: 60),
        new("lamp", 1, 1, [], [("wire", 2), ("scrap", 1)], BuildTicks: 60, Draws: 1, Plug: (11, 1)),
        // A lamp of the arc makers' homes hung aboard (0091): its blueprint opens once an arc lamp is analysed; brighter and
        // colder than the starter lamp, for the same power.
        new("arcLamp", 1, 1, [], [("arcLamp", 1)], BuildTicks: 40, Draws: 1, Plug: (11, 1)),
        // Analyses what is put in its slot, opening what is made of it (ADRs 0024, 0027).
        new("analyzer", 2, 2, [], [("plate", 2), ("wire", 4)], BuildTicks: 200, Draws: 2, Plug: (26, 3), Slots: 1),
        new("crate", 2, 1, [], [("planks", 6), ("iron", 2)], BuildTicks: 40, Slots: 27),
        // Its input, its fuel, then three outputs.
        new("furnace", 2, 2, [], [("scrapChunk", 4), ("hullPlate", 2)], BuildTicks: 200, Slots: 5, Inputs: 2, FuelSlot: 1),
        // The furnace's fire turning a dynamo: power while it burns, from its one slot of fuel.
        new("burnerGenerator", 2, 2, [], [("frame", 1), ("rotor", 1), ("coil", 1), ("scrap", 6)], BuildTicks: 200, Makes: 6, Plug: (23, 25), Slots: 1, FuelSlot: 0),
        // Fills a battery with what it draws: 3 a tick fills 1800 in 600 ticks at full power.
        new("charger", 1, 2, [], [("frame", 1), ("coil", 1), ("wire", 4), ("circuit", 1)], BuildTicks: 120, Draws: 3, Plug: (8, 10), Slots: 1, Charges: true),
        // Keeps what its network makes and nothing draws, and gives it back when the network draws more than it makes (0101):
        // a minute at its most taken in fills it, two of a spare panel's. Basic infrastructure, not arc-gated; a bigger arc store may come later.
        new("capacitorBank", 2, 2, [], [("frame", 1), ("coil", 2), ("plate", 2), ("circuit", 1)], BuildTicks: 200, Plug: (16, 4), Store: new(12000, 10, 10)),
        // Presses and turns parts at full speed while it draws (Stations): two inputs, the most any craft of its needs, then three outputs. Rests idle.
        new("former", 2, 2, [], [("frame", 2), ("rotor", 1), ("gear", 2), ("circuit", 1)], BuildTicks: 200, Draws: 3, Plug: (4, 21), Slots: 6, Inputs: 3),
        // Smelts on power, twice the furnace's speed and hotter (Stations): its input, then three outputs. Rests idle.
        new("smelter", 2, 2, [], [("frame", 1), ("coil", 2), ("plate", 4), ("circuit", 1)], BuildTicks: 200, Draws: 4, Plug: (4, 17), Slots: 4, Inputs: 1),
        // Assembles electronics at full speed while it draws (Stations): four inputs, a drone's, then three outputs. Rests idle.
        new("assembler", 2, 2, [], [("frame", 1), ("rotor", 1), ("glass", 1), ("wire", 2), ("circuit", 1)], BuildTicks: 200, Draws: 2, Plug: (4, 25), Slots: 7, Inputs: 4),
        // Grows what is planted in it on power (Stations), sealed under glass, needing no air: a seed in, biomass and seeds out. Rests idle.
        new("growCapsule", 2, 3, [], [("frame", 1), ("glass", 2), ("soil", 4), ("circuit", 1), ("wire", 2)], BuildTicks: 200, Draws: 2, Plug: (4, 42), Slots: 3, Inputs: 1),
        // Where heroes are made and where parts are crafted (Stations); its slots are the crafts made: paid by hand, no inputs (0057).
        // Launches the drones docked in its four slots to keep the machines in its zone going (0062), and charges them as a charging station does.
        new("droneBay", 2, 2, [], [("frame", 2), ("plate", 4), ("circuit", 2), ("wire", 2)], BuildTicks: 200, Draws: 4, Plug: (29, 20), Slots: 4, Charges: true),
        new("lifepod", 3, 3, [], [], BuildTicks: 0, Slots: 3, Inputs: 0, Fixed: true),
    ];

    private static readonly Dictionary<string, MachineSpec> ByKind = All.ToDictionary(m => m.Kind, StringComparer.Ordinal);

    /// <summary>The machine by its kind; throws for a kind that is none.</summary>
    public static MachineSpec Get(string kind) => ByKind[kind];

    public static bool TryGet(string kind, [MaybeNullWhen(false)] out MachineSpec spec) => ByKind.TryGetValue(kind, out spec);
}
