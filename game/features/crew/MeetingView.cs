using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// What this player's meeting shows this frame: <see cref="U"/> seconds into it, the engine's clock and its hero's figure's (what the
/// drone's sparks move by), the hero's palm as drawn, the drone's place by the head and the way its eye looks there, and whether the
/// meeting draws the soot left in the pit, the core's picture gone for this player. World pixels.
/// </summary>
public readonly record struct MeetingFrame(double U, double ClockS, double SwarmS, Vector2 Palm, Vector2 Idle, float IdleAngle, bool Pit);

/// <summary>
/// This player's meeting with its drone (drone-meeting), drawn as <see cref="MeetingScene"/> places it: in the wreck, the core
/// waking, lifting from its pit, and after the vision kindling its threads one at a time and rising to the head, on the world's
/// layers (behind the hero on the tethers', over it on the drones', the light on the glows'); the vision on a screen layer of its own
/// over the world (<see cref="Vision"/>): the screen crumbling to flakes that go out as stars, the void, the giant core and the blood,
/// with the hero's own figure moved onto it meanwhile. Unlit, never a node per drop or flake.
/// </summary>
public partial class MeetingView : Node2D
{
    /// <summary>A pit ember's halo, the pick's pixels.</summary>
    private const float PitEmber = 3;
    /// <summary>The giant's pictures' frame, its units (drone.js's <c>GiantBox</c> over <c>GiantR</c>); the soaked disc's radius, units.</summary>
    private const float GiantFrame = 672f / 54, SoakRadius = 5.1f;

    private const string BackdropShader = """
        shader_type canvas_item;
        render_mode unshaded;
        uniform sampler2D screen : hint_screen_texture, filter_linear;
        // The sky and stars are bare colours (their UV below 0); a flake carries the screen's picture where it lay.
        void fragment() {
            if (UV.x >= 0.0) COLOR = vec4(texture(screen, UV).rgb, COLOR.a);
        }
        """;

    private static readonly Color SkyTop = Rgb((2, 3, 7)), SkyBottom = Rgb((5, 7, 12)), Cold = Rgb((206, 218, 238)), Warm = Rgb((240, 226, 206));
    private static readonly Color StarLight = Rgb((214, 224, 240)), Lit = Rgb((176, 184, 196));

    /// <summary>A mote's dark rim past its side, the side its lit top shows from and that top's height, world px.</summary>
    private const float MoteRim = 0.0875f, LitFrom = 0.275f, LitTop = 0.125f;

    private static readonly Color RimDark = new("1a1b1f"), RimLight = new("26282d"), SootDark = new("4c4f55"), SootLight = new("606369");
    private static readonly Color SootTop = new(170 / 255f, 174 / 255f, 180 / 255f, 0.45f), SparkDark = Rgb(DroneInk.Dark);
    private static Texture2D? _body, _skin, _lens, _lensLit;

    private readonly MeetingScene _scene;
    private readonly Node2D _under = new(), _underAdd = new(), _over = new(), _overAdd = new();
    private readonly Node2D _backdrop = new(), _backDrops = new(), _backDropsAdd = new(), _glow = new(), _giant = new(), _giantAdd = new(), _frontDrops = new(), _frontDropsAdd = new();
    private readonly Triangles _tri = new(2048, 3072);
    /// <summary>The drone's sparks as it will fly with them, idle on its orbits: shown as their threads kindle.</summary>
    private readonly DroneSwarm _swarm = new();
    private readonly Spark[] _sparks = new Spark[DroneInk.Sparks];
    private MeetingFrame _f;
    private MeetingCore _core;
    private CoreFace _face;
    private MeetingFlakes? _flakes;
    private Transform2D _toScreen = Transform2D.Identity;
    private Vector2[] _bPoints = new Vector2[4096];
    private Color[] _bColours = new Color[4096];
    private Vector2[] _bUvs = new Vector2[4096];
    private int[] _bIndices = new int[6144];
    private int _bCount, _bIndexCount;

    /// <summary>The screen layer the vision is drawn on, over the world, following the camera: the hero's figure goes onto it meanwhile.</summary>
    public CanvasLayer Vision { get; } = new() { Layer = ScreenLayers.Vision, FollowViewportEnabled = true, Visible = false };

    public MeetingView(MeetingScene scene)
    {
        _scene = scene;
        TopLevel = true;
        foreach (var (node, z, adds) in new (Node2D, int, bool)[]
        {
            (_under, Layers.Tethers, false), (_underAdd, Layers.Tethers, true),
            (_over, Layers.Drones, false), (_overAdd, Layers.Glows, true),
        })
            AddChild(Unlit(node, z, adds));
        AddChild(Vision);
        // On the vision's own layer: the void and the flakes, the drops behind the core, its glow, the core, its light, then the
        // hero's figure (Layers.Hero), then the drops before it.
        foreach (var (node, z, adds) in new (Node2D, int, bool)[]
        {
            (_backdrop, -20, false), (_backDrops, -12, false), (_backDropsAdd, -11, true), (_glow, -10, true), (_giant, -9, false),
            (_giantAdd, -4, true), (_frontDrops, 30, false), (_frontDropsAdd, 31, true),
        })
            Vision.AddChild(Unlit(node, z, adds));
        _backdrop.Material = new ShaderMaterial { Shader = new Shader { Code = BackdropShader } };
        _under.Draw += DrawUnder;
        _underAdd.Draw += DrawUnderAdd;
        _over.Draw += DrawOver;
        _overAdd.Draw += DrawOverAdd;
        _backdrop.Draw += DrawBackdrop;
        _backDrops.Draw += () => Drops(_backDrops, front: false, light: false);
        _backDropsAdd.Draw += () => Drops(_backDropsAdd, front: false, light: true);
        _frontDrops.Draw += () => Drops(_frontDrops, front: true, light: false);
        _frontDropsAdd.Draw += () => Drops(_frontDropsAdd, front: true, light: true);
        _glow.Draw += DrawGlow;
        _giant.Draw += DrawGiant;
        _giantAdd.Draw += DrawGiantLight;
    }

    /// <summary>Whether the vision is up this frame: the screen crumbling, the void, or crumbling back.</summary>
    public bool InVision => Meeting.PhaseOf(_f.U) is MeetingPhase.Into or MeetingPhase.Vision or MeetingPhase.Back;

    /// <summary>Shows <paramref name="frame"/>.</summary>
    public void Show(MeetingFrame frame)
    {
        _f = frame;
        _core = _scene.CoreAt(frame.U, V(frame.Idle), frame.IdleAngle);
        _face = _scene.FaceOf(frame.U);
        for (int i = 0; i < _sparks.Length; i++) _sparks[i] = _swarm.SparkAt(i, (float)_core.Angle, frame.SwarmS);
        Vision.Visible = InVision;
        if (InVision)
        {
            var viewport = GetViewport();
            var size = viewport.GetVisibleRect().Size;
            var canvas = viewport.GetCanvasTransform();
            _toScreen = canvas;
            _backdrop.Transform = canvas.AffineInverse();
            if (_flakes == null || !Mathf.IsEqualApprox((float)_flakes.Width, size.X) || !Mathf.IsEqualApprox((float)_flakes.Height, size.Y))
            {
                var hover = canvas * W(_scene.Hover);
                _flakes = new MeetingFlakes(size.X, size.Y, new Vec(hover.X, hover.Y));
            }
        }
        foreach (var node in (ReadOnlySpan<Node2D>)[_under, _underAdd, _over, _overAdd])
            node.QueueRedraw();
        if (InVision)
            foreach (var node in (ReadOnlySpan<Node2D>)[_backdrop, _backDrops, _backDropsAdd, _glow, _giant, _giantAdd, _frontDrops, _frontDropsAdd])
                node.QueueRedraw();
    }

    private MeetingPhase Phase => Meeting.PhaseOf(_f.U);
    private bool Lifted => _f.U >= Meeting.Times.LiftFrom;
    /// <summary>Whether the small core hangs in the wreck: lifted from its pit and not yet grown, or back and shrunk; till it reaches the head.</summary>
    private bool SmallCore => Meeting.DrawsDrone(_f.U) && (Phase == MeetingPhase.Wreck && Lifted || Phase == MeetingPhase.Home);

    // ---------- the wreck ----------

    /// <summary>The soot left in the pit, once the core has lifted from it, for a player who no longer sees it lie there.</summary>
    private void DrawUnder()
    {
        if (!Lifted || !_f.Pit) return;
        _tri.Clear();
        for (int j = 0; j < Meeting.HeapCount; j++)
        {
            var mote = Meeting.HeapAt(j);
            var at = Box + W(mote.At);
            Mote(_tri, at.X, at.Y, (float)mote.Side, mote.Dark);
            if (mote.Hot) Square(at, (float)mote.Side, new Color(LightDrone.Ember, 0.9f));
        }
        _tri.Flush(_under.GetCanvasItem());
    }

    private void DrawUnderAdd()
    {
        if (!Lifted || !_f.Pit) return;
        _tri.Clear();
        for (int j = 0; j < Meeting.HeapCount; j++)
            if (Meeting.HeapAt(j) is { Hot: true } mote) Halo(Box + W(mote.At), PitEmber, LightDrone.Ember, 0.3f);
        _tri.Flush(_underAdd.GetCanvasItem());
    }

    /// <summary>The small core over the hero: lifted, or back with its cloud going out and its sparks kindling, each side of it.</summary>
    private void DrawOver()
    {
        if (!SmallCore) return;
        var at = W(_core.At);
        bool home = Phase == MeetingPhase.Home;
        if (home) Body(front: false);
        LightDrone.DrawCore(_over, at);
        if (home) Body(front: true);
    }

    /// <summary>The cloud's motes not yet put out on one side of the core, and the sparks kindled there, in the drone's own pick px.</summary>
    private void Body(bool front)
    {
        _tri.Clear();
        Cloud(front);
        _tri.Flush(_over.GetCanvasItem());
        _tri.Clear();
        const float Side = DroneInk.Spark;
        for (int i = 0; i < _sparks.Length; i++)
            if (_sparks[i] is var spark && spark.Front == front && Kindled(i)) _tri.Rect(spark.X - Side / 2, spark.Y - Side / 2, Side, Side, SparkDark);
        _over.DrawSetTransform(W(_core.At), 0, new Vector2(DroneInk.Px, DroneInk.Px));
        _tri.Flush(_over.GetCanvasItem());
        _over.DrawSetTransform(Vector2.Zero, 0);
    }

    /// <summary>Whether spark <paramref name="i"/>'s thread has kindled past it.</summary>
    private bool Kindled(int i) => Meeting.Passed(i % 3, _f.U, _sparks[i].X, _sparks[i].Y);

    /// <summary>The light in the wreck: the small core's ember halos as it wakes, its coal's once back, its cloud's embers, its threads kindling.</summary>
    private void DrawOverAdd()
    {
        if (!Meeting.DrawsDrone(_f.U) || InVision) return;
        _tri.Clear();
        double u = _f.U;
        if (SmallCore)
        {
            var at = W(_core.At);
            var (breathing, waking) = Meeting.SmallHalos(u, Meeting.Breath(_f.ClockS), _core.R);
            Halo(at, (float)(5.4 * _core.R + 34), Rgb(Meeting.Ember), (float)breathing);
            Halo(at, (float)(5.4 * _core.R + 26), Rgb(Meeting.Ember), (float)waking);
            if (_face.Lens > 0) LightDrone.Coal(_tri, at, (float)_core.Angle, EmberArt.Mine, (float)_f.SwarmS, (float)_face.Lens, DroneInk.Px, kindled: KindledShare);
            if (Phase == MeetingPhase.Home) Embers();
        }
        _tri.Flush(_overAdd.GetCanvasItem());
        if (SmallCore && Phase == MeetingPhase.Home) Kindling();
    }

    /// <summary>
    /// The drone's threads as far round as each has kindled, and the tails of the sparks they have lit, as the drone draws them, in its
    /// own pick px.
    /// </summary>
    private void Kindling()
    {
        _tri.Clear();
        var salts = EmberArt.Mine;
        LightDrone.Cloud(_tri, salts, KindledShare);
        for (int o = 0; o < 3; o++) LightDrone.Orbit(_tri, o, salts, (float)Meeting.Kindled(o, _f.U));
        for (int i = 0; i < _sparks.Length; i++)
            if (Kindled(i)) LightDrone.Tail(_tri, _sparks[i], _sparks[i].Heat, salts, Salts.OutOf(_sparks[i].X, _sparks[i].Y));
        _overAdd.DrawSetTransform(W(_core.At), 0, new Vector2(DroneInk.Px, DroneInk.Px));
        _tri.Flush(_overAdd.GetCanvasItem());
        _overAdd.DrawSetTransform(Vector2.Zero, 0);
    }

    /// <summary>How much of it has kindled into the drone it will be, so nothing pops as the meeting ends.</summary>
    private float KindledShare => Phase == MeetingPhase.Home
        ? (float)(Meeting.Kindled(0, _f.U) + Meeting.Kindled(1, _f.U) + Meeting.Kindled(2, _f.U)) / 3
        : 0;

    /// <summary>Where the lying core's picture's top-left is.</summary>
    private Vector2 Box => W(_scene.Core) - W(Meeting.InPicture);

    /// <summary>The cloud on one side of the small core, back from the vision: where the blood's rings shrank to, each mote put out as its thread kindles past it.</summary>
    private void Cloud(bool front)
    {
        float t = (float)_f.SwarmS;
        for (int i = 0; i < MeetingScene.Drops; i++)
        {
            var mote = Meeting.MoteAt(i, t);
            if (mote.Front == front && !Meeting.Passed(i % 3, _f.U, mote.X, mote.Y))
                Mote(_tri, (float)_core.At.X + mote.X, (float)_core.At.Y + mote.Y, mote.Side, mote.Dark);
        }
    }

    /// <summary>The hot motes' heat in the cloud, as the drone's own glow adds it.</summary>
    private void Embers()
    {
        float t = (float)_f.SwarmS;
        for (int i = 0; i < MeetingScene.Drops; i++)
        {
            var mote = Meeting.MoteAt(i, t);
            if (mote.Heat <= 0.05f || Meeting.Passed(i % 3, _f.U, mote.X, mote.Y)) continue;
            Square(new Vector2((float)_core.At.X + mote.X, (float)_core.At.Y + mote.Y), mote.Side,
                new Color(LightDrone.Ember.Lerp(Colors.White, 0.25f * mote.Heat), 0.95f * mote.Heat));
        }
    }

    // ---------- the vision ----------

    /// <summary>The void, its stars, and the screen in flakes flying from the core and going out as stars.</summary>
    private void DrawBackdrop()
    {
        if (_flakes == null) return;
        (_bCount, _bIndexCount) = (0, 0);
        float w = (float)_flakes.Width, h = (float)_flakes.Height, k = (float)_flakes.Scale;
        var bare = new Vector2(-1, -1);
        int at = _bCount;
        BPoint(new Vector2(0, 0), SkyTop, bare);
        BPoint(new Vector2(w, 0), SkyTop, bare);
        BPoint(new Vector2(w, h), SkyBottom, bare);
        BPoint(new Vector2(0, h), SkyBottom, bare);
        BQuad(at);
        foreach (var star in _flakes.Stars)
        {
            float s = (float)star.Side, x = (float)star.At.X - s / 2, y = (float)star.At.Y - s / 2;
            var colour = new Color(star.Warm ? Warm : Cold, (float)star.Alpha);
            at = _bCount;
            BPoint(new Vector2(x, y), colour, bare);
            BPoint(new Vector2(x + s, y), colour, bare);
            BPoint(new Vector2(x + s, y + s), colour, bare);
            BPoint(new Vector2(x, y + s), colour, bare);
            BQuad(at);
        }
        double e = MeetingFlakes.Crumbled(_f.U);
        if (e < 1)
            foreach (var flake in _flakes.Flakes)
            {
                if (MeetingFlakes.At(flake, e) is not { } shown) continue;
                var mid = W(shown.Middle);
                float turn = (float)shown.Turn, size = (float)shown.Size, alpha = (float)shown.Alpha;
                if (alpha > 0.003f)
                {
                    at = _bCount;
                    var colour = new Color(1, 1, 1, alpha);
                    foreach (var corner in (ReadOnlySpan<Vec>)[flake.A, flake.B, flake.C, flake.D])
                        BPoint(mid + ((W(corner) - W(flake.Middle)) * size).Rotated(turn), colour, new Vector2((float)corner.X / w, (float)corner.Y / h));
                    BQuad(at);
                }
                if (shown.Star > 0.003)
                {
                    float s = 1.6f * k;
                    var colour = new Color(StarLight, (float)shown.Star);
                    at = _bCount;
                    BPoint(mid + new Vector2(-s / 2, -s / 2), colour, bare);
                    BPoint(mid + new Vector2(s / 2, -s / 2), colour, bare);
                    BPoint(mid + new Vector2(s / 2, s / 2), colour, bare);
                    BPoint(mid + new Vector2(-s / 2, s / 2), colour, bare);
                    BQuad(at);
                }
            }
        RenderingServer.CanvasItemAddTriangleArray(_backdrop.GetCanvasItem(), new ReadOnlySpan<int>(_bIndices, 0, _bIndexCount),
            new ReadOnlySpan<Vector2>(_bPoints, 0, _bCount), new ReadOnlySpan<Color>(_bColours, 0, _bCount), new ReadOnlySpan<Vector2>(_bUvs, 0, _bCount),
            ReadOnlySpan<int>.Empty, ReadOnlySpan<float>.Empty, default, -1);
    }

    /// <summary>The red glow round the giant as it drinks, then ember as the blood turns; the small core's halos as it grows.</summary>
    private void DrawGlow()
    {
        _tri.Clear();
        var at = W(_core.At);
        float reach = (float)(5.4 * _core.R + 20);
        var (drinking, embers) = _scene.VoidGlow(_f.U, _core.R);
        Halo(at, reach * 1.15f, Rgb(Meeting.Drinking), (float)drinking);
        Halo(at, reach * 1.1f, Rgb(Meeting.Ember), (float)embers);
        var (breathing, waking) = Meeting.SmallHalos(_f.U, Meeting.Breath(_f.ClockS), _core.R);
        Halo(at, (float)(5.4 * _core.R + 34), Rgb(Meeting.Ember), (float)breathing);
        Halo(at, (float)(5.4 * _core.R + 26), Rgb(Meeting.Ember), (float)waking);
        _tri.Flush(_glow.GetCanvasItem());
    }

    /// <summary>The giant core: its body lit from above, the blood soaked into it, its pits turned with its lens, the lens and its light.</summary>
    private void DrawGiant()
    {
        LoadGiant();
        var at = W(_core.At);
        float unit = (float)(_core.R * Meeting.Px), side = GiantFrame * unit, angle = (float)_core.Angle;
        var box = new Rect2(-side / 2, -side / 2, side, side);
        _giant.DrawSetTransform(at, 0);
        _giant.DrawTextureRect(_body!, box, false);
        if (_face.Soak > 0.003) _giant.DrawCircle(Vector2.Zero, SoakRadius * unit, new Color(Rgb(Meeting.Soaked), (float)_face.Soak));
        _giant.DrawSetTransform(at, angle);
        _giant.DrawTextureRect(_skin!, box, false);
        _giant.DrawTextureRect(_lens!, box, false);
        if (_face.Lens > 0.002) _giant.DrawTextureRect(_lensLit!, box, false, new Color(1, 1, 1, (float)_face.Lens));
        _giant.DrawSetTransform(Vector2.Zero, 0);
    }

    /// <summary>The giant's light: its lens's halo.</summary>
    private void DrawGiantLight()
    {
        _tri.Clear();
        if (_face.Lens > 0.002)
        {
            double big = Meeting.Ss(_core.R, 2, 20);
            var lens = W(_core.At) + Vector2.FromAngle((float)_core.Angle) * (float)(2.2 * _core.R * Meeting.Px);
            Halo(lens, (float)(14 * 0.82 * Meeting.Lerp(1, 0.5, big)), Rgb(Meeting.Sign), (float)(0.55 * _face.Glow * _face.Lens * Meeting.Lerp(1, 0.3, big)));
        }
        _tri.Flush(_giantAdd.GetCanvasItem());
    }

    /// <summary>The blood's drops on one side of the core: a tail, a darker rim, the drop, its sheen and lit top; or, <paramref name="light"/>, their embers.</summary>
    private void Drops(Node2D on, bool front, bool light)
    {
        _tri.Clear();
        if (front && !light) Threads();
        double glows = Meeting.Ss(_core.R, 3, 10);
        for (int i = 0; i < MeetingScene.Drops; i++)
        {
            if (_scene.DropAt(i, _f.U, _core) is not { } drop || drop.Front != front) continue;
            float r = (float)drop.Radius;
            Vector2 at = W(drop.At), tail = W(drop.Tail), mid = W(drop.Mid);
            if (light)
            {
                float ember = (float)drop.Ember;
                if (ember <= 0.003f) continue;
                Disc(at, r * 0.9f, new Color(Rgb(Meeting.Ember).Lerp(Colors.White, 0.25f), 0.9f * ember));
                Halo(at, r * 3.4f / (float)Meeting.Px, Rgb(Meeting.Ember), (float)(0.3 * ember * glows));
                continue;
            }
            Color fill = Rgb(drop.Fill), rim = Rgb(drop.Rim);
            float w = 1.8f * r, edge = 0.8f * (float)Meeting.Px;
            Line(tail, mid, w + edge, new Color(rim, 0.9f));
            Line(mid, at, w + edge, new Color(rim, 0.9f));
            Line(tail, mid, w, fill);
            Line(mid, at, w, fill);
            Disc(at, r + 0.4f * (float)Meeting.Px, rim);
            Disc(at, r, fill);
            if (r > 1.2f * Meeting.Px)
            {
                if (drop.Lit > 0.003) Disc(at + new Vector2(-0.35f * r, -0.45f * r), 0.3f * r, new Color(Lit, (float)drop.Lit));
                Disc(at + new Vector2(-0.3f * r, -0.35f * r), 0.38f * r, new Color(Rgb(drop.Sheen), (float)drop.SheenAlpha));
            }
        }
        _tri.Flush(on.GetCanvasItem());
    }

    /// <summary>The blood's threads in flight, before the core: each a fine strand, far thinner than the arm it leaves, with a bead at its head.</summary>
    private void Threads()
    {
        var palm = V(_f.Palm);
        var colour = Rgb(Meeting.Blood);
        float width = (float)MeetingScene.Thread;
        Span<Vec> points = stackalloc Vec[MeetingScene.ThreadPoints];
        for (int i = 0; i < MeetingScene.Drops; i++)
            for (int strand = 0; strand < MeetingScene.Strands; strand++)
            {
                int n = _scene.ThreadAt(i, strand, _f.U, palm, _core, points);
                for (int j = 0; j + 1 < n; j++) Line(W(points[j]), W(points[j + 1]), width, new Color(colour, 0.9f));
                if (n > 0) Disc(W(points[n - 1]), width * 0.8f, colour);
            }
    }

    // ---------- both ----------

    /// <summary>A mote of soot, its middle at (<paramref name="x"/>, <paramref name="y"/>), its side <paramref name="s"/>: a dark rim, its tone, a lit top on the larger ones.</summary>
    private static void Mote(Triangles into, float x, float y, float s, bool dark)
    {
        x -= s / 2;
        y -= s / 2;
        into.Rect(x - MoteRim, y - MoteRim, s + 2 * MoteRim, s + 2 * MoteRim, dark ? RimDark : RimLight);
        into.Rect(x, y, s, s, dark ? SootDark : SootLight);
        if (s > LitFrom) into.Rect(x, y, s, LitTop, SootTop);
    }

    private static void LoadGiant()
    {
        if (_body != null) return;
        static Texture2D Of(string name) => HiRes.Texture(HiRes.Load(name) ?? throw new InvalidOperationException($"No picture {name}"));
        (_body, _skin, _lens, _lensLit) = (Of("drone_giant"), Of("drone_giant_skin"), Of("drone_giant_lens"), Of("drone_giant_lit"));
    }

    private static Node2D Unlit(Node2D node, int z, bool adds)
    {
        (node.ZAsRelative, node.ZIndex) = (false, z);
        node.Material = new CanvasItemMaterial
        {
            LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
            BlendMode = adds ? CanvasItemMaterial.BlendModeEnum.Add : CanvasItemMaterial.BlendModeEnum.Mix,
        };
        return node;
    }

    private void BPoint(Vector2 at, Color colour, Vector2 uv)
    {
        if (_bCount == _bPoints.Length)
        {
            Array.Resize(ref _bPoints, _bCount * 2);
            Array.Resize(ref _bColours, _bCount * 2);
            Array.Resize(ref _bUvs, _bCount * 2);
        }
        (_bPoints[_bCount], _bColours[_bCount], _bUvs[_bCount]) = (at, colour, uv);
        _bCount++;
    }

    private void BQuad(int at)
    {
        if (_bIndexCount + 6 > _bIndices.Length) Array.Resize(ref _bIndices, _bIndices.Length * 2);
        foreach (int k in (ReadOnlySpan<int>)[0, 1, 2, 0, 2, 3]) _bIndices[_bIndexCount++] = at + k;
    }

    private void Line(Vector2 a, Vector2 b, float width, Color colour)
    {
        if (colour.A <= 0.003f || a.IsEqualApprox(b)) return;
        _tri.Quad(a, b, width, colour);
    }

    private void Disc(Vector2 at, float radius, Color colour) => _tri.Fan(at, radius, colour, colour);

    private void Square(Vector2 at, float side, Color colour) => _tri.Rect(at.X - side / 2, at.Y - side / 2, side, side, colour);

    /// <summary>The pick's <c>halo</c>: <paramref name="colour"/> at <paramref name="alpha"/> in the middle, fading out to <paramref name="radius"/> of its pixels.</summary>
    private void Halo(Vector2 at, float radius, Color colour, float alpha)
    {
        if (alpha > 0.005f) _tri.Fan(at, radius * (float)Meeting.Px, new Color(colour, alpha), new Color(colour, 0), 12);
    }

    private static Color Rgb((double R, double G, double B) c) => new((float)(c.R / 255), (float)(c.G / 255), (float)(c.B / 255));

    private static Vector2 W(Vec v) => new((float)v.X, (float)v.Y);

    private static Vec V(Vector2 v) => new(v.X, v.Y);
}
