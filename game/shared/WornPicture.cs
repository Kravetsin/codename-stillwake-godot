using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Shared;

/// <summary>
/// A worn item's picture on its card (the item card mockup's medium card), in a small dark window
/// (<see cref="DrawWindow"/>): its icon <see cref="CardLook.WornScale"/> times; the helmet lamp on the helmet
/// it is worn on, its warm beam reaching out across the window with motes drifting through it, as wide as the game's
/// (<see cref="Light.HeadLampHalfAngle"/>); a battery glowing green as much as it holds, slowly pulsing.
/// </summary>
public partial class WornPicture : Control
{
    /// <summary>The window's side, and the helmet lamp's wider one, in screen pixels.</summary>
    private const int Side = 72, LampWidth = 132, HelmetLeft = 6;
    /// <summary>Where the lamp's bulb is on the helmet's picture, and where the battery's glow sits on its icon, in their pixels.</summary>
    private static readonly Vector2 Bulb = new(12.4f, 3.2f), Cell = new(8, 9.5f);
    private static readonly Color BulbColour = new("fff4d6"), BulbGlow = new("ffd696"), MoteColour = new("ffecc8");
    private const float BeamAlpha = 0.42f, BeamMiddle = 0.15f, BulbGlowAcross = 24, ChargeGlowAcross = 36;

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

    private sealed class Mote
    {
        public float X, Y, Speed, Phase, Bright;
    }

    private readonly Random _random = new();
    private readonly Mote[] _motes = [];
    private readonly Control? _light;
    private readonly Vector2 _bulb;
    private readonly float _share;
    private readonly TextureRect? _glow;
    private double _seconds;

    public WornPicture(ItemCard card)
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ClipContents = true;
        bool lamp = card.Item == Content.HelmetLamp.Item;
        CustomMinimumSize = new Vector2(lamp ? LampWidth : Side, Side);
        int scale = lamp ? CardLook.HelmetScale : CardLook.WornScale;
        var texture = lamp ? GD.Load<Texture2D>("res://art/hero_helmet.png") : ItemIcons.Icon(card.Picture);
        var size = texture.GetSize() * scale;
        var origin = new Vector2(lamp ? HelmetLeft : Mathf.Round((Side - size.X) / 2), Mathf.Round((Side - size.Y) / 2));
        AddChild(new TextureRect
        {
            Texture = texture,
            Position = origin,
            Size = size,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            TextureFilter = TextureFilterEnum.Nearest,
            MouseFilter = MouseFilterEnum.Ignore,
        });
        if (lamp)
        {
            _bulb = origin + Bulb * scale;
            _motes = new Mote[CardLook.Motes];
            for (int i = 0; i < _motes.Length; i++)
                _motes[i] = new Mote
                {
                    X = Between(0, LampWidth),
                    Y = Between(0, Side),
                    Speed = Between(CardLook.MoteSlowest, CardLook.MoteFastest),
                    Phase = Between(0, Mathf.Tau),
                    Bright = Between(0.6f, 1.1f),
                };
            // Made only for the lamp: a node never put in the tree is never freed.
            _light = new Control { Material = Added, Size = new Vector2(LampWidth, Side), MouseFilter = MouseFilterEnum.Ignore };
            _light.Draw += DrawBeam;
            AddChild(_light);
            return;
        }
        if (card.Charge is not { } charge)
        {
            SetProcess(false);
            return;
        }
        _share = charge.Share;
        var cell = origin + Cell * scale;
        _glow = new TextureRect
        {
            Texture = PixelArt.Glow,
            Position = cell - Vector2.One * ChargeGlowAcross / 2,
            Size = Vector2.One * ChargeGlowAcross,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            Material = Added,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(_glow);
        _Process(0);
    }

    private float Between(float from, float to) => from + (to - from) * (float)_random.NextDouble();

    public override void _Draw() => DrawWindow(this, Size);

    /// <summary>A card's picture window (the mockup's <c>.win</c>): its dark ground, a shadow along its top and left, a faint light along its bottom and right.</summary>
    private static void DrawWindow(CanvasItem on, Vector2 size)
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
        _seconds += delta;
        if (_glow != null)
        {
            float pulse = CardLook.ChargeGlow + CardLook.ChargePulse * (float)Math.Sin(_seconds * 1.7);
            _glow.Modulate = ItemCardView.Green with { A = _share * pulse };
            return;
        }
        foreach (var m in _motes)
        {
            m.X += m.Speed * (float)delta;
            m.Y += (float)(Math.Sin(_seconds * 0.9 + m.Phase) * 3 * delta);
            if (m.X > LampWidth + 2) (m.X, m.Y) = (-2, Between(4, Side - 4));
        }
        _light?.QueueRedraw();
    }

    /// <summary>The lamp's beam, bright at the bulb and gone at its reach, the motes lit inside it, the bulb and its glow.</summary>
    private void DrawBeam()
    {
        var on = _light!;
        var (r, g, b) = Light.HeadLamp;
        var warm = new Color(r, g, b);
        float reach = LampWidth - _bulb.X + 10, half = Light.HeadLampHalfAngle, near = reach * 0.45f;
        const int Steps = 8;
        for (int i = 0; i < Steps; i++)
        {
            float a0 = -half + 2 * half * i / Steps, a1 = -half + 2 * half * (i + 1) / Steps;
            Vector2 d0 = Vector2.FromAngle(a0), d1 = Vector2.FromAngle(a1);
            on.DrawPolygon([_bulb, _bulb + d0 * near, _bulb + d1 * near],
                [warm with { A = BeamAlpha }, warm with { A = BeamMiddle }, warm with { A = BeamMiddle }]);
            on.DrawPolygon([_bulb + d0 * near, _bulb + d0 * reach, _bulb + d1 * reach, _bulb + d1 * near],
                [warm with { A = BeamMiddle }, warm with { A = 0 }, warm with { A = 0 }, warm with { A = BeamMiddle }]);
        }
        float spread = Mathf.Tan(half);
        foreach (var m in _motes)
        {
            float dx = m.X - _bulb.X, dy = Math.Abs(m.Y - _bulb.Y);
            if (dx <= 0 || dy >= spread * dx) continue;
            float lit = (1 - dy / (spread * dx)) * (1 - dx / reach) * m.Bright;
            if (lit <= 0.02f) continue;
            on.DrawRect(new Rect2(m.X - 0.5f, m.Y - 0.5f, 1, 1), MoteColour with { A = 0.7f * lit });
            on.DrawTextureRect(SoftDots.Dot, new Rect2(m.X - 2.2f, m.Y - 2.2f, 4.4f, 4.4f), false, BulbGlow with { A = 0.3f * lit });
        }
        on.DrawRect(new Rect2(_bulb - Vector2.One * 2, Vector2.One * 4), BulbColour);
        on.DrawTextureRect(PixelArt.Glow, new Rect2(_bulb - Vector2.One * BulbGlowAcross / 2, Vector2.One * BulbGlowAcross), false, BulbGlow with { A = 0.7f });
    }
}
