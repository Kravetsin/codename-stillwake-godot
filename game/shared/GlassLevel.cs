using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Shared;

/// <summary>
/// A store's liquid in its sight glass, unlit, for the glass is backlit (sight-glass): <c>{kind}_level</c>, the glass full, shown from
/// its bottom up to the level, its surface <c>{kind}_level_top</c> moved down onto it. The glass is where that picture draws: its
/// top and bottom found once a kind. Placed at the machine's top-left.
/// </summary>
public partial class GlassLevel : Node2D
{
    /// <summary>How thick the surface is, in the world's pixels, as the art draws it.</summary>
    private const float Surface = 0.6f;

    private static readonly CanvasItemMaterial Unlit = new() { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };

    /// <summary>Each kind's glass, its top and bottom in the world's pixels, found in its level picture.</summary>
    private static readonly Dictionary<string, (float Top, float Bottom)> Glasses = new();

    private readonly Sprite2D _body, _top;
    private readonly float _glassTop, _glassBottom;

    public GlassLevel(string kind)
    {
        AddChild(_body = new Sprite2D { Centered = false, Material = Unlit, RegionEnabled = true, Texture = PixelArt.Picture($"{kind}_level") });
        AddChild(_top = new Sprite2D { Centered = false, Material = Unlit, RegionEnabled = true, Texture = PixelArt.Picture($"{kind}_level_top") });
        if (!Glasses.TryGetValue(kind, out var glass)) Glasses[kind] = glass = Find($"{kind}_level");
        (_glassTop, _glassBottom) = glass;
    }

    /// <summary>Whether a machine of <paramref name="kind"/> has a sight glass to show a level in.</summary>
    public static bool Has(string kind) => HiRes.Load($"{kind}_level") != null;

    /// <summary>The rows picture <paramref name="name"/> draws anything on, top and bottom, in the world's pixels: its glass.</summary>
    private static (float Top, float Bottom) Find(string name)
    {
        var image = HiRes.Load(name)!;
        int top = -1, bottom = -1;
        for (int y = 0; y < image.GetHeight(); y++)
            for (int x = 0; x < image.GetWidth(); x++)
                if (image.GetPixel(x, y).A > 0.5f)
                {
                    if (top < 0) top = y;
                    bottom = y;
                    break;
                }
        return (top / (float)HiRes.Scale, (bottom + 1) / (float)HiRes.Scale);
    }

    /// <summary>Shows <paramref name="amount"/> of <paramref name="capacity"/>.</summary>
    public void Show(long amount, long capacity)
    {
        float height = SightGlass.Height(amount, capacity, _glassBottom - _glassTop);
        Visible = height > 0;
        if (!Visible) return;
        float top = _glassBottom - height, width = _body.Texture.GetSize().X;
        _body.RegionRect = new Rect2(0, top, width, height);
        _body.Position = new Vector2(0, top);
        _top.RegionRect = new Rect2(0, _glassTop, width, Mathf.Min(Surface, height));
        _top.Position = new Vector2(0, top);
    }
}
