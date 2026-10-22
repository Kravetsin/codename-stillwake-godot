using System;

namespace Stillwake.Game.Features.Power;

/// <summary>
/// How a capacitor bank's gauge reads (0101), plain C# so it is tested: six segments lit from the foot as far as the bank
/// is full, and a spark running up them while power goes in, down while it comes out.
/// </summary>
public static class StoreGauge
{
    public const int Segments = 6;

    /// <summary>How long a spark takes to run the gauge's length, in seconds.</summary>
    public const double SparkS = 0.9;

    /// <summary>How lit the lowest segment is at the least, while the bank holds anything: never dark until empty.</summary>
    public const float Least = 0.15f;

    /// <summary>
    /// How lit segment <paramref name="segment"/> (0 the lowest) is, 0 to 1, for a store holding <paramref name="energy"/> of
    /// <paramref name="capacity"/>: each a sixth of full, lit whole below how full it is and the one it reaches by how far
    /// into it.
    /// </summary>
    public static float Lit(int segment, int energy, int capacity)
    {
        if (capacity <= 0 || energy <= 0) return 0;
        float filled = (float)energy * Segments / capacity - segment;
        return segment == 0 ? Math.Clamp(filled, Least, 1) : Math.Clamp(filled, 0, 1);
    }

    /// <summary>
    /// How strongly the spark lights segment <paramref name="segment"/> at <paramref name="seconds"/>, 0 to 1, by which way
    /// power flows (<paramref name="flow"/>): up the gauge while it goes in, down while it comes out, nowhere while it holds.
    /// </summary>
    public static float SparkAt(int segment, int flow, double seconds)
    {
        if (flow == 0) return 0;
        double along = seconds / SparkS % 1 * (Segments + 1) - 0.5;
        double at = flow > 0 ? along : Segments - 1 - along;
        return (float)Math.Max(0, 1 - Math.Abs(at - segment));
    }
}
