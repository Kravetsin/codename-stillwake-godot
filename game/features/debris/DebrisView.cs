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
/// and out at its end; behind the hull, since it never collides with it (0006). Only pieces within the
/// camera's view and <see cref="Margin"/> cells round it are drawn, their pictures taken from a pool that
/// never frees (0052). The piece the crew's hands would work is outlined, green while they would and red
/// while the sim would refuse it (<see cref="Ui.AimedPiece"/>); a piece this peer is grabbing is gone at
/// once (0014). A wreck shows the share of its hit points lost as cracks (<see cref="Cracks"/>, 0034); each
/// hit jolts it and throws sparks, and a wreck broken bursts apart, for every peer alike; only this
/// player's own hits are heard (0042).
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
    /// <summary>Cells round the camera's view a piece is still drawn in: the biggest wreck's half and a frame's drift.</summary>
    private const int Margin = 4;
    /// <summary>Pictures made at the start, small pieces' and wrecks': more than the far field shows in view at once (0052).</summary>
    private const int WarmSmall = 48, WarmWrecks = 32;
    private static readonly Color AimReady = new("7fe08a"), AimRefused = new("e05555");

    /// <summary>What is shown of a piece: its picture, the damage last seen, the jolt still settling and which way, and where it was on which tick.</summary>
    private sealed class Shown(Sprite2D picture, bool wreck)
    {
        public Sprite2D Picture { get; } = picture;
        /// <summary>Whether its picture is a wreck's, drawn with the cracking material, and so goes back to the wrecks' pool.</summary>
        public bool Wreck { get; } = wreck;
        public int Damage { get; set; }
        public double JoltMs { get; set; }
        public Vector2 Away { get; set; }
        public int X { get; set; }
        public int Speed { get; set; }
        public int End { get; set; }
        public int Tick { get; set; }
    }

    private readonly Dictionary<int, Shown> _shown = new();
    /// <summary>Pictures not drawn now, small pieces' and wrecks' apart, as their materials differ: kept, never freed.</summary>
    private readonly Stack<Shown> _small = new(), _wrecks = new();
    private static readonly Dictionary<(string Item, int Look), Texture2D> Textures = new();
    /// <summary>The piece aimed at, outlined over the rest.</summary>
    private readonly Outline _aimed = new(1);
    private readonly Heard2D _chop = Cues.Player2D(S.Chop, S.Suit, voices: 4), _break = Cues.Player2D(S.Break, S.Suit, voices: 2);
    private readonly OwnHits _own = new();
    private double _clockMs;
    /// <summary>Every piece out but those this peer is grabbing, and of them the ones drawn this frame.</summary>
    private readonly HashSet<int> _out = [], _drawn = [];
    private readonly List<int> _gone = [];

    /// <summary>The pieces read last frame: for scripted runs.</summary>
    public IReadOnlyList<Piece> Pieces { get; private set; } = [];
    /// <summary>The pictures drawn last frame, and the most ever made: for the frame log and scripted runs.</summary>
    public (int Drawn, int Made) Pictures => (_shown.Count, _shown.Count + _small.Count + _wrecks.Count);

    public override void _Ready()
    {
        // Behind the back walls: debris drifts past behind the hull.
        ZIndex = Layers.Debris;
        // Every picture loaded now, not on the frame a piece first comes into view.
        foreach (var (item, (_, count)) in DebrisLooks.Of)
            for (int look = 0; look < count; look++) PictureOf(item, look);
        // Pictures made now for a crowd of pieces in view, not some forty in the frame a cloud drifts in.
        for (int i = 0; i < WarmSmall; i++) _small.Push(Made(wreck: false));
        for (int i = 0; i < WarmWrecks; i++) _wrecks.Push(Made(wreck: true));
        AddChild(_aimed);
        AddChild(_chop);
        AddChild(_break);
    }

    /// <summary>
    /// Shows the client's world through the current registry's debris, a resync bringing a new one, what
    /// lies within <paramref name="view"/> (in cells) and <see cref="Margin"/> round it.
    /// <paramref name="progress"/> is how far the world is between its last tick and the next.
    /// </summary>
    public void Frame(Client client, DebrisModule debris, Rect2 view, double progress, double elapsedMs)
    {
        _clockMs += elapsedMs;
        Pieces = debris.Pieces;
        _own.Read(client.InFlight);
        var grabbed = Predicted.Grabbing(client.InFlight);
        int left = (int)Math.Floor(view.Position.X - Margin) * Unit, right = (int)Math.Ceiling(view.End.X + Margin) * Unit;
        int top = (int)Math.Floor(view.Position.Y - Margin) * Unit, bottom = (int)Math.Ceiling(view.End.Y + Margin) * Unit;
        // Kept between frames, refilled: a frame allocates nothing here.
        _out.Clear();
        _drawn.Clear();
        var all = debris.Pieces;
        for (int i = 0; i < all.Count; i++)
        {
            var p = all[i];
            if (grabbed.Contains(p.Id)) continue;
            _out.Add(p.Id);
            if (p.X >= left && p.X <= right && p.Y >= top && p.Y <= bottom) _drawn.Add(p.Id);
        }
        _gone.Clear();
        foreach (int id in _shown.Keys)
            if (!_drawn.Contains(id)) _gone.Add(id);
        foreach (int id in _gone)
        {
            var gone = _shown[id];
            // Gone from the world, not only from the view: broken, or faded out at its end.
            if (!_out.Contains(id) && Fading.Broken(gone.Damage, gone.X, gone.Speed, gone.End, client.World.Tick - gone.Tick))
                Burst(gone.Picture.Position, _own.Ran(id));
            gone.Picture.Visible = false;
            (gone.Wreck ? _wrecks : _small).Push(gone);
            _shown.Remove(id);
        }

        var aimed = ui.AimedPiece.Get();
        _aimed.Begin();
        for (int i = 0; i < all.Count; i++)
        {
            var piece = all[i];
            if (!_drawn.Contains(piece.Id)) continue;
            if (!_shown.TryGetValue(piece.Id, out var one))
            {
                one = Take(piece);
                _shown[piece.Id] = one;
            }
            (one.X, one.Speed, one.End, one.Tick) = (piece.X, piece.Speed, piece.End, client.World.Tick);
            var (x, y) = PieceDrawn.At(piece, progress);
            var at = new Vector2((float)(x / Unit * Cell), (float)(y / Unit * Cell));
            if (piece.Damage > one.Damage) Struck(one, piece, at);
            one.JoltMs = Math.Max(0, one.JoltMs - elapsedMs);
            double jolt = one.JoltMs / JoltMs;
            jolt *= jolt;
            var picture = one.Picture;
            picture.Position = at + one.Away * (float)(JoltPixels * jolt);
            picture.Modulate = new Color(1, 1, 1, (float)Fading.FadeOf(x, piece.Start, piece.End));
            // A slow tumble, each piece its own way; a wreck rocks.
            double turn = one.Wreck
                ? Rock * Math.Sin(_clockMs / RockMs * 2 * Math.PI + piece.Id)
                : _clockMs / 4000 * (piece.Id % 2 == 0 ? 1 : -1) + piece.Id;
            picture.Rotation = (float)(turn + JoltTurn * jolt * (piece.Id % 2 == 0 ? 1 : -1));
            if (aimed is { } a && a.Id == piece.Id)
                _aimed.Add(picture.Texture, picture.Position, picture.Rotation, picture.Scale.X, (a.Refused ? AimRefused : AimReady) with { A = picture.Modulate.A });
        }
        _aimed.End();
    }

    /// <summary>
    /// A picture for <paramref name="piece"/> coming into view: one from the pool of its kind, a wreck's or
    /// a small piece's, else a new one; dressed as the piece, its cracks as its damage, unjolted.
    /// </summary>
    private Shown Take(Piece piece)
    {
        bool wreck = Wreckage.HpOf(piece.Item) != null;
        var pool = wreck ? _wrecks : _small;
        var one = pool.Count > 0 ? pool.Pop() : Made(wreck);
        var picture = one.Picture;
        var (texture, scale) = PictureOf(piece.Item, piece.Id);
        picture.Texture = texture;
        picture.Scale = Vector2.One * scale;
        picture.Visible = true;
        (one.Damage, one.JoltMs, one.Away) = (piece.Damage, 0, Vector2.Zero);
        if (wreck) ShowCracks(picture, piece);
        return one;
    }

    /// <summary>A new picture, hidden: a wreck's or a small piece's.</summary>
    private Shown Made(bool wreck)
    {
        var body = new Sprite2D { Visible = false };
        // Only wrecks crack: a small piece's picture takes no instance uniforms (render/Lit.cs).
        Lit.Set(body, cracks: wreck);
        AddChild(body);
        return new Shown(body, wreck);
    }

    /// <summary>
    /// A hit landed on <paramref name="piece"/>, drawn at <paramref name="at"/>: its cracks deepen, it jolts away
    /// from the nearest hero and sparks fly that way; the chop is heard if the hit was this player's own (0042).
    /// </summary>
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
        if (_own.Ran(piece.Id)) _chop.PlayAt(side);
    }

    /// <summary>
    /// A wreck broken at <paramref name="at"/>: a puff of dust and sparks, and the break heard if this player's
    /// own hit broke it (<paramref name="own"/>, 0042). The last hit removes the wreck on its tick, so its chop
    /// never plays: the break, with an impact of its own, is the hit.
    /// </summary>
    private void Burst(Vector2 at, bool own)
    {
        Particles.Shared.Emit(Effect.Dust, at.X, at.Y, 24, 0, inSpace: true);
        Particles.Shared.Emit(Effect.Dust, at.X, at.Y, 24, Math.PI, inSpace: true);
        Particles.Shared.Emit(Effect.Spark, at.X, at.Y, 14, -Math.PI / 2, inSpace: true);
        if (own) _break.PlayAt(at);
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

    /// <summary>The share of its hit points <paramref name="piece"/>, a wreck, has lost, as cracks laid out by its id.</summary>
    private static void ShowCracks(Sprite2D picture, Piece piece)
    {
        if (Wreckage.HpOf(piece.Item) is int hp) Cracks.Show(picture, (float)piece.Damage / hp, piece.Id);
    }

    /// <summary>
    /// The picture piece <paramref name="id"/> wears (<see cref="DebrisLooks"/>), a pixel of art a pixel of the
    /// world, or its item's the size of a piece, and its scale; each loaded once.
    /// </summary>
    private static (Texture2D Texture, float Scale) PictureOf(string item, int id)
    {
        if (!DebrisLooks.Of.TryGetValue(item, out var looks)) return (ItemIcons.Icon(item), ItemSize * Cell / ItemIcons.Pixels);
        var key = (item, id % looks.Count);
        if (!Textures.TryGetValue(key, out var texture)) Textures[key] = texture = GD.Load<Texture2D>(DebrisLooks.PictureOf(item, id));
        return (texture, 1);
    }
}
