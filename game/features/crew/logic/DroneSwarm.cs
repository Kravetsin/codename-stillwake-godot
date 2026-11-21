using System;
using System.Collections.Generic;
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

    /// <summary>The spyglass on the aim (companion-scan).</summary>
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
/// a share of them forming one shape at a time and flowing along its threads. A change sends each spark straight from where it is
/// to its new place, late by its own share. Pick px and seconds of the client's clock; client only.
/// </summary>
public sealed class DroneSwarm
{
    private static readonly (float Phase, float R1)[] Seeds = DrawSeeds();
    private static readonly ThreadPath LampPath = new(I.Lamp), ScanPath = new(I.Scan), FanPath = new(I.Fan);
    /// <summary>Each spark's place in order along a shape of <see cref="DroneInk.Share"/>, -1 if it keeps orbiting; how many take part.</summary>
    private static readonly int[] Ranks = RanksOf(I.Share, out ShareCount);
    private static readonly int ShareCount;

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
            _flies[i] = f > 0 || _fromShape[i] == null || Ranks[i] >= 0 && shape != SwarmShape.Idle;
        }
        (_wasForm, _wasTurn) = (Form(timeS), TurnOf(Shape, aim, timeS));
        (_was, Shape, _changedS, _pointed) = (Shape, shape, timeS, pointed);
    }

    /// <summary>Back on the idle orbits at once, nothing in flight: shown again after it was hidden, or on a load.</summary>
    public void Reset()
    {
        (Shape, _was, _changedS, _pointed, _wasForm) = (SwarmShape.Idle, SwarmShape.Idle, double.NegativeInfinity, null, 0);
        Array.Clear(_flies);
        Array.Clear(_fromIn);
    }

    /// <summary>How far its shape has formed at <paramref name="timeS"/>, 0..1; 0 idle.</summary>
    public float Form(double timeS) => Shape == SwarmShape.Idle ? 0 : Smooth(Elapsed(timeS));

    /// <summary>The shape it left, still falling apart at <paramref name="timeS"/>, and how much of it shows; its turn as it left.</summary>
    public (SwarmShape Shape, float Form, float Turn) Leaving(double timeS) =>
        _was == SwarmShape.Idle ? (_was, 0, 0) : (_was, _wasForm * (1 - Smooth(Elapsed(timeS))), _wasTurn);

    /// <summary>Which way <paramref name="shape"/> is turned at <paramref name="timeS"/>: the mirror and spyglass to the aim, a sign pointed or up and swaying.</summary>
    public float TurnOf(SwarmShape shape, float aim, double timeS) => Turn(shape, aim, timeS, _pointed);

    /// <summary>The polylines of <paramref name="shape"/>, pick px about the core unturned; none for idle.</summary>
    public static float[][] LinesOf(SwarmShape shape) => shape switch
    {
        SwarmShape.Lamp => I.Lamp,
        SwarmShape.Scan => I.Scan,
        SwarmShape.Sign => I.Fan,
        _ => [],
    };

    /// <summary>Spark <paramref name="i"/> at <paramref name="timeS"/>, the lens aimed <paramref name="aim"/>: the pick's <c>drawBody</c>.</summary>
    public Spark SparkAt(int i, float aim, double timeS)
    {
        float t = (float)timeS;
        var orbit = Orbit(i, t);
        bool toIn = In(i);
        var (tx, ty, tdx, tdy) = toIn ? Place(Shape, Ranks[i], TurnOf(Shape, aim, timeS), t) : (orbit.X, orbit.Y, orbit.Dx, orbit.Dy);
        float k = Progress(i, timeS, toIn);
        if (k >= 1) return toIn ? new Spark(tx, ty, tdx, tdy, true, HeatOf(i, 1, t), false, 0, 0) : Orbiting(i, orbit, t);
        float f = _fromIn[i] + ((toIn ? 1 : 0) - _fromIn[i]) * k;
        var from = _fromShape[i] switch
        {
            null => (X: _fromX[i], Y: _fromY[i], Dx: tdx, Dy: tdy),
            SwarmShape.Idle => (orbit.X, orbit.Y, orbit.Dx, orbit.Dy),
            SwarmShape shape => Place(shape, Ranks[i], Turn(shape, aim, timeS, _fromPointed[i]), t),
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
    private bool In(int i) => Shape != SwarmShape.Idle && Ranks[i] >= 0;

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

    /// <summary>The place of rank <paramref name="rank"/> on <paramref name="shape"/> turned <paramref name="turn"/> at <paramref name="t"/>, flowing along it.</summary>
    private static (float X, float Y, float Dx, float Dy) Place(SwarmShape shape, int rank, float turn, float t)
    {
        var path = shape switch { SwarmShape.Lamp => LampPath, SwarmShape.Scan => ScanPath, _ => FanPath };
        var (x, y, dx, dy) = path.At((rank + 0.5f) / ShareCount + t * I.Flow / path.Length);
        var (px, py) = Rotate(x, y, turn);
        var (hx, hy) = Rotate(dx, dy, turn);
        return (px, py, hx, hy);
    }

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
