using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Landing;
using Stillwake.Sim.Players;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// Each hero sitting in a drop pod, strapped in behind its porthole: its own figure in its suit, seen through the glass only
/// (<c>dropPod_porthole.png</c>, the pod's dim inside, clips it), the same for every peer; the hero's walking body is hidden
/// while it sits (<see cref="HeroView"/>). The seats are read every frame, a handful at most, so the frame a hero leaves its
/// body shows and the porthole empties together.
/// </summary>
public partial class SeatedHeroes : Node2D
{
    /// <summary>Where the hips rest in the pod's picture, in pixels from its corner: the helmet fills the porthole, the chest below it.</summary>
    private static readonly Vector2 SeatAt = new(16, 35);

    private readonly IGridQueries _grid;
    private readonly LandingModule _landing;
    private readonly IPlayersQueries _players;
    /// <summary>Each pod sat in: its porthole, the figure in it and whose it is, made again for another hero.</summary>
    private readonly Dictionary<int, (Sprite2D Porthole, Figure Figure, int Peer)> _shown = [];

    /// <summary>A pod still coming down, drawn by the landing feature: nobody shows in it till it stands.</summary>
    private readonly Func<int, bool> _held;

    public SeatedHeroes(IGridQueries grid, LandingModule landing, IPlayersQueries players, Func<int, bool>? held = null)
    {
        _held = held ?? (_ => false);
        _grid = grid;
        _landing = landing;
        _players = players;
        ZIndex = Layers.Screens;
    }

    public override void _Process(double delta)
    {
        var seated = new HashSet<int>();
        foreach (var seat in _landing.Seats)
        {
            if (_grid.MachineById(seat.Pod) is not { } pod || _held(seat.Pod)) continue;
            seated.Add(seat.Pod);
            if (_shown.TryGetValue(seat.Pod, out var was) && was.Peer != seat.Peer) Free(seat.Pod);
            int colour = Suits.Of(_players, seat.Peer);
            if (!_shown.TryGetValue(seat.Pod, out var shown))
            {
                var porthole = new Sprite2D { Texture = PixelArt.Lit("dropPod_porthole"), Centered = false, ClipChildren = ClipChildrenMode.AndDraw };
                Lit.Set(porthole);
                var figure = new Figure(colour, own: false) { OnPixels = SeatAt };
                porthole.AddChild(figure);
                AddChild(porthole);
                _shown[seat.Pod] = shown = (porthole, figure, seat.Peer);
            }
            // Over the pod's picture, which rises over its footprint as a tall machine's does (0099).
            shown.Porthole.Position = new Vector2(pod.Cell % _grid.Width, pod.Cell / _grid.Width - ShipTiles.RiseOf(pod.Kind)) * ShipCamera.Cell;
            shown.Figure.Wear(colour);
            shown.Figure.Show(new Hero(0, 0, 0, 0, false, 1), Pose.Strapped, 0, false, delta * 1000);
        }
        foreach (var pod in _shown.Keys.Where(p => !seated.Contains(p)).ToList()) Free(pod);
    }

    private void Free(int pod)
    {
        _shown[pod].Porthole.Visible = false;
        _shown[pod].Porthole.QueueFree();
        _shown.Remove(pod);
    }
}
