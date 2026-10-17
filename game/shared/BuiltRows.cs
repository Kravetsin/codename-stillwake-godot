using System;
using Stillwake.Sim;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Shared;

/// <summary>
/// A view's copy of a grid layer compared with it row by row within a box: what is built now and was at the
/// last look (<see cref="IGridQueries.Built"/>). Past both every layer is empty, in the grid and in a copy
/// that started empty alike, so a big world costs what its ship does. For a view's first look, or when the
/// grid's log no longer reaches back to its last (0051).
/// </summary>
public static class BuiltRows
{
    /// <summary>Whether <paramref name="layer"/> and <paramref name="copy"/>, of a grid <paramref name="width"/> wide, agree along row <paramref name="y"/> of <paramref name="box"/>.</summary>
    public static bool Same(ReadOnlySpan<int> layer, int[] copy, int width, Box box, int y) =>
        layer.Slice(y * width + box.Left, box.Width).SequenceEqual(copy.AsSpan(y * width + box.Left, box.Width));
}
