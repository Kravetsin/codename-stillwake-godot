using System;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Machines = Stillwake.Content.Machines;

namespace Stillwake.Game.Render;

/// <summary>
/// Where a machine's picture, built or planned, stands over the cells above its footprint (the arc bench's vise, the
/// workbench's board, 0099): the pointer there means that machine, as the eye reads it.
/// </summary>
public static class Risen
{
    /// <summary>
    /// The cell of the footprint's top row under <paramref name="cell"/> of the nearest machine or machine plan below it in its
    /// column whose picture rises by <paramref name="riseOf"/> as far as <paramref name="cell"/>; null with none.
    /// </summary>
    public static int? Under(IGridQueries grid, BuildingModule building, int cell, Func<string, int> riseOf)
    {
        int width = grid.Width, x = cell % width, y = cell / width, best = int.MaxValue;
        for (int c = cell + width; c < width * grid.Height; c += width)
            if (grid.MachineAt(c) is { } machine)
            {
                if (riseOf(machine.Kind) >= c / width - y) best = c / width;
                break;
            }
        foreach (var plan in building.Plans)
        {
            if (plan.Place is not { Layer: "machine", Kind: var kind } || !Machines.TryGet(kind, out var spec)) continue;
            int left = plan.Cell % width, top = plan.Cell / width;
            if (x >= left && x < left + spec.Width && top > y && top < best && riseOf(kind) >= top - y) best = top;
        }
        return best == int.MaxValue ? null : best * width + x;
    }

    /// <summary>
    /// <paramref name="point"/>, in cells, moved down into the footprint's top row of the machine or plan whose picture rises over it
    /// (<see cref="Under"/>), when nothing is built or planned in its own cell; else as it is.
    /// </summary>
    public static (double X, double Y) Over(IGridQueries grid, BuildingModule building, (double X, double Y) point, Func<string, int> riseOf)
    {
        int x = (int)Math.Floor(point.X), y = (int)Math.Floor(point.Y);
        if (x < 0 || y < 0 || x >= grid.Width || y >= grid.Height) return point;
        int cell = y * grid.Width + x;
        if (grid.MachineAt(cell) != null || building.PlansAt(cell).Count > 0 || Under(grid, building, cell, riseOf) is not int top) return point;
        return (point.X, top / grid.Width + 0.5);
    }
}
