using System;
using System.Diagnostics;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Effects;

/// <summary>
/// One stage's particles (features/effects, effects-on-their-layer): the luminous ones of its <see cref="Glims"/>, the
/// glints, shock lines, trails and blooms of its <see cref="Flares"/> and the arcs of its <see cref="Bolts"/> (0075).
/// Light is added over the world, drawn at the screen's resolution with soft edges — streaks along their own path,
/// white at the head, glints, rings, trails, arcs — and matter is lit by the room: motes, wisps, chips and grains,
/// each catching a little light of its own. Drawn at its stage's layer; moved on every frame, after every feature, so
/// what they stamped this frame is drawn this frame.
/// </summary>
public partial class EffectsView : Node2D
{
    private static readonly CanvasItemMaterial Added = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
        LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
    };

    /// <summary>A streak is at least this wide, and a glint's spikes and a ring, in screen pixels: thinner, it would flicker away.</summary>
    private const float Thinnest = 1.1f;
    /// <summary>What a mote or a grain of matter gives as light of its own, of its brightness: else the dim room swallows it.</summary>
    private const double MatterLight = 0.4;

    // Drawn a pixel of the screen a unit, so strokes are as fine as the screen allows, over the world's pixels.
    // Dust along the floor is drawn apart, behind the walls and over the back walls, so a wall hides it (0077).
    private readonly Node2D _lowMatter = new() { TextureFilter = TextureFilterEnum.Linear, ZAsRelative = false, ZIndex = Layers.LowDust };
    private readonly Node2D _matter = new() { TextureFilter = TextureFilterEnum.Linear },
        _light = new() { Material = Added, TextureFilter = TextureFilterEnum.Linear };
    private float _zoom = 1;
    private readonly double[] _xs = new double[Glim.Kept + 2], _ys = new double[Glim.Kept + 2];
    private readonly Vector2[][] _points = new Vector2[Glim.Kept + 3][];
    private readonly Color[][] _colours = new Color[Glim.Kept + 3][];
    private readonly Strokes _strokes = new();
    private readonly Stopwatch _clock = new();
    private double _spentMs;
    private int _frames;

    private readonly Glims _glims;
    private readonly Flares _flares;
    private readonly Bolts _bolts;
    private readonly Stage _stage;
    private bool _drewNothing;

    /// <summary>
    /// Whether it draws in the world's pixels however its parent turns, flips or moves: a figure's held stage, a part of the
    /// figure for the order it draws in, not for where (effects-on-their-layer).
    /// </summary>
    public bool InWorld { get; init; }

    /// <summary>A stage's particles, its matter and its light, at the z it is given.</summary>
    public EffectsView(Stage stage)
    {
        (_stage, _glims, _flares, _bolts) = (stage, stage.Glims, stage.Flares, stage.Bolts);
        // After the features: a trail or a bloom they stamp this frame is drawn this frame.
        ProcessPriority = 100;
        Lit.Set(_matter);
        Lit.Set(_lowMatter);
        AddChild(_lowMatter);
        AddChild(_matter);
        _matter.Draw += () => DrawMatter(_matter, low: false);
        _lowMatter.Draw += () => DrawMatter(_lowMatter, low: true);
        AddChild(_light);
        AddChild(_strokes);
        _light.Draw += DrawLight;
        for (int n = 0; n < _points.Length; n++) (_points[n], _colours[n]) = (new Vector2[n], new Color[n]);
    }

    /// <summary>The CPU time moving them and recording their drawing took a frame, on average since last asked, in ms: for <c>--measure</c>.</summary>
    public double TakeCostMs()
    {
        double ms = _frames > 0 ? _spentMs / _frames : 0;
        (_spentMs, _frames) = (0, 0);
        return ms;
    }

    public override void _Process(double delta)
    {
        _clock.Restart();
        if (InWorld) GlobalTransform = Transform2D.Identity;
        double ms = delta * 1000;
        _glims.Step(ms);
        _flares.Step(ms);
        _flares.TakeFrame();
        _bolts.Step(ms);
        // An empty stage, drawn empty once, draws nothing more till something is thrown on it.
        bool empty = _stage.Empty;
        if (empty && _drewNothing) return;
        _drewNothing = empty;
        // A pixel of the world on the screen here: the camera's zoom.
        _zoom = Math.Max(0.01f, GetGlobalTransformWithCanvas().X.Length());
        _light.Scale = Vector2.One / _zoom;
        _matter.Scale = _lowMatter.Scale = _light.Scale;
        _lowMatter.QueueRedraw();
        _matter.QueueRedraw();
        _light.QueueRedraw();
        Stroke();
        _spentMs += _clock.Elapsed.TotalMilliseconds;
        _frames++;
    }

    private static Color ColourOf((int R, int G, int B) c, double alpha) =>
        new(c.R / 255f, c.G / 255f, c.B / 255f, (float)Math.Clamp(alpha, 0, 1));

    private static Color ColourOf((double R, double G, double B) c, double alpha) =>
        new((float)(c.R / 255), (float)(c.G / 255), (float)(c.B / 255), (float)Math.Clamp(alpha, 0, 1));

    private static (double R, double G, double B) Whiter((double R, double G, double B) c, double t) =>
        (c.R + (255 - c.R) * t, c.G + (255 - c.G) * t, c.B + (255 - c.B) * t);

    private Vector2 Screen(double x, double y) => new((float)x * _zoom, (float)y * _zoom);

    /// <summary>A soft disc <paramref name="size"/> pixels of the world across at a point of the world, squashed to <paramref name="squash"/> of its height.</summary>
    private void Blob(Node2D on, Texture2D texture, double x, double y, double size, Color colour, double squash = 1)
    {
        if (colour.A <= 0.004f) return;
        float s = (float)size * _zoom, h = s * (float)squash;
        var at = Screen(x, y);
        on.DrawTextureRect(texture, new Rect2(at.X - s / 2, at.Y - h / 2, s, h), false, colour);
    }

    /// <summary>Dust, smoke, chips and grains, lit by the room as everything solid is: onto <paramref name="on"/>, those along the floor (<c>Low</c>) or the rest; dust along the floor gives no light of its own.</summary>
    private void DrawMatter(Node2D on, bool low)
    {
        long began = Stopwatch.GetTimestamp();
        foreach (var g in _glims.Live)
        {
            if (!g.Look.Matter || g.Look.Low != low) continue;
            double b = LuminousMath.Brightness(g);
            var c = LuminousMath.RampAt(g.Look.Ramp, g.Age);
            switch (g.Look.Kind)
            {
                case Glimmer.Mote:
                    Blob(on, SoftDots.Dot, g.X, g.Y, g.Size * 2, ColourOf(c, b));
                    break;
                case Glimmer.Streak:
                    // A grain: its head lit as a mote is; its smear along its way is its light (Stroke).
                    Blob(on, SoftDots.Dot, g.X, g.Y, g.Look.Width * 3, ColourOf(c, b));
                    break;
                case Glimmer.Wisp:
                    Wisp(on, g, c);
                    break;
                case Glimmer.Chip:
                    Chip(on, g, c);
                    break;
            }
        }
        on.DrawSetTransform(Vector2.Zero);
        _spentMs += Stopwatch.GetElapsedTime(began).TotalMilliseconds;
    }

    private void Wisp(Node2D on, Glim g, (double R, double G, double B) c)
    {
        double q = g.Age;
        double swell = Math.Min(1, q / 0.18) * Math.Pow(1 - q, 1.1);
        float r = (float)(g.Size * (1 + g.Look.Grow * (1 - (1 - q) * (1 - q)))) * _zoom;
        double v = Math.Sqrt(g.Vx * g.Vx + g.Vy * g.Vy), stretch = 1 + Math.Min(1.2, v * 0.025);
        on.DrawSetTransform(Screen(g.X, g.Y), (float)(Math.Atan2(g.Vy, g.Vx) + g.Seed * 0.02), new Vector2((float)stretch, (float)(1 / Math.Sqrt(stretch))));
        on.DrawTextureRect(SoftDots.Puff, new Rect2(-r, -r, r * 2, r * 2), false, ColourOf(c, g.Look.Alpha * g.Bright * swell));
    }

    private static readonly Vector2[] ChipShape = [new(-1, -0.6f), new(1.2f, -0.3f), new(0, 0.9f)];
    private readonly Vector2[] _chip = new Vector2[3];

    private void Chip(Node2D on, Glim g, (double R, double G, double B) c)
    {
        double q = g.Age;
        on.DrawSetTransform(Screen(g.X, g.Y), (float)g.Turn, Vector2.One * _zoom * (float)(g.Size / g.Look.Size));
        for (int i = 0; i < 3; i++) _chip[i] = ChipShape[i];
        on.DrawColoredPolygon(_chip, ColourOf(c, 1 - q * q));
    }

    /// <summary>
    /// Everything that gives light, added over the world, in two batches: fine dots (glowing motes, dust catching light),
    /// soft glows (blooms, streak heads, glints' blooms, chips flashing, arcs' glow); the strokes over them are one
    /// multimesh (<see cref="Stroke"/>).
    /// </summary>
    private void DrawLight()
    {
        long began = Stopwatch.GetTimestamp();
        var live = _glims.Live;
        foreach (var g in live)
        {
            if (g.Look.Kind != Glimmer.Mote || g.Look.Low) continue;
            double b = LuminousMath.Brightness(g) * (g.Look.Matter ? MatterLight : 1);
            if (b > 0.006) Blob(_light, SoftDots.Dot, g.X, g.Y, g.Size * 2, ColourOf(LuminousMath.RampAt(g.Look.Ramp, g.Age), b));
        }
        foreach (var g in live)
        {
            if (g.Look.Low || g.Look.Matter && g.Look.Kind != Glimmer.Chip || g.Look.Halo <= 0 && g.Look.Kind != Glimmer.Chip) continue;
            if (g.Look.Kind == Glimmer.Chip)
            {
                // A face turned to the light flashes.
                double face = Math.Sin(g.Turn * 2);
                if (face > 0.9) Blob(_light, PixelArt.Glow, g.X, g.Y, 3, ColourOf((255, 240, 220), (face - 0.9) * 8 * (1 - g.Age)));
                continue;
            }
            double b = LuminousMath.Brightness(g);
            var c = LuminousMath.RampAt(g.Look.Ramp, g.Age);
            if (g.Look.Kind == Glimmer.Streak) Blob(_light, PixelArt.Glow, g.X, g.Y, g.Look.Halo * 1.2, ColourOf(c, b * 0.45 * Fx.Glow));
            else Blob(_light, PixelArt.Glow, g.X, g.Y, g.Look.Halo, ColourOf(c, b * 0.25 * Fx.Glow));
        }
        foreach (var gl in _flares.Glints)
            if (gl.Strength > 0.01) Blob(_light, PixelArt.Glow, gl.X, gl.Y, gl.Radius * 1.2, ColourOf(gl.Colour, gl.Strength * 0.6));
        foreach (var bloom in _flares.Blooms)
            Blob(_light, PixelArt.Glow, bloom.X, bloom.Y, bloom.Size, ColourOf(bloom.Colour, bloom.Alpha * Fx.Glow), bloom.Squash);
        _spentMs += Stopwatch.GetElapsedTime(began).TotalMilliseconds;
    }

    private readonly (double X, double Y)[] _bolt = new (double, double)[65];

    /// <summary>Every stroke of light this frame into the one multimesh: streaks, trails, arcs, shock rings, glints' spikes.</summary>
    private void Stroke()
    {
        _strokes.Scale = Vector2.One / _zoom;
        _strokes.Begin();
        foreach (var g in _glims.Live)
        {
            if (g.Look.Kind != Glimmer.Streak || g.Look.Low) continue;
            double b = LuminousMath.Brightness(g) * (g.Look.Matter ? MatterLight : 1);
            if (b > 0.006) Streak(g, LuminousMath.RampAt(g.Look.Ramp, g.Age), b);
        }
        var trails = _flares.Trails;
        for (int i = 0; i < trails.Count; i++) Trail(trails[i]);
        foreach (var bolt in _bolts.Live) Arc(bolt);
        foreach (var s in _flares.Shocks)
            if (s.Alpha > 0.01) Ring(s);
        foreach (var gl in _flares.Glints)
        {
            Glint(gl);
            gl.Drawn = true;
        }
        _strokes.End();
    }

    /// <summary>A stroke's solid core from a width in screen pixels drawn whole: the feather either side makes up the rest.</summary>
    private static float Core(float width) => Math.Max(0.5f, width - 0.6f);

    /// <summary>
    /// A line of light through <paramref name="count"/> points of <see cref="_xs"/>, <see cref="_ys"/>: from a whitened head at
    /// <paramref name="a"/>, through <paramref name="c"/> at seven tenths of it by three tenths along, to nothing.
    /// </summary>
    private void Line(int count, (double R, double G, double B) c, double hot, double a, double width)
    {
        if (count < 2) return;
        var points = _points[count];
        var colours = _colours[count];
        double along = 0, total = 0;
        for (int i = 1; i < count; i++) total += Math.Sqrt((_xs[i] - _xs[i - 1]) * (_xs[i] - _xs[i - 1]) + (_ys[i] - _ys[i - 1]) * (_ys[i] - _ys[i - 1]));
        var head = ColourOf(Whiter(c, hot), a);
        for (int i = 0; i < count; i++)
        {
            if (i > 0) along += Math.Sqrt((_xs[i] - _xs[i - 1]) * (_xs[i] - _xs[i - 1]) + (_ys[i] - _ys[i - 1]) * (_ys[i] - _ys[i - 1]));
            double s = total > 0 ? along / total : 0;
            points[i] = Screen(_xs[i], _ys[i]);
            colours[i] = s < 0.3 ? head.Lerp(ColourOf(c, a * 0.7), (float)(s / 0.3)) : ColourOf(c, a * 0.7 * (1 - (s - 0.3) / 0.7));
        }
        _strokes.Line(points, colours, Core(Math.Max(Thinnest, (float)width * _zoom)));
    }

    /// <summary>One stroke along a streak's path, as long as its speed says, white-hot at the head and gone at the tail.</summary>
    private void Streak(Glim g, (double R, double G, double B) c, double b)
    {
        int n = LuminousMath.StreakPoints(g, LuminousMath.LengthOf(g), _xs, _ys);
        Line(n, c, g.Look.Hot, Math.Min(1, b * 1.25), g.Look.Width * 1.5);
    }

    private void Trail(Trail t)
    {
        var points = _flares.TrailPoints;
        int n = Math.Min(t.Count, _xs.Length);
        for (int i = 0; i < n; i++) (_xs[i], _ys[i]) = points[t.From + i];
        Line(n, (t.Colour.R, t.Colour.G, t.Colour.B), t.Hot, Math.Min(1, t.Alpha * Fx.Glow), t.Width);
    }

    private readonly Vector2[] _arc = new Vector2[65];
    private readonly Color[] _arcColours = new Color[65];

    /// <summary>
    /// An arc (arcs-are-hairlines), in pixels of the screen whatever the zoom: its last shapes fading in its deeper colour, then a
    /// thin haze and a hair of white over it; its branches thinner and dimmer.
    /// </summary>
    private void Arc(Bolt bolt)
    {
        var (x0, y0, x1, y1, _) = bolt.Ends();
        double a = Math.Clamp(bolt.Intensity, 0, 1);
        for (int back = bolt.Ghosts; back >= 1; back--)
        {
            int n = bolt.GhostOf(back, x0, y0, x1, y1, _bolt);
            for (int i = 0; i < n; i++) _arc[i] = Screen(_bolt[i].X, _bolt[i].Y);
            Paint(n, ColourOf(bolt.Tint.Ghost, a * 0.35 * (1 - back / (Bolts.Ghosts + 1.0))), Core(Thinnest));
        }
        for (int line = 0; line < bolt.Lines; line++)
        {
            int n = bolt.PointsOf(line, x0, y0, x1, y1, _bolt);
            for (int i = 0; i < n; i++) _arc[i] = Screen(_bolt[i].X, _bolt[i].Y);
            float thin = line == 0 ? 1 : 0.7f;
            if (bolt.Fine)
            {
                // A hair of light, a streak's width, in a faint blue haze twice as wide.
                Paint(n, ColourOf(Bolts.FineHaze, a * 0.35), Core(Thinnest * 2.2f));
                Paint(n, ColourOf(Bolts.FineCore, a), Core(Thinnest));
                continue;
            }
            Paint(n, ColourOf(bolt.Tint.Rim, a * (line == 0 ? 0.3 : 0.18)), Core(Math.Max(Thinnest, (float)(Bolts.Haze * bolt.Width * thin))));
            Paint(n, ColourOf(bolt.Tint.Core, a * (line == 0 ? 1 : 0.7)), Core(Math.Max(Thinnest, (float)(Bolts.Core * bolt.Width * thin))));
        }
        void Paint(int n, Color colour, float width)
        {
            for (int i = 0; i < n; i++) _arcColours[i] = colour;
            _strokes.Line(_arc.AsSpan(0, n), _arcColours.AsSpan(0, n), width);
        }
    }

    private const int RingSegments = 64;
    private readonly Vector2[] _ring = new Vector2[RingSegments + 1];
    private readonly Color[] _ringColours = new Color[RingSegments + 1];

    /// <summary>A shock line: a faint ring, closed, squashed flat along a floor.</summary>
    private void Ring(Shock s)
    {
        var centre = Screen(s.X, s.Y);
        float r = (float)s.Radius * _zoom;
        var colour = ColourOf(s.Colour, s.Alpha);
        var squash = new Vector2(1, (float)s.Squash);
        for (int i = 0; i <= RingSegments; i++)
        {
            _ring[i] = centre + Vector2.FromAngle(Mathf.Tau * i / RingSegments) * squash * r;
            _ringColours[i] = colour;
        }
        _strokes.Line(_ring, _ringColours, Core(Math.Max(Thinnest, (float)s.Width * _zoom)));
    }

    /// <summary>Where along a spike its colour stops are, and how bright each is: clear ends, a hot middle.</summary>
    private static readonly double[] SpikeAt = [0, 0.4, 0.5, 0.6, 1], SpikeAlpha = [0, 0.45, 1, 0.45, 0];
    private readonly Vector2[] _spike = new Vector2[5];
    private readonly Color[] _spikeColours = new Color[5];

    /// <summary>A cross flare's spikes: a long one across, a shorter one up and down, faint diagonals; each bright in the middle and clear at its ends.</summary>
    private void Glint(Glint g)
    {
        double a = g.Strength;
        if (a <= 0.01 || g.Radius <= 0.2) return;
        var c = ((double)g.Colour.R, (double)g.Colour.G, (double)g.Colour.B);
        var hot = Whiter(c, 0.8);
        float w = Core(Math.Max(Thinnest, (float)Math.Max(0.2, g.Radius * 0.05) * _zoom));
        double r = g.Radius, d = r * 0.24;
        Spike(g, -r, 0, r, 0, c, hot, a, w);
        Spike(g, 0, -r * 0.6, 0, r * 0.6, c, hot, a * 0.85, w * 0.9f);
        Spike(g, -d, -d, d, d, c, hot, a * 0.4, w * 0.7f);
        Spike(g, -d, d, d, -d, c, hot, a * 0.4, w * 0.7f);
    }

    private void Spike(Glint g, double x0, double y0, double x1, double y1, (double, double, double) c, (double, double, double) hot, double a, float w)
    {
        double cos = Math.Cos(g.Turn), sin = Math.Sin(g.Turn);
        for (int i = 0; i < 5; i++)
        {
            double x = x0 + (x1 - x0) * SpikeAt[i], y = y0 + (y1 - y0) * SpikeAt[i];
            _spike[i] = Screen(g.X + x * cos - y * sin, g.Y + x * sin + y * cos);
            _spikeColours[i] = ColourOf(i == 2 ? hot : c, a * SpikeAlpha[i]);
        }
        _strokes.Line(_spike, _spikeColours, w);
    }
}
