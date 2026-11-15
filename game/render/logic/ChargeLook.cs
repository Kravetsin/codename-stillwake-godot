using System;

namespace Stillwake.Game.Render;

/// <summary>
/// How a blade's charge looks (spear-effects, skins): the glow's colour, a torn bolt's root's white heart, the bolts' tint, the
/// blade's glow at rest and what a full charge adds, and the numbers its <see cref="ArcBurst"/> runs on. The arc module's is
/// <see cref="Arc"/>; an effect skin's is its own (<c>SkinEffects</c>).
/// </summary>
public sealed record ChargeLook((int R, int G, int B) Tint, (int R, int G, int B) Core, BoltTint Bolts, ArcNumbers Numbers, double Glow = 0.03, double Charged = 0.12)
{
    /// <summary>The arc module's (0086), the user's pick «Срыв разряда»: violet.</summary>
    public static readonly ChargeLook Arc = new((180, 154, 255), (239, 233, 218), BoltTint.Violet, new ArcNumbers());
}

/// <summary>
/// The numbers a charge runs on (<see cref="ArcBurst"/>), the arc module's by default: its fizz, how it builds and lets go, its
/// torn bolts' lengths and lives. Lengths are world px, lives ms.
/// </summary>
public sealed record ArcNumbers
{
    /// <summary>Fizz sparks a second at rest and what a full charge adds; their length, life and brightness.</summary>
    public double FizzPerSecond { get; init; } = 20;
    public double FizzCharged { get; init; } = 40;
    public (double From, double To) SparkPx { get; init; } = (0.35, 1.25);
    public (double From, double To) SparkMs { get; init; } = (35, 90);
    public (double From, double To) SparkAlpha { get; init; } = (0.4, 0.7);

    /// <summary>World px the blade travels for a whole unit of charge; below <see cref="StillPx"/> a second it leaks <see cref="Leak"/> a frame.</summary>
    public double PxPerCharge { get; init; } = 24;
    public double StillPx { get; init; } = 4;
    public double Leak { get; init; } = 0.985;

    /// <summary>The thresholds a charge lets go at, drawn anew each time; the first.</summary>
    public (double From, double To) Threshold { get; init; } = (0.45, 1.55);
    public double FirstThreshold { get; init; } = 0.8;

    /// <summary>A torn bolt's length, a power law cut to this range with <see cref="Tail"/>; forked from <see cref="ForkPx"/>; its life.</summary>
    public (double From, double To) BoltPx { get; init; } = (1, 12);
    public (double From, double To) BoltMs { get; init; } = (260, 640);
    public double Tail { get; init; } = 1.2;
    public double ForkPx { get; init; } = 5.5;

    /// <summary>A torn bolt's length for a draw <paramref name="u"/> (0 to 1): most short, a few long.</summary>
    public double LengthOf(double u)
    {
        double k = 1 - Math.Pow(BoltPx.From / BoltPx.To, Tail);
        return BoltPx.From / Math.Pow(1 - u * k, 1 / Tail);
    }
}
