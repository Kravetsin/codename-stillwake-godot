using System;

namespace Stillwake.Game.Shared;

/// <summary>
/// How high a store's sight glass shows its liquid (sight-glass): the glass of a tank, a cistern and a fuel tank alike, rows
/// <see cref="Top"/> to <see cref="Bottom"/> of their pictures, filled from the bottom by any level, to a hi-res pixel. No rule of
/// its own: the sim says what each holds.
/// </summary>
public static class SightGlass
{
    /// <summary>The glass's top and bottom, in the world's pixels down the machine's picture.</summary>
    public const float Top = 11, Bottom = 21;

    /// <summary>Hi-res pixels to a world's pixel (<c>HiRes.Scale</c>): the finest step the glass shows.</summary>
    public const int Fine = 4;

    /// <summary>The steps from empty to full.</summary>
    public const int Steps = (int)((Bottom - Top) * Fine);

    /// <summary>
    /// The height of liquid shown for <paramref name="amount"/> of <paramref name="capacity"/>, in the world's pixels: none at none,
    /// a step at least for any, rounded up, and the glass full only when the store is (one loaded fuller from an old save too).
    /// </summary>
    public static float Height(long amount, long capacity)
    {
        if (amount <= 0 || capacity <= 0) return 0;
        if (amount >= capacity) return Steps / (float)Fine;
        long steps = Math.Min(Steps - 1, (amount * Steps + capacity - 1) / capacity);
        return steps / (float)Fine;
    }
}
