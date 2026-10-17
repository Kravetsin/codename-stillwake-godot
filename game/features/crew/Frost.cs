using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The cold past the field, without a word (0050): frost creeping in from the screen's edges, ragged and
/// veined, with a faint static over it, thin a little past the line and closing in toward the world's
/// edge. One full-screen quad over the world, under the HUD; hidden while there is none. Reads the cold
/// (<see cref="Report.Cold"/>) four times a second and eases toward it.
/// </summary>
public partial class Frost : CanvasLayer
{
    private const double RefreshS = 0.25;
    /// <summary>How long the frost takes to grow or thaw by the whole of its range.</summary>
    private const double EaseS = 1.0;
    // Made once: a string passed each frame would make a StringName each frame for the GC to finalize.
    private static readonly StringName Strength = "strength";

    private const string Shader = """
        shader_type canvas_item;
        uniform float strength = 0.0;

        // Dave Hoskins' hash without sine: no blocks where sin loses precision far from the origin.
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
            for (int i = 0; i < 4; i++) { sum += amp * noise(p); p = p * 2.03 + 17.0; amp *= 0.5; }
            return sum;
        }
        float smin(float a, float b, float k) {
            float h = clamp(0.5 + 0.5 * (b - a) / k, 0.0, 1.0);
            return mix(b, a, h) - k * h * (1.0 - h);
        }

        void fragment() {
            vec2 size = 1.0 / SCREEN_PIXEL_SIZE;
            float aspect = size.x / size.y;
            vec2 a = abs(UV - 0.5) * 2.0;
            // How far in from the nearest edge, in screen heights, the corners rounded: frost gathers there first.
            float d = smin((1.0 - a.x) * 0.5 * aspect, (1.0 - a.y) * 0.5, 0.18);
            vec2 q = UV * vec2(aspect, 1.0);
            // A ragged front reaching in with the cold; behind it the ice, veined, thicker toward the edge.
            float reach = mix(0.03, 0.36, strength);
            float front = reach * (0.55 + 0.9 * fbm(q * 3.5));
            float ice = 1.0 - smoothstep(front * 0.45, front, d);
            float veins = pow(1.0 - abs(fbm(q * 16.0 + 5.0) * 2.0 - 1.0), 5.0);
            float thick = 1.0 - clamp(d / max(front, 0.001), 0.0, 1.0);
            vec3 colour = mix(vec3(0.60, 0.76, 0.88), vec3(0.94, 0.98, 1.0), veins);
            float alpha = ice * (0.22 + 0.5 * thick + 0.35 * veins) * (0.3 + 0.7 * strength);
            // Static: a grain changing every frame, faint, thicker in the ice.
            float grain = hash(floor(FRAGCOORD.xy / 2.0) + floor(TIME * 24.0) * vec2(7.0, 13.0)) - 0.5;
            alpha += grain * strength * (0.05 + 0.12 * ice);
            COLOR = vec4(colour + grain * 0.2, clamp(alpha, 0.0, 0.9));
        }
        """;

    private readonly Func<float> _read;
    private readonly ColorRect _screen = new();
    private readonly ShaderMaterial _material = new() { Shader = new Shader { Code = Shader } };
    private double _sinceRead = RefreshS, _shown;
    private float _cold;

    /// <param name="read">How cold this player's hero is, 0 to 1: <see cref="Report.Cold"/> at its body's cell.</param>
    public Frost(Func<float> read)
    {
        _read = read;
        Layer = ScreenLayers.Frost;
        _screen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _screen.MouseFilter = Control.MouseFilterEnum.Ignore;
        _screen.Material = _material;
        _screen.Visible = false;
        AddChild(_screen);
    }

    public override void _Process(double delta)
    {
        _sinceRead += delta;
        if (_sinceRead >= RefreshS)
        {
            _sinceRead = 0;
            _cold = _read();
        }
        _shown = _shown < _cold ? Math.Min(_cold, _shown + delta / EaseS) : Math.Max(_cold, _shown - delta / EaseS);
        _screen.Visible = _shown > 0;
        if (_screen.Visible) _material.SetShaderParameter(Strength, (float)_shown);
    }
}
