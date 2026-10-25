namespace Stillwake.Sim.Grid;

/// <summary>
/// Cells of one structure side by side along a row, one thing as long as it runs: a launch platform's pieces, one deck with one
/// lamp (its drop, to come). Read as <c>codeAt</c> gives the layer, the grid's or one drawn.
/// </summary>
public static class StructureRuns
{
    /// <summary>The run <paramref name="cell"/> lies in: its first cell and how many cells of <paramref name="cell"/>'s code run on along its row.</summary>
    public static (int First, int Length) RunOf(int cell, int width, Func<int, int> codeAt)
    {
        int code = codeAt(cell), x = cell % width, left = 0, right = 0;
        while (left < x && codeAt(cell - left - 1) == code) left++;
        while (x + right + 1 < width && codeAt(cell + right + 1) == code) right++;
        return (cell - left, left + right + 1);
    }
}
