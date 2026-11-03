using System;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Items;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// How a switched body's lights show (2026-11-03-the-regen-suit): on with charge, a wave runs up its bands from the boots to the
/// collar after each breath and the collar's arcs flicker; on without it, the bands are dark and the socket's red pip blinks.
/// </summary>
public static class SuitGlow
{
    /// <summary>The share of a breath the wave takes to climb from the boots to the collar.</summary>
    public const double Climb = 0.6;
    /// <summary>The bands' strength between waves.</summary>
    public const double Rest = 0.3;
    /// <summary>How far up the body, as a share of it, the wave's light spreads either side of its front.</summary>
    public const double Spread = 0.25;
    /// <summary>How long the arcs hold one strength, and how long the empty belt's pip is lit and then dark.</summary>
    public const double ArcMs = 70, PipMs = 500;

    /// <summary>A shown hero's suit: its mode working (the sim's <c>SuitOn</c>), its belt able to pay a breath, and how far the world is from one breath to the next.</summary>
    public static (bool On, bool Charged, double Phase) Of(ICrewQueries crew, IItemsQueries items, int peer, long tick) =>
        (crew.SuitOn(peer), items.CanDrain(peer, Stillwake.Content.Suit.RegenCost),
            tick % Stillwake.Content.Crew.BreathEvery / (double)Stillwake.Content.Crew.BreathEvery);

    /// <summary>How far up the body a part's bands sit, 0 at the boots to 1 at the collar; none for a part with none.</summary>
    public static double? UpOf(string part) => part switch
    {
        "shin" => 0.04,
        "thigh" => 0.18,
        "forearm" => 0.22,
        "upperArm" => 0.32,
        "pack" => 0.58,
        "torso" => 0.7,
        _ => null,
    };

    /// <summary>The strength of bands <paramref name="up"/> the body, <paramref name="phase"/> of the way from one breath to the next.</summary>
    public static double Bands(double phase, double up) => Rest + (1 - Rest) * Math.Max(0, 1 - Math.Abs(phase / Climb - up) / Spread);

    /// <summary>The arcs' strength: a new one every <see cref="ArcMs"/>, never steady.</summary>
    public static double Arcs(double timeMs)
    {
        uint h = (uint)(long)(timeMs / ArcMs) * 2654435761u;
        return 0.55 + 0.45 * (h >> 24) / 255.0;
    }

    /// <summary>Whether the empty belt's pip is lit now.</summary>
    public static bool Pip(double timeMs) => timeMs % (2 * PipMs) < PipMs;
}
