using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Fog;

/// <summary>
/// A planet's fog at its sides (fog-edges), without a word: one full-screen quad over the whole world, the heroes and the sky,
/// under all that is read. Each column as thick as <see cref="FogShare.Column"/> says where it lies, the shader working it out
/// again pixel by pixel; over that the whole screen as <see cref="FogScreen"/> has it, held whole a moment after a move across
/// the fog. Textured from the frost's noise (<c>Features.Crew.Frost</c>): billows warped through themselves and long flat
/// wisps, thinner and thicker by them and lighter and darker, so even whole it is no flat colour. The texture follows the
/// view as it scrolls, drifting with the ground, but not a move across the fog: a period's jump would show it pop. Darker under
/// the bands' ground line as underground is. Hidden while none of it is on screen.
/// </summary>
public partial class FogView : CanvasLayer
{
    private const int Cell = ShipCamera.Cell;
    private static readonly StringName ViewLeft = "view_left", ViewRight = "view_right", ViewTop = "view_top", ViewBottom = "view_bottom",
        Screen = "screen", Scroll = "scroll";

    private const string Code = """
        shader_type canvas_item;
        uniform float view_left = 0.0;
        uniform float view_right = 1.0;
        uniform float view_top = 0.0;
        uniform float view_bottom = 1.0;
        // The texture's own place, in cells: the view's top-left as it scrolled, a move across the fog left out.
        uniform vec2 scroll = vec2(0.0);
        uniform float screen = 0.0;
        uniform float width = 1.0;
        uniform float fog = 1.0;
        uniform float out_cols = 0.0;
        uniform float lead = 1.0;
        uniform float edge = 1.0;
        uniform vec3 colour = vec3(0.75);
        uniform float ground_left = 0.0;
        uniform float ground_right = 0.0;
        // Under the bands' ground line the fog darkens over a few rows, as underground is (0077).
        uniform float deepen = 24.0;
        uniform float dark = 0.12;

        float smooth01(float t) { t = clamp(t, 0.0, 1.0); return t * t * (3.0 - 2.0 * t); }
        float ground(float x) { return x < width * 0.5 ? ground_left : ground_right; }

        // FogShare.Column, again: whole over a band, thinning past it to edge, gone lead columns further in.
        float column(float x) {
            float t = min(x, width - x), inner = fog + out_cols;
            if (t >= inner + lead) return 0.0;
            if (t >= inner) return edge * smooth01((inner + lead - t) / lead);
            if (t > fog) return edge + (1.0 - edge) * smooth01((inner - t) / max(1.0, out_cols));
            return 1.0;
        }

        // Dave Hoskins' hash without sine, and value noise and fbm as the frost's.
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
        float fbm(vec2 p) {
            float sum = 0.0, amp = 0.5;
            for (int i = 0; i < 5; i++) { sum += amp * noise(p); p = p * 2.03 + 17.0; amp *= 0.5; }
            return sum;
        }

        void fragment() {
            float x = mix(view_left, view_right, UV.x);
            float y = mix(view_top, view_bottom, UV.y);
            vec2 p = scroll + UV * vec2(view_right - view_left, view_bottom - view_top);
            // Billows wider than tall, warped through themselves as the frost's are and drifting slowly sideways; over them long
            // wisps, stretched flat, sliding faster. No ridges: they draw veins, and veins read as water, not fog.
            vec2 q = p * vec2(0.025, 0.045);
            vec2 warp = vec2(fbm(q + vec2(TIME * 0.02, 0.0)), fbm(q + vec2(5.2, 1.3) - vec2(0.0, TIME * 0.01)));
            float billow = fbm(q + warp * 1.6 + vec2(TIME * 0.015, 0.0));
            float wisp = fbm(p * vec2(0.012, 0.11) + vec2(TIME * 0.06, billow * 2.0));
            float n = smoothstep(0.2, 0.8, billow * 0.75 + wisp * 0.4 - 0.08);
            float base = 1.0 - (1.0 - column(x)) * (1.0 - screen);
            // Thicker in the billows, thinner between, but whole where it is whole: at a seam nothing shows through.
            float alpha = clamp(base + base * (1.0 - base) * (n - 0.45) * 2.6, 0.0, 1.0);
            vec3 tint = colour * (0.76 + 0.38 * n);
            // Lightening as the whole screen thickens toward a seam, so a move from a tunnel to the sky shows no change of shade.
            float under = smoothstep(ground(x), ground(x) + deepen, y) * (1.0 - screen);
            COLOR = vec4(tint * mix(1.0, dark, under), alpha);
        }
        """;

    private readonly FogEdges _edges;
    private readonly FogScreen _screen;
    private readonly ColorRect _quad = new();
    private readonly ShaderMaterial _material = new() { Shader = new Shader { Code = Code } };
    private Vector2? _last;
    private Vector2 _scroll;

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
        _material.SetShaderParameter("out_cols", (float)edges.Out);
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
        var corner = view.Position / Cell;
        double left = corner.X, right = view.End.X / Cell;
        // The texture scrolls with the view, but a jump of over half the grid is a move across the fog: left out.
        if (_last is not Vector2 last) _scroll = corner;
        else if (Math.Abs(corner.X - last.X) <= _edges.Width / 2.0) _scroll += corner - last;
        _last = corner;
        double screen = _screen.Step((left + right) / 2, delta * 1000);
        // The fog thickens only toward the sides: clear at both edges of the view, none between.
        _quad.Visible = screen > 0 || FogShare.Column(_edges, left) > 0 || FogShare.Column(_edges, right) > 0;
        if (!_quad.Visible) return;
        _material.SetShaderParameter(ViewLeft, (float)left);
        _material.SetShaderParameter(ViewRight, (float)right);
        _material.SetShaderParameter(ViewTop, corner.Y);
        _material.SetShaderParameter(ViewBottom, view.End.Y / Cell);
        _material.SetShaderParameter(Scroll, _scroll);
        _material.SetShaderParameter(Screen, (float)screen);
    }
}
