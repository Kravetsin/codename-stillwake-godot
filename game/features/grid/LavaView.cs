using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Grid;

/// <summary>
/// The planet's lava (its core, <c>Content.Lava</c>): its band drawn glowing over its own tiles, unlit, from
/// <c>art/planet/wall_lava</c> flowing slowly, its surface white-hot; one quad the band's length, which the GPU clips to the view.
/// Lava is never dug nor built, so the band is the ground's rows, never read again. Where the open is over its surface it gives
/// light (<see cref="Lights"/>, <see cref="LavaLooks"/>), looked for each frame along the view's columns only.
/// </summary>
public partial class LavaView : Node2D
{
    private const int Cell = ShipCamera.Cell;

    private static readonly Shader Shader = new()
    {
        Code = """
            shader_type canvas_item;
            render_mode unshaded;
            uniform sampler2D lava : filter_nearest, repeat_enable;
            // The surface's row in pixels of the world.
            uniform float surface = 0.0;
            varying vec2 world;

            void vertex() {
                world = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
            }

            void fragment() {
                vec2 p = floor(world);
                vec2 size = vec2(textureSize(lava, 0));
                float t = TIME;
                // Two sheets of crust drifting slowly apart, each bent by a slow wave; the brighter shows: cracks open and close.
                vec2 a = floor(p + vec2(t * 2.0 + sin(p.y * 0.4 + t * 0.8) * 2.0, cos(p.x * 0.11 + t * 0.6) * 1.5));
                vec2 b = floor(p.yx * vec2(1.0, -1.0) + vec2(-t * 1.5 + 37.0, 11.0 + sin(p.x * 0.07 + t) * 2.0));
                vec3 one = texture(lava, a / size).rgb, two = texture(lava, b / size).rgb;
                vec3 c = max(one, two * 0.85);
                // Churning: each patch breathes, brighter and dimmer, slowly.
                c *= 0.88 + 0.16 * sin(t * 1.7 + p.x * 0.09 + p.y * 0.3);
                // The surface white-hot, a row of pixels, then orange fading into the flow below it.
                float down = p.y - surface;
                if (down < 1.0) c = vec3(1.0, 0.86, 0.5);
                else if (down < 4.0) c = mix(vec3(1.0, 0.55, 0.15), c, (down - 1.0) / 3.0);
                COLOR = vec4(c, 1.0);
            }
            """,
    };

    private readonly IGridQueries _grid;
    private readonly int _top, _rows, _left, _right;
    private readonly List<LightSource> _lights = [];
    private double _timeMs;

    /// <summary>The lava's lights near the view: where the open is over its surface, the nearest the view's middle.</summary>
    public IReadOnlyList<LightSource> Lights => _lights;

    public LavaView(IGridQueries grid, Spec.Ground ground)
    {
        _grid = grid;
        (_top, _rows) = (ground.Height - ground.CoreRows, ground.LavaRows);
        (_left, _right) = (ground.BedrockSide, ground.Width - ground.BedrockSide);
        ZIndex = Layers.Lava;
        var material = new ShaderMaterial { Shader = Shader };
        material.SetShaderParameter("lava", GD.Load<Texture2D>("res://art/planet/wall_lava.png"));
        material.SetShaderParameter("surface", _top * Cell);
        Material = material;
    }

    public override void _Draw() =>
        DrawRect(new Rect2(_left * Cell, _top * Cell, (_right - _left) * Cell, _rows * Cell), Colors.White);

    public override void _Process(double delta)
    {
        _timeMs += delta * 1000;
        _lights.Clear();
        var view = GetViewport().GetCanvasTransform().AffineInverse() * GetViewportRect();
        // Below the view, or too far above it for its light to reach in: no lights.
        if (view.End.Y < (_top - LavaLooks.Reach) * Cell || view.Position.Y > _top * Cell) return;
        int w = _grid.Width, row = (_top - 1) * w;
        int from = Math.Max(_left, (int)Math.Floor(view.Position.X / Cell) - LavaLooks.Reach);
        int to = Math.Min(_right, (int)Math.Ceiling(view.End.X / Cell) + LavaLooks.Reach);
        int middle = (int)(view.GetCenter().X / Cell);
        // Out from the view's middle both ways, so the cap leaves out the farthest.
        Stretch(Math.Clamp(middle, from, to), to, +1);
        Stretch(Math.Clamp(middle, from, to) - 1, from - 1, -1);

        void Stretch(int start, int end, int step)
        {
            int run = 0;
            for (int x = start; x != end && _lights.Count < LavaLooks.MostLights; x += step)
            {
                bool open = _grid.StructureAt(row + x) != Structure.Wall;
                // A light every few open cells from the first, so a pit one cell wide gets one.
                if (open && run++ % LavaLooks.LightEvery == 0) Add(x);
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
}
