using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// How light falls on what the ship draws (render/lighting.ts), one model for all of it — cells,
/// machines, heroes, items, debris: every lit picture is drawn with <see cref="Set"/>'s material. It
/// takes the stars' light from the starlight worked out cell by cell, blended with the neighbouring
/// cells of its own side only — a picture drawn in wall cells (walls, their edges, doors) from wall
/// cells, all else from the open ones, so the back wall beside a door is lit by the room it is in —
/// and adds what Godot's lamps give over it. Lights add — a fire's warmth over a lamp's — and where
/// their sum with the stars would pass whole in a colour, as the TypeScript game's
/// <c>min(1, ambient + light)</c> stopped it, the whole of it is scaled down till no colour does, so it
/// keeps its hue: clamping each colour alone would wash a fire's orange over a lamp out to yellow. Each
/// lamp gives its share of that by what it gives of the light all of them pour there
/// (<see cref="ShowLamplight"/>), so a surface by a fire, a lamp and embers is never brighter than its
/// own colour fully lit — but for a glowing particle's spot, added over it up to <see cref="Light.ParticleMost"/>,
/// so an ember by a fire still lights the wall behind it. A normal-mapped picture is shaded toward or away from a lamp within that; a
/// wall takes <see cref="Light.WallIn"/> of what reaches its face.
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
            // Drawn in wall cells, it takes the stars' light from wall cells; all else from the open ones.
            uniform bool blocks = false;
            uniform vec3 ambient = vec3(0.0);
            // The most a glowing particle's spot adds in a colour, past whatever else lights it.
            uniform float spot_most = 0.0;
            // The stars' light, a texel a cell, its alpha whole in a wall's cell, laid over the world at
            // starlight_rect (corner, size, in pixels).
            uniform sampler2D starlight : filter_nearest, repeat_disable, hint_default_black;
            uniform vec4 starlight_rect = vec4(0.0, 0.0, 1.0, 1.0);
            // All the lamps' light summed, unshadowed, a texel a cell, laid at lamplight_rect.
            uniform sampler2D lamplight : filter_linear, repeat_disable, hint_default_black;
            uniform vec4 lamplight_rect = vec4(0.0, 0.0, 1.0, 1.0);
            varying vec2 world;
            varying vec3 star;
            varying vec3 lamps;
            varying float spots;
            varying float scale;

            void vertex() {
                world = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
            }

            vec4 cell(ivec2 at) {
                return texelFetch(starlight, clamp(at, ivec2(0), textureSize(starlight, 0) - 1), 0);
            }

            void fragment() {
                // Blended between the four cells round this point, those of its side only (lighting.ts).
                vec2 at = (world - starlight_rect.xy) / starlight_rect.zw * vec2(textureSize(starlight, 0));
                vec2 corner = floor(at - 0.5);
                vec2 f = at - 0.5 - corner;
                vec4 own = cell(ivec2(floor(at)));
                bool wall = blocks && own.a > 0.5;
                vec3 sum = vec3(0.0);
                float weight = 0.0;
                for (int i = 0; i < 4; i++) {
                    vec2 o = vec2(float(i % 2), float(i / 2));
                    vec4 near = cell(ivec2(corner + o));
                    float w = mix(1.0 - f.x, f.x, o.x) * mix(1.0 - f.y, f.y, o.y);
                    if ((near.a > 0.5) == wall) {
                        sum += near.rgb * w;
                        weight += w;
                    }
                }
                star = weight > 0.0 ? sum / weight : own.rgb;
                vec4 field = texture(lamplight, (world - lamplight_rect.xy) / lamplight_rect.zw);
                lamps = field.rgb;
                spots = field.a;
                // Where the stars and lamps together pass whole in a colour, all of their light is scaled down
                // till none does: each colour at most whole, and a fire's orange over a lamp stays orange.
                vec3 fits = (1.0 - ambient) / max(star + lamps, vec3(0.0001));
                scale = min(1.0, min(fits.r, min(fits.g, fits.b)));
                star *= scale;
            }

            void light() {
                vec3 shine = star;
                float alpha = 1.0;
                if (!LIGHT_IS_DIRECTIONAL) {
                    vec3 l = LIGHT_DIRECTION;
                    // A lamp far off along the floor shades as one 45 degrees up, never as a grazing one.
                    vec3 tilted = normalize(vec3(l.xy, max(l.z, length(l.xy))));
                    float facing = clamp(dot(NORMAL, tilted) / tilted.z, 0.0, 1.5);
                    // Lights add: this lamp gives its share of the sum, scaled to fit, up to what the ambient
                    // and the stars leave.
                    vec3 gives = LIGHT_COLOR.rgb;
                    if (LIGHT_COLOR.a < 0.75) {
                        // A glowing particle's light, marked by its alpha: a spot over what is lit already,
                        // past whole, or an ember by a fire would light nothing.
                        // Its share of all the spots here, together at most spot_most.
                        shine = gives * mix(1.0, facing, relief) * min(1.0, spot_most / max(spots, 0.0001));
                    } else {
                        vec3 all = max(lamps, gives);
                        vec3 room = max(1.0 - ambient - star, 0.0);
                        shine = gives / max(all, vec3(0.0001)) * min(all * scale * mix(1.0, facing, relief), room);
                        alpha = LIGHT_COLOR.a;
                    }
                }
                LIGHT = vec4(shine * COLOR.rgb * share, alpha);
            }
            """,
    };

    private static readonly Dictionary<(float Share, bool Relief), ShaderMaterial> Made = new();
    private static (Texture2D Texture, Rect2 Rect)? _starlight, _lamplight;

    /// <summary>
    /// Lights <paramref name="item"/> from now on: by the lamps and the stars, with a normal map's
    /// <paramref name="relief"/>. One that takes less than the whole <paramref name="share"/> — a wall's,
    /// an edge's, a door's <see cref="Light.WallIn"/> — is drawn in wall cells and takes the stars'
    /// light from its own side.
    /// </summary>
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
        kept.SetShaderParameter("blocks", share < 1f);
        kept.SetShaderParameter("ambient", new Vector3(Light.Ambient.R, Light.Ambient.G, Light.Ambient.B));
        kept.SetShaderParameter("spot_most", Light.ParticleMost);
        if (_starlight is var (stars, starsAt)) ShowOn(kept, "starlight", stars, starsAt);
        if (_lamplight is var (lamps, lampsAt)) ShowOn(kept, "lamplight", lamps, lampsAt);
        return Made[(share, relief)] = kept;
    }

    /// <summary>
    /// The stars' light from now on: <paramref name="texture"/>, a texel a cell, its alpha whole in a
    /// wall's cell and nothing in the open, laid over <paramref name="rect"/> in pixels.
    /// </summary>
    public static void ShowStarlight(Texture2D texture, Rect2 rect)
    {
        _starlight = (texture, rect);
        foreach (var material in Made.Values) ShowOn(material, "starlight", texture, rect);
    }

    /// <summary>
    /// The lamps' light from now on (<c>features/light/LightField.cs</c>): all of it summed, unshadowed,
    /// a texel a cell laid over <paramref name="rect"/> in pixels, kept current by updating it in place.
    /// </summary>
    public static void ShowLamplight(Texture2D texture, Rect2 rect)
    {
        _lamplight = (texture, rect);
        foreach (var material in Made.Values) ShowOn(material, "lamplight", texture, rect);
    }

    private static void ShowOn(ShaderMaterial material, string name, Texture2D texture, Rect2 rect)
    {
        material.SetShaderParameter(name, texture);
        material.SetShaderParameter(name + "_rect", new Vector4(rect.Position.X, rect.Position.Y, rect.Size.X, rect.Size.Y));
    }
}
