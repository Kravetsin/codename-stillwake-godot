using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The heat past a body's high, without a word (2026-11-14-heat-limits, the user's pick C): the world at the screen's edges
/// shimmers, its rows pushed sideways by a rising wave, faintly warmed, and sparks drift up from the bottom and the sides; the
/// shimmer reaches in and the sparks thicken as the heat deepens. One full-screen quad over the world, under the HUD; hidden while
/// there is none. Reads the heat (<see cref="Report.Heat"/>) four times a second and eases toward it, as the frost does.
/// </summary>
public partial class Haze : CanvasLayer
{
    private const double RefreshS = 0.25;
    /// <summary>How long the haze takes to grow or fade by the whole of its range.</summary>
    private const double EaseS = 1.0;
    private static readonly StringName Strength = "strength";

    private const string Shader = """
        shader_type canvas_item;
        uniform float strength = 0.0;
        uniform sampler2D screen : hint_screen_texture, filter_linear, repeat_disable;

        float hash(float n) { return fract(sin(n * 127.1 + 311.7) * 43758.5453); }

        // A spark's light at p (pixels): a short stroke rising, its glow round it.
        vec3 spark(vec2 p, vec2 size, float i, float t, float e) {
            float side = hash(i * 1.7) < 0.6 ? 0.0 : (hash(i * 2.9) < 0.5 ? 1.0 : 2.0);
            float at = hash(i * 3.3), life = 2.2 + hash(i * 5.1) * 2.6, seed = hash(i * 7.7), hue = hash(i * 9.3);
            float rise = 0.35 + hash(i * 4.4) * 0.35, width = (1.4 + hash(i * 6.6) * 1.8) * size.y / 600.0;
            float ph = fract(t / life + seed), up = ph * size.y * rise;
            vec2 s;
            if (side == 0.0) { float u = at < 0.5 ? at * 0.5 : 1.0 - (1.0 - at) * 0.5; s = vec2(u * size.x, size.y + 6.0 - up); }
            else { s = vec2(side == 1.0 ? at * size.x * 0.08 : size.x - at * size.x * 0.08, size.y * (0.55 + 0.45 * seed) - up * 0.8); }
            s.x += sin(t * 1.6 + seed * 20.0) * 10.0 * ph * size.y / 600.0;
            float a = (1.0 - ph) * min(1.0, ph * 8.0) * (0.45 + 0.55 * e) * (0.7 + 0.3 * sin(t * 20.0 + seed * 40.0));
            vec2 tail = s + vec2(0.0, 5.0 * size.y / 600.0 + width * 2.0);
            vec2 pa = p - s, ba = tail - s;
            float d = length(pa - ba * clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0));
            float core = 1.0 - smoothstep(width * 0.3, width * 0.6, d), glow = exp(-d * d / (width * width * 6.0)) * 0.35;
            return mix(vec3(1.0, 0.88, 0.63), vec3(1.0, 0.38, 0.13), hue) * a * (core + glow);
        }

        void fragment() {
            vec2 size = 1.0 / SCREEN_PIXEL_SIZE;
            float e = strength, y = FRAGCOORD.y * 600.0 / size.y;
            // The rows pushed sideways by a wave rising with time, kept at the edges: reaching in as the heat deepens.
            float off = 4.0 * e * (sin(y * 0.045 + TIME * 5.2) + 0.55 * sin(y * 0.13 - TIME * 3.3)) * size.y / 600.0;
            float r = length((SCREEN_UV - 0.5) * 2.0), inner = mix(1.05, 0.5, e);
            float edge = smoothstep(inner, inner + 0.35, r);
            vec3 base = texture(screen, SCREEN_UV).rgb;
            vec3 hot = mix(texture(screen, SCREEN_UV + vec2(off * SCREEN_PIXEL_SIZE.x, 0.0)).rgb, vec3(1.0, 0.6, 0.29), 0.04 * e);
            vec3 sparks = vec3(0.0);
            float n = floor(90.0 * min(1.0, 0.15 + e));
            for (float i = 0.0; i < 90.0; i += 1.0) { if (i >= n) break; sparks += spark(FRAGCOORD.xy, size, i, TIME, e); }
            COLOR = vec4(mix(base, hot, edge) + sparks, 1.0);
        }
        """;

    private readonly Func<float> _read;
    private readonly ColorRect _screen = new();
    private readonly ShaderMaterial _material = new() { Shader = new Shader { Code = Shader } };
    private double _sinceRead = RefreshS, _shown;
    private float _heat;

    /// <param name="read">How hot this player's hero is, 0 to 1: <see cref="Report.Heat"/> at its body's cell.</param>
    public Haze(Func<float> read)
    {
        _read = read;
        Layer = ScreenLayers.Haze;
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
            _heat = _read();
        }
        _shown = _shown < _heat ? Math.Min(_heat, _shown + delta / EaseS) : Math.Max(_heat, _shown - delta / EaseS);
        _screen.Visible = _shown > 0;
        if (_screen.Visible) _material.SetShaderParameter(Strength, (float)_shown);
    }
}
