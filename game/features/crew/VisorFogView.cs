using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The fogged visor (2026-11-05-the-last-breath), as <see cref="VisorFog"/> says: one full-screen quad over the world and the
/// frost, under the HUD, reading the screen under it: grey condensation in from the corners, ragged, a brighter line at its
/// front, the world blurred under it, beads in the corners; evaporating in patches, the beads last. Hidden while clear.
/// </summary>
public partial class VisorFogView : CanvasLayer
{
    private static readonly StringName Reach = "reach", Go = "go";

    private const string Shader = """
        shader_type canvas_item;
        uniform sampler2D screen : hint_screen_texture, filter_linear_mipmap;
        uniform float reach = 0.0;
        uniform float go = 0.0;
        const vec3 fog = vec3(204.0, 213.0, 219.0) / 255.0;
        const vec3 bead = vec3(214.0, 222.0, 228.0) / 255.0;
        const vec3 glint = vec3(245.0, 250.0, 252.0) / 255.0;
        const vec3 ink = vec3(23.0, 18.0, 15.0) / 255.0;

        // Dave Hoskins' hash without sine, as the frost's.
        float hash(vec2 p) {
            vec3 p3 = fract(vec3(p.xyx) * 0.1031);
            p3 += dot(p3, p3.yzx + 33.33);
            return fract((p3.x + p3.y) * p3.z);
        }
        float noise(vec2 p) {
            vec2 i = floor(p), f = fract(p);
            vec2 u = f * f * (3.0 - 2.0 * f);
            return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), u.x), mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), u.x), u.y);
        }
        float fbm(vec2 p) {
            float sum = 0.0, amp = 0.5;
            for (int i = 0; i < 4; i++) { sum += amp * noise(p); p = p * 2.03 + 17.0; amp *= 0.5; }
            return sum / 0.9375;
        }
        // 0 at the screen's corners, 1 at its middle, ragged like paint; q is the screen in heights.
        float field(vec2 uv, vec2 q) {
            float f = 1.0 - length((uv - 0.5) / 0.62) + (fbm(q * 7.5) - 0.5) * 0.32 + (fbm(q * 22.0 + 5.0) - 0.5) * 0.08;
            return clamp((f + 0.39) / 1.49, 0.0, 1.0);
        }

        void fragment() {
            vec2 size = 1.0 / SCREEN_PIXEL_SIZE;
            float aspect = size.x / size.y;
            vec2 q = UV * vec2(aspect, 1.0);
            // The look was drawn on a picture 288 pixels high: its pixels, here.
            vec2 p = q * 288.0;
            float grain = clamp((noise(p / 3.0) * 0.35 + fbm(p / 9.0) * 0.65 - 0.15) / 0.7, 0.0, 1.0);
            float front = reach - field(UV, q);
            float a = 0.0;
            if (front > 0.0) {
                a = 0.16 + 0.42 * smoothstep(0.0, 0.22, front);
                if (front < 0.016) a += 0.16;
                a *= 0.8 + (grain - 0.5) * 0.5;
            } else if (front > -0.03) a = 0.07 * (1.0 + front / 0.03);
            // Evaporating in patches: the thinnest grain first.
            float ev = go * 1.1;
            if (go > 0.0) a *= smoothstep(ev - 0.08, ev + 0.04, grain);
            a = clamp(a, 0.0, 1.0);
            vec3 world = texture(screen, SCREEN_UV).rgb;
            vec3 blurred = textureLod(screen, SCREEN_UV, log2(size.y / 72.0)).rgb;
            vec3 colour = mix(mix(world, blurred, a), fog, a);
            // A bead in some cells of a grid near the corners: a pale body, a shaded underside, one bright point; drying last.
            vec2 cell = floor(p / 24.0), at = p - cell * 24.0;
            if (hash(cell + 71.0) < 0.45) {
                float rad = 1.2 + hash(cell + 3.0) * 1.6;
                vec2 c = rad + 1.0 + vec2(hash(cell + 11.0), hash(cell + 29.0)) * (22.0 - 2.0 * rad);
                vec2 cq = (cell * 24.0 + c) / 288.0;
                float th = field(cq / vec2(aspect, 1.0), cq);
                float dry = 0.7 + hash(cell + 47.0) * 0.28;
                float shown = th < 0.3 ? smoothstep(th + 0.05, th + 0.11, reach) * (1.0 - smoothstep(dry, dry + 0.1, go)) : 0.0;
                vec2 d = at - c;
                float r = length(d), px = 288.0 / size.y;
                if (shown > 0.01 && r < rad + px) {
                    float inside = 1.0 - smoothstep(rad - px, rad, r);
                    colour = mix(colour, bead, 0.32 * inside * shown);
                    float under = (1.0 - smoothstep(0.3, 0.5, abs(r - rad))) * step(0.2 * rad, d.y);
                    colour = mix(colour, ink, 0.35 * under * shown);
                    vec2 g = d + rad * vec2(0.45, 0.5);
                    if (g.x >= 0.0 && g.x < 1.0 && g.y >= 0.0 && g.y < 1.0) colour = mix(colour, glint, 0.85 * shown);
                }
            }
            COLOR = vec4(colour, 1.0);
        }
        """;

    private readonly ColorRect _screen = new();
    private readonly ShaderMaterial _material = new() { Shader = new Shader { Code = Shader } };

    public VisorFogView()
    {
        Layer = ScreenLayers.VisorFog;
        _screen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _screen.MouseFilter = Control.MouseFilterEnum.Ignore;
        _screen.Material = _material;
        _screen.Visible = false;
        AddChild(_screen);
    }

    /// <summary>Shows <paramref name="look"/>: the suit gauge hands it on each frame.</summary>
    public void Show(FogLook look)
    {
        _screen.Visible = look.Reach > 0;
        if (!_screen.Visible) return;
        _material.SetShaderParameter(Reach, (float)look.Reach);
        _material.SetShaderParameter(Go, (float)look.Go);
    }
}
