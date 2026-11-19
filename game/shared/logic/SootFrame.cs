using System;
using System.Collections.Generic;

namespace Stillwake.Game.Shared;

/// <summary>
/// Where the drone's soot sits round a drone window (drone-frame, the pick «Угли»'s <c>frameOf</c>): spots spread evenly along a
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
            }
            at += length;
        }
        return new SootSpots([.. xs], [.. ys], [.. along], corners, lengths, perimeter);
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
/// Span j links spot j to the next. Positions are the outline's own coordinates.
/// </summary>
public sealed class SootSpots
{
    internal readonly float[] Xs, Ys, Alongs, Phases;
    internal readonly int[] Ranks, Leads;
    internal readonly sbyte[] Ways;
    private readonly (float X, float Y)[] _corners;
    private readonly float[] _lengths;

    internal SootSpots(float[] xs, float[] ys, float[] along, (float X, float Y)[] corners, float[] lengths, float perimeter)
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
        // Each span's own phase, as the pick's seeds drew them.
        Phases = new float[n];
        var draw = new SootRng((uint)(n * 31 + (int)(MaxX - MinX)));
        for (int j = 0; j < n; j++) Phases[j] = draw.Next();
    }

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

    /// <summary>Which way round spot <paramref name="i"/> is reached: 1 the outline's order, -1 against it.</summary>
    public int Way(int i) => Ways[i];

    /// <summary>Spot <paramref name="i"/>'s place in its way's order from the near corner: 0 the nearest.</summary>
    public int Rank(int i) => Ranks[i];

    /// <summary>Span <paramref name="j"/>'s own phase, 0..1.</summary>
    public float Phase(int j) => Phases[j];

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
