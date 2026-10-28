namespace Stillwake.Content;

/// <summary>
/// Meteors breaking the hull (2026-10-28-hull-meteors, 2026-10-28-meteor-blast): a place's shower, a meteor every
/// <see cref="Shower.EveryMin"/> to <see cref="Shower.EveryMax"/> ticks, seen <see cref="Shower.Warning"/> ticks before it strikes,
/// of a power drawn by weight from <see cref="Shower.Powers"/>. If its power reaches the struck wall's
/// <see cref="Material.Strength"/>, it breaks every wall nearer than <see cref="Shower.Hole"/> + (power - strength) ×
/// <see cref="Shower.Blast"/> cells, each by its own strength; a wall stronger than its power stops it whole.
/// </summary>
public static class Meteors
{
    /// <summary>
    /// A place's meteors: how often, how long seen coming; how far, in cells, one breaks a wall its power just reaches (<c>Hole</c>)
    /// and how much farther for each point over (<c>Blast</c>); how hard, each power by its weight.
    /// </summary>
    public sealed record Shower(int EveryMin, int EveryMax, int Warning, int Hole, int Blast, IReadOnlyList<(int Power, int Weight)> Powers);

    /// <summary>
    /// The starting space: one every four to six minutes, three seconds seen, mostly weak but never too weak for scrap (2): every
    /// one tears a hole in it, 5 cells across at power 2 to 15 at 7. Iron (5) breaks to one in five, 5 to 9 across; concrete (9) never.
    /// </summary>
    public static readonly Shower StartingSpace = new(4 * 60 * 20, 6 * 60 * 20, 3 * 20, Hole: 3, Blast: 1,
        [(2, 34), (3, 26), (4, 20), (5, 10), (6, 6), (7, 4)]);
}
