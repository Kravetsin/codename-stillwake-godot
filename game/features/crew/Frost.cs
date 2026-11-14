using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The cold past the field, without a word (0050; the look the user's pick C, 2026-11-14-frost-film): a misted film creeping in
/// from the screen's edges, as breath on cold glass, blurring and paling the world behind it, its front ragged and drifting, motes
/// of rime sinking in it. One full-screen quad over the world, under the HUD; hidden while there is none. Reads the cold
/// (<see cref="Report.Cold"/>) four times a second and eases toward it (<see cref="ScreenEase"/>), the film growing from nothing.
/// </summary>
public partial class Frost : CanvasLayer
{
    private const double RefreshS = 0.25;
    // Made once: a string passed each frame would make a StringName each frame for the GC to finalize.
    private static readonly StringName Strength = "strength";

    private const string Shader = """
        shader_type canvas_item;
        uniform float strength = 0.0;
        uniform sampler2D screen : hint_screen_texture, filter_linear, repeat_disable;

        // Dave Hoskins' hashes without sine: no blocks where sin loses precision far from the origin.
        float hash(vec2 p) {
            vec3 p3 = fract(vec3(p.xyx) * 0.1031);
            p3 += dot(p3, p3.yzx + 33.33);
            return fract((p3.x + p3.y) * p3.z);
        }
        vec2 hash2(vec2 p) {
            vec3 p3 = fract(vec3(p.xyx) * vec3(0.1031, 0.1030, 0.0973));
            p3 += dot(p3, p3.yzx + 33.33);
            return fract((p3.xx + p3.yz) * p3.zy);
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
            float s = strength;
            float aspect = SCREEN_PIXEL_SIZE.y / SCREEN_PIXEL_SIZE.x;
            vec2 q = SCREEN_UV * vec2(aspect, 1.0);
            // How far in from the nearest edge, in screen heights, the corners rounded: the film gathers there first.
            vec2 a = abs(SCREEN_UV - 0.5) * 2.0;
            float d = smin((1.0 - a.x) * 0.5 * aspect, (1.0 - a.y) * 0.5, 0.18);
            float front = 0.40 * s * (0.65 + 0.7 * fbm(q * 1.6 + vec2(TIME * 0.012, 0.0)));
            float f = d - front;
            // Full over the outer part, soft toward the front; growing from nothing as the cold does.
            float m = (1.0 - smoothstep(-0.6 * front - 0.001, 0.0, f)) * smoothstep(0.0, 0.15, s);
            COLOR = vec4(0.0);
            // The middle of the screen, clear of the film, costs no more.
            if (m > 0.0) {
                // The blur: a golden-angle disc, wider where the film is thicker.
                float r = 0.011 * m;
                vec3 sum = texture(screen, SCREEN_UV).rgb;
                for (int i = 0; i < 16; i++) {
                    float fi = float(i), an = fi * 2.39996;
                    vec2 o = vec2(cos(an), sin(an)) * sqrt((fi + 0.5) / 16.0) * r;
                    sum += texture(screen, clamp(SCREEN_UV + vec2(o.x / aspect, o.y), 0.0, 1.0)).rgb;
                }
                float dens = m * (0.7 + 0.6 * fbm(q * 6.0 + vec2(TIME * 0.02, TIME * 0.008)));
                vec3 colour = mix(sum / 17.0, vec3(0.80, 0.86, 0.92), clamp(0.30 * dens, 0.0, 1.0)) + vec3(0.03, 0.04, 0.05) * dens;
                // Rime motes in two depths, sinking and swaying in the film; each twinkles slowly, never a grain.
                float motes = 0.0;
                for (int l = 0; l < 2; l++) {
                    float fl = float(l);
                    vec2 mc = q * (9.0 + fl * 7.0) - vec2(TIME * (0.05 + 0.03 * fl), TIME * (0.11 + 0.05 * fl));
                    vec2 mid = floor(mc);
                    float h = hash(mid + fl * 13.0);
                    vec2 mp = fract(mc) - 0.5 - (hash2(mid + 5.0) - 0.5) * 0.5 + 0.12 * vec2(sin(TIME * 0.7 + h * 6.28), cos(TIME * 0.5 + h * 9.0));
                    float rad = 0.05 + 0.04 * h;
                    motes += (1.0 - smoothstep(rad * 0.3, rad, length(mp))) * step(h, 0.4) * (0.55 + 0.45 * sin(TIME * 1.1 + h * 40.0));
                }
                motes *= m;
                COLOR = vec4(colour + vec3(0.92, 0.96, 1.0) * motes * 0.5, clamp(max(m, motes * 0.6), 0.0, 1.0));
            }
        }
        """;

    private readonly Func<float> _read;
    private readonly ColorRect _screen = new();
    private readonly ShaderMaterial _material = new() { Shader = new Shader { Code = Shader } };
    private double _sinceRead = RefreshS, _shown;
    private float _cold;

    /// <param name="read">How cold this player's hero is, 0 to 1: <see cref="Report.Cold"/> at its body's cell, bare or not.</param>
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
        if (_screen.Visible) _material.SetShaderParameter(Strength, (float)_shown);
    }
}
