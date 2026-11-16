using Stillwake.Sim.Items;
using Stillwake.Sim.Players;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The suit colours (features/crew/suits.ts), one for each of <c>Content.Crew.Colours</c>: far apart, and
/// none red, which the screen keeps for danger and refusals. Colours are 0xRRGGBB.
/// </summary>
public static class Suits
{
    public static readonly int[] All = [0xe8b04a, 0x5fc8c0, 0xa08cf0, 0x8cd06a, 0x5a8ef0, 0xe88cc8];

    /// <summary>The suit colour hero <paramref name="id"/> wears, as the sim says; the first while the sim does not know it.</summary>
    public static int Of(IPlayersQueries players, int id) =>
        players.ColourOf(id) is int i && i >= 0 && i < All.Length ? All[i] : All[0];

    /// <summary>
    /// What <paramref name="peer"/>'s figure is drawn in: its helmet, suit's body and cape, items or none (2026-10-27-suits-are-worn),
    /// in the skins it chose (hero-skins). The one lookup every figure of a peer's hero goes through.
    /// </summary>
    public static Dressed DressedAs(IItemsQueries items, IPlayersQueries players, int peer) => Skins.Dress(
        items.WornIn(peer, Stillwake.Content.Helmet.Slot), items.WornIn(peer, Stillwake.Content.Suit.Slot),
        items.WornIn(peer, Stillwake.Content.Cape.Slot), players.SkinsOf(peer));

}
