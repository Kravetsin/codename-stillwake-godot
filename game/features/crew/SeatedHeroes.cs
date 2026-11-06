using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Landing;
using Stillwake.Sim.Players;
using Spec = Stillwake.Content.Hero;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// Each hero in a seat, drawn as dressed, the same for every peer; the hero's walking body is hidden while it sits
/// (<see cref="HeroView"/>). In a drop pod it is strapped in behind the porthole, seen through the glass only
/// (<c>dropPod_porthole.png</c>, the pod's dim inside, clips it); in a treadmill wheel it stands on the treads, running in place
/// while the sim says it exerts (power-chain). The seats are read every frame, a handful at most, so the frame a hero leaves its
/// body shows and the seat empties together.
/// </summary>
public partial class SeatedHeroes : Node2D
{
    /// <summary>Where the hips rest in the pod's picture, in pixels from its corner: the helmet fills the porthole, the chest below it.</summary>
    private static readonly Vector2 SeatAt = new(16, 35);
    /// <summary>Where a wheel's rider has its feet, in pixels from the wheel picture's corner: the middle of its lowest tread.</summary>
    private static readonly Vector2 WheelFeet = new(24, 44);
    /// <summary>
    /// A wheel's hoop turning under its runner (<c>art/treadWheel_turn_*.png</c>, the whole wheel but its rail, a seventy-second of
    /// a turn apart, so they loop), and how long each shows: slower than the treads would truly go, which would only flicker.
    /// </summary>
    private const int TurnFrames = 6;
    private const double TurnStepMs = 80;

    private readonly IGridQueries _grid;
    private readonly LandingModule _landing;
    private readonly ICrewQueries _crew;
    private readonly IPlayersQueries _players;
    private readonly IItemsQueries _items;
    /// <summary>
    /// Each seat sat in: what holds the figure (a pod's porthole, or a plain node over a wheel), the figure, whose it is, and how
    /// far its rider has run in place, in the hero's units, and a wheel's hoop and how long it has turned; made again for another hero.
    /// </summary>
    private readonly Dictionary<int, (Node2D Holder, Figure Figure, int Peer, double Ran, Sprite2D? Hoop, double TurnedMs)> _shown = [];

    /// <summary>A pod still coming down, drawn by the landing feature: nobody shows in it till it stands.</summary>
    private readonly Func<int, bool> _held;

    public SeatedHeroes(IGridQueries grid, LandingModule landing, ICrewQueries crew, IPlayersQueries players, IItemsQueries items, Func<int, bool>? held = null)
    {
        _held = held ?? (_ => false);
        _grid = grid;
        _landing = landing;
        _crew = crew;
        _players = players;
        _items = items;
        ZIndex = Layers.Screens;
    }

    public override void _Process(double delta)
    {
        var seated = new HashSet<int>();
        foreach (var seat in _landing.Seats)
        {
            if (_grid.MachineById(seat.Pod) is not { } pod || _held(seat.Pod)) continue;
            bool flies = Content.Machines.Is(pod.Kind, Content.MachineTrait.Lands);
            seated.Add(seat.Pod);
            if (_shown.TryGetValue(seat.Pod, out var was) && was.Peer != seat.Peer) Free(seat.Pod);
            int colour = Suits.Of(_players, seat.Peer);
            if (!_shown.TryGetValue(seat.Pod, out var shown))
            {
                Node2D holder = flies
                    ? new Sprite2D { Texture = PixelArt.Lit("dropPod_porthole"), Centered = false, ClipChildren = ClipChildrenMode.AndDraw }
                    : new Node2D();
                if (holder is Sprite2D porthole) Lit.Set(porthole);
                // In a wheel the rider stands between its hoop and its rail; the tile under it shows the wheel standing.
                var hoop = flies ? null : Picture(holder, "treadWheel_turn_0");
                var figure = new Figure(colour, own: false) { OnPixels = flies ? SeatAt : WheelFeet };
                holder.AddChild(figure);
                if (!flies) Picture(holder, "treadWheel_front");
                AddChild(holder);
                _shown[seat.Pod] = shown = (holder, figure, seat.Peer, 0, hoop, 0);
            }
            // Over the seat's picture, which rises over its footprint as a tall machine's does (0099).
            shown.Holder.Position = new Vector2(pod.Cell % _grid.Width, pod.Cell / _grid.Width - ShipTiles.RiseOf(pod.Kind)) * ShipCamera.Cell;
            var (helmet, body) = Suits.WornBy(_items, seat.Peer);
            shown.Figure.Wear(colour, helmet, body, _players.LookOf(seat.Peer));
            if (flies)
            {
                shown.Figure.Show(new Hero(0, 0, 0, 0, false, 1), Pose.Strapped, 0, false, delta * 1000);
                continue;
            }
            // Running in place: the stride follows a way run that only the treads take, at a run's speed.
            bool runs = _crew.HeroOf(seat.Peer) is { Exerting: true };
            double ran = shown.Ran + (runs ? Spec.Run * delta * 1000 / Hero.StepMs : 0), turned = shown.TurnedMs + (runs ? delta * 1000 : 0);
            _shown[seat.Pod] = shown with { Ran = ran, TurnedMs = turned };
            shown.Hoop!.Texture = PixelArt.Picture($"treadWheel_turn_{(long)(turned / TurnStepMs) % TurnFrames}");
            shown.Figure.Show(new Hero((int)ran, 0, runs ? Spec.Run : 0, 0, false, 1), runs ? Pose.Run : Pose.Stand, 0, false, delta * 1000);
        }
        foreach (var pod in _shown.Keys.Where(p => !seated.Contains(p)).ToList()) Free(pod);
    }

    /// <summary>Picture <paramref name="name"/> laid over the wheel in <paramref name="holder"/>, lit as the machine is.</summary>
    private static Sprite2D Picture(Node2D holder, string name)
    {
        var sprite = new Sprite2D { Texture = PixelArt.Picture(name), Centered = false };
        Lit.Set(sprite);
        holder.AddChild(sprite);
        return sprite;
    }

    private void Free(int pod)
    {
        _shown[pod].Holder.Visible = false;
        _shown[pod].Holder.QueueFree();
        _shown.Remove(pod);
    }
}
