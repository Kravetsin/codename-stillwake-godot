using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// What bends as bodies brush it (plant-stirs): grass, tufts and a cave's floor moss, drawn with a material whose
/// <c>vertex()</c> moves each vertex by the <see cref="Stirs"/> pushed here each frame — by its height over a point's ground
/// (none at or under it, whole a blade's height up, none two blades up) and across a cell either side of it. A quad's top
/// leans whole by its middle, so its blades bend as a clump rather than stretch; its tips drop along their arc. Lit pictures
/// bend through <see cref="Lit.SetBending"/>; a glow added over them through <see cref="Glow"/>.
/// </summary>
public static class Bend
{
    /// <summary>The stirs' uniforms and <c>stirred(world)</c>, the push at a point of the world, for a shader's <c>vertex()</c>.</summary>
    public const string Code = """

            // Each point a body stirs: (x, its ground's y, the push at a blade's tip), in pixels (Stirs).
            uniform vec4 stirs[128];
            uniform int stir_count = 0;
            // The wind: how far a gust leans a tip now, signed, in pixels, rolling along the ground by wind_roll (Gusts.At); only
            // what stands in the open (windy) and only its tips, the top corners of its quad.
            uniform float wind = 0.0;
            uniform float wind_roll = 0.0;
            uniform float windy = 0.0;

            // The push on vertex `corner` (VERTEX_ID: 0 top left, 3 top right) of a quad at `at` in the world. A quad's top leans
            // whole, both corners by its middle a cell in, so its blades bend as a clump and are never stretched; each clump a
            // little stiffer or softer than the next; a bent tip drops along its arc, a 16 px blade by lean² / 32.
            vec2 stirred(vec2 at, int corner) {
                bool tip = corner == 0 || corner == 3;
                float x = at.x + (corner == 0 ? 4.0 : (corner == 3 ? -4.0 : 0.0));
                float by = 0.0;
                for (int i = 0; i < stir_count; i++) {
                    vec4 s = stirs[i];
                    float across = 1.0 - abs(x - s.x) / 8.0;
                    if (across <= 0.0) continue;
                    float up = s.y - at.y;
                    by += s.z * across * clamp(up / 16.0, 0.0, 1.0) * (1.0 - smoothstep(16.0, 32.0, up));
                }
                float stiff = fract(sin(floor(x / 8.0) * 12.9898) * 43758.5453);
                by *= 0.7 + 0.6 * stiff;
                if (tip) by += windy * wind * (0.8 + 0.2 * sin(x * 0.045 - wind_roll) + 0.15 * sin(wind_roll * 2.0 + stiff * 40.0));
                by = clamp(by, -8.0, 8.0);
                return vec2(by, by * by / 32.0);
            }
            """;

    private static readonly List<ShaderMaterial> Materials = [];
    private static readonly Vector4[] Shown = new Vector4[Stirs.Most];
    private static readonly (float X, float Ground, float Offset)[] Written = new (float, float, float)[Stirs.Most];
    private static int _count;
    private static (float Lean, float Roll) _wind;

    /// <summary>A glow added unlit over what bends, bending with it: a cave's floor moss's.</summary>
    public static readonly ShaderMaterial Glow = Made(windy: false, new ShaderMaterial
    {
        Shader = new Shader
        {
            Code = "shader_type canvas_item;\nrender_mode blend_add, unshaded;\n" + Code + """

                void vertex() {
                    VERTEX += stirred((MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy, VERTEX_ID);
                }
                """,
        },
    });

    private static ShaderMaterial Made(bool windy, ShaderMaterial material)
    {
        Add(material, windy);
        return material;
    }

    /// <summary>
    /// Bends <paramref name="material"/> from now on: its shader holds <see cref="Code"/>; <paramref name="windy"/> for what stands in
    /// the open, which a gust leans too, never a cave's moss.
    /// </summary>
    public static void Add(ShaderMaterial material, bool windy)
    {
        material.SetShaderParameter("windy", windy ? 1f : 0f);
        Materials.Add(material);
        Upload(material);
    }

    /// <summary>The stirs this frame, to every bending material; nothing sent while none were or are.</summary>
    public static void Push(Stirs stirs)
    {
        int count = stirs.Write(Written);
        if (count == 0 && _count == 0) return;
        _count = count;
        for (int i = 0; i < count; i++) Shown[i] = new Vector4(Written[i].X, Written[i].Ground, Written[i].Offset, 0);
        foreach (var material in Materials) Upload(material);
    }

    /// <summary>The wind this frame: how far a gust leans a tip, signed, in pixels, and how far its strength has rolled (<c>Gusts.At</c>).</summary>
    public static void Wind(float lean, float roll)
    {
        if (_wind == (lean, roll) || (lean == 0 && _wind.Lean == 0)) return;
        _wind = (lean, roll);
        foreach (var material in Materials)
        {
            material.SetShaderParameter("wind", lean);
            material.SetShaderParameter("wind_roll", roll);
        }
    }

    private static void Upload(ShaderMaterial material)
    {
        material.SetShaderParameter("wind", _wind.Lean);
        material.SetShaderParameter("wind_roll", _wind.Roll);
        material.SetShaderParameter("stirs", Shown);
        material.SetShaderParameter("stir_count", _count);
    }
}
