using System.Collections.Generic;
using Stillwake.Game.Render;

namespace Stillwake.Game;

/// <summary>
/// How what grows in a planet's caves looks (0080), drawn in <c>art/caves</c> from the cave concept's own drawing code
/// (direction C, light by depth band, with direction A's glow moss and threads in the garden): each kind's pictures,
/// <c>{kind}_{variant}</c> — a stalactite's and a stalagmite's <c>{kind}_{band}_{variant}</c>, in its band's stone — with what
/// of it glows in <c>…_glow</c>; the light a glowing one casts; the glints that sparkle in a helmet's beam; the spores, drips
/// and embers about them; and the stone's tint by band.
/// </summary>
public static class CaveLooks
{
    /// <summary>
    /// A kind's look: how many pictures; whether they are by band; the light it casts, if any — a colour, at a strength, keeping
    /// <c>Keep</c> a cell out to <c>Reach</c> cells — from its glow's middle; how many
    /// glints sparkle in a beam on it and their colour.
    /// </summary>
    public sealed record Look(int Variants)
    {
        public bool ByBand { get; init; }
        public (float R, float G, float B)? Light { get; init; }
        public float Strength { get; init; } = 0.8f;
        public float Keep { get; init; } = 0.55f;
        public int Reach { get; init; } = 3;
        /// <summary>How far its base sinks under the ground's top edge, or its top up into a ceiling, in pixels: planted, not set on it.</summary>
        public int Sink { get; init; } = 2;
        public int Glints { get; init; }
        public (int R, int G, int B) GlintColour { get; init; } = (244, 240, 255);
    }

    // The concept's palette of light (objects.js PAL).
    private static readonly (float, float, float) Teal = (0.35f, 1f, 0.85f), Violet = (0.7f, 0.5f, 1f), Blue = (0.5f, 0.78f, 1f),
        Amber = (1f, 0.7f, 0.32f), Rose = (1f, 0.5f, 0.5f), Ember = (1f, 0.42f, 0.16f), Green = (0.6f, 1f, 0.45f);

    public static readonly IReadOnlyDictionary<string, Look> Of = new Dictionary<string, Look>
    {
        ["glowcapTeal"] = new(3) { Light = Teal, Strength = 1.5f, Keep = 0.8f, Reach = 8 },
        ["glowcapViolet"] = new(3) { Light = Violet, Strength = 1.5f, Keep = 0.8f, Reach = 8 },
        ["crystalIce"] = new(3) { Light = Blue, Strength = 1.5f, Keep = 0.8f, Reach = 8, Glints = 2, GlintColour = (232, 248, 255) },
        ["crystalAmber"] = new(3) { Light = Amber, Strength = 1.5f, Keep = 0.8f, Reach = 8, Glints = 2, GlintColour = (255, 242, 192) },
        ["crystalRose"] = new(3) { Light = Rose, Strength = 1.35f, Keep = 0.8f, Reach = 8, Glints = 2, GlintColour = (255, 224, 220) },
        ["crystalEmber"] = new(3) { Light = Ember, Strength = 1.5f, Keep = 0.8f, Reach = 8, Glints = 1, GlintColour = (255, 240, 176) },
        ["stalagmite"] = new(3) { ByBand = true, Sink = 1 },
        ["stalactite"] = new(3) { ByBand = true, Sink = 1 },
        ["crystalIceHang"] = new(3) { Light = Blue, Strength = 1.4f, Keep = 0.78f, Reach = 8, Glints = 2, GlintColour = (232, 248, 255) },
        ["crystalAmberHang"] = new(3) { Light = Amber, Strength = 1.4f, Keep = 0.78f, Reach = 8, Glints = 2, GlintColour = (255, 242, 192) },
        ["hangcap"] = new(3) { Light = Violet, Strength = 1.3f, Keep = 0.76f, Reach = 7 },
        ["threadViolet"] = new(3) { Light = Violet, Strength = 1.1f, Keep = 0.74f, Reach = 6 },
        ["threadTeal"] = new(3) { Light = Teal, Strength = 1.1f, Keep = 0.74f, Reach = 6 },
        ["roots"] = new(3) { Light = Green, Strength = 0.8f, Keep = 0.66f, Reach = 4 },
        ["moss"] = new(4) { Light = Teal, Strength = 1f, Keep = 0.7f, Reach = 5 },
        ["mica"] = new(0) { Glints = 3 },
        ["veinIce"] = new(3) { Glints = 3, GlintColour = (232, 250, 255) },
        ["veinCopper"] = new(3) { Glints = 3, GlintColour = (255, 208, 138) },
        ["fossil"] = new(3),
        ["geode"] = new(2) { Light = Violet, Strength = 1.4f, Keep = 0.78f, Reach = 7, Glints = 2, GlintColour = (240, 216, 255) },
        ["heatCrack"] = new(4) { Light = Ember, Strength = 1.3f, Keep = 0.74f, Reach = 6 },
    };

    /// <summary>Moss in the cold band is the surface's green; deeper it is the garden's teal.</summary>
    public static readonly (float R, float G, float B) ColdMoss = Green;

    /// <summary>
    /// The most cave lights shown at once, the nearest the view's middle: more than the 323 the densest view of the planet holds at
    /// the farthest zoom with every glowing growth's reach round it (seed 3096446930), so two alike in view always light alike; past
    /// it, zoomed out further in development only, those farthest from the middle go dark.
    /// </summary>
    public const int MostLights = 384;

    /// <summary>
    /// How wide a growth's light is, in pixels: its shadows' edges soften over as much, as a lamp's do, not cut along a line from a
    /// point.
    /// </summary>
    public const float LightSize = 4f;

    /// <summary>
    /// A glint in a helmet's beam (the concept's): a white point and a cross of its colour, arms of <see cref="GlintArm"/> pixels,
    /// twinkling about <see cref="GlintTwinkle"/> times a second, shown only where the beam reaches it with nothing between.
    /// </summary>
    public const int GlintArm = 3;
    public const double GlintTwinkle = 1.6;

    /// <summary>
    /// What drifts in the caves, from what is in view only (0075's toolkit): spores up from glowcaps, drips from one stalactite
    /// in <see cref="DripShare"/> every <see cref="DripEvery"/> seconds, faint embers up from the heat's cracks; made at
    /// <see cref="MotesASecond"/>, at most <see cref="MostMotes"/> alive together.
    /// </summary>
    public const double MotesASecond = 9;
    public const int MostMotes = 48, DripShare = 3;
    public static readonly (double From, double To) DripEvery = (2.5, 6);

    /// <summary>A spore: a fine soft dot drifting slowly up and curling, in its cap's colour.</summary>
    public static readonly Luminous Spore = new(Glimmer.Mote, [(236, 255, 246), (160, 236, 214)], (3000, 6000))
    { Bright = 0.75, Fade = 0.9, Gravity = -3, Drag = 0.5, Turbulence = 8, NoiseScale = 0.05, Size = 0.6 };

    /// <summary>
    /// A drip: a short pale streak falling from a stalactite's tip to the first solid under it, within <see cref="DripRows"/>
    /// cells, splashing there in <see cref="SplashDrops"/> <see cref="Splash"/>es; its life long enough to fall them all.
    /// </summary>
    public static readonly Luminous Drip = new(Glimmer.Streak, [(236, 250, 255), (180, 220, 245)], (2600, 2800))
    { Bright = 0.8, Fade = 0.2, Gravity = 420, Drag = 0, Width = 0.22, Stretch = 0.03, Shortest = 2, Longest = 6, Matter = true, Bounce = 0 };

    public const int DripRows = 60, SplashDrops = 3;

    /// <summary>A drop of a drip's splash: a fleck thrown up off where it landed, falling back.</summary>
    public static readonly Luminous Splash = new(Glimmer.Streak, [(236, 250, 255), (180, 220, 245)], (180, 320))
    { Bright = 0.7, Fade = 1, Gravity = 300, Drag = 0.5, Width = 0.2, Stretch = 0.03, Shortest = 0.8, Longest = 2.5, Matter = true, Bounce = 0 };

    /// <summary>A faint ember rising from the heat's cracks over the bedrock.</summary>
    public static readonly Luminous Ember_ = Luminous.Ember with { LifeMs = (2200, 4200), Gravity = -10, Drag = 0.9, Turbulence = 10, Bright = 0.7, Light = false };

    /// <summary>
    /// The stone's tint by band, multiplied over the ground in its rows (0080): the coldest a cool blue-grey, the garden as it is,
    /// the amber deep warmer, the ember band a warm red-brown; blended from one band's middle to the next. From
    /// <see cref="TintFrom"/> rows under the surface's middle, so the land under the sky keeps its own colours.
    /// </summary>
    public static readonly (float R, float G, float B)[] Tint = [(0.86f, 0.94f, 1f), (0.94f, 1f, 0.98f), (1f, 0.9f, 0.88f), (1f, 0.78f, 0.66f)];
    public const int TintFrom = 40, TintOver = 60;
}
