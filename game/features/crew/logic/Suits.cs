using Stillwake.Sim.Items;
using Stillwake.Sim.Players;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The suit colours (features/crew/suits.ts), one for each of <c>Content.Crew.Colours</c>: far apart, and
/// none red, which the screen keeps for danger and refusals. Colours are 0xRRGGBB.
/// </summary>
public static class Suits
{
    public static readonly int[] All = [0xe8b04a, 0x5fc8c0, 0xa08cf0, 0x8cd06a];

    /// <summary>The suit colour hero <paramref name="id"/> wears, as the sim says; the first while the sim does not know it.</summary>
    public static int Of(IPlayersQueries players, int id) =>
        players.ColourOf(id) is int i && i >= 0 && i < All.Length ? All[i] : All[0];

    /// <summary>The helmet and the suit's body <paramref name="peer"/> wears, their items or null (2026-10-27-suits-are-worn): what its figure is drawn in.</summary>
    public static (string? Helmet, string? Body) WornBy(IItemsQueries items, int peer) =>
        (items.WornIn(peer, Stillwake.Content.Helmet.Slot), items.WornIn(peer, Stillwake.Content.Suit.Slot));

}
