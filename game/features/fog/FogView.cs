using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Fog;

/// <summary>
/// A planet's fog at its sides (fog-edges), without a word: one full-screen quad over the whole world, the heroes and the sky,
/// under all that is read. Each column as thick as <see cref="FogShare.Column"/> says where it lies, the shader working it
/// out again pixel by pixel; over that the whole screen as <see cref="FogScreen"/> has it, held whole a moment after a move
/// across the fog. Faint wisps drift through it where it is neither clear nor whole. Hidden while none of it is on screen.
/// </summary>
public partial class FogView : CanvasLayer
{
    private const int Cell = ShipCamera.Cell;
    private static readonly StringName ViewLeft = "view_left", ViewRight = "view_right", ViewTop = "view_top", ViewBottom = "view_bottom", Screen = "screen";

    private const string Code = """
        shader_type canvas_item;
        uniform float view_left = 0.0;
        uniform float view_right = 1.0;
        uniform float view_top = 0.0;
        uniform float view_bottom = 1.0;
        uniform float ground_left = 0.0;
        uniform float ground_right = 0.0;
        uniform float screen = 0.0;
        uniform float width = 1.0;
        uniform float fog = 1.0;
        uniform float lead = 1.0;
        uniform float edge = 1.0;
        uniform vec3 colour = vec3(0.75);
        // Under the bands' ground line the fog darkens over a few rows, as underground is (0077).
        uniform float deepen = 24.0;
        uniform float dark = 0.12;

        float smooth01(float t) { t = clamp(t, 0.0, 1.0); return t * t * (3.0 - 2.0 * t); }
        float ground(float x) { return x < width * 0.5 ? ground_left : ground_right; }

        // FogShare.Column, again: none from lead columns inside the zone in, edge at its side, whole at the seam and out.
        float column(float x) {
            float t = min(x, width - x);
            if (t >= fog + lead) return 0.0;
            if (t >= fog) return edge * smooth01((fog + lead - t) / lead);
            if (t >= fog * 0.5) return edge + (1.0 - edge) * smooth01((fog - t) / (fog * 0.5));
            return 1.0;
        }

        float hash(vec2 p) {
            vec3 p3 = fract(vec3(p.xyx) * 0.1031);
            p3 += dot(p3, p3.yzx + 33.33);
            return fract((p3.x + p3.y) * p3.z);
        }
        float noise(vec2 p) {
            vec2 i = floor(p), f = fract(p);
            vec2 u = f * f * (3.0 - 2.0 * f);
            return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), u.x), mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), u.x), u.y);
        }

        void fragment() {
            float x = mix(view_left, view_right, UV.x);
            float y = mix(view_top, view_bottom, UV.y);
            float base = 1.0 - (1.0 - column(x)) * (1.0 - screen);
            // Wisps where it is thin, never where it is whole: at a seam nothing shows through.
            float wisp = noise(vec2(x * 0.08 + TIME * 0.15, UV.y * 6.0 - TIME * 0.05));
            float alpha = base + (1.0 - base) * base * 0.6 * wisp;
            // Lightening as the whole screen thickens toward a seam, so a move from a tunnel to the sky shows no change of shade.
            vec3 shade = colour * mix(1.0, dark, smoothstep(ground(x), ground(x) + deepen, y) * (1.0 - screen));
            COLOR = vec4(shade, clamp(alpha, 0.0, 1.0));
        }
        """;

    private readonly FogEdges _edges;
    private readonly FogScreen _screen;
    private readonly ColorRect _quad = new();
    private readonly ShaderMaterial _material = new() { Shader = new Shader { Code = Code } };

    /// <param name="edges">The planet's bands (<see cref="FogEdges.Of"/>).</param>
    /// <param name="colour">Its fog's colour (<c>PlanetLooks.Look.Fog</c>).</param>
    public FogView(FogEdges edges, (float R, float G, float B) colour)
    {
        (_edges, _screen) = (edges, new FogScreen(edges));
        Layer = ScreenLayers.Fog;
        _quad.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _quad.MouseFilter = Control.MouseFilterEnum.Ignore;
        _quad.Material = _material;
        _quad.Visible = false;
        _material.SetShaderParameter("width", (float)edges.Width);
        _material.SetShaderParameter("fog", (float)edges.Fog);
        _material.SetShaderParameter("lead", (float)FogLooks.Lead);
        _material.SetShaderParameter("edge", (float)FogLooks.Edge);
        _material.SetShaderParameter("colour", new Vector3(colour.R, colour.G, colour.B));
        // Each band's ground line beside the zone: under it the fog darkens.
        _material.SetShaderParameter("ground_left", (float)edges.LeftLine);
        _material.SetShaderParameter("ground_right", (float)edges.RightLine);
        AddChild(_quad);
    }

    public override void _Process(double delta)
    {
        var view = GetViewport().GetCanvasTransform().AffineInverse() * GetViewport().GetVisibleRect();
        double left = view.Position.X / Cell, right = view.End.X / Cell;
        double screen = _screen.Step((left + right) / 2, delta * 1000);
        // The fog thickens only toward the sides: clear at both edges of the view, none between.
        _quad.Visible = screen > 0 || FogShare.Column(_edges, left) > 0 || FogShare.Column(_edges, right) > 0;
        if (!_quad.Visible) return;
        _material.SetShaderParameter(ViewLeft, (float)left);
        _material.SetShaderParameter(ViewRight, (float)right);
        _material.SetShaderParameter(ViewTop, view.Position.Y / Cell);
        _material.SetShaderParameter(ViewBottom, view.End.Y / Cell);
        _material.SetShaderParameter(Screen, (float)screen);
    }
}
