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
/// A storm's lightning seen (surface-storms, the picks of 2026-11-16): through its glow, arcs crackling on the edges of every
/// open-sky ground cell of its zone, thickest at the struck cell («Зарница»); in its last moments a faint leader stepping down
/// from above the view, then the return stroke and two more on the same channel («Ступенчатый лидер»), lighting the ground;
/// discharges running out over the zone, thunder, sparks and dark grains thrown from the crater («Искры»), and soot streaked
/// downwind left on the ground while this surface is shown. It reads the sim's <c>Strike</c> once a tick.
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

    private readonly IGridQueries _grid;
    private readonly HazardsModule _hazards;
    private readonly Func<double> _progress;
    private readonly Func<int> _tick;
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

    public BoltView(IGridQueries grid, HazardsModule hazards, Func<double> progress, Func<int> tick)
    {
        _grid = grid;
        _hazards = hazards;
        _progress = progress;
        _tick = tick;
        ZIndex = Layers.Effects;
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded, BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        AddChild(_thunder);
        // Soot lies on the ground, lit as the ground is, under the plants and the heroes.
        _marks = new SootMarks(_grid, _soot) { ZIndex = Layers.Edges, ZAsRelative = false };
        AddChild(_marks);
    }

    public override void _Process(double delta)
    {
        if (_tick() != _read)
        {
            _read = _tick();
            var now = _hazards.Strike;
            if (_glowing is { } was && now == null && was.Left <= 2) Struck(was);
            if (now != null && (_glowing == null || _glowing.Cell != now.Cell)) Begin(now);
            _glowing = now;
        }
        (_grown, _leader, _bright, _stroke, _branched) = (-1, 0, 0, 0, false);
        if (_glowing is { } glow) Glow(glow, delta);
        if (_since >= 0)
        {
            _since += delta;
            Flash(_since);
            if (_since > BoltLook.Done) _since = -1;
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
        double from = Math.Min(view.Position.Y - 2 * Cell, ty - 40 * Cell);
        var channel = BoltLook.Channel(_random, (tx, from), (tx, ty));
        _channel = Vectors(channel);
        _branches.Clear();
        foreach (var branch in BoltLook.Branches(_random, channel)) _branches.Add(Vectors(branch));
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

    /// <summary>The glow's arcs this frame, more as it nears its strike and near the middle; the leader in its last moments.</summary>
    private void Glow(Strike strike, double delta)
    {
        int glow = Math.Max(strike.Left, _glowTicks);
        double left = Math.Max(0, strike.Left - _progress()), grown = _grown = 1 - left / Math.Max(1, glow);
        var bolts = Stages.Of(Layers.Effects).Bolts;
        double frames = delta * 60;
        foreach (var (cell, off) in _zone)
        {
            double rate = BoltLook.GlowRate(grown, off, cell == strike.Cell) * frames;
            if (_random.NextDouble() < rate) Arc(bolts, cell, BoltLook.GlowIntensity(grown), 0.55);
        }
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
    }

    /// <summary>The bolt's strokes and their light, <paramref name="since"/> seconds after the strike; discharges over the zone early on.</summary>
    private void Flash(double since)
    {
        if (_channel.Length == 0 || _struck < 0) return;
        double bright = _bright = BoltLook.Brightness(since);
        var (x, y) = Top(_struck);
        if (bright > 0)
        {
            _stroke = BoltLook.ReturnReach(_channel.Length, since);
            _branched = BoltLook.Branched(since);
            Stages.Of(Layers.Effects).Flares.Hold(_channel, x, y - 12 * Cell, Light, bright * 1.6 * Fx.Glow, 40, 0.4f, 0.75);
        }
        if (since < BoltLook.Discharges)
        {
            var bolts = Stages.Of(Layers.Effects).Bolts;
            foreach (var (cell, off) in _zone)
                if (_random.NextDouble() < 0.12 * (1 - off * 0.5)) Arc(bolts, cell, 1 - since / BoltLook.Discharges * 0.5, 0.8);
        }
    }

    /// <summary>
    /// The glow's violet line along the top of every zone cell, flickering, brighter as it grows and near the middle; the
    /// leader's faint steps; the strokes' channel, even along its length (a <c>Trail</c> fades to its tail), and its forks.
    /// </summary>
    public override void _Draw()
    {
        if (_grown >= 0)
        {
            var (r, g, b) = BoltTint.Violet.Rim;
            foreach (var (cell, off) in _zone)
            {
                float a = (float)((0.12 + 0.55 * _grown) * (1 - 0.55 * off) * (0.65 + 0.35 * _random.NextDouble()));
                var left = new Vector2(cell % _grid.Width * Cell, cell / _grid.Width * Cell);
                var right = left + new Vector2(Cell, 0);
                DrawLine(left, right, Color.Color8((byte)r, (byte)g, (byte)b, (byte)(255 * a * 0.5f)), 4);
                DrawLine(left, right, Color.Color8(190, 150, 255, (byte)(255 * a)), 1.2f);
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
