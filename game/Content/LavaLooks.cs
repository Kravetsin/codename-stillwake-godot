namespace Stillwake.Game;

/// <summary>
/// How the planet's lava looks (its core, <c>Content.Lava</c>): its band drawn glowing from <c>art/planet/wall_lava</c>, flowing
/// slowly, and a light of its colour every <see cref="LightEvery"/> cells along its surface where the open is over it, at most
/// <see cref="MostLights"/>, the nearest the view's middle.
/// </summary>
public static class LavaLooks
{
    /// <summary>The light it gives: a deep orange, stronger than a crystal's, reaching as far as a lamp's.</summary>
    public static readonly (float R, float G, float B) Light = (1f, 0.4f, 0.1f);

    public const float Strength = 1.6f, Keep = 0.8f, Size = 12f;
    public const int Reach = 9;

    /// <summary>Cells between two lights along an open stretch of its surface, from its first: a stretch narrower still gets one.</summary>
    public const int LightEvery = 3;

    /// <summary>The most lights it gives at once: more than the widest view's surface holds at <see cref="LightEvery"/>.</summary>
    public const int MostLights = 96;

    /// <summary>How much each light breathes, up and down, as the lava churns: a share of its strength.</summary>
    public const float Flicker = 0.15f;
}
