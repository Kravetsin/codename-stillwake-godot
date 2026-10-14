using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// How light falls on what the ship draws (render/lighting.ts): every lit picture is drawn with
/// <see cref="Set"/>'s material, which takes the stars' light from the starlight worked out cell by
/// cell, smoothed between cells, and adds what Godot's lamps give over it — where both reach, the
/// brighter shows, as in the TypeScript game, not their sum. A normal-mapped picture is shaded toward
/// or away from a lamp, a flat face taking the lamp's whole colour wherever it stands; a wall takes
/// <see cref="Light.WallIn"/> of what reaches its face.
/// </summary>
public static class Lit
{
    /// <summary>The light mask of lamps, and of the starlight: what <see cref="Set"/> gives what it lights.</summary>
    public const int LampMask = 1, StarMask = 2;

    private static readonly Shader Shader = new()
    {
        Code = """
            shader_type canvas_item;
            // How much a normal map shades: 0 flat, 1 as a face turned toward a lamp 45 degrees up.
            uniform float relief = 0.0;
            // The share of light that falls on it: a wall keeps Light.WallIn of what reaches its face.
            uniform float share = 1.0;
            // The stars' light, a texel a cell, laid over the world at starlight_rect (corner, size, in pixels).
            uniform sampler2D starlight : filter_linear, repeat_disable, hint_default_black;
            uniform vec4 starlight_rect = vec4(0.0, 0.0, 1.0, 1.0);
            varying vec2 world;

            void vertex() {
                world = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
            }

            void light() {
                vec3 star = texture(starlight, (world - starlight_rect.xy) / starlight_rect.zw).rgb;
                vec3 shine = star;
                float alpha = 1.0;
                if (!LIGHT_IS_DIRECTIONAL) {
                    vec3 l = LIGHT_DIRECTION;
                    // A lamp far off along the floor shades as one 45 degrees up, never as a grazing one.
                    vec3 tilted = normalize(vec3(l.xy, max(l.z, length(l.xy))));
                    float facing = clamp(dot(NORMAL, tilted) / tilted.z, 0.0, 1.5);
                    // Only what a lamp gives over the stars: lit by both, a point shows the brighter.
                    shine = max(LIGHT_COLOR.rgb * LIGHT_ENERGY * mix(1.0, facing, relief) - star, 0.0);
                    alpha = LIGHT_COLOR.a;
                }
                LIGHT = vec4(shine * COLOR.rgb * share, alpha);
            }
            """,
    };

    private static readonly Dictionary<(float Share, bool Relief), ShaderMaterial> Made = new();
    private static (Texture2D Texture, Rect2 Rect)? _starlight;

    /// <summary>Lights <paramref name="item"/> from now on: by the lamps and the stars, with a normal map's <paramref name="relief"/>.</summary>
    public static void Set(CanvasItem item, float share = 1f, bool relief = false)
    {
        item.Material = MaterialOf(share, relief);
        item.LightMask = LampMask | StarMask;
    }

    private static ShaderMaterial MaterialOf(float share, bool relief)
    {
        if (Made.TryGetValue((share, relief), out var kept)) return kept;
        kept = new ShaderMaterial { Shader = Shader };
        kept.SetShaderParameter("share", share);
        kept.SetShaderParameter("relief", relief ? 0.7f : 0f);
        if (_starlight is var (texture, rect)) ShowOn(kept, texture, rect);
        return Made[(share, relief)] = kept;
    }

    /// <summary>The stars' light from now on: <paramref name="texture"/>, a texel a cell, laid over <paramref name="rect"/> in pixels.</summary>
    public static void ShowStarlight(Texture2D texture, Rect2 rect)
    {
        _starlight = (texture, rect);
        foreach (var material in Made.Values) ShowOn(material, texture, rect);
    }

    private static void ShowOn(ShaderMaterial material, Texture2D texture, Rect2 rect)
    {
        material.SetShaderParameter("starlight", texture);
        material.SetShaderParameter("starlight_rect", new Vector4(rect.Position.X, rect.Position.Y, rect.Size.X, rect.Size.Y));
    }
}
