namespace Stillwake.Content;

/// <summary>
/// Meteors breaking the hull (2026-10-28-hull-meteors, unified-damage): a place's shower, a meteor every <see cref="Shower.EveryMin"/>
/// to <see cref="Shower.EveryMax"/> ticks, seen <see cref="Shower.Warning"/> ticks before it strikes, of a power drawn by weight from
/// <see cref="Shower.Powers"/>; the first <see cref="Shower.First"/> ticks into the world when it says. It strikes as a burst of
/// <see cref="Shower.Charge"/> + power × <see cref="Shower.PerPower"/> from the wall it meets, within <see cref="Shower.Reach"/> cells:
/// what it pays for is lost, what it reaches but cannot pay for cracks, so a crack a meteor left makes the next breach.
/// </summary>
public static class Meteors
{
    /// <summary>
    /// A place's meteors: how often, how long seen coming; a burst's damage at no power (<c>Charge</c>) and how much a point of
    /// power adds (<c>PerPower</c>), in tenths of strength; how far it reaches, in cells; how hard, each power by its weight; ticks
    /// till the first is seen coming (<c>First</c>), 0 for an interval like any other.
    /// </summary>
    public sealed record Shower(int EveryMin, int EveryMax, int Warning, int Charge, int PerPower, int Reach,
        IReadOnlyList<(int Power, int Weight)> Powers, int First = 0);

    /// <summary>
    /// The starting space till anyone has landed (2026-10-29-weak-meteors-till-landing): the first a quarter of an hour in, then one
    /// every four to six minutes, as weak as a castaway can patch. A burst of 440 at power 2 to 1100 at 5, on a scrap hull two cells
    /// thick, 541 a cell to break, built: 2 cracks it, 3 and 4 break the outer cell, 5, one in twenty, breaches it, four cells.
    /// </summary>
    public static readonly Shower BeforeLanding = new(4 * 60 * 20, 6 * 60 * 20, 3 * 20, Charge: 0, PerPower: 220, Reach: 4,
        [(2, 40), (3, 35), (4, 20), (5, 5)], First: 15 * 60 * 20);

    /// <summary>
    /// The starting space once a hero has landed: one every four to six minutes, three seconds seen. A burst of 440 at power 2 to 1540
    /// at 7: power 5 and up, one in five, breaches a scrap hull alone, the rest crack it or hole its outer cell, so a second on a
    /// cracked patch breaches; an iron cell (1352) breaks only to the strongest.
    /// </summary>
    public static readonly Shower StartingSpace = new(4 * 60 * 20, 6 * 60 * 20, 3 * 20, Charge: 0, PerPower: 220, Reach: 4,
        [(2, 34), (3, 26), (4, 20), (5, 10), (6, 6), (7, 4)]);
}
