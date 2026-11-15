using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Sky;

/// <summary>
/// The wall cells of a grid <paramref name="width"/> by <paramref name="height"/> facing space, and which sides (<see cref="Frost.SidesOf"/>):
/// found once over what is built as a surface loads, then kept by the cells a change names and their neighbours, never walked whole again.
/// </summary>
public sealed class HullCells(int width, int height, Func<int, bool> wall, Func<int, bool> space)
{
    private readonly Dictionary<int, int> _sides = new();

    /// <summary>Each wall cell facing space, by its sides facing it.</summary>
    public IReadOnlyDictionary<int, int> Sides => _sides;

    /// <summary>Bumped whenever a cell joins, leaves or changes its sides.</summary>
    public int Version { get; private set; }

    /// <summary>Every cell from (<paramref name="left"/>, <paramref name="top"/>) to (<paramref name="right"/>, <paramref name="bottom"/>) looked at anew.</summary>
    public void Scan(int left, int top, int right, int bottom)
    {
        for (int y = Math.Max(0, top); y <= Math.Min(height - 1, bottom); y++)
            for (int x = Math.Max(0, left); x <= Math.Min(width - 1, right); x++) Look(x, y);
    }

    /// <summary>A cell that changed: it and its four neighbours, whose sides it may open or close.</summary>
    public void Changed(int cell)
    {
        int x = cell % width, y = cell / width;
        Look(x, y);
        if (y > 0) Look(x, y - 1);
        if (x < width - 1) Look(x + 1, y);
        if (y < height - 1) Look(x, y + 1);
        if (x > 0) Look(x - 1, y);
    }

    private void Look(int x, int y)
    {
        int cell = y * width + x, sides = Frost.SidesOf(x, y, width, height, wall, space);
        _sides.TryGetValue(cell, out int was);
        if (sides == was) return;
        if (sides == 0) _sides.Remove(cell);
        else _sides[cell] = sides;
        Version++;
    }
}
