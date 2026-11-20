using System;
using System.Collections.Generic;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Crew;

/// <summary>Where a plate of the drone's armour is in the meeting: still out among the debris, flying home, or home on the drone.</summary>
public enum PlateStage { Away, Flying, Home }

/// <summary>A plate in the meeting, world px: where its middle is, its turn, how far it is turned over (1 face on, below 0 its back), whether before the core.</summary>
public readonly record struct MeetingPlate(PlateStage Stage, Vec At, double Turn, double Flip, bool Front);

/// <summary>A drop of blood in the meeting's ring, world px: its head, the end of its short tail, before the core or behind it, its alpha and radius.</summary>
public readonly record struct BloodDrop(Vec At, Vec Tail, bool Front, double Alpha, double Radius);

/// <summary>A mote of the core's soot on its way up from the heap, world px: its middle and side, whether it still looks as in the heap, its alpha.</summary>
public readonly record struct RisingMote(Vec At, double Side, bool Heaped, double Alpha);

/// <summary>A mote of the heap the core lies in, world px from the lying core's picture's top-left: its middle and side, its tone, whether an ember.</summary>
public readonly record struct HeapMote(Vec At, double Side, bool Dark, bool Hot);

/// <summary>
/// The light drone's first meeting with its hero (drone-meeting), the picked «Вихрь + Капли» (pick page drone-meeting.js, its kit's
/// kneel): the hero kneels and reaches to the core; blood bursts from the palm in drops that circle the core in a growing ring,
/// tighten and soak in while red cracks spread over it and turn ember; its soot rises into a cloud, its lens lights, four plates
/// snap home from the debris one at a time along ember arcs, and the drone rises to its place by the head as the hero stands. The
/// screen swims as a weaker last breath meanwhile. Every time is seconds from the press, every place world px; the pick's numbers
/// are its canvas pixels, a quarter of a world pixel each (<see cref="Px"/>), and its scene had the hero right of the core: a
/// <c>side</c> of -1 mirrors what it put by the hero. Client only, the sim untouched.
/// </summary>
public static class Meeting
{
    /// <summary>A world px per pick pixel.</summary>
    public const double Px = 0.25;

    /// <summary>When it ends: the hero stood, the drone in its place.</summary>
    public const double End = 12.8;

    /// <summary>
    /// The pick's timeline: the kneel, the palm let go, the stand; the drops bursting out and soaking in; the cracks spreading and
    /// turning ember; the core lifting free, its soot forming the cloud, its lens lighting; the first plate's landing and the beat
    /// between them; the rise.
    /// </summary>
    public static class Times
    {
        public const double KneelFrom = 0.3, KneelTo = 1.1, Release = 7.55, Stand = 12.0;
        public const double BurstFrom = 1.45, BurstTo = 5.2, SinkFrom = 6.9, SinkTo = 7.25, Soaked = 7.55;
        public const double CrackFrom = 2.0, CrackTo = 6.5, EmberFrom = 6.8, EmberTo = 7.5;
        public const double LiftFrom = 7.8, LiftTo = 8.3, FormFrom = 7.9, FormTo = 8.55, LensFrom = 8.1, LensTo = 8.5, LensFlare = 8.95;
        public const double PlatesFrom = 8.7, PlateGap = 0.75, PlateFlight = 0.2;
        public const double RiseFrom = 11.5, RiseTo = 12.4;
    }

    /// <summary>The dizziness at its most, of the last breath (the pick's «strong»), held while the blood is drawn.</summary>
    public const double Strong = 0.62;

    /// <summary>The ring: its drops, their radius at first, how fast its spin picks up, when it grows and when it tightens (the pick's <c>ring</c>).</summary>
    private const int DropCount = 64;
    private const double RingFirst = 9, RingWide = 21, RingTight = 7.5, RingSunk = 4.6, Spin = 5.2, SpinUp = 0.25;
    private static readonly (double From, double To) Grow = (1.6, 5.4), Tighten = (6.0, 7.0);

    /// <summary>Blood: its colour, the sheen and the darker rim (the pick's <c>BL</c>, <c>BL_L</c>, <c>BL_D</c>), 0..255.</summary>
    public static readonly (double R, double G, double B) Blood = (84, 10, 14), BloodLight = (138, 28, 28), BloodDark = (34, 4, 7);

    /// <summary>The cracks' first red, and the drone's ember they turn to, 0..255.</summary>
    public static readonly (double R, double G, double B) CrackRed = (160, 26, 22), Ember = (255, 138, 58);

    /// <summary>The red glow round the core while it drinks, 0..255, and its radius; the ember halo's radius; the lens's colour lit (the pick's <c>SIGN</c>).</summary>
    public static readonly (double R, double G, double B) Drinking = (150, 20, 18), Sign = (168, 238, 255);
    public const double DrinkingRadius = 26 * Px, HaloRadius = 40 * Px;

    /// <summary>The core's radius, world px, as the soak fills it (drone.js's core, 5.4 of its pixels).</summary>
    public const double CoreRadius = 5.4 * Px;

    /// <summary>The lying core's middle in its picture, from the picture's top-left (drone.js's <c>machine_alienCore</c>).</summary>
    public static readonly Vec InPicture = new(32 * Px, 57.5 * Px);

    // ---------- the hero ----------

    /// <summary>How far down onto its knee the hero is at <paramref name="u"/>, 0 standing to 1 knelt.</summary>
    public static double Kneel(double u) => Ss(u, Times.KneelFrom, Times.KneelTo) * (1 - Up(u));

    /// <summary>How far the hero leans over the core, radians forward about the hip: over it while the blood goes, upright on the knee after, swaying with the dizziness.</summary>
    public static double Lean(double u) =>
        (Ss(u, 0.35, 1.15) - 0.85 * Ss(u, Times.Release, Times.Release + 0.5)) * (1 - Up(u)) + 0.05 * Math.Sin(u * 2.1) * Dizzy(u);

    /// <summary>How far the near hand reaches to the core, 0..1: on it from the kneel till the palm is let go.</summary>
    public static double Reach(double u) => Ss(u, 0.9, 1.45) * (1 - Ss(u, Times.Release, Times.Release + 0.35));

    /// <summary>
    /// Where the head looks, radians down: down at the core, at the hand on it, at the core again as it lets go, then after the
    /// drone as it rises, <paramref name="toDrone"/> being that turn (<c>Poses.HeadTurn</c>); swaying with the dizziness.
    /// </summary>
    public static double Look(double u, double toDrone)
    {
        double look = Lerp(0.5, 0.2, Ss(u, Times.KneelFrom, Times.KneelTo));
        look = Lerp(look, 0.5, Ss(u, Times.Release, Times.Release + 0.5));
        look = Lerp(look, toDrone, Ss(u, Times.RiseFrom - 0.4, Times.RiseFrom + 0.2));
        return Math.Clamp(look + 0.07 * Math.Sin(u * 1.7 + 1) * Dizzy(u), -Poses.Head.Up, Poses.Head.Down);
    }

    /// <summary>How far into the last breath the screen goes at <paramref name="u"/>: up while the blood is drawn, easing as the hero stands; never ink.</summary>
    public static double Dizzy(double u) => Strong * Ss(u, 1.6, 3.0) * (1 - Ss(u, Times.Stand, End));

    private static double Up(double u) => Ss(u, Times.Stand, End);

    /// <summary>Whether the hero is held by the meeting at <paramref name="u"/>: from the press to the end, nothing cancels it.</summary>
    public static bool Holds(double u) => u >= 0 && u < End;

    // ---------- the core, the drone ----------

    /// <summary>Whether the core still lies in its heap, its picture and its glow shown: till it lifts free as its soot rises.</summary>
    public static bool CoreLies(double u) => u < Times.LiftFrom;

    /// <summary>Whether the meeting draws the drone, the hero's own hidden: till it rises to the head.</summary>
    public static bool DrawsDrone(double u) => u < Times.RiseFrom;

    /// <summary>Whether the meeting draws the lifted core as the drone forming, its soot and plates on it: till the hero's own takes over.</summary>
    public static bool FormsDrone(double u) => u >= Times.LiftFrom && DrawsDrone(u);

    /// <summary>
    /// Whether the lying core's picture is gone for this player: the core lost, or this player paired or <paramref name="taken"/> it in
    /// their own meeting, while no meeting still shows it lying (<paramref name="held"/>).
    /// </summary>
    public static bool CoreGone(bool lost, bool paired, bool taken, bool held) => lost || (paired || taken) && !held;

    /// <summary>How far the core has woken, 0..1: its cracks spreading, then turning ember.</summary>
    public static double Wake(double u) => 0.5 * Ss(u, Times.CrackFrom, Times.CrackTo) + 0.5 * Ss(u, Times.EmberFrom, Times.EmberTo);

    /// <summary>How far the core has lifted free of its soot, and risen to the head, 0..1.</summary>
    public static double Lift(double u) => Ss(u, Times.LiftFrom, Times.LiftTo);

    public static double Rise(double u) => Ss(u, Times.RiseFrom, Times.RiseTo);

    /// <summary>How far the soot has risen into the drone's cloud, 0..1; formed at 1.</summary>
    public static double Form(double u) => Math.Clamp((u - Times.FormFrom) / (Times.FormTo - Times.FormFrom), 0, 1);

    /// <summary>Where the core hangs once lifted, from where it lay: just over it, a little away from the hero.</summary>
    public static Vec Hover(Vec core, int side) => new(core.X - side * 2 * Px, core.Y - 41.5 * Px);

    /// <summary>
    /// Where the core, then the drone, is at <paramref name="u"/>: lying, lifting free to hang, bobbing (<paramref name="clockS"/>),
    /// then rising along a curve to <paramref name="idle"/>, its place by the head, there at the rise's end.
    /// </summary>
    public static Vec DroneAt(double u, Vec core, int side, Vec idle, double clockS)
    {
        double lift = Lift(u), rise = Rise(u);
        var hover = Hover(core, side);
        hover = hover with { Y = hover.Y + Math.Sin(clockS * 1.7) * 1.5 * Px * lift };
        return rise > 0 ? Bez(hover, new Vec(hover.X + side * 4 * Px, idle.Y + 36 * Px), idle, rise) : Lerp(core, hover, lift);
    }

    /// <summary>The way the lens faces, radians: as it lay, turning to the helmet at <paramref name="helm"/> as it lifts, then the drone's own way, <paramref name="drone"/>, as it rises.</summary>
    public static double AngleOf(double u, Vec at, Vec helm, double drone)
    {
        double a = AngLerp(0, Math.Atan2(helm.Y - at.Y, helm.X - at.X), Lift(u));
        return AngLerp(a, drone, Ss(u, Times.RiseFrom, Times.RiseFrom + 0.7));
    }

    /// <summary>How lit the lens is, 0..1, and its glow; its flare spent by <see cref="Times.LensFlare"/>.</summary>
    public static (double On, double Glow) Lens(double u) =>
        (Ss(u, Times.LensFrom, Times.LensTo), 0.35 + 0.5 * Bump(u, Times.LensFrom, Times.LensTo, Times.LensFlare));

    /// <summary>How far the lit lens has gone from the pick's colour to the drone's own, so the hand-over at the rise shows no jump.</summary>
    public static double LensShift(double u) => Ss(u, Times.LensFlare, Times.RiseFrom);

    /// <summary>The ember halo over the core, its alpha: breathing (<paramref name="breath"/>, 0.7..1), brighter as it wakes, gone as the soot forms.</summary>
    public static double Halo(double u, double breath) => 0.55 * breath * (1 - Ss(u, Times.FormFrom, Times.FormTo)) * (1 + 0.7 * Wake(u));

    /// <summary>The ember halo's breath at <paramref name="clockS"/>, as <c>CoreGlow</c> breathes, 0.7..1.</summary>
    public static double Breath(double clockS) => 0.7 + 0.3 * Math.Sin(clockS / 2.6 * 2 * Math.PI);

    /// <summary>The share of the halo the lying core's glow adds while it wakes, over its own breath.</summary>
    public static double GlowMore(double u) => 0.7 * Wake(u);

    /// <summary>The red glow round the core while it drinks, its alpha.</summary>
    public static double DrinkGlow(double u) => 0.4 * Bump(u, 2.5, 4.0, 7.5);

    /// <summary>The blood soaked into the core's crust, its alpha.</summary>
    public static double Soak(double u) => 0.38 * Ss(u, 2.3, 4.0) * (1 - Ss(u, 7.3, Times.FormFrom));

    /// <summary>The cracks' colour at <paramref name="u"/>, red turning ember, 0..255, and their alpha, gone as the soot forms.</summary>
    public static ((double R, double G, double B) Colour, double Alpha) CrackLook(double u)
    {
        double k = Ss(u, Times.EmberFrom, Times.EmberTo);
        var colour = (Lerp(CrackRed.R, Ember.R, k), Lerp(CrackRed.G, Ember.G, k), Lerp(CrackRed.B, Ember.B, k));
        return (colour, (0.75 + 0.25 * k) * (1 - Ss(u, Times.FormFrom, Times.FormTo)));
    }

    // ---------- blood ----------

    private readonly record struct DropSeed(int Orbit, double Phase, double Wide, double Size, double BurstWay, double BurstReach, double Out, double Sink);

    private static readonly DropSeed[] Ring = RingOf();

    /// <summary>How many drops the ring holds.</summary>
    public static int Drops => Ring.Length;

    /// <summary>
    /// Drop <paramref name="i"/> at <paramref name="u"/>: burst from the palm at <paramref name="palm"/>, drawn into the ring about
    /// the core at <paramref name="core"/>, circling as it grows, tightens and soaks in; none before it bursts or once it has sunk.
    /// </summary>
    public static BloodDrop? DropAt(int i, double u, Vec palm, Vec core, int side)
    {
        var d = Ring[i];
        if (u < d.Out || u > d.Sink + 0.3) return null;
        double k = u - Times.BurstFrom, th = d.Phase + Spin * k + SpinUp * k * k, tilt = -0.3 + d.Orbit * 1.05;
        double rx = d.Wide * Lerp(Lerp(Lerp(RingFirst, RingWide, Ss(u, Grow.From, Grow.To)), RingTight, Ss(u, Tighten.From, Tighten.To)), RingSunk,
            Ss(u, d.Sink, d.Sink + 0.3));
        Vec Orbit(double a)
        {
            var (x, y) = Rot(Math.Cos(a) * rx, Math.Sin(a) * rx * 0.3, tilt);
            return new Vec(core.X + side * x * Px, core.Y + y * Px);
        }
        double b = Ss(u, d.Out, d.Out + 0.25), m = Ss(u, d.Out + 0.15, d.Out + 0.5);
        var burst = new Vec(palm.X + side * Math.Cos(d.BurstWay) * d.BurstReach * b * Px, palm.Y + Math.Sin(d.BurstWay) * d.BurstReach * b * Px);
        return new BloodDrop(Lerp(burst, Orbit(th), m), Lerp(burst, Orbit(th - 0.32), m), m <= 0.5 || Math.Sin(th) >= 0,
            1 - Ss(u, d.Sink + 0.1, d.Sink + 0.3), d.Size * Px);
    }

    /// <summary>How many drops are out at <paramref name="u"/>.</summary>
    public static int DropsOut(double u, Vec palm, Vec core, int side)
    {
        int n = 0;
        for (int i = 0; i < Ring.Length; i++)
            if (DropAt(i, u, palm, core, side) != null) n++;
        return n;
    }

    // ---------- the cracks ----------

    private readonly record struct CrackSeed(Vec[] Points, double At);

    private static readonly CrackSeed[] Cracks = CracksOf();

    /// <summary>How many cracks open over the core; how many points a crack's drawn part has at most.</summary>
    public static int CrackCount => Cracks.Length;
    public const int CrackPoints = 4;

    /// <summary>
    /// The part of crack <paramref name="k"/> open at <paramref name="u"/>, the nearer the palm the sooner, written into
    /// <paramref name="into"/> in world px over the core at <paramref name="at"/> turned <paramref name="angle"/>; how many points.
    /// </summary>
    public static int CrackAt(int k, double u, Vec at, double angle, int side, Span<Vec> into)
    {
        var crack = Cracks[k];
        double grown = Math.Clamp((Ss(u, Times.CrackFrom, Times.CrackTo) - crack.At) / 0.35, 0, 1);
        if (grown <= 0) return 0;
        int n = Part(crack.Points, grown, into);
        for (int i = 0; i < n; i++)
        {
            var (x, y) = Rot(side * into[i].X, into[i].Y, angle);
            into[i] = new Vec(at.X + x * Px, at.Y + y * Px);
        }
        return n;
    }

    // ---------- the soot ----------

    private static readonly HeapMote[] Heap = HeapOf();
    private static readonly (double Delay, double X, double Y)[] Rises = RisesOf();

    /// <summary>How many motes lie in the heap; how many rise, the heap's and those hidden in it.</summary>
    public static int HeapCount => Heap.Length;
    public static int Motes => Rises.Length;

    /// <summary>Heap mote <paramref name="j"/>, from the lying core's picture's top-left, world px.</summary>
    public static HeapMote HeapAt(int j) => Heap[j];

    /// <summary>How far mote <paramref name="i"/> has risen from the heap to its orbit, 0..1: each a little after the last.</summary>
    public static double MoteRise(int i, double u) => Sm((Form(u) - Rises[i].Delay) / 0.45);

    /// <summary>
    /// Mote <paramref name="i"/> on its way up from the heap of the core at <paramref name="core"/> to its orbit at <paramref name="home"/>,
    /// growing from its heap size to <paramref name="side"/>; none before it leaves.
    /// </summary>
    public static RisingMote? MoteAt(int i, double u, Vec core, Vec home, double side)
    {
        double e = MoteRise(i, u);
        if (e <= 0) return null;
        var heap = Heap[i % Heap.Length];
        var rise = Rises[i];
        var from = new Vec(core.X - InPicture.X + heap.At.X + rise.X * Px, core.Y - InPicture.Y + heap.At.Y + rise.Y * Px);
        var bend = new Vec(Lerp(from.X, home.X, 0.3), Math.Min(from.Y, home.Y) - 12 * Px);
        return new RisingMote(Bez(from, bend, home, e), Lerp(heap.Side, side, e), e < 0.5, i >= Heap.Length ? Math.Min(1, e * 3) : 1);
    }

    // ---------- the plates ----------

    /// <summary>Where each plate floats among the debris right of the ship, pick px from the core, its first turn, its spin, its tumble's phase.</summary>
    private static readonly (double X, double Y, double Turn, double Spin, double Phase)[] Debris =
    [
        (248, -155.5, 0.4, 0.11, 0), (342, -259.5, 1.7, -0.09, 1.9), (448, -103.5, 3.0, 0.13, 3.8), (541, -205.5, 4.3, -0.12, 5.7),
    ];

    /// <summary>How many plates fly in.</summary>
    public static int Plates => Debris.Length;

    /// <summary>When plate <paramref name="i"/> leaves the debris: one at a time, a beat between.</summary>
    public static double PlateLeaves(int i) => Times.PlatesFrom + i * Times.PlateGap;

    /// <summary>How the plates out among the debris fade in as the meeting begins, 0..1.</summary>
    public static double DebrisIn(double u) => Ss(u, Times.KneelFrom, Times.KneelTo);

    /// <summary>Plate <paramref name="i"/> floating among the debris at <paramref name="clockS"/>: where, its turn and its tumble.</summary>
    public static (Vec At, double Turn, double Flip) DebrisAt(int i, double clockS, Vec core)
    {
        var d = Debris[i];
        return (new Vec(core.X + (d.X + Math.Sin(clockS * 0.21 + i * 1.7) * 7) * Px, core.Y + (d.Y + Math.Cos(clockS * 0.17 + i * 2.3) * 5) * Px),
            d.Turn + clockS * d.Spin, Math.Cos(clockS * 0.3 + d.Phase));
    }

    /// <summary>
    /// Plate <paramref name="i"/> at <paramref name="u"/>: out among the debris, then snapping home straight and fast to its place
    /// <paramref name="home"/> on the drone at <paramref name="drone"/> (<see cref="DroneSwarm.PlateIn"/>, idle), then there.
    /// </summary>
    public static MeetingPlate PlateOf(int i, double u, double clockS, Vec core, Vec drone, Plate home)
    {
        double t0 = PlateLeaves(i);
        if (u < t0)
        {
            var (at, turn, flip) = DebrisAt(i, clockS, core);
            return new MeetingPlate(PlateStage.Away, at, turn, flip, true);
        }
        var to = new Vec(drone.X + home.X, drone.Y + home.Y);
        double e = Math.Clamp((u - t0) / Times.PlateFlight, 0, 1);
        e *= e;
        if (e >= 1) return new MeetingPlate(PlateStage.Home, to, home.Turn, home.Flip, home.Front);
        var (left, leftTurn, leftFlip) = DebrisAt(i, clockS - (u - t0), core);
        return new MeetingPlate(PlateStage.Flying, Lerp(left, to, e), AngLerp(leftTurn, home.Turn, e), Lerp(leftFlip, home.Flip, e), home.Front);
    }

    /// <summary>The ember arc drawing plate <paramref name="i"/> home, its alpha: struck just before it leaves, gone just after it lands.</summary>
    public static double ArcAlpha(int i, double u)
    {
        double t0 = PlateLeaves(i);
        return Bump(u, t0 - 0.14, t0 - 0.04, t0 + 0.28);
    }

    /// <summary>The flash where plate <paramref name="i"/> lands, its alpha.</summary>
    public static double LandFlash(int i, double u)
    {
        double landed = PlateLeaves(i) + Times.PlateFlight;
        return u >= landed ? 0.7 * (1 - Ss(u, landed, landed + 0.25)) : 0;
    }

    /// <summary>
    /// A flickering ember arc from <paramref name="from"/> to <paramref name="to"/>, its jags drawn from <paramref name="seed"/>,
    /// written into <paramref name="into"/> (41 points at most); how many points, none for one too short to see.
    /// </summary>
    public static int Arc(Vec from, Vec to, int seed, Span<Vec> into)
    {
        double d = Math.Sqrt((to.X - from.X) * (to.X - from.X) + (to.Y - from.Y) * (to.Y - from.Y)) / Px;
        if (d < 2) return 0;
        var r = new Xorshift(seed);
        int n = Math.Max(4, Math.Min(40, (int)Math.Floor(d / 7 + 0.5)));
        double nx = -(to.Y - from.Y) / Px / d, ny = (to.X - from.X) / Px / d, amp = Math.Min(6, d * 0.08) * Px;
        into[0] = from;
        for (int j = 1; j < n; j++)
        {
            double e = (double)j / n, o = (r.Next() - 0.5) * 2 * amp * Math.Sin(e * Math.PI);
            into[j] = new Vec(Lerp(from.X, to.X, e) + nx * o, Lerp(from.Y, to.Y, e) + ny * o);
        }
        into[n] = to;
        return n + 1;
    }

    /// <summary>The seeds of plate <paramref name="i"/>'s two arcs at <paramref name="u"/>: fresh jags 30 times a second.</summary>
    public static (int First, int Second) ArcSeeds(int i, double u)
    {
        int frame = (int)Math.Floor(u * 30);
        return (frame * 7 + i * 131 + 1, frame * 11 + i * 71 + 5);
    }

    // ---------- the pick's seeds and pen ----------

    /// <summary>The ring's drops, as the pick's <c>ringOf</c> drew them from its xorshift, seed 311, in its order.</summary>
    private static DropSeed[] RingOf()
    {
        var r = new Xorshift(311);
        var drops = new DropSeed[DropCount];
        for (int j = 0; j < drops.Length; j++)
        {
            double phase = r.Next() * 6.283, wide = 0.8 + r.Next() * 0.4, size = 0.8 + r.Next() * 0.7, way = -2.4 + r.Next() * 2.9, reach = 7 + r.Next() * 9;
            double at = Lerp(Times.BurstFrom, Times.BurstTo, r.Next()), sink = Lerp(Times.SinkFrom, Times.SinkTo, r.Next());
            drops[j] = new DropSeed(j % 3, phase, wide, size, way, reach, at, sink);
        }
        return drops;
    }

    /// <summary>The pick's short pen cracks over the crust, core pixels, seed 91, each opening the later the further from the palm's touch.</summary>
    private static CrackSeed[] CracksOf()
    {
        var r = new Xorshift(91);
        double contact = Math.Atan2(-6.7, 4);
        var cracks = new CrackSeed[9];
        for (int i = 0; i < cracks.Length; i++)
        {
            double a = r.Next() * 6.283, d = 1 + r.Next() * 3.4, x = Math.Cos(a) * d, y = Math.Sin(a) * d, dir = r.Next() * 6.283;
            var points = new Vec[4];
            points[0] = new Vec(x, y);
            for (int j = 1; j < points.Length; j++)
            {
                dir += (r.Next() - 0.5) * 1.4;
                double length = 0.8 + r.Next() * 1.2;
                x += Math.Cos(dir) * length;
                y += Math.Sin(dir) * length;
                double m = Math.Sqrt(x * x + y * y);
                if (m > 4.9) (x, y) = (x * 4.9 / m, y * 4.9 / m);
                points[j] = new Vec(x, y);
            }
            double apart = Math.Abs(a - contact) % 6.283;
            if (apart > Math.PI) apart = 6.283 - apart;
            cracks[i] = new CrackSeed(points, 0.65 * apart / Math.PI);
        }
        return cracks;
    }

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

    /// <summary>Each of the cloud's 170 motes, seed 950: how late in the forming it leaves; the second 90 hidden in the heap a little off their mote.</summary>
    private static (double, double, double)[] RisesOf()
    {
        var r = new Xorshift(950);
        var rises = new (double, double, double)[DroneSwarm.Tune.Motes];
        for (int i = 0; i < rises.Length; i++)
        {
            double delay = r.Next() * 0.55;
            rises[i] = i >= 90 ? (delay, (r.Next() - 0.5) * 5, (r.Next() - 0.5) * 2.5) : (delay, 0, 0);
        }
        return rises;
    }

    /// <summary>The pick's <c>sub</c>: the polyline <paramref name="points"/> from its start to <paramref name="share"/> of its length.</summary>
    private static int Part(Vec[] points, double share, Span<Vec> into)
    {
        Span<double> lengths = stackalloc double[points.Length];
        lengths[0] = 0;
        for (int i = 1; i < points.Length; i++)
            lengths[i] = lengths[i - 1] + Math.Sqrt((points[i].X - points[i - 1].X) * (points[i].X - points[i - 1].X) + (points[i].Y - points[i - 1].Y) * (points[i].Y - points[i - 1].Y));
        double total = lengths[^1] > 0 ? lengths[^1] : 1, b = share * total;
        if (b <= 0) return 0;
        int n = 0;
        into[n++] = points[0];
        for (int i = 1; i < points.Length - 1; i++)
            if (lengths[i] > 0 && lengths[i] < b) into[n++] = points[i];
        int k = 1;
        while (k < points.Length - 1 && lengths[k] < b) k++;
        double seg = lengths[k] - lengths[k - 1];
        into[n++] = Lerp(points[k - 1], points[k], Math.Clamp((b - lengths[k - 1]) / (seg > 0 ? seg : 1), 0, 1));
        return n;
    }

    /// <summary>The pick kit's xorshift: the same draws from the same seed as its pages and drone.js.</summary>
    private sealed class Xorshift(int seed)
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

    private static double Sm(double x)
    {
        x = Math.Clamp(x, 0, 1);
        return x * x * (3 - 2 * x);
    }

    private static double Ss(double u, double a, double b) => Sm((u - a) / (b - a));

    private static double Bump(double u, double a, double b, double d) => Ss(u, a, b) * (1 - Ss(u, b, d));

    private static double Lerp(double a, double b, double k) => a + (b - a) * k;

    private static Vec Lerp(Vec a, Vec b, double k) => new(Lerp(a.X, b.X, k), Lerp(a.Y, b.Y, k));

    private static Vec Bez(Vec a, Vec b, Vec d, double s)
    {
        double k = 1 - s;
        return new Vec(k * k * a.X + 2 * k * s * b.X + s * s * d.X, k * k * a.Y + 2 * k * s * b.Y + s * s * d.Y);
    }

    private static (double X, double Y) Rot(double x, double y, double a) => (x * Math.Cos(a) - y * Math.Sin(a), x * Math.Sin(a) + y * Math.Cos(a));

    private static double AngLerp(double a, double b, double e)
    {
        double d = b - a;
        while (d > Math.PI) d -= 2 * Math.PI;
        while (d < -Math.PI) d += 2 * Math.PI;
        return a + d * e;
    }
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
