namespace Stillwake.Game;

/// <summary>
/// How the fog at a planet's sides looks (fog-edges); its colour is the planet type's (<c>PlanetLooks.Look.Fog</c>). On each
/// column it rises from nothing <see cref="Lead"/> columns inside the zone to <see cref="Edge"/> at the zone's side, so the
/// hollow band is never seen clear, and to whole at the seam; the whole screen with it from the zone's side to the seam.
/// </summary>
public static class FogLooks
{
    /// <summary>Columns inside the zone where the fog begins.</summary>
    public const int Lead = 24;

    /// <summary>How thick the fog is at the zone's side, of whole.</summary>
    public const double Edge = 0.85;

    /// <summary>A move across the fog: the screen held whole this long, then eased back over <see cref="FadeMs"/>.</summary>
    public const double FlashMs = 150, FadeMs = 400;
}
