using System;

namespace Stillwake.Game.Features.Backdrop;

/// <summary>
/// Where the land behind a planet's ground has its ground line (composed-backdrops): the surface's top row averaged over
/// <see cref="BackdropLooks.Recipe.Reach"/> columns either side of the camera, wrapping at the world's width, so in a valley the
/// far land comes down with the hero. The rows are read once when the surface is shown: digging does not move the horizon.
/// </summary>
public static class GroundLine
{
    /// <summary>Each column's surface row: its first cell from the top that is not open sky, a wall or a back wall; the grid's height where none is.</summary>
    public static int[] Tops(ReadOnlySpan<int> structure, ReadOnlySpan<int> back, int width, int height)
    {
        var tops = new int[width];
        for (int x = 0; x < width; x++)
        {
            int y = 0;
            while (y < height && structure[y * width + x] == 0 && back[y * width + x] == 0) y++;
            tops[x] = y;
        }
        return tops;
    }

    /// <summary>The mean of <paramref name="tops"/> over <paramref name="reach"/> columns either side of <paramref name="column"/>, any column, wrapping.</summary>
    public static double Mean(int[] tops, double column, int reach)
    {
        int n = tops.Length, middle = (int)Math.Floor(column);
        long sum = 0;
        for (int x = middle - reach; x <= middle + reach; x++) sum += tops[((x % n) + n) % n];
        return (double)sum / (2 * reach + 1);
    }
}
