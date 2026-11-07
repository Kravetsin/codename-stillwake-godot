using System;
using Godot;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Shared;

/// <summary>
/// A machine window's hologram (machine-holograms): its ground in the station's tone under the window's contents, and a
/// beam from the machine in the world to where it touches the window, growing and shrinking by <see cref="HoloMotion"/>.
/// The first child of its window's parent, so it draws over the world and under every panel; not a control, so it takes
/// no clicks. Its owner steps it every frame with the window and the machine's box, and fades the window's contents by
/// what it returns. It keeps the last rectangles it was given, never the window: a window freed as it closes, or a
/// machine destroyed while open, still shrinks and retracts where it was. Redrawn every frame while it shows: the beam
/// follows the hero and the camera. Seen only by the hero who opened it; no world light.
/// </summary>
public partial class MachineHolo : Node2D
{
    /// <summary>What an owner shows: the machine open, its window on the screen, its box in world pixels, its tone.</summary>
    public readonly record struct Target(int Machine, Rect2 Window, Rect2 Box, HoloTone Tone);

    /// <summary>The beam's sections along its length, each its own shimmer.</summary>
    private const int Sections = 14;

    /// <summary>The beam's width at the machine, and the extra at the window when it touches, the reference's outer band's.</summary>
    private const float BeamFrom = 6, BeamGrows = 6;

    /// <summary>
    /// Across the beam, the reference's three bands (a bright core, a middle, a faint outer) as one soft profile: drawn as a
    /// texture, filtered, so its edges fade instead of stepping as a polygon's do.
    /// </summary>
    private static readonly ImageTexture Profile = BeamProfile();

    private static readonly Color Core = new(240 / 255f, 252 / 255f, 1);

    private readonly HoloMotion _motion = new();
    private readonly TextureRect _emitter = ScreenGlow.Of(Colors.White with { A = HoloLook.EmitterAlpha }, 0);
    private readonly StyleBoxFlat _ground = new();
    // A shadow would show through the translucent ground: the glow is a border outside it, fading out from its edge.
    private readonly StyleBoxFlat _glow = Glow();
    private readonly Vector2[] _quad = new Vector2[4];
    private readonly Color[] _colours = new Color[4];
    private readonly Vector2[] _uvs = new Vector2[4];
    private Rect2 _window, _box;
    /// <summary>This frame's, from <see cref="Place"/>: the window, the touch point, the emitter and the layer's pixels to the node's.</summary>
    private HoloRect _shownWindow;
    private HoloTouch _touch;
    private Vector2 _from;
    private Transform2D _toLocal = Transform2D.Identity;
    private Color _light = Colors.White;
    private double _now;

    public MachineHolo()
    {
        Visible = false;
        TextureFilter = TextureFilterEnum.Linear;
        _ground.SetBorderWidthAll(1);
        AddChild(_emitter);
    }

    /// <summary>
    /// One frame of <paramref name="seconds"/>, <paramref name="open"/> the window open or null once it is shut; the
    /// contents' alpha. A window not laid out yet (no size) holds the hologram where it is and draws nothing new.
    /// </summary>
    public float Step(double seconds, Target? open)
    {
        if (open is { } t)
        {
            bool laid = t.Window.Size.X > 0 && t.Window.Size.Y > 0;
            if (laid) (_window, _box) = (t.Window, t.Box);
            _motion.Step(t.Machine, laid ? seconds : 0);
            if (laid) Tone(t.Tone);
        }
        else _motion.Step(null, seconds);
        _now += seconds;
        Visible = _motion.Shown;
        if (Visible) Place();
        return (float)_motion.Contents;
    }

    /// <summary>Where the window and the machine are on the screen this frame, the emitter's glow on it, and a redraw.</summary>
    private void Place()
    {
        var canvas = GetViewport().GetCanvasTransform();
        var box = Of(canvas * _box);
        _shownWindow = Of(_window);
        _touch = HoloShape.Touch(_shownWindow, box);
        var (ex, ey) = HoloShape.Emitter(box, (_touch.X, _touch.Y), Render.ShipCamera.Cell * canvas.X.Length());
        _from = new Vector2(ex, ey);
        // The glow on the machine, breathing.
        _toLocal = GetGlobalTransform().AffineInverse();
        float k = (float)((0.5 + 0.5 * _motion.Beam) * (0.88 + 0.12 * Math.Sin(_now * 2.2)));
        _emitter.Size = new Vector2(2 * HoloLook.EmitterRadius, 2 * HoloLook.EmitterRadius);
        _emitter.Position = _toLocal * _from - _emitter.Size / 2;
        _emitter.Modulate = _light with { A = k };
        QueueRedraw();
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
        var tint = HoloLook.Of(tone);
        _light = new Color(tint.Light);
        _ground.BgColor = new Color(tint.Ground) with { A = HoloLook.GroundAlpha };
        _ground.BorderColor = _light with { A = HoloLook.Edge };
        _glow.BgColor = _light with { A = HoloLook.Glow };
        _glow.BorderColor = _light with { A = 0 };
    }

    public override void _Draw()
    {
        // In the canvas layer's pixels, as the window's rectangle and the mapped box are.
        DrawSetTransformMatrix(_toLocal);
        if (HoloShape.Grown(_shownWindow, _touch, _motion.Window) is var (grown, line)) Ground(_shownWindow, _touch, grown, line);
        Beam(_from, _from.Lerp(new Vector2(_touch.X, _touch.Y), (float)_motion.Beam));
        DrawRect(new Rect2(_from.X - 1, _from.Y, 2, 1), Core with { A = 0.85f * (float)(0.5 + 0.5 * _motion.Beam) });
    }

    /// <summary>The window's ground as grown: its colour, its edge and glow, the light's wash from the near edge, and that edge bright.</summary>
    private void Ground(HoloRect window, HoloTouch touch, HoloRect grown, bool line)
    {
        float x0 = MathF.Round(grown.X), y0 = MathF.Round(grown.Y);
        float x1 = Math.Max(x0 + 1, MathF.Round(grown.Right)), y1 = Math.Max(y0 + 1, MathF.Round(grown.Bottom));
        DrawStyleBox(_glow, new Rect2(x0, y0, x1 - x0, y1 - y0));
        DrawStyleBox(_ground, new Rect2(x0, y0, x1 - x0, y1 - y0));
        var near = _light with { A = HoloLook.NearEdge };
        switch (touch.Edge)
        {
            case HoloEdge.Right: DrawRect(new Rect2(x1 - 1, y0, 1, y1 - y0), near); break;
            case HoloEdge.Left: DrawRect(new Rect2(x0, y0, 1, y1 - y0), near); break;
            case HoloEdge.Bottom: DrawRect(new Rect2(x0, y1 - 1, x1 - x0, 1), near); break;
            default: DrawRect(new Rect2(x0, y0, x1 - x0, 1), near); break;
        }
        if (line) return;
        // The wash, anchored to the whole window: whole at its near edge, none at its middle.
        var (mx, my) = window.Middle;
        Color Wash(float share) => _light with { A = HoloLook.Wash * Math.Clamp(share, 0, 1) };
        switch (touch.Edge)
        {
            case HoloEdge.Right or HoloEdge.Left:
                {
                    bool right = touch.Edge == HoloEdge.Right;
                    float a = right ? Math.Max(x0, mx) : x0, b = right ? x1 : Math.Min(x1, mx);
                    if (b <= a) return;
                    float half = window.W / 2;
                    Color ca = Wash(right ? (a - mx) / half : (mx - a) / half), cb = Wash(right ? (b - mx) / half : (mx - b) / half);
                    Quad(new(a, y0), new(b, y0), new(b, y1), new(a, y1), ca, cb, cb, ca);
                    break;
                }
            default:
                {
                    bool below = touch.Edge == HoloEdge.Bottom;
                    float a = below ? Math.Max(y0, my) : y0, b = below ? y1 : Math.Min(y1, my);
                    if (b <= a) return;
                    float half = window.H / 2;
                    Color ca = Wash(below ? (a - my) / half : (my - a) / half), cb = Wash(below ? (b - my) / half : (my - b) / half);
                    Quad(new(x0, a), new(x1, a), new(x1, b), new(x0, b), ca, ca, cb, cb);
                    break;
                }
        }
    }

    /// <summary>The beam from <paramref name="from"/> on the machine to <paramref name="to"/>: three soft bands in the light, shimmering along.</summary>
    private void Beam(Vector2 from, Vector2 to)
    {
        float length = from.DistanceTo(to);
        if (length < 1) return;
        var across = (to - from).Orthogonal() / length;
        float reach = (float)_motion.Beam;
        float wide = BeamFrom + BeamGrows * reach;
        for (int i = 0; i < Sections; i++)
        {
            float s0 = (float)i / Sections, s1 = (float)(i + 1) / Sections;
            Vector2 c0 = from.Lerp(to, s0), c1 = from.Lerp(to, s1);
            Vector2 h0 = across * Mathf.Lerp(BeamFrom, wide, s0) / 2, h1 = across * Mathf.Lerp(BeamFrom, wide, s1) / 2;
            Color a0 = _light with { A = Shimmer(s0, length) }, a1 = _light with { A = Shimmer(s1, length) };
            (_quad[0], _quad[1], _quad[2], _quad[3]) = (c0 + h0, c1 + h1, c1 - h1, c0 - h0);
            (_colours[0], _colours[1], _colours[2], _colours[3]) = (a0, a1, a1, a0);
            (_uvs[0], _uvs[1], _uvs[2], _uvs[3]) = (new(0, 0), new(1, 0), new(1, 1), new(0, 1));
            DrawPolygon(_quad, _colours, _uvs, Profile);
        }
    }

    /// <summary>The beam's light at <paramref name="s"/> of its length, rippling along it and slowly over time, as the reference's.</summary>
    private float Shimmer(float s, float length) =>
        (float)(0.72 + 0.28 * Math.Sin(s * length * 0.16 - _now * 3.4) * Math.Sin(_now * 1.1 + s * 4));

    private void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color ca, Color cb, Color cc, Color cd)
    {
        (_quad[0], _quad[1], _quad[2], _quad[3]) = (a, b, c, d);
        (_colours[0], _colours[1], _colours[2], _colours[3]) = (ca, cb, cc, cd);
        DrawPolygon(_quad, _colours);
    }

    private static ImageTexture BeamProfile()
    {
        // About the beam's width on the screen, so filtering does not thin the core away.
        const int across = 12;
        var image = Image.CreateEmpty(1, across, false, Image.Format.Rgba8);
        for (int y = 0; y < across; y++)
        {
            // From the middle, 0, to the edge, 1: the core a sixth wide, the middle half, the outer all of it.
            float d = Math.Abs((y + 0.5f) / across - 0.5f) * 2;
            float a = 0.6f * Bell(d, 1 / 6f) + 0.3f * Bell(d, 0.45f) + 0.1f * Bell(d, 1);
            image.SetPixel(0, y, Colors.White with { A = Math.Min(1, a) });
        }
        return ImageTexture.CreateFromImage(image);

        static float Bell(float d, float half) => d >= half ? 0 : 0.5f + 0.5f * MathF.Cos(MathF.PI * d / half);
    }

    private static StyleBoxFlat Glow()
    {
        var glow = new StyleBoxFlat { DrawCenter = false, BorderBlend = true };
        glow.SetBorderWidthAll(HoloLook.GlowSize);
        glow.SetExpandMarginAll(HoloLook.GlowSize);
        return glow;
    }

    private static HoloRect Of(Rect2 r) => new(r.Position.X, r.Position.Y, r.Size.X, r.Size.Y);
}
