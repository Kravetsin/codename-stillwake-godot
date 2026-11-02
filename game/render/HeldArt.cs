using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// A held tool's pictures with its modules' overlays laid over them (<see cref="HeldParts"/>, 0098), put together once a
/// picture and set of layers and kept: the hand and the card draw the same texture, whatever is fitted.
/// </summary>
public static class HeldArt
{
    private static readonly Dictionary<string, Texture2D> Made = new();

    /// <summary><paramref name="picture"/> with <paramref name="layers"/> laid over it in order; the picture itself for none.</summary>
    public static Texture2D Of(string picture, IReadOnlyList<HeldLayer> layers)
    {
        string key = layers.Count == 0 ? picture : picture + "|" + string.Join("|", layers.Select(l => $"{l.Picture}@{l.X},{l.Y}"));
        if (Made.TryGetValue(key, out var kept)) return kept;
        var texture = GD.Load<Texture2D>(picture);
        if (layers.Count == 0) return Made[key] = picture.StartsWith("res://art/hero_", System.StringComparison.Ordinal) ? PixelArt.Picture(NameOf(picture)) : texture;
        if (HiRes.Load(NameOf(picture)) is { } fine)
        {
            foreach (var layer in layers)
            {
                // A module with no picture of its own yet stands in blocks, in its place, rather than turn the tool to pixels.
                var over = HiRes.Load(NameOf(layer.Picture)) ?? Grown(Rgba(GD.Load<Texture2D>(layer.Picture)));
                fine.BlendRect(over, new Rect2I(Vector2I.Zero, over.GetSize()), new Vector2I(layer.X, layer.Y) * HiRes.Scale);
            }
            return Made[key] = HiRes.Texture(fine);
        }
        var image = Rgba(texture);
        foreach (var layer in layers)
        {
            var over = Rgba(GD.Load<Texture2D>(layer.Picture));
            image.BlendRect(over, new Rect2I(Vector2I.Zero, over.GetSize()), new Vector2I(layer.X, layer.Y));
        }
        return Made[key] = ImageTexture.CreateFromImage(image);
    }

    /// <summary>The name <see cref="HiRes.Load"/> knows the picture at <paramref name="path"/> by.</summary>
    private static string NameOf(string path) => path["res://art/".Length..^4];

    private static Image Grown(Image image)
    {
        image.Resize(image.GetWidth() * HiRes.Scale, image.GetHeight() * HiRes.Scale, Image.Interpolation.Nearest);
        return image;
    }

    private static Image Rgba(Texture2D texture)
    {
        var image = texture.GetImage();
        if (image.IsCompressed()) image.Decompress();
        image.Convert(Image.Format.Rgba8);
        return image;
    }
}
