using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// The ship's pictures from <c>game/art</c> (exported from the TypeScript ASCII art, a pixel of art a
/// pixel of the world), and for those lit with relief a normal map worked out from each picture's own
/// brightness, so bevels, rivets and seams catch a lamp from the side they face.
/// </summary>
public static class PixelArt
{
    private static readonly Dictionary<string, Texture2D> Loaded = new();
    private static readonly Dictionary<string, CanvasTexture> WithNormals = new();

    /// <summary>The picture <c>res://art/{name}.png</c>, loaded once.</summary>
    public static Texture2D Picture(string name)
    {
        if (!Loaded.TryGetValue(name, out var texture))
            Loaded[name] = texture = GD.Load<Texture2D>($"res://art/{name}.png");
        return texture;
    }

    /// <summary>The picture with its normal map, for a picture drawn on its own (a socket).</summary>
    public static CanvasTexture Lit(string name)
    {
        if (WithNormals.TryGetValue(name, out var kept)) return kept;
        var diffuse = Picture(name);
        var image = diffuse.GetImage();
        if (image.IsCompressed()) image.Decompress();
        kept = new CanvasTexture { DiffuseTexture = diffuse, NormalTexture = ImageTexture.CreateFromImage(NormalMap(image)) };
        WithNormals[name] = kept;
        return kept;
    }

    /// <summary>
    /// A normal map of <paramref name="image"/>, its brightness read as height. A clear pixel stands at
    /// <paramref name="clearHeight"/>, so an overlay such as a wall's edge, drawn over a wall, has no
    /// cliff where it ends; a seamless picture laid over the world <paramref name="wraps"/> at its edges.
    /// </summary>
    public static Image NormalMap(Image image, float clearHeight = 0, bool wraps = false, float strength = 2.5f)
    {
        int w = image.GetWidth(), h = image.GetHeight();
        var normal = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        float Height(int x, int y)
        {
            var c = wraps
                ? image.GetPixel((x + w) % w, (y + h) % h)
                : image.GetPixel(Mathf.Clamp(x, 0, w - 1), Mathf.Clamp(y, 0, h - 1));
            return c.A < 0.5f ? clearHeight : c.Luminance;
        }
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                // Sobel slopes, screen-wise: x to the right, y down.
                float dx = Height(x + 1, y - 1) + 2 * Height(x + 1, y) + Height(x + 1, y + 1)
                         - Height(x - 1, y - 1) - 2 * Height(x - 1, y) - Height(x - 1, y + 1);
                float dy = Height(x - 1, y + 1) + 2 * Height(x, y + 1) + Height(x + 1, y + 1)
                         - Height(x - 1, y - 1) - 2 * Height(x, y - 1) - Height(x + 1, y - 1);
                // Godot reads normal maps Y-up (OpenGL), so the screen's downward slope is stored as it is.
                var n = new Vector3(-dx * strength, dy * strength, 1f).Normalized();
                normal.SetPixel(x, y, new Color(n.X * 0.5f + 0.5f, n.Y * 0.5f + 0.5f, n.Z * 0.5f + 0.5f));
            }
        return normal;
    }

    /// <summary>A white disc fading to nothing at its edge, to tint: what makes a lamp glow.</summary>
    public static GradientTexture2D Glow { get; } = new()
    {
        Width = 64,
        Height = 64,
        Fill = GradientTexture2D.FillEnum.Radial,
        FillFrom = new Vector2(0.5f, 0.5f),
        FillTo = new Vector2(1f, 0.5f),
        Gradient = new Gradient
        {
            Offsets = [0f, 0.35f, 1f],
            Colors = [new Color(1, 1, 1, 0.9f), new Color(1, 1, 1, 0.35f), new Color(1, 1, 1, 0)],
        },
    };
}
