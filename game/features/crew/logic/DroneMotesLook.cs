using System;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A drone window projected before the hero (drone-holo): a small cloud of hologram motes over the drone, each drifting about its
/// own spot and twinkling, risen out of the lens as it comes on and sunk back as it goes out. Client only. Lengths in world px.
/// </summary>
public static class DroneMotesLook
{
    /// <summary>How many motes, and their side.</summary>
    public const int Count = 12;
    public const double Side = 0.9;

    /// <summary>The cloud's half width and half height about its middle.</summary>
    public const double HalfWide = 4, HalfHigh = 3;

    /// <summary>How far each mote drifts from its spot, and how long a drift takes at most and at least, ms.</summary>
    public const double Drift = 0.7, SlowMs = 2600, QuickMs = 1500;

    /// <summary>The faintest a mote twinkles to, of the hologram's alpha.</summary>
    public const double Faintest = 0.45;

    /// <summary>The cone from the lens up to the cloud: its alpha at the lens and at the cloud, and the cloud's width it spans.</summary>
    public const double ConeAtLens = 0.35, ConeAtCloud = 0.08, ConeWide = 0.8;

    /// <summary>
    /// Mote <paramref name="i"/>'s spot from the cloud's middle, scattered evenly over the cloud's oval by the golden angle, so none
    /// sit together and the cloud reads as one shape.
    /// </summary>
    public static (double X, double Y) SpotOf(int i)
    {
        double r = Math.Sqrt((i + 0.5) / Count), turn = i * 2.399963229728653;
        return (Math.Cos(turn) * r * HalfWide, Math.Sin(turn) * r * HalfHigh);
    }

    /// <summary>
    /// Where mote <paramref name="i"/> is from the cloud's middle at <paramref name="timeMs"/>, <paramref name="shown"/> in: on its
    /// drifting spot when whole, on the way from the lens at <paramref name="lens"/> (from the middle too) before that.
    /// </summary>
    public static (double X, double Y) At(int i, double shown, double timeMs, double seed, (double X, double Y) lens)
    {
        var (x, y) = SpotOf(i);
        double phase = seed + i * 1.7, ms = QuickMs + (SlowMs - QuickMs) * ((i * 7 % Count) / (double)Count);
        double t = 2 * Math.PI * timeMs / ms;
        x += Drift * Math.Sin(t + phase);
        y += Drift * 0.8 * Math.Cos(t * 1.3 + phase * 0.6);
        // Each rises a little after the last, so the cloud swells from its middle out.
        double own = Math.Clamp(shown * 1.4 - 0.4 * (i / (double)(Count - 1)), 0, 1), eased = own * own * (3 - 2 * own);
        return (lens.X + (x - lens.X) * eased, lens.Y + (y - lens.Y) * eased);
    }

    /// <summary>Mote <paramref name="i"/>'s twinkle at <paramref name="timeMs"/>, <see cref="Faintest"/>..1 of the hologram's alpha.</summary>
    public static double Twinkle(int i, double timeMs, double seed)
    {
        double wave = 0.5 + 0.5 * Math.Sin(timeMs / 1000 * (3.1 + i * 0.37) + seed * 1.3 + i * 2.1);
        return Faintest + (1 - Faintest) * wave;
    }
}
