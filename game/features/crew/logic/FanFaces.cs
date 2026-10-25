using System;
using System.Collections.Generic;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// Where a tool's aim glints (the fan of rays): the faces its hit would strike, and its reach. A stretch from (<see cref="X0"/>,
/// <see cref="Y0"/>) to (<see cref="X1"/>, <see cref="Y1"/>), in cells; a cell's whole square when it is a back wall or a plant.
/// </summary>
public readonly record struct Glint(double X0, double Y0, double X1, double Y1);

/// <summary>What of a tool's fan is lit while it is aimed (the fan of rays), from what the sim says it would strike.</summary>
public static class FanFaces
{
    /// <summary>How far apart the drill's reach is dotted, in cells.</summary>
    public const double DotEvery = 0.6;

    /// <summary>
    /// The faces <paramref name="hewn"/>'s walls would be struck on: each side of a wall cell open to the air (no wall, nothing
    /// that stops a body) and turned toward <paramref name="hand"/>, in thousandths of a cell; a back wall's and a plant's own
    /// cell whole. In the order the sim lists them, a cell's sides left, right, top, bottom.
    /// </summary>
    public static List<Glint> Of(IGridQueries grid, Hewn hewn, Point hand, bool back)
    {
        var glints = new List<Glint>();
        int width = grid.Width;
        foreach (int cell in hewn.Cells)
        {
            int x = cell % width, y = cell / width;
            if (back)
            {
                glints.Add(new Glint(x, y, x + 1, y + 1));
                continue;
            }
            bool Open(int nx, int ny) => nx >= 0 && ny >= 0 && nx < width && ny < grid.Height
                && grid.WallAt(ny * width + nx) == null && !grid.Blocks(ny * width + nx);
            if (hand.X < x * Hero.Unit && Open(x - 1, y)) glints.Add(new Glint(x, y, x, y + 1));
            if (hand.X > (x + 1) * Hero.Unit && Open(x + 1, y)) glints.Add(new Glint(x + 1, y, x + 1, y + 1));
            if (hand.Y < y * Hero.Unit && Open(x, y - 1)) glints.Add(new Glint(x, y, x + 1, y));
            if (hand.Y > (y + 1) * Hero.Unit && Open(x, y + 1)) glints.Add(new Glint(x, y + 1, x + 1, y + 1));
        }
        foreach (var plant in hewn.Plants)
            glints.Add(new Glint(plant.Cell % width, plant.Cell / width, plant.Cell % width + 1, plant.Cell / width + 1));
        return glints;
    }

    /// <summary>
    /// The edge of <paramref name="weapon"/>'s reach from <paramref name="hand"/> aimed at <paramref name="heading"/>, in cells: the
    /// arc its range sweeps over its fan, from one edge to the other, a point at the turn <paramref name="along"/> of the way.
    /// </summary>
    public static (double X, double Y) ArcAt(Spec.Weapon weapon, Point hand, int heading, double along)
    {
        double angle = (heading + weapon.Arc * (along - 0.5)) * 2 * Math.PI / Spec.Headings.Turn;
        double range = weapon.Range / (double)Hero.Unit;
        return (hand.X / (double)Hero.Unit + Math.Cos(angle) * range, hand.Y / (double)Hero.Unit + Math.Sin(angle) * range);
    }

    /// <summary>The dots of the drill's reach from <paramref name="hand"/> along <paramref name="heading"/>, in cells, every <see cref="DotEvery"/> past the hand.</summary>
    public static List<(double X, double Y)> DotsOf(Spec.Weapon weapon, Point hand, int heading)
    {
        double angle = heading * 2 * Math.PI / Spec.Headings.Turn, range = weapon.Range / (double)Hero.Unit;
        var dots = new List<(double X, double Y)>();
        for (double d = DotEvery; d <= range + 1e-9; d += DotEvery)
            dots.Add((hand.X / (double)Hero.Unit + Math.Cos(angle) * d, hand.Y / (double)Hero.Unit + Math.Sin(angle) * d));
        return dots;
    }
}
