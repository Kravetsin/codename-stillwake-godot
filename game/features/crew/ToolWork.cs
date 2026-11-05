using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Sound;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// Every running electric tool seen at work (tool-contact), this hero's and its friends': a steady stream thrown where it touches,
/// as what it touches looks hit, whether a hit lands this frame or not; and the plasma cutter's jet out to there. Its hits still
/// crack what they hit and are heard as they land.
/// </summary>
public partial class ToolWork : Node2D
{
    /// <summary>
    /// One hero's tool this frame: from its muzzle, in the world's pixels, to where it works, how what it touches looks hit
    /// (none while it touches nothing), and the floor its chips fall to.
    /// </summary>
    public readonly record struct Seen(int Hero, string Tool, Vector2 From, Vector2 At, HitLook? Look, double? Floor);

    private readonly Dictionary<int, StrikeTrain> _trains = new();
    private readonly List<Seen> _jets = new();
    private readonly HashSet<int> _working = new();
    private readonly List<int> _idle = new();
    private readonly Random _flicker = new();

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
        _jets.Clear();
        _working.Clear();
        foreach (var s in seen)
        {
            if (s.Tool == Content.Weapons.PlasmaCutter.Item) _jets.Add(s);
            if (s.Look is not { } look) continue;
            _working.Add(s.Hero);
            var train = _trains.GetValueOrDefault(s.Hero);
            int due = train.Due(elapsedMs, true, WorkLooks.ThrowsPerSecond);
            _trains[s.Hero] = train;
            var back = (s.From - s.At).Normalized();
            for (int i = 0; i < due; i++)
                Stages.Of(Layers.Effects).Hits.Throw(look, s.At.X, s.At.Y, back.X, back.Y, WorkLooks.ShareOf(s.Tool), s.Floor);
        }
        // One that stopped touching begins afresh, its first throw at once, when it touches again.
        _idle.Clear();
        foreach (int hero in _trains.Keys)
            if (!_working.Contains(hero)) _idle.Add(hero);
        foreach (int hero in _idle) _trains.Remove(hero);
        QueueRedraw();
    }

    private static Color Lit((float R, float G, float B, float A) c, float light) => new(c.R, c.G, c.B, c.A * light);

    public override void _Draw()
    {
        foreach (var jet in _jets)
        {
            float light = 1 - WorkLooks.JetFlicker * (float)_flicker.NextDouble();
            DrawLine(jet.From, jet.At, Lit(WorkLooks.JetGlow, light), WorkLooks.JetGlowWidth, true);
            DrawLine(jet.From, jet.At, Lit(WorkLooks.JetCore, light), WorkLooks.JetCoreWidth, true);
        }
    }
}
