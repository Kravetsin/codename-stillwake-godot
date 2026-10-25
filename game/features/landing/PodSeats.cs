using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Landing;

namespace Stillwake.Game.Features.Landing;

/// <summary>
/// Each drop pod with a hero in it, without a word: the hero behind its porthole from the chest up and the lamps round its skirt
/// lit (<c>dropPod_seated.png</c> over <c>machine_dropPod.png</c>); the hero's own body is hidden while it sits. Read on a timer.
/// </summary>
public partial class PodSeats : Node2D
{
    private const double RefreshMs = 250;
    private readonly IGridQueries _grid;
    private readonly LandingModule _landing;
    private readonly Dictionary<int, Sprite2D> _shown = [];
    private double _sinceRead = RefreshMs;

    public PodSeats(IGridQueries grid, LandingModule landing)
    {
        _grid = grid;
        _landing = landing;
        ZIndex = Layers.Screens;
    }

    public override void _Process(double delta)
    {
        if ((_sinceRead += delta * 1000) < RefreshMs) return;
        _sinceRead = 0;
        var seated = new HashSet<int>();
        foreach (var seat in _landing.Seats)
        {
            if (_grid.MachineById(seat.Pod) is not { } pod) continue;
            seated.Add(seat.Pod);
            if (!_shown.TryGetValue(seat.Pod, out var sprite))
            {
                sprite = new Sprite2D { Texture = PixelArt.Lit("dropPod_seated"), Centered = false };
                Lit.Set(sprite);
                AddChild(sprite);
                _shown[seat.Pod] = sprite;
            }
            sprite.Position = new Vector2(pod.Cell % _grid.Width, pod.Cell / _grid.Width) * ShipCamera.Cell;
        }
        foreach (var pod in new List<int>(_shown.Keys))
            if (!seated.Contains(pod))
            {
                _shown[pod].QueueFree();
                _shown.Remove(pod);
            }
    }
}
