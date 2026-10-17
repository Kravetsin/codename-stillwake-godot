namespace Stillwake.Content;

/// <summary>
/// The field round the build zone (0048): out to <see cref="Margin"/> cells past the zone's edge a hero
/// breathes as anywhere; past it the cold drains the suit, more the deeper out, up to
/// <see cref="EdgeCost"/> units a breath at the world's edge (0050).
/// </summary>
public static class Field
{
    /// <summary>How far past the build zone the field reaches, in cells, every way.</summary>
    public const int Margin = 64;

    /// <summary>What the suit pays a breath at the world's edge, or off the grid: a full suit, in one breath.</summary>
    public const int EdgeCost = 45;
}
