namespace Stillwake.Content;

/// <summary>
/// The field round the build zone (0048): out to <see cref="Margin"/> cells past the zone's edge space cools from
/// <c>Temperature.Hull</c> to <c>Temperature.Space</c>, and past it on to <c>Temperature.Deep</c> at the world's edge (body-temperature).
/// </summary>
public static class Field
{
    /// <summary>How far past the build zone the field reaches, in cells, every way.</summary>
    public const int Margin = 128;
}
