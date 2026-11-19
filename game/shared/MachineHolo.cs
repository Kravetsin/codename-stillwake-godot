using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Shared;

/// <summary>
/// A hologram window's hologram, the look «Перо» (<see cref="HoloPen"/>): a beam from the machine (or the drone's lens) to the
/// window's near edge, two laser pens tracing its frame from there, its dark ground fading in under them, the grid's scan
/// line wiping in from the near edge; once open a thin buzzing frame and two flickering beams to its near corners, never
/// across its face. It opens and closes by <see cref="HoloMotion"/>. The first child of its window's parent, so it draws
/// over the world and under every panel; not a control, so it takes no clicks. Its owner steps it every frame with the
/// window and the machine's box, and shows the window's controls by the <see cref="HoloReveal"/> it returns. It keeps the
/// last rectangles it was given, never the window: a window freed as it closes, or a machine destroyed while open, still
/// closes where it was. Redrawn every frame while it shows: the beams follow the hero and the camera. Seen only by the hero
/// who opened it; no world light.
/// </summary>
public partial class MachineHolo : Node2D
{
    /// <summary>
    /// What an owner shows: the machine open, its window on the screen, its box in world pixels, its tone. An empty box (the
    /// drone with no hero to float by) has no beam: the window fades in and out whole where it is.
    /// </summary>
    public readonly record struct Target(int Machine, Rect2 Window, Rect2 Box, HoloTone Tone);

    private readonly HoloMotion _motion = new();
    /// <summary>The light added over the ground: frame, pens, beams and scan line, as the pick's lighter blend.</summary>
    private readonly Node2D _lit = new() { Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add } };
    private readonly TextureRect _emitter = ScreenGlow.Of(Colors.White, 0);
    /// <summary>The glows where the pens' beams end, each pen's.</summary>
    private readonly TextureRect[] _tips = [ScreenGlow.Of(Colors.White, 0), ScreenGlow.Of(Colors.White, 0)];
    private readonly List<(float X, float Y)> _penA = [], _penB = [];
    /// <summary>Polyline points by their count, so tracing allocates nothing.</summary>
    private readonly Vector2[][] _points = [[], [], [], [], [], [], []];
    private Rect2 _window, _box;
    /// <summary>Whether the last box given was empty: no beam, the window faded whole.</summary>
    private bool _beamless;
    /// <summary>This frame's, from <see cref="Place"/>: the window on whole pixels, its frame's line, the touch point on it, the emitter, the beams' ends.</summary>
    private HoloRect _shownWindow, _frame;
    private HoloTouch _touch;
    private Vector2 _from, _endA, _endB;
    private Transform2D _toLocal = Transform2D.Identity;
    private Color _light = Colors.White, _ground = Colors.Black;
    private HoloTone? _tone;
    private double _now;

    public MachineHolo()
    {
        Visible = false;
        TextureFilter = TextureFilterEnum.Linear;
        AddChild(_lit);
        _lit.Draw += DrawLight;
        _emitter.Size = new Vector2(2 * HoloLook.EmitterRadius, 2 * HoloLook.EmitterRadius);
        AddChild(_emitter);
        foreach (var tip in _tips) AddChild(tip);
    }

    /// <summary>
    /// One frame of <paramref name="seconds"/>, <paramref name="open"/> the window open or null once it is shut; how the
    /// window's controls show. A window not laid out yet (no size) holds the hologram where it is and draws nothing new.
    /// </summary>
    public HoloReveal Step(double seconds, Target? open)
    {
        if (open is { } t)
        {
            bool laid = t.Window.Size.X > 0 && t.Window.Size.Y > 0;
            if (laid) (_window, _box, _beamless) = (t.Window, t.Box, t.Box.Size == Vector2.Zero);
            _motion.Step(t.Machine, laid ? seconds : 0);
            if (laid) Tone(t.Tone);
        }
        else _motion.Step(null, seconds);
        _now += seconds;
        Visible = _motion.Shown;
        if (!Visible) return HoloReveal.None;
        Place();
        if (_beamless) return HoloReveal.Whole((float)_motion.Progress);
        var f = _motion.Frame;
        return new HoloReveal(HoloPen.ControlsAlpha(f), _touch.Edge, HoloPen.WipeReach(_shownWindow, _touch.Edge, f.Wiped));
    }

    /// <summary>Where the window, the machine, the pens and the beams' ends are on the screen this frame, the glows on them, and a redraw.</summary>
    private void Place()
    {
        var canvas = GetViewport().GetCanvasTransform();
        float x0 = MathF.Round(_window.Position.X), y0 = MathF.Round(_window.Position.Y);
        float x1 = Math.Max(x0 + 1, MathF.Round(_window.End.X)), y1 = Math.Max(y0 + 1, MathF.Round(_window.End.Y));
        _shownWindow = new HoloRect(x0, y0, x1 - x0, y1 - y0);
        // The 1 px lines on the window's outer pixels, crisp.
        _frame = new HoloRect(x0 + 0.5f, y0 + 0.5f, Math.Max(0, x1 - x0 - 1), Math.Max(0, y1 - y0 - 1));
        _toLocal = GetGlobalTransform().AffineInverse();
        QueueRedraw();
        _lit.QueueRedraw();
        _emitter.Visible = !_beamless;
        foreach (var tip in _tips) tip.Visible = false;
        if (_beamless) return;
        var box = Of(canvas * _box);
        _touch = HoloPen.Inset(_frame, HoloShape.Touch(_frame, box));
        var (ex, ey) = HoloShape.Emitter(box, (_touch.X, _touch.Y), Render.ShipCamera.Cell * canvas.X.Length());
        _from = new Vector2(ex, ey);
        Glow(_emitter, _from, HoloLook.EmitterRadius, Colors.White with { A = HoloLook.EmitterAlpha * (float)(0.88 + 0.12 * Math.Sin(_now * 2.2)) });
        var f = _motion.Frame;
        HoloPen.Path(_frame, _touch, f.Along, 1, _penA);
        HoloPen.Path(_frame, _touch, f.Along, -1, _penB);
        if (f.Reach < 1 || f.Trace <= 0) return;
        _endA = V(_penA[^1]).Lerp(V(HoloPen.Near(_frame, _touch.Edge, 1)), (float)f.Settle);
        _endB = V(_penB[^1]).Lerp(V(HoloPen.Near(_frame, _touch.Edge, -1)), (float)f.Settle);
        bool moving = !f.Traced || f.Settle < 1;
        float radius = moving ? HoloLook.TipRadius : HoloLook.TipSettledRadius;
        var tint = Pale(HoloLook.TipPale) with { A = moving ? HoloLook.TipAlpha : HoloLook.TipSettledAlpha };
        Glow(_tips[0], _endA, radius, tint);
        Glow(_tips[1], _endB, radius, tint);
    }

    /// <summary>Machine <paramref name="machine"/>'s footprint's box in world pixels, where the beam leaves it; empty for one not there.</summary>
    public static Rect2 BoxOf(IGridQueries? grid, int machine)
    {
        if (grid?.MachineById(machine) is not { } m) return default;
        var (x, y, w, h) = HoloShape.BoxOf(grid.Footprint(m.Kind, m.Cell) ?? [m.Cell], grid.Width);
        const int Cell = Render.ShipCamera.Cell;
        return new Rect2(x * Cell, y * Cell, w * Cell, h * Cell);
    }

    /// <summary>Gone at once: another owner's machine opened, or a load or resync.</summary>
    public void Snap()
    {
        _motion.Snap();
        Visible = false;
    }

    private void Tone(HoloTone tone)
    {
        if (_tone == tone) return;
        _tone = tone;
        var tint = HoloLook.Of(tone);
        (_light, _ground) = (new Color(tint.Light), new Color(tint.Ground));
    }

    public override void _Draw()
    {
        // In the canvas layer's pixels, as the window's rectangle and the mapped box are.
        DrawSetTransformMatrix(_toLocal);
        double ground = _beamless ? _motion.Progress : _motion.Frame.Ground;
        if (ground > 0) DrawRect(Rect(_shownWindow), _ground with { A = HoloLook.GroundAlpha * (float)ground });
    }

    /// <summary>The light, added over the ground: the scan line, the frame traced or buzzing, the beams.</summary>
    private void DrawLight()
    {
        _lit.DrawSetTransformMatrix(_toLocal);
        if (_beamless)
        {
            Buzz((float)_motion.Progress);
            return;
        }
        var f = _motion.Frame;
        if (f.Grid > 0 && f.Grid < 1)
        {
            float reach = HoloPen.WipeReach(_shownWindow, _touch.Edge, f.Wiped);
            // The line and its glow lean into the side still to come, as the pick's.
            _lit.DrawRect(Rect(HoloPen.Band(_shownWindow, _touch.Edge, reach + 4 - HoloLook.ScanGlow, reach + 4)), _light with { A = HoloLook.ScanGlowAlpha });
            _lit.DrawRect(Rect(HoloPen.Band(_shownWindow, _touch.Edge, reach - (HoloLook.ScanCore - 1), reach + 1)), Pale(HoloLook.ScanPale) with { A = HoloLook.ScanCoreAlpha });
        }
        if (f.Traced) Buzz(1);
        else if (f.Trace > 0)
        {
            Stroke(_penA, 1);
            Stroke(_penB, 1);
        }
        if (f.Reach < 1 || f.Trace <= 0)
        {
            Beam(_from, _from.Lerp(new Vector2(_touch.X, _touch.Y), (float)f.Reach), HoloLook.ReachAlpha);
            return;
        }
        float flicker = HoloLook.FlickerLow + HoloLook.FlickerDepth * (float)(Math.Sin(_now * 23) * Math.Sin(_now * 9.1));
        Beam(_from, _endA, flicker);
        Beam(_from, _endB, flicker);
    }

    /// <summary>The whole frame, each edge buzzing on its own, at <paramref name="alpha"/>.</summary>
    private void Buzz(float alpha)
    {
        var r = _frame;
        (float X, float Y) tl = (r.X, r.Y), tr = (r.Right, r.Y), br = (r.Right, r.Bottom), bl = (r.X, r.Bottom);
        Edge(tr, tl, 0);
        Edge(tl, bl, 1);
        Edge(bl, br, 2);
        Edge(br, tr, 3);

        void Edge((float X, float Y) a, (float X, float Y) b, int i)
        {
            float buzz = HoloLook.BuzzLow + HoloLook.BuzzDepth * (float)(0.5 + 0.5 * Math.Sin(_now * 31 + i * 2.1) * Math.Sin(_now * 13.7 + i));
            Line(V(a), V(b), alpha * buzz);
        }
    }

    /// <summary>A pen's path as the frame's line: its soft glow and its pale core.</summary>
    private void Stroke(List<(float X, float Y)> path, float alpha)
    {
        if (path.Count < 2 || path.Count >= _points.Length) return;
        var points = _points[path.Count];
        if (points.Length != path.Count) points = _points[path.Count] = new Vector2[path.Count];
        for (int i = 0; i < path.Count; i++) points[i] = V(path[i]);
        _lit.DrawPolyline(points, _light with { A = HoloLook.FrameGlowAlpha * alpha }, HoloLook.FrameGlow, true);
        _lit.DrawPolyline(points, Pale(HoloLook.FramePale) with { A = HoloLook.FrameCoreAlpha * alpha }, HoloLook.FrameCore, true);
    }

    private void Line(Vector2 a, Vector2 b, float alpha)
    {
        _lit.DrawLine(a, b, _light with { A = HoloLook.FrameGlowAlpha * alpha }, HoloLook.FrameGlow, true);
        _lit.DrawLine(a, b, Pale(HoloLook.FramePale) with { A = HoloLook.FrameCoreAlpha * alpha }, HoloLook.FrameCore, true);
    }

    /// <summary>A thin beam from <paramref name="from"/> to <paramref name="to"/>: a faint glow and a pale core.</summary>
    private void Beam(Vector2 from, Vector2 to, float alpha)
    {
        if (from.DistanceSquaredTo(to) < 1) return;
        _lit.DrawLine(from, to, _light with { A = HoloLook.BeamGlowAlpha * alpha }, HoloLook.BeamGlow, true);
        _lit.DrawLine(from, to, Pale(HoloLook.BeamPale) with { A = HoloLook.BeamCoreAlpha * alpha }, HoloLook.BeamCore, true);
    }

    private void Glow(TextureRect glow, Vector2 at, float radius, Color colour)
    {
        glow.Visible = true;
        glow.Size = new Vector2(2 * radius, 2 * radius);
        glow.Position = _toLocal * at - glow.Size / 2;
        glow.Modulate = colour;
    }

    /// <summary>The light <paramref name="share"/> of the way to white.</summary>
    private Color Pale(float share) => _light.Lerp(Colors.White, share);

    private static Vector2 V((float X, float Y) p) => new(p.X, p.Y);

    private static Rect2 Rect(HoloRect r) => new(r.X, r.Y, r.W, r.H);

    private static HoloRect Of(Rect2 r) => new(r.Position.X, r.Position.Y, r.Size.X, r.Size.Y);
}
