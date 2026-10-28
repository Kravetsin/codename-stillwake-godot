namespace Stillwake.Content;

/// <summary>
/// Meteors breaking the hull (2026-10-28-hull-meteors, 2026-10-28-meteor-blast): a place's shower, a meteor every
/// <see cref="Shower.EveryMin"/> to <see cref="Shower.EveryMax"/> ticks, seen <see cref="Shower.Warning"/> ticks before it strikes,
/// of a power drawn by weight from <see cref="Shower.Powers"/>. If its power reaches the struck wall's
/// <see cref="Material.Strength"/>, it breaks every wall within (power - strength + 1) × <see cref="Shower.Blast"/> cells, each by
/// its own strength.
/// </summary>
public static class Meteors
{
    /// <summary>A place's meteors: how often, how long seen coming, how wide a power point breaks, and how hard, each power by its weight.</summary>
    public sealed record Shower(int EveryMin, int EveryMax, int Warning, int Blast, IReadOnlyList<(int Power, int Weight)> Powers);

    /// <summary>
    /// The starting space: one every four to six minutes, three seconds seen, mostly weak. A scrap wall (2) breaks to seven in ten,
    /// its hole 3×3 cells at power 2 and some 19 across at 6; an iron one (5) to three in twenty, 3×3 or 7 across; a concrete one (9) never.
    /// </summary>
    public static readonly Shower StartingSpace = new(4 * 60 * 20, 6 * 60 * 20, 3 * 20, Blast: 2,
        [(1, 30), (2, 25), (3, 18), (4, 12), (5, 9), (6, 6)]);
}
