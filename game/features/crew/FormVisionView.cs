using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using I = Stillwake.Game.Shared.DroneInk;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A form's vision as this player sees it (form-vision, the picks «Проступает схема» and «Кадр следует»), drawn as <see cref="FormVision"/>
/// places it: on a screen layer of its own over the world (<see cref="Vision"/>), the world going black, the void's stars, the form's scene and a
/// drone of its own flying out before the floating hero, whose figure is moved onto the layer meanwhile. The scene is the pick's, in its px on a
/// stage at the hero's feet, mirrored for a hero facing right: the game's pictures until the drone takes them, then a thread of sparks running
/// over each, the picture darkening and its diagram staying, the scanner's dots flying out one by one. The layer glides in to each thing over its
/// zoom at rest. Unlit, never a node per thing.
/// </summary>
public partial class FormVisionView : Node2D
{
    /// <summary>The meteor's ember, an alarm's reds: the vision's threads and dots are the player's (<see cref="EmberArt.Mine"/>).</summary>
    private static readonly Color Ember = Rgb(I.Ember), Red = Rgb(I.Red), Hot = Ember.Lerp(Red, 0.5f), CrackHot = Ember.Lerp(Red, 0.75f);
    private static readonly Color LampLight = Rgb((255, 236, 206)), Ice = Rgb((206, 224, 236)), Ink = Rgb((22, 23, 27)), CableLit = Rgb((120, 126, 134));
    private static readonly Color CrackLit = Rgb((160, 168, 178)), Dark = Rgb((3, 4, 7)), Star = Rgb((206, 216, 232)), Shade = Rgb((2, 3, 5));
    private static readonly Color FireTail = Rgb((255, 190, 120)), FireHead = Rgb((255, 214, 168)), DotDark = Rgb(I.Dark);

    /// <summary>The pick's widths and reaches, its px: a thread's glow and its line, the beam's and the meteor's path's reach, a dash of the path and its gap.</summary>
    private const float Wide = 2.4f, Fine = 0.7f, BeamReach = 900, PathReach = 700, Dash = 4, DashGap = 7;

    /// <summary>The meteor's fire for each share of its size, the pick's px: its tail, its half-width, its halo.</summary>
    private const float FireLength = 300, FireHalf = 16, FireHalo = 60;

    /// <summary>The red round a cracked piece's blow: how far, how strong.</summary>
    private const float BlowHalo = 26, BlowAlpha = 0.3f;

    /// <summary>How many times over a picture is kept, its pixels whole, so it stays crisp while the view glides between whole zooms.</summary>
    private const int Kept = 2;

    /// <summary>How many sides the vignette's ring has.</summary>
    private const int ShadeSides = 48;

    /// <summary>The screen layer the vision is drawn on, over the world, zoomed to it (<see cref="Zoomed"/>): the hero's figure goes onto it meanwhile.</summary>
    public CanvasLayer Vision { get; } = new() { Layer = ScreenLayers.Vision, Visible = false };

    private static readonly Dictionary<string, Texture2D> Pictures = [];

    /// <summary>
    /// The pick's frame at the hero's feet, and on it, back to front: the void and its stars; the light under the things (the beam, the bulb's halo,
    /// the meteor's fire); the things; the drone's threads; the gauge's dots over the drone, their dark and their light; the vignette over all,
    /// the hero and the drone too.
    /// </summary>
    private readonly Node2D _stage = new(), _void = new(), _under = new(), _things = new(), _threads = new(), _dotDark = new(), _dotLight = new(), _shade = new();
    private readonly LightDrone _drone = new();
    private Salts _salts;
    /// <summary>The player's colour, as its threads are drawn.</summary>
    private Color _own;
    private readonly Triangles _tri = new(512, 768);
    private readonly List<Vec[]> _cut = [];
    private readonly List<Vector2> _segments = [];
    private readonly Vector2[] _shadePoints = new Vector2[2 * ShadeSides];
    private readonly Color[] _shadeColours = new Color[2 * ShadeSides];
    private static readonly int[] ShadeTriangles = RingTriangles();
    private VisionFrame _f;
    private double _clockMs;
    /// <summary>
    /// The screen as the pick's px have it this frame, its left the side the hero faces: with the view at rest (what the void and the cold are
    /// laid over), and as it has glided (what the vignette is).
    /// </summary>
    private Rect2 _page, _seen;
    /// <summary>The drone and its eye in the pick's px this frame.</summary>
    private Vector2 _d, _eye;
    /// <summary>The gauge's dots this frame and how far each is out, and the reticle's marks they leave, the pick's px.</summary>
    private readonly VisionDot?[] _dots = new VisionDot?[I.Dots];
    private readonly float[] _away = new float[I.Dots];
    private readonly Vector2[] _seats = new Vector2[I.Dots];

    public FormVisionView()
    {
        TopLevel = true;
        AddChild(Vision);
        Vision.AddChild(_stage);
        foreach (var (node, z, adds) in new (Node2D, int, bool)[]
        {
            (_void, -20, false), (_under, -11, true), (_things, -10, false), (_threads, -9, true),
            (_dotDark, Layers.Drones + 1, false), (_dotLight, Layers.Drones + 2, true), (_shade, 40, false),
        })
            _stage.AddChild(Unlit(node, z, adds));
        _drone.Scripted = true;
        Vision.AddChild(_drone);
        _void.Draw += DrawVoid;
        _under.Draw += DrawUnder;
        _things.Draw += DrawThings;
        _threads.Draw += DrawThreads;
        _dotDark.Draw += DrawDotDark;
        _dotLight.Draw += DrawDotLight;
        _shade.Draw += DrawShade;
    }

    /// <summary>Reads the visions' pictures ahead, once, so none is read as a vision plays.</summary>
    public static void Load()
    {
        foreach (var prop in FormVision.Props.All) PictureOf(prop);
    }

    /// <summary>The vision's drone's reticle marks, its eye and how red it burns, world px, as a lesson reads the hero's drone.</summary>
    public ((float X, float Y)[] Seats, Vector2 Eye, float Tint)? DroneScan => _drone.Visible ? (_drone.Seats, _drone.LensBox.GetCenter(), _drone.Tint) : null;

    /// <summary>The point of the world the vision's drone looks at in <paramref name="frame"/>, for a hero at <paramref name="feet"/> facing <paramref name="facing"/>: where its head turns.</summary>
    public static Vector2 AimOf(VisionFrame frame, Vector2 feet, int facing) =>
        StageOf(feet, facing) * W(frame.Drone.At) + Turned(new Vector2(MathF.Cos((float)frame.Drone.Look), MathF.Sin((float)frame.Drone.Look)) * 60, facing);

    /// <summary>
    /// Shows <paramref name="frame"/> for a hero held at <paramref name="feet"/>, world px, facing <paramref name="facing"/>, its drone coming out
    /// from <paramref name="home"/>, where it hung by the head, and going back there.
    /// </summary>
    public void Show(VisionFrame frame, Vector2 feet, int facing, Vector2 home, double elapsedMs)
    {
        _f = frame;
        _clockMs += elapsedMs;
        Vision.Visible = true;
        var stage = StageOf(feet, facing);
        _stage.Transform = stage;
        var toPage = stage.AffineInverse();
        var viewport = GetViewport();
        var screen = viewport.GetVisibleRect();
        var rest = Zoomed(stage, viewport.GetCanvasTransform(), screen, (float)frame.Veil);
        Vision.Transform = rest * Glide(stage, frame.Cam);
        (_page, _seen) = (PageOf(rest, toPage, screen), PageOf(Vision.Transform, toPage, screen));

        var drone = frame.Drone;
        var goal = home.Lerp(stage * W(drone.At), (float)frame.Veil);
        var look = Turned(new Vector2(MathF.Cos((float)drone.Look), MathF.Sin((float)drone.Look)), facing);
        _drone.Visible = true;
        (_salts, _drone.Salts) = (EmberArt.Mine, EmberArt.Mine);
        _own = EmberArt.Of(_salts.Line());
        _drone.Follow(V(goal), default, new DroneIntent(V(goal), V(look), drone.Red, false, drone.Thought, default), true, elapsedMs, _clockMs);
        _drone.Scanning = drone.Shape == SwarmShape.Scan;
        _drone.Idle = drone.Shape is not (SwarmShape.Scan or SwarmShape.Lamp);
        _drone.ShapeOver = (float)drone.ShapeOver;
        for (int j = 0; j < _dots.Length; j++)
        {
            _dots[j] = frame.Form == VisionForm.Scan ? FormVision.DotAt(j, frame.U) : null;
            _away[j] = (float)(_dots[j]?.Away ?? 0);
        }
        _drone.Gauge(drone.Reads, _away);
        _drone.Sign(null);
        (_d, _eye) = (toPage * _drone.GlobalPosition, toPage * _drone.LensBox.GetCenter());
        for (int j = 0; j < _seats.Length; j++) _seats[j] = toPage * new Vector2(_drone.Seats[j].X, _drone.Seats[j].Y);

        foreach (var node in (ReadOnlySpan<Node2D>)[_void, _under, _things, _threads, _dotDark, _dotLight, _shade]) node.QueueRedraw();
    }

    /// <summary>
    /// The vision's layer at rest over the screen: as the void comes, from the world's camera to the vision's own zoom
    /// (<see cref="FormVision.Zoom"/>), the pick's frame's middle eased to the screen's, in whole screen px; the glide over it keeps its fractions, so it never jitters.
    /// </summary>
    private static Transform2D Zoomed(Transform2D stage, Transform2D camera, Rect2 screen, float veil)
    {
        var middle = stage * W(FormVision.Middle);
        float zoom = Mathf.Lerp(camera.X.Length(), FormVision.Zoom(screen.Size.Y), veil);
        var at = (camera * middle).Lerp(screen.GetCenter(), veil) - middle * zoom;
        return new Transform2D(new Vector2(zoom, 0), new Vector2(0, zoom), at.Round());
    }

    /// <summary>The frame's glide («Кадр следует») within the layer at rest, in the world's px: the point it follows brought to the pick's middle, zoomed about it.</summary>
    private static Transform2D Glide(Transform2D stage, VisionCam cam)
    {
        float zoom = (float)cam.Zoom;
        return new Transform2D(new Vector2(zoom, 0), new Vector2(0, zoom), stage * W(FormVision.Middle) - zoom * (stage * W(cam.Middle)));
    }

    /// <summary>The screen under <paramref name="layer"/> in the pick's px, whichever way they are mirrored.</summary>
    private static Rect2 PageOf(Transform2D layer, Transform2D toPage, Rect2 screen)
    {
        var world = layer.AffineInverse() * screen;
        return new Rect2(toPage * world.Position, Vector2.Zero).Expand(toPage * world.End)
            .Expand(toPage * new Vector2(world.Position.X, world.End.Y)).Expand(toPage * new Vector2(world.End.X, world.Position.Y));
    }

    /// <summary>The pick's px to the world's: its hero's feet on <paramref name="feet"/>, a world px four of its, mirrored for a hero facing right (the pick's faces left).</summary>
    private static Transform2D StageOf(Vector2 feet, int facing)
    {
        float k = (float)(1 / FormVision.PerWorldPx), across = -facing * k;
        return new Transform2D(new Vector2(across, 0), new Vector2(0, k), feet - new Vector2(across * (float)FormVision.Feet.X, k * (float)FormVision.Feet.Y));
    }

    /// <summary>A way in the pick's px as the world turns it for a hero facing <paramref name="facing"/>, a world px each.</summary>
    private static Vector2 Turned(Vector2 way, int facing) => new(-facing * way.X / (float)FormVision.PerWorldPx, way.Y / (float)FormVision.PerWorldPx);

    private float Clock => (float)(_clockMs / 1000);

    // ---------- the void ----------

    private void DrawVoid()
    {
        float v = (float)_f.Veil;
        _void.DrawRect(_page.Merge(_seen).Grow(4), new Color(Dark, v));
        _tri.Clear();
        float spread = Math.Max(1, _page.Size.X / (float)FormVision.Frame.X);
        foreach (var (x, y, side, alpha) in FormVision.Stars)
            _tri.Rect(_page.Position.X + (float)(x / FormVision.Frame.X) * _page.Size.X, _page.Position.Y + (float)(y / FormVision.Frame.Y) * _page.Size.Y,
                (float)side * spread, (float)side * spread, new Color(Star, (float)alpha * v));
        _tri.Flush(_void.GetCanvasItem());
    }

    /// <summary>The vignette over it all: clear in the middle, darkening to the corners as the void comes.</summary>
    private void DrawShade()
    {
        float v = (float)_f.Veil;
        if (v <= 0.003f) return;
        var middle = _seen.Position + _seen.Size * new Vector2(0.55f, 0.5f);
        float outer = _seen.Size.Length() * 0.6f, inner = outer * 0.3f;
        for (int i = 0; i < ShadeSides; i++)
        {
            var way = Vector2.FromAngle(Mathf.Tau * i / ShadeSides);
            (_shadePoints[2 * i], _shadeColours[2 * i]) = (middle + way * inner, new Color(Shade, 0));
            (_shadePoints[2 * i + 1], _shadeColours[2 * i + 1]) = (middle + way * outer, new Color(Shade, 0.6f * v));
        }
        _tri.Clear();
        _tri.Polygon(_shadePoints, _shadeColours, ShadeTriangles);
        _tri.Flush(_shade.GetCanvasItem());
    }

    private static int[] RingTriangles()
    {
        var all = new int[ShadeSides * 6];
        for (int i = 0; i < ShadeSides; i++)
        {
            int a = 2 * i, b = 2 * ((i + 1) % ShadeSides);
            all[6 * i] = a;
            all[6 * i + 1] = a + 1;
            all[6 * i + 2] = b + 1;
            all[6 * i + 3] = a;
            all[6 * i + 4] = b + 1;
            all[6 * i + 5] = b;
        }
        return all;
    }

    // ---------- the light under the things ----------

    private void DrawUnder()
    {
        float v = (float)_f.Veil;
        _tri.Clear();
        // A soft cold light behind the hero, so his figure reads against the void (the pick draws him at his own colours on its black).
        var middle = W(FormVision.Feet) - new Vector2(0, (float)((FormVision.Float(_f.Form, _f.U).Lift + FormVision.HeroTall / 2) * FormVision.PerWorldPx));
        Halo(middle, 200, Star, 0.14f * v);
        Halo(middle, 110, Star, 0.1f * v);
        if (_f.Lamp is { } lamp && _f.First is { } hung)
        {
            float lit = (float)lamp.Light * v;
            if (lit > 0.003f)
            {
                float a = (float)_f.Drone.Look, spread = MathF.Tan((float)FormVision.Half);
                Span<Vector2> cone = [new(2, -3), new(BeamReach, -BeamReach * spread), new(BeamReach, BeamReach * spread), new(2, 3)];
                for (int i = 0; i < cone.Length; i++) cone[i] = _d + cone[i].Rotated(a);
                Color near = new(LampLight, 0.22f * lit), far = new(LampLight, 0);
                _tri.Polygon(cone, [near, far, far, near], [0, 1, 2, 0, 2, 3]);
            }
            Halo(W(lamp.Bulb), 46, LampLight, 0.28f * (float)hung.Alpha * v);
        }
        if (_f.Meteor is { } meteor)
        {
            float a = (float)meteor.Alpha * v, size = (float)meteor.Scale;
            Vector2 head = W(meteor.Head), way = W(meteor.Way), side = new Vector2(-way.Y, way.X) * FireHalf * size;
            var bright = new Color(FireHead, 0.8f * a);
            if (a > 0.003f) _tri.Polygon([head - way * FireLength * size, head + side, head - side], [new Color(FireTail, 0), bright, bright], [0, 1, 2]);
            Halo(head, FireHalo * size, Ember, 0.5f * a);
        }
        _tri.Flush(_under.GetCanvasItem());
    }

    // ---------- the things, the game's pictures ----------

    private void DrawThings()
    {
        float v = (float)_f.Veil;
        switch (_f.Form)
        {
            case VisionForm.Scan:
                foreach (var shown in (ReadOnlySpan<PropShown?>)[_f.First, _f.Second])
                    if (shown is { } q)
                    {
                        float alpha = (float)(q.Alpha * v * (1 - FormVision.TraceDark * q.Trace));
                        Picture(q, alpha);
                        if (q.Cracked) InkCracks(q, alpha);
                    }
                break;
            case VisionForm.Lamp when _f.Lamp is { } lamp && _f.First is { } hung && _f.Second is { } crate:
                float a = (float)hung.Alpha * v;
                Vector2[] cable = [W(lamp.Pivot), W(lamp.Top)];
                _things.DrawPolyline(cable, new Color(Ink, a), 1.6f, true);
                _things.DrawPolyline(cable, new Color(CableLit, 0.5f * a), 0.6f, true);
                // The lamp darkens by half under its diagram, as the pick has it.
                Picture(hung, a * (float)(1 - FormVision.LampDark * hung.Trace));
                CrateInBeam(crate, lamp, v);
                break;
            case VisionForm.Alarms:
                if (_f.Frost is { } frost) Frost(frost, v);
                if (_f.Meteor is { } meteor) Picture(Rock(meteor), (float)meteor.Alpha * v);
                break;
        }
    }

    /// <summary>The meteor's rock as it flies, turning.</summary>
    private static PropShown Rock(VisionMeteor meteor) =>
        new(FormVision.Props.Rock with { Scale = meteor.Scale }, meteor.Head, meteor.Spin, meteor.Alpha, meteor.Trace, false);

    /// <summary>A prop's picture where it is shown, <paramref name="alpha"/> opaque.</summary>
    private void Picture(PropShown q, float alpha)
    {
        if (alpha <= 0.003f) return;
        float w = (float)(q.Prop.W * q.Prop.Scale), h = (float)(q.Prop.H * q.Prop.Scale);
        _things.DrawSetTransform(W(q.At), (float)q.Angle, Vector2.One);
        _things.DrawTextureRect(PictureOf(q.Prop), new Rect2(-w / 2, -h / 2, w, h), false, new Color(1, 1, 1, alpha));
        _things.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    /// <summary>A cracked prop's cracks in ink, a pale edge under them, as a wreck's are drawn.</summary>
    private void InkCracks(PropShown q, float alpha)
    {
        if (alpha <= 0.003f) return;
        foreach (var line in q.Prop.Cracks)
        {
            var points = Placed(q, line);
            var lit = new Vector2[points.Length];
            for (int i = 0; i < points.Length; i++) lit[i] = points[i] + new Vector2(0.7f, 0.9f);
            _things.DrawPolyline(lit, new Color(CrackLit, 0.35f * alpha), 0.7f, true);
            _things.DrawPolyline(points, new Color(Ink, 0.95f * alpha), 1.3f, true);
        }
    }

    /// <summary>The crate, seen only where the beam falls: its picture cut to the beam's cone.</summary>
    private void CrateInBeam(PropShown crate, VisionLamp lamp, float v)
    {
        float alpha = (float)(lamp.Light * (1 - FormVision.TraceDark * crate.Trace)) * v;
        if (lamp.Light * lamp.InCone * v <= 0.003) return;
        float a = (float)_f.Drone.Look, half = (float)FormVision.Half;
        Vector2 d = _d, left = _d + Vector2.FromAngle(a - half) * BeamReach, right = _d + Vector2.FromAngle(a + half) * BeamReach;
        var corners = new List<(Vector2 At, Vector2 Uv)>(8);
        foreach (var (x, y) in (ReadOnlySpan<(double, double)>)[(0, 0), (1, 0), (1, 1), (0, 1)])
            corners.Add((W(FormVision.Placed(crate, new Vec(x * crate.Prop.W, y * crate.Prop.H))), new Vector2((float)x, (float)y)));
        var cut = Clip(Clip(Clip(corners, d, left, right), left, right, d), right, d, left);
        if (cut.Count < 3) return;
        var points = new Vector2[cut.Count];
        var uvs = new Vector2[cut.Count];
        var colours = new Color[cut.Count];
        for (int i = 0; i < cut.Count; i++) (points[i], uvs[i], colours[i]) = (cut[i].At, cut[i].Uv, new Color(1, 1, 1, alpha));
        _things.DrawPolygon(points, colours, uvs, PictureOf(crate.Prop));
    }

    /// <summary>The part of <paramref name="polygon"/> on <paramref name="c"/>'s side of the line from <paramref name="a"/> to <paramref name="b"/>, its picture's places carried.</summary>
    private static List<(Vector2 At, Vector2 Uv)> Clip(List<(Vector2 At, Vector2 Uv)> polygon, Vector2 a, Vector2 b, Vector2 c)
    {
        var kept = new List<(Vector2, Vector2)>(polygon.Count + 2);
        float side = (b - a).Cross(c - a);
        float Inside(Vector2 p) => (b - a).Cross(p - a) * side;
        for (int i = 0; i < polygon.Count; i++)
        {
            var (p, q) = (polygon[i], polygon[(i + 1) % polygon.Count]);
            float ip = Inside(p.At), iq = Inside(q.At);
            if (ip >= 0) kept.Add(p);
            if (ip >= 0 != iq >= 0)
            {
                float t = ip / (ip - iq);
                kept.Add((p.At.Lerp(q.At, t), p.Uv.Lerp(q.Uv, t)));
            }
        }
        return kept;
    }

    /// <summary>
    /// The cold from the edge the hero faces: a watercolour wash to its wavy front, the front's line, crystals growing behind it. The pick's
    /// frame ends where it creeps from: on a wider screen it sets off from the screen's own edge and comes to rest where the pick has it.
    /// </summary>
    private void Frost(VisionFrost frost, float v)
    {
        float a = (float)frost.Alpha * v;
        if (a <= 0.003f) return;
        float shift = FrostShift(frost.Front), x = (float)frost.Front;
        var front = FrontLine(x + shift);
        float edge = Math.Min(0, Math.Min(_page.Position.X, _seen.Position.X)), reach = Math.Max(x + shift + 30 - edge, 1);
        var points = new Vector2[2 * front.Length];
        var colours = new Color[points.Length];
        var triangles = new int[6 * (front.Length - 1)];
        for (int i = 0; i < front.Length; i++)
        {
            (points[2 * i], colours[2 * i]) = (new Vector2(edge - 4, front[i].Y), new Color(Ice, 0.3f * a));
            float along = Math.Clamp((front[i].X - edge) / reach, 0, 1);
            (points[2 * i + 1], colours[2 * i + 1]) = (front[i], new Color(Ice, Mathf.Lerp(0.3f, 0.04f, along) * a));
            if (i == 0) continue;
            int k = 6 * (i - 1), p = 2 * (i - 1), q = 2 * i;
            (triangles[k], triangles[k + 1], triangles[k + 2], triangles[k + 3], triangles[k + 4], triangles[k + 5]) = (p, p + 1, q + 1, p, q + 1, q);
        }
        _tri.Clear();
        _tri.Polygon(points, colours, triangles);
        _tri.Flush(_things.GetCanvasItem());
        _things.DrawPolyline(front, new Color(Ice, 0.3f * a), 1, true);
        _segments.Clear();
        float down = _page.Size.Y / (float)FormVision.Frame.Y;
        foreach (var (cx, cy, size, turn, _) in FormVision.Crystals)
        {
            float grown = (float)FormVision.Sm((x - cx) / 40);
            if (grown > 0.01f) Crystal(new Vector2(shift + (float)cx, _page.Position.Y + (float)cy * down), (float)size * grown, (float)turn);
        }
        if (_segments.Count > 0) _things.DrawMultiline([.. _segments], new Color(Ice, 0.7f * a), 0.8f, true);
    }

    /// <summary>How far toward a wider screen's edge the cold still is, with its front at <paramref name="front"/>: none once it has come to rest.</summary>
    private float FrostShift(double front) => Math.Min(0, _page.Position.X) * (1 - (float)Math.Clamp((front + 10) / 270, 0, 1));

    /// <summary>The front of the cold at <paramref name="x"/>, the pick's px, down the whole screen.</summary>
    private Vector2[] FrontLine(double x)
    {
        float top = Math.Min(_page.Position.Y, _seen.Position.Y) - 4, bottom = Math.Max(_page.End.Y, _seen.End.Y) + 4;
        var line = new Vector2[(int)MathF.Ceiling((bottom - top) / 12) + 1];
        for (int i = 0; i < line.Length; i++)
        {
            float y = top + 12 * i;
            line[i] = new Vector2((float)FormVision.EdgeX(x, y), y);
        }
        return line;
    }

    /// <summary>A six-armed crystal at <paramref name="at"/>, arms <paramref name="size"/> long, two twigs off each, into the segments.</summary>
    private void Crystal(Vector2 at, float size, float turn)
    {
        for (int m = 0; m < 6; m++)
        {
            float a = turn + m * Mathf.Pi / 3;
            var mid = at + Vector2.FromAngle(a) * size * 0.55f;
            _segments.Add(at);
            _segments.Add(at + Vector2.FromAngle(a) * size);
            foreach (float b in (ReadOnlySpan<float>)[-0.7f, 0.7f])
            {
                _segments.Add(mid);
                _segments.Add(mid + Vector2.FromAngle(a + b) * size * 0.32f);
            }
        }
    }

    // ---------- the drone's threads over them ----------

    private void DrawThreads()
    {
        float v = (float)_f.Veil, t = Clock;
        _tri.Clear();
        switch (_f.Form)
        {
            case VisionForm.Scan:
                foreach (var shown in (ReadOnlySpan<PropShown?>)[_f.First, _f.Second])
                    if (shown is { } q)
                    {
                        float k = (float)q.Alpha * v;
                        Diagram(q, (float)q.Trace, k, _own, t);
                        if (q.Cracked && q.Trace > 0)
                            Halo(W(FormVision.Placed(q, q.Prop.Blow)), BlowHalo, Red, BlowAlpha * (float)q.Trace * k * (0.75f + 0.25f * MathF.Sin(t * 5)));
                    }
                break;
            case VisionForm.Lamp when _f.Lamp is { } lamp && _f.First is { } hung && _f.Second is { } crate:
                float f = (float)hung.Trace, a = (float)hung.Alpha * v;
                if (f > 0)
                {
                    Cut([[lamp.Pivot, lamp.Top]], f);
                    Threads(a, _own, 0.6f);
                    Diagram(hung, f, a, _own, t);
                }
                float lit = (float)lamp.Light * v, inBeam = lit * (float)lamp.InCone;
                if (inBeam > 0.003f) Halo(W(crate.At), 70, LampLight, 0.18f * inBeam);
                Diagram(crate, (float)crate.Trace, lit, _own, t);
                // The drone copies the dish: a thread from its eye to the bulb, a spark running home along it.
                float study = (float)lamp.Study * a;
                if (study > 0.003f)
                {
                    var bulb = W(lamp.Bulb);
                    _tri.Quad(_eye, bulb, 0.4f, new Color(_own, 0.25f * study));
                    Halo(bulb.Lerp(_eye, (float)FormVision.Sm(lamp.StudyRun)), 5, _own, 0.7f * study);
                }
                break;
            case VisionForm.Alarms:
                if (_f.Frost is { } frost && frost.Alpha * v > 0.003 && frost.Trace > 0)
                {
                    var front = FrontLine(frost.Front + FrostShift(frost.Front));
                    var line = new Vec[front.Length];
                    for (int i = 0; i < front.Length; i++) line[i] = V(front[i]);
                    Cut([line], frost.Trace);
                    Threads((float)frost.Alpha * v, _own, 1);
                }
                if (_f.Meteor is { } meteor)
                {
                    float shows = (float)meteor.Alpha * v, taken = (float)meteor.Trace;
                    Diagram(Rock(meteor), taken, shows, Hot, t);
                    // Its path ahead, dashed, as the drone foresees it.
                    var path = new Color(Hot, 0.45f * taken * shows);
                    Vector2 head = W(meteor.Head), way = W(meteor.Way);
                    if (path.A > 0.003f)
                        for (float s = 0; s < PathReach; s += Dash + DashGap) _tri.Quad(head + way * s, head + way * Math.Min(s + Dash, PathReach), 0.8f, path);
                }
                break;
        }
        _tri.Flush(_threads.GetCanvasItem());
    }

    /// <summary>
    /// What the drone has understood of <paramref name="q"/>, <paramref name="f"/> of it: its outline and its main lines in the player's threads drawn up to
    /// that share, a cracked one's cracks flickering red, and sparks running round its outline as far as it is drawn.
    /// </summary>
    private void Diagram(PropShown q, float f, float alpha, Color hot, float t)
    {
        if (f <= 0.003f || alpha <= 0.003f) return;
        var outline = PlacedVec(q, q.Prop.Outline);
        Cut([outline], f);
        Threads(alpha, hot, 1);
        var lines = new List<Vec[]>(q.Prop.Lines.Length);
        foreach (var line in q.Prop.Lines) lines.Add(PlacedVec(q, line));
        Cut(lines, f);
        Threads(alpha, hot, 0.6f);
        if (q.Cracked && q.Prop.Cracks.Length > 0)
        {
            var cracks = new List<Vec[]>(q.Prop.Cracks.Length);
            foreach (var crack in q.Prop.Cracks) cracks.Add(PlacedVec(q, crack));
            Cut(cracks, f);
            Threads(alpha * (0.7f + 0.3f * MathF.Sin(t * 17)), CrackHot, 1.1f);
        }
        double length = FormVision.LengthOf(outline);
        if (length < 1e-6) return;
        int count = (int)Math.Ceiling(length / 12);
        for (int i = 0; i < count; i++)
        {
            double s = ((i + 0.5) / count + t * 28 / length) % 1;
            if (s > f) continue;
            var (at, heading) = FormVision.OnLine(outline, length, s);
            float heat = (0.7f + 0.25f * MathF.Sin(t * 13 + i)) * alpha;
            for (int m = 0; m < 3; m++)
            {
                float back = m * 1.3f;
                var colour = new Color(Paled(hot, 0.3f * (1 - m / 3f)), (0.95f - m * 0.3f) * heat);
                _tri.Rect((float)(at.X - heading.X * back) - 0.6f, (float)(at.Y - heading.Y * back) - 0.6f, 1.2f, 1.2f, colour);
            }
        }
    }

    /// <summary>Cuts <paramref name="lines"/> to share <paramref name="f"/> of their length, into the lines drawn next.</summary>
    private void Cut(IReadOnlyList<Vec[]> lines, double f) => FormVision.Cut(lines, f, _cut);

    /// <summary>The lines cut last as the drone's threads: a faint wide glow, a fine pale line over it.</summary>
    private void Threads(float alpha, Color hot, float k)
    {
        if (alpha <= 0.003f) return;
        foreach (var line in _cut)
        {
            if (line.Length < 2) continue;
            var points = new Vector2[line.Length];
            for (int i = 0; i < line.Length; i++) points[i] = W(line[i]);
            _threads.DrawPolyline(points, new Color(hot, 0.08f * alpha * k), Wide, true);
            _threads.DrawPolyline(points, new Color(Paled(hot, 0.2f), 0.55f * alpha * k), Fine, true);
        }
    }

    /// <summary><paramref name="hot"/> toward white: the player's colour as far as it has room, an alarm's red whole.</summary>
    private Color Paled(Color hot, float pale) => hot.Lerp(Colors.White, hot == _own ? pale * _salts.Room : pale);

    // ---------- the gauge's dots ----------

    /// <summary>
    /// Dot <paramref name="j"/> as drawn, the pick's px: where it left and where it is, its heading while it flies, its side growing from a
    /// spark's into a dot's; none while it sits in its mark.
    /// </summary>
    private (Vector2 From, Vector2 At, Vector2 Way, float Side, VisionDot Dot)? DotOf(int j)
    {
        if (_dots[j] is not { } dot) return null;
        Vector2 from = EndOf(dot.From, j), to = EndOf(dot.To, j);
        var way = dot.Along < 1 ? (to - from).Normalized() : Vector2.Zero;
        float side = Mathf.Lerp(I.Spark * I.Px * (float)FormVision.PerWorldPx, I.Dot, (float)dot.Away);
        return (from, from.Lerp(to, (float)dot.Along), way, side, dot);
    }

    private Vector2 EndOf(int end, int j) => end == FormVision.Seat ? _seats[j] : W(FormVision.Rows[end]) + new Vector2((j - (I.Dots - 1) / 2f) * I.DotStep, 0);

    private void DrawDotDark()
    {
        _tri.Clear();
        for (int j = 0; j < _dots.Length; j++)
            if (DotOf(j) is { } d) _tri.Rect(d.At.X - d.Side / 2, d.At.Y - d.Side / 2, d.Side, d.Side, DotDark);
        _tri.Flush(_dotDark.GetCanvasItem());
    }

    /// <summary>Each dot's glow and the tail behind it as it flies, every other's thread back to where it left.</summary>
    private void DrawDotLight()
    {
        _tri.Clear();
        float t = Clock;
        for (int j = 0; j < _dots.Length; j++)
        {
            if (DotOf(j) is not { } d) continue;
            if (d.Way != Vector2.Zero && j % I.DotThreadEvery == 0) _tri.Quad(d.From, d.At, I.ThreadLine, new Color(_own, I.ThreadAlpha));
            float lit = (float)d.Dot.Lit, flicker = I.DotFlicker + I.DotFlickerSwing * MathF.Sin(t * I.DotFlickerPace + j * I.DotFlickerStep);
            float dotHeat = lit > 0 ? (I.DotLit + I.DotLitMore * lit) * flicker : I.DotDark;
            float heat = Math.Min(1, Mathf.Lerp(I.OnShape + I.OnShapeSwing * MathF.Sin(t * I.OnShapePace + j), dotHeat, (float)d.Dot.Away));
            if (heat <= 0.02f) continue;
            float burn = Salts.DotBurn(j, t, (float)d.Dot.Away);
            for (int m = 0; m < I.Tail; m++)
            {
                var at = d.At - d.Way * I.TailStep * m;
                var colour = EmberArt.Of(_salts.Spark(burn, I.TailPale * (1 - m / (float)I.Tail)), (I.TailAlpha - m * I.TailFade) * heat * _salts.Gain);
                _tri.Rect(at.X - d.Side / 2, at.Y - d.Side / 2, d.Side, d.Side, colour);
            }
        }
        _tri.Flush(_dotLight.GetCanvasItem());
    }

    /// <summary>A soft disc of light at <paramref name="at"/>, <paramref name="alpha"/> at its middle, fading out at <paramref name="radius"/>.</summary>
    private void Halo(Vector2 at, float radius, Color colour, float alpha)
    {
        if (alpha > 0.005f) _tri.Fan(at, radius, new Color(colour, alpha), new Color(colour, 0), 24);
    }

    // ---------- helpers ----------

    private static Texture2D PictureOf(VisionProp prop)
    {
        if (Pictures.TryGetValue(prop.Picture, out var texture)) return texture;
        var image = HiRes.Load(prop.Picture) ?? throw new InvalidOperationException($"No picture {prop.Picture}");
        var size = image.GetSize();
        // Grown in whole pixels, then sampled smoothly: crisp at a whole zoom and between, with no shimmer as the view glides.
        image.Resize(size.X * Kept, size.Y * Kept, Image.Interpolation.Nearest);
        return Pictures[prop.Picture] = HiRes.Sampled(ImageTexture.CreateFromImage(HiRes.Mipped(image)), size);
    }

    private static Vector2[] Placed(PropShown q, Vec[] line)
    {
        var points = new Vector2[line.Length];
        for (int i = 0; i < line.Length; i++) points[i] = W(FormVision.Placed(q, line[i]));
        return points;
    }

    private static Vec[] PlacedVec(PropShown q, Vec[] line)
    {
        var points = new Vec[line.Length];
        for (int i = 0; i < line.Length; i++) points[i] = FormVision.Placed(q, line[i]);
        return points;
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

    private static Color Rgb((int R, int G, int B) c) => new(c.R / 255f, c.G / 255f, c.B / 255f);

    private static Vector2 W(Vec v) => new((float)v.X, (float)v.Y);

    private static Vec V(Vector2 v) => new(v.X, v.Y);
}
