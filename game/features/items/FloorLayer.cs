using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Rooms;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// The stacks lying about (floor-layer.ts): each its item's picture drawn unlit, in its own colours whatever the
/// room's light, with a pale rim added over it as light (0075), breathing slowly, so it stands out in a dark room. At
/// rest it stands on the line it lies on and bobs up from it; moving it glides from where the last tick had it to
/// where this one does; in space it turns. One this player is picking up is gone at once (0014). A body's lie unseen inside it.
/// </summary>
public partial class FloorLayer : Node2D
{
    private const int Unit = 1000;
    private const float Cell = ShipCamera.Cell;
    /// <summary>A stack's rim round its picture, drawn a pixel of it a pixel of the world, as a share of the picture.</summary>
    private const float RimSize = 1.18f;
    /// <summary>How far apart stacks at rest in one cell are drawn, as a share of a cell.</summary>
    private const float Spread = 1.2f;
    /// <summary>A stack at rest rises this many pixels off the line it lies on and back, over this many ms: alive, not part of the floor.</summary>
    private const float Bob = 1.5f;
    private const double BobMs = 1600;
    /// <summary>The rim's strength, added over the picture, at the bottom of a bob and at its top.</summary>
    private const float RimLow = 0.22f, RimHigh = 0.5f;
    /// <summary>One floating in space turns once in this many ms, as debris does.</summary>
    private const double TurnMs = 6000;

    private static readonly CanvasItemMaterial Unlit = new() { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
    private static readonly CanvasItemMaterial Glowing = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
        LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
    };

    /// <summary>A stack drawn: its nodes; the world tick it was last read on and its position then and the tick before; when it came to rest.</summary>
    private sealed class Shown(Node2D view, Node2D picture, Sprite2D rim)
    {
        public readonly Node2D View = view, Picture = picture;
        public readonly Sprite2D Rim = rim;
        public int Tick, FromX, FromY, ToX, ToY;
        public double RestMs = double.NaN, LandMs = double.NaN, GlintMs;
        /// <summary>Whether it was flying when last drawn: landing, it squashes, hops and clinks.</summary>
        public bool Flying;
    }

    private readonly Dictionary<int, Shown> _shown = new();
    private double _clockMs;
    private readonly List<FloorStack> _lying = [];
    private readonly HashSet<int> _ids = [];
    private readonly List<int> _gone = [];
    private readonly Dictionary<int, int> _inCell = new(), _placed = new();
    private readonly Voices _clinks = new(S.World, voices: 4);

    public override void _Ready() => AddChild(_clinks);

    /// <param name="taking">Stacks this peer has a pick-up in flight for.</param>
    /// <param name="tick">The world's tick, whose positions the floor holds.</param>
    /// <param name="progress">How far the world is between its last tick and the next, 0 to 1.</param>
    public void Update(IGridQueries grid, IItemsQueries items, IRoomsQueries rooms, IReadOnlySet<int> taking, int tick, double progress, double elapsedMs)
    {
        _clockMs += elapsedMs;
        // Kept between frames, refilled: a frame allocates nothing here.
        var lying = _lying;
        lying.Clear();
        _ids.Clear();
        var floor = items.Floor;
        for (int i = 0; i < floor.Count; i++)
            // A body's stacks lie inside it, unseen: the body is drawn by crew (2026-10-29-body-bag).
            if (!taking.Contains(floor[i].Id) && !Reach.Hidden(floor[i], items.Ticks) && floor[i].Body == null)
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
                (one.Tick, one.FromX, one.FromY, one.ToX, one.ToY) = (tick, s.X, s.Y, s.X, s.Y);
                // Already lying there when first seen (a load, a join): its bob in a phase of its own.
                if (Resting(s)) one.RestMs = _clockMs - s.Id * 300 % BobMs;
                _shown[s.Id] = one;
                AddChild(one.View);
            }
            else if (one.Tick != tick)
                (one.Tick, one.FromX, one.FromY, one.ToX, one.ToY) = (tick, one.ToX, one.ToY, s.X, s.Y);
            bool inSpace = rooms.Afloat(s.Cell);
            // Between the last two ticks' positions, both the sim's: never past them into a floor or a wall.
            double x = one.FromX + (one.ToX - one.FromX) * progress;
            double y = one.FromY + (one.ToY - one.FromY) * progress;
            double rise = 0;
            if (!Resting(s))
            {
                one.RestMs = double.NaN;
                one.Flying = true;
            }
            else
            {
                if (double.IsNaN(one.RestMs)) one.RestMs = _clockMs;
                if (one.Flying && !inSpace) Landed(one, s);
                one.Flying = false;
                int n = inCell.GetValueOrDefault(s.Cell, 1);
                int i = placed.GetValueOrDefault(s.Cell);
                placed[s.Cell] = i + 1;
                x += (i - (n - 1) / 2.0) * Spread * Unit;
                // Up from the line it rests on and back, starting from it; afloat in space it drifts as loot does (0034).
                if (!inSpace) rise = (1 - Math.Cos((_clockMs - one.RestMs) / BobMs * 2 * Math.PI)) / 2;
            }
            var (hop, squash) = FloorFeel.Landing(_clockMs - one.LandMs);
            one.View.Position = new Vector2((float)(x / Unit * Cell), (float)(y / Unit * Cell - rise * Bob - hop));
            one.View.Scale = new Vector2(1 + squash / 2, 1 - squash);
            // Spinning the way it flies while it flies (2026-10-29-kill-feel); afloat in space it turns slowly as debris does.
            one.Picture.Rotation = inSpace ? (float)((_clockMs + s.Id * 1000) / TurnMs * 2 * Math.PI)
                : one.Flying ? (float)((_clockMs + s.Id * 170) / 1000 * KillLooks.Spin * 2 * Math.PI * Math.Sign(s.Vx == 0 ? 1 : s.Vx)) : 0;
            if (Resting(s) && !inSpace && KillLooks.Glints.Contains(s.Item)) Glints(one, (float)(x / Unit * Cell), (float)(y / Unit * Cell), elapsedMs);
            one.Rim.Modulate = new Color(1, 1, 1, (float)(RimLow + (RimHigh - RimLow) * rise));
        }
    }

    private static bool Resting(FloorStack s) => s.Vx == 0 && s.Vy == 0;

    /// <summary><paramref name="one"/>, <paramref name="s"/>, come down from flight: it squashes and hops, and clinks, higher the fewer it holds.</summary>
    private void Landed(Shown one, FloorStack s)
    {
        one.LandMs = _clockMs;
        var at = new Vector2(s.X, s.Y) / Unit * Cell;
        _clinks.Play(S.Clink, at, S.Clink.Tune * (1 + 0.25f / Math.Max(1, s.Count)));
    }

    /// <summary>A rare drop lying still glints now and then, each on a beat of its own.</summary>
    private static void Glints(Shown one, float x, float y, double elapsedMs)
    {
        one.GlintMs -= elapsedMs;
        if (one.GlintMs > 0) return;
        one.GlintMs = KillLooks.GlintEvery * 1000 * (0.7 + 0.6 * Random.Shared.NextDouble());
        Stages.Of(Layers.Effects).Flares.Glint(x, y - Cell * 0.6f, 5, (255, 250, 220));
    }

    /// <summary>An item's picture with a pale rim behind it, standing on the origin: its rim's lowest pixel on it.</summary>
    private static Shown PictureOf(string item)
    {
        var texture = ItemIcons.Icon(item);
        var rim = new Sprite2D { Texture = ItemIcons.Silhouette(item), Scale = Vector2.One * RimSize, Material = Glowing };
        var body = new Sprite2D { Texture = texture, Material = Unlit };
        // Turned about its middle, raised by its rim's half height less the clear rows under the picture.
        float half = (texture.GetHeight() / 2f - ItemIcons.ClearBelow(item)) * RimSize;
        var picture = new Node2D { Position = new Vector2(0, -half) };
        picture.AddChild(rim);
        picture.AddChild(body);
        var view = new Node2D();
        view.AddChild(picture);
        return new Shown(view, picture, rim);
    }
}
