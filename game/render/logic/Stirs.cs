using System;
using System.Collections.Generic;

namespace Stillwake.Game.Render;

/// <summary>
/// What brushes the plants (plant-stirs): each body pushes the grass sideways at the points along the ground by its feet, a
/// cell's edge apart, and each point springs toward that push, so grass parts round a hero standing, leans the way a runner
/// goes and rings back behind it. Only points pushed or still moving are kept; a shader bends what is drawn there by them
/// (<c>Bend</c>). Fed by every hero on a surface; creatures and swings push it the same way next. Plain C#.
/// </summary>
public sealed class Stirs
{
    /// <summary>The most points kept: past it the stillest go first.</summary>
    public const int Most = 128;

    /// <summary>Pixels between two points along the ground: a cell, so a grass tile's corners are its points.</summary>
    public const int Step = 8;

    /// <summary>A hero's run, in pixels a second: thousandths of a cell a controller step, 60 steps a second, 8 pixels a cell.</summary>
    public const float Run = Content.Hero.Run * 60 * 8 / 1000f;

    /// <summary>How far round a body it pushes, in its feel's reach: past it the push is under a hundredth.</summary>
    private const float Reaches = 3;

    /// <summary>Under this many pixels, and as slow, a point is still.</summary>
    private const float Still = 0.05f;

    private sealed class Point
    {
        public float Offset, Speed, Push;
    }

    private readonly Dictionary<(int Column, int Row), Point> _points = new();
    private readonly List<(int Column, int Row)> _gone = [];

    /// <summary>How many points move or are pushed.</summary>
    public int Count => _points.Count;

    /// <summary>
    /// A body with its feet at (<paramref name="x"/>, <paramref name="feet"/>), in pixels, going <paramref name="vx"/> pixels a
    /// second, pushing this frame: away from its feet either side by <see cref="FloraLooks.Feel.Part"/>, the way it goes by
    /// <see cref="FloraLooks.Feel.Lean"/> at a run. Called for each body before <see cref="Advance"/>.
    /// </summary>
    public void Body(float x, float feet, float vx, FloraLooks.Feel feel)
    {
        int row = (int)MathF.Round(feet / Step), middle = (int)MathF.Round(x / Step);
        int reach = (int)MathF.Ceiling(feel.Reach * Reaches / Step);
        float lean = feel.Lean * Math.Clamp(vx / Run, -1, 1);
        for (int column = middle - reach; column <= middle + reach; column++)
        {
            float d = (column * Step - x) / feel.Reach, near = MathF.Exp(-0.5f * d * d);
            // Parting: none under the feet, most a reach out, gone past three.
            float push = feel.Part * d * MathF.Exp(0.5f) * near + lean * near;
            if (!_points.TryGetValue((column, row), out var point)) _points[(column, row)] = point = new Point();
            point.Push += push;
        }
    }

    /// <summary>
    /// Springs every point toward what pushes it over <paramref name="seconds"/>, at <paramref name="feel"/>'s rate; then forgets
    /// the pushes, and the points still and unpushed. Past <see cref="Most"/>, the stillest go.
    /// </summary>
    public void Advance(double seconds, FloraLooks.Feel feel)
    {
        float dt = (float)Math.Min(seconds, 0.1), w = 2 * MathF.PI * feel.SpringHz, z = feel.Damping;
        int steps = Math.Max(1, (int)MathF.Ceiling(dt * 240));
        float h = dt / steps;
        _gone.Clear();
        foreach (var (key, point) in _points)
        {
            float k = w * Uneven(key.Column);
            for (int i = 0; i < steps; i++)
            {
                point.Speed += (k * k * (point.Push - point.Offset) - 2 * z * k * point.Speed) * h;
                point.Offset += point.Speed * h;
            }
            if (point.Push == 0 && MathF.Abs(point.Offset) < Still && MathF.Abs(point.Speed) < Still * w) _gone.Add(key);
            point.Push = 0;
        }
        foreach (var key in _gone) _points.Remove(key);
        if (_points.Count <= Most) return;
        _gone.Clear();
        foreach (var (key, point) in _points) _gone.Add(key);
        _gone.Sort((a, b) => Stirring(_points[a], w).CompareTo(Stirring(_points[b], w)));
        int excess = _points.Count - Most;
        for (int i = 0; i < excess; i++) _points.Remove(_gone[i]);
    }

    /// <summary>
    /// How much faster or slower than the feel's rate the grass at <paramref name="column"/> springs, 0.8 to 1.2 by a hash: side
    /// by side, clumps move out of step, as blades do, never as one sheet.
    /// </summary>
    public static float Uneven(int column)
    {
        uint h = unchecked((uint)column * 0x9E3779B1u);
        h ^= h >> 15;
        h = unchecked(h * 0x85EBCA77u);
        h ^= h >> 13;
        return 0.8f + 0.4f * (h & 0xFFFF) / 65535f;
    }

    private static float Stirring(Point point, float w) => MathF.Abs(point.Offset) + MathF.Abs(point.Speed) / w;

    /// <summary>The points into <paramref name="into"/> as (x, the ground's y, the push at a blade's tip), in pixels; how many.</summary>
    public int Write(Span<(float X, float Ground, float Offset)> into)
    {
        int n = 0;
        foreach (var ((column, row), point) in _points)
        {
            if (n == into.Length) break;
            into[n++] = (column * Step, row * Step, point.Offset);
        }
        return n;
    }

    /// <summary>The push at the tip of a blade standing on the ground at <paramref name="row"/>, at <paramref name="column"/>'s point; 0 for none.</summary>
    public float At(int column, int row) => _points.TryGetValue((column, row), out var point) ? point.Offset : 0;
}
