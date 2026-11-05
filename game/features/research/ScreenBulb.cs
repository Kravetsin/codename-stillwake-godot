using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// The bulb at the top of the screen, seen by this player only (<see cref="BulbSignal"/>, hologram-keys): the picked bulb, large, hung cap up on a short cord from the
/// screen's top; it heats, bursts, and the find that lit it hatches out. The research key throws a beam up to it and the find slides down into the key
/// (<see cref="FindFlight"/>), whose number takes it on landing.
/// </summary>
public partial class ScreenBulb : CanvasLayer
{
    /// <summary>The bulb's picture on screen, its middle below the screen's top, the cord's foot and the cap plate's top; and the screen pixels to a picture pixel.</summary>
    private const float PictureW = 40, PictureH = 48, Middle = 42, CordFoot = 18, PlateTop = 15.5f, S = 4;
    /// <summary>Where the find hatches, and where the burst throws the glass from, below the screen's top.</summary>
    private const float Hatch = 44, Burst = 48, SmokeFrom = 20;
    private static readonly Color Ink = new("1b1e26"), Plate = new("2a2d35"), PlateLit = new("5c6270"), Glass = new("aeb8c8"), GlassBright = new("e4ebf5"),
        SmokeInk = new("8f96a3"), SparkHot = new("ffe6a8"), SparkWarm = new("f2b84b"), SparkCold = new("8a3c17"), Beam = new(124 / 255f, 240 / 255f, 150 / 255f);
    private static readonly (float X, float Y)[] Cord = [(0, -2), (0.4f, 4), (-0.2f, 9), (0.2f, 14), (0, CordFoot)];

    private readonly BulbSignal _signal;
    private readonly Func<bool> _inWorld;
    private readonly Func<Rect2?> _key;
    private readonly Func<int, Texture2D?> _find;
    private readonly Control _root = new() { MouseFilter = Control.MouseFilterEnum.Ignore, TextureFilter = CanvasItem.TextureFilterEnum.Linear };
    private Texture2D? _hatched;

    /// <param name="signal">When it plays, read by the research key.</param>
    /// <param name="inWorld">Whether a world is played.</param>
    /// <param name="key">Where the research key is on the screen; null while it does not show.</param>
    /// <param name="find">The picture of the find that lit it, so many pixels across.</param>
    public ScreenBulb(BulbSignal signal, Func<bool> inWorld, Func<Rect2?> key, Func<int, Texture2D?> find)
    {
        (_signal, _inWorld, _key, _find) = (signal, inWorld, key, find);
        Layer = ScreenLayers.TabletEdge;
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_root);
        var shader = GD.Load<Shader>("res://shared/holo.gdshader");
        _root.AddChild(new Part(DrawBulb));
        _root.AddChild(new Part(DrawBeam) { Material = new ShaderMaterial { Shader = shader } });
        _root.AddChild(new Part(DrawFind));
        var grey = new ShaderMaterial { Shader = shader };
        grey.SetShaderParameter("grey", true);
        _root.AddChild(new Part(DrawTinted) { Material = grey });
    }

    public override void _Process(double delta)
    {
        bool played = _signal.Time != null;
        _signal.Step(delta);
        _root.Visible = _inWorld();
        if (_signal.Time is not { } t)
        {
            _hatched = null;
            if (played) Redraw();
            return;
        }
        if (t >= _signal.BurstsAt) _hatched ??= _find((int)FindFlight.HatchSize);
        Redraw();
    }

    private void Redraw()
    {
        foreach (var child in _root.GetChildren()) ((CanvasItem)child).QueueRedraw();
    }

    private float Centre => _root.GetViewportRect().Size.X / 2;

    /// <summary>The cord, the cap's plate and the bulb, then what the burst throws.</summary>
    private void DrawBulb(Part c)
    {
        if (_signal.Time is not { } t) return;
        double age = t - _signal.BurstsAt;
        if (age >= BulbSignal.Gone) return;
        float x = Centre, alpha = age < 0 ? 1 : (float)BulbSignal.CapAlpha(age);
        Pen(c, x, Cord, 1.6f, 3, (float)Math.Clamp(t / BulbSignal.DrawnBy, 0, 1), 0.3f, Ink with { A = alpha });
        if (t >= BulbSignal.DrawnBy * 0.5)
        {
            c.DrawRect(new Rect2(x - 2.5f, PlateTop, 5, 3), Plate with { A = alpha });
            c.DrawRect(new Rect2(x - 2.5f, PlateTop, 5, 1), PlateLit with { A = alpha });
        }
        // Hung cap up: the picture turned over.
        c.DrawSetTransform(new Vector2(x, Middle), Mathf.Pi);
        var at = new Rect2(-PictureW / 2, -PictureH / 2, PictureW, PictureH);
        if (age < 0)
        {
            c.DrawTextureRect(PixelArt.Picture(BulbSignal.DrawingOf(t) is { } d ? $"bulb_drawing_{d}" : "bulb_glass"), at, false);
            if (BulbSignal.FireOf(_signal.Heat(t)) is { } fire) c.DrawTextureRect(PixelArt.Picture($"bulb_fire_{fire}"), at, false);
            c.DrawSetTransform(Vector2.Zero);
            return;
        }
        c.DrawTextureRect(PixelArt.Picture("bulb_stump"), at, false, new Color(1, 1, 1, alpha));
        c.DrawSetTransform(Vector2.Zero);
        Thrown(c, new Vector2(x, Burst), (float)age);
    }

    /// <summary>The flash, the glass and the sparks of the burst at <paramref name="at"/>, and the wisp of smoke from the cord's foot.</summary>
    private void Thrown(Part c, Vector2 at, float age)
    {
        var from = at + new Vector2(0, -1.5f * S);
        if (age < BulbSignal.Flash) c.DrawCircle(from, 3.75f * S, Colors.White);
        double floor = (_root.GetViewportRect().Size.Y - from.Y) / S;
        for (int i = 0; i < BulbSignal.Shards; i++)
        {
            if (BulbSignal.Shard(i, age, floor) is not { } shard) continue;
            var (sx, sy, turn, size, a, bright) = shard;
            c.DrawSetTransform(from + new Vector2((float)sx, (float)sy) * S, (float)turn, Vector2.One * S);
            float z = (float)size;
            c.DrawColoredPolygon([new Vector2(-z, -z * 0.4f), new Vector2(z, -z * 0.7f), new Vector2(z * 0.2f, z)], (bright ? GlassBright : Glass) with { A = (float)a });
        }
        c.DrawSetTransform(Vector2.Zero);
        for (int i = 0; i < BulbSignal.Sparks; i++)
        {
            if (BulbSignal.Spark(i, age) is not { } spark) continue;
            var (sx, sy, dx, dy, cooled) = spark;
            var p = at + new Vector2((float)sx, (float)sy) * S;
            c.DrawLine(p, p - new Vector2((float)dx, (float)dy) * 0.028f * S, cooled < 0.35 ? SparkHot : cooled < 0.7 ? SparkWarm : SparkCold, 0.7f * S * 0.6f, true);
        }
        if (BulbSignal.SmokeAlpha(age) is var smoke && smoke <= 0) return;
        var points = new Vector2[15];
        for (int i = 0; i < points.Length; i++)
        {
            var (px, py) = BulbSignal.Smoke(i / (double)(points.Length - 1), age);
            points[i] = new Vector2(at.X + (float)px * S, SmokeFrom + (float)py * S);
        }
        c.DrawPolyline(points, SmokeInk with { A = (float)smoke }, 0.55f * S, true);
    }

    /// <summary>Seconds since the burst while the find is on its way, and the key's middle and top; null otherwise.</summary>
    private (double T, (double X, double Y) From, (double X, double Y) To, (double X, double Y) Top)? Flying()
    {
        if (_signal.Time is not { } t || t < _signal.BurstsAt || _key() is not { } key) return null;
        double ft = t - _signal.BurstsAt;
        if (ft >= BulbSignal.FlightS) return null;
        var mid = key.GetCenter();
        return (ft, (Centre, Hatch), (mid.X, mid.Y), (mid.X, key.Position.Y));
    }

    /// <summary>The beam from the key's top to its far end, faint, its edges brighter, flickering.</summary>
    private void DrawBeam(Part c)
    {
        if (Flying() is not { } f || FindFlight.Head(f.T, f.From, f.To, f.Top) is not { } head) return;
        float flick = (float)FindFlight.Flicker(f.T), w0 = 3.5f, w1 = 1.8f;
        Vector2 top = new((float)f.Top.X, (float)f.Top.Y), end = new((float)head.X, (float)head.Y);
        c.DrawColoredPolygon([top - new Vector2(w0, 0), end - new Vector2(w1, 0), end + new Vector2(w1, 0), top + new Vector2(w0, 0)], Beam with { A = 0.13f * flick });
        c.DrawLine(top - new Vector2(w0, 0), end - new Vector2(w1, 0), Beam with { A = 0.55f * flick }, 0.8f, true);
        c.DrawLine(top + new Vector2(w0, 0), end + new Vector2(w1, 0), Beam with { A = 0.55f * flick }, 0.8f, true);
    }

    /// <summary>The find as it is, fading as it goes into the hologram's colour; faint the moment it hatches.</summary>
    private void DrawFind(Part c)
    {
        if (Flying() is not { } f || _hatched == null) return;
        float a = (1 - (float)FindFlight.Faded(f.T)) * (f.T < 0.03 ? 0.45f : 1);
        if (a > 0) c.DrawTextureRect(_hatched, Square(f, 1), false, new Color(1, 1, 1, a));
    }

    /// <summary>The find in the hologram's colour, a little larger, coming up as the other fades.</summary>
    private void DrawTinted(Part c)
    {
        if (Flying() is not { } f || _hatched == null || FindFlight.Faded(f.T) is var mix && mix <= 0) return;
        c.DrawTextureRect(_hatched, Square(f, 1.12f), false, Beam with { A = (float)mix });
    }

    private static Rect2 Square((double T, (double X, double Y) From, (double X, double Y) To, (double X, double Y) Top) f, float by)
    {
        var (x, y) = FindFlight.Find(f.T, f.From, f.To);
        float size = (float)FindFlight.Size(f.T) * by;
        return new Rect2((float)x - size / 2, (float)y - size / 2, size, size);
    }

    /// <summary>A thin pen line along <paramref name="points"/> from <paramref name="x"/>: it wanders, swells in the middle, tapers at its ends, drawn on to <paramref name="part"/> of its length.</summary>
    private static void Pen(Part c, float x, (float X, float Y)[] points, float width, int seed, float part, float wander, Color ink)
    {
        if (part <= 0) return;
        var q = new Vector2[points.Length];
        for (int i = 0; i < q.Length; i++)
            q[i] = new Vector2(x + points[i].X + ((float)BulbSignal.Noise(seed + i * 1.7) - 0.5f) * wander, points[i].Y + ((float)BulbSignal.Noise(seed + i * 2.9 + 5) - 0.5f) * wander);
        var along = new float[q.Length];
        for (int k = 1; k < q.Length; k++) along[k] = along[k - 1] + q[k].DistanceTo(q[k - 1]);
        float length = along[^1], upto = length * part;
        for (int k = 1; k < q.Length && along[k - 1] < upto; k++)
        {
            float u = along[k] / length, swell = Mathf.Pow(Mathf.Sin(Mathf.Pi * Mathf.Clamp(u * 0.96f + 0.02f, 0, 1)), 0.6f);
            float w = width * (0.35f + 0.75f * swell) * (0.88f + 0.24f * (float)BulbSignal.Noise(seed + k * 3.3));
            var end = along[k] > upto ? q[k - 1].Lerp(q[k], (upto - along[k - 1]) / (along[k] - along[k - 1])) : q[k];
            c.DrawLine(q[k - 1], end, ink, w, true);
        }
    }

    /// <summary>A layer of it, drawn through its own material.</summary>
    private sealed partial class Part(Action<Part> paint) : Node2D
    {
        public override void _Draw() => paint(this);
    }
}
