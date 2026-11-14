using Stillwake.Sim.Items;

namespace Stillwake.Game.Features.Crew;

/// <summary>What a hero's figure shows fitted into the tool in its hand (0098, tool-in-hand).</summary>
public static class Fittings
{
    /// <summary>
    /// The modules fitted into the stack of <paramref name="wields"/> in peer <paramref name="peer"/>'s hand, as the sim holds
    /// it (<see cref="IItemsQueries.ToolStack"/>, <see cref="ToolStats.ModulesOf"/>): every peer's alike, a friend's too; none
    /// for an empty hand or a plain tool, whatever else is carried.
    /// </summary>
    public static IReadOnlyList<Content.ModuleSpec> Of(IItemsQueries items, int peer, string? wields) =>
        wields != null && items.ToolStack(peer, wields) is { } stack ? ToolStats.ModulesOf(stack) : [];
}
