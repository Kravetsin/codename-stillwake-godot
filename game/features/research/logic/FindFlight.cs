using System;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// The find's way down from the burst bulb into the research key, as picked (hologram-keys): the key throws a beam up to the bulb, the find slides down it,
/// shrinking and fading into the hologram's colour, the beam drawn back after it. Times are seconds since the burst; points in screen pixels.
/// </summary>
public static class FindFlight
{
    /// <summary>How long the beam takes to reach the bulb, and when the find sets off down it.</summary>
    public const double ReachS = 0.14, SetsOffS = 0.12;

    /// <summary>The find's size as it hatches and as it lands.</summary>
    public const double HatchSize = 40, LandSize = 18;

    /// <summary>How far down the beam the find is at <paramref name="t"/>, 0 at the bulb to 1 on the key, eased.</summary>
    public static double Along(double t) => Ease((t - SetsOffS) / (BulbSignal.FlightS - SetsOffS));

    /// <summary>Where the find's middle is at <paramref name="t"/>, from <paramref name="from"/> to <paramref name="to"/>, the key's middle.</summary>
    public static (double X, double Y) Find(double t, (double X, double Y) from, (double X, double Y) to) => Lerp(from, to, Along(t));

    /// <summary>
    /// Where the beam's far end is at <paramref name="t"/>: rising from the key's top <paramref name="keyTop"/> to the bulb, then riding the find down; null once
    /// it is back in the key.
    /// </summary>
    public static (double X, double Y)? Head(double t, (double X, double Y) from, (double X, double Y) to, (double X, double Y) keyTop)
    {
        var head = t < ReachS ? Lerp(keyTop, from, Ease(t / ReachS)) : Find(t, from, to);
        return head.Y < keyTop.Y - 1 ? head : null;
    }

    /// <summary>The find's size at <paramref name="t"/>.</summary>
    public static double Size(double t) => HatchSize + (LandSize - HatchSize) * Along(t);

    /// <summary>How far it has faded into the hologram's colour at <paramref name="t"/>, 0 to 1.</summary>
    public static double Faded(double t) => Math.Clamp((Along(t) - 0.25) / 0.6, 0, 1);

    /// <summary>The beam's flicker at <paramref name="t"/>, 0.85 to 1.</summary>
    public static double Flicker(double t) => 0.85 + 0.15 * BulbSignal.Noise(Math.Floor(t * 40) + 3);

    private static double Ease(double t)
    {
        t = Math.Clamp(t, 0, 1);
        return t * t * (3 - 2 * t);
    }

    private static (double X, double Y) Lerp((double X, double Y) a, (double X, double Y) b, double t) => (a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
}
