using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// Every tool's haze at work (spark-rule), this hero's and its friends': a struck blade's sweep, a running tool's wheel, helix or
/// jet. Where a haze lies in matter it throws that matter's look, from there along the haze, <see cref="WorkLooks.HazeRate"/> hits
/// a second, whatever was hit and whoever hit it; the plasma cutter's jet is drawn out to where it stops (<see cref="CutterJet"/>).
/// </summary>
public partial class ToolWork : Node2D
{
    /// <summary>
    /// One hero's running tool this frame: from its nozzle, where its edge begins, in the world's pixels, to where it works, the way it
    /// points, whether it touches anything, where it touches each wreck it touches, by id, and its haze (<see cref="ToolHaze"/>).
    /// </summary>
    public readonly record struct Seen(int Hero, string Tool, Vector2 From, Vector2 At, Vector2 Way, bool Touching,
        IReadOnlyList<(int Piece, Vector2 At)> Wrecks, IReadOnlyList<HazePoint> Haze);

    private readonly List<Seen> _jets = new();
    private readonly Dictionary<int, double> _touched = new();
    private readonly List<int> _gone = new();
    private readonly List<int> _touchedNow = new();
    private readonly Ink _ink = new();
    private readonly InkCanvas _canvas = new();
    private double _clockMs;
    private uint _thrown;

    /// <summary>The wrecks a haze has lain in within a trail's life (<see cref="BladeTrail.LifeMs"/>), by id: what they throw is the haze's.</summary>
    public IReadOnlyCollection<int> Touched => _touched.Keys;

    public ToolWork()
    {
        ZIndex = Layers.Hero;
        Material = new CanvasItemMaterial
        {
            BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
            LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
        };
    }

    /// <summary>
    /// One frame of <paramref name="elapsedMs"/>: every running tool, <paramref name="seen"/>, and every haze at work,
    /// <paramref name="hazes"/>, thrown off what each lies in by <paramref name="matter"/>, chips falling to the floor
    /// <paramref name="floor"/> finds under where they leave it.
    /// </summary>
    public void Frame(IReadOnlyList<Seen> seen, IReadOnlyList<IReadOnlyList<HazePoint>> hazes, ToolHaze.MatterAt matter,
        Func<double, double, double?> floor, double elapsedMs)
    {
        _clockMs += elapsedMs;
        _jets.Clear();
        foreach (var s in seen)
            if (s.Tool == Content.Weapons.PlasmaCutter.Item) _jets.Add(s);
        bool Solid(double x, double y) => matter(x, y) != null;
        double share = WorkLooks.HazeRate * elapsedMs / 1000;
        var hits = Stages.Of(Layers.Effects).Hits;
        foreach (var haze in hazes)
        {
            _touchedNow.Clear();
            var touches = ToolHaze.Touches(haze, matter, _touchedNow);
            foreach (int piece in _touchedNow) _touched[piece] = _clockMs;
            int all = 0;
            foreach (var t in touches) all += t.Points.Count;
            foreach (var t in touches)
            {
                // Each thing it lies in its part of the haze's throws, from a few of its points, picked afresh each frame.
                int n = Math.Min(WorkLooks.ContactPoints, t.Points.Count);
                for (int k = 0; k < n; k++)
                {
                    var p = ToolHaze.Ridden(haze, t.Points[(int)(unchecked(_thrown++ * 2654435761u) % (uint)t.Points.Count)], Solid);
                    var (ox, oy) = p.Ride is { Count: > 0 } ride ? ride[^1] : (p.X, p.Y);
                    hits.ThrowAlong(t.Look, p, share * t.Points.Count / all / n, t.Wreck ? null : floor(ox, oy));
                }
            }
        }
        _gone.Clear();
        foreach (var (piece, at) in _touched)
            if (_clockMs - at > BladeTrail.LifeMs) _gone.Add(piece);
        foreach (int piece in _gone) _touched.Remove(piece);
        QueueRedraw();
    }

    public override void _Draw()
    {
        _ink.Clear();
        _ink.Feather = InkCanvas.PixelOf(this);
        foreach (var jet in _jets) CutterJet.Draw(_ink, (jet.From.X, jet.From.Y), (jet.At.X, jet.At.Y), jet.Touching, _clockMs);
        _canvas.Draw(this, _ink);
    }
}
