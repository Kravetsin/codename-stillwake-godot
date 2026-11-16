using System;

namespace Stillwake.Game.Features.Sky;

/// <summary>
/// What a weather blows across the open sky in view (docs/plans/surface-storms.md), as the pick page drew it, laid out for the
/// streaks' shader (<c>StormView</c>): the world cut into slanting cells along the fall, each holding at most one streak, so the
/// pick's count over its view stays the count over any view, and nothing is laid on the CPU a frame; and the ground line the grit
/// thins from. Plain C#.
/// </summary>
public static class Streaks
{
    /// <summary>The pick's view and the box past it its streaks wrapped round, in pixels of the world: what a weather's count filled.</summary>
    public const float Wide = StretchLooks.Weather.BoxWide, Tall = StretchLooks.Weather.BoxTall;

    /// <summary>How high over the ground low streaks reach, thinning to none: the pick's ground halfway down its view.</summary>
    public const float LowReach = 70;

    /// <summary>
    /// The cells <paramref name="weather"/>'s streaks fall in, in pixels of the world: <c>Along</c> the fall, a pixel longer than its
    /// longest streak, so a streak reaches back into one cell at most; <c>Across</c> it, as wide as gives a cell to each of the
    /// pick's count over its box.
    /// </summary>
    public static (float Along, float Across) Cells(StretchLooks.Weather weather)
    {
        float along = MathF.Ceiling(weather.Streaks.Length.Most) + 1;
        return (along, Wide * Tall / weather.Count / along);
    }

    /// <summary>
    /// Seconds the streaks' shader is given: <paramref name="seconds"/> taken within <see cref="Period"/>, so a world hours old keeps
    /// its streaks' flight precise in a float; they jump once a period.
    /// </summary>
    public static float Flown(double seconds) => (float)(seconds % Period);

    /// <summary>Seconds after which the streaks' clock starts over: long beside a storm.</summary>
    public const double Period = 1000;

    /// <summary>Columns either side a smoothed ground line (<see cref="Smoothed"/>) is averaged over: wide, so it never steps with a ledge.</summary>
    public const int GroundReach = 24;

    /// <summary>
    /// <paramref name="tops"/>, each column's top row, averaged over <paramref name="reach"/> columns either side, wrapping, in pixels
    /// of the world, a column <paramref name="cell"/> pixels: the line the grit thins from and a hero's depth under the weather is
    /// read from. Once, as a surface loads.
    /// </summary>
    public static float[] Smoothed(int[] tops, int reach, int cell)
    {
        int n = tops.Length;
        var line = new float[n];
        long sum = 0;
        for (int k = -reach; k <= reach; k++) sum += tops[((k % n) + n) % n];
        for (int x = 0; x < n; x++)
        {
            line[x] = (float)sum / (2 * reach + 1) * cell;
            sum += tops[((x + reach + 1) % n + n) % n] - tops[((x - reach) % n + n) % n];
        }
        return line;
    }

    /// <summary>The smoothed <paramref name="line"/> at <paramref name="x"/> pixels across, between its columns' middles, wrapping.</summary>
    public static float GroundAt(float[] line, float x, int cell)
    {
        int n = line.Length;
        float at = x / cell - 0.5f;
        int left = (int)MathF.Floor(at);
        float k = at - left;
        float a = line[((left % n) + n) % n], b = line[((left + 1) % n + n) % n];
        return a + (b - a) * k;
    }
}
