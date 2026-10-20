using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// How light falls on what the ship draws (render/lighting.ts), one model for all of it — cells,
/// machines, heroes, items, debris: every lit picture is drawn with <see cref="Set"/>'s material, its
/// light worked out here, never by Godot's lights (0029). It takes the stars' light from the starlight
/// worked out cell by cell, blended with the neighbouring cells of its own side only — a picture drawn
/// in wall cells (walls, their edges, doors) from wall cells, all else from the open ones, so the back
/// wall beside a door is lit by the room it is in — and the lamps' and fires' from the lightmap at its
/// pixel (<c>features/light/Lightmap.cs</c>), shadowed already. Lights add — a fire's warmth over a
/// lamp's — and where their sum with the stars would pass whole in a colour, as the TypeScript game's
/// <c>min(1, ambient + light)</c> stopped it, the whole of it is scaled down till no colour does, so it
/// keeps its hue: clamping each colour alone would wash a fire's orange over a lamp out to yellow. The
/// glowing particles' spots are added over that, past whole, together at most
/// <see cref="Light.ParticleMost"/> by their brightest colour, so an ember by a fire still lights the
/// wall behind it. A wall takes <see cref="Light.WallIn"/> of what reaches its face. A normal-mapped
/// picture is shaded toward the way the lamps' light comes from — up the main lightmap's slope, tilted
/// 45 degrees up — within that cap; one without a normal map faces the screen and is shaded flat. A
/// picture of debris shows its damage first, as cracks (<see cref="Cracks"/>).
/// </summary>
public static class Lit
{
    // Only debris cracks: instance uniforms take slots of Godot's global buffer for every item drawn with them,
    // which a big ship's thousands of sockets ran out of (each failure logged, a minute to load), so all else has none.
    private static readonly Shader Plain = new() { Code = Head + Cracks.None + Body }, Cracking = new() { Code = Head + Cracks.Code + Body };

    private const string Head = """
            shader_type canvas_item;
            render_mode unshaded;
            // The share of light that falls on it: a wall keeps Light.WallIn of what reaches its face.
            uniform float share = 1.0;
            // Drawn in wall cells, it takes the stars' light from wall cells; all else from the open ones.
            uniform bool blocks = false;
            uniform vec3 ambient = vec3(0.0);
            // How much a normal map shades: 0 flat, 1 as a face turned toward the light 45 degrees up.
            uniform float relief = 0.7;
            // The most the glowing particles' spots add in a colour, past whatever else lights it.
            uniform float spot_most = 0.0;
            // The stars' light, a texel a cell, its alpha whole in a wall's cell, laid over the world at
            // starlight_rect (corner, size, in pixels).
            uniform sampler2D starlight : filter_nearest, repeat_disable, hint_default_black;
            uniform vec4 starlight_rect = vec4(0.0, 0.0, 1.0, 1.0);
            // The lamps' and fires' light, and the spots', shadowed, from lightmap_at (pixels) a texel
            // every lightmap_texel pixels; blended between texels, so a shadow's edge does not step at
            // the world's pixels on a screen showing each of them larger.
            uniform sampler2D lightmap : filter_linear, repeat_disable, hint_default_black;
            uniform sampler2D spotmap : filter_linear, repeat_disable, hint_default_black;
            uniform vec2 lightmap_at = vec2(0.0);
            uniform float lightmap_texel = 1.0;
            varying vec2 world;
            """;

    private const string Body = """

            void vertex() {
                world = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
            }

            vec4 cell(ivec2 at) {
                return texelFetch(starlight, clamp(at, ivec2(0), textureSize(starlight, 0) - 1), 0);
            }

            vec3 mapped(sampler2D map, vec2 at) {
                return texture(map, at / vec2(textureSize(map, 0))).rgb;
            }

            void fragment() {
                // Debris that axes have hit shows it, on its own pixels, before the light falls on it.
                COLOR.rgb = cracked(COLOR.rgb, UV, TEXTURE_PIXEL_SIZE);
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
                vec3 star = weight > 0.0 ? sum / weight : own.rgb;
                vec2 texel = (world - lightmap_at) / lightmap_texel;
                vec3 lamps = mapped(lightmap, texel), spots = mapped(spotmap, texel);
                // Where the lamps' light comes from: up its slope, half a cell each side. Where it hardly
                // changes, or none falls, the picture is shaded as facing it.
                const vec3 LUMA = vec3(0.3, 0.59, 0.11);
                float h = 8.0 / lightmap_texel;
                vec2 slope = vec2(
                    dot(mapped(lightmap, texel + vec2(h, 0.0)) - mapped(lightmap, texel - vec2(h, 0.0)), LUMA),
                    dot(mapped(lightmap, texel + vec2(0.0, h)) - mapped(lightmap, texel - vec2(0.0, h)), LUMA));
                float steep = clamp(length(slope) / max(dot(lamps, LUMA), 0.0001) / 0.06, 0.0, 1.0);
                vec3 toward = normalize(vec3(slope / max(length(slope), 0.0001) * steep, 1.0));
                float facing = clamp(dot(NORMAL, toward) / toward.z, 0.0, 1.5);
                // Where the stars and lamps together pass whole in a colour, all of their light is scaled down
                // till none does: each colour at most whole, and a fire's orange over a lamp stays orange.
                vec3 fits = (1.0 - ambient) / max(star + lamps, vec3(0.0001));
                float scale = min(1.0, min(fits.r, min(fits.g, fits.b)));
                float brightest = max(spots.r, max(spots.g, spots.b));
                vec3 lit = min(star * scale + lamps * scale * mix(1.0, facing, relief), 1.0 - ambient);
                vec3 light = lit + spots * min(1.0, spot_most / max(brightest, 0.0001));
                COLOR.rgb *= ambient + light * share;
            }
            """;

    private static readonly Dictionary<(float Share, bool Cracks), ShaderMaterial> Made = new();
    private static (Texture2D Texture, Rect2 Rect)? _starlight;
    private static (Texture2D Main, Texture2D Spots)? _lightmaps;
    private static (Vector2 At, float Texel) _placed = (Vector2.Zero, 1);

    /// <summary>
    /// Lights <paramref name="item"/> from now on, by the lamps and the stars. One that takes less than
    /// the whole <paramref name="share"/> — a wall's, an edge's, a door's <see cref="Light.WallIn"/> — is
    /// drawn in wall cells and takes the stars' light from its own side. One that <paramref name="cracks"/>,
    /// a piece of debris, shows its damage as <see cref="Cracks.Show"/> sets it.
    /// </summary>
    public static void Set(CanvasItem item, float share = 1f, bool cracks = false) => item.Material = MaterialOf(share, cracks);

    private static Vector3 _ambient = new(Light.Ambient.R, Light.Ambient.G, Light.Ambient.B);

    /// <summary>
    /// The light every lit thing has in the dark, <see cref="Light.Ambient"/> aboard; none on a planet, where unlit ground and
    /// caves are black (0077). Set by the light of the surface drawn, for every lit material.
    /// </summary>
    public static void Ambient((float R, float G, float B) ambient)
    {
        _ambient = new Vector3(ambient.R, ambient.G, ambient.B);
        foreach (var (_, material) in Made) material.SetShaderParameter("ambient", _ambient);
    }

    private static ShaderMaterial MaterialOf(float share, bool cracks)
    {
        if (Made.TryGetValue((share, cracks), out var kept)) return kept;
        kept = new ShaderMaterial { Shader = cracks ? Cracking : Plain };
        kept.SetShaderParameter("share", share);
        kept.SetShaderParameter("blocks", share < 1f);
        kept.SetShaderParameter("ambient", _ambient);
        kept.SetShaderParameter("spot_most", Light.ParticleMost);
        if (_starlight is var (stars, starsAt)) ShowStars(kept, stars, starsAt);
        if (_lightmaps is var (main, spots)) ShowMaps(kept, main, spots);
        Place(kept);
        return Made[(share, cracks)] = kept;
    }

    /// <summary>
    /// The stars' light from now on: <paramref name="texture"/>, a texel a cell, its alpha whole in a
    /// wall's cell and nothing in the open, laid over <paramref name="rect"/> in pixels.
    /// </summary>
    public static void ShowStarlight(Texture2D texture, Rect2 rect)
    {
        _starlight = (texture, rect);
        foreach (var material in Made.Values) ShowStars(material, texture, rect);
    }

    /// <summary>The lamps' and fires' lightmap and the spots', from now on, wherever <see cref="PlaceLightmaps"/> lays them.</summary>
    public static void ShowLightmaps(Texture2D main, Texture2D spots)
    {
        _lightmaps = (main, spots);
        foreach (var material in Made.Values) ShowMaps(material, main, spots);
    }

    /// <summary>Where the lightmaps lie this frame: their corner, in pixels, and how many pixels a texel spans.</summary>
    public static void PlaceLightmaps(Vector2 at, float texel)
    {
        if (_placed == (at, texel)) return;
        _placed = (at, texel);
        foreach (var material in Made.Values) Place(material);
    }

    private static void ShowStars(ShaderMaterial material, Texture2D texture, Rect2 rect)
    {
        material.SetShaderParameter("starlight", texture);
        material.SetShaderParameter("starlight_rect", new Vector4(rect.Position.X, rect.Position.Y, rect.Size.X, rect.Size.Y));
    }

    private static void ShowMaps(ShaderMaterial material, Texture2D main, Texture2D spots)
    {
        material.SetShaderParameter("lightmap", main);
        material.SetShaderParameter("spotmap", spots);
    }

    private static void Place(ShaderMaterial material)
    {
        material.SetShaderParameter("lightmap_at", _placed.At);
        material.SetShaderParameter("lightmap_texel", _placed.Texel);
    }
}
