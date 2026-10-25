using System.Collections.Generic;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Crew;

/// <summary>A side of a cell: which of its four edges.</summary>
public enum Side { Left, Right, Top, Bottom }

/// <summary>An edge of cell (<see cref="X"/>, <see cref="Y"/>) a tool's aim glows along, its rim drawn just inside it.</summary>
public readonly record struct Face(int X, int Y, Side Side);

/// <summary>Where a tool's aim glows (the fan of rays): the edges of what its hit would strike that the player sees.</summary>
public static class FanFaces
{
    /// <summary>
    /// The faces of what <paramref name="hewn"/> strikes: of a wall, each side open to the air (no wall, nothing that stops a
    /// body) and turned toward <paramref name="hand"/>, in thousandths of a cell; of a back wall or a plant, the edge of the
    /// ground they cover together, its sides that meet no other of them. In the order the sim lists them, a cell's sides left,
    /// right, top, bottom.
    /// </summary>
    public static List<Face> Of(IGridQueries grid, Hewn hewn, Point hand, bool back)
    {
        var faces = new List<Face>();
        int width = grid.Width;
        bool Open(int x, int y) => x >= 0 && y >= 0 && x < width && y < grid.Height && grid.WallAt(y * width + x) == null && !grid.Blocks(y * width + x);
        var covered = new HashSet<int>();
        if (back) covered.UnionWith(hewn.Cells);
        foreach (var plant in hewn.Plants) covered.Add(plant.Cell);
        bool Edge(int x, int y) => x < 0 || y < 0 || x >= width || !covered.Contains(y * width + x);
        void Around(int cell)
        {
            int x = cell % width, y = cell / width;
            if (Edge(x - 1, y)) faces.Add(new Face(x, y, Side.Left));
            if (Edge(x + 1, y)) faces.Add(new Face(x, y, Side.Right));
            if (Edge(x, y - 1)) faces.Add(new Face(x, y, Side.Top));
            if (Edge(x, y + 1)) faces.Add(new Face(x, y, Side.Bottom));
        }
        foreach (int cell in hewn.Cells)
        {
            if (back)
            {
                Around(cell);
                continue;
            }
            int x = cell % width, y = cell / width;
            if (hand.X < x * Hero.Unit && Open(x - 1, y)) faces.Add(new Face(x, y, Side.Left));
            if (hand.X > (x + 1) * Hero.Unit && Open(x + 1, y)) faces.Add(new Face(x, y, Side.Right));
            if (hand.Y < y * Hero.Unit && Open(x, y - 1)) faces.Add(new Face(x, y, Side.Top));
            if (hand.Y > (y + 1) * Hero.Unit && Open(x, y + 1)) faces.Add(new Face(x, y, Side.Bottom));
        }
        foreach (var plant in hewn.Plants) Around(plant.Cell);
        return faces;
    }
}
