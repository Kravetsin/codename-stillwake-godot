using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// A planet's sky behind its ground (0079), in the "Stillwake" style (<c>art/planet</c>): the day's gradient, a pale gas
/// giant, high thin cloud, the sun with its glint, then far mesas, a nearer ridge and hills with spindly trees, each layer
/// further off moving less with the camera. Drawn in screen space under the world, a pixel of art a pixel of the world at
/// the camera's zoom, each layer repeating across; every layer's ground line lies where the world's surface does,
/// moved by its share of the camera's height, and what lies under a layer is filled with its bottom colour.
/// </summary>
public partial class PlanetSky : Node2D
{
    /// <summary>The row of the art where its land meets the ground: the world's surface lies there.</summary>
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

    /// <summary>The land, far to near: its picture, the art's row its top is at, and how much of the camera's movement it follows.</summary>
    private static readonly (string Name, float Top, float Follows)[] Land = [("far", 96, 0.1f), ("ridge", 140, 0.18f), ("hills", 150, 0.3f)];

    private readonly ColorRect _above = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly Sprite2D _sky, _giant, _clouds, _sun;
    private readonly List<(Sprite2D Sprite, ColorRect Under, float Top, float Follows)> _land = [];

    /// <param name="set">The folder of its pictures under <c>art</c>: its planet type's backdrop (<see cref="PlanetLooks"/>, 0095).</param>
    public PlanetSky(string set)
    {
        var sky = PixelArt.Picture($"{set}/sky");
        _above.Color = TopOf(sky);
        AddChild(_above);
        AddChild(_sky = Layer(sky, repeat: false));
        AddChild(_giant = Layer(PixelArt.Picture($"{set}/giant"), repeat: false));
        AddChild(_clouds = Layer(PixelArt.Picture($"{set}/clouds"), repeat: true));
        _sun = Layer(PixelArt.Picture($"{set}/sun"), repeat: false);
        _sun.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        AddChild(_sun);
        foreach (var (name, top, follows) in Land)
        {
            var texture = PixelArt.Picture($"{set}/{name}");
            var under = new ColorRect { Color = BottomOf(texture), MouseFilter = Control.MouseFilterEnum.Ignore };
            var sprite = Layer(texture, repeat: true);
            AddChild(under);
            AddChild(sprite);
            _land.Add((sprite, under, top, follows));
        }
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
    /// a <paramref name="screen"/>, the world's surface at row <paramref name="ground"/> (pixels), <paramref name="seconds"/> into the world.
    /// </summary>
    public void Follow(Vector2 centre, float zoom, Vector2 screen, float ground, double seconds)
    {
        // Where a layer following this share of the camera has its ground line on the screen.
        float LineOf(float follows) => screen.Y / 2 + (ground - centre.Y) * zoom * follows;
        float sky = LineOf(SkyFollows);
        _above.Size = screen;
        var gradient = _sky.Texture.GetSize();
        _sky.Position = new Vector2(0, sky - GroundLine * zoom);
        _sky.Scale = new Vector2(screen.X / gradient.X, zoom);
        _giant.Position = screen * Giant - _giant.Texture.GetSize() * zoom / 2;
        _giant.Scale = Vector2.One * zoom;
        _sun.Position = screen * Sun - _sun.Texture.GetSize() * zoom / 2;
        _sun.Scale = Vector2.One * zoom;
        Spread(_clouds, centre.X * CloudFollows + (float)(seconds * CloudDrift), zoom, screen, screen.Y * CloudsAt);
        foreach (var (sprite, under, top, follows) in _land)
        {
            float y = LineOf(follows) + (top - GroundLine) * zoom;
            Spread(sprite, centre.X * follows, zoom, screen, y);
            float bottom = y + sprite.Texture.GetHeight() * zoom;
            under.Position = new Vector2(0, bottom - 1);
            under.Size = new Vector2(screen.X, Mathf.Max(0, screen.Y - bottom + 1));
        }
    }

    /// <summary>A repeating layer over the whole screen's width, its top at <paramref name="y"/>, looking at <paramref name="x"/> pixels of art across.</summary>
    private static void Spread(Sprite2D layer, float x, float zoom, Vector2 screen, float y)
    {
        float across = screen.X / zoom;
        layer.RegionRect = new Rect2(Mathf.PosMod(x - across / 2, layer.Texture.GetWidth()), 0, across, layer.Texture.GetHeight());
        layer.Position = new Vector2(0, y);
        layer.Scale = Vector2.One * zoom;
    }

    private static Color TopOf(Texture2D texture) => PixelOf(texture, 0);

    private static Color BottomOf(Texture2D texture) => PixelOf(texture, texture.GetHeight() - 1);

    private static Color PixelOf(Texture2D texture, int row)
    {
        var image = texture.GetImage();
        if (image.IsCompressed()) image.Decompress();
        return image.GetPixel(0, row);
    }
}
