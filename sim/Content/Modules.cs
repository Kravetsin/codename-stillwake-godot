using System.Diagnostics.CodeAnalysis;

namespace Stillwake.Content;

/// <summary>
/// A module (0086): an item fitted into a machine by hand, Valheim's way: one of each kind a machine at most, taken
/// out again by hand, never moved by drones, never loot. What it adds is in thousandths added to 1000, so +1000 is
/// twice: a station's <see cref="Station.Speed"/>, a machine's <c>Draws</c>, a tool's damage to come. Gains round
/// down, costs up.
/// </summary>
/// <param name="Item">The item it is, by its id.</param>
/// <param name="Hosts">The kinds of machine it fits into: electric stations only, for it multiplies what they draw.</param>
/// <param name="Speed">Thousandths added to a station's speed: its work a tick, rounded down.</param>
/// <param name="Draw">Thousandths added to what its machine draws, rounded up.</param>
/// <param name="Damage">Thousandths added to a tool's damage, rounded down: the arc on the drill, next (0086).</param>
/// <param name="Heat">Added to a fed station's heat: an unlock module, the crucible, to come (0086).</param>
public sealed record ModuleSpec(string Item, IReadOnlyList<string> Hosts, int Speed = 0, int Draw = 0, int Damage = 0, int Heat = 0);

/// <summary>Every module, by its item (0086).</summary>
public static class Modules
{
    /// <summary>
    /// The arc module, of a spire's relic coil (0085): twice the speed for twice the power in an electric station. The
    /// furnace and the pod take none: one burns planks, the other is paid by hand.
    /// </summary>
    public static readonly ModuleSpec Arc = new("arcModule", ["smelter", "former", "assembler"], Speed: 1000, Draw: 1000, Damage: 1000);

    public static readonly IReadOnlyList<ModuleSpec> All = [Arc];

    private static readonly Dictionary<string, ModuleSpec> ByItem = All.ToDictionary(m => m.Item, StringComparer.Ordinal);

    /// <summary>The module <paramref name="item"/> is; throws for an item that is none.</summary>
    public static ModuleSpec Get(string item) => ByItem[item];

    public static bool TryGet(string? item, [MaybeNullWhen(false)] out ModuleSpec module)
    {
        module = null;
        return item != null && ByItem.TryGetValue(item, out module);
    }

    /// <summary>Whether module <paramref name="item"/> fits into a machine of <paramref name="kind"/>.</summary>
    public static bool Fits(string? item, string kind) => TryGet(item, out var module) && module.Hosts.Contains(kind);
}
