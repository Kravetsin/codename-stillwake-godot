using Godot;

namespace Stillwake.Game.Menu;

/// <summary>
/// The title's soft gradients drawn by a shader with a little noise in them (2026-10-25-title-scene): eight bits of a dark
/// colour show their steps as bands across a screen, and the noise, under a step, hides them.
/// </summary>
public static class Dither
{
    private const string Noise = """
        float noise(vec2 at) { return fract(sin(dot(floor(at), vec2(12.9898, 78.233))) * 43758.5453) - 0.5; }
        """;

    private static readonly Shader ShadeShader = new()
    {
        Code = $$"""
            shader_type canvas_item;
            {{Noise}}
            uniform sampler2D screen : hint_screen_texture, filter_nearest, repeat_disable;
            // Darker toward the left, easing out over two thirds of the screen. Blended over the screen itself, not by alpha:
            // the noise then lands on the final colour, a step of brightness either way, and breaks every band.
            void fragment() {
                float a = 0.74 * pow(1.0 - smoothstep(0.0, 0.66, UV.x), 1.6);
                vec3 under = texture(screen, SCREEN_UV).rgb * (1.0 - a);
                float n = noise(FRAGCOORD.xy) + noise(FRAGCOORD.xy + vec2(37.0, 91.0));
                COLOR = vec4(under + vec3(n / 255.0) * min(a * 12.0, 1.0), 1.0);
            }
            """,
    };

    private static readonly Shader GlowShader = new()
    {
        Code = $$"""
            shader_type canvas_item;
            render_mode blend_add;
            {{Noise}}
            uniform vec4 tint : source_color = vec4(1.0);
            uniform float strength = 1.0;
            // A soft disc, brightest at its middle, the tint added to what is under it.
            void fragment() {
                float d = clamp(1.0 - distance(UV, vec2(0.5)) * 2.0, 0.0, 1.0);
                float a = d * d * strength;
                COLOR = vec4(tint.rgb * a + noise(FRAGCOORD.xy) * 2.0 / 255.0, 1.0);
            }
            """,
    };

    /// <summary>The screen darkened from the left, so the title's words read over the scene: a full-screen rect.</summary>
    public static ColorRect Shade()
    {
        var shade = new ColorRect { Material = new ShaderMaterial { Shader = ShadeShader }, MouseFilter = Control.MouseFilterEnum.Ignore };
        shade.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        return shade;
    }

    /// <summary>A soft glow of <paramref name="tint"/> added to what is under it, <paramref name="size"/> pixels across; its strength set by <see cref="Shine"/>.</summary>
    public static ColorRect Glow(Color tint, float size)
    {
        var material = new ShaderMaterial { Shader = GlowShader };
        material.SetShaderParameter("tint", tint);
        return new ColorRect { Material = material, Size = new Vector2(size, size), Position = -new Vector2(size, size) / 2, MouseFilter = Control.MouseFilterEnum.Ignore };
    }

    /// <summary>How strongly a <see cref="Glow"/> shines now, 0 to 1.</summary>
    public static void Shine(ColorRect glow, float strength) => ((ShaderMaterial)glow.Material).SetShaderParameter("strength", strength);
}
