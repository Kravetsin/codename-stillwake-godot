namespace Stillwake.Content;

/// <summary>
/// Meteors breaking the hull (2026-10-28-hull-meteors, 2026-10-29-meteor-wave): a place's shower, a meteor every
/// <see cref="Shower.EveryMin"/> to <see cref="Shower.EveryMax"/> ticks, seen <see cref="Shower.Warning"/> ticks before it strikes, of
/// a power drawn by weight from <see cref="Shower.Powers"/>. If its power reaches the struck wall's <see cref="Material.Armour"/>, a
/// blast of <see cref="Shower.Charge"/> + power × <see cref="Shower.PerPower"/> spreads from it, spending <see cref="Step"/> a cell
/// (<see cref="Diagonal"/> aslant) and <see cref="PerStrength"/> a point of strength for each wall it breaks: walls soak it up.
/// </summary>
public static class Meteors
{
    /// <summary>
    /// A place's meteors: how often, how long seen coming; a blast's charge at no power (<c>Charge</c>) and how much a point of
    /// power adds (<c>PerPower</c>); how hard, each power by its weight.
    /// </summary>
    public sealed record Shower(int EveryMin, int EveryMax, int Warning, int Charge, int PerPower, IReadOnlyList<(int Power, int Weight)> Powers);

    /// <summary>What a blast spends crossing to a side or up or down a cell, and aslant: about the distance, doubled, in whole units.</summary>
    public const int Step = 2, Diagonal = 3;

    /// <summary>What a blast spends a point of a wall's armour to break it, a back wall's too, which it breaks without stopping.</summary>
    public const int PerStrength = 2;

    /// <summary>
    /// The starting space: one every four to six minutes, three seconds seen, mostly weak but never too weak for scrap (2). A blast
    /// of 22 at power 2 to 52 at 7: a scrap hull torn 7 cells along at the weakest, 17 at the strongest; iron (5) breaks to one in
    /// five and soaks a blast two and a half times as fast as scrap; concrete (9) never.
    /// </summary>
    public static readonly Shower StartingSpace = new(4 * 60 * 20, 6 * 60 * 20, 3 * 20, Charge: 10, PerPower: 6,
        [(2, 34), (3, 26), (4, 20), (5, 10), (6, 6), (7, 4)]);
}
