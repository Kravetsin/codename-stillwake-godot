using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Hazards.Logic;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Hazards;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Hazards;

/// <summary>
/// A storm's lightning seen (surface-storms, the picks of 2026-11-16): through its glow, soft violet fires on the grass tips and
/// cell corners of every open-sky ground cell of its zone and on the helmets of heroes standing in it («Огни святого Эльма»); in
/// its last moments a faint leader stepping down from above the view, then the return stroke and two more on the same channel
/// («Ступенчатый лидер»), lighting the scene hard and sinking the rest in blue shadow («Свет от канала»); arcs running out over
/// the zone, thunder, sparks and dark grains thrown from the crater («Искры»), soot streaked downwind; and over the view of this
/// player's hero, if it stood in the zone, the channel burnt in for a second («Ожог на сетчатке»). It reads <c>Strike</c> once a tick.
/// </summary>
public partial class BoltView : Node2D
{
    private const int Cell = ShipCamera.Cell;
    private static readonly (int R, int G, int B) Core = (250, 252, 255), Halo = (150, 172, 245), Light = (196, 210, 255);
    private static readonly Luminous Spark = Luminous.BreakSpark with
    {
        Ramp = [(255, 214, 120), (255, 140, 50), (170, 50, 20), (70, 24, 12)],
        LifeMs = (600, 1100),
        Gravity = 240,
        Drag = 0.6,
        Bounce = 0.2,
    };
    private static readonly Luminous Grain = Luminous.Chip with { Ramp = [(62, 48, 36), (96, 78, 58)], Gravity = 320, Size = 0.8, Bounce = 0.15, LifeMs = (1200, 1800) };

    private const string ShadeCode = """
        shader_type canvas_item;
        render_mode blend_mul, unshaded;
        uniform vec2 centre;
        uniform vec2 size;
        uniform float radius = 1.0;
        uniform float k = 0.0;
        void fragment() {
            // White near the channel, the rest sunk toward a deep blue by k: the multiply leaves what the light lit.
            float d = length((UV - centre) * size) / radius;
            vec3 mid = mix(vec3(1.0), vec3(0.47, 0.49, 0.63), k), far = mix(vec3(1.0), vec3(0.13, 0.16, 0.29), k);
            vec3 c = d < 0.12 ? vec3(1.0) : d < 0.55 ? mix(vec3(1.0), mid, (d - 0.12) / 0.43) : mix(mid, far, clamp((d - 0.55) / 0.45, 0.0, 1.0));
            COLOR = vec4(c, 1.0);
        }
        """;
    private static readonly StringName Centre = "centre", Size = "size", Radius = "radius", K = "k";
    private static readonly GradientTexture2D Fire = new()
    {
        Fill = GradientTexture2D.FillEnum.Radial,
        FillFrom = new Vector2(0.5f, 0.5f),
        FillTo = new Vector2(1, 0.5f),
        Width = 32,
        Height = 32,
        Gradient = new Gradient
        {
            Offsets = [0, 0.35f, 1],
            Colors = [Color.Color8(226, 206, 255, 230), Color.Color8(170, 130, 255, 170), Color.Color8(110, 70, 230, 0)],
        },
    };

    private readonly IGridQueries _grid;
    private readonly HazardsModule _hazards;
    private readonly Sim.Crew.ICrewQueries _crew;
    private readonly Func<int> _peer;
    private readonly Func<IReadOnlyList<(Vector2 Feet, float Vx)>> _heroes;
    private readonly Func<double> _progress;
    private readonly Func<int> _tick;
    private readonly CanvasLayer _shadeLayer = new() { Layer = ScreenLayers.BoltShade, Visible = false };
    private readonly ColorRect _shade = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly ShaderMaterial _shadeMaterial = new() { Shader = new Shader { Code = ShadeCode } };
    private readonly CanvasLayer _burnLayer = new() { Layer = ScreenLayers.BoltBurn, Visible = false };
    private readonly Painted _burnDark, _burnLight;
    /// <summary>The channel and its forks as they were on the screen at the strike, for the burn; the camera's zoom then.</summary>
    private Vector2[] _burnt = [];
    private readonly List<Vector2[]> _burntForks = [];
    private float _burnZoom = 2;
    private double _burnSince = -1, _clock;
    private readonly Random _random = new();
    private readonly Voices _thunder = new(S.World, voices: 1);
    private readonly Node2D _marks;
    private int _read = -1;
    private Strike? _glowing;
    /// <summary>The ticks the glow had left when first seen: its whole length, unless joined late.</summary>
    private int _glowTicks;
    /// <summary>The zone's ground cells the glow crackles on, found once as a glow is first seen: each its cell and its share off the middle.</summary>
    private readonly List<(int Cell, float Off)> _zone = [];
    private Vector2[] _channel = [];
    private readonly List<Vector2[]> _branches = [];
    /// <summary>What _Draw shows this frame: the glow's growth (below 0 for none), the leader's points, the strokes' points and brightness.</summary>
    private double _grown = -1, _bright;
    private int _leader, _stroke;
    private bool _branched;
    private double _since = -1;
    private int _struck = -1;
    private readonly List<int> _soot = [];

    public BoltView(IGridQueries grid, HazardsModule hazards, Sim.Crew.ICrewQueries crew, Func<int> peer,
        Func<IReadOnlyList<(Vector2 Feet, float Vx)>> heroes, Func<double> progress, Func<int> tick)
    {
        (_grid, _hazards, _crew, _peer, _heroes, _progress, _tick) = (grid, hazards, crew, peer, heroes, progress, tick);
        ZIndex = Layers.Effects;
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded, BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        AddChild(_thunder);
        // Soot lies on the ground, lit as the ground is, under the plants and the heroes.
        _marks = new SootMarks(_grid, _soot) { ZIndex = Layers.Edges, ZAsRelative = false };
        AddChild(_marks);
        _shade.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _shade.Material = _shadeMaterial;
        _shadeLayer.AddChild(_shade);
        AddChild(_shadeLayer);
        _burnDark = new Painted(DrawBurn) { Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Mul } };
        _burnLight = new Painted(DrawEdge) { Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add } };
        _burnLayer.AddChild(_burnDark);
        _burnLayer.AddChild(_burnLight);
        AddChild(_burnLayer);
    }

    public override void _Process(double delta)
    {
        _clock += delta;
        if (_tick() != _read)
        {
            _read = _tick();
            var now = _hazards.Strike;
            if (_glowing is { } was && now == null && was.Left <= 2) Struck(was);
            if (now != null && (_glowing == null || _glowing.Cell != now.Cell)) Begin(now);
            _glowing = now;
        }
        (_grown, _leader, _bright, _stroke, _branched) = (-1, 0, 0, 0, false);
        if (_glowing is { } glow) Glow(glow);
        _shadeLayer.Visible = false;
        if (_since >= 0)
        {
            _since += delta;
            Flash(_since);
            if (_since > Math.Max(BoltLook.Done, BoltLook.LightTail)) _since = -1;
        }
        _burnLayer.Visible = _burnSince >= 0;
        if (_burnSince >= 0)
        {
            _burnSince += delta;
            _burnDark.QueueRedraw();
            _burnLight.QueueRedraw();
            if (_burnSince > BoltLook.BurnDone) _burnSince = -1;
        }
        QueueRedraw();
    }

    /// <summary>A glow first seen: its zone's ground cells found and its channel drawn from above the view down to the struck cell.</summary>
    private void Begin(Strike strike)
    {
        _glowTicks = strike.Left;
        _zone.Clear();
        int width = _grid.Width, column = strike.Cell % width;
        for (int x = Math.Max(0, column - strike.Spread); x <= Math.Min(width - 1, column + strike.Spread); x++)
            if (Ground(x) is int cell) _zone.Add((cell, Math.Abs(x - column) / (float)Math.Max(1, strike.Spread)));
        var (tx, ty) = Top(strike.Cell);
        var view = GetCanvasTransform().AffineInverse() * GetViewportRect();
        // From above the farthest zoom's view, so zooming out mid-glow never shows where it starts.
        double from = Math.Min(view.Position.Y - 2 * Cell, ty - (ShipCamera.Farthest.Y + 40) * Cell);
        var channel = BoltLook.Channel(_random, (tx, from), (tx, ty));
        _channel = Vectors(channel);
        _branches.Clear();
        foreach (var branch in BoltLook.Branches(_random, channel, 40 * Cell)) _branches.Add(Vectors(branch));
    }

    private static Vector2[] Vectors(List<(double X, double Y)> points)
    {
        var vectors = new Vector2[points.Count];
        for (int i = 0; i < points.Count; i++) vectors[i] = new((float)points[i].X, (float)points[i].Y);
        return vectors;
    }

    /// <summary>The first cell from the top of <paramref name="x"/>'s column that is a wall with only open sky above it; null under a back wall.</summary>
    private int? Ground(int x)
    {
        for (int y = 0; y < _grid.Height; y++)
        {
            int cell = y * _grid.Width + x;
            if (_grid.WallAt(cell) != null) return cell;
            if (_grid.BackAt(cell) != null) return null;
        }
        return null;
    }

    /// <summary>The middle of <paramref name="cell"/>'s top edge, in pixels of the world.</summary>
    private (float X, float Y) Top(int cell) => ((cell % _grid.Width + 0.5f) * Cell, cell / _grid.Width * Cell);

    /// <summary>How far the glow has grown this frame, for the fires; its faint light at the struck cell; the leader in its last moments.</summary>
    private void Glow(Strike strike)
    {
        int glow = Math.Max(strike.Left, _glowTicks);
        double left = Math.Max(0, strike.Left - _progress()), grown = _grown = 1 - left / Math.Max(1, glow);
        var (x, y) = Top(strike.Cell);
        Stages.Of(Layers.Effects).Flares.Hold(this, x, y, BoltTint.Violet.Glow, 0.25 + 0.45 * grown * Fx.Glow, 4, 0.5f, 0.8);
        double seconds = left / Sim.World.TicksPerSecond;
        if (seconds < BoltLook.Leader) _leader = BoltLook.LeaderReach(_channel.Length, seconds);
    }

    /// <summary>An arc crackling on <paramref name="cell"/>'s top edge or down one of its open sides, bowed up.</summary>
    private void Arc(Bolts bolts, int cell, double intensity, double width)
    {
        float x0 = cell % _grid.Width * Cell, y0 = cell / _grid.Width * Cell;
        double a = _random.NextDouble() * Cell, b = _random.NextDouble() * Cell;
        int side = _random.Next(4);
        (double, double, double, double, double) ends = side switch
        {
            0 when _grid.WallAt(cell - 1) == null => (x0, y0 + a * 0.6, x0 + b * 0.5, y0, -0.35),
            1 when _grid.WallAt(cell + 1) == null => (x0 + Cell, y0 + a * 0.6, x0 + Cell - b * 0.5, y0, 0.35),
            _ => (x0 + a - Cell / 2, y0, x0 + a + Cell * (0.5 + 1.5 * _random.NextDouble()), y0, -0.3),
        };
        bolts.Add(() => ends, _random.Next(3, 7), intensity, width, branchy: _random.NextDouble() < 0.2, tint: BoltTint.Violet);
    }

    /// <summary>The strike at <paramref name="was"/>'s cell: thunder, sparks and grains from the crater, the bolt's strokes begun, the soot laid.</summary>
    private void Struck(Strike was)
    {
        _struck = was.Cell;
        _since = 0;
        var (x, y) = Top(was.Cell);
        _thunder.Play(S.Thunder, new Vector2(x, y));
        var glims = Stages.Of(Layers.Effects).Glims;
        float floor = y + Cell;
        glims.Spray(Spark, x, floor - 1, 22, -Math.PI / 2, (-1.1, 1.1), (50, 130), floor);
        glims.Spray(Grain, x, floor - 1, 14, -Math.PI / 2, (-0.9, 0.9), (40, 100), floor);
        var bolts = Stages.Of(Layers.Effects).Bolts;
        foreach (var (cell, off) in _zone)
            if (_random.NextDouble() < 1 - 0.6 * off) Arc(bolts, cell, 1, 0.9);
        if (!_soot.Contains(was.Cell)) _soot.Add(was.Cell);
        _marks.QueueRedraw();
        // This player's hero in the zone, under the open sky, as the sim shocked it: the channel burns into its view.
        if (_crew.HeroOf(_peer()) is { } hero && _crew.OpenSky(hero.Cell)
            && Math.Abs(hero.Cell % _grid.Width - was.Cell % _grid.Width) <= was.Spread)
        {
            var screen = GetCanvasTransform();
            _burnZoom = screen.X.Length();
            _burnt = Array.ConvertAll(_channel, p => screen * p);
            _burntForks.Clear();
            foreach (var fork in _branches) _burntForks.Add(Array.ConvertAll(fork, p => screen * p));
            _burnSince = 0;
        }
    }

    /// <summary>The bolt's strokes and their light, <paramref name="since"/> seconds after the strike; discharges over the zone early on.</summary>
    private void Flash(double since)
    {
        if (_channel.Length == 0 || _struck < 0) return;
        double bright = _bright = BoltLook.Brightness(since), light = BoltLook.ChannelLight(since);
        var (x, y) = Top(_struck);
        if (bright > 0)
        {
            _stroke = BoltLook.ReturnReach(_channel.Length, since);
            _branched = BoltLook.Branched(since);
        }
        if (light > 0)
        {
            Stages.Of(Layers.Effects).Flares.Hold(_channel, x, y - 8 * Cell, Light, light * 4 * Fx.Glow, 40, 0.4f, 0.75);
            var screen = GetCanvasTransform();
            var size = GetViewportRect().Size;
            _shadeMaterial.SetShaderParameter(Centre, screen * new Vector2(x, y - 5 * Cell) / size);
            _shadeMaterial.SetShaderParameter(Size, size);
            _shadeMaterial.SetShaderParameter(Radius, 250 * screen.X.Length());
            _shadeMaterial.SetShaderParameter(K, (float)Math.Clamp(light * 0.85, 0, 1));
            _shadeLayer.Visible = true;
        }
        if (since < BoltLook.Discharges)
        {
            var bolts = Stages.Of(Layers.Effects).Bolts;
            foreach (var (cell, off) in _zone)
                if (_random.NextDouble() < 0.12 * (1 - off * 0.5)) Arc(bolts, cell, 1 - since / BoltLook.Discharges * 0.5, 0.8);
        }
    }

    /// <summary>
    /// The glow's fires over the zone and on the helmets in it; the leader's faint steps; the strokes' channel, even along its
    /// length (a <c>Trail</c> fades to its tail), and its forks.
    /// </summary>
    public override void _Draw()
    {
        if (_grown >= 0 && _glowing is { } glowing)
        {
            foreach (var (cell, off) in _zone)
            {
                int column = cell % _grid.Width;
                float x0 = column * Cell, y0 = cell / _grid.Width * Cell;
                double strength = BoltLook.FireStrength(_grown, off);
                FireAt(new Vector2(x0, y0), strength, column * 7);
                FireAt(new Vector2(x0 + 2 + 4 * (float)BoltLook.Hash(column, 7), y0 - 2 - 2.5f * (float)BoltLook.Hash(column, 8)), strength, column * 7 + 1);
                if (BoltLook.Hash(column, 9) < 0.5)
                    FireAt(new Vector2(x0 + 5 + 2 * (float)BoltLook.Hash(column, 10), y0 - 1.5f - 2 * (float)BoltLook.Hash(column, 11)), strength, column * 7 + 2);
            }
            double helmet = BoltLook.HelmetFire(_grown, _clock);
            if (helmet > 0)
                foreach (var (feet, _) in _heroes())
                {
                    int cell = (int)(feet.Y - 1) / Cell * _grid.Width + (int)feet.X / Cell;
                    if (Math.Abs((int)feet.X / Cell - glowing.Cell % _grid.Width) <= glowing.Spread && _crew.OpenSky(cell))
                        Burning(new Vector2(feet.X, feet.Y - 5 * Cell - 0.5f), helmet, 77);
                }
        }
        if (_leader > 1) Channel(_channel, 0, _leader, 0.45);
        if (_bright > 0 && _stroke > 1)
        {
            Channel(_channel, _channel.Length - _stroke, _stroke, _bright);
            if (_branched)
                foreach (var branch in _branches) Channel(branch, 0, branch.Length, _bright * 0.6, 0.6f);
        }
    }

    /// <summary>A zone's fire at <paramref name="at"/>, if lit yet, pulsing at its own pace.</summary>
    private void FireAt(Vector2 at, double strength, int seed)
    {
        if (!BoltLook.FireLit(seed, _grown)) return;
        double pulse = 0.55 + 0.45 * Math.Sin(_clock * (3 + 2 * BoltLook.Hash(seed, 13)) + seed);
        Burning(at, Math.Clamp(strength * pulse, 0, 1), seed);
    }

    /// <summary>One fire: a soft violet blot, three short tufts flicking up from it, a white point.</summary>
    private void Burning(Vector2 at, double strength, int seed)
    {
        float a = (float)strength;
        if (a <= 0.01f) return;
        float r = 3f + 1.6f * (float)BoltLook.Hash(seed, 4);
        DrawTextureRect(Fire, new Rect2(at - new Vector2(r, r), new Vector2(2 * r, 2 * r)), false, new Color(1, 1, 1, a));
        var tuft = Color.Color8(226, 214, 255, (byte)(204 * a));
        int slot = (int)(_clock * 15);
        for (int k = 0; k < 3; k++)
        {
            double angle = -Math.PI / 2 + (k - 1) * 0.45 + 0.25 * Math.Sin(_clock * 17 + seed + k);
            float length = (float)((1.4 + 1.6 * BoltLook.Hash(seed + k, slot)) * a);
            DrawLine(at, at + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * length, tuft, 0.3f, true);
        }
        DrawRect(new Rect2(at - new Vector2(0.3f, 0.3f), new Vector2(0.6f, 0.6f)), new Color(1, 1, 1, a));
    }

    /// <summary>The channel's afterimage over the view, multiplied in: a dark violet trace drifting and paling, its forks fainter.</summary>
    private void DrawBurn(Node2D on)
    {
        double a = BoltLook.Burn(_burnSince);
        if (a <= 0 || _burnt.Length < 2) return;
        float z = _burnZoom / 2, s = (float)_burnSince;
        var drift = new Vector2(4 * Mathf.Sin(s * 1.3f) + 2 * Mathf.Min(s, 2), 1.5f * Mathf.Min(s, 2)) * z;
        var main = Array.ConvertAll(_burnt, p => p + drift);
        Band(on, main, 16 * z, (170, 150, 215), 0.5 * a);
        Band(on, main, 8 * z, (120, 90, 180), 0.65 * a);
        Band(on, main, 3.5f * z, (70, 40, 120), 0.62 * a);
        foreach (var fork in _burntForks)
        {
            var moved = Array.ConvertAll(fork, p => p + drift);
            Band(on, moved, 6 * z, (150, 120, 200), 0.4 * a);
            Band(on, moved, 2.5f * z, (90, 60, 150), 0.55 * a);
        }
    }

    /// <summary>A multiplied band: white where clear, so <paramref name="alpha"/> mixes the colour toward white.</summary>
    private static void Band(Node2D on, Vector2[] points, float width, (int R, int G, int B) colour, double alpha)
    {
        float k = (float)Math.Clamp(alpha, 0, 1);
        var c = new Color(1 - k * (1 - colour.R / 255f), 1 - k * (1 - colour.G / 255f), 1 - k * (1 - colour.B / 255f));
        on.DrawPolyline(points, c, width, true);
    }

    /// <summary>The burn's first moment: a white-violet flash at the screen's edges and short arcs crackling in from them.</summary>
    private void DrawEdge(Node2D on)
    {
        double a = BoltLook.Edge(_burnSince);
        if (a <= 0) return;
        var size = GetViewportRect().Size;
        float scale = size.Y / 336, depth = 36 * scale;
        var glow = Color.Color8(226, 214, 255, (byte)(255 * 0.7 * a));
        var clear = glow with { A = 0 };
        (Vector2 A, Vector2 B, Vector2 C, Vector2 D)[] sides =
        [
            (new(0, 0), new(size.X, 0), new(size.X, depth), new(0, depth)),
            (new(0, size.Y), new(size.X, size.Y), new(size.X, size.Y - depth), new(0, size.Y - depth)),
            (new(0, 0), new(0, size.Y), new(depth, size.Y), new(depth, 0)),
            (new(size.X, 0), new(size.X, size.Y), new(size.X - depth, size.Y), new(size.X - depth, 0)),
        ];
        foreach (var (p0, p1, p2, p3) in sides) on.DrawPolygon([p0, p1, p2, p3], [glow, glow, clear, clear]);
        var random = new Random((int)(_burnSince * 30) + 1);
        float perimeter = 2 * (size.X + size.Y);
        for (int i = 0; i < 16; i++)
        {
            double u = BoltLook.Hash(i, 300) * perimeter, side = BoltLook.Hash(i, 301) - 0.5, length = (14 + 22 * BoltLook.Hash(i, 302)) * scale;
            var (p, n) = u < size.X ? (new Vector2((float)u, 0), Vector2.Down)
                : u < size.X + size.Y ? (new Vector2(size.X, (float)u - size.X), Vector2.Left)
                : u < 2 * size.X + size.Y ? (new Vector2(2 * size.X + size.Y - (float)u, size.Y), Vector2.Up)
                : (new Vector2(0, perimeter - (float)u), Vector2.Right);
            var end = p + n * (float)length + new Vector2((float)side, (float)side) * 20 * scale;
            var points = Vectors(BoltLook.Jag(random, (p.X, p.Y), (end.X, end.Y), 0.3, 3));
            float w = 0.6f * scale;
            on.DrawPolyline(points, Color.Color8(110, 70, 230, (byte)(255 * 0.28 * a)), 7 * w, true);
            on.DrawPolyline(points, Color.Color8(178, 132, 255, (byte)(255 * 0.7 * a)), 3 * w, true);
            on.DrawPolyline(points, Color.Color8(252, 246, 255, (byte)(255 * a)), 1.2f * w, true);
        }
    }

    /// <summary>A node that draws by the view's hand, on its own material.</summary>
    private sealed partial class Painted(Action<Node2D> draw) : Node2D
    {
        public override void _Draw() => draw(this);
    }

    /// <summary><paramref name="count"/> of <paramref name="points"/> from <paramref name="from"/>: a cold wide halo, a pale body, a white core.</summary>
    private void Channel(Vector2[] points, int from, int count, double bright, float width = 1)
    {
        var span = points.AsSpan(from, Math.Min(count, points.Length - from)).ToArray();
        float a = (float)Math.Clamp(bright, 0, 1);
        DrawPolyline(span, Color.Color8((byte)Halo.R, (byte)Halo.G, (byte)Halo.B, (byte)(255 * a * 0.22f)), 7 * width, true);
        DrawPolyline(span, Color.Color8((byte)Light.R, (byte)Light.G, (byte)Light.B, (byte)(255 * a * 0.55f)), 2.6f * width, true);
        DrawPolyline(span, Color.Color8((byte)Core.R, (byte)Core.G, (byte)Core.B, (byte)(255 * a)), 1.1f * width, true);
    }

    /// <summary>The soot streaked downwind from each crater struck while the surface is shown, on the ground cells still there.</summary>
    private sealed partial class SootMarks(IGridQueries grid, List<int> struck) : Node2D
    {
        private static readonly Color Soot = new(0.11f, 0.1f, 0.1f, 0.6f);

        public override void _Draw()
        {
            foreach (int cell in struck)
            {
                int width = grid.Width, column = cell % width, row = cell / width;
                for (int dx = 1; dx >= -4; dx--)
                {
                    int x = column + dx;
                    if (x < 0 || x >= width) continue;
                    for (int y = Math.Max(0, row - 2); y <= Math.Min(grid.Height - 1, row + 2); y++)
                    {
                        int at = y * width + x;
                        if (grid.WallAt(at) == null || (y > 0 && grid.WallAt(at - width) != null)) continue;
                        float fade = 1 - Math.Max(0, -dx) * 0.2f, deep = dx == 0 ? 2.6f : 1.6f;
                        var colour = Soot with { A = Soot.A * fade };
                        DrawRect(new Rect2(x * Cell, y * Cell, Cell, deep), colour);
                        DrawRect(new Rect2(x * Cell + 1, y * Cell + deep, Cell - 3, 0.8f), colour with { A = colour.A * 0.6f });
                        break;
                    }
                }
            }
        }
    }
}
