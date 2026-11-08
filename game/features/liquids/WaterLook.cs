using Godot;

namespace Stillwake.Game.Features.Liquids;

/// <summary>
/// How water is drawn (docs/plans/liquids.md), from the air overlay's wash (air-wash, the user's pick): a cool watercolour wash,
/// granulated and pooled in two tones, its pigment drifting slowly along as a current would carry it, deeper and darker the
/// further under the surface; along the surface a pale tide line whose edge wanders as wet paint does. Each cell is filled to
/// its level, as Terraria fills its tiles; falling water is streaked running down. Lit by the cave's lights as the ground is.
/// Reads <see cref="WaterCells"/>' texture, a texel a cell of its box, on a sprite scaled a cell a texel and placed over the
/// box, at <c>origin</c>.
/// </summary>
public static class WaterLook
{
    public static readonly Shader Shader = new()
    {
        Code = """
            shader_type canvas_item;
            uniform float cell = 8.0;
            // Where the texture's first cell lies in the world, in pixels.
            uniform vec2 origin = vec2(0.0);

            const vec3 WASH = vec3(0.34, 0.52, 0.58), DEEP = vec3(0.10, 0.22, 0.28), TIDE = vec3(0.78, 0.88, 0.90);
            // The pigment drifts along this many pixels a second, as a slow current.
            const vec2 DRIFT = vec2(5.0, 0.6);
            // The surface's wiggles creep slower, small and large ones each their own way, so its shape changes rather than slides.
            const vec2 EDGE_DRIFT = vec2(-3.5, 0.0), EDGE_SWELL = vec2(1.75, 0.0);
            // How many cells down the water is darkest.
            const float DARKEST = 6.0;
            // How fast falling water's streaks and grain run down it, in pixels a second.
            const float FLOW = 160.0;

            float hash(vec2 p) {
                vec3 p3 = fract(vec3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return fract((p3.x + p3.y) * p3.z);
            }

            float noise(vec2 p) {
                vec2 i = floor(p), f = fract(p), u = f * f * (3.0 - 2.0 * f);
                return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), u.x), mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), u.x), u.y);
            }

            vec4 cell_at(sampler2D cells, vec2 c, ivec2 size) {
                if (c.x < 0.0 || c.y < 0.0 || c.x >= float(size.x) || c.y >= float(size.y)) return vec4(0.0);
                return texelFetch(cells, ivec2(c), 0);
            }

            void fragment() {
                ivec2 size = textureSize(TEXTURE, 0);
                vec2 local = UV * vec2(size) * cell, px = local + origin;
                vec2 c = floor(local / cell), f = local / cell - c;
                vec4 own = cell_at(TEXTURE, c, size);
                if (own.a < 0.5) discard;
                // The surface across this cell, blended with the neighbour's toward the side it is nearer, so a run reads flat;
                // a cell with water over it is full.
                vec4 beside = cell_at(TEXTURE, c + vec2(f.x < 0.5 ? -1.0 : 1.0, 0.0), size);
                bool covered = own.b > 0.5;
                float surface = covered ? 1.0 : mix(own.r, beside.a > 0.5 ? beside.r : own.r, abs(f.x - 0.5));
                // The edge wanders as wet paint does, in two sizes, slowly.
                float wander = covered ? 0.0 : (noise((px - EDGE_DRIFT * TIME) * 0.23) - 0.5) * 0.14
                    + (noise((px - EDGE_SWELL * TIME) * 0.05 + 3.1) - 0.5) * 0.12;
                float under = (surface + wander - (1.0 - f.y)) * cell;
                if (under < 0.0) discard;
                // How deep: this cell's part and every wet cell over it, a few at most.
                float above = 0.0;
                for (int i = 1; i <= int(DARKEST); i++) {
                    if (cell_at(TEXTURE, c - vec2(0.0, float(i)), size).a < 0.5) break;
                    above += 1.0;
                }
                float deep = clamp((under + above * cell) / (DARKEST * cell), 0.0, 1.0);
                vec2 pigment = px - DRIFT * TIME;
                float grain = 0.7 + 0.6 * noise(pigment * 0.85);
                float blot = step(0.58, noise(pigment * 0.045 + 7.3));
                float alpha = (0.34 + 0.34 * deep + 0.08 * blot) * grain;
                vec3 colour = mix(WASH, DEEP, deep);
                if (own.g > 0.5) {
                    // Falling: long pale streaks and grain carried down with it.
                    float y = px.y - mod(TIME, 600.0) * FLOW;
                    float streak = smoothstep(0.5, 0.75, noise(vec2(px.x * 0.6, y * 0.04)));
                    colour = mix(WASH, TIDE, streak * 0.6);
                    alpha = (0.5 + 0.25 * streak) * (0.75 + 0.5 * noise(vec2(px.x * 0.85, y * 0.1)));
                }
                float tide = above > 0.0 ? 0.0 : 1.0 - smoothstep(0.6, 1.6, under);
                alpha = max(alpha, tide * (0.62 + 0.2 * noise(px * 0.5)));
                colour = mix(colour, TIDE, tide * 0.8);
                COLOR = vec4(colour, clamp(alpha, 0.0, 0.92));
            }
            """,
    };
}
