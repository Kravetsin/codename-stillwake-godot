using Godot;

namespace Stillwake.Game.Features.Atmosphere;

/// <summary>
/// How the air is drawn over the rooms, behind what stands in them, with the overlay on only (air-wash): a faint cool
/// watercolour wash wherever there is air, granulated and pooled in two tones, with a dried tide line along its edge, so a
/// filling room's front is a line crawling from its vents; nothing where a room holds none. A venting room's wash turns rust,
/// rust streaks running through what air it has left toward its opening. The lifepod's reach is air with an edge of its own.
/// Quiet enough to leave on.
/// Reads <see cref="AirField.Write"/>'s texture, a texel a cell of its region, on a sprite scaled a cell a texel and
/// placed over the region, at `origin`.
/// </summary>
public static class AirLook
{
    public static readonly Shader Shader = new()
    {
        Code = """
            shader_type canvas_item;
            render_mode unshaded;
            uniform float cell = 8.0;
            // Where the texture's first cell lies in the world, in pixels: it covers only what is built.
            uniform vec2 origin = vec2(0.0);
            uniform bool overlay = false;
            // The lifepod's reach in pixels: left, top, right, bottom; none while right is not past left.
            uniform vec4 reach = vec4(0.0);

            const vec3 WASH = vec3(0.60, 0.70, 0.74), RUST = vec3(0.74, 0.35, 0.26);
            // The wash's pigment settles this many pixels a second, one way.
            const vec2 DRIFT = vec2(1.8, 0.9);
            // The edge's wiggles creep slower, small and large ones each their own way, so its shape changes rather than
            // slides; where the air is stays put.
            const vec2 EDGE_DRIFT = vec2(-0.7, 0.5), EDGE_SWELL = vec2(0.35, -0.3);

            // Without sin: it keeps its grain far from the world's origin.
            float hash(vec2 p) {
                vec3 p3 = fract(vec3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return fract((p3.x + p3.y) * p3.z);
            }

            float noise(vec2 p) {
                vec2 i = floor(p), f = fract(p), u = f * f * (3.0 - 2.0 * f);
                return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), u.x), mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), u.x), u.y);
            }

            // TEXTURE is the fragment's only, so it is passed in.
            vec4 cell_at(sampler2D cells, vec2 c, ivec2 size) {
                return texelFetch(cells, clamp(ivec2(c), ivec2(0), size - 1), 0);
            }

            // How far px lies outside the pod's reach, a box with corners rounded by rad; negative inside, far off with no pod.
            float pod_distance(vec2 px, float rad) {
                if (reach.z <= reach.x) return 10000.0;
                vec2 mid = (reach.xy + reach.zw) * 0.5, half_size = (reach.zw - reach.xy) * 0.5;
                vec2 q = abs(px - mid) - half_size + rad;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - rad;
            }

            // Rust streaks running along heading a, in a frame that turns only by sixteenths of a turn: a frame turning
            // with every pixel's heading fans them out where the headings toward an opening converge.
            float streaks(vec2 px, float a, float t) {
                float b = round(a / 0.39269908) * 0.39269908;
                vec2 d = vec2(cos(b), sin(b)), n = vec2(-d.y, d.x);
                return smoothstep(0.70, 0.76, noise(vec2(dot(px, d) * 0.05 - t * 1.4, dot(px, n) * 0.45)));
            }

            void fragment() {
                if (!overlay) discard;
                ivec2 size = textureSize(TEXTURE, 0);
                vec2 local = UV * vec2(size) * cell, px = local + origin;
                vec4 own = cell_at(TEXTURE, floor(local / cell), size);
                if (own.a < 0.5) discard;
                // Wrapped so the noise keeps its precision.
                float t = mod(TIME, 600.0);
                // Blended between the four cells round this pixel, a cell out of every room counting as this one; the
                // flow weighted by how red each is.
                vec2 g = local / cell - 0.5, corner = floor(g), f = g - corner;
                vec2 v = vec2(0.0), flow = vec2(0.0);
                for (int i = 0; i < 4; i++) {
                    vec2 o = vec2(float(i % 2), float(i / 2));
                    vec4 near = cell_at(TEXTURE, corner + o, size);
                    float w = mix(1.0 - f.x, f.x, o.x) * mix(1.0 - f.y, f.y, o.y);
                    v += (near.a > 0.5 ? near.rg : own.rg) * w;
                    float a = near.b * 6.2831853;
                    flow += vec2(cos(a), sin(a)) * near.g * near.a * w;
                }
                // The edge wanders as wet paint does, in two sizes, slowly; the pod's reach is air with an edge of its own.
                // TIME, not t: wrapped at 600 s the shapes would jump every ten minutes; Godot rolls TIME over hourly.
                float wander = (noise((px - EDGE_DRIFT * TIME) * 0.23) - 0.5) * 0.34
                    + (noise((px - EDGE_SWELL * TIME) * 0.05 + 3.1) - 0.5) * 0.3;
                float edge = max(v.x, 0.5 - pod_distance(px, 10.0) / 8.0) + wander;
                float in_air = step(0.5, edge);
                vec2 pigment = px - DRIFT * TIME;
                float blot = step(0.58, noise(pigment * 0.045 + 7.3));
                float grain = 0.7 + 0.6 * noise(pigment * 0.85);
                float tide = in_air * (1.0 - smoothstep(0.08, 0.15, edge - 0.5));
                float alpha = max(in_air * (0.10 + 0.055 * blot) * grain, tide * (0.24 + 0.12 * noise(px * 0.5)));
                vec3 colour = mix(WASH, RUST, v.y);
                // Venting: streaks run toward the opening in what air is left. A room stays venting while open to space,
                // so its airless cells show nothing, as any vacuum: a holed wreck room is no alarm all game.
                if (v.y > 0.01 && v.x > 0.01) {
                    float heading = length(flow) > 0.001 ? atan(flow.y, flow.x) : 3.14159265;
                    float s = v.y * min(v.x * 3.0, 1.0) * streaks(px, heading, t);
                    alpha = max(alpha, s * 0.24 * grain);
                    colour = mix(colour, RUST, s);
                }
                if (alpha < 0.003) discard;
                COLOR = vec4(colour, alpha);
            }
            """,
    };
}
