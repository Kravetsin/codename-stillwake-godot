namespace Stillwake.Game;

/// <summary>
/// How the planet's lava looks (its core, <c>Content.Lava</c>), after the cave concepts' core (0087): its band and the haze off
/// it drawn by the lava view's shaders, and where the open is over its surface a light of its colour every
/// <see cref="LightEvery"/> cells, at most <see cref="MostLights"/>, the nearest the view's middle, and embers rising.
/// </summary>
public static class LavaLooks
{
    /// <summary>The light it gives, the concept's: a deep orange, strong and holding well over a few cells.</summary>
    public static readonly (float R, float G, float B) Light = (1f, 0.42f, 0.14f);

    public const float Strength = 1.8f, Keep = 0.85f, Size = 12f;
    public const int Reach = 7;

    /// <summary>Cells between two lights along an open stretch of its surface, from its first: a stretch narrower still gets one.</summary>
    public const int LightEvery = 3;

    /// <summary>The most lights it gives at once: more than the widest view's surface holds at <see cref="LightEvery"/>.</summary>
    public const int MostLights = 96;

    /// <summary>How much each light breathes, up and down, as the lava churns: a share of its strength.</summary>
    public const float Flicker = 0.15f;

    /// <summary>The warm haze over open lava, the concept's bloom: how many rows it rises, its strength at the surface, its colour.</summary>
    public const int HazeRows = 1;

    public const float Haze = 0.8f;
    public static readonly (float R, float G, float B) HazeColour = (1f, 0.5f, 0.18f);

    /// <summary>Embers rising off the open surface near the view, a second for each open cell of it.</summary>
    public const double EmbersASecond = 0.06;
}
