namespace Stillwake.Content;

/// <summary>
/// Meteors breaking the hull (2026-10-28-hull-meteors): a place's shower, a meteor every <see cref="Shower.EveryMin"/> to
/// <see cref="Shower.EveryMax"/> ticks, seen <see cref="Shower.Warning"/> ticks before it strikes, of a power drawn by weight from
/// <see cref="Shower.Powers"/>. It breaks a wall whose material's <see cref="Material.Strength"/> its power reaches.
/// </summary>
public static class Meteors
{
    /// <summary>A place's meteors: how often, how long seen coming, and how hard, each power by its weight.</summary>
    public sealed record Shower(int EveryMin, int EveryMax, int Warning, IReadOnlyList<(int Power, int Weight)> Powers);

    /// <summary>
    /// The starting space: one every four to six minutes, two seconds seen, mostly weak. A scrap wall (2) breaks to seven
    /// in ten, an iron one (5) to three in twenty, a concrete one (9) never.
    /// </summary>
    public static readonly Shower StartingSpace = new(4 * 60 * 20, 6 * 60 * 20, 2 * 20,
        [(1, 30), (2, 25), (3, 18), (4, 12), (5, 9), (6, 6)]);
}
