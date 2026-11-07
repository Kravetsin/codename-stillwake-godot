using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// The anime art's pictures: <c>res://art/hd/{name}.png</c>, <see cref="Scale"/> times the pixels of the picture they stand for,
/// reporting that picture's size so nothing that lays pictures out by their size changes, and sampled smoothly whatever node draws them.
/// </summary>
public static class HiRes
{
    /// <summary>How many pixels of a hi-res picture stand for one of the world's; zoom reaches 2.5, the rest is room for the screen.</summary>
    public const int Scale = 4;

    /// <summary>The world's pixels a hi-res hero part has clear round its pixel picture's box: for its outline, which a limb's box would cut.</summary>
    public const int FigureMargin = 3;

    /// <summary>The hi-res picture standing for <paramref name="name"/> as pixels, RGBA8; none if it has none.</summary>
    public static Image? Load(string name)
    {
        string path = $"res://art/hd/{name}.png";
        if (!ResourceLoader.Exists(path)) return null;
        var image = GD.Load<Texture2D>(path).GetImage();
        if (image.IsCompressed()) image.Decompress();
        image.Convert(Image.Format.Rgba8);
        return image;
    }

    /// <summary>Whether <paramref name="name"/> has a hi-res picture.</summary>
    public static bool Has(string name) => ResourceLoader.Exists($"res://art/hd/{name}.png");

    private static readonly Dictionary<(string, int, bool), Image> Reduced = new();

    /// <summary>
    /// The picture <paramref name="name"/> at the world's pixels, its hi-res one reduced <paramref name="by"/> times (<see cref="Reduce.Pixels"/>), a
    /// copy: what reads a picture's size or shape (a light's panel, a planting row, a tile's cells) rather than draws it; <paramref name="soft"/> keeps
    /// part-covered pixels part clear, for what weighs a glow.
    /// </summary>
    public static Image Logical(string name, int by = Scale, bool soft = false)
    {
        if (!Reduced.TryGetValue((name, by, soft), out var made))
        {
            var fine = Load(name) ?? throw new System.InvalidOperationException($"No picture {name}");
            int w = fine.GetWidth(), h = fine.GetHeight();
            Reduced[(name, by, soft)] = made = Image.CreateFromData(w / by, h / by, false, Image.Format.Rgba8, Reduce.Pixels(fine.GetData(), w, h, by, soft));
        }
        return (Image)made.Duplicate();
    }

    private static readonly Dictionary<string, Vector2> Middles = new();

    /// <summary>The middle of light picture <paramref name="name"/> by brightness, in the world's pixels from its corner: where a light it shows gives off.</summary>
    public static Vector2 MiddleOf(string name)
    {
        if (Middles.TryGetValue(name, out var known)) return known;
        var image = Logical(name, soft: true);
        var sum = Vector2.Zero;
        float weight = 0;
        for (int y = 0; y < image.GetHeight(); y++)
            for (int x = 0; x < image.GetWidth(); x++)
            {
                float a = image.GetPixel(x, y).A;
                sum += new Vector2(x + 0.5f, y + 0.5f) * a;
                weight += a;
            }
        return Middles[name] = weight > 0 ? sum / weight : (Vector2)image.GetSize() / 2;
    }

    /// <summary>
    /// A packed sheet of <paramref name="placed"/> pictures: <paramref name="logical"/> grown <see cref="Scale"/> times, each picture with a
    /// hi-res one (named with <paramref name="suffix"/>) put in its place, at the size <paramref name="logical"/> reports.
    /// </summary>
    public static Texture2D Sheet(Image logical, IEnumerable<(string Name, Rect2 At)> placed, string suffix = "")
    {
        var sheet = (Image)logical.Duplicate();
        sheet.Resize(logical.GetWidth() * Scale, logical.GetHeight() * Scale, Image.Interpolation.Nearest);
        foreach (var (name, at) in placed)
            if (Load(name + suffix) is { } fine)
                sheet.BlitRect(fine, new Rect2I(Vector2I.Zero, fine.GetSize()), (Vector2I)at.Position * Scale);
        return Sampled(ImageTexture.CreateFromImage(Mipped(sheet)), logical.GetSize());
    }

    /// <summary>The picture <paramref name="image"/>, <see cref="Scale"/> times too large, to draw at its proper size.</summary>
    public static Texture2D Texture(Image image)
    {
        var size = image.GetSize() / Scale;
        return Sampled(ImageTexture.CreateFromImage(Mipped(image)), size);
    }

    /// <summary><paramref name="image"/> with mipmaps made and no dark fringe: clear pixels take their neighbours' colour first.</summary>
    public static Image Mipped(Image image)
    {
        var made = (Image)image.Duplicate();
        if (made.HasMipmaps()) made.ClearMipmaps();
        made.FixAlphaEdges();
        made.GenerateMipmaps();
        return made;
    }

    /// <summary>
    /// <paramref name="texture"/> reporting <paramref name="size"/>, its own filter overriding the pixel art's nearest of the nodes drawing it;
    /// with its <paramref name="normal"/> map and its <paramref name="specular"/> one, where ore shines (<c>render/Lit.cs</c>).
    /// </summary>
    public static CanvasTexture Sampled(ImageTexture texture, Vector2I size, Texture2D? normal = null, Texture2D? specular = null)
    {
        texture.SetSizeOverride(size);
        return new CanvasTexture
        {
            DiffuseTexture = texture,
            NormalTexture = normal,
            SpecularTexture = specular,
            TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps,
        };
    }
}
