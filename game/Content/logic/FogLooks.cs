namespace Stillwake.Game;

/// <summary>
/// How the fog at a planet's sides looks (fog-edges); its colour is the planet type's (<c>PlanetLooks.Look.Fog</c>). Whole over
/// each band, so its hollow is never seen; thinning over the <c>Ground.FogOut</c> columns past it to <see cref="Edge"/>, where a
/// hero moved across falls out of it; gone <see cref="Lead"/> columns further in. Textured as the frost is: billows drifting with
/// the ground, a finer swirl through them and pale streaks, so even whole it is no flat colour.
/// </summary>
public static class FogLooks
{
    /// <summary>Columns inside the zone past the thick fog where it thins to nothing.</summary>
    public const int Lead = 24;

    /// <summary>How thick the fog is where a moved hero falls out of it, of whole: the hero seen through it.</summary>
    public const double Edge = 0.6;

    /// <summary>A move across the fog: the screen held whole this long, then eased back over <see cref="FadeMs"/>.</summary>
    public const double FlashMs = 150, FadeMs = 400;
}
