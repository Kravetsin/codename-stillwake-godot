using Stillwake.Sim.Items;
using Stillwake.Sim.Players;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The suit colours (the pick «Восемь экипажа»), one for each of <c>Content.Crew.Colours</c>: far apart, none red, which
/// the screen keeps for danger and refusals, none amber, which would be lost in the drone's ember. Colours are 0xRRGGBB.
/// </summary>
public static class Suits
{
    public static readonly int[] All = [0xd8ec54, 0x5fc8c0, 0x5a8ef0, 0xdc54cc, 0x8cc4f8, 0x8c64e8, 0xe88cc8, 0x8cd06a];

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
