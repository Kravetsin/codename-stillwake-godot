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

/// <summary>
/// A tool's aim drawn as one glow along the surface (the fan of rays): each face's light running into the rock from its edge,
/// its ends that meet no other face fading out, and the rock in each inner corner lit too, where two faces of different cells
/// meet, so the corner is no hole in the glow.
/// </summary>
public static class FaceGlow
{
    /// <summary>The two ends of <paramref name="face"/>, in cells: its start, then its end, left to right or top to bottom.</summary>
    public static ((int X, int Y) From, (int X, int Y) To) EndsOf(Face face) => face.Side switch
    {
        Side.Left => ((face.X, face.Y), (face.X, face.Y + 1)),
        Side.Right => ((face.X + 1, face.Y), (face.X + 1, face.Y + 1)),
        Side.Top => ((face.X, face.Y), (face.X + 1, face.Y)),
        _ => ((face.X, face.Y + 1), (face.X + 1, face.Y + 1)),
    };

    /// <summary>Which way <paramref name="side"/>'s light runs, into its own cell, a step a cell.</summary>
    public static (int X, int Y) Inward(Side side) => side switch
    {
        Side.Left => (1, 0),
        Side.Right => (-1, 0),
        Side.Top => (0, 1),
        _ => (0, -1),
    };

    /// <summary>Whether each face's start and end meet another face of <paramref name="faces"/>, in its order: an end that does not fades.</summary>
    public static List<(bool From, bool To)> Joined(IReadOnlyList<Face> faces)
    {
        var ends = new Dictionary<(int, int), int>();
        foreach (var f in faces)
        {
            var (from, to) = EndsOf(f);
            ends[from] = ends.GetValueOrDefault(from) + 1;
            ends[to] = ends.GetValueOrDefault(to) + 1;
        }
        var joined = new List<(bool, bool)>(faces.Count);
        foreach (var f in faces)
        {
            var (from, to) = EndsOf(f);
            joined.Add((ends[from] > 1, ends[to] > 1));
        }
        return joined;
    }

    /// <summary>
    /// The inner corners of <paramref name="faces"/>: a point, in cells, where a face of one cell meets a face of another at a
    /// right angle, and which way the rock behind both lies from it, the two faces' light together, in order of the faces.
    /// </summary>
    public static List<((int X, int Y) At, (int X, int Y) Into)> Corners(IReadOnlyList<Face> faces)
    {
        var corners = new List<((int, int), (int, int))>();
        for (int i = 0; i < faces.Count; i++)
            for (int j = i + 1; j < faces.Count; j++)
            {
                Face a = faces[i], b = faces[j];
                bool across = (a.Side is Side.Left or Side.Right) != (b.Side is Side.Left or Side.Right);
                if (!across || (a.X == b.X && a.Y == b.Y)) continue;
                var (af, at) = EndsOf(a);
                var (bf, bt) = EndsOf(b);
                (int, int)? shared = af == bf || af == bt ? af : at == bf || at == bt ? at : null;
                if (shared is not { } point) continue;
                var (ia, ib) = (Inward(a.Side), Inward(b.Side));
                corners.Add((point, (ia.X + ib.X, ia.Y + ib.Y)));
            }
        return corners;
    }
}
