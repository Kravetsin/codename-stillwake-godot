using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Shared;

/// <summary>
/// A store's liquid in its sight glass, unlit, for the glass is backlit (sight-glass): <c>{kind}_level</c> shown from the glass's
/// bottom up to its level, its surface <c>{kind}_level_top</c> moved down onto it. Placed at the machine's top-left.
/// </summary>
public partial class GlassLevel : Node2D
{
    /// <summary>How thick the surface is, in the world's pixels, as the art draws it.</summary>
    private const float Surface = 0.6f;

    private static readonly CanvasItemMaterial Unlit = new() { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };

    private readonly Sprite2D _body, _top;

    public GlassLevel(string kind)
    {
        AddChild(_body = new Sprite2D { Centered = false, Material = Unlit, RegionEnabled = true, Texture = PixelArt.Picture($"{kind}_level") });
        AddChild(_top = new Sprite2D { Centered = false, Material = Unlit, RegionEnabled = true, Texture = PixelArt.Picture($"{kind}_level_top") });
    }

    /// <summary>Whether a machine of <paramref name="kind"/> has a sight glass to show a level in.</summary>
    public static bool Has(string kind) => HiRes.Load($"{kind}_level") != null;

    /// <summary>Shows <paramref name="amount"/> of <paramref name="capacity"/>.</summary>
    public void Show(long amount, long capacity)
    {
        float height = SightGlass.Height(amount, capacity);
        Visible = height > 0;
        if (!Visible) return;
        float top = SightGlass.Bottom - height, width = _body.Texture.GetSize().X;
        _body.RegionRect = new Rect2(0, top, width, height);
        _body.Position = new Vector2(0, top);
        _top.RegionRect = new Rect2(0, SightGlass.Top, width, Mathf.Min(Surface, height));
        _top.Position = new Vector2(0, top);
    }
}
