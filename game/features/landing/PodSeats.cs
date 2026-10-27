using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Landing;

namespace Stillwake.Game.Features.Landing;

/// <summary>
/// Each drop pod with a hero in it, without a word: the lamps round its skirt lit and the glint on its glass over the hero
/// strapped in behind it (<c>dropPod_lit.png</c> over the crew's seated figure, over <c>machine_dropPod.png</c>). The seats are
/// compared every frame, a handful at most, and the pictures laid again only when they change: the frame a hero leaves, its
/// lamps go out.
/// </summary>
public partial class PodSeats : Node2D
{
    private readonly IGridQueries _grid;
    private readonly LandingModule _landing;
    private readonly Dictionary<int, Sprite2D> _shown = [];
    private Seat[] _seen = [];

    private readonly Arrivals? _arrivals;
    private int _heldVersion;

    /// <param name="arrivals">The pods still coming down: their lamps light once they stand.</param>
    public PodSeats(IGridQueries grid, LandingModule landing, Arrivals? arrivals = null)
    {
        _grid = grid;
        _landing = landing;
        _arrivals = arrivals;
        ZIndex = Layers.Screens;
    }

    public override void _Process(double delta)
    {
        int held = _arrivals?.Version ?? 0;
        if (_landing.Seats.SequenceEqual(_seen) && held == _heldVersion) return;
        (_seen, _heldVersion) = ([.. _landing.Seats], held);
        var seated = new HashSet<int>();
        foreach (var seat in _seen)
        {
            if (_grid.MachineById(seat.Pod) is not { } pod || _arrivals?.Holds(seat.Pod) == true) continue;
            seated.Add(seat.Pod);
            if (!_shown.TryGetValue(seat.Pod, out var sprite))
            {
                sprite = new Sprite2D { Texture = PixelArt.Lit("dropPod_lit"), Centered = false };
                Lit.Set(sprite);
                AddChild(sprite);
                _shown[seat.Pod] = sprite;
            }
            // Over the pod's picture, which rises over its footprint as a tall machine's does (0099).
            sprite.Position = new Vector2(pod.Cell % _grid.Width, pod.Cell / _grid.Width - ShipTiles.RiseOf(pod.Kind)) * ShipCamera.Cell;
        }
        foreach (var pod in _shown.Keys.Where(p => !seated.Contains(p)).ToList())
        {
            _shown[pod].Visible = false;
            _shown[pod].QueueFree();
            _shown.Remove(pod);
        }
    }
}
