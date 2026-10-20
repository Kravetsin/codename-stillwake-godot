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
    /// A canister's air swirling into the helmet (0081): streaks, motes and translucent wisps, times <see cref="Count"/>,
    /// thrown over <see cref="SwirlEmitMs"/> on a ring <see cref="SwirlFrom"/> to <see cref="SwirlTo"/> pixels round the
    /// visor, squashed to <see cref="SwirlSquash"/> of its height, each spiralling in by <see cref="SwirlTurns"/> of a turn
    /// to the visor over its life; all of it in about 0.9 s.
    /// </summary>
    public const int SwirlStreaks = 16, SwirlMotes = 22, SwirlWisps = 4;
    public const double SwirlEmitMs = 300, SwirlFrom = 9, SwirlTo = 17, SwirlTurns = 1.1, SwirlSquash = 0.8;

    /// <summary>How fast a swirl's middle follows the visor, a share a second as a rate: a head turned eases it over, never jerks it.</summary>
    public const double SwirlFollow = 20;

    /// <summary>Breathing room air again: a few pale motes puffed off the visor, drifting at up to this many pixels a second.</summary>
    public const int VisorMotes = 4;
    public const double VisorDrift = 14;

    /// <summary>The most picked-up icons flying home to the hero on their ribbons of light at once (slice 3): past it a stack shows in the popup alone.</summary>
    public const int MostRibbons = 4;
}
