using System;
using System.Collections.Generic;
using I = Stillwake.Game.Shared.DroneInk;

namespace Stillwake.Game.Shared;

/// <summary>
/// Where the drone's sparks land round a drone window (drone-frame, the pick «Нити»): <see cref="DroneInk.FrameRows"/> rows of small
/// squares, the outline's own and each next <see cref="DroneInk.FrameGap"/> px further in, a square every gap along each row (at most
/// <see cref="DroneInk.FrameMost"/>), counted from the corner nearest the drone. Screen px. Client only.
/// </summary>
public static class SootFrame
{
    /// <summary>The squares round <paramref name="outline"/>, its corners in order, counted from corner <paramref name="nearCorner"/>.</summary>
    public static SootSpots Spots(IReadOnlyList<(float X, float Y)> outline, int nearCorner)
    {
        int m = outline.Count;
        var corners = new (float X, float Y)[m];
        for (int k = 0; k < m; k++) corners[k] = outline[(nearCorner + k) % m];
        float area = 0;
        for (int k = 0; k < m; k++) area += corners[k].X * corners[(k + 1) % m].Y - corners[(k + 1) % m].X * corners[k].Y;
        var xs = new List<float>();
        var ys = new List<float>();
        var lags = new List<float>();
        var pers = new List<float>();
        var rows = new List<int>();
        for (int row = 0; row < I.FrameRows; row++)
        {
            var ring = row == 0 ? corners : Inset(corners, row * I.FrameGap, area > 0);
            var lengths = new float[m];
            float perimeter = 0;
            for (int k = 0; k < m; k++)
            {
                var (a, b) = (ring[k], ring[(k + 1) % m]);
                perimeter += lengths[k] = MathF.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
            }
            if (perimeter <= 0) continue;
            int n = Math.Clamp((int)MathF.Round(perimeter / I.FrameGap), 1, I.FrameMost);
            for (int j = 0; j < n; j++)
            {
                float d = perimeter * j / n;
                var (x, y) = PointAt(ring, lengths, d);
                xs.Add(x);
                ys.Add(y);
                // Each square is reached from the near corner the shorter way round, the farthest last.
                lags.Add(MathF.Min(d, perimeter - d) / (perimeter / 2));
                pers.Add((float)j / n);
                rows.Add(row);
            }
        }
        return new SootSpots([.. xs], [.. ys], [.. lags], [.. pers], [.. rows]);
    }

    /// <summary>The corner of <paramref name="outline"/> nearest <paramref name="drone"/>, screen px: where the sparks reach it first.</summary>
    public static int NearCorner(IReadOnlyList<(float X, float Y)> outline, (float X, float Y) drone)
    {
        int best = 0;
        float bestFar = float.MaxValue;
        for (int k = 0; k < outline.Count; k++)
        {
            float dx = outline[k].X - drone.X, dy = outline[k].Y - drone.Y, far = dx * dx + dy * dy;
            if (far < bestFar) (best, bestFar) = (k, far);
        }
        return best;
    }

    /// <summary><paramref name="corners"/> moved <paramref name="by"/> px inward, each corner along its two edges' inward normals.</summary>
    private static (float X, float Y)[] Inset((float X, float Y)[] corners, float by, bool leftIsIn)
    {
        int m = corners.Length;
        var inset = new (float X, float Y)[m];
        for (int k = 0; k < m; k++)
        {
            var (n1x, n1y) = Inward(corners[(k + m - 1) % m], corners[k], leftIsIn);
            var (n2x, n2y) = Inward(corners[k], corners[(k + 1) % m], leftIsIn);
            // The mitre: as far along both normals' middle as keeps each edge `by` in.
            float dot = 1 + n1x * n2x + n1y * n2y, k2 = dot > 1e-4f ? by / dot : by;
            inset[k] = (corners[k].X + (n1x + n2x) * k2, corners[k].Y + (n1y + n2y) * k2);
        }
        return inset;
    }

    private static (float X, float Y) Inward((float X, float Y) a, (float X, float Y) b, bool leftIsIn)
    {
        float dx = b.X - a.X, dy = b.Y - a.Y, d = MathF.Sqrt(dx * dx + dy * dy);
        if (d <= 0) return (0, 0);
        return leftIsIn ? (-dy / d, dx / d) : (dy / d, -dx / d);
    }

    private static (float X, float Y) PointAt((float X, float Y)[] ring, float[] lengths, float d)
    {
        for (int k = 0; k < ring.Length; k++)
        {
            if (d <= lengths[k] && lengths[k] > 0)
            {
                var (a, b) = (ring[k], ring[(k + 1) % ring.Length]);
                float u = d / lengths[k];
                return (a.X + (b.X - a.X) * u, a.Y + (b.Y - a.Y) * u);
            }
            d -= lengths[k];
        }
        return ring[0];
    }
}

/// <summary>
/// The squares round one outline (<see cref="SootFrame.Spots"/>), in the outline's own coordinates: each one's place, how late it
/// leaves (0 the near corner's, 1 the farthest), its share of the way round its row from the near corner, and its row.
/// </summary>
public sealed class SootSpots(float[] xs, float[] ys, float[] lags, float[] pers, int[] rows)
{
    /// <summary>How many squares there are.</summary>
    public int Count => xs.Length;

    /// <summary>Where square <paramref name="i"/> lands.</summary>
    public (float X, float Y) Spot(int i) => (xs[i], ys[i]);

    /// <summary>How late square <paramref name="i"/> leaves the drone, 0..1: how far round from the near corner, the shorter way, of half the row.</summary>
    public float Lag(int i) => lags[i];

    /// <summary>Square <paramref name="i"/>'s share of the way round its row from the near corner, 0..1: its shimmer's phase.</summary>
    public float Per(int i) => pers[i];

    /// <summary>Square <paramref name="i"/>'s row, 0 the outline's own.</summary>
    public int Row(int i) => rows[i];
}

/// <summary>The pick page's <c>rng</c>: a xorshift seeded as its was, draws 0..1.</summary>
internal struct SootRng
{
    private uint _s;

    public SootRng(uint seed)
    {
        _s = unchecked((uint)((ulong)seed * 2654435761UL));
        if (_s == 0) _s = 1;
    }

    public float Next()
    {
        _s ^= _s << 13;
        _s ^= _s >> 17;
        _s ^= _s << 5;
        return (float)(_s / 4294967296.0);
    }
}
