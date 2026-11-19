using System;

namespace Stillwake.Game.Features.Crew;

/// <summary>A figure the light drone's shards fly into (drone-swarm).</summary>
public enum SwarmShape
{
    /// <summary>Circling the core lazily, each turning on its own: before its first mode.</summary>
    Idle,

    /// <summary>A dish behind the core, open toward the aim.</summary>
    Lamp,

    /// <summary>A comb before the core, sweeping about the aim, glowing.</summary>
    Scan,

    /// <summary>A fan toward the gesture's hologram above, glowing.</summary>
    Sign,
}

/// <summary>
/// One shard as drawn: its middle from the core's, world px; its turn, radians; whether it is before the core; how much it glows,
/// 0 to 1; how far along its flight to a new shape, 0 not left, 1 home.
/// </summary>
public readonly record struct Shard(float X, float Y, float Turn, bool Front, float Glow, float Flight);

/// <summary>
/// The light drone's shards (drone-swarm, the picked «Рой»): ten pieces of scrap the core holds by its field, in a shape per mode. On a
/// change each leaves late by its index and flies an arc to its new place, overshooting home. The pick page's <c>makeSwarm</c> and
/// <c>morph</c>, its small swarm, its numbers a quarter of its canvas pixels. Client only.
/// </summary>
public sealed class DroneSwarm
{
    /// <summary>The swarm's numbers: world px, radians, seconds of the client's clock as the pick counted them, ms for the flight.</summary>
    public static class Tune
    {
        /// <summary>How many shards it holds.</summary>
        public const int Shards = 10;

        /// <summary>Idle: the orbit's least radius and how much more the slowest shard has, how flat it is, its tilt; its bob and the bob's pace.</summary>
        public const float Orbit = 3, OrbitMore = 1.5f, OrbitFlat = 0.5f, OrbitTilt = -0.25f, Bob = 0.375f, BobPace = 1.3f;

        /// <summary>Idle: a shard's least pace round the orbit and its own turning's, radians a second; its pace adds as much again at most.</summary>
        public const float OrbitPace = 0.45f, SpinPace = 0.8f;

        /// <summary>The lamp's dish: half its height, how far behind the core its middle is, its curve (depth = half²/this).</summary>
        public const float DishHalf = 3.75f, DishBack = 2, DishCurve = 6.5f;

        /// <summary>The scanner's comb: how far ahead, half its height, its sweep's swing and pace, radians and radians a second.</summary>
        public const float CombAhead = 2.75f, CombHalf = 3.5f, Sweep = 0.5f, SweepPace = 3.1f;

        /// <summary>The sign's fan: its reach, half its spread and its turn off straight up, radians.</summary>
        public const float FanReach = 2.5f, FanSpread = 0.85f, FanOff = -0.15f;

        /// <summary>How late the last shard leaves after the first, and each one's flight, ms.</summary>
        public const double StaggerMs = 400, FlightMs = 600;

        /// <summary>A flight's sideways arc, at most, past the length it is full from; its overshoot home (the pick's back-out).</summary>
        public const float Swirl = 2.5f, SwirlFull = 3.5f, Overshoot = 1.45f;
    }

    /// <summary>Each shard's idle phase, radians, and its share of the extra pace and radius, as the pick's seeds drew them (drone.js).</summary>
    private static readonly (float Phase, float Pace)[] Seeds =
    [
        (6.0784f, 0.2687f), (4.1631f, 0.4900f), (0.8636f, 0.7773f), (2.8170f, 0.5826f), (1.5347f, 0.3682f),
        (5.5398f, 0.6317f), (6.0030f, 0.5969f), (0.2359f, 0.6172f), (1.3322f, 0.4056f), (1.2653f, 0.6629f),
    ];

    /// <summary>Where the sign stands from the drone: straight above it (<see cref="DroneSign.Place"/>).</summary>
    public const float SignAim = -MathF.PI / 2;

    /// <summary>How long a change takes, ms: the last shard's wait and its flight.</summary>
    public static double SettleMs => (Tune.Shards - 1) * Tune.StaggerMs / Tune.Shards + Tune.FlightMs;

    private SwarmShape _from = SwarmShape.Idle;
    private double _changedMs = double.NegativeInfinity;
    private Shard[]? _left;

    /// <summary>The shape it is in or flying into.</summary>
    public SwarmShape Shape { get; private set; } = SwarmShape.Idle;

    /// <summary>
    /// Sends it into <paramref name="shape"/> at <paramref name="timeMs"/>, aimed <paramref name="aim"/>; the same shape does nothing.
    /// Mid-flight the shards leave from where they are, so a quick change never jumps.
    /// </summary>
    public void Become(SwarmShape shape, float aim, double timeMs)
    {
        if (shape == Shape) return;
        if (timeMs - _changedMs < SettleMs)
        {
            var left = new Shard[Tune.Shards];
            for (int i = 0; i < left.Length; i++) left[i] = At(i, aim, timeMs);
            _left = left;
        }
        else _left = null;
        (_from, Shape, _changedMs) = (Shape, shape, timeMs);
    }

    /// <summary>Shard <paramref name="i"/> at <paramref name="timeMs"/>, the lens aimed <paramref name="aim"/>, radians.</summary>
    public Shard At(int i, float aim, double timeMs)
    {
        float t = (float)(timeMs / 1000);
        var to = In(Shape, i, aim, t);
        double u = Math.Clamp((timeMs - _changedMs - i * Tune.StaggerMs / Tune.Shards) / Tune.FlightMs, 0, 1);
        if (u >= 1) return to;
        var start = _left is { } left ? left[i] : In(_from, i, aim, t);
        if (u <= 0) return start with { Flight = 0 };
        float k = (float)u, e = BackOut(k), dx = to.X - start.X, dy = to.Y - start.Y, length = MathF.Sqrt(dx * dx + dy * dy);
        if (length == 0) length = 1;
        float side = MathF.Sin(k * MathF.PI) * Tune.Swirl * (i % 2 == 1 ? 1 : -1) * MathF.Min(1, length / Tune.SwirlFull);
        return new Shard(start.X + dx * e - dy / length * side, start.Y + dy * e + dx / length * side, AngleLerp(start.Turn, to.Turn, e),
            k < 0.5f ? start.Front : to.Front, start.Glow * (1 - k) + to.Glow * k, k);
    }

    /// <summary>Shard <paramref name="i"/> held in <paramref name="shape"/>, <paramref name="t"/> seconds on, aimed <paramref name="aim"/>: the pick's <c>tf</c>.</summary>
    public static Shard In(SwarmShape shape, int i, float aim, float t)
    {
        float w = i / (Tune.Shards - 1f) * 2 - 1;
        switch (shape)
        {
            case SwarmShape.Lamp:
                {
                    float y = w * Tune.DishHalf, x = -Tune.DishBack + y * y / Tune.DishCurve;
                    var (px, py) = Rotate(x, y, aim);
                    return new Shard(px, py, aim + MathF.Atan2(1, 2 * y / Tune.DishCurve), false, 0, 1);
                }
            case SwarmShape.Scan:
                {
                    float swung = aim + MathF.Sin(t * Tune.SweepPace) * Tune.Sweep;
                    var (px, py) = Rotate(Tune.CombAhead, w * Tune.CombHalf, swung);
                    return new Shard(px, py, swung + MathF.PI / 2, true, 1, 1);
                }
            case SwarmShape.Sign:
                {
                    float at = SignAim + Tune.FanOff + w * Tune.FanSpread;
                    return new Shard(MathF.Cos(at) * Tune.FanReach, MathF.Sin(at) * Tune.FanReach, at, true, 1, 1);
                }
            default:
                {
                    var (phase, pace) = Seeds[i];
                    float th = phase + t * Tune.OrbitPace * (1 + pace), r = Tune.Orbit + pace * Tune.OrbitMore;
                    var (px, py) = Rotate(MathF.Cos(th) * r, MathF.Sin(th) * r * Tune.OrbitFlat, Tune.OrbitTilt);
                    return new Shard(px, py + MathF.Sin(t * Tune.BobPace + phase) * Tune.Bob, phase + t * (Tune.SpinPace + pace), MathF.Sin(th) >= 0, 0, 1);
                }
        }
    }

    private static (float X, float Y) Rotate(float x, float y, float a) => (x * MathF.Cos(a) - y * MathF.Sin(a), x * MathF.Sin(a) + y * MathF.Cos(a));

    private static float BackOut(float u)
    {
        float v = u - 1;
        return 1 + (Tune.Overshoot + 1) * v * v * v + Tune.Overshoot * v * v;
    }

    private static float AngleLerp(float a, float b, float e)
    {
        float d = b - a;
        while (d > MathF.PI) d -= 2 * MathF.PI;
        while (d < -MathF.PI) d += 2 * MathF.PI;
        return a + d * e;
    }
}
