using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Backdrop;

/// <summary>
/// The pictures a planet's land places, packed once into one texture (composed-backdrops), so a layer's thousands of plants
/// reach the renderer as a few triangle arrays rather than one per picture change, which cost thousands of draw calls a frame.
/// Kept at half the hi-res detail: the nearest layer shows a world pixel at most 1.4 screen pixels wide.
/// </summary>
public sealed class BackdropAtlas
{
    private const int Reduce = 2, Gap = 32, Width = 4096;
    private readonly Dictionary<string, Rect2> _uv = new();

    /// <summary>The packed pictures, sampled smoothly with mipmaps.</summary>
    public CanvasTexture Texture { get; }

    public BackdropAtlas(IEnumerable<string> names)
    {
        var images = names.Distinct().Select(n => (Name: n, Image: Halved(HiRes.Load(n) ?? throw new InvalidOperationException($"No picture {n}"))))
            .OrderByDescending(p => p.Image.GetHeight()).ThenBy(p => p.Name, StringComparer.Ordinal).ToList();
        // Shelves left to right, a gap round each so mipmaps never blend in a neighbour.
        var at = new Dictionary<string, Vector2I>();
        int x = Gap, y = Gap, shelf = 0;
        foreach (var (name, image) in images)
        {
            if (x + image.GetWidth() + Gap > Width)
                (x, y, shelf) = (Gap, y + shelf + Gap, 0);
            at[name] = new Vector2I(x, y);
            x += image.GetWidth() + Gap;
            shelf = Math.Max(shelf, image.GetHeight());
        }
        int height = y + shelf + Gap;
        var sheet = Image.CreateEmpty(Width, height, false, Image.Format.Rgba8);
        foreach (var (name, image) in images)
        {
            sheet.BlitRect(image, new Rect2I(Vector2I.Zero, image.GetSize()), at[name]);
            _uv[name] = new Rect2((Vector2)at[name] / new Vector2(Width, height), (Vector2)image.GetSize() / new Vector2(Width, height));
        }
        Texture = new CanvasTexture
        {
            DiffuseTexture = ImageTexture.CreateFromImage(HiRes.Mipped(sheet)),
            TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps,
        };
    }

    /// <summary>Where picture <paramref name="name"/> lies in <see cref="Texture"/>, in its 0..1 coordinates.</summary>
    public Rect2 Uv(string name) => _uv[name];

    private static Image Halved(Image image)
    {
        image.Resize(Math.Max(1, image.GetWidth() / Reduce), Math.Max(1, image.GetHeight() / Reduce), Image.Interpolation.Lanczos);
        return image;
    }
}
