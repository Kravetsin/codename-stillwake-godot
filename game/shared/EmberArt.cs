using System;
using Godot;
using I = Stillwake.Game.Shared.DroneInk;

namespace Stillwake.Game.Shared;

/// <summary>
/// Draws what an ember look gathered (<see cref="EmberMarks"/>) in this player's salts: its sparks' dark squares on a canvas item of
/// the normal blend, its light (halos, lines, glowing squares, in that order) on one that adds. Line points are kept by their count,
/// so a frame allocates nothing once warm.
/// </summary>
public sealed class EmberArt
{
    /// <summary>A spark's dark.</summary>
    public static readonly Color Dark = new(I.Dark.R / 255f, I.Dark.G / 255f, I.Dark.B / 255f);

    /// <summary>This client's own player's salts, which all the drone draws for them alone burns in; crew sets it as the hero is dressed.</summary>
    public static Salts Mine { get; set; } = Salts.Embers;

    private const int HaloSides = 16;
    private readonly Triangles _triangles = new();
    private Vector2[][] _points = [];

    /// <summary><paramref name="glint"/> as a colour at <paramref name="alpha"/>, no more than whole.</summary>
    public static Color Of(Glint glint, float alpha = 1) => new(glint.R, glint.G, glint.B, Math.Min(1, alpha));

    /// <summary>A mark's colour: a line's and a square's light lifted by the salts' gain, a halo's and an icon run's never.</summary>
    private static Color Of(float pale, float alpha, float burn, bool run, bool lifted) =>
        Of(run ? Mine.Run(burn, pale) : Mine.Spark(burn, pale), lifted && !run ? alpha * Mine.Gain : alpha);

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
        foreach (var h in marks.Halos) _triangles.Fan(new Vector2(h.X, h.Y), h.Radius, Of(h.Pale, h.Alpha, h.Burn, h.Run, false), Of(h.Pale, 0, h.Burn, h.Run, false), HaloSides);
        _triangles.Flush(on.GetCanvasItem());
        var points = marks.Points;
        foreach (var line in marks.Lines)
        {
            var run = Run(line.Count);
            for (int i = 0; i < line.Count; i++) run[i] = new Vector2(points[line.From + 2 * i], points[line.From + 2 * i + 1]);
            on.DrawPolyline(run, Of(line.Pale, line.Alpha, line.Burn, line.Run, true), line.Width, true);
        }
        _triangles.Clear();
        foreach (var s in marks.Squares)
            if (!s.Dark) _triangles.Rect(s.X - s.Side / 2, s.Y - s.Side / 2, s.Side, s.Side, Of(s.Pale, s.Alpha, s.Burn, s.Run, true));
        _triangles.Flush(on.GetCanvasItem());
    }

    private Vector2[] Run(int count)
    {
        if (count >= _points.Length) Array.Resize(ref _points, count + 1);
        return _points[count] ??= new Vector2[count];
    }
}
