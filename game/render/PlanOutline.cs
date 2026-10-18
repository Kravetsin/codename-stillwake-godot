using System.Collections.Generic;

namespace Stillwake.Game.Render;

/// <summary>A stretch of an outline, in pixels of the world: <see cref="Length"/> pixels from (X, Y), one step (Dx, Dy) at a time.</summary>
public readonly record struct OutlineRun(int X, int Y, int Length, int Dx, int Dy);

/// <summary>
/// The dashed line round each group of plans (0022's plan look): a pixel inside the edge of every cell
/// side whose neighbour is not of its group, so a row of walls is one box, not a box a cell. Worked out
/// from the cells, never from the pictures, whose atlas has no room around them. Each run goes clockwise
/// round its group, so the dashes march round it.
/// </summary>
public static class PlanOutline
{
    /// <summary>
    /// The runs round <paramref name="cells"/>, each a cell of a plan with its group and how tall it is drawn
    /// from the cell's top, in pixels (a floor its strip, the rest a whole <paramref name="size"/>): one cut
    /// short joins its group only side by side.
    /// </summary>
    public static List<OutlineRun> Of(IEnumerable<(int Cell, string Group, int Height)> cells, int width, int size)
    {
        var at = new Dictionary<int, (string Group, int Height)>();
        foreach (var (cell, group, height) in cells) at[cell] = (group, height);
        var runs = new List<OutlineRun>();
        foreach (var (cell, (group, h)) in at)
        {
            int x = cell % width, X = x * size, Y = cell / width * size;
            bool Same(int other, bool above) =>
                at.TryGetValue(other, out var o) && o.Group == group && o.Height == h && (!above || h == size);
            if (!Same(cell - width, above: true)) runs.Add(new(X, Y, size, 1, 0));
            if (x == width - 1 || !Same(cell + 1, above: false)) runs.Add(new(X + size - 1, Y, h, 0, 1));
            if (!Same(cell + width, above: true)) runs.Add(new(X + size - 1, Y + h - 1, size, -1, 0));
            if (x == 0 || !Same(cell - 1, above: false)) runs.Add(new(X, Y + h - 1, h, 0, -1));
        }
        return runs;
    }

    /// <summary>
    /// The dashes of <paramref name="run"/> as rectangles (x, y, w, h): <paramref name="dash"/> pixels on in every
    /// <paramref name="period"/>, the pattern moved <paramref name="offset"/> pixels along the run's way, so it marches.
    /// </summary>
    public static IEnumerable<(int X, int Y, int W, int H)> Dashes(OutlineRun run, double offset, int dash, int period)
    {
        int shift = (int)System.Math.Floor(offset);
        int from = -1;
        for (int s = 0; s <= run.Length; s++)
        {
            bool on = false;
            if (s < run.Length)
            {
                // How far along the run's way this pixel lies, the same for every run on its line.
                int u = run.Dx != 0 ? run.Dx * (run.X + run.Dx * s) : run.Dy * (run.Y + run.Dy * s);
                on = Mod(u - shift, period) < dash;
            }
            if (on && from < 0) from = s;
            if (on || from < 0) continue;
            int x1 = run.X + run.Dx * from, y1 = run.Y + run.Dy * from, x2 = run.X + run.Dx * (s - 1), y2 = run.Y + run.Dy * (s - 1);
            yield return (System.Math.Min(x1, x2), System.Math.Min(y1, y2), System.Math.Abs(x2 - x1) + 1, System.Math.Abs(y2 - y1) + 1);
            from = -1;
        }
    }

    private static int Mod(int a, int n) => (a % n + n) % n;
}
