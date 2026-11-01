using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// The anime art's pictures: <c>res://art/hd/{name}.png</c>, <see cref="Scale"/> times the pixels of the picture they stand for,
/// reporting that picture's size so nothing that lays pictures out by their size changes, and sampled smoothly whatever node draws them.
/// Off until the switch (2026-11-01-anime-cel-art): <c>-- --anime-art</c> or an <c>--anime</c> demo turns it on; off, the game is drawn as it was.
/// </summary>
public static class HiRes
{
    /// <summary>Whether the new art is drawn.</summary>
    public static readonly bool On = OS.GetCmdlineUserArgs().Any(a => a is "--anime-art" or "--anime" || a.StartsWith("--anime=", System.StringComparison.Ordinal));

    /// <summary>How many pixels of a hi-res picture stand for one of the world's; zoom reaches 2.5, the rest is room for the screen.</summary>
    public static int Scale => On ? 4 : 1;

    /// <summary>The world's pixels a hi-res hero part has clear round its pixel picture's box: for its outline, which a limb's box would cut.</summary>
    public const int FigureMargin = 3;

    /// <summary>The hi-res picture standing for <paramref name="name"/> as pixels, RGBA8; none if it has none.</summary>
    public static Image? Load(string name)
    {
        string path = $"res://art/hd/{name}.png";
        if (!On || !ResourceLoader.Exists(path)) return null;
        var image = GD.Load<Texture2D>(path).GetImage();
        if (image.IsCompressed()) image.Decompress();
        image.Convert(Image.Format.Rgba8);
        return image;
    }

    /// <summary>
    /// A packed sheet of <paramref name="placed"/> pictures: <paramref name="logical"/> grown <see cref="Scale"/> times, each picture with a
    /// hi-res one (named with <paramref name="suffix"/>) put in its place, at the size <paramref name="logical"/> reports.
    /// </summary>
    public static Texture2D Sheet(Image logical, IEnumerable<(string Name, Rect2 At)> placed, string suffix = "")
    {
        if (!On) return ImageTexture.CreateFromImage(logical);
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

    /// <summary><paramref name="texture"/> reporting <paramref name="size"/>, its own filter overriding the pixel art's nearest of the nodes drawing it.</summary>
    public static CanvasTexture Sampled(ImageTexture texture, Vector2I size, Texture2D? normal = null)
    {
        texture.SetSizeOverride(size);
        return new CanvasTexture
        {
            DiffuseTexture = texture,
            NormalTexture = normal,
            TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps,
        };
    }
}
