using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The light of the catchers' fight (catcher-combat) as <see cref="CatcherFx"/> draws it, handed in every frame by <see cref="HunterView"/>
/// and drawn that frame, added over the world: aim lines and a bolt's ghost, the blink's specks, tear and falling line, the salvo's
/// columns; their soft glows stamped as the effects layer's blooms.
/// </summary>
public partial class CatcherFxView : Node2D
{
    private static readonly CanvasItemMaterial Added = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
        LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
    };

    private readonly record struct Line(Vector2 From, Vector2 To, Color Colour, float Width);

    private readonly List<Line> _lines = [];
    private readonly List<(Vector2 Ground, double K)> _pillars = [];
    private readonly Vector2[] _corners = new Vector2[4];
    private readonly Color[] _shades = new Color[4];

    public CatcherFxView()
    {
        Material = Added;
        TextureFilter = TextureFilterEnum.Linear;
        ZAsRelative = false;
        ZIndex = Layers.Effects;
    }

    /// <summary>Starts a frame: what was handed in last frame is let go.</summary>
    public void Begin()
    {
        _lines.Clear();
        _pillars.Clear();
    }

    /// <summary>Ends a frame: what was handed in is drawn.</summary>
    public void End() => QueueRedraw();

    /// <summary>An aim line from <paramref name="muzzle"/> to <paramref name="end"/> as <paramref name="thread"/> says: its halo, the line, the dot on its end, the light at the muzzle.</summary>
    public void Aim(Vector2 muzzle, Vector2 end, CatcherFx.Thread thread)
    {
        if (thread.Halo > 0) Add(muzzle, end, CatcherFx.Halo, thread.Halo, 2.6);
        Add(muzzle, end, CatcherFx.Line, thread.Alpha, thread.Width);
        Glow(end, thread.DotRadius, CatcherFx.Halo, thread.DotAlpha, CatcherFx.DotCore);
        Glow(muzzle, thread.MuzzleRadius, CatcherFx.Line, 0.5);
    }

    /// <summary>A bolt's ghost, <paramref name="k"/> of the way out: a faint crooked line where it struck.</summary>
    public void After(Vector2 from, Vector2 to, double k)
    {
        Span<(double X, double Y)> points = stackalloc (double, double)[72];
        int n = CatcherFx.Crooked(from.X, from.Y, to.X, to.Y, 7, 1.2, points);
        for (int i = 1; i < n; i++)
            Add(new Vector2((float)points[i - 1].X, (float)points[i - 1].Y), new Vector2((float)points[i].X, (float)points[i].Y), CatcherFx.Ghost, 0.45 * (1 - k), 0.8);
    }

    /// <summary><paramref name="count"/> red specks round a figure whose feet are at <paramref name="feet"/>, at <paramref name="alpha"/>.</summary>
    public void Specks(Vector2 feet, int frame, int count, double alpha)
    {
        for (int i = 0; i < count; i++)
        {
            var (x, y, length, pale, a) = CatcherFx.Speck(frame, i, alpha);
            var from = feet + new Vector2((float)x, (float)y);
            Add(from, from + new Vector2((float)length, 0), pale ? CatcherFx.Pale : CatcherFx.Red, a, 0.45);
        }
    }

    /// <summary>The tear left where it was, its feet at <paramref name="feet"/>, <paramref name="ms"/> after it blinked: a white-red line across closing.</summary>
    public void Tear(Vector2 feet, double ms)
    {
        if (ms >= CatcherFx.TearMs) return;
        double fade = 1 - ms / CatcherFx.TearMs;
        float w = (float)(28 * fade), y = feet.Y - 24;
        Add(new Vector2(feet.X - w / 2, y), new Vector2(feet.X + w / 2, y), (255, 230, 225), fade, 1.2);
        Add(new Vector2(feet.X - w, y), new Vector2(feet.X + w, y), CatcherFx.Red, 0.5 * fade, 3);
    }

    /// <summary>Where it came, its feet at <paramref name="feet"/>, <paramref name="ms"/> after: a red-white line falling to its feet.</summary>
    public void Arrival(Vector2 feet, double ms)
    {
        if (ms >= CatcherFx.ArriveMs) return;
        double fade = 1 - ms / CatcherFx.ArriveMs;
        Add(new Vector2(feet.X, feet.Y - (float)(52 * fade)), feet, (255, 220, 210), 0.8 * fade, 0.8);
    }

    /// <summary>A shell's mark on <paramref name="ground"/>, <paramref name="k"/> of the way through its flight: a column of light and its glow.</summary>
    public void Pillar(Vector2 ground, double k)
    {
        _pillars.Add((ground, k));
        var (_, _, _, glow) = CatcherFx.PillarOf(k);
        Glow(ground, 7, CatcherFx.PillarGlow, glow);
    }

    /// <summary>
    /// A soft light of <paramref name="radius"/> at <paramref name="at"/>, as the page's glow: <paramref name="rgb"/> fading out, a
    /// <paramref name="core"/> in its middle.
    /// </summary>
    public static void Glow(Vector2 at, double radius, (int R, int G, int B) rgb, double alpha, (int R, int G, int B)? core = null)
    {
        if (alpha <= 0 || radius <= 0) return;
        var flares = Stages.Of(Layers.Effects).Flares;
        flares.Bloom(at.X, at.Y, 2 * radius, rgb, 0.8 * alpha);
        flares.Bloom(at.X, at.Y, 0.8 * radius, core ?? rgb, alpha);
    }

    private void Add(Vector2 from, Vector2 to, (int R, int G, int B) rgb, double alpha, double width)
    {
        if (alpha > 0.004) _lines.Add(new Line(from, to, new Color(rgb.R / 255f, rgb.G / 255f, rgb.B / 255f, (float)Math.Min(1, alpha)), (float)width));
    }

    public override void _Draw()
    {
        var pillar = new Color(CatcherFx.Pillar.R / 255f, CatcherFx.Pillar.G / 255f, CatcherFx.Pillar.B / 255f);
        var thread = new Color(CatcherFx.PillarThread.R / 255f, CatcherFx.PillarThread.G / 255f, CatcherFx.PillarThread.B / 255f);
        float h = (float)CatcherFx.PillarHeight;
        foreach (var (ground, k) in _pillars)
        {
            var (width, alpha, lit, _) = CatcherFx.PillarOf(k);
            float w = (float)width, x = ground.X, y = ground.Y;
            (_corners[0], _corners[1], _corners[2], _corners[3]) = (new(x - w / 2, y), new(x - w * 0.3f, y - h), new(x + w * 0.3f, y - h), new(x + w / 2, y));
            (_shades[0], _shades[3]) = (pillar with { A = (float)alpha }, pillar with { A = (float)alpha });
            (_shades[1], _shades[2]) = (pillar with { A = 0 }, pillar with { A = 0 });
            DrawPolygon(_corners, _shades);
            if (lit > 0.004) DrawLine(ground, new Vector2(x, y - h * 0.8f), thread with { A = (float)lit }, 0.35f, true);
        }
        foreach (var line in _lines) DrawLine(line.From, line.To, line.Colour, line.Width, true);
    }
}
