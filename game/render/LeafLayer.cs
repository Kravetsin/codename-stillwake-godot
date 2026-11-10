using System;
using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// <see cref="Leaves"/> drawn (plant-stirs): each picture a multimesh of its leaves behind the heroes and another of those
/// before them, and as many of their backs, shown while a leaf is flipped over; lit as the world is, their buffers refilled
/// each frame; a leaf turned and squashed across its length as it tumbles, its paint a little lighter or darker, fading as it
/// goes. A few draw calls for every leaf on the surface.
/// </summary>
public partial class LeafLayer : Node2D
{
    /// <summary>Floats an instance takes: a 2D transform and its colour.</summary>
    private const int Stride = 12;

    /// <summary>Per picture: behind the heroes, its face and back; before them, its face and back.</summary>
    private readonly MultiMeshInstance2D[] _layers;
    private readonly float[][] _buffers;
    private readonly int[] _counts;

    /// <summary>Leaves drawn with <paramref name="pictures"/>, their faces and backs, a leaf's <see cref="Leaves.Leaf.Picture"/> its index there.</summary>
    public LeafLayer(IReadOnlyList<(string Front, string Back)> pictures)
    {
        _layers = new MultiMeshInstance2D[pictures.Count * 4];
        _buffers = new float[_layers.Length][];
        _counts = new int[_layers.Length];
        for (int p = 0; p < pictures.Count; p++)
        {
            var size = HiRes.Logical(pictures[p].Front).GetSize();
            Texture2D face = PixelArt.Picture(pictures[p].Front), back = PixelArt.Picture(pictures[p].Back);
            (_layers[4 * p], _layers[4 * p + 1]) = (Layer(face, size, Layers.Bodies), Layer(back, size, Layers.Bodies));
            (_layers[4 * p + 2], _layers[4 * p + 3]) = (Layer(face, size, Layers.GroundFront), Layer(back, size, Layers.GroundFront));
        }
        for (int b = 0; b < _buffers.Length; b++) _buffers[b] = new float[Stride * 64];
    }

    private MultiMeshInstance2D Layer(Texture2D texture, Vector2I size, int z)
    {
        float w = size.X / 2f, h = size.Y / 2f;
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = new Vector2[] { new(-w, -h), new(w, -h), new(w, h), new(-w, h) };
        arrays[(int)Mesh.ArrayType.TexUV] = new Vector2[] { new(0, 0), new(1, 0), new(1, 1), new(0, 1) };
        arrays[(int)Mesh.ArrayType.Index] = new[] { 0, 1, 2, 0, 2, 3 };
        var quad = new ArrayMesh();
        quad.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        var layer = new MultiMeshInstance2D
        {
            Texture = texture,
            ZIndex = z,
            ZAsRelative = false,
            Multimesh = new MultiMesh
            {
                TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
                UseColors = true,
                Mesh = quad,
                // Culled by the box its first frame filled otherwise, and kept: see Strokes.
                CustomAabb = new Aabb(new Vector3(-1e7f, -1e7f, -1), new Vector3(2e7f, 2e7f, 2)),
            },
        };
        Lit.Set(layer);
        AddChild(layer);
        return layer;
    }

    /// <summary>Draws <paramref name="leaves"/> as they are now.</summary>
    public void Show(Leaves leaves)
    {
        Array.Clear(_counts);
        foreach (var l in leaves.All)
        {
            float w = l.Width;
            int b = 4 * l.Picture + (l.Front ? 2 : 0) + (w < 0 ? 1 : 0), n = _counts[b]++;
            ref var buffer = ref _buffers[b];
            if (buffer.Length < (n + 1) * Stride) Array.Resize(ref buffer, buffer.Length * 2);
            float c = MathF.Cos(l.Angle), s = MathF.Sin(l.Angle);
            int k = n * Stride;
            // Godot's 2D transform rows: x.x, y.x, -, origin.x; x.y, y.y, -, origin.y. Squashed across its length, then turned.
            (buffer[k], buffer[k + 1], buffer[k + 2], buffer[k + 3]) = (c, -s * w, 0, l.X);
            (buffer[k + 4], buffer[k + 5], buffer[k + 6], buffer[k + 7]) = (s, c * w, 0, l.Y);
            (buffer[k + 8], buffer[k + 9], buffer[k + 10], buffer[k + 11]) = (l.Shade, l.Shade, l.Shade, l.Alpha);
        }
        for (int b = 0; b < _layers.Length; b++) Fill(_layers[b].Multimesh, _buffers[b], _counts[b]);
    }

    private static void Fill(MultiMesh mesh, float[] buffer, int count)
    {
        int capacity = buffer.Length / Stride;
        if (mesh.InstanceCount != capacity) mesh.InstanceCount = capacity;
        if (count > 0) mesh.Buffer = buffer;
        mesh.VisibleInstanceCount = count;
    }
}
