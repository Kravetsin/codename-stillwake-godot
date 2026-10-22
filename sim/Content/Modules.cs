using System.Diagnostics.CodeAnalysis;

namespace Stillwake.Content;

/// <summary>
/// A module (0086): an item fitted into a machine by hand, Valheim's way: one of each kind a machine at most, taken
/// out again by hand, never moved by drones, never loot. What it adds is in thousandths added to 1000, so +1000 is
/// twice: a station's <see cref="Station.Speed"/>, a machine's <c>Draws</c>, a tool's damage and draw. Gains round
/// down, costs up.
/// </summary>
/// <param name="Item">The item it is, by its id.</param>
/// <param name="Tech">
/// Its technology, by id: the client's icon for it, shown on the sockets that take it, on the slots of modules that fit
/// the machine open and on its card, in place of any list of where it goes (0086).
/// </param>
/// <param name="Hosts">
/// What it fits into, by kind: electric stations, by their machine's kind, and electric tools, by their item (0098), for it
/// multiplies what they draw.
/// </param>
/// <param name="Speed">Thousandths added to a station's speed: its work a tick, rounded down.</param>
/// <param name="Draw">Thousandths added to what its machine or tool draws, rounded up.</param>
/// <param name="Damage">Thousandths added to a tool's damage a hit, rounded down (0098).</param>
/// <param name="Heat">Added to a fed station's heat: an unlock module, the crucible, to come (0086).</param>
public sealed record ModuleSpec(string Item, string Tech, IReadOnlyList<string> Hosts, int Speed = 0, int Draw = 0, int Damage = 0, int Heat = 0);

/// <summary>Every module, by its item (0086).</summary>
public static class Modules
{
    /// <summary>
    /// The arc module, of a sky shard (0096): twice the speed for twice the power in an electric station, twice the damage
    /// for twice the draw in the drill (0098). The furnace and the pod take none: one burns planks, the other is paid by hand.
    /// </summary>
    public static readonly ModuleSpec Arc = new("arcModule", "arc", ["smelter", "former", "assembler", "drill"], Speed: 1000, Draw: 1000, Damage: 1000);

    public static readonly IReadOnlyList<ModuleSpec> All = [Arc];

    private static readonly Dictionary<string, ModuleSpec> ByItem = All.ToDictionary(m => m.Item, StringComparer.Ordinal);

    /// <summary>The module <paramref name="item"/> is; throws for an item that is none.</summary>
    public static ModuleSpec Get(string item) => ByItem[item];

    public static bool TryGet(string? item, [MaybeNullWhen(false)] out ModuleSpec module)
    {
        module = null;
        return item != null && ByItem.TryGetValue(item, out module);
    }

    /// <summary>Whether module <paramref name="item"/> fits into a host of <paramref name="kind"/>.</summary>
    public static bool Fits(string? item, string kind) => TryGet(item, out var module) && module.Hosts.Contains(kind);

    /// <summary>The modules a host of <paramref name="kind"/> takes, a socket each, in <see cref="All"/>'s order; none for most.</summary>
    public static IEnumerable<ModuleSpec> For(string kind) => All.Where(m => m.Hosts.Contains(kind));

    /// <summary>Whether some module fits a host of <paramref name="kind"/>: a station, or a tool an arc bench takes (0099).</summary>
    public static bool Hosted(string? kind) => kind != null && All.Any(m => m.Hosts.Contains(kind));
}
