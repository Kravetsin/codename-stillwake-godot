using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Sound;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// Every running electric tool seen at work (tool-contact, spark-rule), this hero's and its friends': a steady stream of what it
/// touches looks hit, by its material, thrown from where its haze lies in it, whether a hit lands this frame or not, and the
/// plasma cutter's jet out to there (<see cref="CutterJet"/>). Its hits still crack what they hit and are heard as they land.
/// </summary>
public partial class ToolWork : Node2D
{
    /// <summary>
    /// What a running tool touches this frame: how it looks hit, the floor its chips fall to, and where the tool's haze lies in it
    /// (<see cref="ToolHaze.Contacts"/>).
    /// </summary>
    public readonly record struct Target(HitLook Look, double? Floor, IReadOnlyList<HazePoint> Contacts);

    /// <summary>
    /// One hero's tool this frame: from its nozzle, where its edge begins, in the world's pixels, to where it works, the way it points,
    /// whether it touches anything, where it touches each wreck it touches, by id, what it works (<see cref="Target"/>), and its
    /// haze (<see cref="ToolHaze"/>).
    /// </summary>
    public readonly record struct Seen(int Hero, string Tool, Vector2 From, Vector2 At, Vector2 Way, bool Touching,
        IReadOnlyList<(int Piece, Vector2 At)> Wrecks, IReadOnlyList<Target> Targets, IReadOnlyList<HazePoint> Haze);

    private readonly Dictionary<int, StrikeTrain> _trains = new();
    private readonly List<Seen> _jets = new();
    private readonly HashSet<int> _working = new();
    private readonly List<int> _idle = new();
    private readonly Ink _ink = new();
    private readonly InkCanvas _canvas = new();
    private double _clockMs;
    private int _thrown;

    public ToolWork()
    {
        ZIndex = Layers.Hero;
        Material = new CanvasItemMaterial
        {
            BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
            LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
        };
    }

    /// <summary>One frame of <paramref name="elapsedMs"/>: every tool now running, <paramref name="seen"/>.</summary>
    public void Frame(IReadOnlyList<Seen> seen, double elapsedMs)
    {
        _clockMs += elapsedMs;
        _jets.Clear();
        _working.Clear();
        foreach (var s in seen)
        {
            if (s.Tool == Content.Weapons.PlasmaCutter.Item) _jets.Add(s);
            if (s.Targets.Count == 0) continue;
            _working.Add(s.Hero);
            var train = _trains.GetValueOrDefault(s.Hero);
            int due = train.Due(elapsedMs, true, WorkLooks.ThrowsPerSecond);
            _trains[s.Hero] = train;
            for (int i = 0; i < due; i++)
            {
                // Each throw from what it works in turn, at a point of its contacts scattered over them.
                int n = _thrown++;
                var target = s.Targets[n % s.Targets.Count];
                var p = target.Contacts[(int)((uint)(n * 2654435761u) % (uint)target.Contacts.Count)];
                Stages.Of(Layers.Effects).Hits.Throw(target.Look, p.X, p.Y, p.Dx, p.Dy, WorkLooks.ShareOf(s.Tool), target.Floor);
            }
        }
        // One that stopped touching begins afresh, its first throw at once, when it touches again.
        _idle.Clear();
        foreach (int hero in _trains.Keys)
            if (!_working.Contains(hero)) _idle.Add(hero);
        foreach (int hero in _idle) _trains.Remove(hero);
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
