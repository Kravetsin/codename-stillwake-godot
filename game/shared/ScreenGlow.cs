using Godot;

namespace Stillwake.Game.Shared;

/// <summary>
/// The soft light a screen or a status lamp casts on what is round it, as a picture added over what is under it:
/// the build tablet's (0073) and the wrist computer's (0074). Not a light of the lightmap: the HUD is not in the world.
/// </summary>
public static class ScreenGlow
{
    /// <summary>A soft round light of <paramref name="colour"/>, its alpha whole out to <paramref name="core"/> of its radius, added to what is under it.</summary>
    public static TextureRect Of(Color colour, float core)
    {
        var gradient = new Gradient
        {
            Offsets = [0, core, 0.45f, 1],
            Colors = [colour, colour, colour with { A = colour.A * 0.45f }, colour with { A = 0 }],
        };
        return new TextureRect
        {
            Texture = new GradientTexture2D
            {
                Gradient = gradient,
                Fill = GradientTexture2D.FillEnum.Radial,
                FillFrom = new Vector2(0.5f, 0.5f),
                FillTo = new Vector2(1, 0.5f),
                Width = 128,
                Height = 128,
            },
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            TextureFilter = CanvasItem.TextureFilterEnum.Linear,
            Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add },
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
    }
}
