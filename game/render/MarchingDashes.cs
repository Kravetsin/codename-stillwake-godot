using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// Dashes marching along <see cref="OutlineRun"/>s, in the node's <see cref="CanvasItem.Modulate"/>, as wide as the aim's
/// outline (<see cref="Outline.Width"/>) at any zoom: one mesh, a quad a side, each pixel carrying how far round its
/// line it lies, in line widths; the shader shows the dashes and moves them with the time, so marching costs no frame
/// anything and hundreds of plans one draw. Built when the runs change, and again when the zoom or the screen's height
/// does. Unlit, so they read in the dark.
/// </summary>
public partial class MarchingDashes : MeshInstance2D
{
    /// <summary>A dash and its gap, in line widths, and how many line widths a second they march round.</summary>
    private const int Dash = 3, Period = 5, March = 5;

    private static readonly ShaderMaterial Dashes = new()
    {
        Shader = new Shader
        {
            Code = $$"""
                shader_type canvas_item;
                render_mode unshaded;

                void fragment() {
                    // UV.x: how far round its line the pixel lies, in line widths.
                    float along = floor(UV.x);
                    if (mod(along - floor(TIME * {{March}}.0), {{Period}}.0) >= {{Dash}}.0) discard;
                }
                """,
        },
    };

    private IReadOnlyList<OutlineRun> _runs = [];
    /// <summary>Screen pixels a world pixel and the line's width in them, as the mesh was built for; none before it is.</summary>
    private (float Scale, int Width)? _built;

    public MarchingDashes(int z)
    {
        ZIndex = z;
        Material = Dashes;
    }

    /// <summary>Shows <paramref name="runs"/> from now on, in place of those shown before; nothing for none.</summary>
    public void Show(IReadOnlyList<OutlineRun> runs)
    {
        _runs = runs;
        _built = null;
        if (IsInsideTree()) Refresh();
    }

    public override void _Process(double delta) => Refresh();

    /// <summary>The mesh built again if the runs, the zoom or the screen's height changed since it was.</summary>
    private void Refresh()
    {
        var now = (GetScreenTransform().X.Length(), Outline.Width(GetViewportRect().Size.Y));
        if (_built == now) return;
        _built = now;
        Build(now.Item1, now.Item2);
    }

    private void Build(float scale, int width)
    {
        if (_runs.Count == 0 || scale <= 0)
        {
            Mesh = null;
            return;
        }
        var strips = PlanOutline.Strips(_runs, (double)width / scale);
        var vertices = new Vector2[strips.Count * 4];
        var along = new Vector2[strips.Count * 4];
        var indices = new int[strips.Count * 6];
        // World pixels to line widths along a line.
        float widths = scale / width;
        for (int i = 0; i < strips.Count; i++)
        {
            var s = strips[i];
            var start = new Vector2((float)s.X, (float)s.Y);
            var way = new Vector2(s.Dx, s.Dy);
            // Outside the silhouette: the strip's left.
            var outside = new Vector2(s.Dy, -s.Dx) * (float)s.Width;
            var end = start + way * (float)s.Length;
            float from = (float)s.Along * widths, to = (float)(s.Along + s.Length) * widths;
            int v = i * 4;
            (vertices[v], vertices[v + 1], vertices[v + 2], vertices[v + 3]) = (start, end, end + outside, start + outside);
            (along[v], along[v + 1], along[v + 2], along[v + 3]) = (new(from, 0), new(to, 0), new(to, 0), new(from, 0));
            int k = i * 6;
            (indices[k], indices[k + 1], indices[k + 2], indices[k + 3], indices[k + 4], indices[k + 5]) = (v, v + 1, v + 2, v, v + 2, v + 3);
        }
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.TexUV] = along;
        arrays[(int)Mesh.ArrayType.Index] = indices;
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        Mesh = mesh;
    }
}
