using System.Collections.Generic;
using Stillwake.Game.Render;

namespace Stillwake.Game;

/// <summary>
/// How what grows on a planet looks (0079), drawn in <c>art/planet</c> in the "Stillwake" style: each kind's pictures,
/// <c>{kind}_{variant}</c> with what of it glows in <c>{kind}_{variant}_glow</c>, a plant showing one by a hash of its cell;
/// the light its glowing parts cast; and the motes drifting about them in the day.
/// </summary>
public static class FloraLooks
{
    /// <summary>
    /// A kind's pictures: how many, and how many pixels the lowest drawn row of each sinks under the ground's top edge, the rows
    /// there cut off: planted in the ground, not set on it.
    /// </summary>
    /// <param name="Ink">What else its pictures may be drawn as, each the same size with its light in the same place, one a plant.</param>
    /// <param name="Stir">How it answers a body brushing it (plant-stirs).</param>
    public sealed record Look(int Variants, int Sink, IReadOnlyList<string>? Ink = null, Stir Stir = Stir.None);

    /// <summary>How a plant answers a body brushing it (plant-stirs).</summary>
    public enum Stir
    {
        /// <summary>Not at all: a rock, a bell.</summary>
        None,
        /// <summary>It bends with the grass: a tuft.</summary>
        Bends,
        /// <summary>A runner shakes leaves off it: a bush.</summary>
        Sheds,
        /// <summary>It sheds, and once felled it falls over: an acacia.</summary>
        Topples,
    }

    public static readonly IReadOnlyDictionary<string, Look> Of = new Dictionary<string, Look>
    {
        ["acacia"] = new(3, 2, Stir: Stir.Topples),
        ["bush"] = new(2, 2, Stir: Stir.Sheds),
        ["rock"] = new(2, 2),
        ["bellTeal"] = new(2, 1),
        ["bellViolet"] = new(2, 1),
        ["bellAmber"] = new(2, 1),
        ["tuft"] = new(3, 1, ["tuft", "tuftReed", "tuftFungus"], Stir.Bends),
    };

    /// <summary>The picture a plant of <paramref name="kind"/> is drawn with, by a hash of its cell; its shape and light are read from <c>{kind}_{variant}</c> whatever this says.</summary>
    public static string Drawn(string kind, int variant, uint hash)
    {
        var look = Of[kind];
        return look.Ink is { } kinds ? $"planet/{kinds[(int)(hash / (uint)look.Variants % (uint)kinds.Count)]}_{variant}" : $"planet/{kind}_{variant}";
    }

    /// <summary>Every picture a plant of <paramref name="kind"/> may be drawn with, each ink's variants in turn: what a backdrop composes from.</summary>
    public static IEnumerable<string> Pictures(string kind)
    {
        var look = Of[kind];
        foreach (var ink in look.Ink ?? [kind])
            for (int v = 0; v < look.Variants; v++)
                yield return $"planet/{ink}_{v}";
    }

    /// <summary>
    /// The light a glowing part casts (a seed pod, a berry, a bell), as a lamp's falling off by <see cref="LightKeep"/> a cell
    /// out to <see cref="LightReach"/>, at <see cref="LightStrength"/> of its colour; at most <see cref="MostLights"/> a plant.
    /// </summary>
    public const float LightStrength = 0.9f, LightKeep = 0.632f;
    public const int LightReach = 4, MostLights = 3;

    /// <summary>
    /// Motes drifting in the daylight (0075's luminous toolkit): <see cref="MotesASecond"/> made while any glowing plant is in
    /// view, at most <see cref="MostMotes"/> alive; <see cref="NearShare"/> of them by a glowing plant, in its light's colour,
    /// the rest pale gold in the open air over the ground.
    /// </summary>
    public const double MotesASecond = 7, NearShare = 0.65;
    public const int MostMotes = 40;

    /// <summary>A mote: a fine soft dot, drifting slowly up on a curling path, fading over seconds; it gives no light.</summary>
    public static readonly Luminous Mote = new(Glimmer.Mote, [(236, 255, 246), (160, 236, 214)], (3500, 7000))
    { Bright = 0.8, Fade = 0.9, Gravity = -2.5, Drag = 0.4, Turbulence = 7, NoiseScale = 0.04, Size = 0.7 };

    /// <summary>The pale gold motes of the open air.</summary>
    public static readonly (int R, int G, int B)[] AirMote = [(255, 246, 214), (255, 232, 170)];

    /// <summary>A felled plant's leaves, chips of green, and a broken rock's pebbles: <see cref="Fallen"/> of them, falling as matter does on a planet.</summary>
    public static readonly Luminous Leaf = Luminous.Chip with { Ramp = [(149, 200, 138), (63, 122, 98)], Gravity = 260, Bounce = 0.2, Size = 1.4, Low = true };
    public static readonly Luminous Pebble = Luminous.Chip with { Ramp = [(138, 146, 160), (82, 88, 102)], Gravity = 420, Bounce = 0.25, Size = 1.6, Low = true };
    public const int Fallen = 10;

    /// <summary>A hit that cracks a plant without felling it: a shake of this many pixels for this many seconds; what it knocks off is <see cref="HitLooks"/>'.</summary>
    public const float Shake = 1f, ShakeSeconds = 0.18f;

    /// <summary>
    /// How grass answers a body (plant-stirs): parted <see cref="Part"/> pixels at its tips either side of the feet, out to about
    /// <see cref="Reach"/> pixels; leaned <see cref="Lean"/> more the way a runner goes, at a run; sprung back at
    /// <see cref="SpringHz"/>, <see cref="Damping"/> of the way to critical (under 1 overshoots, 1 does not).
    /// </summary>
    public sealed record Feel(string Name, float Reach, float Part, float Lean, float SpringHz, float Damping);

    /// <summary>The feels the user compares in play, F8 in a dev room; the first is the game's.</summary>
    public static readonly IReadOnlyList<Feel> Feels =
    [
        new("soft", Reach: 12, Part: 4, Lean: 3, SpringHz: 2.5f, Damping: 0.3f),
        new("springy", Reach: 10, Part: 4, Lean: 2, SpringHz: 4f, Damping: 0.2f),
        new("heavy", Reach: 14, Part: 5, Lean: 4, SpringHz: 1.5f, Damping: 1f),
    ];

    /// <summary>The feel shown: <see cref="Feels"/>' first, another only while compared in a dev room.</summary>
    public static Feel Feeling { get; set; } = Feels[0];

    /// <summary>A body faster than <see cref="BrushSpeed"/> pixels a second sways a bush or a tree it brushes, once each <see cref="BrushEvery"/> seconds.</summary>
    public const float BrushSpeed = 40, BrushEvery = 0.6f;

    /// <summary>
    /// How a bush or a tree sways about its foot: at <see cref="Hz"/>, <see cref="Damping"/> of critical, a brush or a hit
    /// kicking it <see cref="Kick"/> radians a second, never past <see cref="Most"/> radians; a full gust holds it
    /// <see cref="Gust"/> radians over. Leaves it sheds: <see cref="Hit"/> a hit, <see cref="Trail"/> a second dragged
    /// behind a runner going through it; at most <see cref="Leafage"/> before it grows them back, <see cref="Regrow"/> a second.
    /// </summary>
    public sealed record Swing(float Hz, float Damping, float Kick, float Most, float Gust, int Hit, float Trail, float Leafage, float Regrow);

    /// <summary>A bush: light, quick, its leaves few.</summary>
    public static readonly Swing Bush = new(Hz: 1.4f, Damping: 0.22f, Kick: 0.5f, Most: 0.12f, Gust: 0.05f, Hit: 40, Trail: 90, Leafage: 60, Regrow: 6);

    /// <summary>An acacia: heavy and slow, a crown full of leaves.</summary>
    public static readonly Swing Tree = new(Hz: 0.7f, Damping: 0.18f, Kick: 0.1f, Most: 0.05f, Gust: 0.02f, Hit: 80, Trail: 0, Leafage: 320, Regrow: 15);

    /// <summary>How <paramref name="stir"/> sways; null for what does not.</summary>
    public static Swing? SwingOf(Stir stir) => stir switch { Stir.Sheds => Bush, Stir.Topples => Tree, _ => null };

    /// <summary>
    /// The wind on a surface with plants, a look only: calm for <see cref="Calm"/> seconds, then a gust for <see cref="Gust"/>
    /// that leans grass's tips <see cref="Lean"/> pixels at full strength, carries leaves at <see cref="Speed"/> pixels a second
    /// and blows <see cref="Leaves"/> a second across the view, from the crowns in view and the air upwind.
    /// </summary>
    public sealed record Wind((double From, double To) Calm, (double From, double To) Gust, float Lean, float Speed, double Leaves);

    public static readonly Wind Breeze = new(Calm: (25, 70), Gust: (5, 9), Lean: 3, Speed: 110, Leaves: 80);

    /// <summary>
    /// How each look of leaf (<c>tools/anime-art</c> <c>leaves</c>, picked in plant-stirs) flies: the leaflet swings like a
    /// pendulum, the blot flutters wide and slow, the dry leaf spins and falls faster, the sprig tumbles and falls fastest.
    /// All four are drawn for an acacia and a bush, kept for the plants to come.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Leaves.Flight> Flights = new Dictionary<string, Leaves.Flight>
    {
        ["leaflet"] = new(Fall: 26, Sway: 15, SwayHz: 0.85f, FlipHz: 1.3f, Spin: 0.7f, Tilt: 0.55f, Catch: 1),
        ["blot"] = new(Fall: 19, Sway: 20, SwayHz: 0.65f, FlipHz: 0.9f, Spin: 0.5f, Tilt: 0.7f, Catch: 1.15f),
        ["dry"] = new(Fall: 34, Sway: 6, SwayHz: 1.3f, FlipHz: 2.4f, Spin: 3.2f, Tilt: 0.2f, Catch: 0.85f),
        ["sprig"] = new(Fall: 40, Sway: 4, SwayHz: 1, FlipHz: 1.1f, Spin: 4.2f, Tilt: 0.15f, Catch: 0.7f),
    };

    /// <summary>The leaves the plants shed, front and back, and how each flies: the acacia's blots, then the bush's sprigs.</summary>
    public static readonly IReadOnlyList<(string Front, string Back, Leaves.Flight Flight)> LeafPictures = [.. Shed("blot", "acacia"), .. Shed("sprig", "bush")];

    private static IEnumerable<(string, string, Leaves.Flight)> Shed(string look, string plant)
    {
        for (int n = 0; n < 3; n++) yield return ($"planet/leaf_{look}_{plant}_{n}", $"planet/leaf_{look}_{plant}_{n}_back", Flights[look]);
    }

    /// <summary>Which of <see cref="LeafPictures"/> a plant of <paramref name="kind"/> sheds: the first and how many.</summary>
    public static (int First, int Count) LeavesOf(string kind) => kind == "bush" ? (3, 3) : (0, 3);

    /// <summary>
    /// A felled acacia falls away from the nearest hero within <see cref="FellerReach"/> pixels, as a rod tips, gaining
    /// <see cref="FallPull"/> radians a second each second for every radian it leans; <see cref="FallLeaves"/> fly off its crown
    /// as it starts, <see cref="LandLeaves"/> where it lands; it lies <see cref="LieSeconds"/>, then fades over
    /// <see cref="FadeSeconds"/>.
    /// </summary>
    public const float FellerReach = 6 * 8, FallPull = 7, LieSeconds = 3, FadeSeconds = 1.5f;
    public const int FallLeaves = 80, LandLeaves = 160;
}
