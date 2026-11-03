namespace Stillwake.Content;

/// <summary>
/// The field round the build zone (0048): out to <see cref="Margin"/> cells past the zone's edge a hero
/// breathes as anywhere; past it the cold drains the suit, more the deeper out, up to
/// <see cref="EdgeCost"/> units a breath at the world's edge (0050).
/// </summary>
public static class Field
{
    /// <summary>How far past the build zone the field reaches, in cells, every way.</summary>
    public const int Margin = 128;

    /// <summary>What a hero's air pays a breath at the world's edge: the basic helmet's tank, in one breath.</summary>
    public const int EdgeCost = 45;

    /// <summary>What it pays a breath off the grid: at least any tank (ContentTests), so no helmet takes a hero out of the world (2026-11-03-the-heavy-helmet).</summary>
    public const int OffGrid = 100;
}
