using System;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Sound;

/// <summary>The buses' settings at a share of the way into space (ADR 0033, 0042).</summary>
/// <param name="WorldHz">The World bus's low-pass cutoff.</param>
/// <param name="WorldGain">The World bus's volume, as a linear gain: fading to silence.</param>
/// <param name="SuitHz">The Suit bus's low-pass cutoff: what the hero touches, dulled.</param>
/// <param name="SuitLows">The Suit bus's low-shelf gain: the thud's body.</param>
/// <param name="SuitDb">The Suit bus's volume in decibels: louder out in space, over its bed (0059).</param>
public readonly record struct Mix(double WorldHz, double WorldGain, double SuitHz, double SuitLows, double SuitDb);

/// <summary>
/// The hearing's arithmetic, apart from Godot: how the buses sound at a share of the way from aboard (0)
/// to floating in space (1), and how a share eases toward where it is going.
/// </summary>
public static class Hearing
{
    /// <summary>The quietest the World bus is set: its gain never reaches 0, whose decibels are infinite.</summary>
    public const double Faint = 1e-4;

    /// <summary>The buses <paramref name="floating"/> of the way into space: World dulled and faded, Suit dulled to a thud and raised; the Helmet bus is never touched.</summary>
    public static Mix At(double floating) => new(
        Cutoff(S.ClearHz, S.MuffledHz, floating),
        Math.Max(1 - floating, Faint),
        Cutoff(S.ClearHz, S.SuitMuffledHz, floating),
        1 + (S.SuitLowsGain - 1) * floating,
        S.SuitFloatingDb * floating);

    /// <summary>A cutoff <paramref name="share"/> of the way from <paramref name="clear"/> to <paramref name="muffled"/>, eased in log space so it dulls evenly across the octaves.</summary>
    public static double Cutoff(double clear, double muffled, double share) =>
        Math.Exp(Math.Log(clear) + (Math.Log(muffled) - Math.Log(clear)) * share);

    /// <summary>
    /// <paramref name="share"/> moved <paramref name="elapsedS"/> seconds toward 1 while <paramref name="on"/>,
    /// taking <paramref name="inS"/> for the whole way, or toward 0 taking <paramref name="outS"/>.
    /// </summary>
    public static double Ease(double share, bool on, double elapsedS, double inS, double outS) =>
        on ? Math.Min(1, share + elapsedS / inS) : Math.Max(0, share - elapsedS / outS);
}
