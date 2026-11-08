using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The last breath without a word (2026-11-05-the-last-breath), as <see cref="Tunnel"/> says: one full-screen quad over
/// the HUD reading the screen under it, blurred more the farther from the hero, swimming, swaying about the hero and
/// doubled, browning, then ink; ink while passed out. Hidden while clear. Reads the hero four times a second.
/// </summary>
public partial class TunnelView : CanvasLayer
{
    private const double RefreshS = 0.25;
    /// <summary>Where on the hero the world sways about and blurs least: this far from its head to its feet, its chest.</summary>
    private const float Chest = 0.35f;

    private static readonly StringName Center = "center", Blur = "blur", Dark = "dark", Sway = "sway", Ghost = "ghost", Swim = "swim", Ink = "ink";

    private const string Shader = """
        shader_type canvas_item;
        uniform sampler2D screen : hint_screen_texture, filter_linear_mipmap;
        uniform vec2 center = vec2(0.5);
        uniform float blur = 0.0;
        uniform float dark = 0.0;
        uniform float sway = 0.0;
        uniform vec2 ghost = vec2(0.0);
        uniform float swim = 0.0;
        uniform float ink = 0.0;
        const vec3 brown = vec3(34.0, 12.0, 9.0) / 255.0;
        const vec3 ink_colour = vec3(23.0, 18.0, 15.0) / 255.0;

        void fragment() {
            vec2 size = 1.0 / SCREEN_PIXEL_SIZE;
            float aspect = size.x / size.y;
            vec2 c = center * size, d = FRAGCOORD.xy - c;
            // The world sways about the hero and swims in slow waves.
            float s = sin(-sway), k = cos(-sway);
            vec2 uv = (c + vec2(k * d.x - s * d.y, s * d.x + k * d.y)) / size;
            uv += vec2(sin(uv.y * 9.0 + TIME * 1.7), sin(uv.x * 7.0 + TIME * 1.3)) * swim * vec2(1.0, aspect);
            // Blurred everywhere, the more the farther from the hero, with no line between: at most a picture 40 pixels wide.
            float edge = smoothstep(0.08, 0.7, length(d) / size.x);
            float lod = blur * log2(size.x / 40.0) * mix(0.35, 1.0, edge);
            vec3 world = textureLod(screen, uv, lod).rgb;
            vec3 twin = textureLod(screen, uv - ghost * vec2(1.0, aspect), lod).rgb;
            vec3 colour = mix(mix(world, twin, 0.34), brown, dark * mix(0.55, 1.0, edge));
            COLOR = vec4(mix(colour, ink_colour, ink), 1.0);
        }
        """;

    private readonly Func<(int Gasp, int Out)?> _read;
    private readonly Func<Vector2?> _hero;
    private readonly Tunnel _tunnel = new();
    private readonly ColorRect _screen = new();
    private readonly ShaderMaterial _material = new() { Shader = new Shader { Code = Shader } };
    private double _sinceRead = RefreshS, _clock;

    /// <param name="read">This player's hero's last breath and ticks out, or none.</param>
    /// <param name="hero">Its head and feet in the world as drawn, or none.</param>
    public TunnelView(Func<(int Gasp, int Out)?> read, Func<(Vector2 Head, Vector2 Feet)?> hero)
    {
        _read = read;
        _hero = () => hero() is (var head, var feet) ? head.Lerp(feet, Chest) : null;
        Layer = ScreenLayers.LastBreath;
        _screen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _screen.MouseFilter = Control.MouseFilterEnum.Ignore;
        _screen.Material = _material;
        _screen.Visible = false;
        AddChild(_screen);
    }

    public override void _Process(double delta)
    {
        _clock += delta;
        _sinceRead += delta;
        if (_sinceRead >= RefreshS)
        {
            _sinceRead = 0;
            var (gasp, @out) = _read() ?? (0, 0);
            _tunnel.Read(gasp, @out);
        }
        double p = _tunnel.Advance(delta);
        _screen.Visible = p > 0;
        if (!_screen.Visible) return;
        var look = Tunnel.Of(p, _clock);
        var size = GetViewport().GetVisibleRect().Size;
        // With no hero shown, the screen's middle; panned away, its nearest edge.
        var at = _hero() is { } world ? GetViewport().GetCanvasTransform() * world / size : new Vector2(0.5f, 0.5f);
        _material.SetShaderParameter(Center, at.Clamp(Vector2.Zero, Vector2.One));
        _material.SetShaderParameter(Blur, (float)look.Blur);
        _material.SetShaderParameter(Dark, (float)look.Dark);
        _material.SetShaderParameter(Sway, (float)look.Sway);
        _material.SetShaderParameter(Ghost, new Vector2((float)look.GhostX, (float)look.GhostY));
        _material.SetShaderParameter(Swim, (float)look.Swim);
        _material.SetShaderParameter(Ink, (float)look.Ink);
    }
}
