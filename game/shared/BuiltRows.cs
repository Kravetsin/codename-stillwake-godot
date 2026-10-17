using System;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Shared;

/// <summary>
/// A view's copy of a grid layer compared with it and brought up to it only within a box, row by row: what
/// is built now and was at the last look (<see cref="IGridQueries.Built"/>). Past both every layer is empty,
/// in the grid and in a copy that started empty alike, so a big world costs what its ship does.
/// </summary>
public static class BuiltRows
{
    /// <summary>Whether <paramref name="layer"/> and <paramref name="copy"/>, of a grid <paramref name="width"/> wide, agree along row <paramref name="y"/> of <paramref name="box"/>.</summary>
    public static bool Same(ReadOnlySpan<int> layer, int[] copy, int width, Box box, int y) =>
        layer.Slice(y * width + box.Left, box.Width).SequenceEqual(copy.AsSpan(y * width + box.Left, box.Width));

    /// <summary>Whether <paramref name="layer"/> and <paramref name="copy"/> agree on every cell of <paramref name="box"/>.</summary>
    public static bool Same(ReadOnlySpan<int> layer, int[] copy, int width, Box box)
    {
        for (int y = box.Top; y <= box.Bottom; y++)
            if (!Same(layer, copy, width, box, y)) return false;
        return true;
    }

    /// <summary>Brings <paramref name="copy"/> up to <paramref name="layer"/> on every cell of <paramref name="box"/>.</summary>
    public static void Copy(ReadOnlySpan<int> layer, int[] copy, int width, Box box)
    {
        for (int y = box.Top; y <= box.Bottom; y++)
            layer.Slice(y * width + box.Left, box.Width).CopyTo(copy.AsSpan(y * width + box.Left, box.Width));
    }
}
