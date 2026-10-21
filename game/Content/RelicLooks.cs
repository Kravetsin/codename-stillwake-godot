namespace Stillwake.Game;

public static partial class RelicArt
{
    /// <summary>
    /// Where a relic's arcs play, in pixels from its piece's box corner: its heart (<paramref name="Core"/>), the cracks they leave
    /// from (<paramref name="Sources"/>) and its own metal they jump to (<paramref name="Metal"/>): the collar, the legs, the plates.
    /// </summary>
    public sealed record Sparking((float X, float Y) Core, (float X, float Y)[] Sources, (float X, float Y)[] Metal);
}

/// <summary>
/// How the arc relic looks alive (0095), the relic concept page's live layer at its 24 steps a second: its colour, the rates of its
/// arcs, its big discharges, the light it gives a spire — the lens, the lit shaft, the pillar of light over it, the glowing roots
/// in the rock round its chamber — and how it dies once it is taken. Every number here is the page's, or set against it in game.
/// </summary>
public static class RelicLooks
{
    /// <summary>The relic's light: violet-white, as the arc tech's mark (the page's <c>VC.light</c>).</summary>
    public static readonly (float R, float G, float B) Light = (0.74f, 0.62f, 1f);

    /// <summary>Its arcs' glow and its sparks' colours, core to rim: the page's violet-white.</summary>
    public static readonly (int R, int G, int B) Glow = (150, 110, 255), Hot = (235, 225, 255), Core = (253, 250, 255), Mid = (180, 147, 255);

    /// <summary>
    /// A page step's chance of a small arc from a crack to its own metal or into the air (a quarter), and of a reach to the room's
    /// metal (its <see cref="FarShare"/>); a drip of sparks off it; the cradle's and the niche's chip's.
    /// </summary>
    public const double Rate = 0.55, SmallRate = 0.5, FarShare = 0.12, Drip = 0.22, SmallDrip = 0.12;

    /// <summary>Seconds between big discharges, the cradle's and the chip's: each a branchy bolt from its heart to the room's metal, re-lighting it.</summary>
    public static readonly (double From, double To) Big = (2.6, 5.5), SmallBig = (3, 6.5);

    /// <summary>
    /// A big discharge's light: a flash of the relic's colour at its heart, <see cref="FlashStrength"/> strong (the chip's
    /// <see cref="SmallFlash"/> of it), reaching <see cref="FlashReach"/> cells and dying by <see cref="FlashDecay"/> a 60 Hz frame.
    /// </summary>
    public const double FlashStrength = 2.4, SmallFlash = 0.75, FlashDecay = 0.1;
    public const int FlashReach = 12;

    /// <summary>
    /// The hum: its light breathing over <see cref="HumSeconds"/> between <see cref="HumLow"/> and 1 of its strength, as the page eases
    /// between its trough and crest bakes.
    /// </summary>
    public const double HumSeconds = 1.7, HumLow = 0.62;

    /// <summary>
    /// Taken, the cradle sparks <see cref="DyingSeconds"/> more, its arcs thinning out, its last discharge into the nearest metal
    /// <see cref="LastAt"/> seconds in; then the whole spire goes dark over <see cref="DarkSeconds"/>.
    /// </summary>
    public const double DyingSeconds = 3, LastAt = 2.5, DarkSeconds = 0.4;

    /// <summary>
    /// The lit shaft (the light well): a light every <see cref="ShaftEvery"/> pixels up its middle column from the chamber to the
    /// lens, <see cref="ShaftStrength"/> strong out to <see cref="ShaftReach"/> cells, so the ledges either side catch it; the beam
    /// itself <see cref="BeamWide"/> pixels across, its brightest <see cref="BeamAlpha"/>.
    /// </summary>
    public const int ShaftEvery = 26, ShaftReach = 3, BeamWide = 11;
    public const float ShaftStrength = 0.62f, ShaftKeep = 0.7f, BeamAlpha = 0.32f;

    /// <summary>
    /// The lens's light and the pillar it throws into the sky: <see cref="PillarCells"/> tall, from <see cref="PillarFoot"/> pixels
    /// across at the lens to <see cref="PillarTop"/> at its top, <see cref="PillarAlpha"/> at its foot fading up; a thin white core.
    /// </summary>
    public const int LensReach = 7, PillarCells = 72, PillarFoot = 8, PillarTop = 46;
    public const float LensStrength = 1.4f, PillarAlpha = 0.5f, CoreAlpha = 0.5f;

    /// <summary>
    /// The cradle's own light from its heart (the page's reach), breathing with its hum; the chip's is its picture's (<c>HomesArt</c>).
    /// </summary>
    public const int CradleReach = 9;
    public const float CradleStrength = 2.6f, LightKeep = 0.72f, LightSize = 4f;

    /// <summary>
    /// The roots (V1's fulgurites): <see cref="Roots"/> veins of fused glass from the cradle down and out into the rock, each
    /// <see cref="RootLength"/> pixels long, glowing faintly where the rock still stands round them.
    /// </summary>
    public const int Roots = 9;
    public static readonly (int From, int To) RootLength = (60, 120);

    /// <summary>A hit on the cradle discharges into its striker (the sim's shock): an arc from its heart to the hero this long, in 60 Hz frames.</summary>
    public const int ZapLife = 10;

    /// <summary>Motes rising in the beam, a page step's chance of one.</summary>
    public const double Motes = 0.5;
}
