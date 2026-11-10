using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// What bends as bodies brush it (plant-stirs): grass, tufts and a cave's floor moss, drawn with a material whose
/// <c>vertex()</c> moves each vertex sideways by the <see cref="Stirs"/> pushed here each frame — by its height over a point's
/// ground (none at or under it, whole a blade's height up, none two blades up) and across a cell either side of it. The push
/// depends only on where a vertex is in the world, so tiles side by side stay joined. Lit pictures bend through
/// <see cref="Lit.SetBending"/>; a glow added over them through <see cref="Glow"/>.
/// </summary>
public static class Bend
{
    /// <summary>The stirs' uniforms and <c>stirred(world)</c>, the push at a point of the world, for a shader's <c>vertex()</c>.</summary>
    public const string Code = """

            // Each point a body stirs: (x, its ground's y, the push at a blade's tip), in pixels (Stirs).
            uniform vec4 stirs[128];
            uniform int stir_count = 0;

            vec2 stirred(vec2 at) {
                float by = 0.0;
                for (int i = 0; i < stir_count; i++) {
                    vec4 s = stirs[i];
                    float across = 1.0 - abs(at.x - s.x) / 8.0;
                    if (across <= 0.0) continue;
                    float up = s.y - at.y;
                    by += s.z * across * clamp(up / 16.0, 0.0, 1.0) * (1.0 - smoothstep(16.0, 32.0, up));
                }
                return vec2(clamp(by, -8.0, 8.0), 0.0);
            }
            """;

    private static readonly List<ShaderMaterial> Materials = [];
    private static readonly Vector4[] Shown = new Vector4[Stirs.Most];
    private static readonly (float X, float Ground, float Offset)[] Written = new (float, float, float)[Stirs.Most];
    private static int _count;

    /// <summary>A glow added unlit over what bends, bending with it: a cave's floor moss's.</summary>
    public static readonly ShaderMaterial Glow = Made(new ShaderMaterial
    {
        Shader = new Shader
        {
            Code = "shader_type canvas_item;\nrender_mode blend_add, unshaded;\n" + Code + """

                void vertex() {
                    VERTEX += stirred((MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy);
                }
                """,
        },
    });

    private static ShaderMaterial Made(ShaderMaterial material)
    {
        Add(material);
        return material;
    }

    /// <summary>Bends <paramref name="material"/> from now on: its shader holds <see cref="Code"/>.</summary>
    public static void Add(ShaderMaterial material)
    {
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

    private static void Upload(ShaderMaterial material)
    {
        material.SetShaderParameter("stirs", Shown);
        material.SetShaderParameter("stir_count", _count);
    }
}
