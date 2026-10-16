using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim.Debris;
using S = Stillwake.Game.Sound;
using Wreckage = Stillwake.Content.Wreckage;

namespace Stillwake.Game.Features.Debris;

/// <summary>
/// The debris drifting by (features/debris): each piece its item's picture, drawn between ticks so it
/// glides, tumbling slowly — a wreck only rocking, too big to spin — faded in over the start of its path
/// and out at its end; behind the hull, since it never collides with it (0006). The piece the crew's
/// hands would work is outlined, green while they would and red while the sim would refuse it
/// (<see cref="Ui.AimedPiece"/>); a piece this peer is grabbing is gone at once (0014). Every piece shows
/// the share of its hit points lost as cracks (<see cref="Cracks"/>, 0034); each hit jolts it, throws
/// sparks and is heard where it is, and a wreck broken bursts apart, for every peer alike.
/// </summary>
public partial class DebrisView(Ui ui) : Node2D
{
    private const float Cell = ShipCamera.Cell;
    private const int Unit = DebrisModule.Unit;
    /// <summary>A piece drawn as its item, with no pictures of its own: its size, in cells.</summary>
    private const float ItemSize = 0.8f;
    /// <summary>A wreck rocks this far either way, in radians, once in <see cref="RockMs"/>.</summary>
    private const double Rock = 0.08, RockMs = 7000;
    /// <summary>A hit knocks a piece <see cref="JoltPixels"/> away from the nearest hero and turns it <see cref="JoltTurn"/>, settling over <see cref="JoltMs"/>.</summary>
    private const double JoltMs = 220, JoltPixels = 3, JoltTurn = 0.07;
    private static readonly Color AimReady = new("7fe08a"), AimRefused = new("e05555");

    /// <summary>What is shown of a piece: its picture, the damage last seen, the jolt still settling and which way, and where it was on which tick.</summary>
    private sealed class Shown(Sprite2D picture, int damage)
    {
        public Sprite2D Picture { get; } = picture;
        public int Damage { get; set; } = damage;
        public double JoltMs { get; set; }
        public Vector2 Away { get; set; }
        public int X { get; set; }
        public int Speed { get; set; }
        public int End { get; set; }
        public int Tick { get; set; }
    }

    private readonly Dictionary<int, Shown> _shown = new();
    /// <summary>The piece aimed at, outlined over the rest.</summary>
    private readonly Outline _aimed = new(1);
    private readonly Heard2D _chop = Cues.Player2D(S.Chop, S.Suit, voices: 4), _break = Cues.Player2D(S.Break, S.Suit, voices: 2);
    private double _clockMs;
    private readonly HashSet<int> _ids = [];
    private readonly List<int> _gone = [];

    /// <summary>The pieces read last frame: for scripted runs.</summary>
    public IReadOnlyList<Piece> Pieces { get; private set; } = [];

    public override void _Ready()
    {
        // Behind the back walls: debris drifts past behind the hull.
        ZIndex = Layers.Debris;
        AddChild(_aimed);
        AddChild(_chop);
        AddChild(_break);
    }

    /// <summary>
    /// Shows the client's world through the current registry's debris, a resync bringing a new one.
    /// <paramref name="progress"/> is how far the world is between its last tick and the next.
    /// </summary>
    public void Frame(Client client, DebrisModule debris, double progress, double elapsedMs)
    {
        _clockMs += elapsedMs;
        Pieces = debris.Pieces;
        var grabbed = Predicted.Grabbing(client.InFlight);
        // Kept between frames, refilled: a frame allocates nothing here.
        _ids.Clear();
        var all = debris.Pieces;
        for (int i = 0; i < all.Count; i++)
            if (!grabbed.Contains(all[i].Id)) _ids.Add(all[i].Id);
        _gone.Clear();
        foreach (int id in _shown.Keys)
            if (!_ids.Contains(id)) _gone.Add(id);
        foreach (int id in _gone)
        {
            var gone = _shown[id];
            if (Fading.Broken(gone.Damage, gone.X, gone.Speed, gone.End, client.World.Tick - gone.Tick)) Burst(gone.Picture.Position);
            gone.Picture.QueueFree();
            _shown.Remove(id);
        }

        var aimed = ui.AimedPiece.Get();
        _aimed.Begin();
        for (int i = 0; i < all.Count; i++)
        {
            var piece = all[i];
            if (!_ids.Contains(piece.Id)) continue;
            if (!_shown.TryGetValue(piece.Id, out var one))
            {
                one = new Shown(PictureOf(piece.Item, piece.Id), piece.Damage);
                _shown[piece.Id] = one;
                AddChild(one.Picture);
                ShowCracks(one.Picture, piece);
            }
            (one.X, one.Speed, one.End, one.Tick) = (piece.X, piece.Speed, piece.End, client.World.Tick);
            double x = piece.X - piece.Speed * progress;
            var at = new Vector2((float)(x / Unit * Cell), (float)((double)piece.Y / Unit * Cell));
            if (piece.Damage > one.Damage) Struck(one, piece, at);
            one.JoltMs = Math.Max(0, one.JoltMs - elapsedMs);
            double jolt = one.JoltMs / JoltMs;
            jolt *= jolt;
            var picture = one.Picture;
            picture.Position = at + one.Away * (float)(JoltPixels * jolt);
            picture.Modulate = new Color(1, 1, 1, (float)Fading.FadeOf(x, piece.Start, piece.End));
            // A slow tumble, each piece its own way; a wreck rocks.
            double turn = Wreckage.HpOf(piece.Item) != null
                ? Rock * Math.Sin(_clockMs / RockMs * 2 * Math.PI + piece.Id)
                : _clockMs / 4000 * (piece.Id % 2 == 0 ? 1 : -1) + piece.Id;
            picture.Rotation = (float)(turn + JoltTurn * jolt * (piece.Id % 2 == 0 ? 1 : -1));
            if (aimed is { } a && a.Id == piece.Id)
                _aimed.Add(picture.Texture, picture.Position, picture.Rotation, picture.Scale.X, (a.Refused ? AimRefused : AimReady) with { A = picture.Modulate.A });
        }
        _aimed.End();
    }

    /// <summary>A hit landed on <paramref name="piece"/>, drawn at <paramref name="at"/>: its cracks deepen, it jolts away from the nearest hero, sparks fly that way and the chop is heard.</summary>
    private void Struck(Shown one, Piece piece, Vector2 at)
    {
        one.Damage = piece.Damage;
        ShowCracks(one.Picture, piece);
        var from = NearestHero(at);
        var away = (at - from).Normalized();
        if (away == Vector2.Zero) away = Vector2.Right;
        one.Away = away;
        one.JoltMs = JoltMs;
        // Struck on the side facing the hero, sparks thrown back toward it.
        var size = one.Picture.Texture.GetSize() * one.Picture.Scale;
        var side = at - away * Math.Min(size.X, size.Y) * 0.4f;
        Particles.Shared.Emit(Effect.Spark, side.X, side.Y, 8, (-away).Angle(), inSpace: true);
        _chop.PlayAt(side);
    }

    /// <summary>
    /// A wreck broken at <paramref name="at"/>: a puff of dust and sparks, and the break heard. The last hit
    /// removes the wreck on its tick, so its chop never plays: the break, with an impact of its own, is the hit.
    /// </summary>
    private void Burst(Vector2 at)
    {
        Particles.Shared.Emit(Effect.Dust, at.X, at.Y, 24, 0, inSpace: true);
        Particles.Shared.Emit(Effect.Dust, at.X, at.Y, 24, Math.PI, inSpace: true);
        Particles.Shared.Emit(Effect.Spark, at.X, at.Y, 14, -Math.PI / 2, inSpace: true);
        _break.PlayAt(at);
    }

    /// <summary>The chest of the hero nearest <paramref name="at"/>, as the crew drew them this frame: who most likely struck.</summary>
    private Vector2 NearestHero(Vector2 at)
    {
        var nearest = at + Vector2.Left;
        float best = float.MaxValue;
        foreach (var (x, y) in ui.Heroes)
        {
            var hero = new Vector2(x, y);
            if (hero.DistanceSquaredTo(at) >= best) continue;
            best = hero.DistanceSquaredTo(at);
            nearest = hero;
        }
        return nearest;
    }

    /// <summary>The share of its hit points <paramref name="piece"/> has lost, as cracks laid out by its id.</summary>
    private static void ShowCracks(Sprite2D picture, Piece piece)
    {
        if (Wreckage.HpOf(piece.Item) is int hp) Cracks.Show(picture, (float)piece.Damage / hp, piece.Id);
    }

    /// <summary>The picture piece <paramref name="id"/> wears (<see cref="DebrisLooks"/>), a pixel of art a pixel of the world, or its item's the size of a piece; centred, lit.</summary>
    private static Sprite2D PictureOf(string item, int id)
    {
        var body = DebrisLooks.PictureOf(item, id) is string look
            ? new Sprite2D { Texture = GD.Load<Texture2D>(look) }
            : new Sprite2D { Texture = ItemIcons.Icon(item), Scale = Vector2.One * (ItemSize * Cell / ItemIcons.Pixels) };
        Lit.Set(body);
        return body;
    }
}
