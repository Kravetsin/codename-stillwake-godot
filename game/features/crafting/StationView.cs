using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Crafting;

/// <summary>
/// A station's moving parts at work, without a word (0057): over its picture, which shows them at rest, its
/// pictures of them moving (<c>art/{kind}_work_*.png</c>, each the whole machine's size, opaque over what they
/// cover) shown in turn while the sim says it works (<c>CraftingModule.Works</c>): the Former's flywheel turns,
/// its gears with it, and its press head beats on the die. Idle, waiting full or without power, it stands still.
/// Lit as the machine is.
/// </summary>
public partial class StationView : Node2D
{
    private const int Cell = ShipCamera.Cell;

    /// <summary>The stations whose parts move, by kind: how many pictures, each shown this long.</summary>
    private static readonly Dictionary<string, (int Frames, double StepMs)> Moving = new() { ["former"] = (4, 110) };

    private readonly IGridQueries _grid;
    private readonly CraftingModule _crafting;
    /// <summary>The moving parts shown, by machine id.</summary>
    private readonly Dictionary<int, Sprite2D> _shown = new();
    private readonly HashSet<int> _working = new();
    private readonly List<int> _stopped = new();
    private double _clock;

    public StationView(IGridQueries grid, CraftingModule crafting)
    {
        _grid = grid;
        _crafting = crafting;
    }

    /// <summary>The stations shown at work: what a recording checks.</summary>
    public int Working => _shown.Count;

    public override void _Process(double delta)
    {
        _clock += delta * 1000;
        _working.Clear();
        foreach (var m in _grid.Machines)
        {
            if (!Moving.TryGetValue(m.Kind, out var motion) || !_crafting.Works(m.Id)) continue;
            _working.Add(m.Id);
            if (!_shown.TryGetValue(m.Id, out var sprite))
            {
                // Over the machine's picture, lit as it is.
                sprite = new Sprite2D { Centered = false, ZIndex = Layers.Sockets };
                Lit.Set(sprite);
                AddChild(sprite);
                _shown[m.Id] = sprite;
            }
            sprite.Position = new Vector2(m.Cell % _grid.Width, m.Cell / _grid.Width) * Cell;
            // Each machine a step apart from the next, so two Formers never beat in step.
            int frame = ((int)(_clock / motion.StepMs) + m.Id) % motion.Frames;
            sprite.Texture = PixelArt.Picture($"{m.Kind}_work_{frame}");
        }
        _stopped.Clear();
        foreach (var id in _shown.Keys)
            if (!_working.Contains(id)) _stopped.Add(id);
        foreach (var id in _stopped)
        {
            _shown[id].QueueFree();
            _shown.Remove(id);
        }
    }
}
