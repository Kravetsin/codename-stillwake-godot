using System;

namespace Stillwake.Game.Shared;

/// <summary>
/// How high a store's sight glass shows its liquid (sight-glass): filled from its bottom by any level, to a hi-res pixel, the glass
/// wherever its picture draws it (a tank's, a cistern's, the shuttle's gauge). No rule of its own: the sim says what each holds.
/// </summary>
public static class SightGlass
{
    /// <summary>Hi-res pixels to a world's pixel (<c>HiRes.Scale</c>): the finest step a glass shows.</summary>
    public const int Fine = 4;

    /// <summary>
    /// The height of liquid shown for <paramref name="amount"/> of <paramref name="capacity"/> in a glass <paramref name="glass"/> of
    /// the world's pixels tall: none at none, a step at least for any, rounded up, and full only when the store is (one loaded
    /// fuller from an old save too).
    /// </summary>
    public static float Height(long amount, long capacity, float glass)
    {
        int steps = (int)Math.Round(glass * Fine);
        if (amount <= 0 || capacity <= 0 || steps <= 0) return 0;
        if (amount >= capacity) return steps / (float)Fine;
        long shown = Math.Min(steps - 1, (amount * steps + capacity - 1) / capacity);
        return shown / (float)Fine;
    }
}
