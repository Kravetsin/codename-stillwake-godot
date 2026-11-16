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
/// The stacks lying about (2026-11-16-loot-pieces): each a few small objects of its item's picture, drawn unlit, in their own
/// colours whatever the room's light. Flying they tumble; bouncing they clink; come to rest each tips onto the flat side of its
/// outline nearest how it landed (<see cref="LootPose"/>) and lies still. Near the hero a thin warm outline marks them. Moving a
/// stack glides from where the last tick had it to where this one does; in space it turns. One this player is picking up is gone
/// at once (0014). A body's lie unseen inside it.
/// </summary>
public partial class FloorLayer : Node2D
{
    private const int Unit = 1000;
    private const float Cell = ShipCamera.Cell;
    /// <summary>How far apart stacks at rest in one cell are drawn, as a share of a cell.</summary>
    private const float Spread = 1.2f;
    /// <summary>One floating in space turns once in this many ms, as debris does.</summary>
    private const double TurnMs = 6000;

    private static readonly CanvasItemMaterial Unlit = new() { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
    private static readonly Dictionary<string, IReadOnlyList<(float X, float Y)>> Hulls = new();

    /// <summary>One object of a stack: its picture, turned, and the outline behind it; its place beside the others, the angle it lies at.</summary>
    private sealed class Piece(Node2D picture, Sprite2D outline, float apart, float lift)
    {
        public readonly Node2D Picture = picture;
        public readonly Sprite2D Outline = outline;
        public readonly float Apart = apart, Lift = lift;
        public float Landed, Rest;
    }

    /// <summary>A stack drawn: its nodes; the world tick it was last read on and its position then and the tick before; when it came to rest.</summary>
    private sealed class Shown(Node2D view, string picture, Piece[] pieces)
    {
        public readonly Node2D View = view;
        public readonly string Picture = picture;
        public readonly Piece[] Pieces = pieces;
        public int Tick, FromX, FromY, ToX, ToY, LastVy;
        public double LandMs = double.NaN, GlintMs;
        /// <summary>Whether it was flying when last drawn: landing, it squashes, clinks and tips onto a side.</summary>
        public bool Flying;
        /// <summary>How strong its outline is, 0 to 1: it comes and goes as the hero nears and leaves.</summary>
        public float Outlined;
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
    /// <param name="hero">Where this player's hero stands, none before it is placed: stacks near it are outlined.</param>
    /// <param name="tick">The world's tick, whose positions the floor holds.</param>
    /// <param name="progress">How far the world is between its last tick and the next, 0 to 1.</param>
    public void Update(IGridQueries grid, IItemsQueries items, IRoomsQueries rooms, IReadOnlySet<int> taking, HeroSpot? hero, int tick, double progress, double elapsedMs)
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
        var near = Near(hero, grid.Width);
        float fade = (float)(elapsedMs / LootLooks.OutlineMs);
        foreach (var s in lying)
        {
            string picture = Charges.Picture(s.Item, s.Charge);
            Shown one;
            if (!_shown.TryGetValue(s.Id, out var was))
            {
                one = Make(s, picture);
                one.Tick = tick;
            }
            else if (was.Picture != picture || was.Pieces.Length != LootPose.Objects(s.Count))
            {
                // Grown or shrunk past a count that shows another number of objects, or its picture changed: drawn anew, where it was.
                one = Make(s, picture);
                (one.Tick, one.FromX, one.FromY, one.ToX, one.ToY, one.Outlined) = (was.Tick, was.FromX, was.FromY, was.ToX, was.ToY, was.Outlined);
                was.View.QueueFree();
            }
            else one = was;
            if (one.Tick != tick) (one.Tick, one.FromX, one.FromY, one.ToX, one.ToY) = (tick, one.ToX, one.ToY, s.X, s.Y);
            bool inSpace = rooms.Afloat(s.Cell);
            // Between the last two ticks' positions, both the sim's: never past them into a floor or a wall.
            double x = one.FromX + (one.ToX - one.FromX) * progress;
            double y = one.FromY + (one.ToY - one.FromY) * progress;
            // Turned back off a floor or a wall by the sim (2026-11-16-loot-pieces): it clinks.
            if (one.LastVy > 0 && s.Vy < 0 && !inSpace) Clink(s);
            one.LastVy = s.Vy;
            if (!Resting(s)) one.Flying = true;
            else
            {
                if (one.Flying && !inSpace) Landed(one, s);
                one.Flying = false;
                int n = inCell.GetValueOrDefault(s.Cell, 1);
                int i = placed.GetValueOrDefault(s.Cell);
                placed[s.Cell] = i + 1;
                x += (i - (n - 1) / 2.0) * Spread * Unit;
            }
            float px = (float)(x / Unit * Cell), py = (float)(y / Unit * Cell);
            var (_, squash) = FloorFeel.Landing(_clockMs - one.LandMs);
            one.View.Position = new Vector2(px, py);
            one.View.Scale = new Vector2(1 + squash / 2, 1 - squash);
            Pose(one, s, inSpace);
            one.Outlined = Math.Clamp(one.Outlined + (Within(near, s.Cell) ? fade : -fade), 0, 1);
            foreach (var piece in one.Pieces) piece.Outline.Visible = one.Outlined > 0;
            if (one.Outlined > 0)
                foreach (var piece in one.Pieces) piece.Outline.SelfModulate = new Color(LootLooks.OutlineR, LootLooks.OutlineG, LootLooks.OutlineB, LootLooks.OutlineA * one.Outlined);
            if (Resting(s) && !inSpace && KillLooks.Glints.Contains(s.Item)) Glints(one, px, py, elapsedMs);
        }
    }

    private static bool Resting(FloorStack s) => s.Vx == 0 && s.Vy == 0;

    /// <summary>
    /// <paramref name="one"/>'s objects turned and placed: tumbling the way it flies while it flies (2026-10-29-kill-feel), turning
    /// slowly afloat in space as debris does, both about its middle, else tipping from how it landed onto the side it lies on and
    /// coming down onto it, to stand on the line the stack lies on by its lowest point.
    /// </summary>
    private void Pose(Shown one, FloorStack s, bool inSpace)
    {
        var hull = Hull(one.Picture);
        double settle = Math.Clamp((_clockMs - one.LandMs) / LootLooks.SettleMs, 0, 1);
        // Eased out: it tips quickly, then lies still.
        float eased = double.IsNaN(settle) ? 1 : (float)(1 - (1 - settle) * (1 - settle));
        for (int k = 0; k < one.Pieces.Length; k++)
        {
            var piece = one.Pieces[k];
            float angle = inSpace ? (float)((_clockMs + s.Id * 1000 + k * 700) / TurnMs * 2 * Math.PI)
                : one.Flying ? (float)((_clockMs + s.Id * 170 + k * 230) / 1000 * KillLooks.Spin * 2 * Math.PI * Math.Sign(s.Vx == 0 ? 1 : s.Vx))
                : piece.Landed + (piece.Rest - piece.Landed) * eased;
            // Turning in the air about its middle, held clear of the line it lies on by its farthest point: stood on its lowest
            // point as it turned, it bobbed as if it caught on something. Landing, it comes down onto its side as it tips.
            float radius = LootPose.Radius(hull);
            float under = inSpace || one.Flying ? radius : radius + (LootPose.Lowest(hull, angle) - radius) * eased;
            piece.Picture.Rotation = angle;
            piece.Picture.Position = new Vector2(piece.Apart, -under * LootLooks.Size - piece.Lift);
        }
    }

    /// <summary><paramref name="one"/>, <paramref name="s"/>, come down from flight: it squashes and clinks, and each object picks the side it tips onto.</summary>
    private void Landed(Shown one, FloorStack s)
    {
        one.LandMs = _clockMs;
        Clink(s);
        var hull = Hull(one.Picture);
        foreach (var piece in one.Pieces)
        {
            piece.Landed = LootPose.Wrap(piece.Picture.Rotation);
            piece.Rest = LootPose.RestAngle(hull, piece.Landed);
        }
    }

    /// <summary>A clink where <paramref name="s"/> is, higher the fewer it holds.</summary>
    private void Clink(FloorStack s) =>
        _clinks.Play(S.Clink, new Vector2(s.X, s.Y) / Unit * Cell, S.Clink.Tune * (1 + 0.25f / Math.Max(1, s.Count)));

    /// <summary>A rare drop lying still glints now and then, each on a beat of its own.</summary>
    private static void Glints(Shown one, float x, float y, double elapsedMs)
    {
        one.GlintMs -= elapsedMs;
        if (one.GlintMs > 0) return;
        one.GlintMs = KillLooks.GlintEvery * 1000 * (0.7 + 0.6 * Random.Shared.NextDouble());
        Stages.Of(Layers.Effects).Flares.Glint(x, y - Cell * 0.4f, 4, (255, 250, 220));
    }

    /// <summary>The cells within <see cref="LootLooks.OutlineReach"/> of the hero's body, as a box: none before it is placed.</summary>
    private static (int Left, int Top, int Right, int Bottom, int Width) Near(HeroSpot? hero, int width)
    {
        if (hero is not { } h) return (1, 1, 0, 0, width);
        int reach = LootLooks.OutlineReach, half = Stillwake.Content.Hero.Width / 2;
        return (Down(h.X - half, Unit) - reach, Down(h.Y - Stillwake.Content.Hero.Height, Unit) - reach,
            Down(h.X + half - 1, Unit) + reach, Down(h.Y - 1, Unit) + reach, width);
    }

    private static bool Within((int Left, int Top, int Right, int Bottom, int Width) box, int cell) =>
        cell % box.Width >= box.Left && cell % box.Width <= box.Right && cell / box.Width >= box.Top && cell / box.Width <= box.Bottom;

    private static int Down(int a, int b) => a >= 0 ? a / b : -((-a + b - 1) / b);

    /// <summary>A stack's objects, drawn and added, as many as its count shows; already at rest when first seen (a load, a join), each lies at an angle of its own.</summary>
    private Shown Make(FloorStack s, string picture)
    {
        int n = LootPose.Objects(s.Count);
        var view = new Node2D();
        var pieces = new Piece[n];
        for (int k = 0; k < n; k++)
        {
            // Two side by side; a third laid over them.
            float apart = n == 1 ? 0 : n == 2 ? (k - 0.5f) * LootLooks.Apart : k == 2 ? 0 : (k - 0.5f) * 2 * LootLooks.Apart;
            float lift = n == 3 && k == 2 ? LootLooks.Apart * 0.5f : 0;
            var outline = new Sprite2D
            {
                Texture = ItemIcons.Silhouette(picture),
                Scale = Vector2.One * (1 + LootLooks.OutlineSize),
                Material = Unlit,
                Visible = false,
            };
            var body = new Sprite2D { Texture = ItemIcons.Icon(picture), Material = Unlit };
            var turned = new Node2D { Scale = Vector2.One * LootLooks.Size };
            turned.AddChild(outline);
            turned.AddChild(body);
            view.AddChild(turned);
            pieces[k] = new Piece(turned, outline, apart, lift);
        }
        var one = new Shown(view, picture, pieces) { ToX = s.X, ToY = s.Y, FromX = s.X, FromY = s.Y, LastVy = s.Vy };
        if (Resting(s))
        {
            var hull = Hull(picture);
            foreach (var piece in pieces)
                piece.Landed = piece.Rest = LootPose.RestAngle(hull, (float)((s.Id * 2.39 + piece.Apart) % (2 * Math.PI)));
        }
        else one.Flying = true;
        _shown[s.Id] = one;
        AddChild(view);
        return one;
    }

    /// <summary>The hull of <paramref name="picture"/>'s outline, taken once.</summary>
    private static IReadOnlyList<(float X, float Y)> Hull(string picture)
    {
        if (!Hulls.TryGetValue(picture, out var hull)) Hulls[picture] = hull = LootPose.Hull(ItemIcons.Edge(picture));
        return hull;
    }
}
