using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Shared;

/// <summary>
/// A worn item's or food's picture on its card (the item card mockup's medium card), in a small dark window
/// (<see cref="DrawWindow"/>): its icon <see cref="CardLook.WornScale"/> times, the light drone's, a small one,
/// <see cref="CardLook.DroneScale"/> times; a battery glowing green as much as it holds, slowly pulsing.
/// </summary>
public partial class WornPicture : Control
{
    /// <summary>The window's side, in screen pixels.</summary>
    private const int Side = 72;
    /// <summary>Where the battery's glow sits on its icon, in its pixels.</summary>
    private static readonly Vector2 Cell = new(8, 9.5f);
    private const float ChargeGlowAcross = 36;

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

    private readonly float _share;
    private readonly TextureRect? _glow;
    private double _seconds;

    public WornPicture(ItemCard card)
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ClipContents = true;
        CustomMinimumSize = new Vector2(Side, Side);
        bool drone = card.Item == Content.LightDrone.Item;
        int scale = drone ? CardLook.DroneScale : CardLook.WornScale;
        var texture = ItemIcons.Icon(card.Picture);
        var size = texture.GetSize() * scale;
        var origin = new Vector2(Mathf.Round((Side - size.X) / 2), Mathf.Round((Side - size.Y) / 2));
        AddChild(new TextureRect
        {
            Texture = texture,
            Position = origin,
            Size = size,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            TextureFilter = drone ? TextureFilterEnum.Linear : TextureFilterEnum.Nearest,
            MouseFilter = MouseFilterEnum.Ignore,
        });
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

    public override void _Draw() => DrawWindow(this, Size);

    /// <summary>A card's picture window (the mockup's <c>.win</c>): its dark ground, a shadow along its top and left, a faint light along its bottom and right.</summary>
    internal static void DrawWindow(CanvasItem on, Vector2 size)
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
        }
    }
}
