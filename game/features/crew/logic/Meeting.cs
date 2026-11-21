using System;
using System.Collections.Generic;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Crew;

/// <summary>Where the meeting is (the pick's <c>ph</c>): in the wreck, crumbling into the vision, in it, crumbling back, home again.</summary>
public enum MeetingPhase { Wreck, Into, Vision, Back, Home }

/// <summary>A mote of the meeting's cloud: its middle from the core's and its side, world px; whether before the core; its glow and heat; its tone.</summary>
public readonly record struct Mote(float X, float Y, float Side, bool Front, float Glow, float Heat, bool Dark);

/// <summary>A mote of the heap the core lies in, world px from the lying core's picture's top-left: its middle and side, its tone, whether an ember.</summary>
public readonly record struct HeapMote(Vec At, double Side, bool Dark, bool Hot);

/// <summary>The core in the meeting: its middle, world px; its size, pick px a unit of its drawing (1 as it lies, 27 in the vision); its lens's turn.</summary>
public readonly record struct MeetingCore(Vec At, double R, double Angle);

/// <summary>The hero in the meeting: its feet, world px; its size and turn about them; its knees, lean and how far its palm reaches to the core.</summary>
public readonly record struct MeetingHero(Vec Feet, double Scale, double Turn, double Kneel, double Lean, double Reach);

/// <summary>
/// A drop of the blood, world px: its head, the two points of its tail behind it, before the core or behind it, its radius; its
/// colours 0..255 (the drop, its rim, its sheen and the sheen's alpha), the grey light on its top once black, its embers' heat.
/// </summary>
public readonly record struct BloodDrop(Vec At, Vec Tail, Vec Mid, bool Front, double Radius, (double R, double G, double B) Fill,
    (double R, double G, double B) Rim, (double R, double G, double B) Sheen, double SheenAlpha, double Lit, double Ember);

/// <summary>What the core's face shows: the blood soaked into it, its lens lit and its glow.</summary>
public readonly record struct CoreFace(double Soak, double Lens, double Glow);

/// <summary>
/// The light drone's first meeting with its hero (drone-meeting) as the vision the user picked («Стена», pick page drone-vision.js): E
/// holds the hero; the core lights and rises from its pit; the wreck crumbles into flakes that go out as stars while the core grows
/// in place; in the void the hero floats before the dull giant core, its blood flying from the palm and winding into three tilted
/// rings one after another, turning black with embers as the lens opens; they look at each other; the wreck comes back, the core
/// shrinks to where it began to grow, its rings its cloud; then its threads kindle one at a time and it rises to the head. Every time
/// is the pick's seconds, E pressed at <see cref="Times.Press"/>; every place world px, the pick's pixels a quarter of one
/// (<see cref="Px"/>). The pick had the hero right of the core: a <c>side</c> of -1 mirrors what it placed by the hero. Client only;
/// the sim spares the hero meanwhile (<c>Crew.MeetingTicks</c>).
/// </summary>
public static class Meeting
{
    /// <summary>A world px per pick pixel.</summary>
    public const double Px = 0.25;

    /// <summary>The pick's timeline (its <c>TL</c>), seconds from the scene's start, E pressed at <see cref="Press"/>.</summary>
    public static class Times
    {
        public const double Press = 0.35, HudFrom = 0.35, HudTo = 0.75, GradeFrom = 0.35, GradeTo = 1.6, LightFrom = 0.6, LightTo = 1.5;
        public const double LiftFrom = 1.3, LiftTo = 2.5, InFrom = 2.8, InTo = 4.8, DimFrom = 3.0, DimTo = 4.4, ReachFrom = 4.8, ReachTo = 5.6;
        /// <summary>When each ring begins to take the blood; a drop's flight from the palm.</summary>
        public const double Ring0 = 5.8, Ring1 = 7.7, Ring2 = 9.6, Fly = 0.2;
        public const double BlackFrom = 11.6, BlackTo = 13.9, SpinFrom = 11.6, SpinTo = 13.6, LensFrom = 12.6, LensTo = 14.0;
        public const double DizzyFrom = 5.5, DizzyFull = 6.5, DizzyEase = 13.6, DizzyGone = 14.6;
        public const double OutFrom = 16.8, OutTo = 19.0, ThreadsFrom = 19.8, ThreadGap = 1.0, Kindle = 0.25;
        public const double RiseFrom = 23.4, RiseTo = 24.3, GradeOutFrom = 23.8, GradeOutTo = 24.8, HudBackFrom = 24.4, HudBackTo = 24.8;
    }

    /// <summary>When it ends: the drone by the head, the grade gone, the HUD back.</summary>
    public const double End = Times.GradeOutTo;

    /// <summary>How far into the last breath the head swims while the blood is drawn: lighter than the last breath, never ink.</summary>
    public const double Level = 0.55;

    /// <summary>The giant core's size in the vision, pick px a unit; the rings' size there, a share of the cloud's.</summary>
    public const double VoidR = 27, VoidRings = 0.62;

    /// <summary>The hero's size in the vision.</summary>
    public const double VoidScale = 0.4;

    /// <summary>The lying core's middle in its picture, from the picture's top-left (drone.js's <c>machine_alienCore</c>).</summary>
    public static readonly Vec InPicture = new(32 * Px, 57.5 * Px);

    /// <summary>The blood in the vision, its sheen and rim (the pick's <c>BV</c>), then black (<c>BLK</c>), 0..255.</summary>
    public static readonly (double R, double G, double B) Blood = (138, 18, 22), BloodLight = (196, 64, 60), BloodDark = (44, 5, 8);
    public static readonly (double R, double G, double B) Black = (22, 21, 24), BlackLight = (120, 118, 126), BlackDark = (6, 6, 8);

    /// <summary>The soaked blood (<c>BL</c>), the drone's ember, the lens lit (<c>SIGN</c>), the red glow as it drinks.</summary>
    public static readonly (double R, double G, double B) Soaked = (84, 10, 14), Ember = (255, 138, 58);
    public static readonly (double R, double G, double B) Sign = (168, 238, 255), Drinking = (150, 20, 18);

    /// <summary>The cloud's soot, as the drone draws it (<c>LightDrone</c>): dark and light, each with its rim.</summary>
    private static readonly (double R, double G, double B) SootDark = (76, 79, 85), SootLight = (96, 99, 105), RimDark = (26, 27, 31), RimLight = (38, 40, 45);

    // ---------- the timeline ----------

    public static MeetingPhase PhaseOf(double u) =>
        u < Times.InFrom ? MeetingPhase.Wreck : u < Times.InTo ? MeetingPhase.Into : u < Times.OutFrom ? MeetingPhase.Vision
        : u < Times.OutTo ? MeetingPhase.Back : MeetingPhase.Home;

    /// <summary>How far the wreck has crumbled into the vision, 0..1; how far the vision has crumbled back.</summary>
    public static double Into(double u) => C01((u - Times.InFrom) / (Times.InTo - Times.InFrom));

    public static double Out(double u) => C01((u - Times.OutFrom) / (Times.OutTo - Times.OutFrom));

    /// <summary>How far the hero is from where it stands to where it floats in the vision, 0..1.</summary>
    public static double Blend(double u) => PhaseOf(u) switch
    {
        MeetingPhase.Vision => 1,
        MeetingPhase.Into => Sm(Into(u)),
        MeetingPhase.Back => 1 - Sm(Out(u)),
        _ => 0,
    };

    /// <summary>Whether the hero is held by its meeting at <paramref name="u"/>: from the press to the end, nothing cancels it.</summary>
    public static bool Holds(double u) => u >= 0 && u < End;

    /// <summary>How much of the HUD shows, 0..1: gone from E, back at the end.</summary>
    public static double Hud(double u) => C01(1 - Ss(u, Times.HudFrom, Times.HudTo) + Ss(u, Times.HudBackFrom, Times.HudBackTo));

    /// <summary>The cutscene's grade, 0..1: eased in from E, out at the end.</summary>
    public static double Grade(double u) => Ss(u, Times.GradeFrom, Times.GradeTo) * (1 - Ss(u, Times.GradeOutFrom, Times.GradeOutTo));

    /// <summary>How far into the last breath the screen swims at <paramref name="u"/>: while the blood is drawn and turns; never ink.</summary>
    public static double Dizzy(double u) => Level * Ss(u, Times.DizzyFrom, Times.DizzyFull) * (1 - Ss(u, Times.DizzyEase, Times.DizzyGone));

    /// <summary>How brightly the core's ember halo wakes in the wreck, 0..1, dimming as the wreck crumbles.</summary>
    public static double Light(double u) => Ss(u, Times.LightFrom, Times.LightTo) * (1 - Ss(u, Times.DimFrom, Times.DimTo));

    /// <summary>How lit the lens is, 0..1: dead till the blood turns, open from then on.</summary>
    public static double LensOn(double u) => PhaseOf(u) switch
    {
        MeetingPhase.Wreck or MeetingPhase.Into => 0,
        MeetingPhase.Home => 1,
        _ => Ss(u, Times.LensFrom, Times.LensTo),
    };

    public static double LensGlow(double u) => 0.35 + 0.5 * Bump(u, Times.LensFrom, Times.LensTo, Times.LensTo + 0.6);

    /// <summary>How far the core's face has turned from red to ember with its blood.</summary>
    public static double Blackened(double u) => Ss(u, Times.BlackFrom, Times.BlackTo);

    /// <summary>The rings' size, a share of the cloud's: tight in the vision, the cloud's as they shrink back with the core.</summary>
    public static double Rings(double u) => PhaseOf(u) switch
    {
        MeetingPhase.Back => Lerp(VoidRings, 1, Ss(Out(u), 0.3, 1)),
        MeetingPhase.Home => 1,
        _ => VoidRings,
    };

    /// <summary>How far the rings' drops spread off their orbit, a share of the cloud's: a tight band in the vision.</summary>
    public static double Spread(double u) => PhaseOf(u) switch
    {
        MeetingPhase.Back => Lerp(0.2, 1, Ss(Out(u), 0.3, 1)),
        MeetingPhase.Home => 1,
        _ => 0.2,
    };

    /// <summary>Whether the core still lies in its heap, its picture and glow shown: till it lifts.</summary>
    public static bool CoreLies(double u) => u < Times.LiftFrom;

    /// <summary>Whether the meeting draws the drone, the hero's own hidden: till it reaches the head.</summary>
    public static bool DrawsDrone(double u) => u < Times.RiseTo;

    /// <summary>
    /// Whether the lying core's picture is gone for this player: the core lost, or this player paired or <paramref name="taken"/> it in
    /// their own meeting, while no meeting still shows it lying (<paramref name="held"/>).
    /// </summary>
    public static bool CoreGone(bool lost, bool paired, bool taken, bool held) => lost || (paired || taken) && !held;

    /// <summary>The lying core's ember halo's breath at <paramref name="clockS"/>, 0.7..1.</summary>
    public static double Breath(double clockS) => 0.7 + 0.3 * Math.Sin(clockS / 2.6 * 2 * Math.PI);

    /// <summary>The lying core's halo, its alpha: breathing (<paramref name="breath"/>), and brighter as it wakes in a meeting.</summary>
    public static double Halo(double u, double breath) => 0.55 * breath + 0.5 * Light(u);

    /// <summary>How far the lit lens has gone from the pick's colour to the drone's own as it rises, so the hand-over shows no jump.</summary>
    public static double LensShift(double u) => Ss(u, Times.RiseFrom, Times.RiseTo);

    /// <summary>The breathing halo of the small core risen and its waking light, alphas, at size <paramref name="r"/>: the wreck's alone.</summary>
    public static (double Breathing, double Waking) SmallHalos(double u, double breath, double r)
    {
        var phase = PhaseOf(u);
        if (phase is not (MeetingPhase.Wreck or MeetingPhase.Into) || u < Times.LiftFrom) return (0, 0);
        double big = Ss(r, 2, 20), fade = phase == MeetingPhase.Into ? 1 - Ss(Into(u), 0, 0.5) : 1;
        return (0.55 * breath * fade * Lerp(1, 0.07, big), 0.5 * Light(u) * Lerp(1, 0.1, big));
    }

    // ---------- the heap ----------

    private static readonly HeapMote[] Heap = HeapOf();

    public static int HeapCount => Heap.Length;

    /// <summary>Heap mote <paramref name="j"/>, from the lying core's picture's top-left, world px.</summary>
    public static HeapMote HeapAt(int j) => Heap[j];

    // ---------- the cloud and the threads ----------

    /// <summary>How many motes its cloud holds, a drop of blood each.</summary>
    public const int Motes = 170;

    /// <summary>A mote's least side and how much more the largest has, world px.</summary>
    public const float MoteSide = 0.225f, MoteSideMore = 0.225f;

    /// <summary>Each mote's phase, spread, side, whether it smoulders and its tone: drone.js's <c>dustSet</c>, seed 530.</summary>
    private static readonly (float Phase, float R1, float Size, bool Hot, bool Dark)[] MoteSeeds = DrawMotes(530);

    /// <summary>Mote <paramref name="i"/>'s seed: the meeting winds its blood onto the cloud by them.</summary>
    public static (float Phase, float R1, float Size, bool Hot, bool Dark) SeedOf(int i) => MoteSeeds[i];

    /// <summary>Mote <paramref name="i"/> of the cloud <paramref name="t"/> seconds on: three tilted orbits, the middle one the other way.</summary>
    public static Mote MoteAt(int i, float t)
    {
        var seed = MoteSeeds[i];
        int orbit = i % 3;
        float tilt = -0.3f + orbit * 1.05f, way = orbit % 2 == 1 ? -1 : 1;
        float th = seed.Phase + t * (0.8f + 0.25f * orbit) * way, r = 2.5f + orbit * 0.55f + (seed.R1 - 0.5f) * 0.4f;
        var at = Rot(MathF.Cos(th) * r, MathF.Sin(th) * r * 0.3f, tilt);
        float heat = seed.Hot ? 0.45f + 0.4f * MathF.Sin(t * 11 + seed.Phase * 7) : 0;
        return new Mote((float)at.X, (float)at.Y, seed.Size, MathF.Sin(th) >= 0, 0, heat, seed.Dark);
    }

    /// <summary>When thread <paramref name="o"/> of the drone's three kindles, back from the vision: one at a time, a beat between, the fourth beat empty.</summary>
    public static double Kindles(int o) => Times.ThreadsFrom + o * Times.ThreadGap;

    /// <summary>How far round its orbit thread <paramref name="o"/> has kindled at <paramref name="u"/>, 0..1: none till the core is home.</summary>
    public static double Kindled(int o, double u) => PhaseOf(u) == MeetingPhase.Home ? C01((u - Kindles(o)) / Times.Kindle) : 0;

    /// <summary>
    /// Whether thread <paramref name="o"/>'s kindling at <paramref name="u"/> has run past (<paramref name="x"/>, <paramref name="y"/>),
    /// a point by its orbit from the core's middle: it starts at the orbit's long end and runs round its way, putting the drops out and
    /// lighting the sparks as it passes.
    /// </summary>
    public static bool Passed(int o, double u, double x, double y)
    {
        double k = Kindled(o, u);
        if (k <= 0 || k >= 1) return k >= 1;
        var flat = Rot(x, y, -(DroneInk.OrbitTilt + o * DroneInk.OrbitTiltMore));
        double a = Math.Atan2(flat.Y / DroneInk.OrbitFlat, flat.X) * (o % 2 == 1 ? -1 : 1);
        return (a % (2 * Math.PI) + 2 * Math.PI) % (2 * Math.PI) < 2 * Math.PI * k;
    }

    // ---------- the pick's seeds ----------

    /// <summary>The heap of cold soot the core lies in: drone.js's for <c>machine_alienCore</c>, seed 707, in its 64-px picture, here in world px.</summary>
    private static HeapMote[] HeapOf()
    {
        var r = new Xorshift(707);
        var heap = new HeapMote[90];
        for (int i = 0; i < heap.Length; i++)
        {
            double x = (r.Next() * 2 - 1) * 15, top = 7.5 * (1 - x / 15 * (x / 15)), s = 1.3 + r.Next() * 1.5;
            double y = 64 - s / 2 - Math.Pow(r.Next(), 1.6) * top;
            bool dark = r.Next() < 0.5, hot = r.Next() < 0.08;
            r.Next();
            heap[i] = new HeapMote(new Vec((32 + x) * Px, y * Px), s * Px, dark, hot);
        }
        return heap;
    }

    /// <summary>drone.js's <c>dustSet</c>: its xorshift from <paramref name="seed"/>, seven draws a mote in its order.</summary>
    private static (float, float, float, bool, bool)[] DrawMotes(int seed)
    {
        var r = new Xorshift(seed);
        var motes = new (float, float, float, bool, bool)[Motes];
        for (int i = 0; i < motes.Length; i++)
        {
            float phase = (float)r.Next() * 6.283f, r1 = (float)r.Next();
            r.Next();
            r.Next();
            float size = MoteSide + (float)r.Next() * MoteSideMore;
            bool hot = r.Next() < 0.07, dark = r.Next() < 0.5;
            motes[i] = (phase, r1, size, hot, dark);
        }
        return motes;
    }

    internal sealed class Xorshift(int seed)
    {
        private uint _s = unchecked((uint)((ulong)seed * 2654435761UL)) is var s && s != 0 ? s : 1;

        public double Next()
        {
            _s ^= _s << 13;
            _s ^= _s >> 17;
            _s ^= _s << 5;
            return _s / 4294967296.0;
        }
    }

    internal static double C01(double x) => x < 0 ? 0 : x > 1 ? 1 : x;

    internal static double Sm(double x)
    {
        x = C01(x);
        return x * x * (3 - 2 * x);
    }

    internal static double Ss(double u, double a, double b) => Sm((u - a) / (b - a));

    internal static double Bump(double u, double a, double b, double d) => Ss(u, a, b) * (1 - Ss(u, b, d));

    /// <summary>Exact at both ends, so a place lerped all the way is the place itself.</summary>
    internal static double Lerp(double a, double b, double k) => a * (1 - k) + b * k;

    internal static Vec Lerp(Vec a, Vec b, double k) => new(Lerp(a.X, b.X, k), Lerp(a.Y, b.Y, k));

    internal static (double R, double G, double B) Mix((double R, double G, double B) a, (double R, double G, double B) b, double k) =>
        (Lerp(a.R, b.R, k), Lerp(a.G, b.G, k), Lerp(a.B, b.B, k));

    internal static Vec Bez(Vec a, Vec b, Vec d, double s)
    {
        double k = 1 - s;
        return new Vec(k * k * a.X + 2 * k * s * b.X + s * s * d.X, k * k * a.Y + 2 * k * s * b.Y + s * s * d.Y);
    }

    internal static Vec Rot(double x, double y, double a) => new(x * Math.Cos(a) - y * Math.Sin(a), x * Math.Sin(a) + y * Math.Cos(a));

    internal static double AngLerp(double a, double b, double e)
    {
        double d = b - a;
        while (d > Math.PI) d -= 2 * Math.PI;
        while (d < -Math.PI) d += 2 * Math.PI;
        return a + d * e;
    }

    // ---------- the colours of the soot ----------

    internal static ((double R, double G, double B) Fill, (double R, double G, double B) Rim) Soot(bool dark) =>
        dark ? (SootDark, RimDark) : (SootLight, RimLight);
}

/// <summary>
/// One meeting as it plays (drone-meeting): where its core lay, where its hero's feet were at E and the side it stood on, and how far
/// the drone's clock is ahead of the scene's (<see cref="Shift"/>), so its blood lands on the motes the drone flies with. Its blood's
/// timing is worked out once: each drop leaves the palm to reach its ring where the ring passes nearest the hero.
/// </summary>
public sealed class MeetingScene
{
    /// <summary>The drops, one a mote of the drone's cloud, each on its mote's orbit.</summary>
    public const int Drops = Meeting.Motes;

    private const double Tau = 2 * Math.PI;
    /// <summary>The rings' spin: seven whole turns more by the time they slow to the cloud's own pace (the pick's <c>SPIN_K</c>).</summary>
    private const double SpinTurns = 7;
    private static readonly double Fastest = Tau * SpinTurns / (Meeting.Times.SpinFrom + (Meeting.Times.SpinTo - Meeting.Times.SpinFrom) / 2);

    private readonly double[] _arrives = new double[Drops];
    private readonly double[] _full = new double[3];
    private readonly int[] _count = new int[3];
    private readonly double[] _entry = new double[3];

    /// <param name="core">The core's middle where it lay.</param>
    /// <param name="feet">The hero's feet at E.</param>
    /// <param name="side">1 with the hero right of the core, as the pick had it; -1 left.</param>
    /// <param name="shift">The drone's clock, seconds, less the scene's.</param>
    public MeetingScene(Vec core, Vec feet, int side, double shift)
    {
        (Core, Feet, Side, Shift) = (core, feet, side, shift);
        for (int o = 0; o < 3; o++)
        {
            double best = double.NegativeInfinity;
            for (int j = 0; j < 629; j++)
            {
                double x = side * RingPoint(o, j / 100.0, 10 + o * 2.2, 1).X;
                if (x > best) (best, _entry[o]) = (x, j / 100.0);
            }
        }
        double[] begins = [Meeting.Times.Ring0, Meeting.Times.Ring1, Meeting.Times.Ring2];
        for (int i = 0; i < Drops; i++)
        {
            int o = i % 3, dir = o % 2 == 1 ? -1 : 1;
            double begin = begins[o], f0 = Phi(o, begin);
            double want = f0 + ((dir * (_entry[o] - Meeting.SeedOf(i).Phase) - f0) % Tau + Tau) % Tau;
            double a = begin, b = begin + 4;
            for (int k = 0; k < 44; k++)
            {
                double m = (a + b) / 2;
                if (Phi(o, m) < want) a = m;
                else b = m;
            }
            _arrives[i] = b;
            _full[o] = Math.Max(_full[o], b);
            _count[o]++;
        }
    }

    public Vec Core { get; }
    public Vec Feet { get; }
    public int Side { get; }
    public double Shift { get; }

    /// <summary>Where the core hangs once lifted from its pit, and where it shrinks back to: where it began to grow.</summary>
    public Vec Hover => new(Core.X - Side * 2 * Meeting.Px, Core.Y - 41.5 * Meeting.Px);

    /// <summary>The giant core's middle in the vision.</summary>
    public Vec VoidCore => Placed(-60, -196);

    /// <summary>When drop <paramref name="i"/> reaches its ring; when ring <paramref name="ring"/> is full; how many drops it takes.</summary>
    public double Arrives(int i) => _arrives[i];

    public double Full(int ring) => _full[ring];

    public int Count(int ring) => _count[ring];

    /// <summary>How many of ring <paramref name="ring"/>'s drops have reached it at <paramref name="u"/>.</summary>
    public int Arrived(int ring, double u)
    {
        int n = 0;
        for (int i = ring; i < Drops; i += 3)
            if (u >= _arrives[i]) n++;
        return n;
    }

    /// <summary>How black drop <paramref name="i"/> is at <paramref name="u"/>, 0 red to 1: each ring a little after the last, each drop a little apart.</summary>
    public static double BlackOf(int i, double u)
    {
        double from = Meeting.Times.BlackFrom + i % 3 * 0.25 + Meeting.SeedOf(i).R1 * 0.45;
        return Meeting.Ss(u, from, from + 1.2);
    }

    // ---------- the hero ----------

    /// <summary>The hero weightless before the giant: knees loose, drifting, its palm held out while the blood is drawn.</summary>
    public MeetingHero VoidHero(double u)
    {
        double s = Meeting.VoidScale;
        var feet = Placed(370 + Math.Sin(u * 0.5) * 5 * s, -117 + Math.Cos(u * 0.37) * 7 * s);
        double turn = Side * (-0.32 + 0.06 * Math.Sin(u * 0.41));
        double reach = Meeting.Lerp(0.15, 1, Meeting.Ss(u, Meeting.Times.ReachFrom, Meeting.Times.ReachTo)) * (1 - 0.6 * Meeting.Ss(u, _full[2] + 0.3, _full[2] + 1.5));
        return new MeetingHero(feet, s, turn, 0.1 + 0.05 * Math.Sin(u * 0.7), -0.18 + 0.05 * Math.Sin(u * 0.53), reach);
    }

    /// <summary>The hero standing where it pressed E: still, with a slight sway once back from the vision.</summary>
    public MeetingHero WreckHero(double u)
    {
        double sway = Meeting.PhaseOf(u) == MeetingPhase.Home
            ? Meeting.Ss(u, Meeting.Times.OutTo, Meeting.Times.OutTo + 0.8) * (1 - Meeting.Ss(u, Meeting.Times.RiseTo - 0.2, Meeting.Times.RiseTo + 0.3)) : 0;
        return new MeetingHero(Feet, 1, 0, 0, 0.04 * Math.Sin(u * 1.35) * sway, 0);
    }

    /// <summary>The hero at <paramref name="u"/>: standing, floating, or between as the world crumbles.</summary>
    public MeetingHero HeroAt(double u)
    {
        double m = Meeting.Blend(u);
        var wreck = WreckHero(u);
        if (m <= 0) return wreck;
        var lone = VoidHero(u);
        if (m >= 1) return lone;
        return new MeetingHero(Meeting.Lerp(wreck.Feet, lone.Feet, m), Math.Exp(Meeting.Lerp(Math.Log(wreck.Scale), Math.Log(lone.Scale), m)),
            Meeting.Lerp(wreck.Turn, lone.Turn, m), Meeting.Lerp(wreck.Kneel, lone.Kneel, m), Meeting.Lerp(wreck.Lean, lone.Lean, m), Meeting.Lerp(wreck.Reach, lone.Reach, m));
    }

    /// <summary>The way the giant's lens looks: at the floating hero's helmet.</summary>
    public double VoidAngle(double u)
    {
        var hero = VoidHero(u);
        var helm = new Vec(hero.Feet.X + Math.Sin(hero.Turn) * Rig.Neck * hero.Scale, hero.Feet.Y - Math.Cos(hero.Turn) * Rig.Neck * hero.Scale);
        return Math.Atan2(helm.Y - VoidCore.Y, helm.X - VoidCore.X);
    }

    /// <summary>The way the core's lens looks back in the wreck: at the standing hero's helmet.</summary>
    public double WreckAngle => Math.Atan2(Feet.Y - Rig.Neck - Hover.Y, Feet.X - Hover.X);

    // ---------- the core ----------

    /// <summary>
    /// The core at <paramref name="u"/>: lifting from its pit, growing into the giant as the wreck crumbles, there, shrinking back to
    /// exactly where it began to grow, bobbing, then rising along a curve to <paramref name="idle"/>, the drone's place by the head,
    /// its lens turning to <paramref name="idleAngle"/>, the drone's own way.
    /// </summary>
    public MeetingCore CoreAt(double u, Vec idle, double idleAngle)
    {
        switch (Meeting.PhaseOf(u))
        {
            case MeetingPhase.Wreck:
                return new MeetingCore(Meeting.Lerp(Core, Hover, Meeting.Ss(u, Meeting.Times.LiftFrom, Meeting.Times.LiftTo)), 1, 0);
            case MeetingPhase.Into:
                {
                    double z = Meeting.Sm(Meeting.Into(u));
                    return new MeetingCore(Meeting.Lerp(Hover, VoidCore, z), Math.Exp(Math.Log(Meeting.VoidR) * z), Meeting.AngLerp(0, VoidAngle(u), z));
                }
            case MeetingPhase.Vision:
                return new MeetingCore(VoidCore, Meeting.VoidR, VoidAngle(u));
            case MeetingPhase.Back:
                {
                    double z = Meeting.Sm(Meeting.Out(u));
                    return new MeetingCore(Meeting.Lerp(VoidCore, Hover, z), Math.Exp(Math.Log(Meeting.VoidR) * (1 - z)), Meeting.AngLerp(VoidAngle(u), WreckAngle, z));
                }
            default:
                {
                    double rise = Meeting.Ss(u, Meeting.Times.RiseFrom, Meeting.Times.RiseTo);
                    double bob = 1.2 * Meeting.Ss(u, Meeting.Times.OutTo, Meeting.Times.OutTo + 0.6) * (1 - rise) * Math.Sin(u * 1.7);
                    var hover = new Vec(Hover.X, Hover.Y + bob * Meeting.Px);
                    var at = rise > 0 ? Meeting.Bez(hover, new Vec(hover.X + Side * 4 * Meeting.Px, idle.Y + 36 * Meeting.Px), idle, rise) : hover;
                    return new MeetingCore(at, 1, Meeting.AngLerp(WreckAngle, idleAngle, rise));
                }
        }
    }

    /// <summary>The core's face at <paramref name="u"/>: dull till it drinks, the blood soaked into it while red, its eye open as the blood turns.</summary>
    public CoreFace FaceOf(double u)
    {
        if (Meeting.PhaseOf(u) is MeetingPhase.Wreck or MeetingPhase.Into) return new CoreFace(0, 0, 0);
        double drink = Meeting.Ss(u, Meeting.Times.Ring0, _full[2]), black = Meeting.Blackened(u);
        return new CoreFace(0.3 * drink * (1 - black), Meeting.LensOn(u), Meeting.LensGlow(u));
    }

    /// <summary>The glow round the core in the vision as it drinks, red, then ember as the blood turns; alphas.</summary>
    public (double Drinking, double Embers) VoidGlow(double u, double r)
    {
        var phase = Meeting.PhaseOf(u);
        if (phase is not (MeetingPhase.Vision or MeetingPhase.Back)) return (0, 0);
        double big = Meeting.Ss(r, 2, 20), black = Meeting.Blackened(u);
        double drink = Meeting.Ss(u, Meeting.Times.Ring0, _full[2]) * (1 - black);
        double embers = black * (1 - (phase == MeetingPhase.Back ? Meeting.Ss(Meeting.Out(u), 0, 0.5) : 0));
        return (0.35 * drink * Meeting.Lerp(1, 0.3, big), 0.25 * embers * Meeting.Lerp(1, 0.25, big));
    }

    // ---------- the blood ----------

    /// <summary>How many fine threads each drop leaves the palm as, one after another; how many points draw one.</summary>
    public const int Strands = 3, ThreadPoints = 9;

    /// <summary>A thread's width, world px: under a third of the floating hero's forearm, so the blood reads as a spray of strands.</summary>
    public static readonly double Thread = Rig.Arm.Width * Meeting.VoidScale * 0.3;

    /// <summary>A thread's length, a share of its flight; how far behind the one before each strand leaves; how far apart they fan mid-flight, world px.</summary>
    private const double ThreadLength = 0.45, StrandLag = 0.12, StrandSpread = 1.4;

    /// <summary>
    /// Strand <paramref name="strand"/> of drop <paramref name="i"/>'s thread at <paramref name="u"/>, flying from the palm at
    /// <paramref name="palm"/> to its ring about <paramref name="core"/>, written into <paramref name="into"/> from its tail to its head;
    /// how many points, none while it is not in the air. It curves from the hand and meets the ring along the ring's own way round, so
    /// it spirals in with no corner, the strands fanned apart between and together at both ends.
    /// </summary>
    public int ThreadAt(int i, int strand, double u, Vec palm, MeetingCore core, Span<Vec> into)
    {
        if (Meeting.PhaseOf(u) is not (MeetingPhase.Vision or MeetingPhase.Back)) return 0;
        double e = (u - (_arrives[i] - Meeting.Times.Fly)) / Meeting.Times.Fly - strand * StrandLag;
        if (e <= 0 || e - ThreadLength >= 1) return 0;
        var (p0, p1, p2, p3) = Flight(i, u, palm, core);
        double from = Math.Max(0, e - ThreadLength), to = Math.Min(1, e);
        double spread = (strand - (Strands - 1) / 2.0) * StrandSpread * (0.6 + 0.8 * Meeting.SeedOf(i).R1);
        for (int j = 0; j < ThreadPoints; j++)
        {
            double s = Meeting.Lerp(from, to, j / (double)(ThreadPoints - 1)), k = 1 - s;
            double x = k * k * k * p0.X + 3 * k * k * s * p1.X + 3 * k * s * s * p2.X + s * s * s * p3.X;
            double y = k * k * k * p0.Y + 3 * k * k * s * p1.Y + 3 * k * s * s * p2.Y + s * s * s * p3.Y;
            double dx = 3 * k * k * (p1.X - p0.X) + 6 * k * s * (p2.X - p1.X) + 3 * s * s * (p3.X - p2.X);
            double dy = 3 * k * k * (p1.Y - p0.Y) + 6 * k * s * (p2.Y - p1.Y) + 3 * s * s * (p3.Y - p2.Y);
            double length = Math.Sqrt(dx * dx + dy * dy), sine = Math.Sin(Math.PI * s);
            // Fanned by the square of a sine: apart mid-flight, together at both ends, leaving the curve's way unbent there.
            double off = length > 0 ? spread * sine * sine / length : 0;
            into[j] = new Vec(x - dy * off, y + dx * off);
        }
        return ThreadPoints;
    }

    /// <summary>
    /// Drop <paramref name="i"/> on its ring at <paramref name="u"/> about the core <paramref name="core"/>, winding on it once its
    /// threads have reached it, swelling as they feed it; red, then black with embers, the cloud's own soot as the core shrinks back.
    /// None before it reaches its ring, nor outside the vision and the way back.
    /// </summary>
    public BloodDrop? DropAt(int i, double u, MeetingCore core)
    {
        var phase = Meeting.PhaseOf(u);
        double arrives = _arrives[i];
        if (phase is not (MeetingPhase.Vision or MeetingPhase.Back) || u < arrives) return null;
        var seed = Meeting.SeedOf(i);
        int o = i % 3, dir = o % 2 == 1 ? -1 : 1;
        double size = 1.4 + 1.2 * (seed.Size - Meeting.MoteSide) / Meeting.MoteSideMore;
        double r = size * Math.Max(0.5, 1.6 * Math.Sqrt(core.R / Meeting.VoidR)) * Meeting.Lerp(0.3, 1, Meeting.Ss(u, arrives, arrives + 0.35));
        double th = seed.Phase + dir * Phi(o, u), d = Math.Clamp(0.06 * Pace(o, u), 0.08, 0.3);
        var (head, tail, mid) = (RingAt(i, th, u, core), RingAt(i, th - dir * d, u, core), RingAt(i, th - dir * d * 0.5, u, core));
        double black = BlackOf(i, u), soot = phase == MeetingPhase.Back ? Meeting.Ss(core.R, 4, 1.05) : 0;
        var (sootFill, sootRim) = Meeting.Soot(seed.Dark);
        // Back in the wreck it is a mote of the cloud, its side the drone's.
        r = Meeting.Lerp(r, seed.Size / Meeting.Px / 2, soot);
        double heat = 0.45 + 0.4 * Math.Sin((u + Shift) * 11 + seed.Phase * 7);
        bool smoulders = seed.Hot || seed.R1 > 0.86;
        double keep = seed.Hot ? 1 : smoulders ? 1 - (phase == MeetingPhase.Back ? Meeting.Ss(Meeting.Out(u), 0.2, 0.8) : 0) : 0;
        return new BloodDrop(head, tail, mid, Math.Sin(th) >= 0, r * Meeting.Px,
            Meeting.Mix(Meeting.Mix(Meeting.Blood, Meeting.Black, black), sootFill, soot), Meeting.Mix(Meeting.Mix(Meeting.BloodDark, Meeting.BlackDark, black), sootRim, soot),
            Meeting.Mix(Meeting.BloodLight, Meeting.BlackLight, black), 0.8 * (1 - soot), 0.32 * black * (1 - soot), black * keep * heat);
    }

    /// <summary>Where drop <paramref name="i"/> is, <paramref name="a"/> round its ring about <paramref name="core"/> at <paramref name="u"/>, world px.</summary>
    private Vec RingAt(int i, double a, double u, MeetingCore core)
    {
        int o = i % 3;
        double rx = 10 + o * 2.2 + (Meeting.SeedOf(i).R1 - 0.5) * 1.6 * Meeting.Spread(u), scale = core.R * Meeting.Px;
        var q = RingPoint(o, a, rx, Meeting.Rings(u));
        return new Vec(core.At.X + q.X * scale, core.At.Y + q.Y * scale);
    }

    /// <summary>
    /// Drop <paramref name="i"/>'s flight, a cubic curve: from the palm, bowed up or down, to where it meets its ring, coming in along
    /// the ring's way round there, so the thread turns into the orbit with no corner.
    /// </summary>
    private (Vec, Vec, Vec, Vec) Flight(int i, double u, Vec palm, MeetingCore core)
    {
        int o = i % 3, dir = o % 2 == 1 ? -1 : 1;
        double a = _entry[o], rx = 10 + o * 2.2;
        var end = RingAt(i, a, u, core);
        var way = Meeting.Rot(-Math.Sin(a) * rx * dir, Math.Cos(a) * rx * 0.3 * dir, -0.3 + o * 1.05);
        double wayLength = Math.Sqrt(way.X * way.X + way.Y * way.Y), apart = Math.Sqrt((end.X - palm.X) * (end.X - palm.X) + (end.Y - palm.Y) * (end.Y - palm.Y));
        double reach = 0.4 * apart / wayLength, lift = 40 * (0.5 + Meeting.SeedOf(i).R1) * Meeting.Px;
        var bend = new Vec(Meeting.Lerp(palm.X, end.X, 0.45), way.Y >= 0 ? Math.Min(palm.Y, end.Y) - lift : Math.Max(palm.Y, end.Y) + lift);
        return (palm, bend, new Vec(end.X - way.X * reach, end.Y - way.Y * reach), end);
    }

    /// <summary>A point of ring <paramref name="o"/>, <paramref name="a"/> round it, <paramref name="rx"/> units wide, at <paramref name="k"/> of its size: the cloud's orbit.</summary>
    private static Vec RingPoint(int o, double a, double rx, double k) => Meeting.Rot(Math.Cos(a) * rx * k, Math.Sin(a) * rx * 0.3 * k, -0.3 + o * 1.05);

    /// <summary>How far round ring <paramref name="o"/> has turned by <paramref name="u"/>: the cloud's pace on the drone's clock, and the spin, whole turns once it slows.</summary>
    private double Phi(int o, double u) => (0.8 + 0.25 * o) * (u + Shift) + Spin(u);

    private static double Spin(double u)
    {
        double from = Meeting.Times.SpinFrom, span = Meeting.Times.SpinTo - from;
        if (u <= from) return Fastest * Math.Max(0, u);
        double x = Math.Min(u, Meeting.Times.SpinTo) - from;
        return Fastest * (from + x - x * x / (2 * span));
    }

    private static double Pace(int o, double u) => 0.8 + 0.25 * o + Fastest * Meeting.C01(1 - (u - Meeting.Times.SpinFrom) / (Meeting.Times.SpinTo - Meeting.Times.SpinFrom));

    /// <summary>A place the pick put by the hero, pick px from its feet at E, mirrored to the side it stands on.</summary>
    private Vec Placed(double x, double y) => new(Feet.X + Side * x * Meeting.Px, Feet.Y + y * Meeting.Px);
}

/// <summary>
/// The wreck breaking into flakes that go out as stars (the pick's <c>FLAKES</c>), cut along a jittered grid of the screen, each
/// flying from the core and shrinking as it goes, the farther ones the later. Screen px; the pick's frame was 540 px high.
/// </summary>
public sealed class MeetingFlakes
{
    public const int Across = 40, Down = 22;

    /// <summary>A flake: its four corners and middle where it lies, how late it goes, its way, how far and how much it turns.</summary>
    public readonly record struct Flake(Vec A, Vec B, Vec C, Vec D, Vec Middle, double Late, Vec Way, double Go, double Spin);

    /// <summary>A star in the void: where on the screen, its side, alpha, and whether warm.</summary>
    public readonly record struct Star(Vec At, double Side, double Alpha, bool Warm);

    private readonly List<Flake> _flakes = [];
    private readonly List<Star> _stars = [];

    /// <param name="width">The screen's width, px.</param>
    /// <param name="height">The screen's height, px.</param>
    /// <param name="core">Where the core lay on the screen.</param>
    public MeetingFlakes(double width, double height, Vec core)
    {
        (Width, Height) = (width, height);
        double scale = Scale;
        var r = new Meeting.Xorshift(2024);
        double dx = width / Across, dy = height / Down, far = Math.Sqrt(width * width + height * height);
        var grid = new Vec[Across + 1, Down + 1];
        for (int i = 0; i <= Across; i++)
            for (int j = 0; j <= Down; j++)
            {
                bool edge = i == 0 || j == 0 || i == Across || j == Down;
                double x = i * dx + (edge ? 0 : (r.Next() - 0.5) * dx * 0.6);
                double y = j * dy + (edge ? 0 : (r.Next() - 0.5) * dy * 0.6);
                grid[i, j] = new Vec(x, y);
            }
        for (int i = 0; i < Across; i++)
            for (int j = 0; j < Down; j++)
            {
                Vec a = grid[i, j], b = grid[i + 1, j], c = grid[i + 1, j + 1], d = grid[i, j + 1];
                var middle = new Vec((a.X + c.X) / 2, (a.Y + c.Y) / 2);
                double way = Math.Atan2(middle.Y - core.Y, middle.X - core.X) + (r.Next() - 0.5) * 0.9;
                double late = Math.Sqrt((middle.X - core.X) * (middle.X - core.X) + (middle.Y - core.Y) * (middle.Y - core.Y)) / far * 1.4 + r.Next() * 0.12;
                double go = (60 + r.Next() * 140) * scale, spin = (r.Next() - 0.5) * 3;
                _flakes.Add(new Flake(a, b, c, d, middle, late, new Vec(Math.Cos(way), Math.Sin(way)), go, spin));
            }
        // The void's stars (the pick's VSTARS, seed 77): as many to the frame's width as it had.
        var s = new Meeting.Xorshift(77);
        int count = (int)Math.Round(300 * width / (1000 * scale));
        for (int i = 0; i < count; i++)
        {
            double bright = s.Next(), x = s.Next() * width, y = s.Next() * height;
            double side = (bright < 0.06 ? 1.8 : bright < 0.3 ? 1.1 : 0.7) * scale;
            _stars.Add(new Star(new Vec(x, y), side, 0.18 + s.Next() * 0.55, s.Next() < 0.15));
        }
    }

    public double Width { get; }
    public double Height { get; }

    /// <summary>Screen px a pick px: the pick's frame was 540 px high.</summary>
    public double Scale => Height / 540;

    public IReadOnlyList<Flake> Flakes => _flakes;
    public IReadOnlyList<Star> Stars => _stars;

    /// <summary>
    /// Flake <paramref name="f"/> crumbled <paramref name="e"/> (0 whole, 1 gone to stars): where its middle has flown, its turn, its
    /// size, its alpha and its star's; none once gone.
    /// </summary>
    public static (Vec Middle, double Turn, double Size, double Alpha, double Star)? At(Flake f, double e)
    {
        double q = Meeting.C01((e - f.Late * 0.45) / 0.55);
        if (q >= 1) return null;
        double qq = q * q;
        return (new Vec(f.Middle.X + f.Way.X * qq * f.Go, f.Middle.Y + f.Way.Y * qq * f.Go), f.Spin * qq, 1 - 0.94 * Meeting.Sm(q),
            1 - Meeting.Ss(q, 0.55, 1), q > 0.35 ? 0.85 * Meeting.Bump(q, 0.35, 0.75, 1) : 0);
    }

    /// <summary>How far the screen has crumbled at <paramref name="u"/>: into the vision, or back out of it, the same run in reverse.</summary>
    public static double Crumbled(double u) => Meeting.PhaseOf(u) switch
    {
        MeetingPhase.Into => Meeting.Ss(Meeting.Into(u), 0, 0.85),
        MeetingPhase.Back => Meeting.Ss(1 - Meeting.Out(u), 0, 0.85),
        MeetingPhase.Vision => 1,
        _ => 0,
    };
}

/// <summary>
/// Which other heroes start a meeting here (drone-meeting): one seen turning from unpaired to paired while this client watches,
/// the core not lost. A hero first seen paired, or seen first after a load or a resync (a new <c>world</c>), starts none.
/// </summary>
public sealed class MeetingWatch
{
    private readonly Dictionary<int, bool> _paired = [];
    private object? _world;

    /// <summary>Whether <paramref name="peer"/>'s hero, <paramref name="paired"/> now in <paramref name="world"/>, has just paired here.</summary>
    public bool Turned(object world, int peer, bool paired, bool coreLost)
    {
        if (!ReferenceEquals(world, _world)) (_world, _) = (world, Clear());
        bool seen = _paired.TryGetValue(peer, out bool was);
        _paired[peer] = paired;
        return seen && !was && paired && !coreLost;
    }

    private bool Clear()
    {
        _paired.Clear();
        return true;
    }
}
