using System.Collections.Generic;

namespace Stillwake.Game.Shared;

/// <summary>A segmented gauge's cells across its meter (0081): each one's left edge from the meter's and its width.</summary>
public static class MeterCells
{
    /// <summary>
    /// <paramref name="count"/> cells filling <paramref name="width"/> exactly, <paramref name="gap"/> apart and, after every
    /// <paramref name="per"/> of them (0 for none), <paramref name="groupGap"/>; the pixels left over spread one a cell.
    /// </summary>
    public static IReadOnlyList<(int X, int Width)> Lay(int width, int count, int per, int gap, int groupGap)
    {
        int groups = per > 0 ? (count + per - 1) / per : 1;
        int free = width - (count - groups) * gap - (groups - 1) * groupGap;
        var cells = new List<(int, int)>(count);
        int x = 0;
        for (int i = 0; i < count; i++)
        {
            int w = (i + 1) * free / count - i * free / count;
            cells.Add((x, w));
            x += w + (per > 0 && (i + 1) % per == 0 ? groupGap : gap);
        }
        return cells;
    }
}
