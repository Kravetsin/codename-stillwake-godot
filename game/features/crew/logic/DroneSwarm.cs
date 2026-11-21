using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Shared;
using I = Stillwake.Game.Shared.DroneInk;

namespace Stillwake.Game.Features.Crew;

/// <summary>A shape the light drone's sparks form (drone-threads).</summary>
public enum SwarmShape
{
    /// <summary>None: every spark on the three orbits.</summary>
    Idle,

    /// <summary>The lamp's mirror behind the core, open toward the aim.</summary>
    Lamp,

    /// <summary>The reticle on the aim (the pick «Прицел»): a ring, four ticks and the dial marks the gauge's dots leave from.</summary>
    Scan,

    /// <summary>A gesture's fan: turned where a point points, else standing up and swaying.</summary>
    Sign,
}

/// <summary>
/// One spark as drawn, pick px about the core (<see cref="DroneInk.Px"/> world px each): its middle, its heading for its tail,
/// whether before the core, how hot (before an alarm), and while it flies a thread from where it left.
/// </summary>
public readonly record struct Spark(float X, float Y, float Dx, float Dy, bool Front, float Heat, bool Flying, float FromX, float FromY);

/// <summary>
/// The light drone's body (drone-threads, the pick «Нити»): <see cref="DroneInk.Sparks"/> sparks running round three tilted orbits,
/// a share of them forming one shape at a time and flowing along its threads; the scan's reticle (the pick «Прицел») takes them all, its
/// marks' sparks the gauge's dots. A change sends each spark straight from where it is to its new place, late by its own share. Pick px
/// and seconds of the client's clock; client only.
/// </summary>
public sealed class DroneSwarm
{
    private static readonly (float Phase, float R1)[] Seeds = DrawSeeds();
    private static readonly ThreadPath LampPath = new(I.Lamp), FanPath = new(I.Fan);
    /// <summary>Each spark's place in order along a shape of <see cref="DroneInk.Share"/>, -1 if it keeps orbiting; how many take part.</summary>
    private static readonly int[] Ranks = RanksOf(I.Share, out ShareCount);
    private static readonly int ShareCount;

    /// <summary>Each spark's part of the reticle (<see cref="Mark"/>, <see cref="Ring"/>, <see cref="Ticks"/>) and its rank there: it takes them all.</summary>
    private static readonly (int Part, int Rank)[] ScanParts = ScanPartsOf(out ScanCounts);
    private static readonly int[] ScanCounts;
    private const int Mark = 0, Ring = 1, Ticks = 2;
    private static readonly float[] ScanFlows = [0, I.RingFlow, I.TickFlow];
    /// <summary>The ring's and the ticks' closed lengths, what their flow runs by (the pick's <c>PL0</c>).</summary>
    private static readonly float[] ScanLengths = [0, new ThreadPath(I.ScanRing).Length, new ThreadPath(I.ScanTicks).Length];
    /// <summary>Where on its mark each seed sits, unturned, and its heading.</summary>
    private static readonly (float X, float Y, float Dx, float Dy)[] MarkSeats = [.. I.ScanMarks.Select(m => new ThreadPath([m]).At(I.MarkAt))];
    /// <summary>The spark sitting on each mark.</summary>
    private static readonly int[] SeedSparks = SeedSparksOf();

    /// <summary>Whether the scanner reads, since when, and how open the reticle was then; the reticle's lines and paths as last opened.</summary>
    private bool _reading;
    private double _readS = double.NegativeInfinity;
    private float _openFrom, _linesOpen = float.NaN;
    private readonly float[][] _scanLines = new float[I.Dots + I.ScanRing.Length + I.ScanTicks.Length][];
    private ThreadPath _ringPath = new(I.ScanRing), _tickPath = new(I.ScanTicks);

    /// <summary>
    /// Each spark's flight: where it leaves from, live (the orbits, or the shape it held, turned as then pointed) or a fixed point
    /// (it was mid-flight); how far into a shape it was; whether it flies.
    /// </summary>
    private readonly SwarmShape?[] _fromShape = new SwarmShape?[I.Sparks];
    private readonly float?[] _fromPointed = new float?[I.Sparks];
    private readonly float[] _fromX = new float[I.Sparks], _fromY = new float[I.Sparks], _fromIn = new float[I.Sparks];
    private readonly bool[] _flies = new bool[I.Sparks];
    private SwarmShape _was = SwarmShape.Idle;
    private double _changedS = double.NegativeInfinity;
    private float _wasForm, _wasTurn;
    private float? _pointed;

    /// <summary>The shape it is in or forming.</summary>
    public SwarmShape Shape { get; private set; } = SwarmShape.Idle;

    /// <summary>How many sparks a shape takes.</summary>
    public static int Shared => ShareCount;

    /// <summary>
    /// The shape for a drone that <paramref name="projecting"/> (a drone window out), <paramref name="signs"/> (shows a gesture),
    /// <paramref name="scanning"/> or shining, or <paramref name="idle"/> with no mode: a window over a gesture over a mode.
    /// </summary>
    public static SwarmShape ShapeOf(bool projecting, bool signs, bool scanning, bool idle) =>
        projecting ? SwarmShape.Idle : signs ? SwarmShape.Sign : idle ? SwarmShape.Idle : scanning ? SwarmShape.Scan : SwarmShape.Lamp;

    /// <summary>
    /// Sends it into <paramref name="shape"/> at <paramref name="timeS"/>, the lens aimed <paramref name="aim"/>, a sign turned to
    /// <paramref name="pointed"/> (none: up); the same shape only turns it. Each spark leaves from where it is now: one still on the
    /// orbits or a shape keeps running there till it leaves, one mid-flight from where it is.
    /// </summary>
    public void Become(SwarmShape shape, float aim, double timeS, float? pointed = null)
    {
        if (shape == Shape)
        {
            _pointed = pointed;
            return;
        }
        for (int i = 0; i < I.Sparks; i++)
        {
            bool wasIn = In(i);
            float k = Progress(i, timeS, wasIn), f = _fromIn[i] + ((wasIn ? 1 : 0) - _fromIn[i]) * k;
            if (!_flies[i] || k >= 1) (_fromShape[i], _fromPointed[i]) = (wasIn ? Shape : SwarmShape.Idle, _pointed);
            else if (k > 0)
            {
                var now = SparkAt(i, aim, timeS);
                (_fromShape[i], _fromX[i], _fromY[i]) = (null, now.X, now.Y);
            }
            _fromIn[i] = f;
            _flies[i] = f > 0 || _fromShape[i] == null || Takes(shape, i);
        }
        (_wasForm, _wasTurn) = (Form(timeS), TurnOf(Shape, aim, timeS));
        (_was, Shape, _changedS, _pointed) = (Shape, shape, timeS, pointed);
    }

    /// <summary>Back on the idle orbits at once, nothing in flight: shown again after it was hidden, or on a load.</summary>
    public void Reset()
    {
        (Shape, _was, _changedS, _pointed, _wasForm) = (SwarmShape.Idle, SwarmShape.Idle, double.NegativeInfinity, null, 0);
        (_reading, _readS, _openFrom) = (false, double.NegativeInfinity, 0);
        Array.Clear(_flies);
        Array.Clear(_fromIn);
    }

    /// <summary>Whether the scanner reads a thing from <paramref name="timeS"/>: the reticle opens as it starts, and closes once it stops and its dots are home.</summary>
    public void Read(bool reading, double timeS)
    {
        if (reading == _reading) return;
        (_openFrom, _reading, _readS) = (Open(timeS), reading, timeS);
    }

    /// <summary>How far the reticle is opened at <paramref name="timeS"/>, 0..1: its ticks closing in on the core.</summary>
    public float Open(double timeS)
    {
        float since = (float)Math.Min(1e6, timeS - _readS);
        return _reading ? _openFrom + (1 - _openFrom) * Smooth(since / I.OpenIn) : _openFrom * (1 - Smooth((since - I.CloseAfter) / I.CloseIn));
    }

    /// <summary>How far its shape has formed at <paramref name="timeS"/>, 0..1; 0 idle.</summary>
    public float Form(double timeS) => Shape == SwarmShape.Idle ? 0 : Smooth(Elapsed(timeS));

    /// <summary>The shape it left, still falling apart at <paramref name="timeS"/>, and how much of it shows; its turn as it left.</summary>
    public (SwarmShape Shape, float Form, float Turn) Leaving(double timeS) =>
        _was == SwarmShape.Idle ? (_was, 0, 0) : (_was, _wasForm * (1 - Smooth(Elapsed(timeS))), _wasTurn);

    /// <summary>Which way <paramref name="shape"/> is turned at <paramref name="timeS"/>: the mirror and reticle to the aim, a sign pointed or up and swaying.</summary>
    public float TurnOf(SwarmShape shape, float aim, double timeS) => Turn(shape, aim, timeS, _pointed);

    /// <summary>
    /// The polylines of <paramref name="shape"/> at <paramref name="timeS"/>, pick px about the core unturned; none for idle. The reticle's are its
    /// <see cref="Marks"/> marks first, mark j the seat of the gauge's dot j, then its ring and ticks as opened.
    /// </summary>
    public float[][] LinesOf(SwarmShape shape, double timeS)
    {
        if (shape != SwarmShape.Scan) return shape switch { SwarmShape.Lamp => I.Lamp, SwarmShape.Sign => I.Fan, _ => [] };
        Opened(Open(timeS));
        return _scanLines;
    }

    /// <summary>How many of <paramref name="shape"/>'s first lines are its marks, each a seat of the gauge's dots.</summary>
    public static int Marks(SwarmShape shape) => shape == SwarmShape.Scan ? I.Dots : 0;

    /// <summary>The seat of the gauge's dot <paramref name="j"/> on the reticle turned <paramref name="turn"/>, pick px about the core.</summary>
    public static (float X, float Y) Seat(int j, float turn) => Rotate(MarkSeats[j].X, MarkSeats[j].Y, turn);

    /// <summary>The gauge's dot spark <paramref name="i"/> sits on as a mark of the reticle, -1 for a spark on its ring or ticks.</summary>
    public static int SeedOf(int i) => ScanParts[i].Part == Mark ? ScanParts[i].Rank : -1;

    /// <summary>The spark sitting on the reticle's mark <paramref name="j"/>.</summary>
    public static int SparkOfSeed(int j) => SeedSparks[j];

    /// <summary>Spark <paramref name="i"/>'s part of the reticle (0 a mark, 1 the ring, 2 the ticks) and its rank there.</summary>
    public static (int Part, int Rank) ScanPartOf(int i) => ScanParts[i];

    /// <summary>How many sparks each part of the reticle takes: its marks, its ring, its ticks.</summary>
    public static IReadOnlyList<int> ScanShares => ScanCounts;

    /// <summary>Spark <paramref name="i"/> at <paramref name="timeS"/>, the lens aimed <paramref name="aim"/>: the pick's <c>drawBody</c>.</summary>
    public Spark SparkAt(int i, float aim, double timeS)
    {
        float t = (float)timeS;
        var orbit = Orbit(i, t);
        bool toIn = In(i);
        var (tx, ty, tdx, tdy) = toIn ? Place(Shape, i, TurnOf(Shape, aim, timeS), timeS) : (orbit.X, orbit.Y, orbit.Dx, orbit.Dy);
        float k = Progress(i, timeS, toIn);
        if (k >= 1) return toIn ? new Spark(tx, ty, tdx, tdy, true, HeatOf(i, 1, t), false, 0, 0) : Orbiting(i, orbit, t);
        float f = _fromIn[i] + ((toIn ? 1 : 0) - _fromIn[i]) * k;
        var from = _fromShape[i] switch
        {
            null => (X: _fromX[i], Y: _fromY[i], Dx: tdx, Dy: tdy),
            SwarmShape.Idle => (orbit.X, orbit.Y, orbit.Dx, orbit.Dy),
            SwarmShape shape => Place(shape, i, Turn(shape, aim, timeS, _fromPointed[i]), timeS),
        };
        if (k <= 0)
            return _fromShape[i] == SwarmShape.Idle ? Orbiting(i, orbit, t)
                : new Spark(from.X, from.Y, from.Dx, from.Dy, f > 0.5f || orbit.Z >= 0, HeatOf(i, f, t), false, 0, 0);
        float s = Smooth(k), x = from.X + (tx - from.X) * s, y = from.Y + (ty - from.Y) * s;
        float dx = tx - from.X, dy = ty - from.Y, d = MathF.Sqrt(dx * dx + dy * dy);
        if (d < 1e-4f) (dx, dy, d) = (tdx, tdy, 1);
        float heat = HeatOf(i, f, t) + I.FlightBump * MathF.Sin(k * MathF.PI);
        return new Spark(x, y, dx / d, dy / d, f > 0.5f || orbit.Z >= 0, heat, true, from.X, from.Y);
    }
    /// <summary>Spark <paramref name="i"/>'s rank along a shape, -1 if it keeps orbiting through every shape.</summary>
    public static int RankOf(int i) => Ranks[i];

    /// <summary>The pick's <c>hsh</c>: a fixed share 0..1 for each number.</summary>
    public static double Hash(double i)
    {
        double x = Math.Sin(i * 127.1 + 311.7) * 43758.5453;
        return x - Math.Floor(x);
    }

    /// <summary>
    /// Spark <paramref name="i"/> on its orbit at <paramref name="t"/>: its place, heading and side (before the core from 0): the
    /// pick's <c>orbit</c>, hurried and widened as «Нити» runs it.
    /// </summary>
    public static (float X, float Y, float Dx, float Dy, float Z) Orbit(int i, float t)
    {
        var (phase, r1) = Seeds[i];
        int o = i % 3;
        float way = o % 2 == 1 ? -1 : 1, th = phase + t * (I.OrbitPace + I.OrbitPaceMore * o) * way * I.OrbitHurry;
        float rx = (I.Orbit + o * I.OrbitMore + (r1 - 0.5f) * I.OrbitSpread) * I.OrbitWide, tilt = I.OrbitTilt + o * I.OrbitTiltMore;
        var (x, y) = Rotate(MathF.Cos(th) * rx, MathF.Sin(th) * rx * I.OrbitFlat, tilt);
        var (vx, vy) = Rotate(-MathF.Sin(th) * way, MathF.Cos(th) * I.OrbitFlat * way, tilt);
        float v = MathF.Sqrt(vx * vx + vy * vy);
        if (v == 0) v = 1;
        return (x, y, vx / v, vy / v, MathF.Sin(th));
    }

    /// <summary>Whether spark <paramref name="i"/> belongs on the current shape.</summary>
    private bool In(int i) => Takes(Shape, i);

    /// <summary>Whether spark <paramref name="i"/> belongs on <paramref name="shape"/>: the reticle takes every one, the others their share.</summary>
    private static bool Takes(SwarmShape shape, int i) => shape switch { SwarmShape.Idle => false, SwarmShape.Scan => true, _ => Ranks[i] >= 0 };

    /// <summary>How far along its flight spark <paramref name="i"/> is at <paramref name="timeS"/>, eased: 0 not left, 1 there (or not flying).</summary>
    private float Progress(int i, double timeS, bool toIn)
    {
        if (!_flies[i]) return 1;
        float e = Elapsed(timeS), lag = (float)Hash(i + 50);
        return toIn ? Smooth((e - lag * I.Lag) / I.Span) : Smooth((e - lag * I.LagOut) / I.SpanOut);
    }

    private static Spark Orbiting(int i, (float X, float Y, float Dx, float Dy, float Z) orbit, float t) =>
        new(orbit.X, orbit.Y, orbit.Dx, orbit.Dy, orbit.Z >= 0, IdleHeat(i, t), false, 0, 0);

    private static float Turn(SwarmShape shape, float aim, double timeS, float? pointed) => shape == SwarmShape.Sign
        ? pointed ?? -MathF.PI / 2 + I.FanSway * MathF.Sin((float)timeS * I.FanSwayPace)
        : aim;
    /// <summary>A spark <paramref name="f"/> of the way into a shape: hot and flickering on it, else its idle heat; the lamp's a little hotter.</summary>
    private float HeatOf(int i, float f, float t) =>
        (f > 0 ? I.OnShape + I.OnShapeSwing * MathF.Sin(t * I.OnShapePace + i) : IdleHeat(i, t)) + (Shape == SwarmShape.Lamp ? I.LampMore * f : 0);

    /// <summary>Half the orbiting sparks bright and flickering, the rest dim.</summary>
    private static float IdleHeat(int i, float t) =>
        Hash(i + 3) < I.BrightShare ? I.Bright + I.BrightSwing * MathF.Sin(t * I.BrightPace + i * 7) : I.Dim;

    /// <summary>
    /// Spark <paramref name="i"/>'s place on <paramref name="shape"/> turned <paramref name="turn"/> at <paramref name="timeS"/>, flowing along it: on the
    /// reticle a seed sits on its mark, the rest flow along the ring or the ticks as opened, each by its own pace (the pick's parts).
    /// </summary>
    private (float X, float Y, float Dx, float Dy) Place(SwarmShape shape, int i, float turn, double timeS)
    {
        float t = (float)timeS;
        (float X, float Y, float Dx, float Dy) at;
        if (shape == SwarmShape.Scan)
        {
            var (part, rank) = ScanParts[i];
            if (part == Mark) at = MarkSeats[rank];
            else
            {
                Opened(Open(timeS));
                at = (part == Ring ? _ringPath : _tickPath).At((rank + 0.5f) / ScanCounts[part] + t * ScanFlows[part] / ScanLengths[part]);
            }
        }
        else
        {
            var path = shape == SwarmShape.Lamp ? LampPath : FanPath;
            at = path.At((Ranks[i] + 0.5f) / ShareCount + t * I.Flow / path.Length);
        }
        var (px, py) = Rotate(at.X, at.Y, turn);
        var (hx, hy) = Rotate(at.Dx, at.Dy, turn);
        return (px, py, hx, hy);
    }

    /// <summary>The reticle's lines and paths opened <paramref name="open"/> of the way, built again only as that changes.</summary>
    private void Opened(float open)
    {
        if (open == _linesOpen) return;
        _linesOpen = open;
        for (int j = 0; j < I.Dots; j++) _scanLines[j] = I.ScanMarks[j];
        var ring = Lerp(I.ScanRing, I.ScanRingOpen, open);
        var ticks = Lerp(I.ScanTicks, I.ScanTicksOpen, open);
        ring.CopyTo(_scanLines, I.Dots);
        ticks.CopyTo(_scanLines, I.Dots + ring.Length);
        (_ringPath, _tickPath) = (new ThreadPath(ring), new ThreadPath(ticks));
    }

    private static float[][] Lerp(float[][] closed, float[][] opened, float k) =>
        [.. closed.Select((line, l) => line.Select((v, n) => v + (opened[l][n] - v) * k).ToArray())];

    private float Elapsed(double timeS) => (float)Math.Min(1e6, (timeS - _changedS) / I.Form);

    private static float Smooth(float u)
    {
        u = Math.Clamp(u, 0, 1);
        return u * u * (3 - 2 * u);
    }

    private static (float X, float Y) Rotate(float x, float y, float a) => (x * MathF.Cos(a) - y * MathF.Sin(a), x * MathF.Sin(a) + y * MathF.Cos(a));

    private static int[] RanksOf(float share, out int count)
    {
        var ranks = new int[I.Sparks];
        count = 0;
        for (int i = 0; i < ranks.Length; i++) ranks[i] = Hash(i * 3 + 1) < share ? count++ : -1;
        return ranks;
    }

    /// <summary>
    /// The reticle's parts for every spark, as the pick's <c>alloc</c>: each part's count its share of the sparks rounded by largest remainder, the
    /// sparks taken in order of their hash, the marks' first.
    /// </summary>
    private static (int, int)[] ScanPartsOf(out int[] counts)
    {
        double seeds = I.Dots / (double)I.Sparks;
        double[] raw = [.. ((double[])[seeds, I.RingShare, 1 - seeds - I.RingShare]).Select(s => s * I.Sparks)];
        counts = [.. raw.Select(v => (int)Math.Floor(v + 1e-9))];
        int left = (int)Math.Round(raw.Sum()) - counts.Sum();
        var byRest = Enumerable.Range(0, raw.Length).OrderByDescending(p => raw[p] - Math.Floor(raw[p] + 1e-9)).ToArray();
        for (int m = 0; m < left; m++) counts[byRest[m % byRest.Length]]++;
        var parts = new (int, int)[I.Sparks];
        int at = 0;
        var order = Enumerable.Range(0, I.Sparks).OrderBy(i => Hash(i * 3 + 1)).ToArray();
        for (int part = 0; part < counts.Length; part++)
            for (int rank = 0; rank < counts[part] && at < I.Sparks; rank++) parts[order[at++]] = (part, rank);
        return parts;
    }

    private static int[] SeedSparksOf()
    {
        var sparks = new int[I.Dots];
        for (int i = 0; i < I.Sparks; i++)
            if (ScanParts[i] is (Mark, var rank)) sparks[rank] = i;
        return sparks;
    }

    /// <summary>The kit's seeds for the drone's embers (<c>DR.P</c>, seed 530): each one's phase and spread, its side, heat and tone drawn and dropped.</summary>
    private static (float, float)[] DrawSeeds()
    {
        var draw = new PickRng(530);
        var seeds = new (float, float)[I.Sparks];
        for (int i = 0; i < seeds.Length; i++)
        {
            float phase = draw.Next() * 6.283f, r1 = draw.Next();
            for (int skip = 0; skip < 3; skip++) draw.Next();
            seeds[i] = (phase, r1);
        }
        return seeds;
    }

    /// <summary>Polylines walked as one path by a share of its length, wrapping (the pick's <c>mkPath</c> and <c>onPath</c>); pick px.</summary>
    private sealed class ThreadPath
    {
        private readonly (float Ax, float Ay, float Bx, float By, float Length, float At)[] _segments;

        public ThreadPath(float[][] lines)
        {
            var segments = new List<(float, float, float, float, float, float)>();
            float at = 0;
            foreach (var line in lines)
                for (int k = 2; k + 1 < line.Length; k += 2)
                {
                    float ax = line[k - 2], ay = line[k - 1], bx = line[k], by = line[k + 1], d = MathF.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay));
                    if (d < 1e-6f) continue;
                    segments.Add((ax, ay, bx, by, d, at));
                    at += d;
                }
            _segments = [.. segments];
            Length = at;
        }

        /// <summary>The whole length of its lines.</summary>
        public float Length { get; }

        /// <summary>The point <paramref name="s"/> of the way along it, wrapping, and its heading.</summary>
        public (float X, float Y, float Dx, float Dy) At(float s)
        {
            float d = (s % 1 + 1) % 1 * Length;
            int lo = 0, hi = _segments.Length - 1;
            while (lo < hi)
            {
                int m = (lo + hi + 1) >> 1;
                if (_segments[m].At <= d) lo = m;
                else hi = m - 1;
            }
            var q = _segments[lo];
            float e = Math.Clamp((d - q.At) / q.Length, 0, 1);
            return (q.Ax + (q.Bx - q.Ax) * e, q.Ay + (q.By - q.Ay) * e, (q.Bx - q.Ax) / q.Length, (q.By - q.Ay) / q.Length);
        }
    }

    /// <summary>The pick page's <c>rng</c>: a xorshift seeded as its was, draws 0..1.</summary>
    private struct PickRng
    {
        private uint _s;

        public PickRng(uint seed)
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
}
