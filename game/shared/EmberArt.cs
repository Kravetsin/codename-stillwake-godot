using System;
using Godot;
using I = Stillwake.Game.Shared.DroneInk;

namespace Stillwake.Game.Shared;

/// <summary>
/// Draws what an ember look gathered (<see cref="EmberMarks"/>) in the threads' ember: its sparks' dark squares on a canvas item of
/// the normal blend, its light (halos, lines, glowing squares, in that order) on one that adds. Line points are kept by their count,
/// so a frame allocates nothing once warm.
/// </summary>
public sealed class EmberArt
{
    /// <summary>The threads' ember, the one colour of the drone's frame and its icons (<see cref="DroneInk.Ember"/>), and its sparks' dark.</summary>
    public static readonly Color Ember = new(I.Ember.R / 255f, I.Ember.G / 255f, I.Ember.B / 255f), Dark = new(I.Dark.R / 255f, I.Dark.G / 255f, I.Dark.B / 255f);

    private const int HaloSides = 16;
    private readonly Triangles _triangles = new();
    private Vector2[][] _points = [];

    /// <summary>The ember <paramref name="pale"/> of the way to white, at <paramref name="alpha"/>.</summary>
    public static Color Of(float pale, float alpha) => new(Ember.Lerp(Colors.White, pale), alpha);

    /// <summary>The sparks' dark squares of <paramref name="marks"/> on <paramref name="on"/>, which blends normally.</summary>
    public void Darks(EmberMarks marks, CanvasItem on)
    {
        _triangles.Clear();
        foreach (var s in marks.Squares)
            if (s.Dark) _triangles.Rect(s.X - s.Side / 2, s.Y - s.Side / 2, s.Side, s.Side, Dark with { A = s.Alpha });
        _triangles.Flush(on.GetCanvasItem());
    }

    /// <summary>The light of <paramref name="marks"/> on <paramref name="on"/>, which adds: halos under lines under squares.</summary>
    public void Light(EmberMarks marks, CanvasItem on)
    {
        _triangles.Clear();
        foreach (var h in marks.Halos) _triangles.Fan(new Vector2(h.X, h.Y), h.Radius, Of(h.Pale, h.Alpha), Of(h.Pale, 0), HaloSides);
        _triangles.Flush(on.GetCanvasItem());
        var points = marks.Points;
        foreach (var line in marks.Lines)
        {
            var run = Run(line.Count);
            for (int i = 0; i < line.Count; i++) run[i] = new Vector2(points[line.From + 2 * i], points[line.From + 2 * i + 1]);
            on.DrawPolyline(run, Of(line.Pale, line.Alpha), line.Width, true);
        }
        _triangles.Clear();
        foreach (var s in marks.Squares)
            if (!s.Dark) _triangles.Rect(s.X - s.Side / 2, s.Y - s.Side / 2, s.Side, s.Side, Of(s.Pale, s.Alpha));
        _triangles.Flush(on.GetCanvasItem());
    }

    private Vector2[] Run(int count)
    {
        if (count >= _points.Length) Array.Resize(ref _points, count + 1);
        return _points[count] ??= new Vector2[count];
    }
}
