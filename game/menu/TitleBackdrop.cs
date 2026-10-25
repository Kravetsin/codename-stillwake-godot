using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Render;

namespace Stillwake.Game.Menu;

/// <summary>
/// The title's scene (<see cref="TitleScene"/>, 2026-10-25-title-scene), drawn here, none of it the game's world: the game's
/// stars going by to the left, and the
/// plate (<see cref="PlateArt"/>) drifting right, turning slowly, its hero sitting hunched on its end, tethered to it, a
/// red emergency lamp blinking beside him, the only warm light there is. Shards of metal and now and then a limp astronaut
/// come from the right and go by behind the plate. The hero is drawn unlit, in the cold of the planet's light.
/// </summary>
public partial class TitleBackdrop : CanvasLayer
{
    private static readonly Color Space = new("03050a");
    private static readonly Color Cold = new(0.46f, 0.52f, 0.68f), Red = new(1f, 0.22f, 0.16f);

    /// <summary>The plate's shades: shadow, body, lit top, rivet, seam, rib, the dark inside its frame, wire.</summary>
    private static readonly Dictionary<PlatePixel, Color> Shades = new()
    {
        [PlatePixel.Shadow] = new("141923"),
        [PlatePixel.Body] = new("2c3443"),
        [PlatePixel.Lit] = new("56627a"),
        [PlatePixel.Rivet] = new("6f7c94"),
        [PlatePixel.Seam] = new("1c222e"),
        [PlatePixel.Rib] = new("2a3242"),
        [PlatePixel.Frame] = new("0c1017"),
        [PlatePixel.Wire] = new("3a3f4c"),
    };

    /// <summary>How long the lamp's blink takes, on and off, and how much of it it is lit, in seconds.</summary>
    private const double BlinkSeconds = 3.4, LitSeconds = 0.7;

    private readonly Node2D _scene = new(), _behind = new(), _plate = new();
    private readonly Sprite2D _stars = Stars.Sky();
    private readonly Figure _hero = new(Suits.All[0], own: false);
    private readonly Line2D _tether = new() { Width = 1, DefaultColor = new Color("2a3140"), Antialiased = false };
    private readonly Sprite2D _lamp = new();
    /// <summary>The lamp's light round it, where the lamp is.</summary>
    private readonly Node2D _glowAt = new();
    private readonly ColorRect _glow = Dither.Glow(Red, 96);
    private readonly PlateArt _art = PlateArt.Of(7);
    private readonly List<(Drifter Path, double From, Node2D Drawn, Figure? Figure)> _drifting = [];
    private readonly Random _rng = new();
    private double _seconds, _nextDrifter = TitleScene.FirstDrifter, _nextShard, _look = TitleScene.HeadHangs;
    private float _stream;

    public TitleBackdrop()
    {
        Layer = ScreenLayers.TitleScene;
        var space = new ColorRect { Color = Space, MouseFilter = Control.MouseFilterEnum.Ignore };
        space.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(space);
        AddChild(_stars);
        AddChild(_scene);
        _scene.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
        _scene.AddChild(_behind);
        _scene.AddChild(_plate);
        var plate = new Sprite2D { Texture = PlateTexture(_art), Centered = false, Position = new Vector2(-_art.Width / 2f, -_art.Height / 2f) };
        _lamp.Texture = Dot();
        _plate.AddChild(plate);
        _plate.AddChild(_tether);
        _plate.AddChild(_hero);
        _plate.AddChild(_lamp);
        _glowAt.AddChild(_glow);
        _plate.AddChild(_glowAt);
        Unlit(_hero);
        _hero.Modulate = Cold;
        for (int i = 0; i < 4; i++) Shard(_rng.NextDouble() * TitleScene.Wide);
    }

    /// <summary>A pixel of the plate, from its top-left, in the plate's own frame about its middle.</summary>
    private Vector2 OnPlate((int X, int Y) pixel) => new(pixel.X - _art.Width / 2f, pixel.Y - _art.Height / 2f);

    public override void _Process(double delta)
    {
        _seconds += delta;
        var screen = GetViewport().GetVisibleRect().Size;
        float scale = Mathf.Max(screen.X / TitleScene.Wide, screen.Y / TitleScene.High);
        _scene.Scale = Vector2.One * scale;
        _scene.Position = (screen - new Vector2(TitleScene.Wide, TitleScene.High) * scale) / 2;
        _stream = (_stream + (float)delta * Stars.Speed) % 4096;
        _stars.RegionRect = new Rect2(new Vector2(_stream, 0), screen);

        var (turn, dy) = TitleScene.Sway(_seconds);
        _plate.Position = new Vector2((float)TitleScene.PlateX, (float)(TitleScene.PlateY + dy));
        _plate.Rotation = (float)turn;
        _plate.Scale = Vector2.One * TitleScene.PlateScale;

        // The lamp: off, a quick flare, a slow fade; its red on the hero as it burns.
        double phase = _seconds % BlinkSeconds;
        float lit = phase < LitSeconds ? (float)Math.Sin(phase / LitSeconds * Math.PI) : 0;
        var lamp = OnPlate(_art.Lamp) + new Vector2(0, -1);
        _lamp.Position = lamp;
        _lamp.Modulate = new Color(0.35f, 0.08f, 0.06f).Lerp(Red, lit);
        _glowAt.Position = lamp;
        Dither.Shine(_glow, 0.8f * lit);
        _hero.Modulate = Cold.Lerp(new Color(0.9f, 0.42f, 0.38f), 0.3f * lit);

        if (_seconds >= _nextDrifter)
        {
            Drifter(TitleScene.NextDrifter(_rng));
            _nextDrifter = _seconds + TitleScene.Gap(_rng, TitleScene.DrifterGapLeast, TitleScene.DrifterGapMost);
        }
        if (_seconds >= _nextShard)
        {
            Shard(null);
            _nextShard = _seconds + TitleScene.Gap(_rng, TitleScene.ShardGapLeast, TitleScene.ShardGapMost);
        }
        // The hero's head hangs; it lifts a little, slowly, to an astronaut going by before it, and sinks again.
        double look = TitleScene.HeadHangs - TitleScene.HeadSighs * Poses.Sigh(_seconds * 1000);
        var seat = OnPlate(_art.Seat) * TitleScene.PlateScale + _plate.Position;
        for (int i = _drifting.Count - 1; i >= 0; i--)
        {
            var (path, from, drawn, figure) = _drifting[i];
            if (path.At(_seconds - from) is not var (x, y, spin))
            {
                drawn.QueueFree();
                _drifting.RemoveAt(i);
                continue;
            }
            drawn.Position = new Vector2((float)x, (float)y);
            drawn.Rotation = (float)spin;
            if (figure == null) continue;
            figure.Show(new Hero(0, 0, 0, 0, false, 1), Pose.Tumble, 0, false, delta * 1000);
            if (x > seat.X + 20 && x < seat.X + 260) look = Math.Min(look, Math.Max(TitleScene.HeadLifts, Poses.HeadTurn(x - seat.X, y - (seat.Y - Rig.Neck))));
        }
        _look += (look - _look) * (1 - Math.Exp(-delta / TitleScene.HeadSeconds));
        _hero.OnPixels = OnPlate(_art.Seat);
        _hero.Show(new Hero(0, 0, 0, 0, false, 1), Pose.Sit, _look, false, delta * 1000);
        Tether();
    }

    /// <summary>The tether from the hero's belt to its ring on the plate, hanging slack and stirring.</summary>
    private void Tether()
    {
        var belt = OnPlate(_art.Seat) + new Vector2(-2, -(float)(Rig.Hip * 0.15));
        var ring = OnPlate(_art.Anchor);
        const int points = 14;
        var line = new Vector2[points];
        float sag = 7 + 1.5f * Mathf.Sin((float)_seconds * 0.7f);
        for (int i = 0; i < points; i++)
        {
            float t = i / (points - 1f);
            line[i] = ring.Lerp(belt, t) + new Vector2(0, sag * 4 * t * (1 - t));
        }
        _tether.Points = line;
    }

    private void Drifter(Drifter path)
    {
        var figure = new Figure(Suits.All[_rng.Next(1, Suits.All.Length)], own: false);
        Unlit(figure);
        // Turned about its middle, not its feet.
        var middle = new Node2D { Position = new Vector2(0, (float)(Rig.H / 2)) };
        middle.AddChild(figure);
        var holder = new Node2D { Scale = Vector2.One * (float)(0.45 + 0.55 * path.Depth) };
        holder.Modulate = (Cold * (float)(0.35 + 0.45 * path.Depth)) with { A = 1 };
        holder.AddChild(middle);
        Drift(path, holder, figure);
    }

    /// <summary>A shard of metal coming in, or already on its way <paramref name="at"/> across for the first moments.</summary>
    private void Shard(double? at)
    {
        var path = TitleScene.NextShard(_rng);
        double size = 3 + 9 * path.Depth;
        var outline = TitleScene.ShardOf(_rng, size);
        var points = new Vector2[outline.Length];
        for (int i = 0; i < outline.Length; i++) points[i] = new Vector2((float)outline[i].X, (float)outline[i].Y);
        float shade = (float)(0.25 + 0.75 * path.Depth);
        var shard = new Polygon2D { Polygon = points, Color = new Color("0d1119").Lerp(new Color("3a4356"), shade), Antialiased = false };
        shard.AddChild(new Line2D { Points = [.. points, points[0]], Width = 1, DefaultColor = new Color("0d1119").Lerp(new Color("5d6a82"), shade) with { A = 0.7f } });
        double from = _seconds;
        if (at is double x) from -= (path.FromX - x) / (path.FromX - path.ToX) * path.Seconds;
        _behind.AddChild(shard);
        _drifting.Add((path, from, shard, null));
    }

    private void Drift(Drifter path, Node2D drawn, Figure? figure)
    {
        _behind.AddChild(drawn);
        _drifting.Add((path, _seconds, drawn, figure));
    }

    /// <summary>Takes the world's light off everything a figure draws: no world lights it here.</summary>
    private static void Unlit(Node node)
    {
        if (node is CanvasItem { Material: ShaderMaterial } item) item.Material = null;
        foreach (var child in node.GetChildren()) Unlit(child);
    }

    private static ImageTexture PlateTexture(PlateArt art)
    {
        var image = Image.CreateEmpty(art.Width, art.Height, false, Image.Format.Rgba8);
        for (int y = 0; y < art.Height; y++)
            for (int x = 0; x < art.Width; x++)
                if (Shades.TryGetValue(art.At(x, y), out var colour)) image.SetPixel(x, y, colour);
        return ImageTexture.CreateFromImage(image);
    }

    private static Texture2D Dot()
    {
        var image = Image.CreateEmpty(3, 2, false, Image.Format.Rgba8);
        image.Fill(Colors.White);
        return ImageTexture.CreateFromImage(image);
    }
}
