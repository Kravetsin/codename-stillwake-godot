using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Render;

/// <summary>
/// A unit step along the rim of a shape of pixels: from corner (X, Y), in pixels, one pixel toward (Dx, Dy), the shape
/// on its right. The steps round a shape go clockwise on the screen, those round a hole in it the other way.
/// </summary>
public readonly record struct Rim(int X, int Y, int Dx, int Dy);

/// <summary>
/// A straight side of an outline, in pixels of the world: from the corner (X, Y) of the silhouette's rim,
/// <see cref="Length"/> pixels one way (Dx, Dy), the silhouette on its right, <see cref="Along"/> pixels round its loop
/// from the loop's first corner; <see cref="Convex"/> when the loop turns right at its end, round a corner's outside.
/// </summary>
public readonly record struct OutlineRun(int X, int Y, int Length, int Dx, int Dy, int Along, bool Convex);

/// <summary>
/// A side's dashes as a strip of the world: <see cref="Length"/> from (X, Y) one way (Dx, Dy), <see cref="Width"/> wide
/// on its left, outside the silhouette, the strip's start <see cref="Along"/> round its loop.
/// </summary>
public readonly record struct DashStrip(double X, double Y, int Dx, int Dy, double Length, double Width, double Along);

/// <summary>
/// The dashed line round each group of plans and marks (0064): a strip just outside the silhouette of their pictures,
/// as wide as the line is drawn (<see cref="Strips"/>), going clockwise round it so the dashes march. Each picture's
/// rim is worked out from its alpha (<see cref="RimOf"/>); a group's rims are joined (<see cref="Merge"/>), a step two of
/// its shapes share dropping out, so a row of walls is one outline; then traced into straight sides (<see cref="Runs"/>).
/// Steps are kept as one number each while they are joined and traced.
/// </summary>
public static class PlanOutline
{
    /// <summary>The four ways a step goes, clockwise: a right turn is the next, a left turn the one before.</summary>
    private static readonly (int Dx, int Dy)[] Ways = [(1, 0), (0, 1), (-1, 0), (0, -1)];

    /// <summary>Where a loop looks next, in order: a right turn, straight on, a left turn.</summary>
    private static readonly int[] Turns = [1, 0, 3];

    /// <summary>
    /// The rim of the opaque pixels of a <paramref name="width"/> by <paramref name="height"/> mask, row by row: a step
    /// along every side of an opaque pixel facing a clear one or the mask's edge.
    /// </summary>
    public static List<Rim> RimOf(System.Func<int, int, bool> opaque, int width, int height)
    {
        bool At(int x, int y) => x >= 0 && y >= 0 && x < width && y < height && opaque(x, y);
        var rims = new List<Rim>();
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (!At(x, y)) continue;
                if (!At(x, y - 1)) rims.Add(new(x, y, 1, 0));
                if (!At(x + 1, y)) rims.Add(new(x + 1, y, 0, 1));
                if (!At(x, y + 1)) rims.Add(new(x + 1, y + 1, -1, 0));
                if (!At(x - 1, y)) rims.Add(new(x, y + 1, 0, -1));
            }
        return rims;
    }

    /// <summary>
    /// The rim of <paramref name="shapes"/> laid together, each moved to (X, Y): where two meet, the step one takes along
    /// their common side the other takes back, and both drop out. Shapes of one group never overlap: one a cell or a footprint.
    /// </summary>
    public static HashSet<Rim> Merge(IEnumerable<(int X, int Y, IReadOnlyList<Rim> Rims)> shapes) =>
        MergeKeys(shapes).Select(Unkey).ToHashSet();

    /// <summary>
    /// Each group's outline: its shapes merged (<see cref="Merge"/>) and traced (<see cref="Runs"/>); groups never join
    /// each other, so two machines side by side are two outlines.
    /// </summary>
    public static List<OutlineRun> Of(IEnumerable<(string Group, int X, int Y, IReadOnlyList<Rim> Rims)> shapes, int smallestHole)
    {
        var runs = new List<OutlineRun>();
        foreach (var group in shapes.GroupBy(s => s.Group))
            Trace(MergeKeys(group.Select(s => (s.X, s.Y, s.Rims))), smallestHole, runs);
        return runs;
    }

    /// <summary>
    /// The loops of <paramref name="rims"/>, each from its top-left, as the straight sides that follow on round it. A hole
    /// of fewer than <paramref name="smallestHole"/> pixels is not outlined: the gaps between a ladder's rungs, or a
    /// grille's, are part of its silhouette; a room walled round is not.
    /// </summary>
    public static List<OutlineRun> Runs(IReadOnlyCollection<Rim> rims, int smallestHole)
    {
        var runs = new List<OutlineRun>();
        Trace(rims.Select(Key).ToHashSet(), smallestHole, runs);
        return runs;
    }

    // A step as one number, its corner and its way, ordered by row, then column, then way.
    private const int Off = 1 << 20;
    private const long Columns = (1L << 21) - 1;

    private static long Key(int x, int y, int way) => ((long)(y + Off) << 23) | ((long)(x + Off) << 2) | (uint)way;

    private static long Key(Rim r) => Key(r.X, r.Y, WayOf(r.Dx, r.Dy));

    private static int WayOf(int dx, int dy) => dx == 1 ? 0 : dy == 1 ? 1 : dx == -1 ? 2 : 3;

    private static (int X, int Y, int Way) Parts(long key) => ((int)((key >> 2) & Columns) - Off, (int)(key >> 23) - Off, (int)(key & 3));

    private static Rim Unkey(long key)
    {
        var (x, y, way) = Parts(key);
        return new Rim(x, y, Ways[way].Dx, Ways[way].Dy);
    }

    /// <summary>Each shape's steps as numbers, and the steps that would cancel them, kept by the list: a key moves by adding to it.</summary>
    private static readonly Dictionary<IReadOnlyList<Rim>, (long[] Steps, long[] Back)> Keyed = new(ReferenceEqualityComparer.Instance);

    private static HashSet<long> MergeKeys(IEnumerable<(int X, int Y, IReadOnlyList<Rim> Rims)> shapes)
    {
        var rims = new HashSet<long>();
        foreach (var (ox, oy, shape) in shapes)
        {
            if (!Keyed.TryGetValue(shape, out var keyed))
            {
                keyed = (new long[shape.Count], new long[shape.Count]);
                for (int i = 0; i < shape.Count; i++)
                {
                    var r = shape[i];
                    int way = WayOf(r.Dx, r.Dy);
                    keyed.Steps[i] = Key(r.X, r.Y, way);
                    keyed.Back[i] = Key(r.X + r.Dx, r.Y + r.Dy, (way + 2) & 3);
                }
                Keyed[shape] = keyed;
            }
            long moved = ((long)oy << 23) + ((long)ox << 2);
            for (int i = 0; i < keyed.Steps.Length; i++)
                if (!rims.Remove(keyed.Back[i] + moved)) rims.Add(keyed.Steps[i] + moved);
        }
        return rims;
    }

    private static void Trace(HashSet<long> rims, int smallestHole, List<OutlineRun> runs)
    {
        var seen = new HashSet<long>();
        var loop = new List<long>();
        var sides = new List<(int X, int Y, int Way, int Length)>();
        foreach (long start in rims)
        {
            if (seen.Contains(start)) continue;
            Loop(start, rims, seen, loop);
            Sides(loop, sides);
            long area = Area(sides);
            if (area < 0 && -area < smallestHole) continue;
            Add(sides, runs);
        }
    }

    /// <summary>
    /// The loop of rims through <paramref name="start"/>: where two loops touch at a corner it turns right first, so
    /// it keeps to its own shape, two pixels touching only at a corner two loops.
    /// </summary>
    private static void Loop(long start, HashSet<long> rims, HashSet<long> seen, List<long> loop)
    {
        loop.Clear();
        long at = start;
        while (true)
        {
            loop.Add(at);
            seen.Add(at);
            var (x, y, way) = Parts(at);
            int ex = x + Ways[way].Dx, ey = y + Ways[way].Dy;
            long next = -1;
            foreach (int turn in Turns)
            {
                long candidate = Key(ex, ey, (way + turn) & 3);
                if (candidate == start || (rims.Contains(candidate) && !seen.Contains(candidate)))
                {
                    next = candidate;
                    break;
                }
            }
            if (next < 0 || next == start) return;
            at = next;
        }
    }

    /// <summary>A loop as its straight sides from its top-left corner, each from a corner with its way and length.</summary>
    private static void Sides(List<long> loop, List<(int X, int Y, int Way, int Length)> sides)
    {
        sides.Clear();
        // Begun at its least step, a loop's dashes keep their place as plans come and go elsewhere.
        int first = 0;
        for (int i = 1; i < loop.Count; i++)
            if (loop[i] < loop[first]) first = i;
        while ((loop[(first + loop.Count - 1) % loop.Count] & 3) == (loop[first] & 3)) first = (first + 1) % loop.Count;
        for (int k = 0; k < loop.Count; k++)
        {
            var (x, y, way) = Parts(loop[(first + k) % loop.Count]);
            if (sides.Count > 0 && sides[^1].Way == way) sides[^1] = sides[^1] with { Length = sides[^1].Length + 1 };
            else sides.Add((x, y, way, 1));
        }
    }

    /// <summary>The pixels a loop encloses: more than none round a shape, less round a hole.</summary>
    private static long Area(List<(int X, int Y, int Way, int Length)> sides)
    {
        long twice = 0;
        foreach (var s in sides)
        {
            var (dx, dy) = Ways[s.Way];
            twice += (long)s.X * (s.Y + dy * s.Length) - (long)(s.X + dx * s.Length) * s.Y;
        }
        return twice / 2;
    }

    /// <summary>A loop's sides as runs, counted on round it, each told whether the loop turns right at its end.</summary>
    private static void Add(List<(int X, int Y, int Way, int Length)> sides, List<OutlineRun> runs)
    {
        int along = 0;
        for (int i = 0; i < sides.Count; i++)
        {
            var s = sides[i];
            var (dx, dy) = Ways[s.Way];
            runs.Add(new(s.X, s.Y, s.Length, dx, dy, along, sides[(i + 1) % sides.Count].Way == ((s.Way + 1) & 3)));
            along += s.Length;
        }
    }

    /// <summary>
    /// The strips <paramref name="width"/> pixels wide just outside <paramref name="runs"/>, a side's each, longer by
    /// <paramref name="width"/> where its loop turns round a convex corner, so the corner is filled; round a concave one
    /// the next strip starts where this one meets it. Counted on round each loop, the corners too, so the dashes run on.
    /// </summary>
    public static List<DashStrip> Strips(IReadOnlyList<OutlineRun> runs, double width)
    {
        var strips = new List<DashStrip>(runs.Count);
        double corners = 0;
        foreach (var run in runs)
        {
            // A loop's first side starts it.
            if (run.Along == 0) corners = 0;
            strips.Add(new(run.X, run.Y, run.Dx, run.Dy, run.Length + (run.Convex ? width : 0), width, run.Along + corners));
            if (run.Convex) corners += width;
        }
        return strips;
    }
}
