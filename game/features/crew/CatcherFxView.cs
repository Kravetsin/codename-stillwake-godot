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

    private readonly record struct Line(Vector2 From, Vector2 To, Color Colour, float Width, float FadeTo = 1);

    private readonly List<Line> _lines = [];
    private readonly List<(Vector2 Ground, double K)> _pillars = [];
    private readonly List<(Vector2 From, Vector2 To, double Swell, double Ms)> _beams = [];
    private readonly Vector2[] _corners = new Vector2[4];
    private readonly Color[] _shades = new Color[4], _endShades = new Color[2];
    private readonly Vector2[] _ends = new Vector2[2];
    private readonly Heat _heat = new();
    // The screen copied fresh for the heat to read: a figure's glitch (a canvas group) earlier in the frame leaves the copy empty.
    private readonly BackBufferCopy _copy = new() { CopyMode = BackBufferCopy.CopyModeEnum.Viewport, ShowBehindParent = true, Visible = false };

    public CatcherFxView()
    {
        Material = Added;
        TextureFilter = TextureFilterEnum.Linear;
        ZAsRelative = false;
        ZIndex = Layers.Effects;
        AddChild(_copy);
        AddChild(_heat);
    }

    /// <summary>Starts a frame: what was handed in last frame is let go.</summary>
    public void Begin()
    {
        _lines.Clear();
        _pillars.Clear();
        _beams.Clear();
        _heat.Begin();
    }

    /// <summary>Ends a frame: what was handed in is drawn.</summary>
    public void End()
    {
        QueueRedraw();
        _copy.Visible = _heat.End();
    }

    /// <summary>
    /// An aim line from <paramref name="muzzle"/> to <paramref name="end"/> as <paramref name="thread"/> says: its halo, the line, the light
    /// at the muzzle, and the dot on its end where it <paramref name="marks"/> its quarry; then on past it to <paramref name="stop"/>, fading
    /// out over <see cref="CatcherMotion.FadeOver"/> pixels (catcher-motion «Поиск»), lit where solid stops it.
    /// </summary>
    public void Aim(Vector2 muzzle, Vector2 end, Vector2 stop, bool marks, bool solid, CatcherFx.Thread thread)
    {
        if (thread.Halo > 0) Add(muzzle, end, CatcherFx.Halo, thread.Halo, 2.6);
        Add(muzzle, end, CatcherFx.Line, thread.Alpha, thread.Width);
        if (marks) Glow(end, thread.DotRadius, CatcherFx.Halo, thread.DotAlpha, CatcherFx.DotCore);
        Glow(muzzle, thread.MuzzleRadius, CatcherFx.Line, 0.5);
        float past = end.DistanceTo(stop);
        if (past >= 1)
        {
            float run = Math.Min(past, (float)CatcherMotion.FadeOver), left = 1 - run / (float)CatcherMotion.FadeOver;
            var to = end + (stop - end) / past * run;
            if (thread.Halo > 0) Add(end, to, CatcherFx.Halo, thread.Halo, 2.6, left);
            Add(end, to, CatcherFx.Line, thread.Alpha, thread.Width, left);
        }
        if (solid) Glow(stop, 1.6 + 1.6 * thread.Halo / 0.2, CatcherFx.Halo, 0.25 + 0.5 * thread.Alpha, CatcherFx.DotCore);
    }

    /// <summary>
    /// A laser «Жгут» from <paramref name="from"/> to <paramref name="to"/> at <paramref name="swell"/>, <paramref name="ms"/> on the
    /// clock: the heat over it, its halo, body and core, the light at its muzzle and, where solid stopped it, where it melts.
    /// </summary>
    public void Beam(Vector2 from, Vector2 to, double swell, double ms, bool melts)
    {
        if (swell <= 0 || from.DistanceTo(to) < 1) return;
        _beams.Add((from, to, swell, ms));
        _heat.Band(from, to, swell);
        Glow(from, 5 + 6 * swell, CatcherFx.BeamMuzzle, 0.8 * swell, CatcherFx.MuzzleCore);
        if (melts) Glow(to, 4 + 6 * swell, CatcherFx.Melt, 0.9 * swell, CatcherFx.MeltCore);
    }

    /// <summary>The spot a laser melted at <paramref name="at"/>, <paramref name="k"/> of the way cooled (none while it burns).</summary>
    public void Cooling(Vector2 at, double k) => Glow(at, 3.4, CatcherFx.Cooling, 0.7 * (1 - k), CatcherFx.CoolingCore);

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
    /// The missile's lock «Захват» round <paramref name="at"/>, <paramref name="k"/> of the way through its tell, <paramref name="ms"/> on
    /// the clock (<see cref="CatcherFx.LockOf"/>): four amber corners, each a halo and a line, closing in and untwisting.
    /// </summary>
    public void Lock(Vector2 at, double k, double ms)
    {
        var (radius, turn, alpha) = CatcherFx.LockOf(k, ms);
        Corners(at, radius, turn, CatcherFx.LockArm, CatcherFx.LockHalo, 0.25 * alpha, 2.4);
        Corners(at, radius, turn, CatcherFx.LockArm, CatcherFx.LockLine, 0.95 * alpha, 0.7);
    }

    /// <summary>The lock snapped shut on <paramref name="at"/> <paramref name="ms"/> after the launch, fading (<see cref="CatcherFx.SnapOf"/>).</summary>
    public void Snap(Vector2 at, double ms) =>
        Corners(at, CatcherFx.SnapRadius, 0, CatcherFx.SnapArm, CatcherFx.Snap, CatcherFx.SnapOf(ms), 0.8);

    /// <summary>Four corners of a box 1.6 by 2.2 <paramref name="radius"/> round <paramref name="at"/>, turned by <paramref name="turn"/>, their arms pointing in.</summary>
    private void Corners(Vector2 at, double radius, double turn, double arm, (int R, int G, int B) rgb, double alpha, double width)
    {
        if (alpha <= 0.004) return;
        float a = (float)arm;
        for (int q = 0; q < 4; q++)
        {
            float sx = (q & 1) != 0 ? 1 : -1, sy = (q & 2) != 0 ? 1 : -1;
            var corner = new Vector2(sx * (float)radius * 0.8f, sy * (float)radius * 1.1f);
            var (side, end, tip) = (corner - new Vector2(sx * a, 0), corner, corner - new Vector2(0, sy * a));
            Add(at + side.Rotated((float)turn), at + end.Rotated((float)turn), rgb, alpha, width);
            Add(at + end.Rotated((float)turn), at + tip.Rotated((float)turn), rgb, alpha, width);
        }
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

    private void Add(Vector2 from, Vector2 to, (int R, int G, int B) rgb, double alpha, double width, float fadeTo = 1)
    {
        if (alpha > 0.004) _lines.Add(new Line(from, to, new Color(rgb.R / 255f, rgb.G / 255f, rgb.B / 255f, (float)Math.Min(1, alpha)), (float)width, fadeTo));
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
        foreach (var (from, to, swell, ms) in _beams) DrawBeam(from, to, swell, ms);
        foreach (var line in _lines)
        {
            if (line.FadeTo >= 1) DrawLine(line.From, line.To, line.Colour, line.Width, true);
            else
            {
                (_ends[0], _ends[1], _endShades[0], _endShades[1]) = (line.From, line.To, line.Colour, line.Colour with { A = line.Colour.A * line.FadeTo });
                DrawPolylineColors(_ends, _endShades, line.Width, true);
            }
        }
    }

    /// <summary>A laser's halo and body as strips of quads along it, their edges flowing (<see cref="CatcherFx.Edges"/>), then its core.</summary>
    private void DrawBeam(Vector2 from, Vector2 to, double swell, double ms)
    {
        var way = to - from;
        float length = way.Length();
        var along = way / length;
        var across = new Vector2(-along.Y, along.X);
        int n = Math.Max(4, (int)Math.Ceiling(length / 4));
        var body = new Vector2[2 * (n + 1)];
        var halo = new Vector2[2 * (n + 1)];
        for (int j = 0; j <= n; j++)
        {
            float s = length * j / n;
            var at = from + along * s;
            var (up, down) = CatcherFx.Edges(s, ms, swell);
            (body[j], body[n + 1 + j]) = (at + across * (float)up, at - across * (float)down);
            (halo[j], halo[n + 1 + j]) = (at + across * (float)(2.3 * up), at - across * (float)(2.3 * down));
        }
        var strip = new int[6 * n];
        for (int j = 0; j < n; j++)
            (strip[6 * j], strip[6 * j + 1], strip[6 * j + 2], strip[6 * j + 3], strip[6 * j + 4], strip[6 * j + 5]) = (j, j + 1, n + 2 + j, j, n + 2 + j, n + 1 + j);
        var canvas = GetCanvasItem();
        RenderingServer.CanvasItemAddTriangleArray(canvas, strip, halo, Uniform(halo.Length, CatcherFx.BeamHalo, 0.2 * swell));
        RenderingServer.CanvasItemAddTriangleArray(canvas, strip, body, Uniform(body.Length, CatcherFx.BeamBody, 0.5));
        var core = CatcherFx.BeamCore;
        DrawLine(from, to, new Color(core.R / 255f, core.G / 255f, core.B / 255f, (float)(0.72 * Math.Max(0.4, swell))), (float)Math.Max(0.25, 0.8 * 2.2 * swell), true);
    }

    private static Color[] Uniform(int count, (int R, int G, int B) rgb, double alpha)
    {
        var colours = new Color[count];
        Array.Fill(colours, new Color(rgb.R / 255f, rgb.G / 255f, rgb.B / 255f, (float)alpha));
        return colours;
    }

    /// <summary>
    /// The heat over a laser: the picture in a band <see cref="CatcherFx.ShimmerBand"/> pixels tall above it redrawn moved up or down a
    /// little by thin columns, as the page shimmers it; drawn behind the laser itself.
    /// </summary>
    private sealed partial class Heat : Node2D
    {
        private static readonly Shader Shimmer = new()
        {
            Code = """
                shader_type canvas_item;
                render_mode unshaded;
                uniform sampler2D screen : hint_screen_texture, repeat_disable, filter_linear;
                uniform float ms;
                uniform float amp;
                varying vec2 at;

                void vertex() {
                    at = VERTEX;
                }

                void fragment() {
                    float per = SCREEN_PIXEL_SIZE.y / max(abs(dFdy(at.y)), 1e-5);
                    float x = floor(at.x / 2.5) * 2.5;
                    float shift = COLOR.a * amp * sin(x / 5.5 + ms / 38.0) * (0.6 + 0.4 * sin(x / 23.0 - ms / 90.0));
                    COLOR = textureLod(screen, SCREEN_UV - vec2(0.0, shift * per), 0.0);
                }
                """,
        };

        private readonly ShaderMaterial _shimmer = new() { Shader = Shimmer };
        private readonly List<(Vector2 From, Vector2 To, double Swell)> _bands = [];
        private readonly Vector2[] _corners = new Vector2[4];
        private readonly Color[] _swells = new Color[4];

        public Heat()
        {
            Material = _shimmer;
            _shimmer.SetShaderParameter("amp", (float)CatcherFx.ShimmerAmp);
            ShowBehindParent = true;
        }

        public void Begin() => _bands.Clear();

        public void Band(Vector2 from, Vector2 to, double swell) => _bands.Add((from, to, swell));

        /// <summary>Draws this frame's bands; whether there are any.</summary>
        public bool End()
        {
            _shimmer.SetShaderParameter("ms", (float)(Time.GetTicksMsec() % 3_600_000));
            QueueRedraw();
            return _bands.Count > 0;
        }

        public override void _Draw()
        {
            var up = new Vector2(0, (float)CatcherFx.ShimmerBand);
            foreach (var (from, to, swell) in _bands)
            {
                (_corners[0], _corners[1], _corners[2], _corners[3]) = (from, to, to - up, from - up);
                Array.Fill(_swells, new Color(1, 1, 1, (float)swell));
                DrawPrimitive(_corners, _swells, null);
            }
        }
    }
}
