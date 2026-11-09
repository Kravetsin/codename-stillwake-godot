namespace Stillwake.Game;

/// <summary>
/// The knobs of the luminous effects (0075), as the review page's sliders named them, each 1 where the
/// user picked them: how long a streak is drawn for its speed, how many particles an effect throws, how
/// hard paths curl, how bright halos are, how many 60 Hz frames a glint holds, how far heat shimmer wobbles.
/// </summary>
public static class Fx
{
    public const double Length = 1, Count = 1, Turbulence = 1, Glow = 1, Shimmer = 1;

    /// <summary>The breach alarm's red (0075): a venting room's light on the beat, and a weak flash where a meteor cracks a sealed room's wall.</summary>
    public static readonly (int R, int G, int B) Alarm = (255, 50, 44);
    public const int GlintHold = 1;

    /// <summary>
    /// The most glowing particles and flashes that light the room at once: past it the rest only glow, so a
    /// big break costs no more light than a busy furnace room.
    /// </summary>
    public const int MostLights = 48;

    /// <summary>A glowing particle's light, as a share of <see cref="Light.Particle"/> at its brightness.</summary>
    public const float ParticleLight = 0.3f;

    /// <summary>An electric arc's light at its whole brightness (<c>Bolts</c>): a cyan pool round it while it lives.</summary>
    public const float BoltLight = 0.22f;

    /// <summary>
    /// How lively the coil of a running drill with the arc fitted is (slice 3), 1 as picked: its fine arcs, crawling sparks, leaps and glints a
    /// second are times this; its haze pulses as it is.
    /// </summary>
    public const double Crackle = 1;

    /// <summary>
    /// A canister's air flung out of the chest (air-feedback), <see cref="FlingChest"/> of the body up: a flash fading over
    /// <see cref="FlingFlashMs"/>, then <see cref="FlingMotes"/> motes, times <see cref="Count"/>, flung at <see cref="FlingSpeed"/>
    /// pixels a second, each curling up to <see cref="FlingCurl"/> radians a second and dropping a trail mote each <see cref="FlingTrailMs"/>.
    /// </summary>
    public const int FlingMotes = 24;
    public const double FlingChest = 0.55, FlingFlashMs = 280, FlingCurl = 2.6, FlingTrailMs = 20;
    public static readonly (double From, double To) FlingSpeed = (70, 135);

    /// <summary>Breathing room air again: a few pale motes puffed off the visor, drifting at up to this many pixels a second.</summary>
    public const int VisorMotes = 4;
    public const double VisorDrift = 14;

    /// <summary>Rammed (0103): the suit's air knocked out in this many jets and motes, round a ring this many pixels across at its widest.</summary>
    public const int LeakStreaks = 12, LeakMotes = 10;
    public const double LeakRing = 16;

    /// <summary>The most picked-up icons flying home to the hero on their ribbons of light at once (slice 3): past it a stack shows in the tally alone.</summary>
    public const int MostRibbons = 4;

    /// <summary>The most gains' sparks flying to the rim's plate at once (gain-plate): past it a gain counts at once, with no spark.</summary>
    public const int MostSparks = 12;
}
