using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// Dashes a world pixel wide marching along <see cref="OutlineRun"/>s, in the node's <see cref="CanvasItem.Modulate"/>:
/// one mesh, a quad a run, built only when the runs change, each pixel carrying how far round its line it lies; the
/// shader shows the dashes and moves them with the time, so marching costs no frame anything and hundreds of plans one
/// draw. Unlit, so they read in the dark.
/// </summary>
public partial class MarchingDashes : MeshInstance2D
{
    /// <summary>A dash and its gap, in pixels of the world, and how many pixels a second they march round.</summary>
    private const int Dash = 3, Period = 5, March = 5;

    private static readonly ShaderMaterial Dashes = new()
    {
        Shader = new Shader
        {
            Code = $$"""
                shader_type canvas_item;
                render_mode unshaded;

                void fragment() {
                    // UV.x: how far round its line the pixel lies, counted from the run's first pixel's near side.
                    float along = floor(UV.x);
                    if (mod(along - floor(TIME * {{March}}.0), {{Period}}.0) >= {{Dash}}.0) discard;
                }
                """,
        },
    };

    public MarchingDashes(int z)
    {
        ZIndex = z;
        Material = Dashes;
    }

    /// <summary>Shows <paramref name="runs"/> from now on, in place of those shown before; nothing for none.</summary>
    public void Show(IReadOnlyList<OutlineRun> runs)
    {
        var vertices = new List<Vector2>(runs.Count * 4);
        var along = new List<Vector2>(runs.Count * 4);
        var indices = new List<int>(runs.Count * 6);
        foreach (var run in runs)
        {
            // The run's pixels as a box, and where along it the side its first pixel starts from lies.
            int x0 = run.Dx < 0 ? run.X - run.Length + 1 : run.X, y0 = run.Dy < 0 ? run.Y - run.Length + 1 : run.Y;
            int w = run.Dx != 0 ? run.Length : 1, h = run.Dy != 0 ? run.Length : 1;
            int startX = run.Dx < 0 ? run.X + 1 : run.X, startY = run.Dy < 0 ? run.Y + 1 : run.Y;
            float At(int x, int y) => run.Along + (run.Dx != 0 ? (x - startX) * run.Dx : (y - startY) * run.Dy);
            int first = vertices.Count;
            foreach (var (x, y) in ((int, int)[])[(x0, y0), (x0 + w, y0), (x0 + w, y0 + h), (x0, y0 + h)])
            {
                vertices.Add(new Vector2(x, y));
                along.Add(new Vector2(At(x, y), 0));
            }
            indices.AddRange([first, first + 1, first + 2, first, first + 2, first + 3]);
        }
        if (vertices.Count == 0)
        {
            Mesh = null;
            return;
        }
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
        arrays[(int)Mesh.ArrayType.TexUV] = along.ToArray();
        arrays[(int)Mesh.ArrayType.Index] = indices.ToArray();
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        Mesh = mesh;
    }
}
