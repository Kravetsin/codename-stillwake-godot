using System.Diagnostics.CodeAnalysis;

namespace Stillwake.Content;

/// <summary>
/// A crafting machine (0057): a queued station makes the crafts of its <paramref name="Rows"/> one unit at a
/// time from a queue paid when queued; a fed station smelts what its input holds into that item's
/// <see cref="ItemSpec.Yields"/>. Its energy is its machine's: none, what it <c>Draws</c>, or the fire of its
/// <c>FuelSlot</c>.
/// </summary>
/// <param name="Machine">The kind of machine it is.</param>
/// <param name="Speed">
/// Thousandths of full speed: work it adds a tick at a full share of its energy, a unit taking its
/// <see cref="Craft.Ticks"/> × 1000, a smelt its <see cref="ItemSpec.SmeltTicks"/> × 1000.
/// </param>
/// <param name="QueueMost">Queued: the most crafts its queue holds, each of any number of units; 0 for a fed station.</param>
/// <param name="Rows">
/// Queued: its crafts as its panel lays them out, a family a row, simple to complex left to right, at most
/// <see cref="Stations.RowMost"/> a row: what it makes and where each sits. Empty for a fed station.
/// </param>
/// <param name="Heat">Fed: how hot it smelts; it smelts an item whose <see cref="ItemSpec.Heat"/> is at most this. 0 for a queued station.</param>
/// <param name="Process">
/// Fed: what it does to what its input holds, "smelt" or "grow"; it works only an item of the same
/// <see cref="ItemSpec.Process"/>, so a furnace never grows a seed and a grow capsule never smelts rock (0060).
/// </param>
public sealed record Station(string Machine, int Speed, int QueueMost, IReadOnlyList<IReadOnlyList<string>> Rows, int Heat = 0, string Process = "smelt")
{
    /// <summary>Fed: its input slot is its queue.</summary>
    public bool Fed => Heat > 0;
}

/// <summary>Every crafting machine, by its machine's kind (the crafting module).</summary>
public static class Stations
{
    public static readonly IReadOnlyList<Station> All =
    [
        // The lifepod's Fabricator: every craft, at a fifth of full speed, the most basic bench (0046).
        new("lifepod", Speed: 200, QueueMost: 4, Rows: [["plate", "gear", "tube", "frame"], ["wire", "coil", "rotor"], ["fireAxe", "harpoon", "drill"], ["jetpack", "battery", "helmetLamp"]]),
        new("furnace", Speed: 1000, QueueMost: 0, Rows: [], Heat: 1),
        // The first powered station: the pod's parts and components, five times as fast, twice the queue.
        new("former", Speed: 1000, QueueMost: 8, Rows: [["plate", "gear", "tube", "frame"], ["wire", "coil", "rotor", "motor"], ["soil"]]),
        // Smelts as the furnace does, twice as fast on power, and hot enough for what the furnace cannot: silicon into glass.
        new("smelter", Speed: 2000, QueueMost: 0, Rows: [], Heat: 2),
        // The electronics bench: circuits and solar cells, drones of them, and the arc module (0086), at full speed on power, a queue of eight.
        new("assembler", Speed: 1000, QueueMost: 8, Rows: [["circuit", "solarCell"], ["drone"], ["arcModule"]]),
        // Grows what is planted in its input, on power (0060): a seed in 9600 ticks, eight minutes, at a full share.
        new("growCapsule", Speed: 1000, QueueMost: 0, Rows: [], Heat: 1, Process: "grow"),
    ];

    /// <summary>The most crafts a row holds: a panel's columns.</summary>
    public const int RowMost = 4;

    private static readonly Dictionary<string, Station> ByMachine = All.ToDictionary(s => s.Machine, StringComparer.Ordinal);

    /// <summary>The station a machine of <paramref name="kind"/> is; throws for a kind that is none.</summary>
    public static Station Get(string kind) => ByMachine[kind];

    public static bool TryGet(string kind, [MaybeNullWhen(false)] out Station station) => ByMachine.TryGetValue(kind, out station);
}
