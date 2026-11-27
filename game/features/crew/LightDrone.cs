using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using I = Stillwake.Game.Shared.DroneInk;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The light drone (light-drone, drone-threads), the picked «Нити»: a coal core floating by the hero's head, an ember eye where it
/// looks, its body sparks running round three faint orbit threads, a share of them forming the mode's or the gesture's shape and
/// flowing along it (<see cref="DroneSwarm"/>). The core is a picture (<c>drone_core</c>, tools/anime-art drone.js); the sparks, the
/// threads, the coal's cracks and eye are drawn here as the pick drew them, in its player's salts (<see cref="Salts"/>), in its px (<see cref="DroneInk.Px"/> world px each).
/// Drawn unlit, in the world's frame; its light is the head lamp's.
/// </summary>
public partial class LightDrone : Node2D
{
    /// <summary>The core's picture's side, world px (drone.js's <c>CoreBox</c>).</summary>
    private const float CoreBox = 4;

    /// <summary>How many straight pieces an orbit thread is drawn in; a disc's sides.</summary>
    private const int OrbitPieces = 40, DiscSides = 10, HaloSides = 16;

    /// <summary>Its embers' hot orange (the picks' <c>EMBER</c>), a spark's dark.</summary>
    internal static readonly Color Ember = Rgb(I.Ember);
    private static readonly Color Dark = Rgb(I.Dark);

    private static Texture2D? _core;
    private readonly DroneFlight _flight = new();
    private readonly DroneSwarm _swarm = new();
    private readonly Spark[] _sparks = new Spark[I.Sparks];
    /// <summary>Its flurry while projecting (drone-windows), and its sparks this frame.</summary>
    private readonly DroneFlurry _flurry = new();
    private readonly Spark[] _flurrySparks = new Spark[DroneFlurry.Count];
    private double _elapsedMs;
    /// <summary>The reticle's marks in the world, and how far each one's dot is out from it (<see cref="Gauge"/>).</summary>
    private readonly (float X, float Y)[] _seats = new (float X, float Y)[I.Dots];
    private readonly float[] _away = new float[I.Dots];
    /// <summary>The core and the sparks' dark, mixed; the threads, tails and coal, added: both in pick px.</summary>
    private readonly Node2D _body = new(), _glow = new();
    private readonly Triangles _dark = new(I.Sparks * 4, I.Sparks * 6), _light = new(2048, 3072);
    /// <summary>How long it takes to turn most of the way to a new aim, in ms.</summary>
    private const double TurnMs = 90;

    /// <summary>Its bob as it flies, pixels up and down, and how long one takes, ms.</summary>
    private const double BobPx = 1, BobMs = 1800;

    /// <summary>The tow's picked looks (drone-tow.js, tug): the body tightened and the orbits hurried in a heave; the squint's shutting.</summary>
    private const float Tighter = 0.12f, Hurried = 2, SquintWide = 0.4f, SquintShut = 0.82f;
    /// <summary>The thread's widths in screen px of the pick's zoom, its alphas, its beads and their pace a second, and its end's knot.</summary>
    private const float ThreadDark = 2.4f, ThreadDarkAlpha = 0.4f, ThreadWide = 3.6f, ThreadWideAlpha = 0.07f, ThreadFine = 1.15f, ThreadFineAlpha = 0.72f, Knot = 2.1f, KnotHalo = 6;
    private const int Beads = 3;
    private const float BeadPace = 0.9f;
    /// <summary>What it puffs: how many sparks, how long they live, s, how fast they are left, px a second, and how wide, px.</summary>
    private const int Puffs = 34;
    private const float PuffLife = 0.3f, PuffPace = 55, PuffWide = 2.2f;
    private static readonly Color ThreadShade = new(8 / 255f, 10 / 255f, 14 / 255f, ThreadDarkAlpha);

    private readonly Triangles _towDark = new(Puffs * 2, Puffs * 3);
    private readonly Vector2[] _thread = new Vector2[DroneTow.Points];
    private DroneTow? _tow;
    private Vec _hold;
    private float _threadLength;
    private bool _threaded;
    /// <summary>The thread it hauls a thrown thing by (companion-fights): how it hangs as it is cast and buzzes taut, px.</summary>
    private const float HaulSag = 6, HaulBuzz = 0.7f;

    /// <summary>How far a haul drags it toward what it hauls, px, how much tighter it draws, and how long both ease, ms.</summary>
    private const float HaulLeanPx = 3, HaulTighter = 0.12f, HaulEaseMs = 50;

    private (Vec At, double Cast, double Effort)? _haul;
    private Vector2 _strain;

    /// <summary>Its core filling (drone-abilities): how full it shows and the embers flying in.</summary>
    private readonly CoreEmbers _embers = new();
    private readonly CoreEmber[] _emberAt = new CoreEmber[CoreEmbers.Most];

    /// <summary>Its core's charge and a full one's (drone-abilities), set before <see cref="Sign"/> each frame; none for no core shown filling.</summary>
    public (int Charge, int Full)? Charged { get; set; }

    /// <summary>Where its body is turned, and where its eye is: the eye alone glances back on a tow.</summary>
    private float _angle, _eye;
    private double _timeMs;
    /// <summary>How red it is now, 0..1 (<see cref="DroneIntent.Tint"/>).</summary>
    private float _tint;
    /// <summary>The thought whose picture it forms (drone-thoughts), how its mood moves its body, and its orbits' own clock, s, hurried or slowed by it.</summary>
    private DroneThought? _thought;
    private DroneBearing _bearing;
    private double _orbitLead;

    public LightDrone()
    {
        TopLevel = true;
        // Over the hero and what its near hand holds, as the other drones fly.
        (ZAsRelative, ZIndex) = (false, Render.Layers.Drones);
        Visible = false;
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
        foreach (var (node, adds) in new (Node2D, bool)[] { (_body, false), (_glow, true) })
        {
            node.Scale = new Vector2(I.Px, I.Px);
            node.Material = new CanvasItemMaterial
            {
                LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
                BlendMode = adds ? CanvasItemMaterial.BlendModeEnum.Add : CanvasItemMaterial.BlendModeEnum.Mix,
            };
            AddChild(node);
        }
        _body.Draw += DrawBody;
        _glow.Draw += DrawGlow;
    }

    /// <summary>
    /// Sends its sparks into the shape it owes now, after <see cref="Follow"/> each frame: the fan of the gesture playing on its hero
    /// (<see cref="DroneSign"/>; its motion, how far in, where a point points in world px), else its mode's.
    /// </summary>
    public void Sign((GestureMotion Motion, double Ms, Vector2 At)? gesture)
    {
        double t = _timeMs / 1000;
        var kind = gesture is { } playing ? DroneSign.KindOf(playing.Motion) : null;
        float? pointed = null;
        if (kind is { } shown && gesture is { } sign)
        {
            // A point at the drone itself heads the fan where the eye looks.
            var line = sign.At - GlobalPosition;
            if (line.Length() < 1) line = Vector2.FromAngle(_angle);
            pointed = DroneSign.Turned(shown, (line.X, line.Y));
        }
        _swarm.Become(DroneSwarm.ShapeOf(Projecting, kind != null, Scanning, Idle, thinks: _thought != null), _angle, t, pointed, _thought, ShapeOver);
        _swarm.Bear((float)_orbitLead, (float)_bearing.Tilt, (float)_bearing.Dim, 1 + (float)_bearing.Swell, (float)(_tow?.Flat ?? 1));
        foreach (var node in (ReadOnlySpan<Node2D>)[_body, _glow]) node.Scale = Vector2.One * I.Px * (1 + (float)_bearing.Swell);
        for (int i = 0; i < _sparks.Length; i++) _sparks[i] = _swarm.SparkAt(i, _angle, t);
        float turn = _swarm.TurnOf(SwarmShape.Scan, _angle, t);
        for (int j = 0; j < _seats.Length; j++)
        {
            var (x, y) = DroneSwarm.Seat(j, turn);
            _seats[j] = (GlobalPosition.X + x * I.Px, GlobalPosition.Y + y * I.Px);
        }
        if (Charged is (int charge, int full)) _embers.Step(charge, full, t, _elapsedMs / 1000);
        Threads(t);
        _flurry.Step(_elapsedMs / 1000, Projecting);
        for (int i = 0; i < _flurrySparks.Length; i++) _flurrySparks[i] = _flurry.At(i, t);
        _body.QueueRedraw();
        _glow.QueueRedraw();
    }

    /// <summary>How far it flies from its goal, pixels, as last followed.</summary>
    public double Off => _flight.Off;

    /// <summary>Where it flies, pixels of the world, without its bob.</summary>
    public Vec At => _flight.At;

    /// <summary>Whether it scans rather than shines: the scanner worn, not the drone.</summary>
    public bool Scanning { get; set; }

    /// <summary>Whether it neither shines nor scans: no mode of its open yet.</summary>
    public bool Idle { get; set; }

    /// <summary>Whether a drone window of its is out (its own or a friend's): it forms no shape meanwhile.</summary>
    public bool Projecting { get; set; }

    /// <summary>Its player's salts, the colour what leaves its coal burns into.</summary>
    public Salts Salts { get; set; } = Salts.Embers;

    /// <summary>
    /// Whether a script flies it (form-vision): put on its goal as given, not flown after it, and a mark whose dot is out never drawn as a spark,
    /// as the reticle falls apart too.
    /// </summary>
    public bool Scripted { get; set; }

    /// <summary>How long the shape it changes to takes, s; none: the shape's own time.</summary>
    public float? ShapeOver { get; set; }

    /// <summary>The box round its eye as drawn, in pixels of the world: where what it sends out flies from.</summary>
    public Rect2 LensBox
    {
        get
        {
            var eye = GlobalPosition + Vector2.FromAngle(_eye) * I.EyeAhead * I.Px;
            const float Half = I.Eye * 2 * I.Px;
            return new Rect2(eye.X - Half, eye.Y - Half, 2 * Half, 2 * Half);
        }
    }

    /// <summary>The reticle's marks as last formed, in pixels of the world: where the gauge's dots leave from and come home to, one a dot.</summary>
    public (float X, float Y)[] Seats => _seats;

    /// <summary>
    /// What the scanner's gauge does now, after the scanner's frame: whether it <paramref name="reads"/> a thing, the reticle opening as it does, and
    /// how far each dot is <paramref name="away"/> from its mark, 0..1: a mark whose dot is out is dim, its spark gone with the dot.
    /// </summary>
    public void Gauge(bool reads, ReadOnlySpan<float> away)
    {
        _swarm.Read(reads, _timeMs / 1000);
        away.CopyTo(_away);
    }

    /// <summary>How red an alarm turns it now, 0..1.</summary>
    public float Tint => _tint;

    /// <summary>
    /// Flies it by its hero's <paramref name="anchor"/>, moving at <paramref name="hero"/> px/ms, as its will wants
    /// (<paramref name="intent"/>), with no reach in a <paramref name="window"/>: its eye eased toward the look, red as the tint. Towing
    /// (<paramref name="tow"/>), it flies ahead instead, its thread to <paramref name="hold"/>, the shoulder as drawn. A thing thrown
    /// (<paramref name="haul"/>, companion-fights, how far the thread is cast to it, 0..1) it watches and hauls from where it is.
    /// </summary>
    public void Follow(Vec anchor, Vec hero, DroneIntent intent, bool window, double elapsedMs, double timeMs, DroneTow? tow = null, Vec hold = default,
        (Vec At, double Cast, double Effort)? haul = null)
    {
        _haul = Scripted ? null : haul;
        var at = Scripted ? intent.Goal
            : tow is { Riding: true } ? _flight.Ride(anchor, hero, tow.Ahead, tow.Grip, elapsedMs)
            : _flight.Step(anchor, hero, tow?.Back(anchor, intent.Goal) ?? intent.Goal, window, intent.Darting, elapsedMs);
        (_tow, _hold) = (tow, hold);
        // A mood moves the body off where it flies (drone-thoughts): a hop, a sink, a lean.
        var mood = intent.Bearing;
        _thought = intent.Thought;
        _bearing = tow == null ? mood : mood with { Tilt = mood.Tilt + tow.Tilt, Swell = mood.Swell - Tighter * tow.Effort, Hurry = mood.Hurry + Hurried * tow.Effort };
        _orbitLead += elapsedMs / 1000 * _bearing.Hurry;
        var pulled = _haul is { Cast: >= 1, At: var thing, Effort: var effort }
            ? new Vector2((float)(thing.X - at.X), (float)(thing.Y - at.Y)).Normalized() * (float)effort : Vector2.Zero;
        _strain = _strain.Lerp(pulled, (float)(1 - Math.Exp(-elapsedMs / HaulEaseMs)));
        _bearing = _bearing with { Swell = _bearing.Swell - HaulTighter * _strain.Length() };
        GlobalPosition = new Vector2((float)(at.X + _bearing.Off.X), (float)(at.Y + _bearing.Off.Y + BobPx * Math.Sin(Math.Tau * timeMs / BobMs)))
            + _strain * HaulLeanPx;
        // It turns after its look, eased, never snapping across.
        float ease = (float)(1 - Math.Exp(-elapsedMs / TurnMs));
        _angle = TurnedToward(_angle, intent.Look, ease);
        var watched = _haul is { At: var watch } ? DroneFlight.Minus(watch, at) : (Vec?)null;
        _eye = TurnedToward(_eye, Scripted ? intent.Look : watched ?? tow?.Eye(at, hold) ?? intent.Look, ease);
        _tint = (float)intent.Tint;
        _timeMs = timeMs;
        _elapsedMs = elapsedMs;
    }

    private static float TurnedToward(float from, Vec look, float ease) =>
        Mathf.Wrap(from + Mathf.Wrap(Mathf.Atan2((float)look.Y, (float)look.X) - from, -Mathf.Pi, Mathf.Pi) * ease, -Mathf.Pi, Mathf.Pi);

    /// <summary>The tow's thread this frame, in the drone's own px, from its coal to the shoulder.</summary>
    private void Threads(double t)
    {
        _threaded = !Scripted && (_haul is { Cast: > 0 } || _tow is { Threaded: true });
        if (!_threaded) return;
        if (_haul is { Cast: > 0 } haul)
        {
            float cast = (float)haul.Cast, loose = 1 - cast * cast;
            var end = (new Vector2((float)haul.At.X, (float)haul.At.Y) - GlobalPosition) * cast;
            var across = new Vector2(-end.Y, end.X).Normalized();
            float last = _thread.Length - 1;
            for (int j = 0; j < _thread.Length; j++)
            {
                float u = j / last;
                float bend = HaulSag * loose * MathF.Sin(u * 8 - (float)t * 11) + HaulBuzz * (float)haul.Effort * (1 - loose) * MathF.Sin(u * 21 - (float)t * 70);
                _thread[j] = (end * u + across * bend * MathF.Sin(MathF.PI * u)) / _glow.Scale.X;
            }
            _threadLength = end.Length();
            return;
        }
        Span<Vec> points = stackalloc Vec[DroneTow.Points];
        var here = GlobalPosition;
        _threadLength = (float)_tow!.Thread(new Vec(here.X, here.Y), _hold, t, points);
        float scale = _glow.Scale.X;
        for (int j = 0; j < points.Length; j++) _thread[j] = (new Vector2((float)points[j].X, (float)points[j].Y) - here) / scale;
    }

    /// <summary>The thread's dark, under the coal.</summary>
    private void ThreadUnder()
    {
        if (!_threaded) return;
        _towDark.Clear();
        float px = I.WorldPx / _body.Scale.X;
        for (int j = 1; j < _thread.Length; j++) _towDark.Quad(_thread[j - 1], _thread[j], ThreadDark * px, ThreadShade);
        _towDark.Flush(_body.GetCanvasItem());
    }

    /// <summary>The thread's light, burning from the coal's ember to the player's colour, its running beads and the knot at its end.</summary>
    private void ThreadLit(Salts salts, double t)
    {
        if (!_threaded) return;
        float px = I.WorldPx / _glow.Scale.X, gain = salts.Gain, last = _thread.Length - 1;
        var wide = EmberArt.Of(salts.Line(), ThreadWideAlpha * gain);
        float fine = Math.Min(1, ThreadFineAlpha * gain);
        for (int j = 1; j < _thread.Length; j++)
        {
            _light.Quad(_thread[j - 1], _thread[j], ThreadWide * px, wide);
            float burn = Mathf.SmoothStep(0, 1, Math.Max(0.02f, _threadLength * (j - 0.5f) / last) / 6);
            _light.Quad(_thread[j - 1], _thread[j], ThreadFine * px, EmberArt.Of(salts.Spark(burn), fine));
        }
        for (int m = 0; m < Beads; m++)
        {
            float u = (float)((t * BeadPace + m / (double)Beads) % 1) * last;
            int i = Math.Min((int)last - 1, (int)u);
            var (a, b) = (_thread[i], _thread[i + 1]);
            var (at, way) = (a.Lerp(b, u - i), (b - a).Normalized());
            float burn = Mathf.SmoothStep(0, 1, _threadLength * u / last / 6);
            Tail(_light, new Spark(at.X, at.Y, way.X, way.Y, true, 0, false, 0, 0), 0.9f * Math.Min(1, _threadLength / 5), salts, burn);
        }
        var (end, side) = (_thread[^1], Knot * px);
        _light.Rect(end.X - side / 2, end.Y - side / 2, side, side, EmberArt.Of(salts.Spark(1, 0.35f), Math.Min(1, _threadLength / 4)));
        if (_haul is { Cast: > 0 } || _tow is { Caught: false }) Halo(_light, end, KnotHalo * px, EmberArt.Of(salts.Line()), 0.3f * Math.Min(1, _threadLength / 6));
    }

    /// <summary>It puffs sparks back along the way as it heaves.</summary>
    private void Puffed(Salts salts, double t)
    {
        if (_tow is not { Threaded: true } tow || Scripted || _haul is { Cast: > 0 }) return;
        float scale = _glow.Scale.X;
        var back = new Vector2(-(float)tow.Way.X, -(float)tow.Way.Y);
        var across = new Vector2(-back.Y, back.X);
        for (int i = 0; i < Puffs; i++)
        {
            float f = (float)((t / PuffLife + DroneSwarm.Hash(i + 7)) % 1), age = f * PuffLife;
            var (heat, along) = tow.Puff(age);
            if (heat < 0.03) continue;
            float flutter = (float)(DroneSwarm.Hash(i + 91) - 0.5) * 2 * PuffWide * (0.4f + f) + MathF.Sin(f * 9 + i) * 0.5f;
            var at = (back * (age * PuffPace - (float)along) + across * flutter) / scale;
            float hot = (float)heat * (1 - f) * (float)(0.55 + 0.4 * DroneSwarm.Hash(i + 5));
            Tail(_light, new Spark(at.X, at.Y, back.X, back.Y, true, 0, false, 0, 0), hot, salts, Mathf.SmoothStep(0, 1, f * 2.2f));
        }
    }

    /// <summary>Forgets where it flew and what it formed, hidden: shown again, it starts at its goal, idle.</summary>
    public void Lose()
    {
        _flight.Lose();
        _swarm.Reset();
        Array.Clear(_away);
    }

    /// <summary>Whether spark <paramref name="i"/> is out as its gauge dot, which the scanner draws: a seed of the reticle formed, its dot away.</summary>
    private bool Out(int i) => (Scripted || _swarm.Shape == SwarmShape.Scan) && DroneSwarm.SeedOf(i) is int j and >= 0 && _away[j] > 0;

    /// <summary>In the pick's order: the sparks behind the core, the core, the sparks before it.</summary>
    private void DrawBody()
    {
        ThreadUnder();
        Darks(front: false);
        DrawCore(_body, Vector2.Zero, 1 / I.Px);
        Darks(front: true);
    }

    private void Darks(bool front)
    {
        _dark.Clear();
        const float Side = I.Spark;
        for (int i = 0; i < _sparks.Length; i++)
            if (_sparks[i] is var spark && spark.Front == front && !Out(i)) _dark.Rect(spark.X - Side / 2, spark.Y - Side / 2, Side, Side, Dark);
        if (_flurry.Shown > 0)
            foreach (var spark in _flurrySparks)
                if (spark.Front == front) _dark.Rect(spark.X - Side / 2, spark.Y - Side / 2, Side, Side, Dark);
        _dark.Flush(_body.GetCanvasItem());
    }

    /// <summary>The core's picture, drone.js's <c>drone_core</c>.</summary>
    private static Texture2D CorePicture => _core ??= HiRes.Texture(HiRes.Load("drone_core") ?? throw new InvalidOperationException("No picture drone_core"));

    /// <summary>The core's picture at <paramref name="at"/> on <paramref name="on"/>, <paramref name="scale"/> times its size (the drone's and its first meeting's).</summary>
    internal static void DrawCore(CanvasItem on, Vector2 at, float scale = 1)
    {
        float side = CoreBox * scale;
        on.DrawTextureRect(CorePicture, new Rect2(at.X - side / 2, at.Y - side / 2, side, side), false);
    }

    /// <summary>The faint orbits and the shapes' threads, the sparks' flight threads and tails, the coal's light: all added.</summary>
    private void DrawGlow()
    {
        double t = _timeMs / 1000;
        var salts = Salts.Reddened(_tint * I.AlarmRed);
        _light.Clear();
        float dim = (float)_bearing.Dim;
        Cloud(_light, salts, 1 - I.HaloDim * dim);
        Puffed(salts, t);
        ThreadLit(salts, t);
        for (int o = 0; o < 3; o++) Orbit(_light, o, salts, tilt: (float)_bearing.Tilt, dim: dim, flat: (float)(_tow?.Flat ?? 1));
        Shape(_swarm.Shape, _swarm.Form(t), _swarm.TurnOf(_swarm.Shape, _angle, t), salts, t, leaving: false);
        var (leaving, form, turn) = _swarm.Leaving(t);
        Shape(leaving, form, turn, salts, t, leaving: true);
        for (int i = 0; i < _sparks.Length; i++)
        {
            var spark = _sparks[i];
            if (Out(i)) continue;
            float burn = Salts.SparkBurn(i, t, spark.X, spark.Y, spark.Shaped);
            if (spark.Flying && i % I.ThreadEvery == 0)
                _light.Quad(new Vector2(spark.FromX, spark.FromY), new Vector2(spark.X, spark.Y), I.ThreadLine, EmberArt.Of(salts.Spark(burn), I.ThreadAlpha));
            Tail(_light, spark, Math.Min(1, spark.Heat + _tint * I.AlarmHeat), salts, burn);
        }
        // The flurry's are fresh from the coal.
        if (_flurry.Shown > 0)
            foreach (var spark in _flurrySparks) Tail(_light, spark, spark.Heat, salts, burn: 0);
        // Its charge (drone-abilities): the embers flying in, then the coal as full as it shows, breathing once full, flaring as one lands.
        int flying = _embers.At(t, _emberAt);
        for (int i = 0; i < flying; i++)
        {
            var e = _emberAt[i];
            Tail(_light, new Spark(e.X, e.Y, e.Dx, e.Dy, Front: true, e.Heat, Flying: false, 0, 0), e.Heat, salts, burn: 0);
        }
        float full = _embers.Shown >= 0.999f ? 1 + I.ChargePulse * MathF.Sin((float)t * I.ChargePulsePace * Mathf.Tau) : 1;
        Coal(_light, Vector2.Zero, _eye, salts, (float)t, dim: dim, glow: (float)_bearing.Glow + I.FlareGlow * _embers.Flare, squint: (float)(_tow?.Squint ?? 0),
            charge: _embers.Shown * full);
        _light.Flush(_glow.GetCanvasItem());
    }

    /// <summary>A spark's glow <paramref name="heat"/> hot: its square and two behind it along its heading, paler and fainter, <paramref name="burn"/> burnt.</summary>
    internal static void Tail(Triangles into, Spark spark, float heat, Salts salts, float burn)
    {
        if (heat <= 0.02f) return;
        const float Side = I.Spark;
        for (int m = 0; m < I.Tail; m++)
        {
            float back = m * I.TailStep, x = spark.X - spark.Dx * back, y = spark.Y - spark.Dy * back;
            var colour = salts.Spark(burn, I.TailPale * (1 - m / (float)I.Tail));
            into.Rect(x - Side / 2, y - Side / 2, Side, Side, EmberArt.Of(colour, (I.TailAlpha - m * I.TailFade) * heat * salts.Gain));
        }
    }

    /// <summary>
    /// Orbit thread <paramref name="o"/>: a faint tilted ellipse about the core; only <paramref name="share"/> of it, from its long end
    /// the way its sparks run, as it kindles in the meeting (<see cref="Meeting.Passed"/>); tilted <paramref name="tilt"/> more and faded by
    /// <paramref name="dim"/> by a mood (drone-thoughts), <paramref name="flat"/> times as round on a tow. Its colour goes from ember by the coal to the player's far out.
    /// </summary>
    internal static void Orbit(Triangles into, int o, Salts salts, float share = 1, float tilt = 0, float dim = 0, float flat = 1)
    {
        float rx = (I.Orbit + o * I.OrbitMore) * I.OrbitWide, ry = rx * I.OrbitFlat * flat;
        tilt += I.OrbitTilt + o * I.OrbitTiltMore;
        float way = o % 2 == 1 ? -1 : 1;
        float alpha = Math.Min(1, I.OrbitAlpha * salts.Lift(I.OrbitLift) * salts.Gain) * (1 - I.OrbitDim * dim), width = I.OrbitLine * salts.Lift(I.OrbitWiden);
        var last = new Vector2(rx, 0).Rotated(tilt);
        var lastColour = EmberArt.Of(salts.Spark(Salts.OutOf(last.X, last.Y)), alpha);
        for (int k = 1; k <= OrbitPieces && share > (k - 1f) / OrbitPieces; k++)
        {
            float a = way * Mathf.Tau * Math.Min(k / (float)OrbitPieces, share);
            var next = new Vector2(MathF.Cos(a) * rx, MathF.Sin(a) * ry).Rotated(tilt);
            var colour = EmberArt.Of(salts.Spark(Salts.OutOf(next.X, next.Y)), alpha);
            into.Quad(last, next, width, lastColour, colour);
            (last, lastColour) = (next, colour);
        }
    }

    /// <summary>
    /// <paramref name="shape"/>'s threads turned <paramref name="turn"/>, <paramref name="form"/> of the way formed, at <paramref name="t"/> s: a wide faint
    /// line and a fine one; a reticle's mark fades as its dot goes out, gone while it is over a wreck. A thought's picture (the one it left if
    /// <paramref name="leaving"/>) keeps its size whatever the body's swell, a mood's fainter than an alarm's.
    /// </summary>
    private void Shape(SwarmShape shape, float form, float turn, Salts salts, double t, bool leaving)
    {
        if (form <= 0.003f) return;
        var lines = _swarm.LinesOf(shape, t, leaving);
        int marks = DroneSwarm.Marks(shape);
        bool thought = shape == SwarmShape.Thought;
        float scale = thought ? _swarm.Unswell : 1, faint = thought && !_swarm.Alarming(leaving) ? I.MoodThread : 1, gain = salts.Gain;
        var ink = salts.Line();
        foreach (var (width, alpha) in (ReadOnlySpan<(float, float)>)[(I.ShapeWide, I.ShapeWideAlpha), (I.ShapeFine, I.ShapeFineAlpha)])
            for (int l = 0; l < lines.Length; l++)
            {
                float shown = form * faint * (l < marks ? 1 - _away[l] : 1);
                if (shown <= 0.003f) continue;
                var line = lines[l];
                for (int k = 2; k + 1 < line.Length; k += 2)
                    _light.Quad(new Vector2(line[k - 2], line[k - 1]).Rotated(turn) * scale, new Vector2(line[k], line[k + 1]).Rotated(turn) * scale, width * scale, EmberArt.Of(ink, alpha * shown * gain));
            }
    }

    /// <summary>
    /// The coal's light at <paramref name="at"/>, pick px, looking <paramref name="angle"/>, in <paramref name="salts"/>' ember at <paramref name="t"/>
    /// s, <paramref name="alpha"/> of it, <paramref name="scale"/> world px a pick px: its halo, three cracks glowing through its crust, an ember eye where it looks (the pick's <c>coal</c>);
    /// a mood's <paramref name="dim"/> and <paramref name="glow"/> (drone-thoughts); <paramref name="kindled"/> of it lit as the salts' coal, the rest bare; the eye shut by <paramref name="squint"/>.
    /// </summary>
    internal static void Coal(Triangles into, Vector2 at, float angle, Salts salts, float t, float alpha = 1, float scale = 1, float dim = 0, float glow = 0, float kindled = 1, float squint = 0,
        float charge = 0)
    {
        float gain = Mathf.Lerp(1, salts.Gain, kindled), lift = Mathf.Lerp(1, salts.Lift(I.CoalLift), kindled) * gain;
        var hot = EmberArt.Of(salts.Coal());
        float flicker = (I.Flicker + I.FlickerSwing * MathF.Sin(t * I.FlickerPace)) * alpha * (1 - I.CoalDim * dim);
        Halo(into, at, I.CoalHalo * scale * (1 + I.ChargeWide * charge), hot,
            Math.Min(I.CoalHaloMost, (I.CoalHaloAlpha * (1 - I.HaloDim * dim) + I.GlowHalo * glow + I.ChargeGlow * charge) * lift) * alpha);
        var crack = new Color(hot, Math.Min(1, I.CrackAlpha * flicker * lift * (1 + I.ChargeCrack * charge)));
        for (int k = 0; k + 1 < I.Cracks.Length; k += 2)
            into.Quad(at + Vector2.FromAngle(angle + I.Cracks[k]) * I.CrackFrom * scale, at + Vector2.FromAngle(angle + I.Cracks[k + 1] * 0.5f) * I.CrackTo * scale, I.CrackLine * scale, crack);
        var eye = at + Vector2.FromAngle(angle) * I.EyeAhead * scale;
        var pale = EmberArt.Of(salts.Coal(I.EyePale), I.EyeAlpha * flicker);
        float wide = 1 + SquintWide * squint;
        into.Fan(eye, I.Eye * scale * (1 - I.EyeDim * dim) * wide, pale, pale, DiscSides, (1 - SquintShut * squint) / wide);
        Halo(into, eye, I.EyeHalo * scale * (1 - SquintWide * squint), hot, I.EyeHaloAlpha * flicker * gain * (1 - 0.6f * squint));
    }

    /// <summary>The cloud of the player's colour round the core, <paramref name="share"/> of it.</summary>
    internal static void Cloud(Triangles into, Salts salts, float share) =>
        Halo(into, Vector2.Zero, I.Cloud, EmberArt.Of(salts.Line()), salts.Cloud * share);

    /// <summary>The kit's <c>halo</c>: <paramref name="colour"/> at <paramref name="alpha"/> in the middle, fading out to <paramref name="radius"/>.</summary>
    private static void Halo(Triangles into, Vector2 at, float radius, Color colour, float alpha)
    {
        if (alpha > 0.005f) into.Fan(at, radius, new Color(colour, alpha), new Color(colour, 0), HaloSides);
    }

    private static Color Rgb((int R, int G, int B) c) => new(c.R / 255f, c.G / 255f, c.B / 255f);
}
