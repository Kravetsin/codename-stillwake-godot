using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// The ship's pictures from <c>game/art</c> (exported from the TypeScript ASCII art, a pixel of art a
/// pixel of the world), and for those lit with relief a normal map of each picture's whole shape, so
/// the side toward a lamp is the brighter.
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
        kept = new CanvasTexture { DiffuseTexture = diffuse, NormalTexture = ImageTexture.CreateFromImage(VolumeMap(image)) };
        WithNormals[name] = kept;
        return kept;
    }

    /// <summary>
    /// A normal map of <paramref name="image"/>'s whole shape, not its detail: a gentle dome over the box
    /// its opaque pixels fill, or with <paramref name="across"/> a gentle roll from side to side only (a
    /// door, drawn a cell at a time, would show each cell's dome). Lit, the side toward a light is
    /// brighter than the side away from it, and no rivet or seam casts a shadow.
    /// </summary>
    public static Image VolumeMap(Image image, bool across = false)
    {
        // How far the edge of the shape leans out: past about half a seam reads as a hard rim.
        const float Lean = 0.55f;
        int w = image.GetWidth(), h = image.GetHeight();
        var box = image.GetUsedRect();
        var normal = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        float cx = box.Position.X + box.Size.X / 2f, cy = box.Position.Y + box.Size.Y / 2f;
        float hw = Mathf.Max(1, box.Size.X / 2f), hh = Mathf.Max(1, box.Size.Y / 2f);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = Mathf.Clamp((x + 0.5f - cx) / hw, -1, 1), v = across ? 0 : Mathf.Clamp((y + 0.5f - cy) / hh, -1, 1);
                // Screen-wise the normal leans (u, v); Godot reads normal maps Y-up (OpenGL), so v is stored turned.
                var n = new Vector3(u * Lean, -v * Lean, 1f).Normalized();
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
