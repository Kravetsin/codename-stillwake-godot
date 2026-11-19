using System;

namespace Stillwake.Game.Shared;

/// <summary>
/// The drone's soot framing a drone window (drone-frame, the pick «Угли»'s <c>placeFrame</c>, its <c>ember</c> charge and
/// <c>stepFx</c>): opened, its motes leave the drone and stream to the outline's near corner and round it, each to its spot, a span
/// linking two once both have landed; while it is open the spans breathe and a crackle runs along one now and then; closed, the
/// spans break and the motes fly home. A switch mid-run carries each mote on from where it is. The drone's own run
/// (<c>window: false</c>) only times the crackle on its arcs, while no window is out. Screen px and seconds; client only.
/// </summary>
public sealed class SootRun
{
    /// <summary>The pick's numbers: screen px, seconds; the drone's side in world px.</summary>
    public static class Tune
    {
        /// <summary>How long the soot takes to frame a window, and to fly home.</summary>
        public const double OpenS = 0.3, CloseS = 0.2;

        /// <summary>The gap is the outline's length over the share, held between the least and the most.</summary>
        public const float Share = 80, GapMin = 14, GapMax = 32;

        /// <summary>How far each mote nearer the corner trails the one before it, px; the share of the opening by which the head has gone round.</summary>
        public const float Stagger = 6, HeadAt = 0.88f;

        /// <summary>A landed mote's side and its dark rim past it.</summary>
        public const float Mote = 2.3f, MoteRim = 1;

        /// <summary>The take-off's lift, at most and as a share of the flight; the flight home's lift.</summary>
        public const float Lift = 60, LiftShare = 0.22f, HomeLift = 24;

        /// <summary>A landed mote's wobble about its spot, and its two paces, radians a second.</summary>
        public const float Wobble = 0.35f, WobblePaceX = 0.9f, WobblePaceY = 0.7f;

        /// <summary>The flight home: the share of the close the farthest mote has left of it, and how much less the nearest.</summary>
        public const float HomeBy = 0.9f, HomeNearer = 0.3f;

        /// <summary>A hot mote's flicker: its middle, swing and pace.</summary>
        public const float HotMid = 0.45f, HotSwing = 0.35f, HotPace = 4;

        /// <summary>A span lights over this long once both its ends have landed.</summary>
        public const double LinkIn = 0.05;

        /// <summary>A span's flash as it links: how fast it fades, and when it is gone.</summary>
        public const double FlashFade = 0.16, FlashFor = 0.8;

        /// <summary>A span breaks over this long on a close, flashing this much.</summary>
        public const double Break = 0.08;
        public const float BreakFlash = 0.7f;

        /// <summary>The drone's arcs' breath: its alpha at least, how much more at most; a window span's flash's share.</summary>
        public const float BreathLow = 0.14f, BreathSwing = 0.24f, BreathFlash = 0.6f;

        /// <summary>
        /// A window's frame glows whole, <see cref="GlowBase"/> everywhere, and embers' light runs round it: each runner a soft
        /// swell <see cref="GlowPeak"/> more at its middle, <see cref="GlowWidth"/> px to either side, at its own pace (px a
        /// second, the sign its way) from its own start (a share of the outline). Played in game 2026-11-19: spans breathing
        /// each on its own read as loose sticks.
        /// </summary>
        public const float GlowBase = 0.45f, GlowPeak = 0.45f, GlowWidth = 200;
        public static readonly (float Pace, float From)[] Runners = [(42, 0), (-27, 0.37f), (61, 0.69f)];

        /// <summary>A span's core width, its halo's width and alpha, the core's paleness.</summary>
        public const float SpanWidth = 1.6f, SpanHalo = 7f, SpanHaloAlpha = 0.3f, SpanPale = 0.2f;

        /// <summary>How often a crackle comes, least and most; the first's share of the least after its place changes.</summary>
        public const double CrackleMin = 4, CrackleMax = 8, CrackleFirst = 0.6;

        /// <summary>How long a crackle lasts.</summary>
        public const double CrackleLife = 0.13;

        /// <summary>A crackle's jag: its sideways swing and bow, px; its segments; how often a second it is drawn anew.</summary>
        public const float ArcAmp = 2.4f, ArcBow = 2;
        public const int ArcSegments = 5, ArcReseed = 40;

        /// <summary>A crackle's flicker: its low and depth, its pace.</summary>
        public const float ArcFlickerLow = 0.75f, ArcFlicker = 0.25f, ArcFlickerPace = 90;

        /// <summary>A crackle's core and halo widths, its alpha; the halos at its ends, their radius and alpha.</summary>
        public const float ArcCore = 1.2f, ArcHalo = 5, ArcAlpha = 0.95f, ArcEnd = 9, ArcEndAlpha = 0.45f;

        /// <summary>The glass: when it starts to show, and over what share it fades in opening and out closing.</summary>
        public const double GlassFrom = 0.3, GlassOpening = 0.55, GlassClosing = 0.6;

        /// <summary>The contents: when they start to show opening, and over what share.</summary>
        public const double ContentsFrom = 0.5, ContentsFor = 0.4;

        /// <summary>Closing, the window takes clicks only while it is out further than this.</summary>
        public const double ClicksClosing = 0.6;

        /// <summary>The film's alpha at rest and its more before the glass comes; how long it takes to go on a close.</summary>
        public const float Film = 0.02f, FilmMore = 0.06f;
        public const double FilmGoes = 0.1;

        /// <summary>The film's gradient: its pale start's paleness and share of alpha, its middle's share; how far across it slants.</summary>
        public const float FilmPale = 0.3f, FilmTop = 1.6f, FilmMid = 0.5f, FilmSlant = 0.6f;

        /// <summary>The spans' ember, rgb.</summary>
        public const string Ember = "ff8a3a";

        /// <summary>World px per unit of the pick's drawing of the drone (<c>DroneSwarm</c>'s quarter).</summary>
        public const float Ash = 0.25f;

        /// <summary>How many seeds the drone's soot has: a spot past them takes mote <c>i % Seeds</c>'s.</summary>
        public const int Seeds = 170;

        /// <summary>The drone's arcs, one from its core to each plate: what its crackle runs on.</summary>
        public const int DroneSpans = 4;

        /// <summary>
        /// The drone's side, world px: an arc's core and halo widths, its start off the core's middle; the crackle's swing, bow, core
        /// and halo widths, its end halos' radius.
        /// </summary>
        public const float DroneSpan = 0.25f, DroneHalo = 0.97f, DroneArcFrom = 1.15f;
        public const float DroneArcAmp = 0.67f, DroneArcBow = 0.56f, DroneArcCore = 0.33f, DroneArcHalo = 1.39f, DroneArcEnd = 3.06f;

        /// <summary>
        /// A mote carried on from where it was after a switch: how fast the gap closes, its least pace, px a second, and its most
        /// as a share of the head's fastest, so it never outruns a plain run.
        /// </summary>
        public const double CarryFade = 0.06, CarryLeast = 240, CarryMost = 0.4;
    }

    /// <summary>The pick's seeds for the drone's soot (<c>DR.P</c>, seed 530): phase, place, side in its units, hot, tone.</summary>
    private static readonly (float Phase, float R1, float Size, bool Hot, bool Dark)[] Seeds = DrawSeeds();

    private readonly bool _window;
    private readonly Random _random;
    private readonly SootFade _fade = new();
    private double _t, _brokeAt = -1;
    private SootSpots? _spots;
    private bool _open, _hadFrom, _carrying;
    private float _alpha = 1, _film;
    private float[] _x = [], _y = [], _size = [], _carryX = [], _carryY = [], _strength = [], _flash = [];
    private double[] _landed = [];
    private bool[] _atSpot = [];
    private bool _crackles;
    private double _nextCrackle;
    private (int Span, double At, int Seed)? _crackle;

    public SootRun(bool window, Random random) => (_window, _random) = (window, random);

    /// <summary>Its clock, seconds since it began.</summary>
    public double Time => _t;

    /// <summary>How far out the soot is, the run's share: 0 home, 1 framing the window.</summary>
    public double Out => _fade.P;

    /// <summary>Whether any soot is out or on its way.</summary>
    public bool Busy => _open || _fade.P > 0 || _carrying;

    /// <summary>How many motes, and spans, it has.</summary>
    public int Count => _x.Length;

    /// <summary>Where mote <paramref name="i"/> is.</summary>
    public float X(int i) => _x[i];
    public float Y(int i) => _y[i];

    /// <summary>Mote <paramref name="i"/>'s side.</summary>
    public float Size(int i) => _size[i];

    /// <summary>Whether mote <paramref name="i"/> has landed on its spot this opening.</summary>
    public bool Landed(int i) => !double.IsNaN(_landed[i]);

    /// <summary>How linked span <paramref name="j"/> is, 0..1, and its flash.</summary>
    public float Strength(int j) => _strength[j];
    public float Flash(int j) => _flash[j];

    /// <summary>How red-hot mote <paramref name="i"/> is, 0 for a cold one; whether it is of the darker tone.</summary>
    public float Heat(int i)
    {
        var seed = Seeds[i % Tune.Seeds];
        return seed.Hot ? Tune.HotMid + Tune.HotSwing * MathF.Sin((float)_t * Tune.HotPace + seed.Phase * 7) : 0;
    }

    public bool Dark(int i) => Seeds[i % Tune.Seeds].Dark;

    /// <summary>The soot's alpha: 1, or fading in and out on the outline with no drone drawn.</summary>
    public float Alpha => _alpha;

    /// <summary>The film's alpha between the landed motes; 0 with none.</summary>
    public float Film => _film;

    /// <summary>The crackle playing: on which span, how long it has, its seed.</summary>
    public (int Span, float Age, int Seed)? Crackle => _crackle is { } c ? (c.Span, (float)(_t - c.At), c.Seed) : null;

    /// <summary>Whether there is anything to draw: soot out or on its way. Once home, nothing; the drone shows its own.</summary>
    public bool Shows => Busy && Count > 0;

    /// <summary>The frame's glow at mote <paramref name="i"/>, its alpha before a flash (<see cref="Glow(SootSpots, int, double)"/>).</summary>
    public float Glow(int i) => Glow(_spots!, i, _t);

    /// <summary>
    /// The frame's glow at spot <paramref name="i"/> of <paramref name="spots"/> at <paramref name="t"/>: the base everywhere and
    /// the runners' swells passing, so neighbours glow alike and the light runs round the whole frame.
    /// </summary>
    public static float Glow(SootSpots spots, int i, double t)
    {
        float p = spots.Perimeter, s = spots.Round(i), glow = Tune.GlowBase;
        if (p <= 0) return glow;
        foreach (var (pace, from) in Tune.Runners)
        {
            float at = (float)(((from * p + pace * t) % p + p) % p), d = MathF.Abs(s - at);
            d = MathF.Min(d, p - d) / Tune.GlowWidth;
            glow += Tune.GlowPeak * MathF.Exp(-d * d);
        }
        return glow;
    }

    /// <summary>A span's breath at <paramref name="t"/>, from its own <paramref name="phase"/>, 0..1: each at its own pace.</summary>
    public static float Breathe(float phase, double t)
    {
        float b = 0.5f + 0.5f * MathF.Sin((float)t * (0.4f + 0.5f * phase) + phase * 40);
        return Tune.BreathLow + Tune.BreathSwing * b * b;
    }

    /// <summary>A crackle's alpha <paramref name="age"/> into it, at <paramref name="t"/>: fading, flickering.</summary>
    public static float ArcAlpha(int seed, float age, double t) =>
        (float)(1 - age / Tune.CrackleLife) * (Tune.ArcFlickerLow + Tune.ArcFlicker * MathF.Sin(seed + (float)t * Tune.ArcFlickerPace));

    /// <summary>
    /// A crackle's jagged path from a to b into <paramref name="points"/> (<see cref="Tune.ArcSegments"/> + 1 of them), drawn anew
    /// <see cref="Tune.ArcReseed"/> times a second: the pick's <c>jagTo</c>.
    /// </summary>
    public static void Jag(float ax, float ay, float bx, float by, int seed, double t, float amp, float bow, Span<(float X, float Y)> points)
    {
        var draw = new SootRng(unchecked((uint)(seed + (int)Math.Floor(t * Tune.ArcReseed))));
        float dx = bx - ax, dy = by - ay, length = MathF.Sqrt(dx * dx + dy * dy);
        if (length == 0) length = 1;
        float nx = -dy / length, ny = dx / length;
        int k = points.Length - 1;
        points[0] = (ax, ay);
        for (int i = 1; i < k; i++)
        {
            float u = i / (float)k, off = (draw.Next() - 0.5f) * 2 * amp + bow * MathF.Sin(MathF.PI * u);
            points[i] = (ax + dx * u + nx * off, ay + dy * u + ny * off);
        }
        points[k] = (bx, by);
    }

    /// <summary>
    /// Steps it <paramref name="seconds"/> on. A window's run: <paramref name="open"/> or closing, round <paramref name="spots"/>
    /// placed at <paramref name="at"/>, flying from the drone at <paramref name="from"/>, screen px, null with no drone drawn
    /// (the soot fades in on the outline), <paramref name="zoom"/> screen px a world px. The drone's: <paramref name="open"/>
    /// says a window is out, and only its crackle is stepped.
    /// </summary>
    public void Step(double seconds, bool open, SootSpots? spots = null, (float X, float Y) at = default, (float X, float Y)? from = null, float zoom = 1)
    {
        _t += seconds;
        if (!_window)
        {
            StepCrackle(!open, Tune.DroneSpans);
            return;
        }
        spots ??= _spots;
        if (spots == null) return;
        int was = Count;
        bool switched = false;
        if (!ReferenceEquals(spots, _spots))
        {
            // A new outline: open, the soot frames it from the start; it keeps the old one's motes as far as they go.
            switched = _spots != null;
            if (open && _spots != null) _fade.Restart();
            _spots = spots;
            Resize(spots.Count);
        }
        if (open != _open)
        {
            switched = true;
            if (open)
            {
                Array.Fill(_landed, double.NaN);
                _brokeAt = -1;
            }
            else _brokeAt = _t;
            _open = open;
        }
        if ((from != null) != _hadFrom) switched = true;
        _hadFrom = from != null;
        // Where each is shown now, before it is placed anew: a switch carries each on from there.
        Span<float> wasX = switched ? new float[was] : default, wasY = switched ? new float[was] : default;
        if (switched)
            for (int i = 0; i < was; i++) (wasX[i], wasY[i]) = (_x[i], _y[i]);
        _fade.Step(seconds, open);
        float flight = Place(spots, at, from, zoom);
        Carry(switched, wasX, wasY, Math.Min(was, Count), seconds, flight);
        // A mote lands once it is on its spot, not carried there still.
        for (int i = 0; i < Count; i++)
            if (_atSpot[i] && !Carried(i) && double.IsNaN(_landed[i])) _landed[i] = _t;
        Link();
        float glass = _fade.Glass;
        _film = _open ? Tune.Film + Tune.FilmMore * (1 - glass) : Tune.Film * (float)Math.Clamp(1 - (_t - _brokeAt) / Tune.FilmGoes, 0, 1);
        if (_film <= 0.002f) _film = 0;
        StepCrackle(_open && _fade.P >= 1 && !_carrying, Count);
    }

    /// <summary>Every mote where its run puts it, before any carry; returns the head's fastest pace, px a second.</summary>
    private float Place(SootSpots spots, (float X, float Y) at, (float X, float Y)? from, float zoom)
    {
        float p = (float)_fade.P, t = (float)_t;
        var corner = (X: at.X + spots.Corner.X, Y: at.Y + spots.Corner.Y);
        var drone = from ?? corner;
        float flight = MathF.Max(1, MathF.Sqrt((corner.X - drone.X) * (corner.X - drone.X) + (corner.Y - drone.Y) * (corner.Y - drone.Y)));
        var lift = (X: (drone.X + corner.X) / 2, Y: (drone.Y + corner.Y) / 2 - MathF.Min(Tune.Lift, flight * Tune.LiftShare));
        float reach = spots.Reach, head = (flight + reach) * (1 - Square(1 - Math.Clamp(p / Tune.HeadAt, 0, 1)));
        _alpha = from == null ? Smooth(p) : 1;
        for (int i = 0; i < Count; i++)
        {
            var seed = Seeds[i % Tune.Seeds];
            var (ox, oy) = Orbit(i % Tune.Seeds, t);
            float hx = drone.X + ox * Tune.Ash * zoom, hy = drone.Y + oy * Tune.Ash * zoom, home = seed.Size * Tune.Ash * zoom;
            float sx = at.X + spots.Xs[i] + MathF.Sin(t * Tune.WobblePaceX + i * 1.7f) * Tune.Wobble;
            float sy = at.Y + spots.Ys[i] + MathF.Cos(t * Tune.WobblePaceY + i * 2.3f) * Tune.Wobble;
            float d = spots.Alongs[i];
            _atSpot[i] = false;
            if (from == null)
            {
                (_x[i], _y[i], _size[i], _atSpot[i]) = (sx, sy, Tune.Mote, _open);
            }
            else if (_open)
            {
                float ps = MathF.Min(flight + d, head - spots.Leads[i] * Tune.Stagger);
                if (ps <= 0) (_x[i], _y[i], _size[i]) = (hx, hy, home);
                else if (ps < flight)
                {
                    float u = ps / flight;
                    (_x[i], _y[i]) = Bezier(hx, hy, lift.X, lift.Y, corner.X, corner.Y, u);
                    _size[i] = home + (Tune.Mote - home) * u;
                }
                else if (ps >= flight + d - 1e-3f)
                {
                    (_x[i], _y[i], _size[i], _atSpot[i]) = (sx, sy, Tune.Mote, true);
                }
                else
                {
                    var (cx, cy) = spots.PointAt(spots.Ways[i] > 0 ? ps - flight : spots.Perimeter - (ps - flight));
                    (_x[i], _y[i], _size[i]) = (at.X + cx, at.Y + cy, Tune.Mote);
                }
            }
            else
            {
                float k = Smooth(Math.Clamp(p / (Tune.HomeBy - Tune.HomeNearer * d / reach), 0, 1));
                (_x[i], _y[i]) = Bezier(hx, hy, (hx + sx) / 2, (hy + sy) / 2 - Tune.HomeLift * k, sx, sy, k);
                _size[i] = home + (Tune.Mote - home) * k;
            }
        }
        return (float)((flight + reach) * 2 / (Tune.HeadAt * Tune.OpenS));
    }

    /// <summary>
    /// After a switch each mote is carried by the gap from where it was shown to where its new run puts it, the gap closing
    /// smoothly and never faster than a share of the head's <paramref name="headPace"/>.
    /// </summary>
    private void Carry(bool switched, Span<float> wasX, Span<float> wasY, int kept, double seconds, float headPace)
    {
        if (switched)
        {
            Array.Clear(_carryX);
            Array.Clear(_carryY);
            for (int i = 0; i < kept; i++) (_carryX[i], _carryY[i]) = (wasX[i] - _x[i], wasY[i] - _y[i]);
        }
        else if (_carrying)
        {
            double most = Tune.CarryMost * headPace * seconds;
            for (int i = 0; i < Count; i++)
            {
                float gap = MathF.Sqrt(_carryX[i] * _carryX[i] + _carryY[i] * _carryY[i]);
                if (gap == 0) continue;
                double shrink = Math.Min(most, Math.Max(gap * (1 - Math.Exp(-seconds / Tune.CarryFade)), Tune.CarryLeast * seconds));
                float k = gap - shrink < 0.25 ? 0 : (float)((gap - shrink) / gap);
                (_carryX[i], _carryY[i]) = (_carryX[i] * k, _carryY[i] * k);
            }
        }
        _carrying = false;
        for (int i = 0; i < Count; i++)
        {
            if (!Carried(i)) continue;
            _carrying = true;
            (_x[i], _y[i]) = (_x[i] + _carryX[i], _y[i] + _carryY[i]);
        }
    }

    private bool Carried(int i) => _carryX[i] != 0 || _carryY[i] != 0;

    /// <summary>Each span's strength and flash: linking once both its ends have landed, breaking on a close.</summary>
    private void Link()
    {
        int n = Count;
        for (int j = 0; j < n; j++)
        {
            double a = _landed[j], b = _landed[(j + 1) % n];
            float strength = 0, flash = 0;
            if (!double.IsNaN(a) && !double.IsNaN(b))
            {
                if (_open)
                {
                    double u = _t - Math.Max(a, b);
                    strength = (float)Math.Clamp(u / Tune.LinkIn, 0, 1);
                    flash = u < Tune.FlashFor ? (float)Math.Exp(-u / Tune.FlashFade) : 0;
                }
                else
                {
                    double u = (_t - _brokeAt) / Tune.Break;
                    strength = (float)Math.Max(0, 1 - u);
                    flash = u < 1 ? Tune.BreakFlash * (float)(1 - u) : 0;
                }
            }
            (_strength[j], _flash[j]) = (strength, flash);
        }
    }

    /// <summary>
    /// The crackle while <paramref name="steady"/>: the first <see cref="Tune.CrackleFirst"/> of the least wait after it becomes
    /// so, then one every <see cref="Tune.CrackleMin"/>–<see cref="Tune.CrackleMax"/> s on one of <paramref name="spans"/>; none otherwise.
    /// </summary>
    private void StepCrackle(bool steady, int spans)
    {
        if (steady != _crackles)
        {
            _crackles = steady;
            _nextCrackle = _t + Tune.CrackleMin * Tune.CrackleFirst;
            if (!steady) _crackle = null;
        }
        if (_crackle is { } c && _t - c.At >= Tune.CrackleLife) _crackle = null;
        if (!steady || spans <= 0 || _t < _nextCrackle) return;
        _crackle = (_random.Next(spans), _t, _random.Next(1_000_000));
        _nextCrackle = _t + Tune.CrackleMin + _random.NextDouble() * (Tune.CrackleMax - Tune.CrackleMin);
    }

    private void Resize(int n)
    {
        int keep = Math.Min(n, _x.Length);
        float[] Grown(float[] old)
        {
            var array = new float[n];
            Array.Copy(old, array, keep);
            return array;
        }
        (_x, _y, _size, _carryX, _carryY) = (Grown(_x), Grown(_y), Grown(_size), Grown(_carryX), Grown(_carryY));
        (_strength, _flash) = (new float[n], new float[n]);
        _landed = new double[n];
        Array.Fill(_landed, double.NaN);
        _atSpot = new bool[n];
    }

    /// <summary>Mote <paramref name="seed"/>'s place on the drone's idle orbits at <paramref name="t"/>, in the pick's units: its <c>idleMote</c>.</summary>
    private static (float X, float Y) Orbit(int seed, float t)
    {
        var s = Seeds[seed];
        int orbit = seed % 3;
        float tilt = -0.3f + orbit * 1.05f, way = orbit % 2 == 1 ? -1 : 1;
        float th = s.Phase + t * (0.8f + 0.25f * orbit) * way, r = 10 + orbit * 2.2f + (s.R1 - 0.5f) * 1.6f;
        float x = MathF.Cos(th) * r, y = MathF.Sin(th) * r * 0.3f;
        return (x * MathF.Cos(tilt) - y * MathF.Sin(tilt), x * MathF.Sin(tilt) + y * MathF.Cos(tilt));
    }

    private static (float X, float Y) Bezier(float ax, float ay, float bx, float by, float cx, float cy, float s)
    {
        float k = 1 - s;
        return (k * k * ax + 2 * k * s * bx + s * s * cx, k * k * ay + 2 * k * s * by + s * s * cy);
    }

    private static float Square(float x) => x * x;

    private static float Smooth(float u)
    {
        u = Math.Clamp(u, 0, 1);
        return u * u * (3 - 2 * u);
    }

    private static (float, float, float, bool, bool)[] DrawSeeds()
    {
        var draw = new SootRng(530);
        var seeds = new (float, float, float, bool, bool)[Tune.Seeds];
        for (int i = 0; i < seeds.Length; i++)
        {
            float phase = draw.Next() * 6.283f, r1 = draw.Next(), size = 1.4f + draw.Next() * 1.2f;
            bool hot = draw.Next() < 1 / 13f, dark = draw.Next() < 0.5f;
            seeds[i] = (phase, r1, size, hot, dark);
        }
        return seeds;
    }
}
