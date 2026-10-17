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
/// Charge it puts a tick, at full power, into what its first slot holds (<see cref="Charge"/>): a charging
/// station. Its slots take only items with a <c>Capacity</c>, and it draws only while one is short of full.
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
    int? Charges = null);

/// <summary>The machines, by the kind saves and the grid name them.</summary>
public static class Machines
{
    public static readonly IReadOnlyList<MachineSpec> All =
    [
        new("tank", 2, 2, [(0, 1)], [("plate", 4), ("tube", 1)], BuildTicks: 160),
        new("generator", 2, 2, [(0, 1)], [("frame", 1), ("rotor", 1), ("tube", 1), ("wire", 2)], BuildTicks: 200, Draws: 4, Plug: (16, 5)),
        new("solarPanel", 3, 1, [], [("scrap", 6), ("iron", 6), ("circuit", 3)], BuildTicks: 160, Makes: 5, Plug: (24, 14)),
        new("vent", 1, 1, [(0, 0)], [("plate", 1), ("tube", 1)], BuildTicks: 60),
        new("lamp", 1, 1, [], [("wire", 2), ("scrap", 1)], BuildTicks: 60, Draws: 1, Plug: (11, 1)),
        // Analyses what is put in its slot, opening what is made of it (ADRs 0024, 0027).
        new("analyzer", 2, 2, [], [("plate", 2), ("wire", 4)], BuildTicks: 200, Draws: 2, Plug: (26, 3), Slots: 1),
        new("crate", 2, 1, [], [("planks", 6), ("iron", 2)], BuildTicks: 40, Slots: 27),
        // Its input, its fuel, then three outputs.
        new("furnace", 2, 2, [], [("scrapChunk", 4), ("hullPlate", 2)], BuildTicks: 200, Slots: 5, Inputs: 2, FuelSlot: 1),
        // The furnace's fire turning a dynamo: power while it burns, from its one slot of fuel.
        new("burnerGenerator", 2, 2, [], [("frame", 1), ("rotor", 1), ("coil", 1), ("scrap", 6)], BuildTicks: 200, Makes: 6, Plug: (23, 25), Slots: 1, FuelSlot: 0),
        // Fills a battery on its network: from empty to full in Charge.Battery / Charge.PerTick ticks at full power.
        new("charger", 1, 2, [], [("frame", 1), ("coil", 1), ("wire", 4)], BuildTicks: 120, Draws: 3, Plug: (8, 10), Slots: 1, Charges: Charge.PerTick),
        // Where heroes are made and where parts are crafted (Crafts.PodSpeed); its slots are the crafts made.
        new("lifepod", 3, 3, [], [], BuildTicks: 0, Slots: 3, Inputs: 0, Fixed: true),
    ];

    private static readonly Dictionary<string, MachineSpec> ByKind = All.ToDictionary(m => m.Kind, StringComparer.Ordinal);

    /// <summary>The machine by its kind; throws for a kind that is none.</summary>
    public static MachineSpec Get(string kind) => ByKind[kind];

    public static bool TryGet(string kind, [MaybeNullWhen(false)] out MachineSpec spec) => ByKind.TryGetValue(kind, out spec);
}
