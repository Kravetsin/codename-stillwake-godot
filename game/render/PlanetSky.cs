using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// A planet's sky behind its ground (0079), in the "Stillwake" style (<c>art/planet</c>): the day's gradient, a pale gas
/// giant, high thin cloud and the sun with its glint. Drawn in screen space under the world, a pixel of art a pixel of the
/// world at the camera's zoom; the gradient's horizon lies where the land's ground line does, moved by its share of the
/// camera's height. The land in front of it is composed from the planet's own pictures (<c>Features.Backdrop</c>).
/// </summary>
public partial class PlanetSky : Node2D
{
    /// <summary>The row of the gradient where the land meets the ground: the land's ground line lies there.</summary>
    private const float GroundLine = 272;

    /// <summary>How much of the camera's movement the sky, the far things in it and the clouds follow.</summary>
    private const float SkyFollows = 0.04f, CloudFollows = 0.06f;

    /// <summary>How fast the clouds drift by, in pixels of art a second.</summary>
    private const float CloudDrift = 1.5f;

    /// <summary>
    /// Where the sun and the giant hang, their middles as shares of the screen across and down: on the screen, not by the
    /// ground, so they stay in the sky however near the camera is. The clouds' top, likewise.
    /// </summary>
    private static readonly Vector2 Sun = new(0.8f, 0.16f), Giant = new(0.17f, 0.22f);
    private const float CloudsAt = 0.02f;

    private readonly ColorRect _above = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly Sprite2D _sky, _giant, _clouds, _sun;

    /// <param name="set">The folder of its pictures under <c>art</c>: its planet type's backdrop (<see cref="PlanetLooks"/>, 0095).</param>
    public PlanetSky(string set)
    {
        var sky = PixelArt.Far($"{set}/sky");
        _above.Color = TopOf(sky);
        AddChild(_above);
        AddChild(_sky = Layer(sky, repeat: false));
        AddChild(_giant = Layer(PixelArt.Picture($"{set}/giant"), repeat: false));
        AddChild(_clouds = Layer(PixelArt.Far($"{set}/clouds"), repeat: true));
        _sun = Layer(PixelArt.Picture($"{set}/sun"), repeat: false);
        _sun.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        AddChild(_sun);
    }

    private static Sprite2D Layer(Texture2D texture, bool repeat) => new()
    {
        Texture = texture,
        Centered = false,
        RegionEnabled = repeat,
        TextureRepeat = repeat ? CanvasItem.TextureRepeatEnum.Enabled : CanvasItem.TextureRepeatEnum.Disabled,
    };

    /// <summary>
    /// Lays the layers for a camera looking at <paramref name="centre"/> (pixels of the world) at <paramref name="zoom"/> on
    /// a <paramref name="screen"/>, the land's ground line at row <paramref name="ground"/> (pixels), <paramref name="seconds"/> into the world.
    /// </summary>
    public void Follow(Vector2 centre, float zoom, Vector2 screen, float ground, double seconds)
    {
        float sky = screen.Y / 2 + (ground - centre.Y) * zoom * SkyFollows;
        _above.Size = screen;
        var gradient = _sky.Texture.GetSize();
        _sky.Position = new Vector2(0, sky - GroundLine * zoom);
        _sky.Scale = new Vector2(screen.X / gradient.X, zoom);
        _giant.Position = screen * Giant - _giant.Texture.GetSize() * zoom / 2;
        _giant.Scale = Vector2.One * zoom;
        _sun.Position = screen * Sun - _sun.Texture.GetSize() * zoom / 2;
        _sun.Scale = Vector2.One * zoom;
        Spread(_clouds, centre.X * CloudFollows + (float)(seconds * CloudDrift), zoom, screen, screen.Y * CloudsAt);
    }

    /// <summary>A repeating layer over the whole screen's width, its top at <paramref name="y"/>, looking at <paramref name="x"/> pixels of art across.</summary>
    private static void Spread(Sprite2D layer, float x, float zoom, Vector2 screen, float y)
    {
        float across = screen.X / zoom;
        // From its second row: a smoothly sampled repeat would blend its bottom row into its top, a line across the sky.
        layer.RegionRect = new Rect2(Mathf.PosMod(x - across / 2, layer.Texture.GetWidth()), 1, across, layer.Texture.GetHeight() - 1);
        layer.Position = new Vector2(0, y + zoom);
        layer.Scale = Vector2.One * zoom;
    }

    /// <summary>The first pixel of the picture's top row as drawn.</summary>
    private static Color TopOf(Texture2D texture)
    {
        var image = (texture is CanvasTexture { DiffuseTexture: { } painted } ? painted : texture).GetImage();
        if (image.IsCompressed()) image.Decompress();
        return image.GetPixel(0, 0);
    }
}
