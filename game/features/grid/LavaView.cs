using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Features.Sound;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Grid;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Grid;

/// <summary>
/// The planet's lava (its core, <c>Content.Lava</c>), drawn as the cave concepts drew it (0087): over its own tiles, unlit, one
/// quad the band's length that the GPU clips to the view, its shader the concept's value noise — patches of deep orange to gold,
/// thin pale cracks, a white-hot surface — set slowly churning; a warm haze rising off it; embers drifting up. Where rock lies
/// over its surface it shows its dark crust, only the cracks glowing dimly through: the open over each column is a texel of a
/// row texture, laid again when the grid changes. Where the open is over it, it gives light (<see cref="Lights"/>), looked for
/// each frame along the view's columns only. Lava is never dug nor built, so the band is the ground's rows.
/// </summary>
public partial class LavaView : Node2D, ILightSource
{
    private const int Cell = ShipCamera.Cell;

    /// <summary>The concept's value noise, and how open the surface is over a pixel's column, blended across a cell.</summary>
    private static readonly string Common = $$"""
        uniform sampler2D open_cells : filter_linear, repeat_disable;
        // The surface's row in pixels of the world.
        uniform float surface = 0.0;
        varying vec2 world;

        void vertex() {
            world = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
        }

        float hash(vec2 p) {
            vec3 q = fract(vec3(p.xyx) * 0.1031);
            q += dot(q, q.yzx + 33.33);
            return fract((q.x + q.y) * q.z);
        }

        float vnoise(vec2 p, float cell, float seed) {
            vec2 g = floor(p / cell), f = p / cell - g;
            f = f * f * (3.0 - 2.0 * f);
            g += seed * 17.0;
            return mix(mix(hash(g), hash(g + vec2(1.0, 0.0)), f.x), mix(hash(g + vec2(0.0, 1.0)), hash(g + vec2(1.0, 1.0)), f.x), f.y);
        }

        float opened() {
            return texture(open_cells, vec2(world.x / {{Cell}}.0 / float(textureSize(open_cells, 0).x), 0.5)).r;
        }
        """;

    private static readonly Shader Band = new()
    {
        Code = "shader_type canvas_item;\nrender_mode unshaded;\n" + Common + """
            // The band's height in pixels.
            uniform float depth = 32.0;

            void fragment() {
                vec2 p = floor(world);
                float t = TIME;
                // The concept's patches (6-pixel noise) churn slowly along; its cracks (where a 9-pixel noise crosses its middle)
                // drift the other way, opening and closing.
                float n = vnoise(floor(p + vec2(t * 3.0, sin(t * 0.4 + p.x * 0.03) * 1.5)), 6.0, 77.0);
                float k = vnoise(floor(p + vec2(-t * 2.0, 0.0)), 9.0, 78.0);
                bool crack = abs(k - 0.5) < 0.045 + 0.015 * sin(t * 1.3 + p.x * 0.05);
                float down = p.y - surface;
                // As the concept's page shows it lit: white-hot at the top, pale cracks, gold, orange and deep orange patches.
                vec3 hot = down < 2.0 ? vec3(1.0, 0.965, 0.86) : crack ? vec3(1.0, 0.88, 0.4)
                    : n > 0.6 ? vec3(1.0, 0.8, 0.38) : n > 0.4 ? vec3(1.0, 0.66, 0.27) : vec3(1.0, 0.55, 0.22);
                hot = min(hot * (1.0 + 0.05 * sin(t * 1.7 + p.x * 0.09 + p.y * 0.3)), vec3(1.0));
                // Under rock, its crust: the concept's unlit colours, darker, the cracks alone glowing through, dimly.
                vec3 crust = crack ? vec3(0.36, 0.09, 0.03) : n > 0.6 ? vec3(0.2, 0.07, 0.04) : n > 0.4 ? vec3(0.15, 0.05, 0.03) : vec3(0.1, 0.04, 0.03);
                vec3 c = mix(crust, hot, opened());
                c *= 1.0 - 0.1 * smoothstep(0.75, 1.0, down / depth);
                COLOR = vec4(c, 1.0);
            }
            """,
    };

    private static readonly Shader Haze = new()
    {
        Code = "shader_type canvas_item;\nrender_mode unshaded, blend_add;\n" + Common + """
            // How far up it rises, in pixels, and how strong it is at the surface.
            uniform float reach = 16.0;
            uniform float strength = 0.6;
            uniform vec3 colour = vec3(1.0, 0.5, 0.18);

            void fragment() {
                float up = clamp((surface - world.y) / reach, 0.0, 1.0);
                COLOR = vec4(colour, opened() * (1.0 - up) * (1.0 - up) * strength);
            }
            """,
    };

    private readonly IGridQueries _grid;
    private readonly int _top, _rows, _left, _right;
    private readonly List<LightSource> _lights = [];
    private readonly List<int> _open = [];
    private readonly byte[] _openCells;
    private readonly ImageTexture _openTexture;
    private int _version = -1;
    private double _timeMs, _emberDue;
    /// <summary>
    /// The lava heard (0093): its rumble at the open lava nearest the view's middle while any is in reach, its bubbles at random
    /// moments at open cells in reach.
    /// </summary>
    private readonly Heard2D _bed = Cues.Player2D(Game.Sound.LavaBed, Game.Sound.World);
    private readonly Heard2D _bubble = Cues.Player2D(Game.Sound.LavaBubble, Game.Sound.World, voices: 3);
    private readonly List<int> _heard = [];
    private RandomTrain _bubbles;
    private readonly Random _random = new();

    /// <summary>The lava's lights near the view: where the open is over its surface, the nearest the view's middle.</summary>
    public IReadOnlyList<LightSource> Lights => _lights;

    public LavaView(IGridQueries grid, Spec.Ground ground)
    {
        _grid = grid;
        (_top, _rows) = (ground.Height - ground.CoreRows, ground.LavaRows);
        (_left, _right) = (ground.BedrockSide, ground.Width - ground.BedrockSide);
        _openCells = new byte[grid.Width];
        _openTexture = ImageTexture.CreateFromImage(Image.CreateFromData(grid.Width, 1, false, Image.Format.L8, _openCells));
        ZIndex = Layers.Lava;
        Material = MaterialOf(Band);
        ((ShaderMaterial)Material).SetShaderParameter("depth", _rows * Cell);
        var haze = new Node2D { Material = MaterialOf(Haze) };
        var hazeMaterial = (ShaderMaterial)haze.Material;
        hazeMaterial.SetShaderParameter("reach", LavaLooks.HazeRows * Cell);
        hazeMaterial.SetShaderParameter("strength", LavaLooks.Haze);
        hazeMaterial.SetShaderParameter("colour", new Vector3(LavaLooks.HazeColour.R, LavaLooks.HazeColour.G, LavaLooks.HazeColour.B));
        haze.Draw += () => haze.DrawRect(
            new Rect2(_left * Cell, (_top - LavaLooks.HazeRows) * Cell, (_right - _left) * Cell, LavaLooks.HazeRows * Cell), Colors.White);
        AddChild(haze);
        AddChild(_bed);
        AddChild(_bubble);
    }

    private ShaderMaterial MaterialOf(Shader shader)
    {
        var material = new ShaderMaterial { Shader = shader };
        material.SetShaderParameter("open_cells", _openTexture);
        material.SetShaderParameter("surface", _top * Cell);
        return material;
    }

    public override void _Draw() =>
        DrawRect(new Rect2(_left * Cell, _top * Cell, (_right - _left) * Cell, _rows * Cell), Colors.White);

    public override void _Process(double delta)
    {
        _timeMs += delta * 1000;
        _lights.Clear();
        _open.Clear();
        // The open over the surface, laid again when the grid changed: a dig down to it, a wall built over it.
        if (_grid.Version != _version)
        {
            _version = _grid.Version;
            int row = (_top - 1) * _grid.Width;
            for (int x = 0; x < _openCells.Length; x++) _openCells[x] = _grid.WallAt(row + x) == null ? (byte)255 : (byte)0;
            _openTexture.Update(Image.CreateFromData(_openCells.Length, 1, false, Image.Format.L8, _openCells));
        }
        var view = GetViewport().GetCanvasTransform().AffineInverse() * GetViewportRect();
        // Below the view, or too far above it for its light to reach in: no lights, no embers.
        if (view.End.Y < (_top - LavaLooks.Reach) * Cell || view.Position.Y > _top * Cell)
        {
            Hear(view.GetCenter(), delta);
            return;
        }
        int from = Math.Max(_left, (int)Math.Floor(view.Position.X / Cell) - LavaLooks.Reach);
        int to = Math.Min(_right, (int)Math.Ceiling(view.End.X / Cell) + LavaLooks.Reach);
        int middle = Math.Clamp((int)(view.GetCenter().X / Cell), from, to);
        // Out from the view's middle both ways, so the cap leaves out the farthest.
        Stretch(middle, to, +1);
        Stretch(middle - 1, from - 1, -1);
        Embers(delta);
        Hear(view.GetCenter(), delta);

        void Stretch(int start, int end, int step)
        {
            int run = 0;
            for (int x = start; x != end; x += step)
            {
                bool open = _openCells[x] != 0;
                if (open) _open.Add(x);
                // A light every few open cells from the first, so a pit one cell wide gets one.
                if (open && run++ % LavaLooks.LightEvery == 0 && _lights.Count < LavaLooks.MostLights) Add(x);
                if (!open) run = 0;
            }
        }
    }

    private void Add(int x)
    {
        float breathes = 1 - LavaLooks.Flicker + LavaLooks.Flicker * (float)Math.Sin(_timeMs / 600 + x * 1.3);
        _lights.Add(new LightSource((x + 0.5) * Cell, (_top - 0.5) * Cell, LavaLooks.Light, LavaLooks.Strength * breathes,
            LavaLooks.Reach, LavaLooks.Keep, 1, LavaLooks.Size));
    }

    /// <summary>
    /// The lava heard from <paramref name="ear"/>, the view's middle: its rumble at the open surface nearest it while any is within
    /// the rumble's reach, moved along as the view moves, and its bubbles at open cells within it.
    /// </summary>
    private void Hear(Vector2 ear, double delta)
    {
        _heard.Clear();
        float silent = Game.Sound.LavaBed.Heard!.Silent * Cell, best = float.MaxValue;
        int nearest = -1;
        foreach (int x in _open)
        {
            float far = SurfaceAt(x).DistanceTo(ear);
            if (far > silent) continue;
            _heard.Add(x);
            if (far < best) (best, nearest) = (far, x);
        }
        if (nearest >= 0)
        {
            _bed.GlobalPosition = SurfaceAt(nearest);
            // Begun somewhere along its loop, so it never starts on the same swell.
            if (!_bed.Playing) _bed.Play((float)(_random.NextDouble() * _bed.Stream.GetLength()));
        }
        else if (_bed.Playing) _bed.Stop();
        for (int due = _bubbles.Due(delta, _heard.Count > 0, Game.Sound.LavaBubblesPerSecond, _random); due > 0; due--)
        {
            var at = SurfaceAt(_heard[_random.Next(_heard.Count)]) + new Vector2((float)(_random.NextDouble() - 0.5) * Cell, 0);
            var (pitch, _) = StrikeTrain.Vary(_random, Game.Sound.LavaBubblePitch, 0);
            _bubble.PlayAt(at, pitch, (float)_random.NextDouble() * Game.Sound.LavaBubbleLowDb);
        }
    }

    /// <summary>The middle of the open cell over the lava's surface in column <paramref name="x"/>, in pixels of the world.</summary>
    private Vector2 SurfaceAt(int x) => new((x + 0.5f) * Cell, (_top - 0.5f) * Cell);

    /// <summary>Embers rising off the open surface near the view, at <see cref="LavaLooks.EmbersASecond"/> each open cell, at random.</summary>
    private void Embers(double delta)
    {
        if (_open.Count == 0) return;
        var glims = Glims.Shared;
        _emberDue += delta * LavaLooks.EmbersASecond * _open.Count;
        for (; _emberDue >= 1; _emberDue--)
        {
            int x = _open[(int)glims.Between(0, _open.Count - 0.001)];
            glims.Add(CaveLooks.Ember_, (x + glims.Between(0, 1)) * Cell, _top * Cell - glims.Between(1, 4), glims.Between(-4, 4),
                glims.Between(-16, -6), bright: glims.Between(0.4, 0.9));
        }
    }
}
