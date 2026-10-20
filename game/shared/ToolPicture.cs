using System;
using System.Linq;
using Godot;
using Stillwake.Game.Features.Effects;
using Stillwake.Game.Render;

namespace Stillwake.Game.Shared;

/// <summary>
/// A tool's picture on its card (the item card mockup, layout A): its held picture whole, <see cref="CardLook.ToolScale"/>
/// times and crisp, in a small dark window. A drill runs on its own, over and over: its bit turning through its frames,
/// its lamp lit, and its light thrown as the hand's is (<see cref="DrillLight"/>) into particles of its own, drawn over it
/// at <see cref="CardLook.EffectScale"/> screen pixels a pixel of the world: sparks off its point, the arc drill's coil
/// crackling in bursts.
/// </summary>
public partial class ToolPicture : Control
{
    /// <summary>The window's width, and the room above and below the picture, in screen pixels.</summary>
    public const int Width = 208, Room = 11;
    /// <summary>The most particles a card's drill throws at once.</summary>
    private const int MostGlims = 300;
    /// <summary>The warm glow at a running bit's point: its colour, across in screen pixels, at full speed.</summary>
    private static readonly (int, int, int) Heat = (255, 190, 110);
    private const double HeatAcross = 22, HeatAlpha = 0.55;
    /// <summary>The lamp's glow round its pixel: across, in its pixels, and how strong.</summary>
    private const float LampGlowAcross = 6.4f, LampGlowAlpha = 0.55f;

    private static readonly CanvasItemMaterial Added = new() { BlendMode = CanvasItemMaterial.BlendModeEnum.Add, LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
    // The window's ground, the mockup's radial-gradient(#121c33, #080d19 75%) a little above the middle.
    private static readonly GradientTexture2D Ground = new()
    {
        Width = 64,
        Height = 64,
        Fill = GradientTexture2D.FillEnum.Radial,
        FillFrom = new Vector2(0.5f, 0.4f),
        FillTo = new Vector2(1.4f, 0.4f),
        Gradient = new Gradient { Offsets = [0f, 0.75f], Colors = [new Color("121c33"), new Color("080d19")] },
    };

    private readonly HeldLook _look;
    private readonly Texture2D _still;
    private readonly Texture2D[] _spin = [];
    private readonly TextureRect _picture;
    private readonly Control[] _lit = [];
    private readonly DrillLight? _light;
    private readonly Flares _flares = new();
    private readonly Func<(double X, double Y), Vector2> _held;
    private double _clockMs, _spun, _frame;

    public ToolPicture(HeldLook look)
    {
        _look = look;
        _still = GD.Load<Texture2D>(look.Loaded ?? look.Picture);
        int scale = CardLook.ToolScale;
        var size = _still.GetSize() * scale;
        CustomMinimumSize = new Vector2(Width, size.Y + 2 * Room);
        ClipContents = true;
        MouseFilter = MouseFilterEnum.Ignore;
        // The arc drill's sparks leap up off its coil: a little more room above it.
        var origin = new Vector2(Mathf.Round((Width - size.X) / 2), Room + (look.Coil != null ? 3 : 0));
        _picture = new TextureRect
        {
            Texture = _still,
            Position = origin,
            Size = size,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            TextureFilter = TextureFilterEnum.Nearest,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(_picture);
        float k = scale / CardLook.EffectScale;
        _held = p => new Vector2((float)p.X * k, (float)p.Y * k);
        if (look.Spin is not { } spin)
        {
            SetProcess(false);
            return;
        }
        _spin = [.. spin.Select(p => GD.Load<Texture2D>(p))];
        if (look.Lamp is { } at)
        {
            var colour = new Color(HeldLook.LampColour);
            var middle = origin + new Vector2((float)at.X, (float)at.Y) * scale;
            var pixel = new ColorRect { Color = colour, Position = middle - Vector2.One * scale / 2, Size = Vector2.One * scale, MouseFilter = MouseFilterEnum.Ignore };
            float across = scale * LampGlowAcross;
            var glow = new TextureRect
            {
                Texture = PixelArt.Glow,
                Position = middle - Vector2.One * across / 2,
                Size = Vector2.One * across,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                Material = Added,
                Modulate = colour with { A = LampGlowAlpha },
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _lit = [pixel, glow];
            foreach (var c in _lit) AddChild(c);
        }
        var glims = new Glims(MostGlims);
        var bolts = new Bolts();
        _light = new DrillLight(glims, _flares, bolts);
        var effects = new Node2D { Position = origin, Scale = Vector2.One * CardLook.EffectScale };
        effects.AddChild(new EffectsView(glims, _flares, bolts, matter: false));
        AddChild(effects);
    }

    /// <summary>The picture of <paramref name="item"/> held, for its card; null for an item held as no picture.</summary>
    public static ToolPicture? Of(string item) => HeldLook.Of(item) is { } look ? new ToolPicture(look) : null;

    public override void _Draw() => DrawWindow(this, Size);

    /// <summary>A card's picture window (the mockup's <c>.win</c>): its dark ground, a shadow along its top and left, a faint light along its bottom and right.</summary>
    public static void DrawWindow(CanvasItem on, Vector2 size)
    {
        on.DrawTextureRect(Ground, new Rect2(Vector2.Zero, size), false);
        var shade = new Color(0, 0, 0, 0.5f);
        on.DrawRect(new Rect2(0, 0, size.X, 2), shade);
        on.DrawRect(new Rect2(0, 2, 2, size.Y - 2), shade);
        var lit = new Color(140 / 255f, 170 / 255f, 220 / 255f, 0.06f);
        on.DrawRect(new Rect2(0, size.Y - 1, size.X, 1), lit);
        on.DrawRect(new Rect2(size.X - 1, 0, 1, size.Y - 1), lit);
    }

    public override void _Process(double delta)
    {
        double ms = delta * 1000;
        double cycle = HeldLook.SpinUpMs + CardLook.RunMs + HeldLook.SpinDownMs + CardLook.RestMs;
        _clockMs = (_clockMs + ms) % cycle;
        bool running = _clockMs < HeldLook.SpinUpMs + CardLook.RunMs;
        // Spun up and down as the hand's is (Figure), through its frames and back to its picture.
        _spun = running ? Math.Min(1, _spun + ms / HeldLook.SpinUpMs) : Math.Max(0, _spun - ms / HeldLook.SpinDownMs);
        _frame = _spun > 0 ? (_frame + _spun * HeldLook.SpinFps * ms / 1000) % (1 + _spin.Length) : 0;
        int frame = (int)_frame;
        _picture.Texture = frame > 0 ? _spin[frame - 1] : _still;
        foreach (var c in _lit) c.Visible = running;
        var tip = _held(_look.EdgeTo);
        // Nothing to bite on a card: the bit throws its sparks into the dark while it runs at speed.
        _light!.Step(_look, _spun, running && _spun > 0.5, tip, Vector2.Right, _held, ms);
        if (_spun > 0)
            _flares.Bloom(tip.X, tip.Y, HeatAcross / CardLook.EffectScale, Heat, HeatAlpha * _spun * _spun);
    }
}
