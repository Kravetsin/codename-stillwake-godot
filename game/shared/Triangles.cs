using System;
using Godot;

namespace Stillwake.Game.Shared;

/// <summary>
/// Coloured triangles gathered for one call: hundreds of motes or spans cost one draw, not one each. It grows as it is filled,
/// and keeps its room between frames.
/// </summary>
public sealed class Triangles(int points = 64, int indices = 96)
{
    private const int FanSides = 6;
    private Vector2[] _points = new Vector2[points];
    private Color[] _colours = new Color[points];
    private int[] _indices = new int[indices];
    private int _pointCount, _indexCount;

    public void Clear() => (_pointCount, _indexCount) = (0, 0);

    public void Rect(float x, float y, float w, float h, Color colour)
    {
        Room(4, 6);
        int at = _pointCount;
        Point(new Vector2(x, y), colour);
        Point(new Vector2(x + w, y), colour);
        Point(new Vector2(x + w, y + h), colour);
        Point(new Vector2(x, y + h), colour);
        Quad(at);
    }

    /// <summary>A soft disc: <paramref name="middle"/>'s colour at its middle, fading to <paramref name="rim"/>'s at its edge.</summary>
    public void Fan(Vector2 at, float radius, Color middle, Color rim)
    {
        Room(FanSides + 1, FanSides * 3);
        int centre = _pointCount;
        Point(at, middle);
        for (int k = 0; k < FanSides; k++) Point(at + Vector2.FromAngle(Mathf.Tau * k / FanSides) * radius, rim);
        for (int k = 0; k < FanSides; k++)
        {
            _indices[_indexCount++] = centre;
            _indices[_indexCount++] = centre + 1 + k;
            _indices[_indexCount++] = centre + 1 + (k + 1) % FanSides;
        }
    }

    /// <summary>A line from <paramref name="a"/> to <paramref name="b"/>, <paramref name="width"/> wide, as one quad.</summary>
    public void Quad(Vector2 a, Vector2 b, float width, Color colour) => Quad(a, b, width, colour, colour);

    /// <summary>A line from <paramref name="a"/> to <paramref name="b"/>, its colour going from <paramref name="atA"/> to <paramref name="atB"/>.</summary>
    public void Quad(Vector2 a, Vector2 b, float width, Color atA, Color atB)
    {
        var along = b - a;
        if (along.LengthSquared() < 1e-8f) return;
        var side = new Vector2(-along.Y, along.X).Normalized() * (width / 2);
        Room(4, 6);
        int at = _pointCount;
        Point(a + side, atA);
        Point(b + side, atB);
        Point(b - side, atB);
        Point(a - side, atA);
        Quad(at);
    }

    /// <summary>A polygon of <paramref name="points"/>, each its own colour, cut into <paramref name="triangles"/> (indices into it).</summary>
    public void Polygon(ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colours, ReadOnlySpan<int> triangles)
    {
        Room(points.Length, triangles.Length);
        int at = _pointCount;
        for (int i = 0; i < points.Length; i++) Point(points[i], colours[i]);
        foreach (int k in triangles) _indices[_indexCount++] = at + k;
    }

    public void Flush(Rid item)
    {
        if (_indexCount == 0) return;
        RenderingServer.CanvasItemAddTriangleArray(item, new ReadOnlySpan<int>(_indices, 0, _indexCount),
            new ReadOnlySpan<Vector2>(_points, 0, _pointCount), new ReadOnlySpan<Color>(_colours, 0, _pointCount),
            ReadOnlySpan<Vector2>.Empty, ReadOnlySpan<int>.Empty, ReadOnlySpan<float>.Empty, default, -1);
    }

    private void Quad(int at)
    {
        foreach (int k in (ReadOnlySpan<int>)[0, 1, 2, 0, 2, 3]) _indices[_indexCount++] = at + k;
    }

    private void Room(int morePoints, int moreIndices)
    {
        if (_pointCount + morePoints > _points.Length)
        {
            int size = Math.Max(_points.Length * 2, _pointCount + morePoints);
            Array.Resize(ref _points, size);
            Array.Resize(ref _colours, size);
        }
        if (_indexCount + moreIndices > _indices.Length) Array.Resize(ref _indices, Math.Max(_indices.Length * 2, _indexCount + moreIndices));
    }

    private void Point(Vector2 at, Color colour)
    {
        _points[_pointCount] = at;
        _colours[_pointCount] = colour;
        _pointCount++;
    }
}
