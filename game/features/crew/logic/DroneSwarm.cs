using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Features.Crew;

/// <summary>A figure the light drone's soot and plates re-form into (drone-swarm).</summary>
public enum SwarmShape
{
    /// <summary>The soot running round three tilted orbits, the plates circling wide and tumbling: with no mode open yet.</summary>
    Idle,

    /// <summary>A soot mirror behind the core, its inner face lit, the plates in a dish before it, open toward the aim.</summary>
    Lamp,

    /// <summary>A spyglass on the aim (companion-scan, «Подзорная труба»): the plates its two draws, the front one sliding to focus, a soot ring at its mouth, a soot hood behind the core.</summary>
    Scan,

    /// <summary>The plates fanned toward the gesture's hologram above, glowing; the soot keeps its idle orbits.</summary>
    Sign,
}

/// <summary>
/// One plate as drawn: its middle from the core's, world px; its turn, radians; whether it is before the core; how much it glows, 0 to
/// 1; how far along its flight to a new shape, 0 not left, 1 home; how far it is turned about its long axis, 1 face on, 0 edge-on,
/// below 0 its rusty back.
/// </summary>
public readonly record struct Plate(float X, float Y, float Turn, bool Front, float Glow, float Flight, float Flip);

/// <summary>
/// One mote of soot as drawn: its middle from the core's and its side, world px; whether it is before the core; how much the field
/// lights it, 0 to 1; how red-hot it is, 0 for a cold one; whether it is of the darker tone.
/// </summary>
public readonly record struct Mote(float X, float Y, float Side, bool Front, float Glow, float Heat, bool Dark);

/// <summary>
/// The light drone's body (drone-swarm, the picked «Пепел в латах»): a cloud of soot the core holds by its field and four big scrap
/// plates riding it as armour, in a shape per mode. On a change each plate leaves late by its index, flies an arc to its new place,
/// overshooting home, and turns over once on the way; each mote is drawn into the core along its old thread and out along the new one.
/// A few motes are red-hot and flicker. The pick page's <c>makeAsh</c>, small level, its numbers a quarter of its canvas pixels. Client only.
/// </summary>
public sealed class DroneSwarm
{
    /// <summary>The swarm's numbers: world px, radians, seconds of the client's clock as the pick counted them, ms for the flights.</summary>
    public static class Tune
    {
        /// <summary>How many plates and motes it holds.</summary>
        public const int Plates = 4, Motes = 170;

        /// <summary>Idle plates: their orbit's radius, how flat it is, its tilt, its pace; their bob and its pace; how fast they turn on the orbit.</summary>
        public const float Orbit = 4, OrbitFlat = 0.45f, OrbitTilt = -0.2f, OrbitPace = 0.35f, Bob = 0.3f, BobPace = 1.2f, Spin = 1.3f;

        /// <summary>Idle plates tumble about their long axis: the least pace, how much more the fastest has, radians a second.</summary>
        public const float Tumble = 0.3f, TumbleMore = 0.4f;

        /// <summary>The lamp's plates: the dish's half height, how far behind the core its middle is, its curve (depth = half²/this).</summary>
        public const float DishHalf = 3, DishBack = 2.75f, DishCurve = 6;

        /// <summary>The spyglass's plates: the back draw's distance ahead and half width, the front draw's, the front's glow; the tremor on the aim and its pace.</summary>
        public const float BackDraw = 1.5f, BackHalf = 1.1f, FrontDraw = 3.75f, FrontHalf = 0.85f, FrontGlow = 0.2f, Tremor = 0.012f, TremorPace = 6.3f;

        /// <summary>The spyglass focusing: the front draw's least slide out, how much further it goes, its pace; how far the plates are turned (1 face on), the front's swing.</summary>
        public const float FocusLeast = 0.25f, FocusMore = 1.125f, FocusPace = 1.15f, BackFlip = 0.62f, FrontFlip = 0.5f, FrontFlipSwing = 0.12f;

        /// <summary>The sign's fan: its reach, half its spread and its turn off straight up, radians.</summary>
        public const float FanReach = 2.25f, FanSpread = 0.8f, FanOff = -0.15f;

        /// <summary>The soot's idle orbits: the first's radius, how much wider each next is, a mote's spread, how flat they are, the first's tilt and each next's more.</summary>
        public const float DustOrbit = 2.5f, DustOrbitMore = 0.55f, DustOrbitSpread = 0.4f, DustOrbitFlat = 0.3f, DustTilt = -0.3f, DustTiltMore = 1.05f;

        /// <summary>The soot's idle pace round the first orbit and each next's more, radians a second.</summary>
        public const float DustPace = 0.8f, DustPaceMore = 0.25f;

        /// <summary>The soot mirror: half its height, how far behind the core its middle is, its curve, its thickness, its shimmer; the share of its lit face and that light.</summary>
        public const float MirrorHalf = 4, MirrorBack = 1.5f, MirrorCurve = 6, MirrorThick = 0.8f, MirrorShimmer = 0.0875f, MirrorLit = 0.22f, MirrorGlow = 0.55f;

        /// <summary>The spyglass's soot: the share round the core, its radius, how much wider, its pace, its light.</summary>
        public const float GlassCore = 0.2f, GlassCoreR = 1, GlassCoreMore = 0.625f, GlassCorePace = 1.1f, GlassCoreGlow = 0.1f;

        /// <summary>The hood behind the core: the share up to it (with the core's), its distance back and how much deeper, its radius and more, its pace.</summary>
        public const float HoodTo = 0.48f, HoodBack = 1.75f, HoodDeep = 0.75f, HoodR = 0.5f, HoodRMore = 0.875f, HoodPace = 0.6f;

        /// <summary>The ring at the mouth: its distance ahead (plus the focus), its depth and more, its half height, how much the focus narrows it, its pace, its light.</summary>
        public const float RingAt = 6, RingDeep = 0.35f, RingDeepMore = 0.2f, RingHalf = 1.4f, RingNarrow = 0.45f, RingPace = 2.4f, RingGlow = 0.3f;

        /// <summary>A mote's least side and how much more the largest has.</summary>
        public const float MoteSide = 0.225f, MoteSideMore = 0.225f;

        /// <summary>The share of motes that are red-hot, their flicker's middle, swing and pace.</summary>
        public const float HotShare = 0.07f, HeatMid = 0.45f, HeatSwing = 0.4f, HeatPace = 11;

        /// <summary>How late the last plate leaves after the first, and each one's flight, ms.</summary>
        public const double StaggerMs = 350, FlightMs = 650;

        /// <summary>How late the latest mote leaves, and each one's flow, ms.</summary>
        public const double DustLateMs = 450, FlowMs = 850;

        /// <summary>A flight's sideways arc, at most, past the length it is full from; its overshoot home (the pick's back-out).</summary>
        public const float Swirl = 2.25f, SwirlFull = 3.5f, Overshoot = 1.45f;

        /// <summary>A mote's flow: the share of it spent being drawn into the core, its sideways wander at most, its light mid-flow.</summary>
        public const float Inflow = 0.4f, Wander = 0.55f, FlowGlow = 0.04f;
    }

    /// <summary>Each plate's phase, radians, and its share of the tumble's extra pace, as the pick's seeds drew them (drone.js).</summary>
    private static readonly (float Phase, float Pace)[] PlateSeeds = [(3.9737f, 0.8855f), (1.1409f, 0.1770f), (4.8644f, 0.6168f), (5.1218f, 0.7379f)];

    /// <summary>Each mote's phase, three shares that place it, its side's share, whether hot, its tone: the pick's <c>dustSet</c>, seed 530.</summary>
    private static readonly (float Phase, float R1, float R2, float R3, float Size, bool Hot, bool Dark)[] MoteSeeds = DrawMotes(530);

    /// <summary>Where the sign stands from the drone: straight above it (<see cref="DroneSign.Place"/>).</summary>
    public const float SignAim = -MathF.PI / 2;

    /// <summary>How long a change takes the plates, ms: the last one's wait and its flight.</summary>
    public static double PlatesSettleMs => (Tune.Plates - 1) * Tune.StaggerMs / Tune.Plates + Tune.FlightMs;

    /// <summary>How long a change takes the soot, ms: the latest mote's wait and its flow.</summary>
    public static double DustSettleMs => Tune.DustLateMs + Tune.FlowMs;

    /// <summary>How long a change takes all of it, ms.</summary>
    public static double SettleMs => Math.Max(PlatesSettleMs, DustSettleMs);

    private SwarmShape _from = SwarmShape.Idle, _dustFrom = SwarmShape.Idle, _dust = SwarmShape.Idle;
    private double _changedMs = double.NegativeInfinity, _dustChangedMs = double.NegativeInfinity;
    private readonly Plate[] _leftPlates = new Plate[Tune.Plates];
    private readonly Mote[] _leftMotes = new Mote[Tune.Motes];
    private bool _platesLeftMid, _motesLeftMid;

    /// <summary>The shape it is in or re-forming into.</summary>
    public SwarmShape Shape { get; private set; } = SwarmShape.Idle;

    /// <summary>
    /// Whether it is bare (drone-frame): its soot out framing a drone window, but for <see cref="Ring"/>, which runs the idle orbits;
    /// the plates keep the mode's shape.
    /// </summary>
    public bool Bare { get; private set; }

    /// <summary>
    /// The soot that stays while it is bare (the pick's <c>ORB</c>): six motes on each of the three orbits, each orbit's in order
    /// round it, spread evenly by phase among its motes.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<int>> Ring { get; } = RingOf();

    private static readonly bool[] InRing = Enumerable.Range(0, Tune.Motes).Select(i => Ring.Any(o => o.Contains(i))).ToArray();

    /// <summary>Whether mote <paramref name="i"/> is with the drone: every one, or while it is bare only the ring's.</summary>
    public bool Kept(int i) => !Bare || InRing[i];

    /// <summary>
    /// Sends it into <paramref name="shape"/> at <paramref name="timeMs"/>, aimed <paramref name="aim"/>, <paramref name="bare"/> or
    /// not; the same does nothing. Mid-change the plates and motes leave from where they are, so a quick change never jumps.
    /// </summary>
    public void Become(SwarmShape shape, float aim, double timeMs, bool bare = false)
    {
        Bare = bare;
        // Each is read where it is before the flag says to start from there.
        var dust = bare ? SwarmShape.Idle : DustOf(shape);
        if (dust != _dust)
        {
            bool motesMid = timeMs - _dustChangedMs < DustSettleMs;
            if (motesMid)
                for (int i = 0; i < Tune.Motes; i++) _leftMotes[i] = MoteAt(i, aim, timeMs);
            (_motesLeftMid, _dustFrom, _dust, _dustChangedMs) = (motesMid, _dust, dust, timeMs);
        }
        if (shape == Shape) return;
        bool platesMid = timeMs - _changedMs < PlatesSettleMs;
        if (platesMid)
            for (int i = 0; i < Tune.Plates; i++) _leftPlates[i] = PlateAt(i, aim, timeMs);
        _platesLeftMid = platesMid;
        (_from, Shape, _changedMs) = (Shape, shape, timeMs);
    }

    /// <summary>
    /// The shape for a drone that <paramref name="signs"/> (shows a gesture) or not, and <paramref name="scanning"/> or shining, or
    /// <paramref name="idle"/>, no mode of its open: the sign's over any other.
    /// </summary>
    public static SwarmShape ShapeOf(bool signs, bool scanning, bool idle = false) =>
        signs ? SwarmShape.Sign : idle ? SwarmShape.Idle : scanning ? SwarmShape.Scan : SwarmShape.Lamp;

    /// <summary>The soot's own shape in <paramref name="shape"/>: the sign's is the idle orbits, since its own lays out the bag's grid.</summary>
    public static SwarmShape DustOf(SwarmShape shape) => shape == SwarmShape.Sign ? SwarmShape.Idle : shape;

    /// <summary>Plate <paramref name="i"/> at <paramref name="timeMs"/>, the lens aimed <paramref name="aim"/>, radians: the pick's <c>morph</c> and <c>flipK</c>.</summary>
    public Plate PlateAt(int i, float aim, double timeMs)
    {
        float t = (float)(timeMs / 1000);
        var to = PlateIn(Shape, i, aim, t);
        double u = Math.Clamp((timeMs - _changedMs - i * Tune.StaggerMs / Tune.Plates) / Tune.FlightMs, 0, 1);
        if (u >= 1) return to;
        var start = _platesLeftMid ? _leftPlates[i] : PlateIn(_from, i, aim, t);
        if (u <= 0) return start with { Flight = 0 };
        float k = (float)u, e = BackOut(k), dx = to.X - start.X, dy = to.Y - start.Y, length = MathF.Sqrt(dx * dx + dy * dy);
        if (length == 0) length = 1;
        float side = MathF.Sin(k * MathF.PI) * Tune.Swirl * (i % 2 == 1 ? 1 : -1) * MathF.Min(1, length / Tune.SwirlFull);
        // A full turn over the flight, from how it was turned to how it rests.
        float settle = k * k * (3 - 2 * k), flip = MathF.Cos(k * MathF.Tau) * (start.Flip * (1 - settle) + to.Flip * settle);
        return new Plate(start.X + dx * e - dy / length * side, start.Y + dy * e + dx / length * side, AngleLerp(start.Turn, to.Turn, e),
            k < 0.5f ? start.Front : to.Front, start.Glow * (1 - k) + to.Glow * k, k, flip);
    }

    /// <summary>Mote <paramref name="i"/> at <paramref name="timeMs"/>, the lens aimed <paramref name="aim"/>: the pick's <c>flowDust</c>.</summary>
    public Mote MoteAt(int i, float aim, double timeMs)
    {
        float t = (float)(timeMs / 1000);
        var to = MoteIn(_dust, i, aim, t);
        var seed = MoteSeeds[i];
        double u = Math.Clamp((timeMs - _dustChangedMs - seed.R2 * Tune.DustLateMs) / Tune.FlowMs, 0, 1);
        if (u >= 1) return to;
        var start = _motesLeftMid ? _leftMotes[i] : MoteIn(_dustFrom, i, aim, t);
        if (u <= 0) return start;
        float k = (float)u, x, y;
        bool front;
        if (k < Tune.Inflow)
        {
            // Drawn in along its old thread, slow at first.
            float e = k / Tune.Inflow * (k / Tune.Inflow);
            (x, y) = Along(start.X, start.Y, 1 - e);
            front = start.Front;
        }
        else
        {
            float s = (k - Tune.Inflow) / (1 - Tune.Inflow), e = 1 - (1 - s) * (1 - s);
            (x, y) = Along(to.X, to.Y, e);
            front = to.Front;
        }
        float length = MathF.Sqrt(x * x + y * y);
        if (length == 0) length = 1;
        float wander = MathF.Sin(k * MathF.PI) * (seed.R3 - 0.5f) * 2 * Tune.Wander;
        return new Mote(x - y / length * wander, y + x / length * wander, seed.Size, front, Tune.FlowGlow * MathF.Sin(k * MathF.PI), Heat(seed, t), seed.Dark);
    }

    /// <summary>Mote <paramref name="i"/>'s seed: its phase on its orbit, its spread, its side, whether it smoulders and whether dark (the drone's meeting winds its blood onto them).</summary>
    public static (float Phase, float R1, float Size, bool Hot, bool Dark) SeedOf(int i)
    {
        var s = MoteSeeds[i];
        return (s.Phase, s.R1, s.Size, s.Hot, s.Dark);
    }

    /// <summary>Plate <paramref name="i"/> held in <paramref name="shape"/>, <paramref name="t"/> seconds on, aimed <paramref name="aim"/>: the pick's <c>tf</c>.</summary>
    public static Plate PlateIn(SwarmShape shape, int i, float aim, float t)
    {
        float w = i / (Tune.Plates - 1f) * 2 - 1;
        switch (shape)
        {
            case SwarmShape.Lamp:
                {
                    float y = w * Tune.DishHalf, x = -Tune.DishBack + y * y / Tune.DishCurve;
                    var (px, py) = Rotate(x, y, aim);
                    return new Plate(px, py, aim + MathF.Atan2(1, 2 * y / Tune.DishCurve), false, 0, 1, 1);
                }
            case SwarmShape.Scan:
                {
                    // Two draws, each a plate above and below the line: the back one still, the front one sliding to focus.
                    float held = aim + MathF.Sin(t * Tune.TremorPace) * Tune.Tremor;
                    bool front = i % 2 == 1;
                    float up = i < 2 ? -1 : 1, ext = Focus(t);
                    var (px, py) = Rotate(front ? Tune.FrontDraw + ext : Tune.BackDraw, up * (front ? Tune.FrontHalf : Tune.BackHalf), held);
                    float flip = front ? Tune.FrontFlip + Tune.FrontFlipSwing * MathF.Sin(t * Tune.FocusPace + i) : Tune.BackFlip;
                    return new Plate(px, py, held, true, front ? Tune.FrontGlow : 0, 1, flip);
                }
            case SwarmShape.Sign:
                {
                    float at = SignAim + Tune.FanOff + w * Tune.FanSpread;
                    return new Plate(MathF.Cos(at) * Tune.FanReach, MathF.Sin(at) * Tune.FanReach, at, true, 1, 1, 1);
                }
            default:
                {
                    var (phase, pace) = PlateSeeds[i];
                    float th = i / (float)Tune.Plates * MathF.Tau + t * Tune.OrbitPace;
                    var (px, py) = Rotate(MathF.Cos(th) * Tune.Orbit, MathF.Sin(th) * Tune.Orbit * Tune.OrbitFlat, Tune.OrbitTilt);
                    float tumble = MathF.Cos(t * (Tune.Tumble + Tune.TumbleMore * pace) + phase * 3);
                    return new Plate(px, py + MathF.Sin(t * Tune.BobPace + i) * Tune.Bob, th * Tune.Spin + i, MathF.Sin(th) >= 0, 0, 1, tumble);
                }
        }
    }

    /// <summary>Mote <paramref name="i"/> held in <paramref name="shape"/>'s soot, <paramref name="t"/> seconds on, aimed <paramref name="aim"/>: the pick's <c>home</c>.</summary>
    public static Mote MoteIn(SwarmShape shape, int i, float aim, float t)
    {
        var seed = MoteSeeds[i];
        float heat = Heat(seed, t);
        switch (DustOf(shape))
        {
            case SwarmShape.Lamp:
                {
                    float y = (seed.R1 * 2 - 1) * Tune.MirrorHalf;
                    float x = -Tune.MirrorBack + y * y / Tune.MirrorCurve - seed.R2 * Tune.MirrorThick + MathF.Sin(t * 2 + seed.Phase) * Tune.MirrorShimmer;
                    var (px, py) = Rotate(x, y, aim);
                    return new Mote(px, py, seed.Size, false, seed.R2 < Tune.MirrorLit ? Tune.MirrorGlow : 0, heat, seed.Dark);
                }
            case SwarmShape.Scan:
                {
                    if (seed.R3 < Tune.GlassCore)
                    {
                        float th = seed.Phase + t * Tune.GlassCorePace, r = Tune.GlassCoreR + seed.R1 * Tune.GlassCoreMore;
                        return new Mote(MathF.Cos(th) * r, MathF.Sin(th) * r * 0.75f, seed.Size, MathF.Sin(th) >= 0, Tune.GlassCoreGlow, heat, seed.Dark);
                    }
                    if (seed.R3 < Tune.HoodTo)
                    {
                        float a = seed.Phase + t * Tune.HoodPace, rr = Tune.HoodR + seed.R1 * Tune.HoodRMore;
                        var (hx, hy) = Rotate(-Tune.HoodBack - seed.R2 * Tune.HoodDeep + MathF.Cos(a) * rr * 0.5f, MathF.Sin(a) * rr, aim);
                        return new Mote(hx, hy, seed.Size, false, 0, heat, seed.Dark);
                    }
                    float ext = Focus(t), round = seed.Phase + t * Tune.RingPace;
                    float half = (Tune.RingHalf - Tune.RingNarrow * ext) * (0.85f + 0.3f * seed.R1);
                    var (px, py) = Rotate(Tune.RingAt + ext + MathF.Cos(round) * (Tune.RingDeep + seed.R1 * Tune.RingDeepMore), MathF.Sin(round) * half, aim);
                    return new Mote(px, py, seed.Size, MathF.Cos(round) >= 0, Tune.RingGlow, heat, seed.Dark);
                }
            default:
                {
                    // Three tilted orbits, the soot running round each like beads on a wire, the middle one the other way.
                    int orbit = i % 3;
                    float tilt = Tune.DustTilt + orbit * Tune.DustTiltMore, way = orbit % 2 == 1 ? -1 : 1;
                    float th = seed.Phase + t * (Tune.DustPace + Tune.DustPaceMore * orbit) * way;
                    float r = Tune.DustOrbit + orbit * Tune.DustOrbitMore + (seed.R1 - 0.5f) * Tune.DustOrbitSpread;
                    var (px, py) = Rotate(MathF.Cos(th) * r, MathF.Sin(th) * r * Tune.DustOrbitFlat, tilt);
                    return new Mote(px, py, seed.Size, MathF.Sin(th) >= 0, 0, heat, seed.Dark);
                }
        }
    }

    /// <summary>How far the spyglass's front draw has slid out at <paramref name="t"/>, focusing back and forth.</summary>
    private static float Focus(float t) => Tune.FocusLeast + Tune.FocusMore * (0.5f + 0.5f * MathF.Sin(t * Tune.FocusPace));

    /// <summary>How hot a mote is at <paramref name="t"/>: a flicker round the middle for a hot one, none for the rest.</summary>
    private static float Heat((float Phase, float R1, float R2, float R3, float Size, bool Hot, bool Dark) seed, float t) =>
        seed.Hot ? Tune.HeatMid + Tune.HeatSwing * MathF.Sin(t * Tune.HeatPace + seed.Phase * 7) : 0;

    /// <summary>A share <paramref name="s"/> along the field's thread from the core to (<paramref name="x"/>, <paramref name="y"/>), bowed one way as the pick's.</summary>
    private static (float X, float Y) Along(float x, float y, float s)
    {
        float bx = x * 0.5f + y * 0.25f, by = y * 0.5f - x * 0.25f, k = 1 - s;
        return (2 * k * s * bx + s * s * x, 2 * k * s * by + s * s * y);
    }

    /// <summary>The pick's <c>dustSet</c>: its xorshift from <paramref name="seed"/>, seven draws a mote in its order.</summary>
    private static (float, float, float, float, float, bool, bool)[] DrawMotes(int seed)
    {
        uint s = unchecked((uint)((ulong)seed * 2654435761UL));
        if (s == 0) s = 1;
        float Next()
        {
            s ^= s << 13;
            s ^= s >> 17;
            s ^= s << 5;
            return (float)(s / 4294967296.0);
        }
        var motes = new (float, float, float, float, float, bool, bool)[Tune.Motes];
        for (int i = 0; i < motes.Length; i++)
        {
            float phase = Next() * 6.283f, r1 = Next(), r2 = Next(), r3 = Next(), size = Tune.MoteSide + Next() * Tune.MoteSideMore;
            bool hot = Next() < Tune.HotShare, dark = Next() < 0.5f;
            motes[i] = (phase, r1, r2, r3, size, hot, dark);
        }
        return motes;
    }

    /// <summary>Six motes of each orbit, its motes sorted by phase and every sixth of the way along them taken.</summary>
    private static IReadOnlyList<IReadOnlyList<int>> RingOf()
    {
        var ring = new IReadOnlyList<int>[3];
        for (int orbit = 0; orbit < 3; orbit++)
        {
            var ids = Enumerable.Range(0, Tune.Motes).Where(i => i % 3 == orbit).OrderBy(i => MoteSeeds[i].Phase).ToArray();
            ring[orbit] = Enumerable.Range(0, 6).Select(k => ids[k * ids.Length / 6]).ToArray();
        }
        return ring;
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
