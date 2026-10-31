using System.Collections.Generic;

namespace Stillwake.Game;

/// <summary>
/// The creatures' looks the user picked on the loper page (0102), what <see cref="CreatureArt"/>'s generated data means: each design
/// a rig of parts painted as index maps — a pixel's R its ramp, its G its tone, outline to highlight — recoloured at load by a
/// palette, turned and moved about pivots in the world's pixels, its origin the middle of its feet, facing right, y down.
/// </summary>
public static partial class CreatureArt
{
    /// <summary>How a design walks (the page's): legs in diagonal pairs, six in two tripods, a waddle on paws, or hops.</summary>
    public enum Gait { Walk, Tripod, Waddle, Hop }

    /// <summary>What moves a rig's node: nothing, the body, the head, an ear, a leg, the tail, a seed pod, antlers, or the eye.</summary>
    public enum Drive { None, Body, Head, Ear, Leg, Tail, Pod, Antler, Eye }

    /// <summary>A part's picture: <paramref name="W"/> × <paramref name="H"/>, the pixel it turns about, the ramps it is painted in, whether a glow map goes with it.</summary>
    public sealed record Part(int W, int H, int PivotX, int PivotY, int[] Ramps, bool Glow = false);

    /// <summary>
    /// A node of the rig: <paramref name="Part"/> drawn hanging from <paramref name="Parent"/> at (<paramref name="X"/>,
    /// <paramref name="Y"/>), moved by <paramref name="Drive"/>; a leg's place in the stride, whether it is a hind one (a hopper's),
    /// and an ear's resting angle. The part <c>eye</c> is the eye's states, not a picture.
    /// </summary>
    public sealed record Node(string Id, string Part, string? Parent, float X, float Y, Drive Drive, float Phase = 0, bool Hind = false, float Base = 0);

    /// <summary>The eye: where it sits on its parent, and how far its pupil travels each way, calm and wide.</summary>
    public sealed record Eye(float X, float Y, (float X, float Y) Travel, (float X, float Y) TravelWide);

    /// <summary>The page's pose numbers, radians and pixels: ears up and back, perked, a leg's swing and lift, the tail's sway and flight, grazing, a hopper's hop.</summary>
    public sealed record Pose(float EarUp, float EarBack, float EarPerk, float LegAmp, float Lift, float TailAmp, float TailFlee, float Graze, float Hop);

    /// <summary>A design: its gait, whether its head is its body, the ramp its glow maps take, its eye, pose, parts and rig in drawing order.</summary>
    public sealed record Design(
        string Id, Gait Gait, bool Headless, int Glow, Eye Eye, Pose Pose, IReadOnlyDictionary<string, Part> Parts, IReadOnlyList<Node> Rig);

    /// <summary>The parts every design's eye is drawn from: whites calm and wide, pupils calm and wide, half and shut lids.</summary>
    public static readonly string[] EyeParts = ["eye_open", "eye_wide", "pupil", "pupil_wide", "lid_half", "lid_blink"];

    /// <summary>Where a part's picture is: an index map in its design's folder.</summary>
    public static string PictureOf(string design, string part) => $"res://art/creatures/{design}/{part}.png";

    /// <summary>Where a part's glow map is.</summary>
    public static string GlowOf(string design, string part) => $"res://art/creatures/{design}/{part}_glow.png";
}

/// <summary>
/// How each design moves at speed and lights the dark (0102): its gait's numbers, the palette each planet's creatures are
/// recoloured by, and the light their glowing parts cast.
/// </summary>
public static class CreatureLooks
{
    /// <summary>
    /// A design's gait at speed: <paramref name="Stride"/> pixels walked a stride at a slow walk, its strides lengthening as it goes
    /// faster so it takes at most <paramref name="MostStrides"/> a second however fast it runs — a 30-cell-a-second flight a gallop
    /// of long bounds, not a blur of legs; at a full gallop its legs reach <paramref name="Reach"/> farther, its body bounds
    /// <paramref name="Bound"/> pixels a stride, rocks <paramref name="Rock"/> radians nose up and down and stretches
    /// <paramref name="Stretch"/> of its length out and gathers.
    /// </summary>
    public sealed record Run(double Stride, double MostStrides, double Reach, double Bound, double Rock, double Stretch);

    /// <summary>Each design's: the tapir heavier and slower, the six legs quicker and lower, the fluff bounding along in hops.</summary>
    public static readonly IReadOnlyDictionary<string, Run> Runs = new Dictionary<string, Run>
    {
        ["classic"] = new(Stride: 10, MostStrides: 3.2, Reach: 0.6, Bound: 3.5, Rock: 0.07, Stretch: 0.05),
        ["tapir"] = new(Stride: 11, MostStrides: 2.6, Reach: 0.45, Bound: 2.5, Rock: 0.05, Stretch: 0.04),
        ["sixleg"] = new(Stride: 9, MostStrides: 2.8, Reach: 0.5, Bound: 1.5, Rock: 0.03, Stretch: 0.03),
        ["fluff"] = new(Stride: 8, MostStrides: 3.4, Reach: 0, Bound: 5, Rock: 0.06, Stretch: 0.07),
    };

    /// <summary>
    /// Speeds in pixels a second: under <see cref="StillBelow"/> a creature stands, its legs at rest, swinging in full from
    /// <see cref="WalkFrom"/>; a gallop blends in from <see cref="GallopFrom"/>, full at <see cref="GallopFull"/> — a walk is 26 to
    /// 32, a knock 48 to 77, a flight 224 to 240.
    /// </summary>
    public const double StillBelow = 2, WalkFrom = 14, GallopFrom = 90, GallopFull = 200;

    /// <summary>How quickly the speed its legs go by follows the sim's, and its flight and its stand in the air blend in and out, in milliseconds.</summary>
    public const double SpeedMs = 90, BlendMs = 110, GrazeMs = 450;

    /// <summary>How long a creature stands with no hero near before it grazes, in milliseconds.</summary>
    public const double GrazeAfterMs = 1500;

    /// <summary>How long a creature's drawing takes to close a step of its own, two cells, up or down (<c>StepEase</c>, 0106), in milliseconds.</summary>
    public const double StepMs = 100;

    /// <summary>The palette each planet type's creatures are recoloured by: the first planet's the page's «Саванна».</summary>
    public static readonly IReadOnlyDictionary<string, string> PaletteOf = new Dictionary<string, string>
    {
        [Stillwake.Content.PlanetTypes.First.Id] = "savanna",
        [Stillwake.Content.PlanetTypes.Small.Id] = "savanna",
        [Stillwake.Content.PlanetTypes.SmallSpire.Id] = "savanna",
    };

    /// <summary>The palette of a surface whose type names none, as the test strip's.</summary>
    public const string Palette = "savanna";

    /// <summary>
    /// The light a glowing part casts (the fluff's spots and antennae, the tapir's pod, the sixleg's berries): from its glowing
    /// pixels' middle, at <see cref="LightStrength"/> of their colour, falling off by <see cref="LightKeep"/> a cell out to
    /// <see cref="LightReach"/>; the brightest <see cref="MostLights"/> spots of a creature.
    /// </summary>
    public const float LightStrength = 1.1f, LightKeep = 0.837f;
    public const int LightReach = 10, MostLights = 4;

    /// <summary>A glow map's pixels gathered into spots this many pixels square, each a light.</summary>
    public const int SpotPixels = 7;

    /// <summary>
    /// The looks drawn as the tick, the sentry page's candidate 3 «Клещ» (<c>TickArt</c>, <c>TickMotion</c>): a sprite posed and
    /// drawn anew each frame, not a rig of painted parts.
    /// </summary>
    public static readonly IReadOnlySet<string> Ticks = new HashSet<string> { Stillwake.Content.Creatures.Sentry.Look };

    /// <summary>The looks with no art yet, drawn as a plain box that shows what the creature is doing (<c>PlaceholderBodies</c>).</summary>
    public static readonly IReadOnlySet<string> Boxes = new HashSet<string> { Stillwake.Content.Creatures.Warden.Look };

    /// <summary>
    /// A tick's eye lamp's light: its colour at this strength times its brightness (half the page's 0.85, a third more acting), out
    /// to <see cref="LightReach"/> cells; its burner's, orange, at <see cref="TickBurnerLight"/> times its thrust, out to
    /// <see cref="TickBurnerReach"/>.
    /// </summary>
    public const float TickEyeLight = 0.45f, TickActingLight = 1.35f, TickBurnerLight = 0.9f;
    public const int TickBurnerReach = 8;

    /// <summary>The ramps the parts are painted in (the index maps' R, the page's): its fur's two, belly, markings and skin.</summary>
    public const int FurA = 24, FurB = 25, Belly = 26, Mark = 27, Skin = 28;

    /// <summary>
    /// How one creature of a species differs from the next (the user's word, 2026-10-22: by size and colour), drawn only:
    /// <paramref name="Least"/> to <paramref name="Most"/> thousandths of its design's size; its <paramref name="Coat"/> ramp
    /// shaded up to <paramref name="Shade"/> quarter tones lighter or darker and blended up to <paramref name="Blend"/>
    /// thousandths toward <paramref name="Toward"/> or <paramref name="Away"/>, the palette's own ramps, in two steps either way;
    /// one in <paramref name="MorphOneIn"/> a morph, its coat <paramref name="Morph"/> quarter tones off; each marking dropped
    /// <paramref name="Drop"/> thousandths of the time, those kept at least <paramref name="Faint"/> thousandths opaque.
    /// </summary>
    public sealed record Variety(
        int Least, int Most, int Coat, int Shade, int Toward, int Away, int Blend, int MorphOneIn, int Morph, int Drop, int Faint);

    /// <summary>Each species': the loper tan to rosy or ashen, its stripes one to three; the tapir pale at times; the fluff pink or cream, its spots fewer.</summary>
    public static readonly IReadOnlyDictionary<string, Variety> Varieties = new Dictionary<string, Variety>
    {
        ["loper"] = new(Least: 860, Most: 1140, Coat: FurA, Shade: 3, Toward: FurB, Away: Skin, Blend: 420, MorphOneIn: 7, Morph: -4, Drop: 350, Faint: 450),
        ["tapir"] = new(Least: 880, Most: 1160, Coat: FurB, Shade: 3, Toward: FurA, Away: Skin, Blend: 450, MorphOneIn: 7, Morph: 4, Drop: 250, Faint: 500),
        ["sixleg"] = new(Least: 880, Most: 1120, Coat: FurB, Shade: 3, Toward: FurA, Away: Belly, Blend: 450, MorphOneIn: 7, Morph: -4, Drop: 300, Faint: 450),
        ["fluff"] = new(Least: 820, Most: 1150, Coat: FurB, Shade: 3, Toward: Skin, Away: Belly, Blend: 450, MorphOneIn: 7, Morph: 4, Drop: 250, Faint: 1000),
    };
}
