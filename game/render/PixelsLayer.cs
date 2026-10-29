using System;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// The pixels killed creatures burst into (<see cref="PixelDust"/>), a world pixel each, lit as the world is, in one multimesh
/// whose buffer is refilled each frame: thousands of pixels in one draw call.
/// </summary>
public partial class PixelsLayer : MultiMeshInstance2D
{
    /// <summary>Floats an instance takes: a 2D transform and its colour.</summary>
    private const int Stride = 12;

    private float[] _buffer = new float[Stride * 256];

    public PixelsLayer()
    {
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        Vector2[] corners = [new(0, 0), new(1, 0), new(1, 1), new(0, 1)];
        arrays[(int)Mesh.ArrayType.Vertex] = corners;
        arrays[(int)Mesh.ArrayType.TexUV] = corners;
        arrays[(int)Mesh.ArrayType.Index] = new[] { 0, 1, 2, 0, 2, 3 };
        var quad = new ArrayMesh();
        quad.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        Multimesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
            UseColors = true,
            Mesh = quad,
            // Culled by the box its first frame filled otherwise, and kept: see Strokes.
            CustomAabb = new Aabb(new Vector3(-1e7f, -1e7f, -1), new Vector3(2e7f, 2e7f, 2)),
        };
        Lit.Set(this);
    }

    /// <summary>Draws <paramref name="dust"/>'s pixels as they are now, each at its whole pixel.</summary>
    public void Show(PixelDust dust)
    {
        var motes = dust.Motes;
        if (_buffer.Length < motes.Count * Stride) Array.Resize(ref _buffer, Math.Max(_buffer.Length * 2, motes.Count * Stride));
        for (int i = 0; i < motes.Count; i++)
        {
            var m = motes[i];
            int k = i * Stride;
            // Godot's 2D transform rows: x.x, y.x, -, origin.x; x.y, y.y, -, origin.y.
            (_buffer[k], _buffer[k + 1], _buffer[k + 2], _buffer[k + 3]) = (1, 0, 0, MathF.Floor(m.X));
            (_buffer[k + 4], _buffer[k + 5], _buffer[k + 6], _buffer[k + 7]) = (0, 1, 0, MathF.Floor(m.Y));
            (_buffer[k + 8], _buffer[k + 9], _buffer[k + 10], _buffer[k + 11]) = (m.Colour.R / 255f, m.Colour.G / 255f, m.Colour.B / 255f, m.Alpha);
        }
        var mesh = Multimesh;
        int capacity = _buffer.Length / Stride;
        if (mesh.InstanceCount != capacity) mesh.InstanceCount = capacity;
        if (motes.Count > 0) mesh.Buffer = _buffer;
        mesh.VisibleInstanceCount = motes.Count;
    }
}
