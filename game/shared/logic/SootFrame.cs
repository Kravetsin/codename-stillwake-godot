using System;
using System.Collections.Generic;

namespace Stillwake.Game.Shared;

/// <summary>
/// Where the drone's soot sits round a drone window (drone-frame, the picks «Угли»'s <c>frameOf</c> and «Сеть по краю»): spots spread evenly along a
/// closed rectilinear outline, one in each corner, the gap its length ÷ <see cref="SootRun.Tune.Share"/> held to
/// <see cref="SootRun.Tune.GapMin"/>–<see cref="SootRun.Tune.GapMax"/> px, however many spots that takes. Screen px. Client only.
/// </summary>
public static class SootFrame
{
    /// <summary>
    /// The spots round <paramref name="outline"/>, its corners in order, from corner <paramref name="nearCorner"/> on round it.
    /// An edge shorter than the least gap has its two corners alone.
    /// </summary>
    public static SootSpots Spots(IReadOnlyList<(float X, float Y)> outline, int nearCorner)
    {
        int m = outline.Count;
        var corners = new (float X, float Y)[m];
        var lengths = new float[m];
        float perimeter = 0;
        for (int k = 0; k < m; k++) corners[k] = outline[(nearCorner + k) % m];
        for (int k = 0; k < m; k++)
        {
            var (a, b) = (corners[k], corners[(k + 1) % m]);
            lengths[k] = MathF.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
            perimeter += lengths[k];
        }
        float gap = Math.Clamp(perimeter / SootRun.Tune.Share, SootRun.Tune.GapMin, SootRun.Tune.GapMax);
        var xs = new List<float>();
        var ys = new List<float>();
        var along = new List<float>();
        // Each spot's edge, its direction and piece, for its node's place off the outline.
        var edges = new List<(float Dx, float Dy, float Piece, bool Corner)>();
        float at = 0;
        for (int k = 0; k < m; k++)
        {
            float length = lengths[k];
            if (length <= 0) continue;
            // As near the gap as splits the edge evenly, every piece kept within the least and the most.
            int least = (int)MathF.Ceiling(length / SootRun.Tune.GapMax), most = Math.Max(least, (int)MathF.Floor(length / SootRun.Tune.GapMin));
            int pieces = Math.Max(1, Math.Clamp((int)MathF.Round(length / gap), least, most));
            var (a, b) = (corners[k], corners[(k + 1) % m]);
            for (int j = 0; j < pieces; j++)
            {
                xs.Add(a.X + (b.X - a.X) * j / pieces);
                ys.Add(a.Y + (b.Y - a.Y) * j / pieces);
                along.Add(at + length * j / pieces);
                edges.Add(((b.X - a.X) / length, (b.Y - a.Y) / length, length / pieces, j == 0));
            }
            at += length;
        }
        return new SootSpots([.. xs], [.. ys], [.. along], [.. edges], corners, lengths, perimeter);
    }

    /// <summary>The corner of <paramref name="outline"/> nearest <paramref name="drone"/>, screen px: where the soot reaches it first.</summary>
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
}

/// <summary>
/// The spots round one outline (<see cref="SootFrame.Spots"/>), in order round it from the near corner, which is spot 0. Each spot
/// is reached from the near corner the shorter way round: <see cref="Way"/> 1 the outline's own order, -1 back against it.
/// Each spot's mote lands as a node of a net (the pick «Сеть по краю»), a little off the outline (<see cref="Home"/>), and link j
/// joins two nodes near each other (<see cref="Link"/>). Positions are the outline's own coordinates.
/// </summary>
public sealed class SootSpots
{
    internal readonly float[] Xs, Ys, Alongs, OffXs, OffYs, Heats, Sizes, Phase1s, Phase2s;
    internal readonly int[] Ranks, Leads, LinkAs, LinkBs;
    internal readonly sbyte[] Ways;
    private readonly (float X, float Y)[] _corners;
    private readonly float[] _lengths;

    internal SootSpots(float[] xs, float[] ys, float[] along, (float Dx, float Dy, float Piece, bool Corner)[] edges, (float X, float Y)[] corners,
        float[] lengths, float perimeter)
    {
        (Xs, Ys, _corners, _lengths, Perimeter) = (xs, ys, corners, lengths, perimeter);
        int n = xs.Length;
        Alongs = new float[n];
        Ways = new sbyte[n];
        Ranks = new int[n];
        Leads = new int[n];
        int forward = 0;
        for (int i = 0; i < n; i++)
        {
            bool ahead = along[i] <= perimeter / 2;
            (Alongs[i], Ways[i]) = (ahead ? along[i] : perimeter - along[i], (sbyte)(ahead ? 1 : -1));
            if (ahead) forward++;
        }
        // In order round the outline: forward the first spots in order, back the last ones from the end.
        for (int i = 0; i < n; i++)
        {
            (Ranks[i], Leads[i]) = Ways[i] > 0 ? (i, forward - 1 - i) : (n - 1 - i, i - forward);
        }
        (MinX, MinY, MaxX, MaxY) = (float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);
        foreach (var (x, y) in corners) (MinX, MinY, MaxX, MaxY) = (Math.Min(MinX, x), Math.Min(MinY, y), Math.Max(MaxX, x), Math.Max(MaxY, y));
        // Each node's place off its spot, heat, size and phases, drawn from the outline so a frame keeps its net while it stands.
        (OffXs, OffYs, Heats, Sizes, Phase1s, Phase2s) = (new float[n], new float[n], new float[n], new float[n], new float[n], new float[n]);
        var draw = new SootRng((uint)(n * 31 + (int)(MaxX - MinX)));
        for (int i = 0; i < n; i++)
        {
            var e = edges[i];
            float alongBy = e.Corner ? 0 : (draw.Next() - 0.5f) * 2 * SootRun.Tune.NetScatter * e.Piece;
            float across = (draw.Next() - 0.5f) * 2 * SootRun.Tune.NetSpread;
            (OffXs[i], OffYs[i]) = (e.Dx * alongBy - e.Dy * across, e.Dy * alongBy + e.Dx * across);
            Heats[i] = SootRun.Tune.NodeHeatLeast + SootRun.Tune.NodeHeatMore * draw.Next();
            Sizes[i] = SootRun.Tune.NodeSizeLeast + draw.Next();
            (Phase1s[i], Phase2s[i]) = (draw.Next(), draw.Next());
        }
        (LinkAs, LinkBs) = Net();
    }

    /// <summary>Each node to its <see cref="SootRun.Tune.NetNearest"/> nearest within <see cref="SootRun.Tune.NetSearch"/>, each pair once, in a fixed order.</summary>
    private (int[] A, int[] B) Net()
    {
        int n = Xs.Length;
        var a = new List<int>();
        var b = new List<int>();
        var seen = new HashSet<long>();
        var near = new List<(float Far, int J)>();
        for (int i = 0; i < n; i++)
        {
            near.Clear();
            var hi = Home(i);
            for (int j = 0; j < n; j++)
            {
                if (j == i) continue;
                var hj = Home(j);
                float far = MathF.Sqrt((hi.X - hj.X) * (hi.X - hj.X) + (hi.Y - hj.Y) * (hi.Y - hj.Y));
                if (far < SootRun.Tune.NetSearch) near.Add((far, j));
            }
            near.Sort((p, q) => p.Far != q.Far ? p.Far.CompareTo(q.Far) : p.J.CompareTo(q.J));
            for (int k = 0; k < Math.Min(SootRun.Tune.NetNearest, near.Count); k++)
            {
                int lo = Math.Min(i, near[k].J), up = Math.Max(i, near[k].J);
                if (!seen.Add((long)lo * n + up)) continue;
                a.Add(lo);
                b.Add(up);
            }
        }
        return ([.. a], [.. b]);
    }

    /// <summary>Where spot <paramref name="i"/>'s node rests, off the outline by its own few px.</summary>
    public (float X, float Y) Home(int i) => (Xs[i] + OffXs[i], Ys[i] + OffYs[i]);

    /// <summary>How many links the net has.</summary>
    public int Links => LinkAs.Length;

    /// <summary>The two nodes link <paramref name="j"/> joins.</summary>
    public (int A, int B) Link(int j) => (LinkAs[j], LinkBs[j]);

    /// <summary>How many spots there are.</summary>
    public int Count => Xs.Length;

    /// <summary>The outline's length.</summary>
    public float Perimeter { get; }

    /// <summary>How far round the outline the farthest spot is from the near corner, half the perimeter.</summary>
    public float Reach => Perimeter / 2;

    /// <summary>The near corner, where the soot reaches the outline.</summary>
    public (float X, float Y) Corner => _corners[0];

    /// <summary>The box round the outline.</summary>
    public float MinX { get; }
    public float MinY { get; }
    public float MaxX { get; }
    public float MaxY { get; }

    /// <summary>Where spot <paramref name="i"/> is.</summary>
    public (float X, float Y) Spot(int i) => (Xs[i], Ys[i]);

    /// <summary>How far round the outline spot <paramref name="i"/> is from the near corner, the shorter way.</summary>
    public float Along(int i) => Alongs[i];

    /// <summary>How far round the outline spot <paramref name="i"/> is from the near corner in the outline's own order, 0..perimeter.</summary>
    public float Round(int i) => Ways[i] > 0 ? Alongs[i] : Perimeter - Alongs[i];

    /// <summary>Which way round spot <paramref name="i"/> is reached: 1 the outline's order, -1 against it.</summary>
    public int Way(int i) => Ways[i];

    /// <summary>Spot <paramref name="i"/>'s place in its way's order from the near corner: 0 the nearest.</summary>
    public int Rank(int i) => Ranks[i];

    /// <summary>The point <paramref name="s"/> round the outline from the near corner, in its own order.</summary>
    public (float X, float Y) PointAt(float s)
    {
        s = ((s % Perimeter) + Perimeter) % Perimeter;
        for (int k = 0; k < _corners.Length; k++)
        {
            var (a, b) = (_corners[k], _corners[(k + 1) % _corners.Length]);
            if (s <= _lengths[k])
            {
                float u = _lengths[k] > 0 ? s / _lengths[k] : 0;
                return (a.X + (b.X - a.X) * u, a.Y + (b.Y - a.Y) * u);
            }
            s -= _lengths[k];
        }
        return _corners[0];
    }
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
