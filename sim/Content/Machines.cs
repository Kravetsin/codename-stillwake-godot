using System.Diagnostics.CodeAnalysis;

namespace Stillwake.Content;

/// <summary>A machine: what it costs, its footprint, and what it does with power and items.</summary>
/// <param name="Width">Footprint in cells, from the machine's top-left cell.</param>
/// <param name="Ports">Where conduits connect, as offsets inside the footprint; none for a crate.</param>
/// <param name="Cost">Whole units of items it takes from its builder, and gives back when taken apart.</param>
/// <param name="BuildTicks">Ticks of a hero's work to build it.</param>
/// <param name="Makes">Power it makes a tick, cabled to a network; only out in space, every cell of it.</param>
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
/// <param name="Fixed">
/// Built into the ship from its start: never planned, never taken apart, and no blueprint (the lifepod).
/// Meteors hit no machine at all.
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
    bool Fixed = false);

/// <summary>The machines, by the kind saves and the grid name them.</summary>
public static class Machines
{
    public static readonly IReadOnlyList<MachineSpec> All =
    [
        new("tank", 2, 2, [(0, 1)], [("plate", 4), ("gear", 1)], BuildTicks: 160),
        new("generator", 2, 2, [(0, 1)], [("plate", 4), ("rotor", 1), ("wire", 2)], BuildTicks: 200, Draws: 4, Plug: (16, 5)),
        new("solarPanel", 3, 1, [], [("scrap", 6), ("iron", 6)], BuildTicks: 160, Makes: 5, Plug: (24, 14)),
        new("vent", 1, 1, [(0, 0)], [("plate", 1), ("gear", 1)], BuildTicks: 60),
        new("lamp", 1, 1, [], [("wire", 2), ("scrap", 1)], BuildTicks: 60, Draws: 1, Plug: (11, 1)),
        // Analyses what is put in its slot, opening what is made of it (ADRs 0024, 0027).
        new("analyzer", 2, 2, [], [("scrap", 8), ("iron", 4)], BuildTicks: 200, Draws: 2, Plug: (26, 3), Slots: 1),
        new("crate", 2, 1, [], [("planks", 6), ("iron", 2)], BuildTicks: 40, Slots: 27),
        // Its input, its fuel (Furnace.FuelSlot), then three outputs.
        new("furnace", 2, 2, [], [("scrapChunk", 4), ("hullPlate", 2)], BuildTicks: 200, Slots: 5, Inputs: 2),
        // Where heroes are made and where parts are crafted (Crafts.PodSpeed); its slots are the crafts made.
        new("lifepod", 3, 3, [], [], BuildTicks: 0, Slots: 3, Inputs: 0, Fixed: true),
    ];

    private static readonly Dictionary<string, MachineSpec> ByKind = All.ToDictionary(m => m.Kind, StringComparer.Ordinal);

    /// <summary>The machine by its kind; throws for a kind that is none.</summary>
    public static MachineSpec Get(string kind) => ByKind[kind];

    public static bool TryGet(string kind, [MaybeNullWhen(false)] out MachineSpec spec) => ByKind.TryGetValue(kind, out spec);
}
