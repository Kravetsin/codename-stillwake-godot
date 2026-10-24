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
    public sealed record Look(int Variants, int Sink);

    public static readonly IReadOnlyDictionary<string, Look> Of = new Dictionary<string, Look>
    {
        ["acacia"] = new(3, 2),
        ["bush"] = new(2, 2),
        ["rock"] = new(2, 2),
        ["bellTeal"] = new(2, 1),
        ["bellViolet"] = new(2, 1),
        ["bellAmber"] = new(2, 1),
        ["tuft"] = new(3, 1),
    };

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
}
