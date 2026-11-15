using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Sky;

/// <summary>
/// The rime on the hull before the ice belt (the pick «Кромка инея»): on every wall cell facing space, a band grows inward from the
/// edge facing it, each cell's turn by <see cref="Delay"/>: the outer corners first, then a few cold spots, the rest later the
/// farther they lie along the hull from either. It holds while the belt runs and melts back toward the edge after, the last to
/// freeze melting first. Drawn only; the sim knows nothing of it.
/// </summary>
public static class Frost
{
    /// <summary>A side of a cell, as the walls' edges number them.</summary>
    public const int Up = 1, Right = 2, Down = 4, Left = 8;

    /// <summary>The latest a cell's turn comes, in shares of the warning; how long a part takes to grow or melt whole.</summary>
    public const float Latest = 0.6f, Span = 0.32f;

    /// <summary>How much later a cell's turn comes for each cell along the hull from a corner or cold spot.</summary>
    public const float PerCell = 0.05f;

    /// <summary>
    /// How deep the band reaches into its cell at its full, in pixels of the world: its inner edge scalloped between these, a point
    /// each <see cref="Scallop"/> pixels along the hull, as the pick's; an outer corner's disc this wide.
    /// </summary>
    public const float Shallowest = 2.0f, Deepest = 3.4f, Scallop = 4, Corner = 3.4f;

    /// <summary>How far the rime stands proud of the wall's edge, out into space, scalloped between these as its inner edge is: a crust, not a stain.</summary>
    public const float ProudLeast = 0.5f, ProudMost = 1.4f;

    /// <summary>Where a hull cell's inner corners (<see cref="HollowsOf"/>) sit above its sides in what <see cref="HullCells"/> keeps.</summary>
    public const int HollowShift = 4;

    /// <summary>One cell in about this many is a cold spot frost takes early, its turn <see cref="ColdFrom"/> and a little more.</summary>
    public const int ColdEvery = 29;

    public const float ColdFrom = 0.2f;

    /// <summary>
    /// The sides of the cell (<paramref name="x"/>, <paramref name="y"/>) of a grid <paramref name="width"/> by <paramref name="height"/>
    /// facing space: a wall's sides whose neighbour <paramref name="space"/> calls space, the grid's edge too; none for a cell no wall.
    /// </summary>
    public static int SidesOf(int x, int y, int width, int height, Func<int, bool> wall, Func<int, bool> space)
    {
        int cell = y * width + x;
        if (!wall(cell)) return 0;
        int sides = 0;
        if (y == 0 || space(cell - width)) sides |= Up;
        if (x == width - 1 || space(cell + 1)) sides |= Right;
        if (y == height - 1 || space(cell + width)) sides |= Down;
        if (x == 0 || space(cell - 1)) sides |= Left;
        return sides;
    }

    /// <summary>
    /// The inner corners of the wall cell (<paramref name="x"/>, <paramref name="y"/>): each where its two sides are walled but the cell
    /// between them, diagonal, is space, so the bands along both walls turn there. A bit each, from 0 at the top right clockwise, as a
    /// pair of sides counts them in the shader: up and right, right and down, down and left, left and up.
    /// </summary>
    public static int HollowsOf(int x, int y, int width, int height, Func<int, bool> wall, Func<int, bool> space)
    {
        int cell = y * width + x;
        if (!wall(cell) || x == 0 || y == 0 || x == width - 1 || y == height - 1) return 0;
        bool Walled(int at) => !space(at);
        int hollows = 0;
        (int Dx, int Dy)[] corners = [(1, -1), (1, 1), (-1, 1), (-1, -1)];
        for (int i = 0; i < 4; i++)
        {
            var (dx, dy) = corners[i];
            if (Walled(cell + dx) && Walled(cell + dy * width) && space(cell + dy * width + dx)) hollows |= 1 << i;
        }
        return hollows;
    }

    /// <summary>Whether <paramref name="sides"/> meet at an outer corner: two that turn round it.</summary>
    public static bool Cornered(int sides) =>
        (sides & (Up | Right)) == (Up | Right) || (sides & (Right | Down)) == (Right | Down)
        || (sides & (Down | Left)) == (Down | Left) || (sides & (Left | Up)) == (Left | Up);

    /// <summary>
    /// Each cell of <paramref name="hull"/> (cell to its sides facing space) of a grid <paramref name="width"/> wide, by when its turn
    /// comes: an outer corner's at once, a cold spot's at <see cref="ColdFrom"/> and a little more, every other a step of
    /// <see cref="PerCell"/> later for each cell along the hull from the nearest of them, at most <see cref="Latest"/>.
    /// </summary>
    public static Dictionary<int, float> Delays(IReadOnlyDictionary<int, int> hull, int width)
    {
        var delays = new Dictionary<int, float>(hull.Count);
        var queue = new PriorityQueue<int, float>();
        foreach (var (cell, sides) in hull)
        {
            uint hash = Hash(cell);
            float seed = Cornered(sides) ? (hash % 4) * 0.02f : hash % ColdEvery == 0 ? ColdFrom + (hash / ColdEvery % 6) * 0.04f : Latest;
            delays[cell] = seed;
            if (seed < Latest) queue.Enqueue(cell, seed);
        }
        // Out from the corners and cold spots along the hull, a cell's eight neighbours its next: round an inner corner too.
        while (queue.TryDequeue(out int cell, out float at))
        {
            if (at > delays[cell]) continue;
            int x = cell % width;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if ((dx | dy) == 0 || x + dx < 0 || x + dx >= width) continue;
                    int next = cell + dy * width + dx;
                    float later = at + PerCell;
                    if (!delays.TryGetValue(next, out float now) || later >= now) continue;
                    delays[next] = later;
                    queue.Enqueue(next, later);
                }
        }
        return delays;
    }

    /// <summary>
    /// How much of a part whose turn comes at <paramref name="delay"/> is there, 0 to 1, with the warning <paramref name="grow"/> of the
    /// way run and <paramref name="melt"/> of the thaw after (both 0 to 1): grown over <see cref="Span"/> from its turn, melted over as
    /// much, the last to freeze first.
    /// </summary>
    public static float Amount(float delay, float grow, float melt) =>
        Clamp((grow - delay) / Span) * (1 - Clamp((melt - (Latest - delay)) / Span));

    /// <summary>How dark the frost has gone as it melts, 0 to 1 of its darkest, and how strongly the wet it leaves shows (the pick's).</summary>
    public static (float Dark, float Wet) Thawing(double after) =>
        ((float)Smooth(0, 0.4, after), (float)(0.38 * Smooth(0, 0.27, after) * (1 - Smooth(0.91, StretchLooks.Dry, after))));

    /// <summary>Whether any frost or wet shows: growing, held, melting or drying.</summary>
    public static bool Shows(float grow, double after) => grow > 0 && after < StretchLooks.Dry;

    private static float Clamp(float v) => Math.Clamp(v, 0, 1);

    private static double Smooth(double from, double to, double x)
    {
        double t = Math.Clamp((x - from) / (to - from), 0, 1);
        return t * t * (3 - 2 * t);
    }

    /// <summary>A cell's own number, the same on every peer: which are cold spots.</summary>
    public static uint Hash(int cell)
    {
        uint h = (uint)cell * 0x9E3779B1u;
        h ^= h >> 15;
        h *= 0x85EBCA77u;
        return h ^ (h >> 13);
    }
}
