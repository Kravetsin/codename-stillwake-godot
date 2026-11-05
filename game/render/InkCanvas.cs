using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// Draws an <see cref="Ink"/> on a canvas item in one call. Its arrays are kept between draws and grow by doubling, the unused
/// rest left as empty triangles, so a drawing redrawn every frame, sparks or a jet, allocates nothing once it has grown.
/// </summary>
public sealed class InkCanvas
{
    private int[] _indices = [];
    private Vector2[] _points = [];
    private Color[] _colours = [];

    /// <summary>A pixel of the screen on <paramref name="on"/>'s canvas, in its units: what an ink's edges fade over.</summary>
    public static float PixelOf(CanvasItem on) => 1f / Mathf.Max(0.01f, on.GetGlobalTransformWithCanvas().X.Length());

    public void Draw(CanvasItem on, Ink ink)
    {
        if (ink.Empty) return;
        if (_points.Length < ink.Points.Count) (_points, _colours) = (new Vector2[Grown(ink.Points.Count)], new Color[Grown(ink.Points.Count)]);
        if (_indices.Length < ink.Indices.Count) _indices = new int[Grown(ink.Indices.Count / 3) * 3];
        for (int i = 0; i < ink.Points.Count; i++)
        {
            var (p, t) = (ink.Points[i], ink.Tones[i]);
            _points[i] = new Vector2(p.X, p.Y);
            _colours[i] = new Color(t.R, t.G, t.B, t.A);
        }
        ink.Indices.CopyTo(_indices);
        System.Array.Clear(_indices, ink.Indices.Count, _indices.Length - ink.Indices.Count);
        RenderingServer.CanvasItemAddTriangleArray(on.GetCanvasItem(), _indices, _points, _colours);
    }

    private static int Grown(int count)
    {
        int size = 64;
        while (size < count) size *= 2;
        return size;
    }
}
