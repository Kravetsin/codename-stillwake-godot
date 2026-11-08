using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Crew;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A hurt without a word (2026-11-08-hurts-are-shown): each new hurt of this player's hero washes the screen's edges in its
/// kind's colour, as <see cref="HurtLook"/> times it, blinks the suit gauge's loss in it (<see cref="SuitGauge.Hurt"/>) and says
/// the hero's «oof» (<see cref="S.Hurt"/>).
/// One full-screen quad over the world, under all that is read; hidden while clear. Reads the hero each frame, a lookup.
/// </summary>
public partial class HurtView : CanvasLayer
{
    private static readonly StringName Tint = "tint", Strength = "strength";

    private const string Shader = """
        shader_type canvas_item;
        uniform vec4 tint : source_color = vec4(1.0);
        uniform float strength = 0.0;

        float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
        float noise(vec2 p) {
            vec2 i = floor(p), f = fract(p), u = f * f * (3.0 - 2.0 * f);
            return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), u.x), mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), u.x), u.y);
        }
        float fbm(vec2 p) {
            float v = 0.0, a = 0.5;
            for (int k = 0; k < 4; k++) { v += a * noise(p); p *= 2.0; a *= 0.5; }
            return v;
        }

        void fragment() {
            vec2 size = 1.0 / SCREEN_PIXEL_SIZE;
            float m = min(size.x, size.y);
            vec2 px = UV * size;
            // How far in from the nearest edge, by the shorter side; the wash's reach ragged as paper takes it.
            float d = min(min(px.x, size.x - px.x), min(px.y, size.y - px.y)) / m;
            float reach = 0.13 + 0.12 * fbm(px / m * 5.0);
            float wash = 1.0 - smoothstep(0.0, reach, d);
            // Blots where the paper drinks more, and the pigment pooling darker where the wash stops.
            float blot = smoothstep(0.3, 0.8, fbm(px / m * 2.5 + 7.0));
            float pool = smoothstep(0.55, 0.85, wash) * (1.0 - smoothstep(0.85, 1.0, wash));
            vec3 colour = tint.rgb * (1.0 - 0.22 * pool);
            COLOR = vec4(colour, clamp(wash * (0.6 + 0.4 * blot) * strength, 0.0, 1.0));
        }
        """;

    private readonly Func<(int Hurts, Harm Hurt)?> _read;
    private readonly SuitGauge _gauge;
    private readonly HurtSeen _seen = new();
    private readonly ColorRect _screen = new();
    private readonly ShaderMaterial _material = new() { Shader = new Shader { Code = Shader } };
    private readonly AudioStreamPlayer _oof = Cues.Player(S.Hurt, S.Helmet);
    private double _since = HurtLook.FadeS;

    /// <param name="read">This player's hero's hurts and the last one's kind, or none.</param>
    /// <param name="gauge">The suit's gauge, whose loss blinks with each hurt.</param>
    public HurtView(Func<(int Hurts, Harm Hurt)?> read, SuitGauge gauge)
    {
        (_read, _gauge) = (read, gauge);
        Layer = ScreenLayers.Hurt;
        _screen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _screen.MouseFilter = Control.MouseFilterEnum.Ignore;
        _screen.Material = _material;
        _screen.Visible = false;
        AddChild(_screen);
        AddChild(_oof);
    }

    public override void _Process(double delta)
    {
        if (_seen.Read(_read()) is Harm harm)
        {
            var tint = new Color(HurtLook.Of(harm));
            _material.SetShaderParameter(Tint, tint);
            _gauge.Hurt(tint);
            _oof.Play();
            _since = 0;
        }
        else _since += delta;
        float strength = (float)HurtLook.Edge(_since);
        _screen.Visible = strength > 0;
        if (_screen.Visible) _material.SetShaderParameter(Strength, strength);
    }
}
