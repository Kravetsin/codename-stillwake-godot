using System;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The drone's flurry while a drone window is out (drone-windows): at the hero's waist, a few of its sparks dart about it at random in a
/// small oval, as the old hologram's motes drifted over it, risen out of the core as it comes and sunk back as it goes. Pick px about
/// the core and seconds of the client's clock; client only.
/// </summary>
public sealed class DroneFlurry
{
    /// <summary>How many sparks dart; the oval's half width and half height they dart in, pick px (the old motes' cloud, 4 by 3 world px).</summary>
    public const int Count = 14;
    public const float HalfWide = 10, HalfHigh = 7.5f;

    /// <summary>The shortest and longest a dart takes, s; the share of one spent darting, the rest hovering where it went.</summary>
    public const float HopShortest = 0.18f, HopLongest = 0.5f, Dart = 0.45f;

    /// <summary>How long it takes to rise out of the core, and to sink back, s.</summary>
    public const float RiseS = 0.15f, SinkS = 0.12f;

    /// <summary>A spark's least and most heat, flickering anew each dart.</summary>
    public const float Coolest = 0.35f, Hottest = 1;

    /// <summary>How far out it is: 0 in the core, 1 darting all about.</summary>
    public float Shown { get; private set; }

    /// <summary>Steps it <paramref name="seconds"/> on, <paramref name="on"/> while a drone window is out.</summary>
    public void Step(double seconds, bool on) =>
        Shown = Math.Clamp(Shown + (float)(on ? seconds / RiseS : -seconds / SinkS), 0, 1);

    /// <summary>Spark <paramref name="i"/> at <paramref name="t"/> s: where it is about the core, the way it darts, before or behind, how hot.</summary>
    public Spark At(int i, double t)
    {
        float hop = HopShortest + (HopLongest - HopShortest) * Hash(i, 0.5);
        double phase = t / hop + Hash(i, 0.25);
        long n = (long)Math.Floor(phase);
        float s = (float)(phase - n), e = Math.Min(1, s / Dart);
        e = e * e * (3 - 2 * e);
        var (ax, ay) = Spot(i, n);
        var (bx, by) = Spot(i, n + 1);
        float x = ax + (bx - ax) * e, y = ay + (by - ay) * e, dx = bx - ax, dy = by - ay, d = MathF.Sqrt(dx * dx + dy * dy);
        (dx, dy) = d > 1e-4f ? (dx / d, dy / d) : (1, 0);
        // Each rises a little after the last, so the flurry swells from the core out.
        float own = Math.Clamp(Shown * 1.4f - 0.4f * i / (Count - 1), 0, 1);
        own = own * own * (3 - 2 * own);
        float heat = (Coolest + (Hottest - Coolest) * Hash(i, n + 0.75)) * own * (s < Dart ? 1 : 0.8f);
        return new Spark(x * own, y * own, dx, dy, Hash(i, n + 0.125) > 0.4f, heat, false, 0, 0);
    }

    /// <summary>Spark <paramref name="i"/>'s <paramref name="n"/>-th spot, somewhere in the oval, never in the core's middle.</summary>
    private static (float X, float Y) Spot(int i, long n)
    {
        float turn = MathF.Tau * Hash(i, n + 0.3), r = 0.35f + 0.65f * MathF.Sqrt(Hash(i, n + 0.6));
        return (MathF.Cos(turn) * r * HalfWide, MathF.Sin(turn) * r * HalfHigh);
    }

    /// <summary>A hash of spark <paramref name="i"/> and <paramref name="n"/>, 0..1.</summary>
    private static float Hash(int i, double n)
    {
        double v = Math.Sin(i * 127.1 + n * 311.7 + 74.7) * 43758.5453;
        return (float)(v - Math.Floor(v));
    }
}
