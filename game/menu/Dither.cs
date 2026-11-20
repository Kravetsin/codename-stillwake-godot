using System.Globalization;
using System.Linq;
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

    private static string F(double v) => v.ToString("0.0#####", CultureInfo.InvariantCulture);

    /// <summary>The soft light's falloff as a shader's sum: each stop's span added straight between its strengths.</summary>
    private static string Falloff() => string.Join("\n", TitleScene.Falloff.Zip(TitleScene.Falloff.Skip(1), (a, b) =>
        $"    if (u >= {F(a.At)} && u < {F(b.At)}) a = mix({F(a.Alpha)}, {F(b.Alpha)}, (u - {F(a.At)}) / {F(b.At - a.At)});"));

    private static readonly Shader SunShader = new()
    {
        Code = $$"""
            shader_type canvas_item;
            render_mode blend_premul_alpha;
            {{Noise}}
            // The sun (TitleScene): its warm soft falloff over the dark, its small hard disc on it; noise on the final colour.
            void fragment() {
                float d = distance(UV, vec2(0.5)) * {{F(2 * TitleScene.GlowR)}};
                float u = d / {{F(TitleScene.GlowR)}};
                float a = 0.0;
            {{Falloff()}}
                float core = clamp(({{F(TitleScene.CoreR)}} - d) / max(fwidth(d), 1e-4) + 0.5, 0.0, 1.0);
                vec3 rgb = vec3(1.0, 228.0 / 255.0, 190.0 / 255.0) * a * (1.0 - core) + vec3(1.0, 247.0 / 255.0, 230.0 / 255.0) * core;
                float alpha = a * (1.0 - core) + core;
                COLOR = vec4(rgb + vec3(noise(FRAGCOORD.xy) * 2.0 / 255.0) * min(alpha * 12.0, 1.0), alpha);
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

    /// <summary>The title's sun and its soft light, in the scene's pixels about <see cref="TitleScene.Sun"/>.</summary>
    public static ColorRect Sun()
    {
        float r = (float)TitleScene.GlowR;
        return new ColorRect
        {
            Material = new ShaderMaterial { Shader = SunShader },
            Size = new Vector2(2 * r, 2 * r),
            Position = new Vector2((float)TitleScene.Sun.X - r, (float)TitleScene.Sun.Y - r),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
    }
}
