namespace Stillwake.Game;

/// <summary>
/// The knobs of the luminous effects (0075), as the review page's sliders named them, each 1 where the
/// user picked them: how long a streak is drawn for its speed, how many particles an effect throws, how
/// hard paths curl, how bright halos are, how many 60 Hz frames a glint holds, how far heat shimmer wobbles.
/// </summary>
public static class Fx
{
    public const double Length = 1, Count = 1, Turbulence = 1, Glow = 1, Shimmer = 1;
    public const int GlintHold = 1;

    /// <summary>
    /// The most glowing particles and flashes that light the room at once: past it the rest only glow, so a
    /// big break costs no more light than a busy furnace room.
    /// </summary>
    public const int MostLights = 48;

    /// <summary>A glowing particle's light, as a share of <see cref="Light.Particle"/> at its brightness.</summary>
    public const float ParticleLight = 0.3f;
}
