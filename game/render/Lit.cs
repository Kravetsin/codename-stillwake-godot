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
/// 45 degrees up — within that cap; one without a normal map faces the screen and is shaded flat. A vein's ore on the walls' layer
/// shines past whole in the lamps' and spots' light, not the stars', by the atlas's specular mask (<see cref="Light.OreShine"/>).
/// </summary>
public static class Lit
{
    private static readonly Shader Plain = new() { Code = Head + Vertex + Body };

    /// <summary>The same light on what bends as a body brushes it (plant-stirs): moved by <see cref="Bend"/>'s stirs before it is lit.</summary>
    private static readonly Shader Bending = new() { Code = Head + Bend.Code + Bent + Body };

    private const string Vertex = """

            void vertex() {
                world = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
            }
            """;

    private const string Bent = """

            void vertex() {
                world = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
                vec2 by = stirred(world, VERTEX_ID == 0 || VERTEX_ID == 3);
                VERTEX += by;
                world += by;
            }
            """;

    private const string Head = """
            shader_type canvas_item;
            render_mode unshaded;
            // The share of light that falls on it: a wall keeps Light.WallIn of what reaches its face.
            uniform float share = 1.0;
            // Drawn in wall cells, it takes the stars' light from wall cells; all else from the open ones.
            uniform bool blocks = false;
            uniform vec3 ambient = vec3(0.0);
            // The daylight of the moment on a planet (Daylight.Sky): what the stars' light, baked at the sky's whole, is multiplied by.
            uniform vec3 sky = vec3(1.0);
            // How much a normal map shades: 0 flat, 1 as a face turned toward the light 45 degrees up.
            uniform float relief = 0.7;
            // The most the glowing particles' spots add in a colour, past whatever else lights it.
            uniform float spot_most = 0.0;
            // The stars' light, a texel a cell, its alpha whole in a wall's cell, laid over the world at
            // starlight_rect (corner, size, in pixels), starlight_cells across and down, cut into tiles of
            // starlight_tile cells a side, a layer each, row by row (StarTiles).
            uniform sampler2DArray starlight : filter_nearest, repeat_disable, hint_default_black;
            uniform vec4 starlight_rect = vec4(0.0, 0.0, 1.0, 1.0);
            uniform ivec2 starlight_cells = ivec2(1);
            uniform int starlight_tile = 1;
            // The lamps' and fires' light, and the spots', shadowed, from lightmap_at (pixels) a texel
            // every lightmap_texel pixels; blended between texels, so a shadow's edge does not step at
            // the world's pixels on a screen showing each of them larger.
            uniform sampler2D lightmap : filter_linear, repeat_disable, hint_default_black;
            uniform sampler2D spotmap : filter_linear, repeat_disable, hint_default_black;
            uniform vec2 lightmap_at = vec2(0.0);
            uniform float lightmap_texel = 1.0;
            // The walls round see_at (pixels) fade to see_least within see_radius, so the back wall behind shows (back-focus-faint).
            uniform vec2 see_at = vec2(0.0);
            uniform float see_radius = 0.0;
            uniform float see_least = 0.35;
            // A bare picture (the back walls) is drawn as painted, out of the light, as near see_at as see_bare has it (back-focus-faint).
            uniform bool bare = false;
            uniform float see_bare = 0.0;
            // Ore shines in the lamps' light by its atlas's specular mask, past whole (Light.OreShine); only the walls' layer asks.
            uniform float ore_shine = 0.0;
            varying vec2 world;
            """;

    private const string Body = """

            vec4 cell(ivec2 at) {
                ivec2 c = clamp(at, ivec2(0), starlight_cells - 1);
                int across = (starlight_cells.x + starlight_tile - 1) / starlight_tile;
                return texelFetch(starlight, ivec3(c % starlight_tile, c.y / starlight_tile * across + c.x / starlight_tile), 0);
            }

            vec3 mapped(sampler2D map, vec2 at) {
                return texture(map, at / vec2(textureSize(map, 0))).rgb;
            }

            bool walled(vec2 texel) {
                vec2 w = texel * lightmap_texel + lightmap_at;
                return cell(ivec2(floor((w - starlight_rect.xy) / starlight_rect.zw * vec2(starlight_cells)))).a > 0.5;
            }
            // The lamps' light at texel when both texels blended there, blend either way, lie on the pixel's side; else here's.
            // The lamps' light at texel when both texels blended there along blend lie on the pixel's side; else here's.
            float own(vec2 texel, vec2 blend, bool wall, float here, vec3 luma) {
                return walled(texel - blend) == wall && walled(texel + blend) == wall ? dot(mapped(lightmap, texel), luma) : here;
            }

            // The lamps' light across 2 * step about texel, a sample reaching a cell of the other side taken as here's.
            float sided(vec2 texel, vec2 step, bool wall, float here, vec3 luma) {
                vec2 blend = normalize(step) * 0.5;
                return own(texel + step, blend, wall, here, luma) - own(texel - step, blend, wall, here, luma);
            }

            void fragment() {
                // Blended between the four cells round this point, those of its side only (lighting.ts).
                vec2 at = (world - starlight_rect.xy) / starlight_rect.zw * vec2(starlight_cells);
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
                vec3 star = (weight > 0.0 ? sum / weight : own.rgb) * sky;
                vec2 texel = (world - lightmap_at) / lightmap_texel;
                vec3 lamps = mapped(lightmap, texel), spots = mapped(spotmap, texel);
                // Where the lamps' light comes from: up its slope, a cell each side. Where it hardly
                // changes, or none falls, the picture is shaded as facing it.
                const vec3 LUMA = vec3(0.3, 0.59, 0.11);
                float h = 8.0 / lightmap_texel;
                // Read on its own side only: past a deck, the light of the room below is not light coming at it.
                float here = dot(lamps, LUMA);
                vec2 slope = vec2(
                    sided(texel, vec2(h, 0.0), wall, here, LUMA),
                    sided(texel, vec2(0.0, h), wall, here, LUMA));
                float steep = clamp(length(slope) / max(here, 0.0001) / 0.06, 0.0, 1.0);
                vec3 toward = normalize(vec3(slope / max(length(slope), 0.0001) * steep, 1.0));
                float facing = clamp(dot(NORMAL, toward) / toward.z, 0.0, 1.5);
                // Where the stars and lamps together pass whole in a colour, all of their light is scaled down
                // till none does: each colour at most whole, and a fire's orange over a lamp stays orange.
                vec3 fits = (1.0 - ambient) / max(star + lamps, vec3(0.0001));
                float scale = min(1.0, min(fits.r, min(fits.g, fits.b)));
                float brightest = max(spots.r, max(spots.g, spots.b));
                vec3 lit = min(star * scale + lamps * scale * mix(1.0, facing, relief), 1.0 - ambient);
                vec3 spotted = spots * min(1.0, spot_most / max(brightest, 0.0001));
                vec3 light = lit + spotted;
                // Lamps, fires and glows only, never the stars: unlit ore stays as dark as its rock.
                vec3 shine = SPECULAR_SHININESS.rgb * dot(COLOR.rgb, LUMA) * (lamps + spotted) * share * ore_shine;
                vec3 painted = COLOR.rgb;
                COLOR.rgb = COLOR.rgb * (ambient + light * share) + shine;
                if (blocks && see_radius > 0.0)
                    COLOR.a *= mix(see_least, 1.0, smoothstep(see_radius * 0.5, see_radius, distance(world, see_at)));
                if (bare && see_radius > 0.0)
                    COLOR.rgb = mix(COLOR.rgb, painted, see_bare * (1.0 - smoothstep(see_radius * 0.5, see_radius, distance(world, see_at))));
            }
            """;

    private static readonly Dictionary<(float Share, bool Shines, bool Bends, bool Windy, bool Bare), ShaderMaterial> Made = new();
    private static (Texture2DArray Tiles, Rect2 Rect, Vector2I Cells, int Tile)? _starlight;
    private static (Texture2D Main, Texture2D Spots)? _lightmaps;
    private static (Vector2 At, float Texel) _placed = (Vector2.Zero, 1);
    private static Vector2? _seeThrough;

    /// <summary>
    /// Fades the walls, edges and doors within <paramref name="radius"/> of <paramref name="at"/>, in pixels of the world, to
    /// <paramref name="least"/>, the back walls there drawn as painted, out of the light (back-focus-faint); null for none.
    /// Set every frame, sent only when it changes.
    /// </summary>
    public static void SeeThrough(Vector2? at, float least, float radius)
    {
        if (at == _seeThrough && least == _seeLeast && radius == _seeRadius) return;
        (_seeThrough, _seeLeast, _seeRadius) = (at, least, radius);
        foreach (var material in Every) See(material);
    }

    private static float _seeLeast, _seeRadius;

    private static void See(ShaderMaterial material)
    {
        material.SetShaderParameter("see_at", _seeThrough ?? Vector2.Zero);
        material.SetShaderParameter("see_radius", _seeThrough == null ? 0f : _seeRadius);
        material.SetShaderParameter("see_least", _seeLeast);
        material.SetShaderParameter("see_bare", _seeThrough == null ? 0f : 1f);
    }

    /// <summary>
    /// Lights <paramref name="item"/> from now on, by the lamps and the stars. One that takes less than
    /// the whole <paramref name="share"/> — a wall's, an edge's, a door's <see cref="Light.WallIn"/> — is
    /// drawn in wall cells and takes the stars' light from its own side. One that <paramref name="shines"/> (the walls' layer) has its ore
    /// shine in the lamps' light, by its texture's specular mask (<see cref="Light.OreShine"/>); others have none.
    /// </summary>
    public static void Set(CanvasItem item, float share = 1f, bool shines = false) => item.Material = MaterialOf(share, shines, false, false, false);

    /// <summary>
    /// Lights <paramref name="item"/> as <see cref="Set"/> does, and bends it as bodies brush it (<see cref="Bend"/>, plant-stirs):
    /// grass, tufts, moss; <paramref name="windy"/> for what stands in the open, which a gust leans too.
    /// </summary>
    public static void SetBending(CanvasItem item, bool windy) => item.Material = MaterialOf(1f, false, true, windy, false);

    /// <summary>Lights <paramref name="item"/> as <see cref="Set"/> does, drawn as painted round the pointer while <see cref="SeeThrough"/> has it bare: the back walls (back-focus-faint).</summary>
    public static void SetBare(CanvasItem item) => item.Material = MaterialOf(1f, false, false, false, true);

    private static Vector3 _ambient = new(Light.Ambient.R, Light.Ambient.G, Light.Ambient.B);

    /// <summary>
    /// The light every lit thing has in the dark, <see cref="Light.Ambient"/> aboard; none on a planet, where unlit ground and
    /// caves are black (0077). Set by the light of the surface drawn, for every lit material.
    /// </summary>
    public static void Ambient((float R, float G, float B) ambient)
    {
        _ambient = new Vector3(ambient.R, ambient.G, ambient.B);
        foreach (var material in Every) material.SetShaderParameter("ambient", _ambient);
    }

    private static Vector3 _sky = Vector3.One;

    /// <summary>
    /// The daylight of the moment on a planet (<see cref="Daylight.Sky"/>, 2026-11-07-night-chills): what the light through the
    /// open sky is multiplied by, its starlight baked at the sky's whole; 1 aboard. Lamps, fires and glows are untouched.
    /// </summary>
    public static void Sky((float R, float G, float B) sky)
    {
        var now = new Vector3(sky.R, sky.G, sky.B);
        if (now == _sky) return;
        _sky = now;
        foreach (var material in Every) material.SetShaderParameter("sky", _sky);
    }

    /// <summary>Every material made here, kept lit as the world's light changes: <see cref="Made"/>'s and each <see cref="Painted"/>.</summary>
    private static readonly List<ShaderMaterial> Every = [];

    /// <summary>
    /// A material of its own lit as <see cref="Set"/>'s with <paramref name="share"/>, its picture's colour first changed by
    /// <paramref name="paint"/>, statements run at the start of the fragment on <c>COLOR</c>, beside <paramref name="declarations"/>
    /// (uniforms, varyings) and a <paramref name="vertex"/> function of its own, which must set <c>world</c> as the plain one does: the
    /// frost on the hull (stretches), grown and melted in its shader; <paramref name="after"/>, statements run once it is lit, for what
    /// glows past the light (a suit's embers, <c>SuitFxView</c>). Made once by its user, never freed.
    /// </summary>
    public static ShaderMaterial Painted(string declarations, string vertex, string paint, float share, string after = "")
    {
        var body = Body.Replace("void fragment() {", "void fragment() {\n" + paint + "\n", System.StringComparison.Ordinal);
        if (body == Body) throw new System.InvalidOperationException("Lit's fragment moved: nowhere to paint");
        int end = body.LastIndexOf('}');
        body = body[..end] + after + "\n" + body[end..];
        var made = new ShaderMaterial { Shader = new Shader { Code = Head + declarations + vertex + body } };
        return Prepared(made, share, false, false);
    }

    /// <summary>Another material with <paramref name="painted"/>'s shader (<see cref="Painted"/>), lit with <paramref name="share"/>: its own parameters, one shader compiled. Never freed.</summary>
    public static ShaderMaterial Like(ShaderMaterial painted, float share) => Prepared(new ShaderMaterial { Shader = painted.Shader }, share, false, false);

    private static ShaderMaterial MaterialOf(float share, bool shines, bool bends, bool windy, bool bare)
    {
        if (Made.TryGetValue((share, shines, bends, windy, bare), out var kept)) return kept;
        kept = new ShaderMaterial { Shader = bends ? Bending : Plain };
        if (bends) Bend.Add(kept, windy);
        return Made[(share, shines, bends, windy, bare)] = Prepared(kept, share, shines, bare);
    }

    /// <summary><paramref name="kept"/> given the world's light as it is now, and kept so from now on.</summary>
    private static ShaderMaterial Prepared(ShaderMaterial kept, float share, bool shines, bool bare)
    {
        kept.SetShaderParameter("share", share);
        kept.SetShaderParameter("blocks", share < 1f);
        kept.SetShaderParameter("bare", bare);
        kept.SetShaderParameter("ambient", _ambient);
        kept.SetShaderParameter("sky", _sky);
        kept.SetShaderParameter("spot_most", Light.ParticleMost);
        kept.SetShaderParameter("ore_shine", shines ? Light.OreShine : 0f);
        if (_starlight is { } stars) ShowStars(kept, stars);
        if (_lightmaps is var (main, spots)) ShowMaps(kept, main, spots);
        Place(kept);
        See(kept);
        Every.Add(kept);
        return kept;
    }

    /// <summary>
    /// The stars' light from now on: <paramref name="tiles"/>, a texel a cell, its alpha whole in a
    /// wall's cell and nothing in the open, laid over <paramref name="rect"/> in pixels, <paramref name="cells"/> across and
    /// down, cut into square tiles of <paramref name="tile"/> cells, a layer each, row by row (<c>StarTiles</c>).
    /// </summary>
    public static void ShowStarlight(Texture2DArray tiles, Rect2 rect, Vector2I cells, int tile)
    {
        _starlight = (tiles, rect, cells, tile);
        foreach (var material in Every) ShowStars(material, _starlight.Value);
    }

    /// <summary>The lamps' and fires' lightmap and the spots', from now on, wherever <see cref="PlaceLightmaps"/> lays them.</summary>
    public static void ShowLightmaps(Texture2D main, Texture2D spots)
    {
        _lightmaps = (main, spots);
        foreach (var material in Every) ShowMaps(material, main, spots);
    }

    /// <summary>Where the lightmaps lie this frame: their corner, in pixels, and how many pixels a texel spans.</summary>
    public static void PlaceLightmaps(Vector2 at, float texel)
    {
        if (_placed == (at, texel)) return;
        _placed = (at, texel);
        foreach (var material in Every) Place(material);
    }

    private static void ShowStars(ShaderMaterial material, (Texture2DArray Tiles, Rect2 Rect, Vector2I Cells, int Tile) stars)
    {
        material.SetShaderParameter("starlight", stars.Tiles);
        material.SetShaderParameter("starlight_rect", new Vector4(stars.Rect.Position.X, stars.Rect.Position.Y, stars.Rect.Size.X, stars.Rect.Size.Y));
        material.SetShaderParameter("starlight_cells", stars.Cells);
        material.SetShaderParameter("starlight_tile", stars.Tile);
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
