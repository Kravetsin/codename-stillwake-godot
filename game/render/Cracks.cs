using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// One damage filter for every piece of debris (0034): pixel-art cracks drawn by the lit pictures'
/// shader (<see cref="Lit"/>) on the picture's own pixels, dark lines with a lit pixel below them where
/// the light from above catches the far wall, only where the picture is drawn. They run out from a dent
/// where the blows land, the dent wider and the cracks more and longer as the share of its hit points
/// a piece has lost grows; each piece's seed lays them its own way. Set per picture with
/// <see cref="Show"/>; a picture never set has none and costs a branch.
/// </summary>
public static class Cracks
{
    /// <summary>The shader code <see cref="Lit"/> takes in: <c>cracked(colour, UV, TEXTURE_PIXEL_SIZE)</c>.</summary>
    public const string Code = """
        // How much of its hit points the picture has lost, 0 to 1, and what lays its cracks out (render/Cracks.cs).
        instance uniform float crack = 0.0;
        instance uniform int crack_seed = 0;
        const int CRACKS = 7;
        const int CRACK_STEPS = 40;

        uint crack_hash(uint x) {
            x ^= x >> 16u; x *= 0x7feb352du; x ^= x >> 15u; x *= 0x846ca68bu; x ^= x >> 16u;
            return x;
        }

        float crack_rand(uint seed, uint n) {
            return float(crack_hash(seed * 0x9e3779b9u ^ crack_hash(n)) & 0xffffu) / 65535.0;
        }

        vec3 cracked(vec3 colour, vec2 uv, vec2 texel) {
            if (crack <= 0.0) return colour;
            vec2 size = 1.0 / texel;
            ivec2 here = ivec2(floor(uv * size));
            uint seed = uint(crack_seed);
            // Struck within the middle of the picture; the cracks run out from there in turn.
            vec2 hit = size * (0.3 + 0.4 * vec2(crack_rand(seed, 1u), crack_rand(seed, 2u)));
            float longest = max(size.x, size.y) * 0.5;
            float start = crack_rand(seed, 3u) * 6.2831853;
            // The blow's dent, wider the more it has taken.
            vec2 off = vec2(here) + 0.5 - hit;
            bool dark = dot(off, off) <= crack * crack * 4.0, lit = false;
            for (int i = 0; i < CRACKS; i++) {
                // Crack i shows once that share is lost, and grows to its length over the next 0.3.
                float grown = clamp((crack - float(i) * 0.11) / 0.3, 0.0, 1.0);
                if (grown <= 0.0) break;
                uint n = uint(i) * 16u + 8u;
                float angle = start + float(i) * 2.3999632 + (crack_rand(seed, n) - 0.5) * 0.5;
                int steps = int(longest * (0.45 + 0.55 * crack_rand(seed, n + 1u)) * grown);
                // Later cracks branch off a point along an earlier one.
                vec2 at = hit;
                if (i >= 3) at += vec2(cos(angle + 2.0), sin(angle + 2.0)) * longest * 0.35 * crack_rand(seed, n + 2u);
                for (int s = 0; s < CRACK_STEPS; s++) {
                    if (s >= steps) break;
                    ivec2 p = ivec2(floor(at));
                    if (p == here) dark = true;
                    else if (p + ivec2(0, 1) == here) lit = true;
                    // A crack wanders as it runs.
                    angle += (crack_rand(seed, n + 3u + uint(s)) - 0.5) * 0.9;
                    at += vec2(cos(angle), sin(angle));
                }
            }
            // Dark as the art's own outline, the far wall's edge lit.
            if (dark) return mix(colour, vec3(0.08, 0.09, 0.12), 0.85);
            if (lit) return min(colour * 1.35 + vec3(0.04), vec3(1.0));
            return colour;
        }
        """;

    /// <summary>Shows <paramref name="share"/> of its hit points lost on <paramref name="picture"/>, laid out by <paramref name="seed"/>.</summary>
    public static void Show(CanvasItem picture, float share, int seed)
    {
        picture.SetInstanceShaderParameter("crack", share);
        picture.SetInstanceShaderParameter("crack_seed", seed);
    }
}
