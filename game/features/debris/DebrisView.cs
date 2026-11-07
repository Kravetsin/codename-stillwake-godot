using System;
using System.Collections.Generic;
using System.Linq;
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
/// once (0014). A wreck shows the share of its hit points lost as ink cracks out of each blow (<see cref="WreckCracks"/>); each
/// hit jolts it and throws sparks, and a wreck broken bursts apart, for every peer alike; only this
/// player's own hits are heard (0042).
/// </summary>
public partial class DebrisView(Ui ui) : Node2D
{
    private const float Cell = ShipCamera.Cell;
    private const int Unit = DebrisModule.Unit;
    /// <summary>A wreck rocks this far either way, in radians, once in <see cref="RockMs"/>.</summary>
    private const double Rock = 0.08, RockMs = 7000;
    /// <summary>A hit knocks a piece <see cref="JoltPixels"/> away from the nearest hero and turns it <see cref="JoltTurn"/>, settling over <see cref="JoltMs"/>.</summary>
    private const double JoltMs = 220, JoltPixels = 3, JoltTurn = 0.07;
    /// <summary>Cells round the camera's view a piece is still drawn in: the biggest wreck's half and a frame's drift.</summary>
    private const int Margin = 8;
    /// <summary>Pictures made at the start, small pieces' and wrecks': more than the far field shows in view at once (0052).</summary>
    private const int WarmSmall = 48, WarmWrecks = 32;
    private static readonly Color AimReady = Outline.AimReady, AimRefused = Outline.AimRefused;

    /// <summary>What is shown of a piece: its picture, the damage last seen, the jolt still settling and which way, and where it was on which tick.</summary>
    private sealed class Shown(Sprite2D picture, WreckInk? cracks)
    {
        public Sprite2D Picture { get; } = picture;
        /// <summary>A wreck's cracks over its picture; none for a small piece. A wreck's picture goes back to the wrecks' pool.</summary>
        public WreckInk? Cracks { get; } = cracks;
        public bool Wreck => Cracks != null;
        public int Damage { get; set; }
        public double JoltMs { get; set; }
        /// <summary>How brightly it still flares from its last hit, 1 at the hit.</summary>
        public double Flash { get; set; }
        public Vector2 Away { get; set; }
        public int X { get; set; }
        public int Speed { get; set; }
        public int End { get; set; }
        public int Tick { get; set; }
    }

    private readonly Dictionary<int, Shown> _shown = new();
    /// <summary>Pictures not drawn now, small pieces' and wrecks' apart, as their materials differ: kept, never freed.</summary>
    private readonly Stack<Shown> _small = new(), _wrecks = new();
    /// <summary>The piece aimed at, outlined over the rest.</summary>
    private readonly Outline _aimed = new(1);
    private readonly Heard2D _chop = Cues.Player2D(S.Chop, S.Suit, voices: 4), _break = Cues.Player2D(S.Break, S.Suit, voices: 2),
        _spearHit = Cues.Player2D(S.SpearHit, S.Suit, voices: 2), _hammerBlow = Cues.Player2D(S.HammerBlow, S.Suit, voices: 2);
    private readonly OwnHits _own = new();
    private double _clockMs;
    /// <summary>Every piece out but those this peer is grabbing, and of them the ones drawn this frame.</summary>
    private readonly HashSet<int> _out = [], _drawn = [];
    private readonly Dictionary<int, double> _turns = new();
    private readonly List<int> _gone = [];
    /// <summary>The blows seen landing on each wreck still out, kept while it drifts out of view and back (<see cref="WreckCracks"/>).</summary>
    private readonly Dictionary<int, List<Blow>> _blows = new();

    /// <summary>Each cracked wreck's network, laid out once from its first blow (<see cref="WreckCracks.Network"/>).</summary>
    private readonly Dictionary<int, CrackNet> _nets = new();

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
        AddChild(_spearHit);
        AddChild(_hammerBlow);
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
        _own.Read(client.InFlight, h => debris.Struck(h.Tool, h.X, h.Y, h.Heading).Select(p => p.Id).ToList());
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
            if (!_out.Contains(id) && Fading.Broken(gone.Wreck, debris.CaughtFrom(id) != null, gone.X, gone.Speed, gone.End, client.World.Tick - gone.Tick))
                Burst(gone.Picture, _own.Ran(id));
            gone.Picture.Visible = false;
            (gone.Wreck ? _wrecks : _small).Push(gone);
            _shown.Remove(id);
        }
        _gone.Clear();
        foreach (int id in _blows.Keys)
            if (!_out.Contains(id)) _gone.Add(id);
        foreach (int id in _gone)
        {
            _blows.Remove(id);
            _nets.Remove(id);
        }

        var aimed = ui.AimedPiece.Get();
        _turns.Clear();
        ui.DebrisTurns = _turns;
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
            if (_own.SentOn(piece.Id) is { } sent && !HeldLook.IsDrill(sent)) Felt(one, at, sent, null, own: true);
            if (piece.Damage > one.Damage) Struck(one, piece, at);
            one.Cracks?.Zoomed();
            one.JoltMs = Math.Max(0, one.JoltMs - elapsedMs);
            double jolt = one.JoltMs / JoltMs;
            jolt *= jolt;
            var picture = one.Picture;
            picture.Position = at + one.Away * (float)(JoltPixels * jolt);
            // A struck wreck flares, its picture brighter before the light falls on it, dying away smoothly.
            one.Flash *= Math.Pow(FlashDecay, elapsedMs / FrameMs);
            picture.Modulate = one.Flash > 0.02 ? Colors.White + FlashTint * (float)one.Flash : Colors.White;
            // A slow tumble, each piece its own way; a wreck rocks.
            double turn = one.Wreck
                ? Rock * Math.Sin(_clockMs / RockMs * 2 * Math.PI + piece.Id)
                : _clockMs / 4000 * (piece.Id % 2 == 0 ? 1 : -1) + piece.Id;
            picture.Rotation = (float)(turn + JoltTurn * jolt * (piece.Id % 2 == 0 ? 1 : -1));
            _turns[piece.Id] = picture.Rotation;
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
        picture.Texture = PictureOf(piece.Item, piece.Id);
        picture.Scale = Vector2.One;
        picture.Visible = true;
        (one.Damage, one.JoltMs, one.Away, one.Flash) = (piece.Damage, 0, Vector2.Zero, 0);
        if (wreck) ShowCracks(one, piece);
        return one;
    }

    /// <summary>A new picture, hidden: a wreck's or a small piece's.</summary>
    private Shown Made(bool wreck)
    {
        var body = new Sprite2D { Visible = false };
        Lit.Set(body);
        var cracks = wreck ? new WreckInk() : null;
        if (cracks != null) body.AddChild(cracks);
        AddChild(body);
        return new Shown(body, cracks);
    }

    /// <summary>
    /// A hit landed on <paramref name="piece"/>, drawn at <paramref name="at"/>: its cracks deepen, and unless it was this
    /// player's own hit with a weapon swung, felt as it was sent (0014), it is felt now (<see cref="Felt"/>): under a running
    /// drill's bit on it, that drill's share of the axe's hit; else the axe's whole.
    /// </summary>
    private void Struck(Shown one, Piece piece, Vector2 at)
    {
        var size = one.Picture.Texture.GetSize() * one.Picture.Scale;
        var bit = BitOn(at, size);
        if (Wreckage.HpOf(piece.Item) is int hp)
        {
            // Where it lands: under a drill's bit, else on the side facing the hero who most likely struck, on its picture.
            var away = (at - NearestHero(at)).Normalized();
            var landed = (bit?.At ?? at - away * Math.Min(size.X, size.Y) * 0.4f) - at;
            var local = (landed.Rotated(-one.Picture.Rotation) / one.Picture.Scale).Clamp(-size * 0.35f, size * 0.35f);
            WreckCracks.Land(BlowsOf(piece, size), local.X, local.Y, (double)one.Damage / hp);
        }
        one.Damage = piece.Damage;
        ShowCracks(one, piece);
        if (_own.ToolOn(piece.Id) is { } own && !HeldLook.IsDrill(own)) return;
        string tool = _own.ToolOn(piece.Id) ?? bit?.Tool ?? Content.Weapons.Axe.Item;
        // A running tool is seen at work all along where it touches (tool-contact): its hits only crack the wreck.
        if (HeldLook.IsDrill(tool)) return;
        Felt(one, at, tool, bit?.At, own: false);
    }

    /// <summary>
    /// A hit by <paramref name="tool"/> felt on a piece drawn at <paramref name="at"/>: it jolts away from the nearest hero and
    /// flares, and a wreck's hit (0075, 0094) flies back toward it from <paramref name="bit"/>, a drill's, else the side facing
    /// the hero; this player's <paramref name="own"/> hit, sent this frame, is heard and nudges the view a pixel (0042). An own
    /// drill's chops are heard as each is sent (HeroView).
    /// </summary>
    private void Felt(Shown one, Vector2 at, string tool, Vector2? bit, bool own)
    {
        var from = NearestHero(at);
        var away = (at - from).Normalized();
        if (away == Vector2.Zero) away = Vector2.Right;
        one.Away = away;
        one.JoltMs = JoltMs;
        var size = one.Picture.Texture.GetSize() * one.Picture.Scale;
        var side = at - away * Math.Min(size.X, size.Y) * 0.4f;
        var point = HeldLook.IsDrill(tool) && bit is { } on ? on : side;
        double share = Features.Effects.Impacts.ShareOf(tool);
        Stages.Of(Layers.Effects).Hits.Throw(HitLooks.Of[Impact.Wreck], point.X, point.Y, -away.X, -away.Y, share);
        one.Flash = Math.Max(one.Flash, share);
        if (!own) return;
        HitBy(tool).PlayAt(side);
        Nudged?.Invoke(away);
    }

    /// <summary>
    /// The running drill's bit on a wreck drawn at <paramref name="at"/>, <paramref name="size"/> across, if one is within
    /// <see cref="BitMargin"/> pixels of its picture's box: where on the box it bites, and which drill it is.
    /// </summary>
    private (Vector2 At, string Tool)? BitOn(Vector2 at, Vector2 size)
    {
        var half = size / 2;
        (Vector2 At, string Tool)? nearest = null;
        float best = float.MaxValue;
        foreach (var (x, y, tool) in ui.Bits)
        {
            var bit = new Vector2(x, y);
            var on = new Vector2(Math.Clamp(bit.X, at.X - half.X, at.X + half.X), Math.Clamp(bit.Y, at.Y - half.Y, at.Y + half.Y));
            float gap = bit.DistanceTo(on);
            if (gap > BitMargin || gap >= best) continue;
            (best, nearest) = (gap, (on, tool));
        }
        return nearest;
    }

    /// <summary>How far off a wreck's picture's box a drill's bit, as drawn, still bites it, in pixels: the aim box's margin and a frame's drift.</summary>
    private const float BitMargin = 4;

    private const double FrameMs = 1000.0 / 60;
    private static readonly (int, int, int) BreakGlint = (255, 214, 150), ShockColour = (255, 236, 210);
    /// <summary>How much brighter a struck wreck's picture flares, warm, as its flash dies away by <see cref="FlashDecay"/> a 60 Hz frame.</summary>
    private static readonly Color FlashTint = new(0.16f, 0.14f, 0.12f, 0);
    private const double FlashDecay = 0.84;
    /// <summary>How strongly a break lights what is round it at first: spots add over the room's light, so less than the page drew.</summary>
    private const double BreakFlash = 0.45;

    /// <summary>This player's own axe bit a wreck, knocking it <c>away</c>: the view nudges (<see cref="ShipCamera.Nudge"/>).</summary>
    public event Action<Vector2>? Nudged;

    /// <summary>The player of a wreck's hit by this player's <paramref name="tool"/>: the chop, the spear's point or the sledgehammer's blow (salvage-tools).</summary>
    private Heard2D HitBy(string tool) => S.WreckHitOf(tool) is var hit && hit == S.HammerBlow ? _hammerBlow : hit == S.SpearHit ? _spearHit : _chop;

    /// <summary>
    /// A wreck broken at <paramref name="at"/> (0075): its picture cut into pieces flying apart with glowing torn edges
    /// (<see cref="BreakView"/>), a big glint, sparks curling every way, fine dust, a slow cloud, a fast faint shock
    /// ring and a flash of light dying away; the break heard if this player's own hit broke it (<paramref name="own"/>,
    /// 0042), after the last hit's own sound, heard as it was sent (0014). A bigger picture, a derelict's, bursts with more,
    /// twice as much for the biggest.
    /// </summary>
    private void Burst(Sprite2D picture, bool own)
    {
        var at = picture.Position;
        var size = picture.Texture.GetSize() * picture.Scale;
        float big = Math.Clamp(size.Length() / 26f, 1f, 2f);
        AddChild(new BreakView(picture.Texture, at, picture.Rotation, picture.Scale.X));
        var glims = Stages.Of(Layers.Effects).Glims;
        double spread = 3 * big;
        for (int i = 0; i < Math.Round(BreakSparks * big * Fx.Count); i++)
        {
            double a = glims.Between(0, Math.Tau), s = glims.Between(40, 150);
            glims.Add(Luminous.BreakSpark, at.X + glims.Between(-spread, spread), at.Y + glims.Between(-2, 2), Math.Cos(a) * s, Math.Sin(a) * s);
        }
        for (int i = 0; i < Math.Round(BreakDust * big * Fx.Count); i++)
        {
            double a = glims.Between(0, Math.Tau), s = glims.Between(4, 30);
            glims.Add(Luminous.BreakDust, at.X + glims.Between(-2 * spread, 2 * spread), at.Y + glims.Between(-4, 4), Math.Cos(a) * s, Math.Sin(a) * s,
                bright: glims.Between(0.55, 0.9), size: glims.Between(0.4, 0.75));
        }
        for (int i = 0; i < Vapour; i++)
        {
            double a = glims.Between(0, Math.Tau), s = glims.Between(4, 14);
            glims.Add(Luminous.Vapour, at.X + Math.Cos(a) * 3 * big, at.Y + Math.Sin(a) * 2 * big, Math.Cos(a) * s, Math.Sin(a) * s,
                size: glims.Between(3, 4.5) * big);
        }
        Stages.Of(Layers.Effects).Flares.Glint(at.X, at.Y, 18 * Math.Sqrt(big), BreakGlint);
        Stages.Of(Layers.Effects).Flares.Shock(at.X, at.Y, 4, 4 + 40 * big * Fx.Length, 14 * FrameMs, ShockColour, 0.4 * Fx.Glow, 0.32);
        Stages.Of(Layers.Effects).Flares.Flash(at.X, at.Y, (255, 226, 180), BreakFlash * Fx.Glow, 4, 0.45f, 0.9);
        if (own) _break.PlayAt(at);
    }

    /// <summary>Sparks and dust a breaking wreck throws, the puffs of its cloud: a derelict's up to twice as many.</summary>
    private const int BreakSparks = 26, BreakDust = 44, Vapour = 10;

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

    /// <summary>The share of its hit points <paramref name="piece"/>, a wreck, has lost, as cracks out of its blows laid out by its id.</summary>
    private void ShowCracks(Shown one, Piece piece)
    {
        if (Wreckage.HpOf(piece.Item) is int hp && one.Cracks is { } cracks)
        {
            var size = one.Picture.Texture.GetSize() * one.Picture.Scale;
            var blows = BlowsOf(piece, size);
            cracks.Show(blows, NetOf(piece, blows, size), (double)piece.Damage / hp, piece.Id);
        }
    }

    /// <summary>
    /// The crack network of <paramref name="piece"/>, laid out once out of its first blow over its picture's own pixels (its box, if
    /// the picture will not load); none before a blow.
    /// </summary>
    private CrackNet? NetOf(Piece piece, IReadOnlyList<Blow> blows, Vector2 size)
    {
        if (blows.Count == 0) return null;
        if (_nets.TryGetValue(piece.Id, out var net)) return net;
        var image = DebrisLooks.NameOf(piece.Item, piece.Id) is { } name ? HiRes.Load(name) : null;
        Func<double, double, bool> solid = image == null
            ? (x, y) => Math.Abs(x) <= size.X / 2 && Math.Abs(y) <= size.Y / 2
            : (x, y) =>
            {
                // The hi-res picture is the drawn one at its own scale, so a point from the middle is read at its share across.
                double perX = image.GetWidth() / (double)size.X, perY = image.GetHeight() / (double)size.Y;
                int ix = (int)Math.Floor((x + size.X / 2) * perX), iy = (int)Math.Floor((y + size.Y / 2) * perY);
                return ix >= 0 && iy >= 0 && ix < image.GetWidth() && iy < image.GetHeight() && image.GetPixel(ix, iy).A > DamageLooks.Opaque;
            };
        return _nets[piece.Id] = WreckCracks.Network(blows[0], solid, piece.Id);
    }

    /// <summary>The blows seen on <paramref name="piece"/>; one seen damaged first, with none, struck in its middle (<see cref="WreckCracks.Unseen"/>).</summary>
    private List<Blow> BlowsOf(Piece piece, Vector2 size)
    {
        if (_blows.TryGetValue(piece.Id, out var blows)) return blows;
        _blows[piece.Id] = blows = [];
        if (piece.Damage > 0) blows.Add(WreckCracks.Unseen(piece.Id, size.X, size.Y));
        return blows;
    }

    /// <summary>
    /// The picture piece <paramref name="id"/> wears (<see cref="DebrisLooks"/>), or its item's as the world draws it (a tool's
    /// as it is held), a pixel of art a pixel of the world; each loaded once.
    /// </summary>
    internal static Texture2D PictureOf(string item, int id) =>
        DebrisLooks.NameOf(item, id) is { } name ? PixelArt.Picture(name) : ItemIcons.Icon(item);
}
