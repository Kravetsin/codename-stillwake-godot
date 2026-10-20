using System;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// Many soft-edged strokes of light in one draw call (0075): each a run of segments, a quad apiece in one
/// multimesh whose buffer is refilled each frame, its colour running from one end to the other, its edges fading
/// over the last screen pixel either side, added over the world. Godot draws each polyline as a draw call and a
/// fresh buffer of its own, which hundreds of streaks a frame made the frame's main cost. Filled between
/// <see cref="Begin"/> and <see cref="End"/> in its own units, which its owner keeps a screen pixel each.
/// </summary>
public partial class Strokes : MultiMeshInstance2D
{
    private static readonly Shader Shader = new()
    {
        Code = """
            shader_type canvas_item;
            render_mode unshaded, blend_add;
            // The colour at the segment's far end; its near end's is the instance's colour.
            varying vec4 far_end;

            void vertex() {
                far_end = INSTANCE_CUSTOM;
            }

            void fragment() {
                vec4 c = mix(COLOR, far_end, UV.x);
                // Across, UV.y runs -1..1 over the core and a pixel of feather either side: fade over that last pixel.
                float edge = 1.0 - abs(UV.y);
                c.a *= clamp(edge / max(fwidth(UV.y), 1e-4), 0.0, 1.0);
                COLOR = c;
            }
            """,
    };

    /// <summary>Floats an instance takes: a 2D transform, its near colour, its far colour.</summary>
    private const int Stride = 16;

    private float[] _buffer = new float[Stride * 256];
    private int _count;

    public Strokes()
    {
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        // Along 0..1 from the near end to the far, across -1..1.
        Vector2[] corners = [new(0, -1), new(1, -1), new(1, 1), new(0, 1)];
        arrays[(int)Mesh.ArrayType.Vertex] = corners;
        arrays[(int)Mesh.ArrayType.TexUV] = corners;
        arrays[(int)Mesh.ArrayType.Index] = new[] { 0, 1, 2, 0, 2, 3 };
        var quad = new ArrayMesh();
        quad.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        Multimesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
            UseColors = true,
            UseCustomData = true,
            Mesh = quad,
            // Culled by the box its first frame filled otherwise, and kept: see Lightmap.
            CustomAabb = new Aabb(new Vector3(-1e7f, -1e7f, -1), new Vector3(2e7f, 2e7f, 2)),
        };
        Material = new ShaderMaterial { Shader = Shader };
    }

    /// <summary>The segments drawn last frame: what the frame's cost grows with.</summary>
    public int Segments => _count;

    public void Begin() => _count = 0;

    /// <summary>A stroke through <paramref name="points"/>, coloured at each by <paramref name="colours"/>, its solid core <paramref name="width"/> across.</summary>
    public void Line(ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colours, float width)
    {
        float half = width / 2 + 1;
        for (int i = 0; i + 1 < points.Length; i++)
        {
            var (a, b) = (points[i], points[i + 1]);
            var along = b - a;
            float length = along.Length();
            if (length < 1e-4f) continue;
            var across = new Vector2(-along.Y, along.X) / length * half;
            if (_buffer.Length < (_count + 1) * Stride) Array.Resize(ref _buffer, _buffer.Length * 2);
            int k = _count++ * Stride;
            // Godot's 2D transform rows: x.x, y.x, -, origin.x; x.y, y.y, -, origin.y.
            (_buffer[k], _buffer[k + 1], _buffer[k + 2], _buffer[k + 3]) = (along.X, across.X, 0, a.X);
            (_buffer[k + 4], _buffer[k + 5], _buffer[k + 6], _buffer[k + 7]) = (along.Y, across.Y, 0, a.Y);
            var (c, d) = (colours[i], colours[i + 1]);
            (_buffer[k + 8], _buffer[k + 9], _buffer[k + 10], _buffer[k + 11]) = (c.R, c.G, c.B, c.A);
            (_buffer[k + 12], _buffer[k + 13], _buffer[k + 14], _buffer[k + 15]) = (d.R, d.G, d.B, d.A);
        }
    }

    /// <summary>Hands every segment since <see cref="Begin"/> to the multimesh.</summary>
    public void End()
    {
        var mesh = Multimesh;
        int capacity = _buffer.Length / Stride;
        if (mesh.InstanceCount != capacity) mesh.InstanceCount = capacity;
        if (_count > 0) mesh.Buffer = _buffer;
        mesh.VisibleInstanceCount = _count;
    }
}
