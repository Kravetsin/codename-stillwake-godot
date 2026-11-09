using System;
using System.Collections.Generic;
using Stillwake.Content;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// What a catcher's cut sounds like (tracker-catchers): it keeps the walls round its body each look, and at a new cut names the first
/// cell gone since the last look, broken, or else the first still standing, bitten. The sim keeps only when it cut.
/// </summary>
public sealed class CutHeard
{
    private readonly Dictionary<int, Material> _around = [];
    private readonly List<int> _order = [];

    /// <summary>
    /// The walls round the box <paramref name="left"/>, <paramref name="top"/>, <paramref name="width"/> by <paramref name="height"/>
    /// cells and one cell out, read by <paramref name="wallAt"/>; when <paramref name="cut"/>, what was heard: the first of last
    /// look's cells in row order now gone, broken, else the first standing, bitten; null when it did not cut or nothing stood there.
    /// </summary>
    public (int Cell, Material Material, bool Broke)? Look(int left, int top, int width, int height, int gridWidth, int gridHeight,
        Func<int, Material?> wallAt, bool cut)
    {
        (int Cell, Material Material, bool Broke)? heard = null;
        if (cut)
        {
            foreach (int cell in _order)
                if (wallAt(cell) == null)
                {
                    heard = (cell, _around[cell], true);
                    break;
                }
            if (heard == null)
                foreach (int cell in _order)
                    if (wallAt(cell) is { } standing)
                    {
                        heard = (cell, standing, false);
                        break;
                    }
        }
        _around.Clear();
        _order.Clear();
        for (int row = Math.Max(0, top - 1); row <= Math.Min(gridHeight - 1, top + height); row++)
            for (int column = Math.Max(0, left - 1); column <= Math.Min(gridWidth - 1, left + width); column++)
            {
                int cell = row * gridWidth + column;
                if (wallAt(cell) is not { } material) continue;
                _around[cell] = material;
                _order.Add(cell);
            }
        return heard;
    }
}
