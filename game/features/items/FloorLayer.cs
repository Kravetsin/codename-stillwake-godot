using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Items;
using Stillwake.Sim.Rooms;
using FloorSpec = Stillwake.Content.Floor;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// The stacks lying about (floor-layer.ts): each its item's picture with a pale rim, so it stands out
/// from the floor; at rest it bobs a little, flying it glides between ticks, in space it turns. One this
/// player is picking up is gone at once (0014).
/// </summary>
public partial class FloorLayer : Node2D
{
    private const int Unit = 1000;
    private const float Cell = ShipCamera.Cell;
    /// <summary>How big a stack is drawn, as a share of a cell.</summary>
    private const float Size = 0.6f;
    /// <summary>How far apart stacks at rest in one cell are drawn, as a share of a cell.</summary>
    private const float Spread = 0.35f;
    /// <summary>A stack at rest bobs this many pixels, over this many ms: alive, not part of the floor.</summary>
    private const float Bob = 1.5f;
    private const double BobMs = 1600;
    /// <summary>One floating in space turns once in this many ms, as debris does.</summary>
    private const double TurnMs = 6000;

    private readonly Dictionary<int, (Node2D View, Node2D Picture)> _shown = new();
    private double _clockMs;
    private readonly List<FloorStack> _lying = [];
    private readonly HashSet<int> _ids = [];
    private readonly List<int> _gone = [];
    private readonly Dictionary<int, int> _inCell = new(), _placed = new();

    /// <param name="taking">Stacks this peer has a pick-up in flight for.</param>
    /// <param name="progress">How far the world is between its last tick and the next, 0 to 1.</param>
    public void Update(IItemsQueries items, IRoomsQueries rooms, IReadOnlySet<int> taking, double progress, double elapsedMs)
    {
        _clockMs += elapsedMs;
        // Kept between frames, refilled: a frame allocates nothing here.
        var lying = _lying;
        lying.Clear();
        _ids.Clear();
        var floor = items.Floor;
        for (int i = 0; i < floor.Count; i++)
            if (!taking.Contains(floor[i].Id))
            {
                lying.Add(floor[i]);
                _ids.Add(floor[i].Id);
            }
        _gone.Clear();
        foreach (int id in _shown.Keys)
            if (!_ids.Contains(id)) _gone.Add(id);
        foreach (int id in _gone)
        {
            _shown[id].View.QueueFree();
            _shown.Remove(id);
        }
        // Stacks at rest in one cell, or floating still in space, of different items, side by side rather than one over another.
        var inCell = _inCell;
        var placed = _placed;
        inCell.Clear();
        placed.Clear();
        foreach (var s in lying)
            if (Resting(s)) inCell[s.Cell] = inCell.GetValueOrDefault(s.Cell) + 1;
        foreach (var s in lying)
        {
            if (!_shown.TryGetValue(s.Id, out var one))
            {
                one = PictureOf(Charges.Picture(s.Item, s.Charge));
                _shown[s.Id] = one;
                AddChild(one.View);
            }
            bool inSpace = rooms.RoomAt(s.Cell) == RoomsModule.Space;
            // Between ticks, carried on at its speed: the sim moves it a whole tick at a time.
            int drift = inSpace ? FloorSpec.SpaceDrift : 0;
            double x = s.X + (s.Vx - drift) * progress;
            double y = s.Y + (inSpace || s.Vy != 0 ? s.Vy * progress : 0);
            double bob = 0;
            // Afloat in space too, as a broken wreck's loot drifts off together (0034); only aboard does it bob.
            if (Resting(s))
            {
                int n = inCell.GetValueOrDefault(s.Cell, 1);
                int i = placed.GetValueOrDefault(s.Cell);
                placed[s.Cell] = i + 1;
                x += (i - (n - 1) / 2.0) * Spread * Unit;
                if (!inSpace) bob = Math.Sin((_clockMs + s.Id * 300) / BobMs * 2 * Math.PI) * Bob;
            }
            one.View.Position = new Vector2((float)(x / Unit * Cell), (float)(y / Unit * Cell - bob));
            one.Picture.Rotation = inSpace ? (float)((_clockMs + s.Id * 1000) / TurnMs * 2 * Math.PI) : 0;
        }
    }

    private static bool Resting(FloorStack s) => s.Vx == 0 && s.Vy == 0;

    /// <summary>An item's picture with a pale rim behind it, centred half its size above the origin.</summary>
    private static (Node2D View, Node2D Picture) PictureOf(string item)
    {
        var rim = new Sprite2D
        {
            Texture = ItemIcons.Silhouette(item),
            Modulate = new Color(1, 1, 1, 0.35f),
            Scale = Vector2.One * (Size * 1.18f * Cell / ItemIcons.Pixels),
        };
        var body = new Sprite2D { Texture = ItemIcons.Icon(item), Scale = Vector2.One * (Size * Cell / ItemIcons.Pixels) };
        Lit.Set(rim);
        Lit.Set(body);
        // Turned about its middle, which sits half its size above the point it rests on.
        var picture = new Node2D { Position = new Vector2(0, -Size * Cell / 2) };
        picture.AddChild(rim);
        picture.AddChild(body);
        var view = new Node2D();
        view.AddChild(picture);
        return (view, picture);
    }
}
