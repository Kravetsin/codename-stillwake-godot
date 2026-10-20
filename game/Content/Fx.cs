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

    /// <summary>An electric arc's light at its whole brightness (<c>Bolts</c>): a cyan pool round it while it lives.</summary>
    public const float BoltLight = 0.22f;

    /// <summary>
    /// How lively a running arc drill's coil is (slice 3), 1 as picked: its fine arcs, crawling sparks, leaps and glints a
    /// second are times this; its haze pulses as it is.
    /// </summary>
    public const double Crackle = 1;

    /// <summary>
    /// A canister's air rising round the whole body (0081), about a second: a faint flat ring at the feet, <see cref="VortexRing"/>
    /// pixels across at its widest and squashed to <see cref="VortexRingSquash"/>; streaks and motes, times <see cref="Count"/>,
    /// thrown over <see cref="VortexEmitMs"/> from the feet up to <see cref="VortexStartShare"/> of the body, each spiralling
    /// <see cref="VortexTurns"/> times round it, <see cref="VortexRadius"/> pixels out and <see cref="VortexTilt"/> up and down,
    /// to <see cref="VortexAbove"/> pixels over the head, where it fades out; a pale light round the body of <see cref="VortexLight"/>.
    /// </summary>
    public const int VortexStreaks = 28, VortexMotes = 30;
    public const double VortexEmitMs = 450, VortexStartShare = 0.3, VortexTurns = 1.5, VortexRadius = 11, VortexTilt = 2.5, VortexAbove = 12;
    public const double VortexRing = 16, VortexRingSquash = 0.3, VortexLight = 0.22;

    /// <summary>How fast a vortex follows the body, a rate a second: a turn or a step eases it over, never jerks it.</summary>
    public const double VortexFollow = 20;

    /// <summary>Breathing room air again: a few pale motes puffed off the visor, drifting at up to this many pixels a second.</summary>
    public const int VisorMotes = 4;
    public const double VisorDrift = 14;

    /// <summary>The most picked-up icons flying home to the hero on their ribbons of light at once (slice 3): past it a stack shows in the popup alone.</summary>
    public const int MostRibbons = 4;
}
