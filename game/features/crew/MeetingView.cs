using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// What this player's meeting shows this frame: <see cref="U"/> seconds into it, the engine's clock and its hero's figure's (what the
/// drone's soot moves by), the hero's palm as drawn, the drone's place by the head and the way its lens looks there, the drone's own
/// lens colour, and whether the meeting draws the soot left in the pit, the core's picture gone for this player. World pixels.
/// </summary>
public readonly record struct MeetingFrame(double U, double ClockS, double SwarmS, Vector2 Palm, Vector2 Idle, float IdleAngle, Color Lens, bool Pit);

/// <summary>
/// This player's meeting with its drone (drone-meeting), drawn as <see cref="MeetingScene"/> places it: in the wreck, the core
/// waking, lifting from its pit, and after the vision pulling its plates home along ember arcs and rising to the head, on the world's
/// layers (behind the hero on the tethers', over it on the drones', the light on the glows'); the vision on a screen layer of its own
/// over the world (<see cref="Vision"/>): the screen crumbling to flakes that go out as stars, the void, the giant core and the blood,
/// with the hero's own figure moved onto it meanwhile. Unlit, never a node per drop or flake.
/// </summary>
public partial class MeetingView : Node2D
{
    /// <summary>The pick's widths, its pixels: a crack's glow and line on the giant (their caps), an arc's glow and line, a landing's flash, a pit ember's halo.</summary>
    private const double CrackGlow = 1.7, CrackGlowMost = 7, CrackLine = 0.55, CrackLineMost = 2.6;
    private const float ArcGlow = 3.4f, ArcLine = 0.9f, Flash = 9, PitEmber = 3;
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

    private static readonly Color Dull = Rgb((95, 83, 66));
    private static readonly Color SkyTop = Rgb((2, 3, 7)), SkyBottom = Rgb((5, 7, 12)), Cold = Rgb((206, 218, 238)), Warm = Rgb((240, 226, 206));
    private static readonly Color StarLight = Rgb((214, 224, 240)), Lit = Rgb((176, 184, 196));

    private static Texture2D? _body, _skin, _lens, _lensLit;

    private readonly MeetingScene _scene;
    private readonly Node2D _away = new(), _under = new(), _underAdd = new(), _over = new(), _overAdd = new();
    private readonly Node2D _backdrop = new(), _backDrops = new(), _backDropsAdd = new(), _glow = new(), _giant = new(), _giantAdd = new(), _frontDrops = new(), _frontDropsAdd = new();
    private readonly Triangles _tri = new(2048, 3072);
    private readonly Vec[] _points = new Vec[41];
    private readonly Plate[] _homes = new Plate[DroneSwarm.Tune.Plates];
    private readonly MeetingPlate[] _plates = new MeetingPlate[DroneSwarm.Tune.Plates];
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
            (_away, Layers.Tethers, false), (_under, Layers.Tethers, false), (_underAdd, Layers.Tethers, true),
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
        _away.Draw += DrawAway;
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
        float aim = (float)_core.Angle, t = (float)frame.SwarmS;
        for (int i = 0; i < _plates.Length; i++)
        {
            _homes[i] = DroneSwarm.PlateIn(SwarmShape.Idle, i, aim, t);
            _plates[i] = _scene.PlateOf(i, frame.U, _core.At, _homes[i]);
        }
        _away.Modulate = new Color(1, 1, 1, (float)Meeting.DebrisIn(frame.U));
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
        foreach (var node in (ReadOnlySpan<Node2D>)[_away, _under, _underAdd, _over, _overAdd])
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

    /// <summary>The plates still out among the debris.</summary>
    private void DrawAway()
    {
        for (int i = 0; i < _plates.Length; i++)
            if (_plates[i] is { Stage: PlateStage.Away } plate)
                LightDrone.DrawPlate(_away, i, new Plate(0, 0, (float)plate.Turn, true, 0, 1, (float)plate.Flip), W(plate.At));
    }

    /// <summary>The soot left in the pit, once the core has lifted from it, for a player who no longer sees it lie there.</summary>
    private void DrawUnder()
    {
        if (!Lifted || !_f.Pit) return;
        _tri.Clear();
        for (int j = 0; j < Meeting.HeapCount; j++)
        {
            var mote = Meeting.HeapAt(j);
            var at = Box + W(mote.At);
            LightDrone.Mote(_tri, at.X, at.Y, (float)mote.Side, mote.Dark);
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

    /// <summary>The small core over the hero: lifted, or back with its cloud and its plates coming home; the plates in flight.</summary>
    private void DrawOver()
    {
        if (!SmallCore) return;
        var at = W(_core.At);
        bool home = Phase == MeetingPhase.Home;
        if (home)
        {
            _tri.Clear();
            Cloud(front: false);
            _tri.Flush(_over.GetCanvasItem());
            Homes(front: false);
        }
        var lit = Rgb(Meeting.Sign).Lerp(_f.Lens, (float)Meeting.LensShift(_f.U));
        LightDrone.DrawCore(_over, at, (float)_core.Angle, Dull.Lerp(lit, (float)_face.Lens));
        if (home)
        {
            _tri.Clear();
            Cloud(front: true);
            _tri.Flush(_over.GetCanvasItem());
            Homes(front: true);
        }
        for (int i = 0; i < _plates.Length; i++)
            if (_plates[i] is { Stage: PlateStage.Flying } plate)
                LightDrone.DrawPlate(_over, i, new Plate(0, 0, (float)plate.Turn, plate.Front, 0, 1, (float)plate.Flip), W(plate.At));
    }

    /// <summary>The light in the wreck: the cracks lit as the core wakes, its halos, its lens's once back, its cloud's embers, the arcs and the landings.</summary>
    private void DrawOverAdd()
    {
        if (!Meeting.DrawsDrone(_f.U) || InVision) return;
        _tri.Clear();
        double u = _f.U;
        // Lying, its cracks light over its picture; lifted, on it.
        Cracks(_core, light: true);
        if (SmallCore)
        {
            var at = W(_core.At);
            var (breathing, waking) = Meeting.SmallHalos(u, Meeting.Breath(_f.ClockS), _core.R);
            Halo(at, (float)(5.4 * _core.R + 34), Rgb(Meeting.Ember), (float)breathing);
            Halo(at, (float)(5.4 * _core.R + 26), Rgb(Meeting.Ember), (float)waking);
            if (_face.Lens > 0)
            {
                var lit = Rgb(Meeting.Sign).Lerp(_f.Lens, (float)Meeting.LensShift(u));
                Halo(LightDrone.LensOf(at, (float)_core.Angle), 14 * 0.82f, lit, (float)(0.55 * _face.Glow * _face.Lens));
            }
            if (Phase == MeetingPhase.Home) Embers();
        }
        for (int i = 0; i < _plates.Length; i++)
        {
            var plate = _plates[i];
            float a = (float)Meeting.ArcAlpha(i, u);
            if (a > 0.003f)
            {
                var from = _core.At;
                double dx = plate.At.X - from.X, dy = plate.At.Y - from.Y, d = Math.Sqrt(dx * dx + dy * dy);
                if (d > 0)
                {
                    double off = Math.Min(d * 0.9, 5.4 * _core.R * Meeting.Px);
                    from = new Vec(from.X + dx / d * off, from.Y + dy / d * off);
                }
                var (first, second) = Meeting.ArcSeeds(i, u);
                Arc(from, plate.At, first, a);
                Arc(from, plate.At, second, 0.6f * a);
            }
            Halo(W(plate.At), Flash, Rgb(Meeting.Ember), (float)Meeting.LandFlash(i, u));
        }
        _tri.Flush(_overAdd.GetCanvasItem());
    }

    /// <summary>Where the lying core's picture's top-left is.</summary>
    private Vector2 Box => W(_scene.Core) - W(Meeting.InPicture);

    /// <summary>The cloud on one side of the small core, back from the vision: the drone's own soot, where the blood's rings shrank to.</summary>
    private void Cloud(bool front)
    {
        float aim = (float)_core.Angle, t = (float)_f.SwarmS;
        for (int i = 0; i < MeetingScene.Drops; i++)
        {
            var mote = DroneSwarm.MoteIn(SwarmShape.Idle, i, aim, t);
            if (mote.Front == front) LightDrone.Mote(_tri, (float)_core.At.X + mote.X, (float)_core.At.Y + mote.Y, mote.Side, mote.Dark);
        }
    }

    /// <summary>The hot motes' heat in the cloud, as the drone's own glow adds it.</summary>
    private void Embers()
    {
        float aim = (float)_core.Angle, t = (float)_f.SwarmS;
        for (int i = 0; i < MeetingScene.Drops; i++)
        {
            var mote = DroneSwarm.MoteIn(SwarmShape.Idle, i, aim, t);
            if (mote.Heat <= 0.05f) continue;
            Square(new Vector2((float)_core.At.X + mote.X, (float)_core.At.Y + mote.Y), mote.Side,
                new Color(LightDrone.Ember.Lerp(Colors.White, 0.25f * mote.Heat), 0.95f * mote.Heat));
        }
    }

    /// <summary>The plates home on one side of the small core.</summary>
    private void Homes(bool front)
    {
        for (int i = 0; i < _plates.Length; i++)
            if (_plates[i].Stage == PlateStage.Home && _homes[i].Front == front) LightDrone.DrawPlate(_over, i, _homes[i], W(_core.At));
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

    /// <summary>The giant core: its body lit from above, the blood soaked into it, its pits and rust and cracks turned with its lens, the lens and its light.</summary>
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
        _giant.DrawSetTransform(Vector2.Zero, 0);
        if (Big)
        {
            _tri.Clear();
            Cracks(_core, light: false);
            _tri.Flush(_giant.GetCanvasItem());
        }
        _giant.DrawSetTransform(at, angle);
        _giant.DrawTextureRect(_lens!, box, false);
        if (_face.Lens > 0.002) _giant.DrawTextureRect(_lensLit!, box, false, new Color(1, 1, 1, (float)_face.Lens));
        _giant.DrawSetTransform(Vector2.Zero, 0);
    }

    /// <summary>The giant's light: its cracks while small (the light added), its lens's halo.</summary>
    private void DrawGiantLight()
    {
        _tri.Clear();
        if (!Big) Cracks(_core, light: true);
        if (_face.Lens > 0.002)
        {
            double big = Meeting.Ss(_core.R, 2, 20);
            var lens = W(_core.At) + Vector2.FromAngle((float)_core.Angle) * (float)(2.2 * _core.R * Meeting.Px);
            Halo(lens, (float)(14 * 0.82 * Meeting.Lerp(1, 0.5, big)), Rgb(Meeting.Sign), (float)(0.55 * _face.Glow * _face.Lens * Meeting.Lerp(1, 0.3, big)));
        }
        _tri.Flush(_giantAdd.GetCanvasItem());
    }

    /// <summary>Whether the core is big enough that its cracks are painted on its pale skin rather than added as light (the pick's <c>big &gt; .5</c>).</summary>
    private bool Big => Meeting.Ss(_core.R, 3, 12) > 0.5;

    /// <summary>The blood's drops on one side of the core: a tail, a darker rim, the drop, its sheen and lit top; or, <paramref name="light"/>, their embers.</summary>
    private void Drops(Node2D on, bool front, bool light)
    {
        _tri.Clear();
        var palm = V(_f.Palm);
        double glows = Meeting.Ss(_core.R, 3, 10);
        for (int i = 0; i < MeetingScene.Drops; i++)
        {
            if (_scene.DropAt(i, _f.U, palm, _core) is not { } drop || drop.Front != front) continue;
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

    // ---------- both ----------

    /// <summary>
    /// The cracks open over <paramref name="core"/>, turned with its lens: a wide glow, then a line; added as light while it is small
    /// (<paramref name="light"/>, the line paled), painted on the giant's skin, a twig off each.
    /// </summary>
    private void Cracks(MeetingCore core, bool light)
    {
        if (_face.CrackAlpha <= 0.003 || _face.Cracks <= 0) return;
        var colour = Rgb(_face.CrackColour);
        var line = light ? colour.Lerp(Colors.White, 0.35f) : colour;
        float alpha = (float)_face.CrackAlpha, glowW = (float)(Meeting.Pen(core.R, CrackGlow, CrackGlowMost) * Meeting.Px);
        float lineW = (float)(Meeting.Pen(core.R, CrackLine, CrackLineMost) * Meeting.Px);
        Span<Vec> points = stackalloc Vec[Meeting.CrackPoints];
        for (int k = 0; k < Meeting.CrackCount; k++)
        {
            int n = Meeting.CrackAt(k, _face.Cracks, points);
            for (int j = 0; j + 1 < n; j++) Line(OnCore(core, points[j]), OnCore(core, points[j + 1]), glowW, new Color(colour, 0.28f * alpha));
            for (int j = 0; j + 1 < n; j++) Line(OnCore(core, points[j]), OnCore(core, points[j + 1]), lineW, new Color(line, 0.95f * alpha));
            if (light) continue;
            n = Meeting.CrackAt(k, _face.Cracks, points, twig: true);
            for (int j = 0; j + 1 < n; j++) Line(OnCore(core, points[j]), OnCore(core, points[j + 1]), lineW * 0.7f, new Color(colour.Lerp(Colors.White, 0.2f), 0.75f * alpha));
        }
    }


    /// <summary>A point <paramref name="p"/> in core units on <paramref name="core"/>, turned with its lens, in world pixels.</summary>
    private static Vector2 OnCore(MeetingCore core, Vec p)
    {
        var q = Meeting.Rot(p.X, p.Y, core.Angle);
        double unit = core.R * Meeting.Px;
        return new Vector2((float)(core.At.X + q.X * unit), (float)(core.At.Y + q.Y * unit));
    }

    /// <summary>A flickering ember arc: a wide glow, then a pale line.</summary>
    private void Arc(Vec from, Vec to, int seed, float alpha)
    {
        int n = Meeting.Arc(from, to, seed, _points);
        var ember = Rgb(Meeting.Ember);
        var pale = ember.Lerp(Colors.White, 0.5f);
        for (int j = 0; j + 1 < n; j++) Line(W(_points[j]), W(_points[j + 1]), ArcGlow * (float)Meeting.Px, new Color(ember, 0.25f * alpha));
        for (int j = 0; j + 1 < n; j++) Line(W(_points[j]), W(_points[j + 1]), ArcLine * (float)Meeting.Px, new Color(pale, 0.9f * alpha));
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
