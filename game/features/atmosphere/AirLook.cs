using Godot;

namespace Stillwake.Game.Features.Atmosphere;

/// <summary>
/// How the air is drawn over the rooms, behind what stands in them, a pixel of the world at a time: the
/// lifepod's reach always, a dithered edge breathing with a faint fill; with the overlay on, every room's
/// air in an ordered dither that drifts like a gas, its front lit, blended between cells, red and running
/// to the opening while the room vents, and a sparse grid of grey dots where a room holds none. Reads
/// <see cref="AirField.Write"/>'s texture, a texel a cell, on a sprite scaled a cell a texel.
/// </summary>
public static class AirLook
{
    public static readonly Shader Shader = new()
    {
        Code = """
            shader_type canvas_item;
            render_mode unshaded;
            uniform float cell = 16.0;
            uniform bool overlay = false;
            // The lifepod's reach in pixels: left, top, right, bottom; none while right is not past left.
            uniform vec4 reach = vec4(0.0);

            const float BAYER[16] = float[16](0.0, 8.0, 2.0, 10.0, 12.0, 4.0, 14.0, 6.0, 3.0, 11.0, 1.0, 9.0, 15.0, 7.0, 13.0, 5.0);
            const vec3 AIRED = vec3(0.247, 0.780, 0.627), FRONT = vec3(0.588, 0.941, 0.824), VENTING = vec3(0.878, 0.353, 0.314);
            const vec3 EMPTY = vec3(0.416, 0.455, 0.533), POD = vec3(0.435, 0.827, 1.0), POD_EDGE = vec3(0.745, 0.925, 1.0);

            float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }

            float noise(vec2 p) {
                vec2 i = floor(p), f = fract(p), u = f * f * (3.0 - 2.0 * f);
                float a = hash(i), b = hash(i + vec2(1.0, 0.0)), c = hash(i + vec2(0.0, 1.0)), d = hash(i + vec2(1.0, 1.0));
                return mix(mix(a, b, u.x), mix(c, d, u.x), u.y);
            }

            void fragment() {
                ivec2 size = textureSize(TEXTURE, 0);
                vec2 px = floor(UV * vec2(size) * cell);
                vec4 own = texelFetch(TEXTURE, ivec2(px / cell), 0);
                if (own.a < 0.5) discard;
                ivec2 ip = ivec2(px);
                float threshold = (BAYER[(ip.y % 4) * 4 + ip.x % 4] + 0.5) / 16.0;
                // Wrapped so the noise keeps its precision; 600 s is a whole number of the pod's breaths.
                float t = mod(TIME, 600.0);
                // What this pixel shows, if anything: the pod's reach over the room's air.
                vec4 shown = vec4(0.0);
                bool pod = false;
                if (reach.z > reach.x) {
                    vec2 c = px + 0.5;
                    vec2 d = max(reach.xy - c, c - reach.zw);
                    float sd = (d.x > 0.0 || d.y > 0.0) ? length(max(d, 0.0)) : max(d.x, d.y);
                    if (sd <= 3.0) {
                        float pulse = 0.5 + 0.5 * sin(t / 2.4 * 6.2831853);
                        float n = noise(px * 0.09 + vec2(t * 0.3, -t * 0.5));
                        float inside = clamp(0.5 - sd / 4.0, 0.0, 1.0);
                        bool edge = abs(sd + 1.0) < 1.6;
                        float dens = inside * (overlay ? 0.28 + 0.3 * n : 0.06 + 0.12 * n) + (edge ? 0.3 + 0.3 * pulse : 0.0);
                        if (dens > threshold) shown = vec4(edge ? POD_EDGE : POD, edge ? 0.67 : 0.47);
                        pod = dens > threshold || inside > 0.5;
                    }
                }
                if (pod || !overlay) {
                    if (shown.a == 0.0) discard;
                    COLOR = shown;
                } else {
                    // Blended between the four cells round this pixel, a cell out of every room counting as this one.
                    vec2 g = (px + 0.5) / cell - 0.5, corner = floor(g), f = g - corner;
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
                    if (dens > threshold) {
                        bool front = v.x > 0.12 && v.x < 0.8;
                        COLOR = vec4(mix(front ? FRONT : AIRED, VENTING, v.y), front ? 0.59 : 0.41);
                    } else if (ip.x % 4 == 1 && ip.y % 4 == 1 && v.x < 0.6) {
                        COLOR = vec4(EMPTY, 0.47 * (1.0 - v.x / 0.6));
                    } else {
                        discard;
                    }
                }
            }
            """,
    };
}
