using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The cold past the field, without a word (0050; the look 2026-11-14-frost-film): a sheet of uneven ice over the screen's edges.
/// The world behind it is bent by frozen bumps at three sizes, more in some places than others, with a few soft crests catching
/// the light; cooled, paled and milky where the ice is thick. It thins away toward the middle with no line where it ends, reaching
/// further as the cold deepens. One full-screen quad over the world, under the HUD; hidden while there is none. Reads the cold
/// (<see cref="Report.Cold"/>) four times a second and eases toward it (<see cref="ScreenEase"/>), the ice growing from nothing.
/// </summary>
public partial class Frost : CanvasLayer
{
    private const double RefreshS = 0.25;
    // Made once: a string passed each frame would make a StringName each frame for the GC to finalize.
    private static readonly StringName Strength = "strength", Fading = "fade";

    private const string Shader = """
        shader_type canvas_item;
        uniform float strength = 0.0;
        // How much of it shows (ScreenEase.Fade): it fades out whole as the cold runs out.
        uniform float fade = 0.0;
        uniform sampler2D screen : hint_screen_texture, filter_linear, repeat_disable;

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
        // The slope of an fbm swell at w: the bend a bump of ice gives the world behind it.
        vec2 slope(vec2 w, float e) {
            return vec2(fbm(w + vec2(e, 0.0)) - fbm(w - vec2(e, 0.0)), fbm(w + vec2(0.0, e)) - fbm(w - vec2(0.0, e))) / (2.0 * e);
        }
        // The same of plain noise: the fine ripple, cheaper.
        vec2 ripple(vec2 w, float e) {
            return vec2(noise(w + vec2(e, 0.0)) - noise(w - vec2(e, 0.0)), noise(w + vec2(0.0, e)) - noise(w - vec2(0.0, e))) / (2.0 * e);
        }

        void fragment() {
            float s = strength;
            float aspect = SCREEN_PIXEL_SIZE.y / SCREEN_PIXEL_SIZE.x;
            vec2 q = SCREEN_UV * vec2(aspect, 1.0);
            // How far in from the nearest edge, in screen heights, the corners rounded: the ice gathers there first.
            vec2 a = abs(SCREEN_UV - 0.5) * 2.0;
            float d = smin((1.0 - a.x) * 0.5 * aspect, (1.0 - a.y) * 0.5, 0.18);
            // How far it reaches, wavering slowly; it thins all the way there, so no line shows where it ends, however short it reaches.
            float reach = 0.42 * s * (0.62 + 0.76 * fbm(q * 2.2 + vec2(0.0, TIME * 0.015))) + 0.06;
            float m = smoothstep(0.0, 1.0, 1.0 - d / reach);
            m *= m;
            COLOR = vec4(0.0);
            // The middle of the screen, clear of the ice, costs no more.
            if (m > 0.0) {
                // Frozen bumps at three sizes, all but still; how much each place bends the world differs, in patches.
                vec2 drift = vec2(TIME * 0.004, -TIME * 0.003);
                vec2 big = slope(q * 2.6 + drift, 0.05), mid = slope(q * 8.0 + 31.0 + drift, 0.03), fine = ripple(q * 26.0 + 7.0, 0.02);
                float zone = smoothstep(0.25, 0.75, fbm(q * 1.4 + 53.0));
                // A few soft crests where the ice ridged as it froze: faint, never a sharp line.
                float crest = pow(1.0 - abs(fbm(q * 5.0 + big * 0.3 + 11.0) * 2.0 - 1.0), 6.0);
                vec2 n = big * 1.0 + mid * (0.35 + 0.65 * zone) + fine * 0.12 * zone;
                vec2 off = (big * 0.010 + mid * 0.007 * (0.3 + 0.9 * zone) + fine * 0.0016 * zone) * m * (0.5 + 0.5 * s);
                vec3 world = texture(screen, clamp(SCREEN_UV + vec2(off.x / aspect, off.y), 0.0, 1.0)).rgb;
                // Cold glass: the world greyed and cooled, a milky haze where the ice is thick.
                float grey = dot(world, vec3(0.3, 0.59, 0.11));
                vec3 ice = mix(world, vec3(grey), 0.35) * vec3(0.86, 0.94, 1.06) + vec3(0.03, 0.05, 0.08) + vec3(0.12, 0.15, 0.18) * m * s;
                float lit = pow(clamp(dot(normalize(vec3(-n * 0.5, 1.0)), normalize(vec3(-0.45, -0.6, 0.66))), 0.0, 1.0), 20.0);
                vec3 colour = ice + vec3(0.95) * lit * 0.3 * m * s + vec3(0.85, 0.92, 1.0) * crest * 0.12 * m;
                COLOR = vec4(colour, clamp(m * (0.85 + 0.15 * s), 0.0, 1.0) * fade);
            }
        }
        """;

    private readonly Func<float> _read;
    private readonly ColorRect _screen = new();
    private readonly ShaderMaterial _material = new() { Shader = new Shader { Code = Shader } };
    private double _sinceRead = RefreshS, _shown;
    private float _cold;

    /// <param name="read">How cold this player's hero is, 0 to 1: its body's, <see cref="Report.Cold"/>.</param>
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
        _shown = ScreenEase.Toward(_shown, _cold, delta);
        _screen.Visible = _shown > 0;
        if (!_screen.Visible) return;
        _material.SetShaderParameter(Strength, (float)_shown);
        _material.SetShaderParameter(Fading, ScreenEase.Fade(_shown));
    }
}
