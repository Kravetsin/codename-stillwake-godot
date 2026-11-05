using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Sound;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// Every running electric tool seen at work (tool-contact, damage-looks), this hero's and its friends': a steady stream where it
/// touches, whether a hit lands this frame or not — hot sparks off a wreck (<see cref="ToolSparks"/>), what it touches looks hit
/// off anything else — and the plasma cutter's jet out to there (<see cref="CutterJet"/>). Its hits still crack what they hit and
/// are heard as they land.
/// </summary>
public partial class ToolWork : Node2D
{
    /// <summary>
    /// One hero's tool this frame: from its nozzle, where its edge begins, in the world's pixels, to where it works, the way it points, how what it touches looks hit
    /// (none while it touches nothing), and the floor its chips fall to.
    /// </summary>
    public readonly record struct Seen(int Hero, string Tool, Vector2 From, Vector2 At, Vector2 Way, HitLook? Look, double? Floor);

    private readonly Dictionary<int, StrikeTrain> _trains = new();
    private readonly List<Seen> _jets = new();
    private readonly HashSet<int> _working = new();
    private readonly List<int> _idle = new();
    private readonly ToolSparks _sparks = new();
    private readonly Ink _ink = new();
    private readonly InkCanvas _canvas = new();
    private double _clockMs;

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
        _sparks.Begin(elapsedMs);
        var wreck = HitLooks.Of[Impact.Wreck];
        foreach (var s in seen)
        {
            if (s.Tool == Content.Weapons.PlasmaCutter.Item) _jets.Add(s);
            if (s.Look is not { } look) continue;
            // Back out of the face toward the tool: the way it points, as its muzzle may be pressed right on it.
            var back = -s.Way;
            if (look == wreck)
            {
                _sparks.Touch(s.Hero, s.Tool, (s.At.X, s.At.Y), (back.X, back.Y), elapsedMs);
                continue;
            }
            _working.Add(s.Hero);
            var train = _trains.GetValueOrDefault(s.Hero);
            int due = train.Due(elapsedMs, true, WorkLooks.ThrowsPerSecond);
            _trains[s.Hero] = train;
            for (int i = 0; i < due; i++)
                Stages.Of(Layers.Effects).Hits.Throw(look, s.At.X, s.At.Y, back.X, back.Y, WorkLooks.ShareOf(s.Tool), s.Floor);
        }
        _sparks.End();
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
        foreach (var jet in _jets) CutterJet.Draw(_ink, (jet.From.X, jet.From.Y), (jet.At.X, jet.At.Y), jet.Look != null, _clockMs);
        _sparks.Draw(_ink);
        _canvas.Draw(this, _ink);
    }
}
