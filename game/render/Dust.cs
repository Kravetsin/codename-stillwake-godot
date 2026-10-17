using Godot;
using static Stillwake.Game.Backdrop;

namespace Stillwake.Game.Render;

/// <summary>
/// Dust between the far wrecks and the ship (render/README.md): faint specks a screen pixel or two wide
/// whatever the zoom, drifting by faster than the wrecks, so space has depth and the ship moves through
/// it. A speck is a dot in a clear texel border drawn filtered, so it glides by fractions of a pixel as
/// the stars do rather than jumping whole ones.
/// </summary>
public partial class Dust : DriftLayer
{
    private const uint Seed = 23;
    // The dots: one texel in a 3×3 cell, two by two in a 4×4 cell beside it.
    private static readonly Rect2 Small = new(0, 0, 3, 3), Big = new(3, 0, 4, 4);

    private readonly Texture2D _dots;

    public Dust() : base(Seed, DustTile, DustMean, DustParallax, DustDrift, 0)
    {
        TextureFilter = TextureFilterEnum.Linear;
        var image = Image.CreateEmpty(7, 4, false, Image.Format.Rgba8);
        image.Fill(new Color(1, 1, 1, 0));
        image.SetPixel(1, 1, Colors.White);
        image.FillRect(new Rect2I(4, 1, 2, 2), Colors.White);
        _dots = ImageTexture.CreateFromImage(image);
    }

    protected override void DrawSpot(Vector2 at, uint hash)
    {
        var dot = Scatter.Next(ref hash) < DustBig ? Big : Small;
        float alpha = Mathf.Lerp(DustAlphaMin, DustAlphaMax, Scatter.Next(ref hash));
        // A texel a screen pixel: the plane is drawn scaled by the zoom.
        var size = dot.Size / Zoom;
        DrawTextureRectRegion(_dots, new Rect2(at - size / 2, size), dot, new Color(DustColour.R, DustColour.G, DustColour.B, alpha));
    }
}
