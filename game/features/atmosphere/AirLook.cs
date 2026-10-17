using Godot;

namespace Stillwake.Game.Features.Atmosphere;

/// <summary>
/// How the air is drawn over the rooms, behind what stands in them, a pixel of the world at a time, with
/// the overlay on only: every room's air in an ordered dither that drifts like a gas, its front lit,
/// blended between cells, red and running to the opening while the room vents, a sparse grid of grey dots
/// where a room holds none; and round the lifepod an aura of its air, a ragged blob about its reach whose
/// edge wavers and thins out, never a box. Reads <see cref="AirField.Write"/>'s texture, a texel a cell of its region,
/// on a sprite scaled a cell a texel and placed over the region, at `origin`.
/// </summary>
public static class AirLook
{
    public static readonly Shader Shader = new()
    {
        Code = """
            shader_type canvas_item;
            render_mode unshaded;
            uniform float cell = 16.0;
            // Where the texture's first cell lies in the world, in pixels: it covers only what is built.
            uniform vec2 origin = vec2(0.0);
            uniform bool overlay = false;
            // The lifepod's reach in pixels: left, top, right, bottom; none while right is not past left.
            uniform vec4 reach = vec4(0.0);

            const float BAYER[16] = float[16](0.0, 8.0, 2.0, 10.0, 12.0, 4.0, 14.0, 6.0, 3.0, 11.0, 1.0, 9.0, 15.0, 7.0, 13.0, 5.0);
            const vec3 AIRED = vec3(0.247, 0.780, 0.627), FRONT = vec3(0.588, 0.941, 0.824), VENTING = vec3(0.878, 0.353, 0.314);
            const vec3 EMPTY = vec3(0.416, 0.455, 0.533), POD = vec3(0.435, 0.827, 1.0), POD_WISP = vec3(0.745, 0.925, 1.0);

            float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }

            float noise(vec2 p) {
                vec2 i = floor(p), f = fract(p), u = f * f * (3.0 - 2.0 * f);
                float a = hash(i), b = hash(i + vec2(1.0, 0.0)), c = hash(i + vec2(0.0, 1.0)), d = hash(i + vec2(1.0, 1.0));
                return mix(mix(a, b, u.x), mix(c, d, u.x), u.y);
            }

            void fragment() {
                if (!overlay) discard;
                ivec2 size = textureSize(TEXTURE, 0);
                vec2 local = floor(UV * vec2(size) * cell), px = local + origin;
                vec4 own = texelFetch(TEXTURE, ivec2(local / cell), 0);
                if (own.a < 0.5) discard;
                ivec2 ip = ivec2(px);
                float threshold = (BAYER[(ip.y % 4) * 4 + ip.x % 4] + 0.5) / 16.0;
                // Wrapped so the noise keeps its precision.
                float t = mod(TIME, 600.0);
                // The pod's aura: 1 at a rounded box about its reach, wavering in big lobes and torn at
                // its edge; thick inside, thinning out over the last part.
                float aura = 0.0;
                float wisp = 0.0;
                if (reach.z > reach.x) {
                    vec2 mid = (reach.xy + reach.zw) * 0.5, half_size = (reach.zw - reach.xy) * 0.5;
                    vec2 p = (px + 0.5 - mid) / half_size;
                    float r = pow(pow(abs(p.x), 3.0) + pow(abs(p.y), 3.0), 1.0 / 3.0);
                    vec2 dir = p / max(length(p), 0.001);
                    float lobes = (noise(dir * 1.8 + vec2(t * 0.25, -t * 0.18)) - 0.5) * 0.45;
                    float torn = (noise(px * 0.16 + vec2(t * 0.9, -t * 0.6)) - 0.5) * 0.3;
                    aura = clamp((1.0 - (r + lobes + torn)) / 0.45, 0.0, 1.0);
                    wisp = noise(px * 0.09 + vec2(t * 0.3, -t * 0.5));
                }
                // Blended between the four cells round this pixel, a cell out of every room counting as this one.
                vec2 g = (local + 0.5) / cell - 0.5, corner = floor(g), f = g - corner;
                vec2 v = vec2(0.0);
                for (int i = 0; i < 4; i++) {
                    vec2 o = vec2(float(i % 2), float(i / 2));
                    vec4 near = texelFetch(TEXTURE, clamp(ivec2(corner + o), ivec2(0), size - 1), 0);
                    float w = mix(1.0 - f.x, f.x, o.x) * mix(1.0 - f.y, f.y, o.y);
                    v += (near.a > 0.5 ? near.rg : own.rg) * w;
                }
                vec2 q = px * 0.07;
                if (own.g > 0.5) {
                    float a = own.b * 6.2831853;
                    q -= vec2(cos(a), sin(a)) * t * 4.0;
                } else {
                    q += vec2(0.25, 0.4) * t;
                }
                float dens = v.x * (0.32 + 0.38 * noise(q));
                float pod = aura * (0.22 + 0.4 * wisp);
                if (pod > threshold && pod >= dens) {
                    COLOR = vec4(wisp > 0.7 ? POD_WISP : POD, 0.35 + 0.25 * aura);
                } else if (dens > threshold) {
                    bool front = v.x > 0.12 && v.x < 0.8;
                    COLOR = vec4(mix(front ? FRONT : AIRED, VENTING, v.y), front ? 0.59 : 0.41);
                } else if (aura < 0.2 && ip.x % 4 == 1 && ip.y % 4 == 1 && v.x < 0.6) {
                    COLOR = vec4(EMPTY, 0.47 * (1.0 - v.x / 0.6));
                } else {
                    discard;
                }
            }
            """,
    };
}
