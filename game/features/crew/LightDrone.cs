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
/// threads, the coal's cracks and eye are drawn here as the pick drew them, in its px (<see cref="DroneInk.Px"/> world px each).
/// Drawn unlit, in the world's frame; its light is the head lamp's.
/// </summary>
public partial class LightDrone : Node2D
{
    /// <summary>The core's picture's side, world px (drone.js's <c>CoreBox</c>).</summary>
    private const float CoreBox = 4;

    /// <summary>How many straight pieces an orbit thread is drawn in; a disc's sides.</summary>
    private const int OrbitPieces = 40, DiscSides = 10, HaloSides = 16;

    /// <summary>Its embers' hot orange (the picks' <c>EMBER</c>), an alarm's red, a spark's dark.</summary>
    internal static readonly Color Ember = Rgb(I.Ember);
    private static readonly Color Red = Rgb(I.Red), Dark = Rgb(I.Dark);
    /// <summary>The mode wheel's lamp and scan colours.</summary>
    internal static readonly Color Lamp = new(1f, 0.831f, 0.588f), Scan = new(0.463f, 0.925f, 0.808f);

    private static Texture2D? _core;
    private readonly DroneFlight _flight = new();
    private readonly DroneSwarm _swarm = new();
    private readonly Spark[] _sparks = new Spark[I.Sparks];
    /// <summary>Its flurry while projecting (drone-frame), and its sparks this frame.</summary>
    private readonly DroneFlurry _flurry = new();
    private readonly Spark[] _flurrySparks = new Spark[DroneFlurry.Count];
    private double _elapsedMs;
    /// <summary>The core and the sparks' dark, mixed; the threads, tails and coal, added: both in pick px.</summary>
    private readonly Node2D _body = new(), _glow = new();
    private readonly Triangles _dark = new(I.Sparks * 4, I.Sparks * 6), _light = new(2048, 3072);
    /// <summary>How long it takes to turn most of the way to a new aim, in ms.</summary>
    private const double TurnMs = 90;

    /// <summary>Its bob as it flies, pixels up and down, and how long one takes, ms.</summary>
    private const double BobPx = 1, BobMs = 1800;

    private float _angle;
    private double _timeMs;
    /// <summary>How red it is now, 0..1 (<see cref="DroneIntent.Tint"/>).</summary>
    private float _tint;

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
        _swarm.Become(DroneSwarm.ShapeOf(Projecting, kind != null, Scanning, Idle), _angle, t, pointed);
        for (int i = 0; i < _sparks.Length; i++) _sparks[i] = _swarm.SparkAt(i, _angle, t);
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


    /// <summary>The box round its eye as drawn, in pixels of the world: where what it sends out flies from.</summary>
    public Rect2 LensBox
    {
        get
        {
            var eye = GlobalPosition + Vector2.FromAngle(_angle) * I.EyeAhead * I.Px;
            const float Half = I.Eye * 2 * I.Px;
            return new Rect2(eye.X - Half, eye.Y - Half, 2 * Half, 2 * Half);
        }
    }

    /// <summary>The spyglass's mouth, in pixels of the world, where the scan's sparks leave for the gauge.</summary>
    public Vector2 Mouth => GlobalPosition + new Vector2(I.MouthX, I.MouthY).Rotated(_angle) * I.Px;

    /// <summary>How red an alarm turns it now, 0..1.</summary>
    public float Tint => _tint;

    /// <summary>
    /// Flies it by its hero's <paramref name="anchor"/>, moving at <paramref name="hero"/> px/ms, as its will wants
    /// (<paramref name="intent"/>), with no reach in a <paramref name="window"/>: its eye eased toward the look, red as the tint.
    /// </summary>
    public void Follow(Vec anchor, Vec hero, DroneIntent intent, bool window, double elapsedMs, double timeMs)
    {
        var at = _flight.Step(anchor, hero, intent.Goal, window, intent.Darting, elapsedMs);
        GlobalPosition = new Vector2((float)at.X, (float)(at.Y + BobPx * Math.Sin(Math.Tau * timeMs / BobMs)));
        // It turns after its look, eased, never snapping across.
        float off = Mathf.Wrap(Mathf.Atan2((float)intent.Look.Y, (float)intent.Look.X) - _angle, -Mathf.Pi, Mathf.Pi);
        _angle = Mathf.Wrap(_angle + off * (float)(1 - Math.Exp(-elapsedMs / TurnMs)), -Mathf.Pi, Mathf.Pi);
        _tint = (float)intent.Tint;
        _timeMs = timeMs;
        _elapsedMs = elapsedMs;
    }

    /// <summary>Forgets where it flew and what it formed, hidden: shown again, it starts at its goal, idle.</summary>
    public void Lose()
    {
        _flight.Lose();
        _swarm.Reset();
    }

    /// <summary>The hot colour now: the embers', reddened by an alarm.</summary>
    private Color Hot => Ember.Lerp(Red, Math.Min(1, _tint * I.AlarmRed));

    /// <summary>In the pick's order: the sparks behind the core, the core, the sparks before it.</summary>
    private void DrawBody()
    {
        Darks(front: false);
        DrawCore(_body, Vector2.Zero, 1 / I.Px);
        Darks(front: true);
    }

    private void Darks(bool front)
    {
        _dark.Clear();
        const float Side = I.Spark;
        foreach (var spark in _sparks)
            if (spark.Front == front) _dark.Rect(spark.X - Side / 2, spark.Y - Side / 2, Side, Side, Dark);
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
        var hot = Hot;
        _light.Clear();
        for (int o = 0; o < 3; o++) Orbit(_light, o, hot);
        Shape(_light, _swarm.Shape, _swarm.Form(t), _swarm.TurnOf(_swarm.Shape, _angle, t), hot);
        var (leaving, form, turn) = _swarm.Leaving(t);
        Shape(_light, leaving, form, turn, hot);
        for (int i = 0; i < _sparks.Length; i++)
        {
            var spark = _sparks[i];
            if (spark.Flying && i % I.ThreadEvery == 0)
                _light.Quad(new Vector2(spark.FromX, spark.FromY), new Vector2(spark.X, spark.Y), I.ThreadLine, new Color(hot, I.ThreadAlpha));
            Tail(_light, spark, Math.Min(1, spark.Heat + _tint * I.AlarmHeat), hot);
        }
        if (_flurry.Shown > 0)
            foreach (var spark in _flurrySparks) Tail(_light, spark, spark.Heat, hot);
        Coal(_light, Vector2.Zero, _angle, hot, (float)t);
        _light.Flush(_glow.GetCanvasItem());
    }

    /// <summary>A spark's glow <paramref name="heat"/> hot: its square and two behind it along its heading, paler and fainter.</summary>
    internal static void Tail(Triangles into, Spark spark, float heat, Color hot)
    {
        if (heat <= 0.02f) return;
        const float Side = I.Spark;
        for (int m = 0; m < I.Tail; m++)
        {
            float back = m * I.TailStep, x = spark.X - spark.Dx * back, y = spark.Y - spark.Dy * back;
            var colour = hot.Lerp(Colors.White, I.TailPale * (1 - m / (float)I.Tail));
            into.Rect(x - Side / 2, y - Side / 2, Side, Side, new Color(colour, (I.TailAlpha - m * I.TailFade) * heat));
        }
    }

    /// <summary>
    /// Orbit thread <paramref name="o"/>: a faint tilted ellipse about the core; only <paramref name="share"/> of it, from its long end
    /// the way its sparks run, as it kindles in the meeting (<see cref="Meeting.Passed"/>).
    /// </summary>
    internal static void Orbit(Triangles into, int o, Color hot, float share = 1)
    {
        float rx = (I.Orbit + o * I.OrbitMore) * I.OrbitWide, ry = rx * I.OrbitFlat, tilt = I.OrbitTilt + o * I.OrbitTiltMore;
        float way = o % 2 == 1 ? -1 : 1;
        var colour = new Color(hot, I.OrbitAlpha);
        var last = new Vector2(rx, 0).Rotated(tilt);
        for (int k = 1; k <= OrbitPieces && share > (k - 1f) / OrbitPieces; k++)
        {
            float a = way * Mathf.Tau * Math.Min(k / (float)OrbitPieces, share);
            var next = new Vector2(MathF.Cos(a) * rx, MathF.Sin(a) * ry).Rotated(tilt);
            into.Quad(last, next, I.OrbitLine, colour);
            last = next;
        }
    }

    /// <summary><paramref name="shape"/>'s threads turned <paramref name="turn"/>, <paramref name="form"/> of the way formed: a wide faint line and a fine one.</summary>
    private static void Shape(Triangles into, SwarmShape shape, float form, float turn, Color hot)
    {
        if (form <= 0.003f) return;
        foreach (var (width, alpha) in (ReadOnlySpan<(float, float)>)[(I.ShapeWide, I.ShapeWideAlpha), (I.ShapeFine, I.ShapeFineAlpha)])
        {
            var colour = new Color(hot, alpha * form);
            foreach (var line in DroneSwarm.LinesOf(shape))
                for (int k = 2; k + 1 < line.Length; k += 2)
                    into.Quad(new Vector2(line[k - 2], line[k - 1]).Rotated(turn), new Vector2(line[k], line[k + 1]).Rotated(turn), width, colour);
        }
    }

    /// <summary>
    /// The coal's light at <paramref name="at"/>, pick px, looking <paramref name="angle"/>, <paramref name="hot"/> at <paramref name="t"/>
    /// s, <paramref name="alpha"/> of it, <paramref name="scale"/> world px a pick px: its halo, three cracks glowing through its crust, an ember eye where it looks (the pick's <c>coal</c>).
    /// </summary>
    internal static void Coal(Triangles into, Vector2 at, float angle, Color hot, float t, float alpha = 1, float scale = 1)
    {
        float flicker = (I.Flicker + I.FlickerSwing * MathF.Sin(t * I.FlickerPace)) * alpha;
        Halo(into, at, I.CoalHalo * scale, hot, I.CoalHaloAlpha * alpha);
        var crack = new Color(hot, I.CrackAlpha * flicker);
        for (int k = 0; k + 1 < I.Cracks.Length; k += 2)
            into.Quad(at + Vector2.FromAngle(angle + I.Cracks[k]) * I.CrackFrom * scale, at + Vector2.FromAngle(angle + I.Cracks[k + 1] * 0.5f) * I.CrackTo * scale, I.CrackLine * scale, crack);
        var eye = at + Vector2.FromAngle(angle) * I.EyeAhead * scale;
        var pale = new Color(hot.Lerp(Colors.White, I.EyePale), I.EyeAlpha * flicker);
        into.Fan(eye, I.Eye * scale, pale, pale, DiscSides);
        Halo(into, eye, I.EyeHalo * scale, hot, I.EyeHaloAlpha * flicker);
    }

    /// <summary>The kit's <c>halo</c>: <paramref name="colour"/> at <paramref name="alpha"/> in the middle, fading out to <paramref name="radius"/>.</summary>
    private static void Halo(Triangles into, Vector2 at, float radius, Color colour, float alpha)
    {
        if (alpha > 0.005f) into.Fan(at, radius, new Color(colour, alpha), new Color(colour, 0), HaloSides);
    }

    private static Color Rgb((int R, int G, int B) c) => new(c.R / 255f, c.G / 255f, c.B / 255f);
}
